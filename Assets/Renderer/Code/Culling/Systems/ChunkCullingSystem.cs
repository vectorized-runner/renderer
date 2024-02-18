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

		public int CulledObjectCount => _culledObjectCounter.Count;
		public int FrustumInCount => _frustumInCount.Count;
		public int FrustumOutCount => _frustumOutCount.Count;
		public int FrustumPartialCount => _frustumPartialCount.Count;

		private NativeList<UnsafeAtomicCounter> _renderCountByRenderMeshIndex;
		private NativeAtomicCounter _frustumInCount;
		private NativeAtomicCounter _frustumOutCount;
		private NativeAtomicCounter _frustumPartialCount;
		private NativeAtomicCounter _culledObjectCounter;
		private EntityQuery _cullingQuery;
		private CalculateCameraFrustumPlanesSystem _frustumSystem;

		protected override void OnCreate()
		{
			_frustumSystem = World.GetExistingSystemManaged<CalculateCameraFrustumPlanesSystem>();

			MatricesByRenderMeshIndex = new NativeList<UnsafeList<float4x4>>(0, Allocator.Persistent);
			_renderCountByRenderMeshIndex = new NativeList<UnsafeAtomicCounter>(0, Allocator.Persistent);

			_cullingQuery = GetEntityQuery(
				ComponentType.ReadOnly<WorldRenderBounds>(),
				ComponentType.ReadOnly<LocalToWorld>(),
				ComponentType.ReadOnly<RenderMesh>(),
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

			var sharedComponentCounter = new NativeParallelHashSet<int>(32, Allocator.TempJob);

			new CountSharedComponentsJob
			{
				Counter = sharedComponentCounter,
				RenderMeshHandle = GetSharedComponentTypeHandle<RenderMesh>()
			}.Run(_cullingQuery);

			var uniqueMeshCount = sharedComponentCounter.Count();

			// +1, because when we have 1 mesh, Unity returns SharedComponentIndex of 1
			// We need to offset it by leaving the zero index empty
			var requiredArraySize = uniqueMeshCount + 1;

			while (MatricesByRenderMeshIndex.Length < requiredArraySize)
			{
				MatricesByRenderMeshIndex.Add(new UnsafeList<float4x4>(0, Allocator.Persistent));
				_renderCountByRenderMeshIndex.Add(new UnsafeAtomicCounter(Allocator.Persistent));
			}

			var countAsArray = _renderCountByRenderMeshIndex.AsArray();
			var matrixAsArray = MatricesByRenderMeshIndex.AsArray();

			var clearCountersJob = new ClearCountersJob
			{
				CountByRenderMeshIndex = countAsArray,
			}.Schedule(requiredArraySize, 64, Dependency);

			var chunkCullingJob = new ChunkCullingJob
			{
				PlanePackets = planePackets,
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(true),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(true),
				ChunkCullResultHandle = GetComponentTypeHandle<ChunkCullResult>(),
				RenderMeshHandle = GetSharedComponentTypeHandle<RenderMesh>(),
				RenderCountByRenderMeshIndex = countAsArray,
				CulledObjectCount = _culledObjectCounter,
				FrustumOutCount = _frustumOutCount,
				FrustumInCount = _frustumInCount,
				FrustumPartialCount = _frustumPartialCount,
			}.ScheduleParallel(_cullingQuery, clearCountersJob);

			var initializeRenderBatchesJob = new InitializeRenderBatchesJob
			{
				RenderMatricesByRenderMeshIndex = matrixAsArray,
				RenderCountByRenderMeshIndex = countAsArray,
			}.Schedule(requiredArraySize, 64, chunkCullingJob);

			var collectRenderBatchesJob = new CollectRenderBatchesJob
			{
				MatricesByRenderMeshIndex = matrixAsArray,
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(true),
				LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(true),
				RenderMeshIndexHandle = GetSharedComponentTypeHandle<RenderMesh>()
			}.ScheduleParallel(_cullingQuery, initializeRenderBatchesJob);

			Dependency = FinalJobHandle = collectRenderBatchesJob;
		}
	}
}