using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Renderer
{
	public struct SpawnerData : IComponentData
	{
		public UnsafeHashMap<FixedString64Bytes, Entity> PrefabByLabel;
	}
}