using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Profiling;
using FrustumPlanes = Renderer.UnityPackages.FrustumPlanes;

namespace Renderer
{
	[BurstCompile]
	public struct ChunkCullingJob : IJobChunk
	{
		[ReadOnly] public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;
		[ReadOnly] public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;
		[ReadOnly] public NativeArray<FrustumPlanes.PlanePacket4> PlanePackets;

		public AtomicCounter CulledObjectCount;
		public ComponentTypeHandle<ChunkCullResult> ChunkCullResultHandle;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var chunkWorldRenderBounds = chunk.GetChunkComponentData(ref ChunkWorldRenderBoundsHandle);
			var chunkAabb = chunkWorldRenderBounds.AABB;
			var chunkIntersection = FrustumPlanes.Intersect2(PlanePackets, chunkAabb);
			var frustumMarker = new ProfilerMarker("Frustum");
			var interlockedMarker = new ProfilerMarker("Interlocked");
			var setBitsMarker = new ProfilerMarker("SetBits");
			var partialCullMarker = new ProfilerMarker("PartialCull");

			switch (chunkIntersection)
			{
				case FrustumPlanes.IntersectResult.Out:
				{
					// No Entity is visible, don't need to check Entity AABB's.
					var cullResult = new ChunkCullResult { Lower = new BitField64(0), Upper = new BitField64(0) };
					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					CulledObjectCount.Add(128);
					break;
				}
				case FrustumPlanes.IntersectResult.In:
				{
					// All Entities are visible, no need to check Entity AABB's.
					var cullResult = new ChunkCullResult { Lower = new BitField64(ulong.MaxValue), Upper = new BitField64(ulong.MaxValue) };
					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					break;
				}
				case FrustumPlanes.IntersectResult.Partial:
				{
					// Check Each Entity individually
					partialCullMarker.Begin();
					{
						var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
						var cullResult = new ChunkCullResult();
						var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
						
						while (enumerator.NextEntityIndex(out var entityIndex))
						{
							var aabb = worldRenderBoundsArray[entityIndex].AABB;
							
							frustumMarker.Begin();
							var intersectResult = FrustumPlanes.Intersect2(PlanePackets, aabb);
							frustumMarker.End();
							
							var isVisible = intersectResult != FrustumPlanes.IntersectResult.Out;
							
							interlockedMarker.Begin();
							CulledObjectCount.Add(isVisible ? 0 : 1);
							interlockedMarker.End();

							setBitsMarker.Begin();
							
							// TODO: Remove Lower/Upper branch here. Could inline ChunkEntityEnumerator here
							var lower = entityIndex < 64;

							if (lower)
								cullResult.Lower.SetBits(entityIndex, isVisible);
							else
								cullResult.Upper.SetBits(64 - entityIndex, isVisible);
							
							setBitsMarker.End();
						}

						chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					}
					partialCullMarker.End();
					
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}