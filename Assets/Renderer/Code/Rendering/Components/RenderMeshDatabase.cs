using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Renderer
{
	[Serializable]
	public struct MeshAABBPair
	{
		public Mesh Mesh;
		public AABB AABB;
	}

	[CreateAssetMenu]
	public class RenderMeshDatabase : ScriptableObject
	{
		// These lists are supposed to be built at Editor-time.
		[SerializeField]
		private List<RenderMesh> RenderMeshes;

		[SerializeField]
		private MeshAABBPair[] MeshAABBPairs;

		// Indexed by RenderMeshIndex
		public NativeArray<AABB> MeshAABBs;

		public int MeshCount => RenderMeshes.Count;

		private static RenderMeshDatabase _instance;

		private readonly Dictionary<RenderMesh, RenderMeshIndex> _indexByMesh = new();

#if UNITY_EDITOR
		[Button]
		private static void SerializeMeshAABBs()
		{
			var allMeshes = AssetDatabase.LoadAllAssetsAtPath("Assets").OfType<Mesh>().ToArray();
			var meshCount = allMeshes.Length;
			if (meshCount == 0)
				return;

			var allMeshPaths = allMeshes.Select(AssetDatabase.GetAssetPath);

			foreach (var meshPath in allMeshPaths)
			{
				var meshImporter = AssetImporter.GetAtPath(meshPath) as ModelImporter;
				meshImporter.isReadable = true;
				meshImporter.SaveAndReimport();
			}

			var meshAABBPairs = new MeshAABBPair[meshCount];

			using var allMeshData = Mesh.AcquireReadOnlyMeshData(allMeshes);

			for (int i = 0; i < meshCount; i++)
			{
				meshAABBPairs[i] = new MeshAABBPair
				{
					Mesh = allMeshes[i],
					AABB = RenderMath.ComputeLocalAABB(allMeshData[i])
				};
			}

			foreach (var meshPath in allMeshPaths)
			{
				var meshImporter = AssetImporter.GetAtPath(meshPath) as ModelImporter;
				meshImporter.isReadable = false;
				meshImporter.SaveAndReimport();
			}

			_instance.MeshAABBPairs = meshAABBPairs;
			EditorUtility.SetDirty(_instance);
			AssetDatabase.SaveAssets();
		}
#endif

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