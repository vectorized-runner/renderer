using System;
using UnityEngine;

namespace Renderer
{
    public readonly struct RenderMesh : IEquatable<RenderMesh>
    {
        public readonly Mesh Mesh;
        public readonly Material Material;
        public readonly int Layer;
        public readonly int SubMeshIndex;

        public RenderMesh(Mesh mesh, Material material, int layer, int subMeshIndex)
        {
            Mesh = mesh;
            Material = material;
            Layer = layer;
            SubMeshIndex = subMeshIndex;
        }

        public bool Equals(RenderMesh other)
        {
            return
                Mesh == other.Mesh &&
                Material == other.Material &&
                Layer == other.Layer &&
                SubMeshIndex == other.SubMeshIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is RenderMesh other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + Mesh.GetHashCode();
                hash = hash * 31 + Material.GetHashCode();
                hash = hash * 31 + Layer.GetHashCode();
                hash = hash * 31 + SubMeshIndex.GetHashCode();
                return hash;
            }
        }
    }
}