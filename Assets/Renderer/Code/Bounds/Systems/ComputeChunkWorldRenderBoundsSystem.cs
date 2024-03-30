using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup))]
	[UpdateAfter(typeof(ComputeWorldRenderBoundsSystem))]
	public partial class ComputeChunkWorldRenderBoundsSystem : SystemBase
	{
		private EntityQuery _changedChunksQuery;

		protected override void OnCreate()
		{
			// Notice: We shouldn't exclude Static entities,
			// Because new Static entities might get added into an existing count, requiring bounds re-calculation
			_changedChunksQuery = GetEntityQuery(
				ComponentType.ChunkComponent<ChunkWorldRenderBounds>(),
				ComponentType.ReadOnly<WorldRenderBounds>());
		}

		protected override void OnUpdate()
		{
			// We only need to recalculate ChunkWorldRenderBounds if any of the 'WorldRenderBounds' of Entities is changed
			Dependency = new ComputeChunkBoundsJob
			{
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(true),
				LastSystemVersion = LastSystemVersion
			}.ScheduleParallel(_changedChunksQuery, Dependency);
		}
	}
}