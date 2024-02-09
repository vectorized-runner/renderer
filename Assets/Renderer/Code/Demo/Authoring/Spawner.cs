using System;
using Unity.Collections;
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
				var runtimeSpawnEntities = new FixedList4096Bytes<RuntimeSpawnEntity>();
				var spawnObjects = authoring.SpawnObjects;
				var spawnObjectCount = spawnObjects.Length;

				if (runtimeSpawnEntities.Capacity < spawnObjectCount)
					throw new Exception("Insufficient FixedList capacity.");

				for (int i = 0; i < spawnObjectCount; i++)
				{
					runtimeSpawnEntities.Add(new RuntimeSpawnEntity
					{
						Entity = GetEntity(spawnObjects[i].Prefab, TransformUsageFlags.None),
						Label = new FixedString64Bytes(spawnObjects[i].Label)
					});
				}

				AddComponent(entity, new SpawnerData { RuntimeSpawnEntities = runtimeSpawnEntities });
			}
		}
	}
}