using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[BurstCompile]
	public struct ComputeChunkBoundsJob : IJobChunk
	{
		public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
			var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);

			float3 min = float.MaxValue;
			float3 max = float.MinValue;

			while (enumerator.NextEntityIndex(out var entityIndex))
			{
				var aabb = worldRenderBoundsArray[entityIndex].AABB;
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