using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Renderer
{
	// Question: How to detect destroyed Parents?
	// All Parents have a [Child] buffer

	[BurstCompile]
	public struct HandleDestroyedParentsJob : IJobChunk
	{
		public EntityCommandBuffer.ParallelWriter CommandBuffer;

		[ReadOnly]
		public BufferTypeHandle<Child> ChildTypeHandle;

		[ReadOnly]
		public BufferLookup<Child> ChildLookup;

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
				var childBuffer = childAccessor[entityIndex];
				DestroyChildrenRecursive(ref childBuffer, unfilteredChunkIndex);
				
				Debug.Log("Job running for one entity at least.");
			}
		}
	}

	// AddParent
	// ChangeParent
	// RemoveParent method -> Works on both self and the parent
	// The real question: Why are we supporting parenting like this? It's weird, a lot of handling moved to the developers right now.

	// If Parent is changed to Another entity, then needs to be added to that Parent's child list

	[BurstCompile]
	public unsafe struct HandleChangedParentsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Parent> ParentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PreviousParent> PreviousParentHandle;

		[ReadOnly]
		public EntityTypeHandle EntityTypeHandle;

		public NativeParallelHashMap<Entity, Entity> ParentByRemovedChildren;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);

			var entityCount = chunk.Count;
			var parentArray = chunk.GetNativeArray(ref ParentTypeHandle);
			var previousParentArray = chunk.GetNativeArray(ref PreviousParentHandle);
			var entityArray = chunk.GetEntityDataPtrRO(EntityTypeHandle);

			for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
			{
				var parentEntity = parentArray[entityIndex].Value;
				var previousParentEntity = previousParentArray[entityIndex].Value;
				var childEntity = entityArray[entityIndex];

				// Check if Parent got changed
				if (parentEntity != previousParentEntity)
				{
					if (parentEntity == Entity.Null)
					{
						// Should be removed from Parent's child buffer
						ParentByRemovedChildren.Add(previousParentEntity, childEntity);
					}
					else if (previousParentEntity == Entity.Null)
					{
						// Should be added to Parent's child buffer
						// TODO: Implement
					}
					else
					{
						// Should be removed from Parent's child buffer, should be added to PreviousParent's child buffer
						// TODO: Implement
					}
				}
			}

			throw new System.NotImplementedException();
		}
	}

	public partial class ParentUpdateSystem : SystemBase
	{
		private EntityQuery _removedParentsQuery;
		private EntityQuery _destroyedParentsQuery;
		private EntityQuery _toFullyDestroyQuery;

		protected override void OnCreate()
		{
			_removedParentsQuery = GetEntityQuery(
				ComponentType.Exclude<Parent>(),
				ComponentType.ReadOnly<PreviousParent>());

			// LocalToWorld shouldn't be removed in any case, so I'm using it to detect destroyed entities.
			_destroyedParentsQuery = GetEntityQuery(
				ComponentType.ReadOnly<Child>(),
				ComponentType.Exclude<LocalToWorld>());
		}

		protected override void OnUpdate()
		{
			Debug.Log("ParentUpdateSystem Running =.");
			
			var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

			var destroyedParentsJob = new HandleDestroyedParentsJob
			{
				ChildLookup = GetBufferLookup<Child>(true),
				ChildTypeHandle = GetBufferTypeHandle<Child>(true),
				CommandBuffer = commandBuffer.AsParallelWriter()
			}.ScheduleParallel(_destroyedParentsQuery, Dependency);

			// Let's try the naive version: Complete immediately
			destroyedParentsJob.Complete();
			commandBuffer.Playback(EntityManager);
			commandBuffer.Dispose();

			var parentByRemovedChildren = new NativeParallelMultiHashMap<Entity, Entity>(64, Allocator.TempJob);

			// If Parent is removed from a Child Entity, that Child Entity should get removed from the Parent's Child List
			var dep1 = new HandleChildrenWithRemovedParentJob
			{
				EntityHandle = GetEntityTypeHandle(),
				PreviousParentHandle = GetComponentTypeHandle<PreviousParent>(true),
				ParentByRemovedChildren = parentByRemovedChildren.AsParallelWriter(),
				LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(),
				LocalToWorldLookup = GetComponentLookup<LocalToWorld>(true),
				LocalTransformHandle = GetComponentTypeHandle<LocalTransform>(),
			}.ScheduleParallel(_removedParentsQuery, Dependency);

			var dep2 = new UpdateBufferOfChildrenRemovedParentsJob
			{
				ChildLookup = GetBufferLookup<Child>(),
				ParentRemovedChildrenMap = parentByRemovedChildren
			}.Schedule(dep1);

			Dependency = parentByRemovedChildren.Dispose(dep2);

			EntityManager.RemoveComponent<PreviousParent>(_removedParentsQuery);
		}
	}
}