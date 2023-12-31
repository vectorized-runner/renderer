using System.Collections.Generic;
using UnityEngine;

namespace Renderer
{
    // Consider making this class non-static (an actual system)
    public static class RenderMeshRegisterSystem
    {
        private static readonly Dictionary<RenderMesh, RenderMeshId> _renderMeshById = new();
        private static int _lastUsedId;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            _renderMeshById.Clear();
            _lastUsedId = -1;
        }

        public static RenderMeshId GetRenderMeshId(RenderMesh renderMesh)
        {
            if (_renderMeshById.TryGetValue(renderMesh, out var id))
                return id;

            var newId = ++_lastUsedId;
            return new RenderMeshId(newId);
        }
    }
}