using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGRenderer
{
    public unsafe class BRGRenderer : MonoBehaviour
    {
        private BatchRendererGroup _brg;
        private readonly Dictionary<Material, BatchMaterialID> _materialIdByMaterial = new();
        private readonly Dictionary<Mesh, BatchMeshID> _meshIdByMesh = new();
        private GraphicsBuffer _graphicsBuffer;

        private const int _intSize = sizeof(int);
        private const int _floatSize = sizeof(float);
        private const int _unityMatrixSize = _floatSize * 16;
        private const int _brgMatrixSize = _floatSize * 12;
        private const int _float4Size = _floatSize * 4;

        // TODO-BRG: What's the math on these?
        private const int _bytesPerInstance = _brgMatrixSize * 2 + _float4Size;
        private const int _extraBytes = _unityMatrixSize * 2;
        private const int _instanceCount = 3;

        private BatchID _batchID;

        private void Start()
        {
            _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);

            var bufferCount = GetRequiredIntCountForInstanceData(_bytesPerInstance, _instanceCount, _extraBytes);
            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferCount, _intSize);

            Debug.Log($"GraphicsBuffer BufferCount is {bufferCount}");
        }

        private void OnDestroy()
        {
            _brg.Dispose();
            _graphicsBuffer.Release();
        }

        private void AddBatch()
        {
            // Place a zero matrix at the start of the instance data buffer, so loads from address 0 return zero.
            var zero = new Matrix4x4[1] { Matrix4x4.zero };

            // Create transform matrices for three example instances.
            var matrices = new Matrix4x4[_instanceCount]
            {
                Matrix4x4.Translate(new Vector3(-2, 0, 0)),
                Matrix4x4.Translate(new Vector3(0, 0, 0)),
                Matrix4x4.Translate(new Vector3(2, 0, 0)),
            };

            // Convert the transform matrices into the packed format that the shader expects.
            var objectToWorld = new PackedMatrix[_instanceCount]
            {
                new PackedMatrix(matrices[0]),
                new PackedMatrix(matrices[1]),
                new PackedMatrix(matrices[2]),
            };

            // Also create packed inverse matrices.
            var worldToObject = new PackedMatrix[_instanceCount]
            {
                new PackedMatrix(matrices[0].inverse),
                new PackedMatrix(matrices[1].inverse),
                new PackedMatrix(matrices[2].inverse),
            };

            // Make all instances have unique colors.
            var colors = new Vector4[_instanceCount]
            {
                new Vector4(1, 0, 0, 1),
                new Vector4(0, 1, 0, 1),
                new Vector4(0, 0, 1, 1),
            };

            // In this simple example, the instance data is placed into the buffer like this:
            // Offset | Description
            //      0 | 64 bytes of zeroes, so loads from address 0 return zeroes
            //     64 | 32 uninitialized bytes to make working with SetData easier, otherwise unnecessary
            //     96 | unity_ObjectToWorld, three packed float3x4 matrices
            //    240 | unity_WorldToObject, three packed float3x4 matrices
            //    384 | _BaseColor, three float4s

            // Calculates start addresses for the different instanced properties. unity_ObjectToWorld starts
            // at address 96 instead of 64, because the computeBufferStartIndex parameter of SetData
            // is expressed as source array elements, so it is easier to work in multiples of sizeof(PackedMatrix).
            uint byteAddressObjectToWorld = _unityMatrixSize * 2;
            uint byteAddressWorldToObject = byteAddressObjectToWorld + _unityMatrixSize * _instanceCount;
            uint byteAddressColor = byteAddressWorldToObject + _unityMatrixSize * _instanceCount;

            // Upload the instance data to the GraphicsBuffer so the shader can load them.
            _graphicsBuffer.SetData(zero, 0, 0, 1);
            _graphicsBuffer.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / _brgMatrixSize),
                objectToWorld.Length);
            _graphicsBuffer.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / _brgMatrixSize),
                worldToObject.Length);
            _graphicsBuffer.SetData(colors, 0, (int)(byteAddressColor / _float4Size), colors.Length);

            // Set up metadata values to point to the instance data. Set the most significant bit 0x80000000 in each
            // which instructs the shader that the data is an array with one value per instance, indexed by the instance index.
            // Any metadata values that the shader uses that are not set here will be 0. When a value of 0 is used with
            // UNITY_ACCESS_DOTS_INSTANCED_PROP (i.e. without a default), the shader interprets the
            // 0x00000000 metadata value and loads from the start of the buffer. The start of the buffer is
            // a zero matrix so this sort of load is guaranteed to return zero, which is a reasonable default value.
            var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
            metadata[0] = new MetadataValue
                { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
            metadata[1] = new MetadataValue
                { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };
            metadata[2] = new MetadataValue
                { NameID = Shader.PropertyToID("_BaseColor"), Value = 0x80000000 | byteAddressColor, };

            // Finally, create a batch for the instances and make the batch use the GraphicsBuffer with the
            // instance data as well as the metadata values that specify where the properties are.
            _batchID = _brg.AddBatch(metadata, _graphicsBuffer.bufferHandle);
        }

        // Raw buffers are allocated in ints. This is a utility method that calculates the required number of ints for the data.
        private static int GetRequiredIntCountForInstanceData(int bytesPerInstance, int instanceCount, int extraBytes)
        {
            // Round byte counts to int multiples
            var totalBytes = RoundUpToBase(bytesPerInstance, _intSize) * instanceCount +
                             RoundUpToBase(extraBytes, _intSize);
            return totalBytes / _intSize;
        }

        // 8 -> 8, 9 -> 12, 10 -> 12, 11 -> 12, 12 -> 12, 13 -> 16, etc...
        private static int RoundUpToBase(int num, int baseNumber)
        {
            return ((num + baseNumber - 1) / baseNumber) * baseNumber;
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