using Unity.Entities;

namespace Renderer
{
    public struct RenderBounds : IComponentData
    {
        public AABB AABB;
    }
}