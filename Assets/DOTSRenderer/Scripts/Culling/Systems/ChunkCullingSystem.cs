using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(CullingGroup))]
	public partial class ChunkCullingSystem : SystemBase
	{
		public JobHandle FinalJobHandle { get; private set; }
		public NativeList<UnsafeList<float4x4>> MatricesByRenderMeshIndex;
		public EntityQuery CullingQuery { get; private set; }

#if RENDERER_DEBUG
		public int VisibleObjectCount => _visibleObjectCounter.Count;
		public int FrustumInCount => _frustumInCount.Count;
		public int FrustumOutCount => _frustumOutCount.Count;
		public int FrustumPartialCount => _frustumPartialCount.Count;
		private NativeAtomicCounter _frustumInCount;
		private NativeAtomicCounter _frustumOutCount;
		private NativeAtomicCounter _frustumPartialCount;
		private NativeAtomicCounter _visibleObjectCounter;
#endif
		
		public int UniqueMeshCount { get; private set; }

		private NativeList<UnsafeAtomicCounter> _renderCountByRenderMeshIndex;

		private CalculateCameraFrustumPlanesSystem _frustumSystem;

		protected override void OnCreate()
		{
			_frustumSystem = World.GetExistingSystemManaged<CalculateCameraFrustumPlanesSystem>();

			MatricesByRenderMeshIndex = new NativeList<UnsafeList<float4x4>>(0, Allocator.Persistent);
			_renderCountByRenderMeshIndex = new NativeList<UnsafeAtomicCounter>(0, Allocator.Persistent);

			CullingQuery = GetEntityQuery(
				ComponentType.ReadOnly<WorldRenderBounds>(),
				ComponentType.ReadOnly<LocalToWorld>(),
				ComponentType.ReadOnly<RenderMesh>(),
				ComponentType.ChunkComponentReadOnly(typeof(ChunkCullResult)));
			ComponentType.ChunkComponentReadOnly(typeof(ChunkWorldRenderBounds));

#if RENDERER_DEBUG
			_visibleObjectCounter = new NativeAtomicCounter(Allocator.Persistent);
			_frustumInCount = new NativeAtomicCounter(Allocator.Persistent);
			_frustumOutCount = new NativeAtomicCounter(Allocator.Persistent);
			_frustumPartialCount = new NativeAtomicCounter(Allocator.Persistent);
#endif
		}

		protected override void OnDestroy()
		{
			for (var i = 0; i < MatricesByRenderMeshIndex.Length; i++)
			{
				ref var matrices = ref MatricesByRenderMeshIndex.ElementAsRef(i);
				matrices.Dispose();

				ref var counter = ref _renderCountByRenderMeshIndex.ElementAsRef(i);
				counter.Dispose();
			}

			MatricesByRenderMeshIndex.Dispose();
			_renderCountByRenderMeshIndex.Dispose();

#if RENDERER_DEBUG
			_visibleObjectCounter.Dispose();
			_frustumInCount.Dispose();
			_frustumPartialCount.Dispose();
			_frustumOutCount.Dispose();
#endif
		}

		protected override void OnUpdate()
		{
			var planePackets = _frustumSystem.PlanePackets;

#if RENDERER_DEBUG
			_visibleObjectCounter.Count = 0;
			_frustumPartialCount.Count = 0;
			_frustumOutCount.Count = 0;
			_frustumInCount.Count = 0;
#endif

			var sharedComponentCounter = new NativeParallelHashSet<int>(32, Allocator.TempJob);

			new CountSharedComponentsJob
			{
				Counter = sharedComponentCounter,
				RenderMeshHandle = GetSharedComponentTypeHandle<RenderMesh>()
			}.Run(CullingQuery);

			UniqueMeshCount = sharedComponentCounter.Count();
			// No mesh, no culling
			if (UniqueMeshCount == 0)
			{
				sharedComponentCounter.Dispose();
				return;
			}

			// Stupid Unity returns SharedComponentIndex of 2, when there's only 1 shared component
			// So this is a hack to allocate extra matrix arrays even though it wouldn't be required in an ideal case
			var renderMeshIndexValues = sharedComponentCounter.ToNativeArray(Allocator.Temp);
			var maxIndex = 0;

			foreach (var renderMeshIndex in renderMeshIndexValues)
			{
				maxIndex = math.max(renderMeshIndex, maxIndex);
			}

			maxIndex += 1;
			sharedComponentCounter.Dispose();

			while (MatricesByRenderMeshIndex.Length < maxIndex)
			{
				MatricesByRenderMeshIndex.Add(new UnsafeList<float4x4>(0, Allocator.Persistent));
				_renderCountByRenderMeshIndex.Add(new UnsafeAtomicCounter(Allocator.Persistent));
			}

			var countAsArray = _renderCountByRenderMeshIndex.AsArray();
			var matrixAsArray = MatricesByRenderMeshIndex.AsArray();

			var clearCountersJob = new ClearCountersJob
			{
				CountByRenderMeshIndex = countAsArray,
			}.Schedule(maxIndex, 64, Dependency);

			var chunkCullingJob = new ChunkCullingJob
			{
				PlanePackets = planePackets,
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(true),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(true),
				ChunkCullResultHandle = GetComponentTypeHandle<ChunkCullResult>(),
				RenderMeshHandle = GetSharedComponentTypeHandle<RenderMesh>(),
				RenderCountByRenderMeshIndex = countAsArray,
#if RENDERER_DEBUG 
				VisibleObjectCount = _visibleObjectCounter,
				FrustumOutCount = _frustumOutCount,
				FrustumInCount = _frustumInCount,
				FrustumPartialCount = _frustumPartialCount,
#endif
			}.ScheduleParallel(CullingQuery, clearCountersJob);

			var initializeRenderBatchesJob = new InitializeRenderBatchesJob
			{
				RenderMatricesByRenderMeshIndex = matrixAsArray,
				RenderCountByRenderMeshIndex = countAsArray,
			}.Schedule(maxIndex, 64, chunkCullingJob);

			var collectRenderBatchesJob = new CollectRenderBatchesJob
			{
				MatricesByRenderMeshIndex = matrixAsArray,
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(true),
				LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(true),
				RenderMeshIndexHandle = GetSharedComponentTypeHandle<RenderMesh>()
			}.ScheduleParallel(CullingQuery, initializeRenderBatchesJob);

			Dependency = FinalJobHandle = collectRenderBatchesJob;
		}
	}
}