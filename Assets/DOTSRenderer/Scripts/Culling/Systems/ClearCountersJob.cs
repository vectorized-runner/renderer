using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Renderer
{
	[BurstCompile]
	public struct ClearCountersJob : IJobParallelFor
	{
		public NativeArray<UnsafeAtomicCounter> CountByRenderMeshIndex;

		public void Execute(int index)
		{
			ref var counter = ref CountByRenderMeshIndex.ElementAsRef(index);
			counter.Count = 0;
		}
	}
}