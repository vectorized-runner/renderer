using Unity.Entities;

namespace Renderer
{
	public readonly struct ChunkWorldRenderBounds : IComponentData
	{
		public readonly AABB AABB;

		public ChunkWorldRenderBounds(AABB aabb)
		{
			AABB = aabb;
		}
	}
}