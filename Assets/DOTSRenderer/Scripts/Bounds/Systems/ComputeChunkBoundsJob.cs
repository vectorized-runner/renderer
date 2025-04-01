using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	[BurstCompile]
	public struct ComputeChunkBoundsJob : IJobChunk
	{
		public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;
		
		[ReadOnly]
		public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;

		public uint LastSystemVersion;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);
			
			// TODO-Renderer: Chunk Change Filter doesn't work right now (this code doesn't trigger) when using ChunkGrouping
			// if (!chunk.DidChange(ref WorldRenderBoundsHandle, LastSystemVersion))
			// 	return;
			
			var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
			var entityCount = chunk.Count;
			if (entityCount == 0)
				return;
			
			var firstAABB = worldRenderBoundsArray[0].AABB;
			var min = firstAABB.Min;
			var max = firstAABB.Max;
			
			for (int i = 1; i < entityCount; i++)
			{
				var aabb = worldRenderBoundsArray[i].AABB;
				min = math.min(min, aabb.Min);
				max = math.max(max, aabb.Max);
			}

			var chunkBounds = new ChunkWorldRenderBounds
			{
				AABB = new AABB
				{
					Center = (max + min) * 0.5f,
					Extents = (max - min) * 0.5f
				}
			};
			
			chunk.SetChunkComponentData(ref ChunkWorldRenderBoundsHandle, chunkBounds);
		}
	}
}