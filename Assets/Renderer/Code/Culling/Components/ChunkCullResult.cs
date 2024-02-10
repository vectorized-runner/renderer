using Unity.Entities;

namespace Renderer
{
	public struct ChunkCullResult : IComponentData
	{
		public BitField128 Value;
	}
}