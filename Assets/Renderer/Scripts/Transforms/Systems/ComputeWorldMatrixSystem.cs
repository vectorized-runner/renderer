using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(TransformsGroup))]
	public partial class ComputeWorldMatrixSystem : SystemBase
	{
		private EntityQuery _dynamicRootObjectQuery;

		protected override void OnCreate()
		{
			// If the Root object is Static, the children also shouldn't be able to move.
			// At least this is the current design decision, for now.
			_dynamicRootObjectQuery = GetEntityQuery(
				ComponentType.ReadOnly<LocalTransform>(),
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.Exclude<Static>(),
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
				LocalTransformLookup = GetComponentLookup<LocalTransform>(true),
				LastSystemVersion = LastSystemVersion
			}.ScheduleParallel(_dynamicRootObjectQuery, Dependency);
		}
	}
}