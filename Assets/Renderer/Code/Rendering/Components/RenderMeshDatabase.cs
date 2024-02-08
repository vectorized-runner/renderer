using System;
using System.Collections.Generic;
using UnityEngine;

namespace Renderer
{
	[CreateAssetMenu]
	public class RenderMeshDatabase : ScriptableObject
	{
		public List<RenderMesh> RenderMeshes;

		private static RenderMeshDatabase _instance;

		private readonly Dictionary<RenderMeshIndex, RenderMesh> _meshByIndexCache = new();
		private readonly Dictionary<RenderMesh, RenderMeshIndex> _indexByMeshCache = new();

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


		public RenderMesh GetRenderMesh(RenderMeshIndex index)
		{
			if (!TryGetRenderMesh(index, out var renderMesh))
				throw new Exception($"Couldn't find RenderMesh for index '{index}'");

			return renderMesh;
		}

		public bool TryGetRenderMesh(RenderMeshIndex index, out RenderMesh renderMesh)
		{
			if (_meshByIndexCache.TryGetValue(index, out var cached))
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
			_meshByIndexCache.Add(index, renderMesh);
			return true;
		}

		// TODO: Check this method again, wtf?
		public RenderMeshIndex RegisterRenderMesh(RenderMesh renderMesh)
		{
			if (_indexByMeshCache.TryGetValue(renderMesh, out var cachedIndex))
			{
				return cachedIndex;
			}

			RenderMeshIndex result;

			for (var index = 0; index < RenderMeshes.Count; index++)
			{
				if (RenderMeshes[index].Equals(renderMesh))
				{
					result = new RenderMeshIndex(index);
					_indexByMeshCache.Add(renderMesh, result);
					return result;
				}
			}

			var previousCount = RenderMeshes.Count;
			RenderMeshes.Add(renderMesh);
			result = new RenderMeshIndex(previousCount);
			_indexByMeshCache.Add(renderMesh, result);
			return result;
		}
	}
}