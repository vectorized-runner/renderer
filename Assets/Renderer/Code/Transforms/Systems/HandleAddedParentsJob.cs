using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	[BurstCompile]
	public unsafe struct HandleAddedParentsJob : IJobChunk
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
					// TODO: If Child Buffer already exists, and this Entity isn't it, append to it.
					// var children = ChildLookup[parentEntity];
				}
			}
		}
	}
}