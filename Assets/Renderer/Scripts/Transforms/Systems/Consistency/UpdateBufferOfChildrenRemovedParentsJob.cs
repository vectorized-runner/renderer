using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Renderer
{
	/// <summary>
	/// Why this job can't be done in parallel: For each Parent, multiple child might be removed in one frame,
	/// we can't remove the Children from the buffer in Parallel (at least I can't come up with something quickly now.)
	/// </summary>
	[BurstCompile]
	public struct UpdateBufferOfChildrenRemovedParentsJob : IJob
	{
		[ReadOnly]
		public NativeParallelMultiHashMap<Entity, Entity> ParentRemovedChildrenMap;

		public BufferLookup<Child> ChildLookup;

		public void Execute()
		{
			var parents = ParentRemovedChildrenMap.GetKeyArray(Allocator.Temp);
			var parentCount = parents.Length;

			for (int parentIndex = 0; parentIndex < parentCount; parentIndex++)
			{
				var parentEntity = parents[parentIndex];

				if (ParentRemovedChildrenMap.TryGetFirstValue(parentEntity, out var removedChild, out var iterator))
				{
					var removedChildren = new UnsafeList<Entity>(0, Allocator.Temp);
					removedChildren.Add(removedChild);

					while (ParentRemovedChildrenMap.TryGetNextValue(out removedChild, ref iterator))
					{
						removedChildren.Add(removedChild);
					}

					var childBuffer = ChildLookup[parentEntity];

					for (int childIndex = 0; childIndex < childBuffer.Length; childIndex++)
					{
						var child = childBuffer[childIndex].Value;
						if (removedChildren.Contains(child))
						{
							childBuffer.RemoveAtSwapBack(childIndex);
							childIndex--;
						}
					}
				}
			}
		}
	}
}