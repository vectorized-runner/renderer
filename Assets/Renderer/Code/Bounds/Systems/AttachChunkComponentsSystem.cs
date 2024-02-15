using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup), OrderFirst = true)]
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
			EntityManager.AddChunkComponentData(_cullLackQuery, new ChunkCullResult { Value = new BitField128(0, 0)});
		}
	}
}