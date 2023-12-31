using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
    [CreateAssetMenu]
    public class RendererDemoConfig : ScriptableObject
    {
        public GameObject Prefab;
        public int SpawnCount;
        public float Radius;
        public float3 Center;
    }
}