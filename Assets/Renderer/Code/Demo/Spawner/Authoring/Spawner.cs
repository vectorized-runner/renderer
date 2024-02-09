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
				// Prevent Unity from adding Transform components
				// https://forum.unity.com/threads/unwanted-transforms-are-required-in-1-0-baking.1392481/
				AddTransformUsageFlags(TransformUsageFlags.ManualOverride);
				
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
							Entity = GetEntity(spawnObjects[i].Prefab, TransformUsageFlags.ManualOverride),
							Label = spawnObjects[i].Label,
						}
					});
				}

				AddBuffer<SpawnTriggerElement>(entity);
			}
		}
	}
}