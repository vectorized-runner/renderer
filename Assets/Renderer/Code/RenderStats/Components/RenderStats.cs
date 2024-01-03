using Unity.Entities;

namespace Renderer
{
    public struct RenderStats : IComponentData
    {
        public float AverageFps;
        public float AverageMs;
    }
}