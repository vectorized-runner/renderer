using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
    public struct Position : IComponentData
    {
        public float3 Value;
    }
}