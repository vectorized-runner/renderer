using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(TransformsGroup))]
	[UpdateBefore(typeof(ComputeWorldMatrixSystem))]
	[UpdateBefore(typeof(ComputeWorldRenderBoundsSystem))]
	public partial class MakeStaticSystem : SystemBase
	{
		private EntityQuery _makeStaticQuery;

		protected override void OnCreate()
		{
			_makeStaticQuery = GetEntityQuery(typeof(MakeStatic));
		}

		protected override void OnUpdate()
		{
			if (_makeStaticQuery.CalculateEntityCount() == 0)
				return;
			
			Entities.WithAll<MakeStatic>().ForEach((ref WorldRenderBounds worldRenderBounds,
					ref LocalToWorld localToWorld, in Position position, in Rotation rotation, in Scale scale,
					in RenderBounds renderBounds) =>
				{
					localToWorld = new LocalToWorld
						{ Value = float4x4.TRS(position.Value, rotation.Value, scale.Value) };
					worldRenderBounds = ComputeWorldRenderBoundsSystem.CalculateWorldBounds(renderBounds, localToWorld);
				}).Run();

			EntityManager.RemoveComponent<Position>(_makeStaticQuery);
			EntityManager.RemoveComponent<Rotation>(_makeStaticQuery);
			EntityManager.RemoveComponent<Scale>(_makeStaticQuery);		
			EntityManager.RemoveComponent<RenderBounds>(_makeStaticQuery);
			// Notice this has to be done last
			EntityManager.RemoveComponent<MakeStatic>(_makeStaticQuery);
		}
	}
}