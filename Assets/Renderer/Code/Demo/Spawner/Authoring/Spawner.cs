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
				var builder = new BlobBuilder(Allocator.Temp);
				ref var spawnEntityArray = ref builder.ConstructRoot<SpawnEntityArray>();

				var count = spawnObjects.Length;
				var arrayBuilder = builder.Allocate(
					ref spawnEntityArray.Value,
					count
				);
				
				for (int i = 0; i < count; i++)
				{
					arrayBuilder[i] = new SpawnEntity
					{
						Entity = GetEntity(spawnObjects[i].Prefab, TransformUsageFlags.None),
						Label = spawnObjects[i].Label,
					};
				}

				var spawnEntityArrayRef = builder.CreateBlobAssetReference<SpawnEntityArray>(Allocator.Persistent);
				builder.Dispose();

				AddComponent(entity, new SpawnerData { SpawnEntityArrayRef = spawnEntityArrayRef });
				AddBuffer<SpawnTriggerBuffer>(entity);
			}
		}
	}
}