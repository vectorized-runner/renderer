using System;
using System.Collections.Generic;
using UnityEngine;

namespace Renderer
{
	// Consider making this class non-static (an actual system)
	public static class RenderMeshRegisterSystem
	{
		private static readonly List<RenderMesh> _renderMeshes = new();
		private static readonly Dictionary<RenderMesh, RenderMeshIndex> _renderMeshIndexByMesh = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
			_renderMeshes.Clear();
			_renderMeshIndexByMesh.Clear();
		}

		public static RenderMesh GetRenderMesh(RenderMeshIndex renderMeshIndex)
		{
			// index = 0, count needs to be 1, then count - 1 needs to be greater than index
			if (_renderMeshes.Count - 1 <= renderMeshIndex.Value) return _renderMeshes[renderMeshIndex.Value];

			throw new Exception(
				$"Couldn't find the RenderMesh for '{renderMeshIndex}'. RenderMeshCount: '{_renderMeshes.Count}'");
		}

		public static RenderMeshIndex GetRenderMeshIndex(RenderMesh renderMesh)
		{
			if (_renderMeshIndexByMesh.TryGetValue(renderMesh, out var index))
				return index;

			var renderMeshIndex = new RenderMeshIndex(_renderMeshes.Count);
			_renderMeshes.Add(renderMesh);
			_renderMeshIndexByMesh.Add(renderMesh, renderMeshIndex);
			return renderMeshIndex;
		}
	}
}