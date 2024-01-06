using Unity.Entities;

namespace Renderer
{
	public partial class ComputeChunkBoundsSystem : SystemBase
	{
		private EntityQuery _changedChunksQuery;

		protected override void OnCreate()
		{
			_changedChunksQuery = GetEntityQuery(
				ComponentType.ChunkComponent<ChunkWorldRenderBounds>(),
				ComponentType.ReadOnly<WorldRenderBounds>());

			// We only need to recalculate ChunkWorldRenderBounds if any of the 'WorldRenderBounds' of Entities is changed
			_changedChunksQuery.SetChangedVersionFilter(ComponentType.ReadOnly<WorldRenderBounds>());
		}

		protected override void OnUpdate()
		{
			Dependency = new ComputeChunkBoundsJob
			{
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>()
			}.Schedule(_changedChunksQuery, Dependency);
		}
	}
}