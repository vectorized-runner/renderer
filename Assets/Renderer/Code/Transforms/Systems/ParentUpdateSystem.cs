using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Renderer
{
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
			
			var dep2 = new RemoveChildrenJob
			{
				ChildLookup = GetBufferLookup<Child>(),
				ParentRemovedChildrenMap = parentByRemovedChildren
			}.Schedule(dep1);
			
			Dependency = parentByRemovedChildren.Dispose(dep2);

			EntityManager.RemoveComponent<PreviousParent>(_removedParentsQuery);
		}
	}
}