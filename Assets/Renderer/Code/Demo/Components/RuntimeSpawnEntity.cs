using Unity.Collections;
using Unity.Entities;

namespace Renderer
{
	public struct RuntimeSpawnEntity : IComponentData
	{
		public Entity Entity;
		public FixedString64Bytes Label;
	}
}