using Unity.Entities;

namespace Renderer
{
	public struct SpawnEntityElement : IBufferElementData
	{
		public SpawnEntity Value;
	}
}