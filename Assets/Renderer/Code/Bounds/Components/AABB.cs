using Unity.Mathematics;

namespace Renderer
{
	public struct AABB
	{
		public float3 Center;
		public float3 Extents;

		public float3 Min => Center - Extents;
		public float3 Max => Center + Extents;
	}
}