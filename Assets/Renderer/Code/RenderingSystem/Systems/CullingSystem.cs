using Unity.Entities;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
    // How does IJobChunk work
    // Think about a single Job per chunk
    // Could be running on a separate thread
    // What do we have on this Chunk -> an unique archetype, RenderMeshIndex we're writing to is single
    // But multiple threads (Jobs) could be using the same RenderMeshIndex at the same time (need parallel write)
    [BurstCompile]
    public struct ChunkCullingJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<RenderMeshIndex> RenderMeshHandle;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;
        [ReadOnly] public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;
        [ReadOnly] public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;
        [ReadOnly] public NativeArray<Plane> FrustumPlanes;

        public NativeArray<UnsafeStream> MatricesByRenderMeshIndex;

        [NativeSetThreadIndex] public int ThreadIndex;

        private void AssertValid(AABB aabb)
        {
            Debug.Assert(!math.any(math.isnan(aabb.Min)));
            Debug.Assert(!math.any(math.isinf(aabb.Min)));
            Debug.Assert(!math.any(math.isnan(aabb.Max)));
            Debug.Assert(!math.any(math.isinf(aabb.Max)));
        }

        // Chunk A R=1, Chunk B R=1, Chunk C R=2, Thread 1, Thread 2

        // Goal at this Job: Do check for culling, if culling passes, add entity matrix to render list
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            // TODO: Add culling later. Currently Just add all Entities for Rendering.
            // TODO: Rethink the culling logic here (partial, full-in, full-out)
            // TODO: If ChunkRenderBounds isn't visible, don't check the Entities for visibility

            // All entities have different RenderMeshIndex value
            var renderMeshIndexArray = chunk.GetNativeArray(ref RenderMeshHandle);
            var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
            var localToWorldArray = chunk.GetNativeArray(ref LocalToWorldHandle);

            var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
            while (enumerator.NextEntityIndex(out var i))
            {
                var aabb = worldRenderBoundsArray[i].AABB;

                if (RMath.IsVisibleByCameraFrustum(FrustumPlanes, aabb))
                {
                    // We have the Entity right now. We want to add it for rendering.
                    // But the Render Batches are by render mesh index
                    // TODO: Is this thread-safe?
                    var renderMeshIndex = renderMeshIndexArray[i].Value;
                    var matrixStream = MatricesByRenderMeshIndex[renderMeshIndex];
                    var matrixWriter = matrixStream.AsWriter();
                }
            }
        }
    }

    [BurstCompile]
    public unsafe struct ConvertStreamDataToArrayJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<UnsafeStream> Input;

        [WriteOnly] public NativeArray<UnsafeList<float4x4>> Output;

        public void Execute(int index)
        {
            var allocator = Allocator.Temp;
            var nativeArray = Input[index].ToNativeArray<float4x4>(allocator);
            Output[index] = new UnsafeList<float4x4>(nativeArray.GetTypedPtr(), nativeArray.Length);
        }
    }

    [UpdateInGroup(typeof(CullingGroup))]
    public partial class ChunkCullingSystem : SystemBase
    {
        public NativeArray<UnsafeList<float4x4>> MatricesByRenderMeshIndex;
        public JobHandle FinalJobHandle { get; private set; }
        public List<RenderMesh> RenderMeshes { get; private set; }

        EntityQuery ChunkCullingQuery;
        CalculateCameraFrustumPlanesSystem FrustumSystem;
        NativeList<UnsafeStream> MatrixStreamByRenderMeshIndex;

        protected override void OnCreate()
        {
            RenderMeshes = new List<RenderMesh>();
            FrustumSystem = World.GetExistingSystemManaged<CalculateCameraFrustumPlanesSystem>();
            MatrixStreamByRenderMeshIndex = new NativeList<UnsafeStream>(Allocator.Persistent);

            ChunkCullingQuery = GetEntityQuery(
                ComponentType.ReadOnly<WorldRenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly(typeof(RenderMesh)),
                ComponentType.ChunkComponentReadOnly(typeof(ChunkWorldRenderBounds)));
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < MatrixStreamByRenderMeshIndex.Length; i++)
            {
                ref var stream = ref MatrixStreamByRenderMeshIndex.ElementAsRef(i);
                stream.Dispose();
            }

            MatrixStreamByRenderMeshIndex.Dispose();

            DisposeMatrixArray();
        }

        private void DisposeMatrixArray()
        {
            if (MatricesByRenderMeshIndex.IsCreated)
            {
                // No need to Dispose Internal UnsafeArrays because they were created with Temp memory.
                MatricesByRenderMeshIndex.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            DisposeMatrixArray();

            // TODO-Renderer: Ensure no RenderMeshes are created between update of this system and end of update of the Renderer
            var frustumPlanes = FrustumSystem.NativeFrustumPlanes;
            var matricesByRenderMeshIndex = MatrixStreamByRenderMeshIndex;

            RenderMeshes.Clear();
            var renderMeshCount = RenderMeshes.Count;
            Debug.Assert(renderMeshCount > 0);

            var updateStreamsHandle =
                Job.WithCode(() =>
                    {
                        // Dispose previous frame Streams and Recreate them
                        for (int i = 0; i < matricesByRenderMeshIndex.Length; i++)
                        {
                            ref var matrices = ref matricesByRenderMeshIndex.ElementAsRef(i);
                            matrices.Dispose();
                            matrices = new UnsafeStream(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
                        }

                        // Add new streams to match the RenderMesh count
                        while (matricesByRenderMeshIndex.Length < renderMeshCount)
                        {
                            matricesByRenderMeshIndex.Add(new UnsafeStream(JobsUtility.MaxJobThreadCount,
                                Allocator.TempJob));
                        }
                    })
                    .WithName("UpdateMatrixStreamsJob")
                    .Schedule(Dependency);

            var chunkCullingHandle = new ChunkCullingJob
            {
                ChunkWorldRenderBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(),
                LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(),
                RenderMeshHandle = GetComponentTypeHandle<RenderMeshIndex>(),
                WorldRenderBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(),
                FrustumPlanes = frustumPlanes,
                MatricesByRenderMeshIndex = matricesByRenderMeshIndex.AsDeferredJobArray(),
            }.ScheduleParallel(ChunkCullingQuery, updateStreamsHandle);

            var matrixArrayByRenderMeshIndex =
                new NativeArray<UnsafeList<float4x4>>(renderMeshCount, Allocator.TempJob);
            MatricesByRenderMeshIndex = matrixArrayByRenderMeshIndex;

            var convertStreamJobHandle = new ConvertStreamDataToArrayJob
            {
                Input = matricesByRenderMeshIndex.AsDeferredJobArray(),
                Output = matrixArrayByRenderMeshIndex,
            }.Schedule(renderMeshCount, 16, chunkCullingHandle);

            Dependency = FinalJobHandle = convertStreamJobHandle;
        }
    }
}