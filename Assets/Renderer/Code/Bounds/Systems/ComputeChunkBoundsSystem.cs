using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup))]
	[UpdateAfter(typeof(ComputeWorldRenderBoundsSystem))]
	public partial class ComputeChunkBoundsSystem : SystemBase
	{
		private EntityQuery _changedChunksQuery;

		protected override void OnCreate()
		{
			_changedChunksQuery = GetEntityQuery(
				ComponentType.ChunkComponent<ChunkWorldRenderBounds>(),
				ComponentType.ReadOnly<WorldRenderBounds>());

			// TODO-Renderer: Change filtering doesn't work when spawning Entities dynamically (through SpawnerSystem) -- I couldn't figure out how to fix
			// We only need to recalculate ChunkWorldRenderBounds if any of the 'WorldRenderBounds' of Entities is changed
			// _changedChunksQuery.SetChangedVersionFilter(ComponentType.ReadOnly<WorldRenderBounds>());
		}

		protected override void OnUpdate()
		{
			Dependency = new ComputeChunkBoundsJob
			{
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>()
			}.ScheduleParallel(_changedChunksQuery, Dependency);
		}
	}
}