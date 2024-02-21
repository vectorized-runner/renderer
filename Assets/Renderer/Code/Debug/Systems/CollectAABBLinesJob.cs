using System;
using System.Threading;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[BurstCompile]
	public unsafe struct CollectAABBLinesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ChunkCullResult> CullResultHandle;

		[ReadOnly]
		public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldBoundsHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorldRenderBounds> WorldBoundsHandle;

		public NativeArray<float3> InEntityLinePoints;

		[NativeDisableUnsafePtrRestriction]
		[NoAlias]
		public int* InEntityPointsCounter;

		public NativeArray<float3> OutEntityLinePoints;

		[NativeDisableUnsafePtrRestriction]
		[NoAlias]
		public int* OutEntityPointsCounter;

		// public NativeList<float3>.ParallelWriter OutEntityLinesPoints;
		// public NativeList<int>.ParallelWriter OutEntityLineIndices;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var chunkCullResult = chunk.GetChunkComponentData(ref CullResultHandle);
			var entityCount = chunk.Count;
			var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldBoundsHandle);
			var visibleEntityCount = chunkCullResult.EntityVisibilityMask.CountBits();
			var culledEntityCount = entityCount - visibleEntityCount;
			const int pointsPerAABB = AABBDebugDrawSystem.PointsPerAABB;
			var visiblePointCount = visibleEntityCount * pointsPerAABB;
			var visibleNewCount = Interlocked.Add(ref *InEntityPointsCounter, visiblePointCount);
			var visibleWriteIndex = visibleNewCount - visiblePointCount;
			var culledPointCount = culledEntityCount * pointsPerAABB;
			var culledNewCount = Interlocked.Add(ref *OutEntityPointsCounter, culledPointCount);
			var culledWriteIndex = culledNewCount - culledPointCount;

			for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
			{
				var aabb = worldRenderBoundsArray[entityIndex].AABB;

				if (chunkCullResult.EntityVisibilityMask.IsSet(entityIndex))
				{
					var span = InEntityLinePoints.AsSpan(visibleWriteIndex, pointsPerAABB);
					AppendAABBLines(span, aabb);
					visibleWriteIndex += pointsPerAABB;
				}
				else
				{
					var span = OutEntityLinePoints.AsSpan(culledWriteIndex, pointsPerAABB);
					AppendAABBLines(span, aabb);
					culledWriteIndex += pointsPerAABB;
				}
			}
		}

		// Constructing the cube from lines requires 12 lines - 24 points to be added
		private static void AppendAABBLines(Span<float3> result, AABB aabb)
		{
			var center = aabb.Center;
			var extents = aabb.Extents;
			var ex = extents.x;
			var ey = extents.y;
			var ez = extents.z;
			var p0 = center + new float3(-ex, -ey, -ez);
			var p1 = center + new float3(ex, -ey, -ez);
			var p2 = center + new float3(ex, ey, -ez);
			var p3 = center + new float3(-ex, ey, -ez);
			var p4 = center + new float3(-ex, ey, ez);
			var p5 = center + new float3(ex, ey, ez);
			var p6 = center + new float3(ex, -ey, ez);
			var p7 = center + new float3(-ex, -ey, ez);

			result[0] = p0;
			result[1] = p1;

			result[2] = p0;
			result[3] = p3;

			result[4] = p2;
			result[5] = p3;

			result[6] = p1;
			result[7] = p2;

			result[8] = p4;
			result[9] = p5;

			result[10] = p4;
			result[11] = p7;

			result[12] = p5;
			result[13] = p6;

			result[14] = p6;
			result[15] = p7;

			result[16] = p3;
			result[17] = p4;

			result[18] = p0;
			result[19] = p7;

			result[20] = p2;
			result[21] = p5;

			result[22] = p1;
			result[23] = p6;
		}
	}
}