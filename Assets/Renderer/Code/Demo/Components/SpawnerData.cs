using Unity.Collections;
using Unity.Entities;

namespace Renderer
{
	public struct SpawnerData : IComponentData
	{
		public FixedList4096Bytes<RuntimeSpawnEntity> RuntimeSpawnEntities;
	}
}