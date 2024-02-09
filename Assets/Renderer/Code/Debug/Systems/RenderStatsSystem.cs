using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderDebugGroup))]
	public partial class RenderStatsSystem : SystemBase
	{
		private const int _collectedFrames = 60;
		private NativeArray<float> _averageMsList;
		private float _lastUpdateTime;
		private int _replaceIndex;
		private RenderingSystem _renderingSystem;
		private ChunkCullingSystem _cullingSystem;
		
		protected override void OnCreate()
		{
			_averageMsList = new NativeArray<float>(_collectedFrames, Allocator.Persistent);

			for (var i = 0; i < _collectedFrames; i++)
				// Initialize to 0, will be populated in 60 frames anyway
				_averageMsList[i] = 0;

			EntityManager.AddComponent<RenderStats>(SystemHandle);

			_renderingSystem = World.GetExistingSystemManaged<RenderingSystem>();
			_cullingSystem = World.GetExistingSystemManaged<ChunkCullingSystem>();
		}

		protected override void OnDestroy()
		{
			_averageMsList.Dispose();
		}

		protected override void OnUpdate()
		{
			var currentTime = (float)SystemAPI.Time.ElapsedTime;
			var deltaTime = currentTime - _lastUpdateTime;

			// 100ms -> 1000/10 -> 10fps
			// 33ms -> 1000/33 -> 30fps
			const float secondsToMilliseconds = 1000.0f;
			var thisFrameMs = deltaTime * secondsToMilliseconds;

			_averageMsList[_replaceIndex++] = thisFrameMs;

			if (_replaceIndex == _collectedFrames) _replaceIndex = 0;

			var totalMs = 0f;

			for (var i = 0; i < _collectedFrames; i++) totalMs += _averageMsList[i];

			var averageMs = totalMs / _collectedFrames;
			var averageFps = (int)math.floor(1000f / averageMs);

			var query = GetEntityQuery(typeof(RenderMeshIndex));
			var entityCount = query.CalculateEntityCount();
			var chunkCount = query.CalculateChunkCount();

			EntityManager.SetComponentData(SystemHandle, new RenderStats
			{
				AverageMs = averageMs,
				AverageFps = averageFps,
				RenderedCount = _renderingSystem.RenderedObjectCount,
				CulledCount = _cullingSystem.CulledObjectCount,
				RenderTrisCount = _renderingSystem.RenderedTris,
				RenderVertsCount = _renderingSystem.RenderedVerts,
				TotalObjectCount = entityCount,
				RenderBatchCount = _renderingSystem.RenderBatchCount,
				OutChunks = _cullingSystem.FrustumOutCount,
				InChunks = _cullingSystem.FrustumInCount,
				PartialChunks = _cullingSystem.FrustumPartialCount,
				ChunkCount = chunkCount
			});

			_lastUpdateTime = currentTime;
		}
	}
}