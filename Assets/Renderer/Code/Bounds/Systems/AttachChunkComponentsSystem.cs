using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup), OrderFirst = true)]
	public partial class AttachChunkComponentsSystem : SystemBase
	{
		private EntityQuery _worldLackQuery;

		protected override void OnCreate()
		{
			_worldLackQuery = GetEntityQuery(ComponentType.ChunkComponentExclude<ChunkWorldRenderBounds>());
		}

		protected override void OnUpdate()
		{
			EntityManager.AddChunkComponentData(_worldLackQuery, new ChunkWorldRenderBounds());
		}
	}
}