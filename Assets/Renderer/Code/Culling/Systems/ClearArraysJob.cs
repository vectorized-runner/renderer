using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
	[BurstCompile]
	public struct ClearArraysJob : IJob
	{
		public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;
		public NativeArray<UnsafeAtomicCounter> CountByRenderMeshIndex;

		public void Execute()
		{
			for (int i = 0; i < RenderSettings.MaxSupportedUniqueMeshCount; i++)
			{
				ref var list = ref MatricesByRenderMeshIndex.ElementAsRef(i);
				list.Clear();

				ref var counter = ref CountByRenderMeshIndex.ElementAsRef(i);
				counter.Count = 0;
			}
		}
	}
}