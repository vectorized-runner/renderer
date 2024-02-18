using Unity.Mathematics;

namespace Renderer
{
	public struct AABB
	{
		public float3 Center;
		public float3 Extents;

		public float3 Min => Center - Extents;
		public float3 Max => Center + Extents;

		public void Encapsulate(float3 point)
		{
			var newMin = math.min(Min, point);
			var newMax = math.max(Max, point);
			Center = (newMin + newMax) * 0.5f;
			Extents = (newMax - newMin) * 0.5f;
		}
	}
}