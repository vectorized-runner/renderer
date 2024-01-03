using Unity.Entities;

namespace Renderer
{
    public struct RenderSettings : IComponentData
    {
        public RenderMode RenderMode;
    }
}