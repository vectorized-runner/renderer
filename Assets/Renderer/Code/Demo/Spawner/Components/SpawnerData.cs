using Unity.Entities;

namespace Renderer
{
	public struct SpawnerData : IComponentData
	{
		public BlobAssetReference<SpawnEntityArray> SpawnEntityArrayRef;
	}
}