using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
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
		public ComponentTypeHandle<RenderMeshIndex> RenderMeshIndexHandle;

		[ReadOnly]
		public NativeArray<FrustumPlanes.PlanePacket4> PlanePackets;

		// TODO-Renderer: Make these counters debug mode only behind define
		public NativeAtomicCounter.ParallelWriter CulledObjectCount;
		public NativeAtomicCounter.ParallelWriter FrustumOutCount;
		public NativeAtomicCounter.ParallelWriter FrustumInCount;
		public NativeAtomicCounter.ParallelWriter FrustumPartialCount;

		public ComponentTypeHandle<ChunkCullResult> ChunkCullResultHandle;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var chunkWorldRenderBounds = chunk.GetChunkComponentData(ref ChunkWorldRenderBoundsHandle);
			var chunkAabb = chunkWorldRenderBounds.AABB;
			var chunkIntersection = FrustumPlanes.Intersect2(PlanePackets, chunkAabb);
			// var frustumMarker = new ProfilerMarker("Frustum");
			// var setBitsMarker = new ProfilerMarker("SetBits");
			// var partialCullMarker = new ProfilerMarker("PartialCull");

			switch (chunkIntersection)
			{
				case FrustumPlanes.IntersectResult.Out:
				{
					// No Entity is visible, don't need to check Entity AABB's.
					var cullResult = new ChunkCullResult { Lower = new BitField64(0), Upper = new BitField64(0) };
					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					CulledObjectCount.Increment(chunk.Count);
					FrustumOutCount.Increment();
					break;
				}
				case FrustumPlanes.IntersectResult.In:
				{
					// All Entities are visible, no need to check Entity AABB's.
					var cullResult = new ChunkCullResult
						{ Lower = new BitField64(ulong.MaxValue), Upper = new BitField64(ulong.MaxValue) };
					chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);

					FrustumInCount.Increment();
					break;
				}
				case FrustumPlanes.IntersectResult.Partial:
				{
					FrustumPartialCount.Increment();

					// Check Each Entity individually
					// partialCullMarker.Begin();
					{
						var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
						var renderMeshIndexArray = chunk.GetNativeArray(ref RenderMeshIndexHandle);
						var cullResult = new ChunkCullResult();
						var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

						while (enumerator.NextEntityIndex(out var entityIndex))
						{
							var aabb = worldRenderBoundsArray[entityIndex].AABB;
							var renderMeshIndex = renderMeshIndexArray[entityIndex].Value;

							// frustumMarker.Begin();
							var intersectResult = FrustumPlanes.Intersect2(PlanePackets, aabb);
							// frustumMarker.End();

							var isVisible = intersectResult != FrustumPlanes.IntersectResult.Out;
							if (!isVisible)
							{
								CulledObjectCount.Increment();
							}

							// setBitsMarker.Begin();

							// TODO-Renderer: Remove Lower/Upper branch here. Could inline ChunkEntityEnumerator here
							var lower = entityIndex < 64;

							if (lower)
								cullResult.Lower.SetBits(entityIndex, isVisible);
							else
								cullResult.Upper.SetBits(64 - entityIndex, isVisible);

							// setBitsMarker.End();
						}

						chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
					}
					//partialCullMarker.End();

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}