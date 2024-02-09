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
					var spawnTrigger = SystemAPI.GetSingletonEntity<SpawnEntityElement>();
					var buffer = EntityManager.GetBuffer<SpawnTriggerElement>(spawnTrigger);
					buffer.Add(new SpawnTriggerElement
					{
						Value = new SpawnTrigger
						{
							Amount = 100_000,
							Label = "Cube-Static"
						}
					});
				}
			}

			var seed = (uint)Stopwatch.GetTimestamp();

			Entities.ForEach((ref DynamicBuffer<SpawnTriggerElement> spawnTriggerBuffer,
					ref DynamicBuffer<SpawnEntityElement> spawnEntities) =>
				{
					var random = new Random(seed);

					for (int triggerIndex = 0; triggerIndex < spawnTriggerBuffer.Length; triggerIndex++)
					{
						var trigger = spawnTriggerBuffer[triggerIndex].Value;
						var amount = trigger.Amount;
						var label = trigger.Label;
						var prefab = FindEntity(ref spawnEntities, label);
						var spawnedEntities = EntityManager.Instantiate(prefab, amount, Allocator.Temp);
						var center = float3.zero;
						var distanceMin = 100.0f;
						var distanceMax = 1000.0f;
						var scaleMin = 0.5f;
						var scaleMax = 3.0f;

						for (int entityIndex = 0; entityIndex < spawnedEntities.Length; entityIndex++)
						{
							var distance = random.NextFloat(distanceMin, distanceMax);
							var scale = random.NextFloat(scaleMin, scaleMax);
							var position = center + random.NextFloat3Direction() * distance;
							var rotation = random.NextQuaternionRotation();
							var spawnedEntity = spawnedEntities[entityIndex];
							var matrix = float4x4.TRS(position, rotation, scale);
							EntityManager.SetComponentData(spawnedEntity, new LocalToWorld { Value = matrix });
							// EntityManager.SetComponentData(spawnedEntity, new Position { Value = position });
							// EntityManager.SetComponentData(spawnedEntity, new Rotation { Value = rotation });
							// EntityManager.SetComponentData(spawnedEntity, new Scale { Value = scale });
						}
					}

					spawnTriggerBuffer.Clear();
				})
				.WithStructuralChanges()
				.Run();
		}

		private Entity FindEntity(ref DynamicBuffer<SpawnEntityElement> entities, FixedString64Bytes label)
		{
			for (int i = 0; i < entities.Length; i++)
			{
				if (entities[i].Value.Label == label)
				{
					return entities[i].Value.Entity;
				}
			}

			throw new Exception($"Couldn't find entity by label {label}");
		}
	}
}