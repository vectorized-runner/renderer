using Unity.Collections;
using Unity.Entities;

namespace Renderer
{
	public struct SpawnEntity
	{
		public Entity Entity;
		public FixedString64Bytes Label;
	}
}