using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup))]
	public partial class AttachChunkComponentsSystem : SystemBase
	{
		private EntityQuery _chunkLackQuery;

		protected override void OnCreate()
		{
			_chunkLackQuery = GetEntityQuery(
				ComponentType.ChunkComponentExclude<ChunkWorldRenderBounds>(),
				ComponentType.ChunkComponentExclude<ChunkCullResult>());
		}

		protected override void OnUpdate()
		{
			EntityManager.AddChunkComponentData(_chunkLackQuery, new ChunkWorldRenderBounds());
			EntityManager.AddChunkComponentData(_chunkLackQuery, new ChunkCullResult());
		}
	}
}