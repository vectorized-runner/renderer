using Renderer.UnityPackages;
using Unity.Entities;

namespace Renderer
{
	public struct ChunkCullResult : IComponentData
	{
		public BitField128 EntityVisibilityMask;
		public FrustumPlanes.IntersectResult IntersectResult;
	}
}