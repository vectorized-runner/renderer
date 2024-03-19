using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	// TODO: Remove all PreviousParent
	// How to detect if Parent is removed?
	// Easy: Parent component no longer exists on the Entity, PreviousParent component does exist
	// Question: Could we do it without needing a 'PreviousParent' component? I don't think so, at least for now.
	// Needs to be removed from the Parent's Child List
	[BurstCompile]
	public struct HandleRemovedParentsJob : IJobChunk
	{
		public NativeParallelHashMap<Entity, Entity>.ParallelWriter ChildrenToRemove;

		public EntityCommandBuffer.ParallelWriter ParallelCommandBuffer;
		
		[ReadOnly]
		public ComponentTypeHandle<PreviousParent> PreviousParentHandle;
		
		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);

			var entityCount = chunk.Count;
			var previousParentArray = chunk.GetNativeArray(ref PreviousParentHandle);
			
			for (int i = 0; i < entityCount; i++)
			{
				var previousParent = previousParentArray[i].Value;
				if (previousParent != Entity.Null)
				{
				}
			}
			
			throw new System.NotImplementedException();
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
		private EntityQuery _parentQuery;

		protected override void OnCreate()
		{
			_removedParentsQuery = GetEntityQuery(
				ComponentType.Exclude<Parent>(),
				ComponentType.ReadOnly<PreviousParent>());

			// LocalToWorld shouldn't be removed in any case, so I'm using it to detect destroyed entities.
			_destroyedParentsQuery = GetEntityQuery(
				ComponentType.ReadOnly<Child>(),
				ComponentType.Exclude<LocalToWorld>());

			_parentQuery = GetEntityQuery(ComponentType.ReadOnly<Parent>());
		}

		// TODO: Allow some time for Jobs to complete (?)
		protected override void OnUpdate()
		{
			var addedParentCmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

			var addedParentJobHandle = new HandleNewlyAddedParentJob
			{
				ChildJustAddedEntities = new NativeParallelHashSet<Entity>(32, Allocator.TempJob).AsParallelWriter(),
				ChildLookup = GetBufferLookup<Child>(true),
				EntityTypeHandle = GetEntityTypeHandle(),
				ParallelCommandBuffer = addedParentCmdBuffer.AsParallelWriter(),
				ParentTypeHandle = GetComponentTypeHandle<Parent>(true),
				LastSystemVersion = LastSystemVersion,
			}.ScheduleParallel(_parentQuery, Dependency);

			addedParentJobHandle.Complete();
			addedParentCmdBuffer.Playback(EntityManager);
			addedParentCmdBuffer.Dispose();

			var destroyedParentCmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

			var destroyedParentsJob = new HandleDestroyedParentsJob
			{
				ChildLookup = GetBufferLookup<Child>(true),
				ChildTypeHandle = GetBufferTypeHandle<Child>(true),
				CommandBuffer = destroyedParentCmdBuffer.AsParallelWriter(),
				LocalToWorldLookup = GetComponentLookup<LocalToWorld>(true),
				EntityTypeHandle = GetEntityTypeHandle(),
			}.ScheduleParallel(_destroyedParentsQuery, Dependency);

			// Let's try the naive version: Complete immediately
			destroyedParentsJob.Complete();
			destroyedParentCmdBuffer.Playback(EntityManager);
			destroyedParentCmdBuffer.Dispose();

			// var parentByRemovedChildren = new NativeParallelMultiHashMap<Entity, Entity>(64, Allocator.TempJob);
			//
			// // If Parent is removed from a Child Entity, that Child Entity should get removed from the Parent's Child List
			// var dep1 = new HandleChildrenWithRemovedParentJob
			// {
			// 	EntityHandle = GetEntityTypeHandle(),
			// 	PreviousParentHandle = GetComponentTypeHandle<PreviousParent>(true),
			// 	ParentByRemovedChildren = parentByRemovedChildren.AsParallelWriter(),
			// 	LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(),
			// 	LocalToWorldLookup = GetComponentLookup<LocalToWorld>(true),
			// 	LocalTransformHandle = GetComponentTypeHandle<LocalTransform>(),
			// }.ScheduleParallel(_removedParentsQuery, Dependency);
			//
			// var dep2 = new UpdateBufferOfChildrenRemovedParentsJob
			// {
			// 	ChildLookup = GetBufferLookup<Child>(),
			// 	ParentRemovedChildrenMap = parentByRemovedChildren
			// }.Schedule(dep1);
			//
			// Dependency = parentByRemovedChildren.Dispose(dep2);
			//
			// EntityManager.RemoveComponent<PreviousParent>(_removedParentsQuery);
		}
	}
}