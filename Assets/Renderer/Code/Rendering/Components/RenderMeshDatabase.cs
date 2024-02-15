using System;
using System.Collections.Generic;
using UnityEngine;

namespace Renderer
{
	[CreateAssetMenu]
	public class RenderMeshDatabase : ScriptableObject
	{
		// Not supposed to be edited at Runtime (Cache only checks if count is out-of-date)
		public List<RenderMesh> RenderMeshes;

		private static RenderMeshDatabase _instance;

		private readonly Dictionary<RenderMesh, RenderMeshIndex> _indexByMesh = new();

		private bool IsInitialized()
		{
			return RenderMeshes.Count == _indexByMesh.Count;
		}

		private void Awake()
		{
			InitializeLookupIfRequired();
		}

		private void InitializeLookupIfRequired()
		{
			if (IsInitialized())
				return;

			for (var index = 0; index < RenderMeshes.Count; index++)
			{
				var meshIndex = new RenderMeshIndex(index);
				var mesh = RenderMeshes[index];
				_indexByMesh[mesh] = meshIndex;
			}

			Debug.Assert(IsInitialized());
		}

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
			instance.InitializeLookupIfRequired();

			if (instance == null)
				throw new Exception("Couldn't load the instance");

			return instance;
		}


		public RenderMesh GetRenderMesh(RenderMeshIndex index)
		{
			Debug.Assert(IsInitialized());

			if (!TryGetRenderMesh(index, out var renderMesh))
				throw new Exception($"Couldn't find RenderMesh for index '{index}'");

			return renderMesh;
		}

		private bool TryGetRenderMesh(RenderMeshIndex index, out RenderMesh renderMesh)
		{
			if (index.Value >= RenderMeshes.Count)
			{
				renderMesh = default;
				return false;
			}

			renderMesh = RenderMeshes[index.Value];
			return true;
		}

		// TODO-Renderer: Check this method again, wtf?
		public RenderMeshIndex RegisterRenderMesh(RenderMesh renderMesh)
		{
			// This assertion fails
			// Debug.Assert(IsCacheInitialized());
			// This method is only used at bake-time, so lazy initialization should be ok (can't think of a better idea atm)
			InitializeLookupIfRequired();

			if (_indexByMesh.TryGetValue(renderMesh, out var cachedIndex))
			{
				return cachedIndex;
			}

			var newIndex = new RenderMeshIndex(RenderMeshes.Count);
			RenderMeshes.Add(renderMesh);
			_indexByMesh.Add(renderMesh, newIndex);

			return newIndex;
		}
	}
}