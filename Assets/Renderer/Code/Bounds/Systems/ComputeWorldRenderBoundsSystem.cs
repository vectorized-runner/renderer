using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup))]
	public partial class ComputeWorldRenderBoundsSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			Entities
				.WithNone<Static>()
				.WithChangeFilter<LocalToWorld>()
				.ForEach((ref WorldRenderBounds worldRenderBounds,
					in RenderBounds renderBounds,
					in LocalToWorld localToWorld) =>
				{
					worldRenderBounds = RenderMath.ComputeWorldRenderBounds(renderBounds, localToWorld);
				})
				.ScheduleParallel();
		}
	}
}