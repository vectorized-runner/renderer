using Unity.Collections;
using Unity.Entities;

namespace Renderer
{
	public struct SpawnTrigger : IComponentData
	{
		public FixedString64Bytes Label;
		public int Amount;
	}
}