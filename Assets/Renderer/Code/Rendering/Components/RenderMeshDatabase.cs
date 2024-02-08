using System;
using System.Collections.Generic;
using UnityEngine;

namespace Renderer
{
	[CreateAssetMenu]
	public class RenderMeshDatabase : ScriptableObject
	{
		public List<RenderMesh> RenderMeshes;

		private readonly Dictionary<RenderMeshIndex, RenderMesh> _indexCache = new();
		private readonly Dictionary<RenderMesh, RenderMeshIndex> _renderMeshIndexCache = new();

		public static RenderMeshDatabase Instance
		{
			get
			{
				if (_instance == null)
					_instance = LoadInstance();

				return _instance;
			}
		}

		private static RenderMeshDatabase LoadInstance()
		{
			var instance = Resources.Load<RenderMeshDatabase>("RenderMeshDatabase");
			if (instance == null)
				throw new Exception("Couldn't load the instance");

			return instance;
		}

		private static RenderMeshDatabase _instance;

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

		// TODO: Check this method again, wtf?
		public RenderMeshIndex RegisterRenderMesh(RenderMesh renderMesh)
		{
			if (_renderMeshIndexCache.TryGetValue(renderMesh, out var cachedIndex))
			{
				return cachedIndex;
			}

			RenderMeshIndex result;

			for (var index = 0; index < RenderMeshes.Count; index++)
			{
				if (RenderMeshes[index].Equals(renderMesh))
				{
					result = new RenderMeshIndex(index);
					_renderMeshIndexCache.Add(renderMesh, result);
					return result;
				}
			}

			var previousCount = RenderMeshes.Count;
			RenderMeshes.Add(renderMesh);
			result = new RenderMeshIndex(previousCount);
			_renderMeshIndexCache.Add(renderMesh, result);
			return result;
		}
	}
}