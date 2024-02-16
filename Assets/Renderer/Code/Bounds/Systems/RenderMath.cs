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
	}
}