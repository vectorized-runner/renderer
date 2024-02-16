using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Renderer
{
	public static class RenderMath
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AABB EncapsuleAABBs(AABB first, AABB second)
		{
			var newMin = math.min(first.Min, second.Min);
			var newMax = math.max(first.Max, second.Max);

			return new AABB
			{
				Center = (newMin + newMax) * 0.5f,
				Extents = (newMax - newMin) * 0.5f
			};
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