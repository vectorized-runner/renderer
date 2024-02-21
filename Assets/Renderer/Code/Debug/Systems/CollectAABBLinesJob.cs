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
	public struct CollectAABBLinesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ChunkCullResult> CullResultHandle;

		[ReadOnly]
		public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldBoundsHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorldRenderBounds> WorldBoundsHandle;

		public NativeList<float3>.ParallelWriter InEntityLinePoints;
		
		// public NativeList<float3>.ParallelWriter OutEntityLinesPoints;
		// public NativeList<int>.ParallelWriter OutEntityLineIndices;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			using var marker = new AutoProfilerMarker("DebugCollectAABBLines");
			var chunkCullResult = chunk.GetChunkComponentData(ref CullResultHandle);
			var visibleEntityCount = chunkCullResult.EntityVisibilityMask.CountBits();
			if (visibleEntityCount == 0)
				return;

			var entityCount = chunk.Count;
			var enumerator = new ChunkEntityEnumerator(true, chunkCullResult.EntityVisibilityMask.v128, entityCount);
			var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldBoundsHandle);

			// Don't need the temp list actually - but this is a debug-only code so it's fine
			var arraySize = visibleEntityCount * 2;
			var linePoints = new NativeList<float3>(arraySize, Allocator.Temp);

			while (enumerator.NextEntityIndex(out var entityIndex))
			{
				var aabb = worldRenderBoundsArray[entityIndex].AABB;
				AppendLinePoints(linePoints, aabb);
			}

			InEntityLinePoints.AddRangeNoResize(linePoints);
		}

		// Constructing the cube from lines requires 12 lines - 24 points to be added
		private void AppendLinePoints(NativeList<float3> points, AABB aabb)
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

			points.Add(p0);
			points.Add(p1);

			points.Add(p0);
			points.Add(p3);

			points.Add(p2);
			points.Add(p3);

			points.Add(p1);
			points.Add(p2);

			points.Add(p4);
			points.Add(p5);

			points.Add(p4);
			points.Add(p7);

			points.Add(p5);
			points.Add(p6);

			points.Add(p6);
			points.Add(p7);

			points.Add(p3);
			points.Add(p4);

			points.Add(p0);
			points.Add(p7);

			points.Add(p2);
			points.Add(p5);

			points.Add(p1);
			points.Add(p6);
		}
	}
}