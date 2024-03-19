using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	[BurstCompile]
	public unsafe struct HandleDestroyedParentsJob : IJobChunk
	{
		public EntityCommandBuffer.ParallelWriter CommandBuffer;

		[ReadOnly]
		public BufferTypeHandle<Child> ChildTypeHandle;

		[ReadOnly]
		public BufferLookup<Child> ChildLookup;

		[ReadOnly]
		public ComponentLookup<LocalToWorld> LocalToWorldLookup;

		[ReadOnly]
		public EntityTypeHandle EntityTypeHandle;

		private void DestroyChildrenRecursive(ref DynamicBuffer<Child> childBuffer, int unfilteredChunkIndex)
		{
			var childCount = childBuffer.Length;

			for (int childIndex = 0; childIndex < childCount; childIndex++)
			{
				// I need to Destroy this children
				// I can't use EntityManager.Destroy here (can't do structural changes)
				// If I use EntityCommandBuffer.Destroy, and if it has Child buffer, it will persist another frame,
				// so I need to remove the Child component immediately too.
				var childEntity = childBuffer[childIndex].Value;
				var isChildExists = LocalToWorldLookup.HasComponent(childEntity);

				if (!isChildExists)
					continue;
				
				CommandBuffer.DestroyEntity(unfilteredChunkIndex, childEntity);

				// Remove Child Component to instantly destroy
				if (ChildLookup.TryGetBuffer(childEntity, out var children))
				{
					DestroyChildrenRecursive(ref children, unfilteredChunkIndex);
					CommandBuffer.RemoveComponent<Child>(unfilteredChunkIndex, childEntity);
				}
			}
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);

			var childAccessor = chunk.GetBufferAccessor(ref ChildTypeHandle);
			var entityCount = chunk.Count;

			for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
			{
				var parentEntity = chunk.GetEntityDataPtrRO(EntityTypeHandle)[entityIndex];
				var childBuffer = childAccessor[entityIndex];
				DestroyChildrenRecursive(ref childBuffer, unfilteredChunkIndex);
				CommandBuffer.RemoveComponent<Child>(unfilteredChunkIndex, parentEntity);
			}
		}
	}
}