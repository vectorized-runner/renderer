using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(TransformsGroup))]
	public partial class ComputeWorldMatrixSystem : SystemBase
	{
		private EntityQuery _rootsWithChildrenQuery;

		protected override void OnCreate()
		{
			_rootsWithChildrenQuery = GetEntityQuery(
				ComponentType.ReadOnly<Child>(),
				ComponentType.ReadOnly<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.Exclude<Parent>());
		}

		protected override void OnUpdate()
		{
			Entities
				.WithNone<Static, Parent>()
				.WithChangeFilter<LocalTransform>()
				.WithName("ComputeRootLocalToWorldMatrixJob")
				.ForEach(
					(ref LocalToWorld worldMatrix, in LocalTransform localTransform) =>
					{
						worldMatrix.Value = float4x4.TRS(localTransform.Position, localTransform.Rotation,
							localTransform.Scale);
					})
				.ScheduleParallel();

			Dependency = new ComputeChildLocalToWorldJob
			{
				ChildBufferHandle = GetBufferTypeHandle<Child>(true),
				ChildLookup = GetBufferLookup<Child>(true),
				LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(true),
				LocalToWorldLookup = GetComponentLookup<LocalToWorld>(),
				LocalTransformLookup = GetComponentLookup<LocalTransform>(true)
			}.ScheduleParallel(_rootsWithChildrenQuery, Dependency);
		}
	}
}