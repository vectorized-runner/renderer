using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using FrustumPlanes = Renderer.UnityPackages.FrustumPlanes;

namespace Renderer
{
	[BurstCompile]
	public struct ChunkCullingJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;

		[ReadOnly]
		public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;

		[ReadOnly]
		public NativeArray<FrustumPlanes.PlanePacket4> PlanePackets;

		[ReadOnly]
		public SharedComponentTypeHandle<RenderMeshIndex> RenderMeshIndexHandle;

		[NativeSetThreadIndex]
		public int ThreadIndex;

		public NativeArray<UnsafeAtomicCounter> RenderCountByRenderMeshIndex;

		// TODO-Renderer: Make these counters debug mode only behind define
		public NativeAtomicCounter.ParallelWriter CulledObjectCount;
		public NativeAtomicCounter.ParallelWriter FrustumOutCount;
		public NativeAtomicCounter.ParallelWriter FrustumInCount;
		public NativeAtomicCounter.ParallelWriter FrustumPartialCount;

		public ComponentTypeHandle<ChunkCullResult> ChunkCullResultHandle;

		// UseEnabledMask here is provided by Unity. If RenderMesh was enable-able component, it would save us from checking 
		// if(IsComponentEnabled) checks
		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var chunkWorldRenderBounds = chunk.GetChunkComponentData(ref ChunkWorldRenderBoundsHandle);
			var chunkAabb = chunkWorldRenderBounds.AABB;
			var chunkIntersection = FrustumPlanes.Intersect2(PlanePackets, chunkAabb);

			switch (chunkIntersection)
			{
				case FrustumPlanes.IntersectResult.Out:
				{
					// No Entity is visible, don't need to check Entity AABB's.
					var cullResult = new ChunkCullResult { Value = new BitField128(new v128(0)) };
					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					CulledObjectCount.Increment(chunk.Count);
					FrustumOutCount.Increment();
					break;
				}
				case FrustumPlanes.IntersectResult.In:
				{
					// All Entities are visible, no need to check Entity AABB's.
					
					// Notice optimization: Not setting the actual bitmask here, other job can check it
					// (Entity count + Enabled/Disabled components)
					var visibleEntityCount = useEnabledMask ? new BitField128(chunkEnabledMask).CountBits() : chunk.Count;
					var cullResult = new ChunkCullResult { Value = new BitField128(new v128(ulong.MaxValue)) };
					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					
					var renderMeshIndex = chunk.GetSharedComponent(RenderMeshIndexHandle).Value;
					ref var counter = ref RenderCountByRenderMeshIndex.ElementAsRef(renderMeshIndex);
					counter.Add(ThreadIndex, visibleEntityCount);
					
					FrustumInCount.Increment();
					break;
				}
				case FrustumPlanes.IntersectResult.Partial:
				{
					FrustumPartialCount.Increment();

					// Check Each Entity individually
					{
						var visibleEntityCount = 0;
						var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
						var cullResult = new ChunkCullResult();
						var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

						while (enumerator.NextEntityIndex(out var entityIndex))
						{
							var aabb = worldRenderBoundsArray[entityIndex].AABB;
							var intersectResult = FrustumPlanes.Intersect2(PlanePackets, aabb);
							var isVisible = intersectResult != FrustumPlanes.IntersectResult.Out;
							visibleEntityCount += isVisible ? 1 : 0;
							cullResult.Value.SetBits(entityIndex, isVisible);
						}

						var renderMeshIndex = chunk.GetSharedComponent(RenderMeshIndexHandle).Value;
						ref var counter = ref RenderCountByRenderMeshIndex.ElementAsRef(renderMeshIndex);
						counter.Add(ThreadIndex, visibleEntityCount);
						
						var culledEntityCount = chunk.Count - visibleEntityCount;
						CulledObjectCount.Increment(culledEntityCount);
						chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					}
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}