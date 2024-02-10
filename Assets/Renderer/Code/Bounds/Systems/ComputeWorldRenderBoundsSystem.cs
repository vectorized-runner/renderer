using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup))]
	public partial class ComputeWorldRenderBoundsSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			// TODO-Renderer: Instead of LocalToWorld, use Translation, Rotation, Scale change filter
			// Static Entities don't have RenderBounds anyway, but still adding the filter.
			Entities
				.WithNone<Static>()
				.WithChangeFilter<LocalToWorld>()
				.ForEach((ref WorldRenderBounds worldRenderBounds,
					in RenderBounds renderBounds,
					in LocalToWorld localToWorld) =>
				{
					worldRenderBounds = CalculateWorldBounds(renderBounds, localToWorld);
				})
				.ScheduleParallel();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static WorldRenderBounds CalculateWorldBounds(RenderBounds localBounds, LocalToWorld localToWorld)
		{
			var aabb = localBounds.AABB;
			// Scaled orientation (?)
			var localRight = localToWorld.Right * aabb.Extents.x;
			var localUp = localToWorld.Up * aabb.Extents.y;
			var localForward = localToWorld.Forward * aabb.Extents.z;
			var right = math.right();
			var up = math.up();
			var forward = math.forward();

			var newIi =
				math.abs(math.dot(right, localRight)) +
				math.abs(math.dot(right, localUp)) +
				math.abs(math.dot(right, localForward));

			var newIj =
				math.abs(math.dot(up, localRight)) +
				math.abs(math.dot(up, localUp)) +
				math.abs(math.dot(up, localForward));

			var newIk =
				math.abs(math.dot(forward, localRight)) +
				math.abs(math.dot(forward, localUp)) +
				math.abs(math.dot(forward, localForward));

			var worldExtents = new float3(newIi, newIj, newIk);
			var center = localToWorld.Position;

			return new WorldRenderBounds
			{
				AABB = new AABB
				{
					Center = center,
					Extents = worldExtents
				}
			};
		}
	}
}