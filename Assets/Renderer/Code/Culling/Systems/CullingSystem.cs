using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
	// TODO: This System has data dependency to camera, get that shit working please?
	[UpdateInGroup(typeof(CullingGroup))]
	public partial class ChunkCullingSystem : SystemBase
	{
		public JobHandle FinalJobHandle { get; private set; }
		public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;

		private EntityQuery _chunkCullingQuery;
		private CalculateCameraFrustumPlanesSystem _frustumSystem;

		protected override void OnCreate()
		{
			_frustumSystem = World.GetExistingSystemManaged<CalculateCameraFrustumPlanesSystem>();

			const int maxMeshCount = RenderMeshRegisterSystem.MaxSupportedUniqueMeshCount;

			MatricesByRenderMeshIndex =
				new NativeArray<UnsafeList<float4x4>>(maxMeshCount, Allocator.Persistent);

			for (var i = 0; i < maxMeshCount; i++)
				MatricesByRenderMeshIndex[i] = new UnsafeList<float4x4>(0, Allocator.Persistent);

			_chunkCullingQuery = GetEntityQuery(
				ComponentType.ReadOnly<WorldRenderBounds>(),
				ComponentType.ReadOnly<LocalToWorld>(),
				ComponentType.ReadOnly<RenderMeshIndex>(),
				ComponentType.ChunkComponentReadOnly(typeof(ChunkCullResult)));
			ComponentType.ChunkComponentReadOnly(typeof(ChunkWorldRenderBounds));
		}

		protected override void OnDestroy()
		{
			for (var i = 0; i < MatricesByRenderMeshIndex.Length; i++)
			{
				ref var matrices = ref MatricesByRenderMeshIndex.ElementAsRef(i);
				matrices.Dispose();
			}

			MatricesByRenderMeshIndex.Dispose();
		}

		// TODO: What happens if new objects are created when these jobs are running [?]
		protected override void OnUpdate()
		{
			var frustumPlanes = _frustumSystem.NativeFrustumPlanes;

			// TODO: Use the async version of this (?)
			var chunks = _chunkCullingQuery.ToArchetypeChunkArray(Allocator.TempJob);

			var cullHandle = new ChunkCullingJob
			{
				FrustumPlanes = frustumPlanes,
				ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
				WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(),
				ChunkCullResultHandle = GetComponentTypeHandle<ChunkCullResult>()
			}.ScheduleParallel(_chunkCullingQuery, Dependency);

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
	}
}