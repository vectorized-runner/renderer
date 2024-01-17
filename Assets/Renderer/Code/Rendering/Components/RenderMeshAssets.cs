using System;
using System.Collections.Generic;
using UnityEngine;

namespace Renderer
{
	[CreateAssetMenu]
	public class RenderMeshAssets : ScriptableObject
	{
		public List<RenderMesh> RenderMeshes;

		private readonly Dictionary<RenderMeshIndex, RenderMesh> _indexCache = new();
		private readonly Dictionary<RenderMesh, RenderMeshIndex> _meshCache = new();

		public RenderMesh GetRenderMesh(RenderMeshIndex index)
		{
			if (!TryGetRenderMesh(index, out var renderMesh))
				throw new Exception($"Couldn't find RenderMesh for index '{index}'");

			return renderMesh;
		}

		public bool TryGetRenderMesh(RenderMeshIndex index, out RenderMesh renderMesh)
		{
			if (_indexCache.TryGetValue(index, out var cached))
			{
				renderMesh = cached;
				return true;
			}

			if (index.Value >= RenderMeshes.Count)
			{
				renderMesh = default;
				return false;
			}

			renderMesh = RenderMeshes[index.Value];
			_indexCache.Add(index, renderMesh);
			return true;
		}

		public RenderMeshIndex RegisterMeshAndGetIndex(RenderMesh renderMesh)
		{
			if (_meshCache.TryGetValue(renderMesh, out var cached))
			{
				return cached;
			}

			RenderMeshIndex result;

			for (var index = 0; index < RenderMeshes.Count; index++)
			{
				if (RenderMeshes[index].Equals(renderMesh))
				{
					result = new RenderMeshIndex(index);
					_meshCache.Add(renderMesh, result);
					return result;
				}
			}

			var previousCount = RenderMeshes.Count;
			RenderMeshes.Add(renderMesh);
			result = new RenderMeshIndex(previousCount);
			_meshCache.Add(renderMesh, result);
			return result;
		}
	}
}