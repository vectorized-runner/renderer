using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGRenderer
{
    public unsafe class BRGRenderer : MonoBehaviour
    {
        private BatchRendererGroup _brg;
        private Dictionary<Material, BatchMaterialID> _materialIdByMaterial = new();
        private Dictionary<Mesh, BatchMeshID> _meshIdByMesh = new();

        private const int _floatSize = sizeof(float);
        private const int _unityMatrixSize = _floatSize * 16;
        private const int _brgMatrixSize = _floatSize * 12;
        private const int _float4Size = _floatSize * 4;
        // TODO-BRG: What's the math on these?
        private const int _bytesPerInstance = _brgMatrixSize * 2 + _float4Size;
        private const int _extraBytes = _unityMatrixSize * 2;
        private const int _instanceCount = 3;
        
        private void Start()
        {
            _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
        }

        private void OnDestroy()
        {
            _brg.Dispose();
        }

        private void Update()
        {
        }

        private JobHandle OnPerformCulling(
            BatchRendererGroup rendererGroup,
            BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput,
            IntPtr userContext)
        {
            // This example doesn't use jobs, so it can return an empty JobHandle.
            // Performance-sensitive applications should use Burst jobs to implement
            // culling and draw command output. In this case, this function would return a
            // handle here that completes when the Burst jobs finish.
            return new JobHandle();
        }

        private BatchMaterialID RegisterMaterial(Material mat)
        {
            if (_materialIdByMaterial.TryGetValue(mat, out var id))
                return id;

            id = _brg.RegisterMaterial(mat);
            _materialIdByMaterial[mat] = id;
            return id;
        }

        private BatchMeshID RegisterMesh(Mesh mesh)
        {
            if (_meshIdByMesh.TryGetValue(mesh, out var id))
                return id;

            id = _brg.RegisterMesh(mesh);
            _meshIdByMesh[mesh] = id;
            return id;
        }
    }
}