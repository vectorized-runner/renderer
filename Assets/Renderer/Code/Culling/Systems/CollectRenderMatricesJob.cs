using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
	[BurstCompile]
	public struct CollectRenderMatricesJob : IJob
	{
		[DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
		[ReadOnly] public ComponentTypeHandle<ChunkCullResult> CullResultHandle;
		[ReadOnly] public ComponentTypeHandle<RenderMeshIndex> RenderMeshIndexHandle;
		[ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

		public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;

		public void Execute()
		{
			var chunkCount = Chunks.Length;

			for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
			{
				var chunk = Chunks[chunkIndex];
				var cullResult = chunk.GetChunkComponentData(ref CullResultHandle);
				var renderMeshArray = chunk.GetNativeArray(ref RenderMeshIndexHandle);
				var localToWorldArray = chunk.GetNativeArray(ref LocalToWorldHandle);
				var entityCount = chunk.Count;
				var entityIndex = 0;
				var count = math.min(64, entityCount);

				while (entityIndex < count)
				{
					if (cullResult.Lower.IsSet(entityIndex))
					{
						var renderMeshIndex = renderMeshArray[entityIndex].Value;
						var matrix = localToWorldArray[entityIndex];
						ref var matrices = ref MatricesByRenderMeshIndex.ElementAsRef(renderMeshIndex);
						matrices.Add(matrix.Value);
					}

					entityIndex++;
				}

				while (entityIndex < entityCount)
				{
					if (cullResult.Upper.IsSet(entityIndex))
					{
						var renderMeshIndex = renderMeshArray[entityIndex].Value;
						var matrix = localToWorldArray[entityIndex];
						ref var matrices = ref MatricesByRenderMeshIndex.ElementAsRef(renderMeshIndex);
						matrices.Add(matrix.Value);
					}

					entityIndex++;
				}
			}
		}
	}
}