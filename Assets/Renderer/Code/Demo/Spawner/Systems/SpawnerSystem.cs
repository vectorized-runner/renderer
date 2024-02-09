using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Renderer
{
	public partial class SpawnerSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			// Fast way to trigger Spawner
			{
				if (Input.GetKeyDown(KeyCode.Space))
				{
					var spawnTrigger = SystemAPI.GetSingletonEntity<SpawnerData>();
					var buffer = EntityManager.GetBuffer<SpawnTriggerBuffer>(spawnTrigger);
					buffer.Add(new SpawnTriggerBuffer
					{
						Value = new SpawnTrigger
						{
							Amount = 100_000,
							Label = "Cube"
						}
					});
				}
			}

			var seed = (uint)Stopwatch.GetTimestamp();

			Entities.ForEach((ref DynamicBuffer<SpawnTriggerBuffer> spawnTriggerBuffer, in SpawnerData spawnerData) =>
				{
					var random = new Random(seed);

					for (int triggerIndex = 0; triggerIndex < spawnTriggerBuffer.Length; triggerIndex++)
					{
						var trigger = spawnTriggerBuffer[triggerIndex].Value;
						var amount = trigger.Amount;
						var label = trigger.Label;
						ref var entityArray = ref spawnerData.SpawnEntityArrayRef.Value.Value;
						var prefab = FindEntity(ref entityArray, label);
						var spawnedEntities = EntityManager.Instantiate(prefab, amount, Allocator.Temp);

						var center = float3.zero;
						var radius = 100.0f;

						for (int entityIndex = 0; entityIndex < spawnedEntities.Length; entityIndex++)
						{
							var position = center + random.NextFloat3Direction() * radius;
							var rotation = random.NextQuaternionRotation();
							var scale = 1.0f;
							var spawnedEntity = spawnedEntities[entityIndex];
							EntityManager.SetComponentData(spawnedEntity, new Position { Value = position });
							EntityManager.SetComponentData(spawnedEntity, new Rotation { Value = rotation });
							EntityManager.SetComponentData(spawnedEntity, new Scale { Value = scale });
						}
					}

					spawnTriggerBuffer.Clear();
				})
				.WithStructuralChanges()
				.Run();
		}

		private Entity FindEntity(ref BlobArray<SpawnEntity> entities, FixedString64Bytes label)
		{
			for (int i = 0; i < entities.Length; i++)
			{
				if (entities[i].Label == label)
				{
					return entities[i].Entity;
				}
			}

			throw new Exception($"Couldn't find entity by label {label}");
		}
	}
}