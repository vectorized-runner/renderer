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
	public unsafe struct CollectRenderBatchesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ChunkCullResult> CullResultHandle;

		[ReadOnly]
		public SharedComponentTypeHandle<RenderMesh> RenderMeshIndexHandle;

		[ReadOnly]
		public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

		public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var cullResult = chunk.GetChunkComponentData(ref CullResultHandle);
			var renderCount = cullResult.EntityVisibilityMask.CountBits();
			
			if (renderCount == 0)
			{
				return;
			}

			var renderMeshIndex = chunk.GetSharedComponentIndex(RenderMeshIndexHandle);
			ref var matrices = ref MatricesByRenderMeshIndex.ElementAsRef(renderMeshIndex);
			// Notice optimization: Increment once
			var newLength = Interlocked.Add(ref matrices.m_length, renderCount);
			var writePtr = matrices.Ptr + newLength - renderCount;
			var localToWorldArray = chunk.GetNativeArray(ref LocalToWorldHandle);
			// Use cull results for the mask
			var enumerator = new ChunkEntityEnumerator(true, cullResult.EntityVisibilityMask.v128, chunk.Count);

			while (enumerator.NextEntityIndex(out var index))
			{
				var matrix = localToWorldArray[index].Value;
				*writePtr = matrix;
				writePtr++;
			}
		}
	}
}