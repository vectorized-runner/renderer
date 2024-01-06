using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup))]
	public partial class AttachChunkComponentsSystem : SystemBase
	{
		private EntityQuery _worldLackQuery;
		private EntityQuery _cullLackQuery;

		protected override void OnCreate()
		{
			_worldLackQuery = GetEntityQuery(ComponentType.ChunkComponentExclude<ChunkWorldRenderBounds>());
			_cullLackQuery = GetEntityQuery(ComponentType.ChunkComponentExclude<ChunkCullResult>());
		}

		protected override void OnUpdate()
		{
			EntityManager.AddChunkComponentData(_worldLackQuery, new ChunkWorldRenderBounds());
			EntityManager.AddChunkComponentData(_cullLackQuery, new ChunkCullResult());
		}
	}
}