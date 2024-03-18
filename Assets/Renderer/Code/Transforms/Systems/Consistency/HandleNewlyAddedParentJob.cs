using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	// TODO: Performance Test
	// TODO: LocalTransform has to be fixed
	[BurstCompile]
	public unsafe struct HandleNewlyAddedParentJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Parent> ParentTypeHandle;

		[ReadOnly]
		public BufferLookup<Child> ChildLookup;

		[ReadOnly]
		public EntityTypeHandle EntityTypeHandle;

		public EntityCommandBuffer.ParallelWriter ParallelCommandBuffer;

		// The Entities that we've just added the Child components through the Parallel ECB
		public NativeParallelHashSet<Entity>.ParallelWriter ChildJustAddedEntities;

		public uint LastSystemVersion;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);

			// Only if Parent component is changed
			if (!chunk.DidChange(ref ParentTypeHandle, LastSystemVersion))
				return;

			var parentArray = chunk.GetNativeArray(ref ParentTypeHandle);
			var entityCount = chunk.Count;
			var entityArray = chunk.GetEntityDataPtrRO(EntityTypeHandle);

			for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
			{
				var parentEntity = parentArray[entityIndex].Value;
				if (parentEntity == Entity.Null)
					continue;

				var childEntity = entityArray[entityIndex];

				// All add buffer operations must come before append operations to ensure we can append
				const int addBufferSortIndex = 0;
				const int appendBufferSortIndex = 1;

				// If the Parent doesn't have a Child Buffer yet, add the Child Buffer
				if (!ChildLookup.HasBuffer(parentEntity))
				{
					if (ChildJustAddedEntities.Add(parentEntity))
					{
						ParallelCommandBuffer.AddBuffer<Child>(addBufferSortIndex, parentEntity);
					}

					ParallelCommandBuffer.AppendToBuffer(appendBufferSortIndex, parentEntity,
						new Child { Value = childEntity });
				}
				else
				{
					var children = ChildLookup[parentEntity];
					if (Contains(ref children, childEntity))
					{
						ParallelCommandBuffer.AppendToBuffer(appendBufferSortIndex, parentEntity,
							new Child { Value = childEntity });
					}
				}
			}
		}

		private bool Contains(ref DynamicBuffer<Child> childBuffer, Entity entity)
		{
			var count = childBuffer.Length;

			for (int i = 0; i < count; i++)
			{
				if (childBuffer[i].Value == entity)
					return true;
			}

			return false;
		}
	}
}