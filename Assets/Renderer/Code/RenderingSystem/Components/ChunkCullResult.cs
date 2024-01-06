using Unity.Collections;
using Unity.Entities;

namespace Renderer
{
    public struct ChunkCullResult : IComponentData
    {
        public BitField64 Lower;
        public BitField64 Upper;
    }
}