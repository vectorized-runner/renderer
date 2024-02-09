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
				var count = spawnObjects.Length;

				var buffer = AddBuffer<SpawnEntityElement>(entity);
				
				for (int i = 0; i < count; i++)
				{
					buffer.Add(new SpawnEntityElement
					{
						Value = new SpawnEntity
						{
							Entity = GetEntity(spawnObjects[i].Prefab, TransformUsageFlags.None),
							Label = spawnObjects[i].Label,
						}
					});
				}

				AddBuffer<SpawnTriggerElement>(entity);
			}
		}
	}
}