using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGRenderer
{
    public class SimpleBRGExample : MonoBehaviour
    {
        public Mesh mesh;
        public Material material;

        private BatchRendererGroup _brg;

        private GraphicsBuffer _graphicsBuffer;
        private BatchID _batchID;
        private BatchMeshID _meshID;
        private BatchMaterialID _materialID;

        // Some helper constants to make calculations more convenient.
        private const int _unityMatrixSize = sizeof(float) * 4 * 4;
        private const int _brgMatrixSize = sizeof(float) * 4 * 3;
        private const int _float4Size = sizeof(float) * 4;
        private const int _intSize = sizeof(int);
        private const int _sizePerRenderObject = (_brgMatrixSize * 2) + _float4Size;
        private const int _extraBytes = _unityMatrixSize * 2;
        private const int _renderObjectCount = 3;

        private void Start()
        {
            _brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            _meshID = _brg.RegisterMesh(mesh);
            _materialID = _brg.RegisterMaterial(material);

            AllocateInstanceDateBuffer();
            PopulateInstanceDataBuffer();
        }

        private void OnDestroy()
        {
            _brg.Dispose();
            _graphicsBuffer.Release();
        }

        private void AllocateInstanceDateBuffer()
        {
            var intCount = BufferCountForInstances(_sizePerRenderObject, _renderObjectCount, _extraBytes);
            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, intCount, _intSize);
        }

        private void PopulateInstanceDataBuffer()
        {
            // Place a zero matrix at the start of the instance data buffer, so loads from address 0 return zero.
            var zero = new[] { Matrix4x4.zero };

            // Create transform matrices for three example instances.
            var matrices = new[]
            {
                Matrix4x4.Translate(new Vector3(-2, 0, 0)),
                Matrix4x4.Translate(new Vector3(0, 0, 0)),
                Matrix4x4.Translate(new Vector3(2, 0, 0)),
            };

            // Convert the transform matrices into the packed format that shaders expects.
            var objectToWorld = new[]
            {
                new PackedMatrix(matrices[0]),
                new PackedMatrix(matrices[1]),
                new PackedMatrix(matrices[2]),
            };

            // Also create packed inverse matrices.
            var worldToObject = new[]
            {
                new PackedMatrix(matrices[0].inverse),
                new PackedMatrix(matrices[1].inverse),
                new PackedMatrix(matrices[2].inverse),
            };

            // Make all instances have unique colors.
            var colors = new[]
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

            // Calculates start addresses for the different instanced properties. unity_ObjectToWorld starts at 
            // address 96 instead of 64 which means 32 bits are left uninitialized. This is because the 
            // computeBufferStartIndex parameter requires the start offset to be divisible by the size of the source
            // array element type. In this case, it's the size of PackedMatrix, which is 48.
            uint byteAddressObjectToWorld = _brgMatrixSize * 2;
            uint byteAddressWorldToObject = byteAddressObjectToWorld + _brgMatrixSize * _renderObjectCount;
            uint byteAddressColor = byteAddressWorldToObject + _brgMatrixSize * _renderObjectCount;

            // Upload the instance data to the GraphicsBuffer so the shader can load them.
            _graphicsBuffer.SetData(zero, 0, 0, 1);
            _graphicsBuffer.SetData(objectToWorld, 0, (int)(byteAddressObjectToWorld / _brgMatrixSize),
                objectToWorld.Length);
            _graphicsBuffer.SetData(worldToObject, 0, (int)(byteAddressWorldToObject / _brgMatrixSize),
                worldToObject.Length);
            _graphicsBuffer.SetData(colors, 0, (int)(byteAddressColor / _float4Size), colors.Length);

            // Set up metadata values to point to the instance data. Set the most significant bit 0x80000000 in each
            // which instructs the shader that the data is an array with one value per instance, indexed by the instance index.
            // Any metadata values that the shader uses and not set here will be zero. When such a value is used with
            // UNITY_ACCESS_DOTS_INSTANCED_PROP (i.e. without a default), the shader interprets the
            // 0x00000000 metadata value and loads from the start of the buffer. The start of the buffer which is
            // is a zero matrix so this sort of load is guaranteed to return zero, which is a reasonable default value.
            var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
            metadata[0] = new MetadataValue
                { NameID = Shader.PropertyToID("unity_ObjectToWorld"), Value = 0x80000000 | byteAddressObjectToWorld, };
            metadata[1] = new MetadataValue
                { NameID = Shader.PropertyToID("unity_WorldToObject"), Value = 0x80000000 | byteAddressWorldToObject, };
            metadata[2] = new MetadataValue
                { NameID = Shader.PropertyToID("_BaseColor"), Value = 0x80000000 | byteAddressColor, };

            // Finally, create a batch for the instances, and make the batch use the GraphicsBuffer with the
            // instance data, as well as the metadata values that specify where the properties are. 
            _batchID = _brg.AddBatch(metadata, _graphicsBuffer.bufferHandle);
        }

        // Raw buffers are allocated in ints. This is a utility method that calculates
        // the required number of ints for the data.
        int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
        {
            // Round byte counts to int multiples
            bytesPerInstance = (bytesPerInstance + _intSize - 1) / _intSize * _intSize;
            extraBytes = (extraBytes + _intSize - 1) / _intSize * _intSize;
            int totalBytes = bytesPerInstance * numInstances + extraBytes;
            return totalBytes / _intSize;
        }

        public unsafe JobHandle OnPerformCulling(
            BatchRendererGroup rendererGroup,
            BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput,
            IntPtr userContext)
        {
            // Acquire a pointer to the BatchCullingOutputDrawCommands struct so you can easily
            // modify it directly.
            Debug.Assert(cullingOutput.drawCommands.Length == 1);
            ref var output = ref cullingOutput.drawCommands.ElementAsRef(0);

            // Allocate memory for the output arrays. In a more complicated implementation, you would calculate
            // the amount of memory to allocate dynamically based on what is visible.
            // This example assumes that all of the instances are visible and thus allocates
            // memory for each of them. The necessary allocations are as follows:
            // - a single draw command (which draws kNumInstances instances)
            // - a single draw range (which covers our single draw command)
            // - kNumInstances visible instance indices.
            // You must always allocate the arrays using Allocator.TempJob.
            var batchDrawCommand = Util.Malloc<BatchDrawCommand>(1, Allocator.TempJob);
            // Configure the single draw command to draw kNumInstances instances
            // starting from offset 0 in the array, using the batch, material and mesh
            // IDs registered in the Start() method. It doesn't set any special flags.
            batchDrawCommand->visibleOffset = 0;
            batchDrawCommand->visibleCount = _renderObjectCount;
            batchDrawCommand->batchID = _batchID;
            batchDrawCommand->materialID = _materialID;
            batchDrawCommand->meshID = _meshID;
            batchDrawCommand->submeshIndex = 0;
            batchDrawCommand->splitVisibilityMask = 0xff;
            batchDrawCommand->flags = 0;
            batchDrawCommand->sortingPosition = 0;
            output.drawCommands = batchDrawCommand;

            const int drawRangeCount = 1;
            var batchDrawRange = Util.Malloc<BatchDrawRange>(drawRangeCount, Allocator.TempJob);;
            // Configure the single draw range to cover the single draw command which
            // is at offset 0.
            // drawCommands->drawRanges[0].drawCommandsType = BatchDrawCommandType.Direct;
            batchDrawRange->drawCommandsBegin = 0;
            batchDrawRange->drawCommandsCount = 1;
            // This example doesn't care about shadows or motion vectors, so it leaves everything
            // at the default zero values, except the renderingLayerMask which it sets to all ones
            // so Unity renders the instances regardless of mask settings.
            batchDrawRange->filterSettings = new BatchFilterSettings { renderingLayerMask = 0xffffffff, };
            output.drawRanges = batchDrawRange;
            output.drawRangeCount = drawRangeCount;
            
            output.visibleInstances = Util.Malloc<int>(_renderObjectCount, Allocator.TempJob);
            output.drawCommandPickingInstanceIDs = null;

            output.drawCommandCount = 1;
            output.visibleInstanceCount = _renderObjectCount;

            // This example doesn't use depth sorting, so it leaves instanceSortingPositions as null.
            output.instanceSortingPositions = null;
            output.instanceSortingPositionFloatCount = 0;
         
            // Finally, write the actual visible instance indices to the array. In a more complicated
            // implementation, this output would depend on what is visible, but this example
            // assumes that everything is visible.
            for (int i = 0; i < _renderObjectCount; ++i)
                output.visibleInstances[i] = i;

            // This simple example doesn't use jobs, so it returns an empty JobHandle.
            // Performance-sensitive applications are encouraged to use Burst jobs to implement
            // culling and draw command output. In this case, this function returns a
            // handle here that completes when the Burst jobs finish.
            return new JobHandle();
        }
    }
}