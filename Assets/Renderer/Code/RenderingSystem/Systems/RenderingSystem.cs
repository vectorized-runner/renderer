using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
    public partial class RenderingSystem : SystemBase
    {
        private static Matrix4x4[] _matrixCache;
        private ChunkCullingSystem _cullingSystem;

        private const int _maxDrawCountPerBatch = 1023;

        protected override void OnCreate()
        {
            _cullingSystem = World.GetExistingSystemManaged<ChunkCullingSystem>();
            _matrixCache = new Matrix4x4[_maxDrawCountPerBatch];
        }

        protected override void OnUpdate()
        {
            // TODO-Renderer: Find a way to not call complete on this. Maybe 1 frame delayed rendering is ok?
            _cullingSystem.FinalJobHandle.Complete();

            var renderMeshes = _cullingSystem.RenderMeshes;
            if (renderMeshes.Count == 0)
                return;

            var matricesByRenderMeshIndex = _cullingSystem.MatricesByRenderMeshIndex;
            var renderMeshCount = matricesByRenderMeshIndex.Length;

            for (int renderMeshIndex = 0; renderMeshIndex < renderMeshCount; renderMeshIndex++)
            {
                var matrices = matricesByRenderMeshIndex[renderMeshIndex];
                var drawCount = matrices.Length;
                if (drawCount == 0)
                    continue;

                var renderMesh = renderMeshes[renderMeshIndex];
                var fullBatchCount = drawCount / _maxDrawCountPerBatch;
                int batchIndex;

                for (batchIndex = 0; batchIndex < fullBatchCount; batchIndex++)
                {
                    var span = matrices.AsSpan(batchIndex * _maxDrawCountPerBatch, _maxDrawCountPerBatch);
                    var m4x4 = span.Reinterpret<float4x4, Matrix4x4>();
                    AssertValidMatrices(m4x4);
                    DrawMeshInstanced(renderMesh, m4x4);
                }

                var lastBatchDrawCount = drawCount % _maxDrawCountPerBatch;
                if (lastBatchDrawCount > 0)
                {
                    var span = matrices.AsSpan(batchIndex * _maxDrawCountPerBatch, lastBatchDrawCount);
                    var m4x4 = span.Reinterpret<float4x4, Matrix4x4>();
                    AssertValidMatrices(m4x4);
                    DrawMeshInstanced(renderMesh, m4x4);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DrawMeshInstanced(in RenderMesh renderMesh, ReadOnlySpan<Matrix4x4> matrices)
        {
            Debug.Assert(matrices.Length > 0 && matrices.Length <= 1023);
            matrices.CopyTo(_matrixCache);
            Graphics.DrawMeshInstanced(renderMesh.Mesh, renderMesh.SubMeshIndex, renderMesh.Material, _matrixCache,
                matrices.Length);
        }

        private static void AssertValidMatrices(Span<Matrix4x4> matrices)
        {
            foreach (ref var matrix in matrices)
            {
                ref var f4x4 = ref Unsafe.As<Matrix4x4, float4x4>(ref matrix);
                AssertValid(f4x4.c3.xyz);
            }
        }

        private static void AssertValid(float3 position)
        {
            Debug.Assert(!math.any(math.isnan(position)));
            Debug.Assert(!math.any(math.isinf(position)));
        }
    }
}