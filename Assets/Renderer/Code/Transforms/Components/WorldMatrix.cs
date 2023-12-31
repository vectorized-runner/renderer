using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
    public struct WorldMatrix : IComponentData
    {
        public float4x4 Value;
    }
}