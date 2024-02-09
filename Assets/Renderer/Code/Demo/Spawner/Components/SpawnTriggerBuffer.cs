using Unity.Entities;

namespace Renderer
{
	public struct SpawnTriggerBuffer : IBufferElementData
	{
		public SpawnTrigger Value;
	}
}