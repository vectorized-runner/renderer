using System;
using System.Threading;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using FrustumPlanes = Renderer.UnityPackages.FrustumPlanes;

namespace Renderer
{
	[BurstCompile]
	public unsafe struct ChunkCullingJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;

		[ReadOnly]
		public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;

		[ReadOnly]
		public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

		[ReadOnly]
		public SharedComponentTypeHandle<RenderMeshIndex> RenderMeshIndexHandle;

		[ReadOnly]
		public NativeArray<FrustumPlanes.PlanePacket4> PlanePackets;

		public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;

		// TODO-Renderer: Make these counters debug mode only behind define
		public NativeAtomicCounter.ParallelWriter CulledObjectCount;
		public NativeAtomicCounter.ParallelWriter FrustumOutCount;
		public NativeAtomicCounter.ParallelWriter FrustumInCount;
		public NativeAtomicCounter.ParallelWriter FrustumPartialCount;

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
					CulledObjectCount.Increment(chunk.Count);
					FrustumOutCount.Increment();
					break;
				}
				case FrustumPlanes.IntersectResult.In:
				{
					// All Entities are visible, no need to check Entity AABB's.
					FrustumInCount.Increment();

					// TODO: Early exit, check if all Entities enabled quick
					// TODO: Still gotta check if Enabled/Disabled or not
					// TODO: Implement
					break;
				}
				case FrustumPlanes.IntersectResult.Partial:
				{
					FrustumPartialCount.Increment();

					// Check Each Entity individually
					{
						var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
						var entityCount = chunk.Count;
						var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, entityCount);
						var renderEntityIndices = new UnsafeList<int>(entityCount, Allocator.Temp);

						while (enumerator.NextEntityIndex(out var entityIndex))
						{
							var aabb = worldRenderBoundsArray[entityIndex].AABB;
							var intersectResult = FrustumPlanes.Intersect2(PlanePackets, aabb);
							var isVisible = intersectResult != FrustumPlanes.IntersectResult.Out;

							if (isVisible)
							{
								renderEntityIndices.AddNoResize(entityIndex);
							}
							else
							{
								CulledObjectCount.Increment();
							}
						}

						var renderEntityCount = renderEntityIndices.Length;

						if (renderEntityCount > 0)
						{
							var renderMeshIndex = chunk.GetSharedComponent(RenderMeshIndexHandle).Value;
							var localToWorldArray = chunk.GetNativeArray(ref LocalToWorldHandle);
							ref var matrices = ref MatricesByRenderMeshIndex.ElementAsRef(renderMeshIndex);

							// Notice the optimization: Instead of Matrices.AddRangeNoResize,
							// We increment the count once and copy on the way (don't use temporary List<float4x4>)
							// AddRangeNoResize was already better than AddNoResize (uses Interlocked.Add once)
							var newCount = Interlocked.Add(ref matrices.m_length, renderEntityCount);
							var addPtr = matrices.Ptr + newCount - entityCount;

							for (int i = renderEntityIndices.Length - 1; i >= 0; i--)
							{
								var entityIndex = renderEntityIndices[i];
								var matrix = localToWorldArray[entityIndex].Value;
								*addPtr = matrix;
								addPtr++;
							}
						}
					}
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}