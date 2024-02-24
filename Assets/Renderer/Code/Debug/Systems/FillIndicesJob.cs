using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Renderer
{
	[BurstCompile]
	public struct FillIndicesJob : IJobParallelFor
	{
		public NativeArray<int> IndexArray;
		
		public void Execute(int index)
		{
			IndexArray[index] = index;
		}
	}
}