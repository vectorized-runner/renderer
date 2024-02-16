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
				// The Chunk doesn't have any Entities, updating Render Bounds shouldn't matter.
				return;
			}

			var firstAABB = worldRenderBoundsArray[entityIndex].AABB;
			var resultAABB = firstAABB;

			while (enumerator.NextEntityIndex(out entityIndex))
			{
				var nextAABB = worldRenderBoundsArray[entityIndex].AABB;
				resultAABB = RenderMath.EncapsuleAABBs(resultAABB, nextAABB);
			}

			chunk.SetChunkComponentData(ref ChunkWorldRenderBoundsHandle,
				new ChunkWorldRenderBounds { AABB = resultAABB });
		}
	}
}