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
		public SharedComponentTypeHandle<RenderMesh> RenderMeshHandle;

		[NativeSetThreadIndex]
		public int ThreadIndex;

		public NativeArray<UnsafeAtomicCounter> RenderCountByRenderMeshIndex;

#if RENDERER_DEBUG
		public NativeAtomicCounter.ParallelWriter VisibleObjectCount;
		public NativeAtomicCounter.ParallelWriter FrustumOutCount;
		public NativeAtomicCounter.ParallelWriter FrustumInCount;
		public NativeAtomicCounter.ParallelWriter FrustumPartialCount;
#endif

		public ComponentTypeHandle<ChunkCullResult> ChunkCullResultHandle;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var chunkWorldRenderBounds = chunk.GetChunkComponentData(ref ChunkWorldRenderBoundsHandle);
			var chunkAabb = chunkWorldRenderBounds.AABB;
			var chunkIntersection = FrustumPlanes.Intersect2(PlanePackets, chunkAabb);
			var renderMeshIndex = chunk.GetSharedComponentIndex(RenderMeshHandle);

			switch (chunkIntersection)
			{
				case FrustumPlanes.IntersectResult.Out:
				{
					// No Entity is visible, don't need to check Entity AABB's.
					var cullResult = new ChunkCullResult
					{
						EntityVisibilityMask = new BitField128(new v128(0)),
						IntersectResult = chunkIntersection,
					};
					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
#if RENDERER_DEBUG
					FrustumOutCount.Increment();
#endif
					break;
				}
				case FrustumPlanes.IntersectResult.In:
				{
					// All Entities are visible, no need to check Entity AABBs individually.
					var cullResult = new ChunkCullResult
					{
						EntityVisibilityMask = new BitField128(new v128(0)),
						IntersectResult = chunkIntersection,
					};

					int visibleEntityCount;

					// Fast path
					if (!useEnabledMask)
					{
						// All entities of the Chunk are visible, but might not have 128 entities
						cullResult.EntityVisibilityMask.SetBitsFromStart(true, chunk.Count);
						visibleEntityCount = chunk.Count;
					}
					else
					{
						// Check individually
						var enumerator = new ChunkEntityEnumerator(true, chunkEnabledMask, chunk.Count);

						while (enumerator.NextEntityIndex(out var entityIndex))
						{
							cullResult.EntityVisibilityMask.SetBit(entityIndex, true);
						}

						visibleEntityCount = cullResult.EntityVisibilityMask.CountBits();
					}

					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);

					ref var counter = ref RenderCountByRenderMeshIndex.ElementAsRef(renderMeshIndex);
					counter.Add(ThreadIndex, visibleEntityCount);
					
#if RENDERER_DEBUG
					VisibleObjectCount.Increment(visibleEntityCount);
					FrustumInCount.Increment();
#endif
					break;
				}
				case FrustumPlanes.IntersectResult.Partial:
				{
#if RENDERER_DEBUG
					FrustumPartialCount.Increment();
#endif

					// Check Each Entity individually
					{
						var visibleEntityCount = 0;
						var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
						var cullResult = new ChunkCullResult
						{
							EntityVisibilityMask = new BitField128(new v128(0)),
							IntersectResult = chunkIntersection,
						};
						var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

						while (enumerator.NextEntityIndex(out var entityIndex))
						{
							var aabb = worldRenderBoundsArray[entityIndex].AABB;
							var intersectResult = FrustumPlanes.Intersect2(PlanePackets, aabb);
							var isVisible = intersectResult != FrustumPlanes.IntersectResult.Out;
							visibleEntityCount += isVisible ? 1 : 0;
							cullResult.EntityVisibilityMask.SetBit(entityIndex, isVisible);
						}

						ref var counter = ref RenderCountByRenderMeshIndex.ElementAsRef(renderMeshIndex);
						counter.Add(ThreadIndex, visibleEntityCount);
#if RENDERER_DEBUG
						VisibleObjectCount.Increment(visibleEntityCount);
#endif
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