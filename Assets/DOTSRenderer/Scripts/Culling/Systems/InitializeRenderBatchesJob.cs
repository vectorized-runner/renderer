using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
	[BurstCompile]
	public struct InitializeRenderBatchesJob : IJobParallelFor
	{
		public NativeArray<UnsafeList<float4x4>> RenderMatricesByRenderMeshIndex;

		[ReadOnly]
		public NativeArray<UnsafeAtomicCounter> RenderCountByRenderMeshIndex;
		
		public void Execute(int index)
		{
			ref readonly var counter = ref RenderCountByRenderMeshIndex.ElementAsReadonlyRef(index);
			var count = counter.Count;
			ref var matrices = ref RenderMatricesByRenderMeshIndex.ElementAsRef(index);

			if (matrices.Capacity < count)
			{
				matrices.Resize(count);
			}

			matrices.Clear();
		}
	}
}