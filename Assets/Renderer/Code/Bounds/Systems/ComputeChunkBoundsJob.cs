using System.Runtime.CompilerServices;
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

			if (!enumerator.NextEntityIndex(out var entityIndex))
			{
				// The Chunk doesn't have any Entities, it shouldn't pass culling test towards any camera
				var aabb = new AABB { Center = float.MaxValue, Extents = float3.zero };
				chunk.SetChunkComponentData(ref ChunkWorldRenderBoundsHandle,
					new ChunkWorldRenderBounds { AABB = aabb });

				return;
			}

			var firstAABB = worldRenderBoundsArray[entityIndex].AABB;
			var resultAABB = firstAABB;

			while (enumerator.NextEntityIndex(out entityIndex))
			{
				var nextAABB = worldRenderBoundsArray[entityIndex].AABB;
				resultAABB = Encapsulate(firstAABB, nextAABB);
			}

			chunk.SetChunkComponentData(ref ChunkWorldRenderBoundsHandle,
				new ChunkWorldRenderBounds { AABB = resultAABB });
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static AABB Encapsulate(AABB first, AABB second)
		{
			var newMin = math.min(first.Min, second.Min);
			var newMax = math.max(first.Max, second.Max);
			
			return new AABB
			{
				Center = (newMin + newMax) * 0.5f,
				Extents = (newMax - newMin) * 0.5f
			};
		}
	}
}