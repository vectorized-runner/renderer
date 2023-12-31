using Unity.Entities;

namespace Renderer
{
    public struct RenderMeshId : IComponentData
    {
        public int Value;

        public RenderMeshId(int value)
        {
            Value = value;
        }
    }
}