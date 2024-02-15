using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Renderer
{
	[BurstCompile]
	public struct ClearCountersJob : IJob
	{
		public NativeArray<UnsafeAtomicCounter> CountByRenderMeshIndex;

		public void Execute()
		{
			for (int i = 0; i < RenderSettings.MaxSupportedUniqueMeshCount; i++)
			{
				ref var counter = ref CountByRenderMeshIndex.ElementAsRef(i);
				counter.Count = 0;
			}
		}
	}
}