using Unity.Entities;

namespace Renderer
{
	public struct SpawnTriggerElement : IBufferElementData
	{
		public SpawnTrigger Value;
	}
}