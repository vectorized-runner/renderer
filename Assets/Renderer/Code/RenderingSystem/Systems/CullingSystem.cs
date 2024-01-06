using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Renderer
{
    public partial class ChunkCullingSystem : SystemBase
    {
        public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;

        public JobHandle FinalJobHandle { get; private set; }

        private EntityQuery _chunkCullingQuery;
        private CalculateCameraFrustumPlanesSystem _frustumSystem;

        public const int MaxSupportedUniqueMeshCount = 1024;

        public ChunkCullingSystem(CalculateCameraFrustumPlanesSystem frustumSystem)
        {
            _frustumSystem = frustumSystem;
        }

        protected override void OnCreate()
        {
            _frustumSystem = World.GetExistingSystemManaged<CalculateCameraFrustumPlanesSystem>();

            MatricesByRenderMeshIndex =
                new NativeArray<UnsafeList<float4x4>>(MaxSupportedUniqueMeshCount, Allocator.Persistent);

            for (int i = 0; i < MaxSupportedUniqueMeshCount; i++)
            {
                MatricesByRenderMeshIndex[i] = new UnsafeList<float4x4>(0, Allocator.Persistent);
            }

            _chunkCullingQuery = GetEntityQuery(
                ComponentType.ReadOnly<WorldRenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<RenderMeshIndex>(),
                ComponentType.ChunkComponentReadOnly(typeof(ChunkCullResult)));
                ComponentType.ChunkComponentReadOnly(typeof(ChunkWorldRenderBounds));
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < MatricesByRenderMeshIndex.Length; i++)
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
            _chunkCullingQuery.ToArchetypeChunkArray(Allocator.TempJob);
            
            var cullHandle = new ChunkCullingJob
            {
                ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
                WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(),
                FrustumPlanes = frustumPlanes,
                ChunkCullResultHandle = GetComponentTypeHandle<ChunkCullResult>()
            }.ScheduleParallel(_chunkCullingQuery, Dependency);
            
            var collectJob = new CollectRenderMatricesJob
            {
                Chunks =  
            }.Schedule();
            
            Dependency = FinalJobHandle = ;
        }
    }
}