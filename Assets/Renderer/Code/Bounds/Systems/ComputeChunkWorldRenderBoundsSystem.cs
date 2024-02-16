using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup))]
	[UpdateAfter(typeof(ComputeWorldRenderBoundsSystem))]
	public partial class ComputeChunkWorldRenderBoundsSystem : SystemBase
	{
		private EntityQuery _dynamicChunksQuery;

		protected override void OnCreate()
		{
			_dynamicChunksQuery = GetEntityQuery(
				ComponentType.ChunkComponent<ChunkWorldRenderBounds>(), 
				ComponentType.Exclude<Static>(),
				ComponentType.ReadOnly<WorldRenderBounds>());

			// We only need to recalculate ChunkWorldRenderBounds if any of the 'WorldRenderBounds' of Entities is changed
			_dynamicChunksQuery.SetChangedVersionFilter(ComponentType.ReadOnly<WorldRenderBounds>());
		}

		protected override void OnUpdate()
		{
			Dependency = new ComputeChunkBoundsJob
			{
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>()
			}.ScheduleParallel(_dynamicChunksQuery, Dependency);
		}
	}
}