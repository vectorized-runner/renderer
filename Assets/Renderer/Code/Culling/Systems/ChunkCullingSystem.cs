using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
	// TODO-Renderer: This System has data dependency to camera, get that shit working please?
	[UpdateInGroup(typeof(CullingGroup))]
	public partial class ChunkCullingSystem : SystemBase
	{
		public JobHandle FinalJobHandle { get; private set; }
		public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;
		public UnsafeList<UnsafeAtomicCounter> RenderCountByRenderMeshIndex;

		public int CulledObjectCount => _culledObjectCounter.Count;
		public int FrustumInCount => _frustumInCount.Count;
		public int FrustumOutCount => _frustumOutCount.Count;
		public int FrustumPartialCount => _frustumPartialCount.Count;

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

			RenderCountByRenderMeshIndex = new UnsafeList<UnsafeAtomicCounter>(maxMeshCount, Allocator.Persistent);

			for (int i = 0; i < maxMeshCount; i++)
			{
				RenderCountByRenderMeshIndex.Add(new UnsafeAtomicCounter(Allocator.Persistent));
			}

			for (var i = 0; i < maxMeshCount; i++)
				MatricesByRenderMeshIndex[i] = new UnsafeList<float4x4>(0, Allocator.Persistent);

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
			}

			MatricesByRenderMeshIndex.Dispose();

			for (int i = 0; i < RenderCountByRenderMeshIndex.Length; i++)
			{
				ref var counter = ref RenderCountByRenderMeshIndex.ElementAt(i);
				counter.Dispose();
			}

			RenderCountByRenderMeshIndex.Dispose();

			_culledObjectCounter.Dispose();
			_frustumInCount.Dispose();
			_frustumPartialCount.Dispose();
			_frustumOutCount.Dispose();
		}

		// TODO-Renderer: What happens if new objects are created when these jobs are running [?]
		protected override void OnUpdate()
		{
			var planePackets = _frustumSystem.PlanePackets;

			// TODO-Renderer: This can be made a job
			// This should be safe because job should be already completed at this point
			for (int i = 0; i < MatricesByRenderMeshIndex.Length; i++)
			{
				ref var matrices = ref MatricesByRenderMeshIndex.ElementAsRef(i);
				matrices.Clear();
			}

			// TODO-Renderer: Use the async version of this (?)
			var chunks = _chunkCullingQuery.ToArchetypeChunkArray(Allocator.TempJob);
			_culledObjectCounter.Count = 0;
			_frustumPartialCount.Count = 0;
			_frustumOutCount.Count = 0;
			_frustumInCount.Count = 0;

			for (int i = 0; i < RenderCountByRenderMeshIndex.Length; i++)
			{
				// Setting through pointer, shouldn't require ref access
				var counter = RenderCountByRenderMeshIndex[i];
				counter.Count = 0;
			}

			var cullHandle = new ChunkCullingJob
			{
				PlanePackets = planePackets,
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(),
				ChunkCullResultHandle = GetComponentTypeHandle<ChunkCullResult>(),
				RenderMeshIndexHandle = GetComponentTypeHandle<RenderMeshIndex>(),
				CulledObjectCount = _culledObjectCounter,
				FrustumOutCount = _frustumOutCount,
				FrustumInCount = _frustumInCount,
				FrustumPartialCount = _frustumPartialCount,
				RenderCounterByMeshIndex = RenderCountByRenderMeshIndex,
			}.ScheduleParallel(_chunkCullingQuery, Dependency);

			// At this point, how many objects will be drawn is known, just initialize the list to that count

			var collectJob = new CollectRenderMatricesJob
			{
				Chunks = chunks,
				MatricesByRenderMeshIndex = MatricesByRenderMeshIndex,
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(),
				LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(),
				RenderMeshIndexHandle = GetComponentTypeHandle<RenderMeshIndex>()
			}.Schedule(cullHandle);

			Dependency = FinalJobHandle = collectJob;
		}

		public struct InitializeParallelWritersJob : IJob
		{
			public NativeArray<UnsafeList<float4x4>.ParallelWriter> MatricesByRenderMeshIndex;

			public void Execute()
			{
				throw new System.NotImplementedException();
			}
		}
	}
}