using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Renderer
{
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
		
		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
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

		protected override void OnCreate()
		{
			_removedParentsQuery = GetEntityQuery(
				ComponentType.Exclude<Parent>(),
				ComponentType.ReadOnly<PreviousParent>());
		}

		protected override void OnUpdate()
		{
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