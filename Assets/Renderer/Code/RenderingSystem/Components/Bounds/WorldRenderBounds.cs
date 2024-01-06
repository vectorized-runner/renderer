using Unity.Entities;

namespace Renderer
{
    public struct WorldRenderBounds : IComponentData
    {
        public AABB AABB;
    }
}