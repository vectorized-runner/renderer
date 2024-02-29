using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	[BurstCompile]
	public unsafe struct CollectRemovedChildrenJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PreviousParent> PreviousParentHandle;

		[ReadOnly]
		public EntityTypeHandle EntityHandle;

		public NativeParallelMultiHashMap<Entity, Entity>.ParallelWriter ParentByRemovedChildren;

		// How can this mess up:
		// 2 child, both have parent removed, trying to remove themselves from the parent -- in parallel.
		// Other way to do it: Iterate through all Entities with Child (would be pretty expensive.)
		// A possible way to do it: Build up a multi-hashmap <Entity, Entity>, first collect, then remove

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);

			var entityCount = chunk.Count;
			var previousParentArray = chunk.GetNativeArray(ref PreviousParentHandle);
			var chunkEntityArrayPtr = chunk.GetEntityDataPtrRO(EntityHandle);

			for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
			{
				var thisEntity = chunkEntityArrayPtr[entityIndex];
				var parentEntity = previousParentArray[entityIndex].Value;

				ParentByRemovedChildren.Add(thisEntity, parentEntity);
			}
		}
	}
}