using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
	// TODO-Renderer: This System has data dependency to camera, get that shit working please?
	// TODO-Renderer: New workflow, collect all the Render Matrices while iterating in the Job.
	// Requirement: RenderMesh count needs to be known (all memory will be pre-allocated just as if all objects will be rendered) -- required for per-chunk
	// OR -- No pre-allocation, Collect NativeArray<Chunk> per RenderMeshIndex, run the Job on per RenderMeshIndex
	// Possible optimizations: Collect indices first, increment once
	// Make RenderMesh shared component first
	[UpdateInGroup(typeof(CullingGroup))]
	public partial class ChunkCullingSystem : SystemBase
	{
		public JobHandle FinalJobHandle { get; private set; }
		public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;

		public int CulledObjectCount => _culledObjectCounter.Count;
		public int FrustumInCount => _frustumInCount.Count;
		public int FrustumOutCount => _frustumOutCount.Count;
		public int FrustumPartialCount => _frustumPartialCount.Count;

		private NativeArray<UnsafeAtomicCounter> _renderCountByRenderMeshIndex;
		private NativeAtomicCounter _frustumInCount;
		private NativeAtomicCounter _frustumOutCount;
		private NativeAtomicCounter _frustumPartialCount;
		private NativeAtomicCounter _culledObjectCounter;
		private EntityQuery _chunkCullingQuery;
		private CalculateCameraFrustumPlanesSystem _frustumSystem;

		protected override void OnCreate()
		{
			_frustumSystem = World.GetExistingSystemManaged<CalculateCameraFrustumPlanesSystem>();

			const int maxMeshCount = RenderSettings.MaxSupportedUniqueMeshCount;

			MatricesByRenderMeshIndex =
				new NativeArray<UnsafeList<float4x4>>(maxMeshCount, Allocator.Persistent);

			_renderCountByRenderMeshIndex = new NativeArray<UnsafeAtomicCounter>(maxMeshCount, Allocator.Persistent);

			for (var i = 0; i < maxMeshCount; i++)
			{
				MatricesByRenderMeshIndex[i] = new UnsafeList<float4x4>(0, Allocator.Persistent);
				_renderCountByRenderMeshIndex[i] = new UnsafeAtomicCounter(Allocator.Persistent);
			}

			_chunkCullingQuery = GetEntityQuery(
				ComponentType.ReadOnly<WorldRenderBounds>(),
				ComponentType.ReadOnly<LocalToWorld>(),
				ComponentType.ReadOnly<RenderMeshIndex>(),
				ComponentType.ChunkComponentReadOnly(typeof(ChunkCullResult)));
			ComponentType.ChunkComponentReadOnly(typeof(ChunkWorldRenderBounds));

			_culledObjectCounter = new NativeAtomicCounter(Allocator.Persistent);
			_frustumInCount = new NativeAtomicCounter(Allocator.Persistent);
			_frustumOutCount = new NativeAtomicCounter(Allocator.Persistent);
			_frustumPartialCount = new NativeAtomicCounter(Allocator.Persistent);
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

			_culledObjectCounter.Dispose();
			_frustumInCount.Dispose();
			_frustumPartialCount.Dispose();
			_frustumOutCount.Dispose();
		}

		protected override void OnUpdate()
		{
			var planePackets = _frustumSystem.PlanePackets;

			_culledObjectCounter.Count = 0;
			_frustumPartialCount.Count = 0;
			_frustumOutCount.Count = 0;
			_frustumInCount.Count = 0;

			var clearCountersJob = new ClearCountersJob
			{
				CountByRenderMeshIndex = _renderCountByRenderMeshIndex,
			}.Schedule(_renderCountByRenderMeshIndex.Length, 64, Dependency);

			var chunkCullingJob = new ChunkCullingJob
			{
				PlanePackets = planePackets,
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(),
				ChunkCullResultHandle = GetComponentTypeHandle<ChunkCullResult>(),
				RenderMeshIndexHandle = GetSharedComponentTypeHandle<RenderMeshIndex>(),
				RenderCountByRenderMeshIndex = _renderCountByRenderMeshIndex,
				CulledObjectCount = _culledObjectCounter,
				FrustumOutCount = _frustumOutCount,
				FrustumInCount = _frustumInCount,
				FrustumPartialCount = _frustumPartialCount,
			}.ScheduleParallel(_chunkCullingQuery, clearCountersJob);

			var initializeRenderBatchesJob = new InitializeRenderBatchesJob
			{
				RenderMatricesByRenderMeshIndex = MatricesByRenderMeshIndex,
				RenderCountByRenderMeshIndex = _renderCountByRenderMeshIndex,
			}.Schedule(_renderCountByRenderMeshIndex.Length, 64, chunkCullingJob);

			var collectRenderBatchesJob = new CollectRenderBatchesJob
			{
				MatricesByRenderMeshIndex = MatricesByRenderMeshIndex,
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(),
				LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(),
				RenderMeshIndexHandle = GetSharedComponentTypeHandle<RenderMeshIndex>()
			}.ScheduleParallel(_chunkCullingQuery, initializeRenderBatchesJob);

			Dependency = FinalJobHandle = collectRenderBatchesJob;
		}
	}
}