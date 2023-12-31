using Unity.Entities;

namespace Renderer
{
    public struct RenderMeshIndex : IComponentData
    {
        public int Value;

        public RenderMeshIndex(int value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}