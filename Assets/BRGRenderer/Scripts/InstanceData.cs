using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BRGRenderer
{
    public struct InstanceData
    {
        public Mesh Mesh;
        public Material Material;
        public NativeList<PackedMatrix> ObjectToWorlds;
        public NativeList<PackedMatrix> WorldToObjects;
        public NativeList<float4> Colors;
        public NativeList<LocalTransform> Transforms;
    }
}