using UnityEngine;

namespace BRGRenderer
{
    [CreateAssetMenu]
    public class BRGConfig : ScriptableObject
    {
        public Mesh[] Meshes;
        public Material[] Materials;
        public int ObjectCount;
    }
}