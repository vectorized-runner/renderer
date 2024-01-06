using System;
using System.Collections.Generic;
using UnityEngine;

namespace Renderer
{
	// Consider making this class non-static (an actual system)
	public static class RenderMeshRegisterSystem
	{
		// TODO: Ensure RenderMesh count doesn't surpass this.
		public const int MaxSupportedUniqueMeshCount = 1024;
		
		public static readonly List<RenderMesh> RenderMeshes = new();
		private static readonly Dictionary<RenderMesh, RenderMeshIndex> _renderMeshIndexByMesh = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			RenderMeshes.Clear();
			_renderMeshIndexByMesh.Clear();
		}

		public static RenderMesh GetRenderMesh(RenderMeshIndex renderMeshIndex)
		{
			// index = 0, count needs to be 1, then count - 1 needs to be greater than index
			if (RenderMeshes.Count - 1 <= renderMeshIndex.Value) return RenderMeshes[renderMeshIndex.Value];

			throw new Exception(
				$"Couldn't find the RenderMesh for '{renderMeshIndex}'. RenderMeshCount: '{RenderMeshes.Count}'");
		}

		public static RenderMeshIndex GetRenderMeshIndex(RenderMesh renderMesh)
		{
			if (_renderMeshIndexByMesh.TryGetValue(renderMesh, out var index))
				return index;

			var renderMeshIndex = new RenderMeshIndex(RenderMeshes.Count);
			RenderMeshes.Add(renderMesh);
			_renderMeshIndexByMesh.Add(renderMesh, renderMeshIndex);
			return renderMeshIndex;
		}
	}
}