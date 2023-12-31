using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
    public struct Rotation : IComponentData
    {
        public quaternion Value;
    }
}