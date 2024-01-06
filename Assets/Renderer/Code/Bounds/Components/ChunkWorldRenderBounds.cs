using Unity.Entities;

namespace Renderer
{
    public struct ChunkWorldRenderBounds : IComponentData
    {
        public AABB AABB;
    }
}