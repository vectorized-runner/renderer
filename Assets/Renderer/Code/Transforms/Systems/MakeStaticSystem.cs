using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(TransformsGroup))]
	[UpdateBefore(typeof(ComputeWorldMatrixSystem))]
	public partial class MakeStaticSystem : SystemBase
	{
		private EntityQuery _query;

		protected override void OnUpdate()
		{
			Entities.WithAll<MakeStatic>().ForEach((ref WorldRenderBounds worldRenderBounds,
					ref LocalToWorld localToWorld, in Position position, in Rotation rotation, in Scale scale,
					in RenderBounds renderBounds) =>
				{
					localToWorld = new LocalToWorld
						{ Value = float4x4.TRS(position.Value, rotation.Value, scale.Value) };
					worldRenderBounds = ComputeWorldRenderBoundsSystem.CalculateWorldBounds(renderBounds, localToWorld);
				})
				.WithStoreEntityQueryInField(ref _query).Run();

			EntityManager.RemoveComponent<MakeStatic>(_query);
			EntityManager.RemoveComponent<Position>(_query);
			EntityManager.RemoveComponent<Rotation>(_query);
			EntityManager.RemoveComponent<Scale>(_query);		
			EntityManager.RemoveComponent<RenderBounds>(_query);
		}
	}
}