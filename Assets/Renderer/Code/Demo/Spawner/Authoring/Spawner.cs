using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	public class Spawner : MonoBehaviour
	{
		[Serializable]
		public struct SpawnObject
		{
			public GameObject Prefab;
			public string Label;
		}

		public SpawnObject[] SpawnObjects;

		public class SpawnerBaker : Baker<Spawner>
		{
			public override void Bake(Spawner authoring)
			{
				var entity = GetEntity(TransformUsageFlags.None);
				var spawnObjects = authoring.SpawnObjects;
				var spawnObjectCount = spawnObjects.Length;
				// TODO: Dispose this Hashmap to not leak memory
				var hashMap = new UnsafeHashMap<FixedString64Bytes, Entity>(32, Allocator.Persistent);

				for (int i = 0; i < spawnObjectCount; i++)
				{
					hashMap.Add(new FixedString64Bytes(spawnObjects[i].Label),
						GetEntity(spawnObjects[i].Prefab, TransformUsageFlags.None));
				}

				AddComponent(entity, new SpawnerData { PrefabByLabel = hashMap });
			}
		}
	}
}