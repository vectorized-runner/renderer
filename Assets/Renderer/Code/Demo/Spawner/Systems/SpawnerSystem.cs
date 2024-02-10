using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup), OrderFirst = true)]
	public partial class SpawnerSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			// Fast way to trigger Spawner
			if (!Input.GetKeyDown(KeyCode.Space))
				return;

			var spawnTrigger = SystemAPI.GetSingletonEntity<SpawnEntityElement>();
			EntityManager.AddComponentData(spawnTrigger, new SpawnTrigger
			{
				Amount = 100_000,
				Label = "Cube-Dynamic"
			});

			var seed = (uint)Stopwatch.GetTimestamp();

			Entities.ForEach((ref DynamicBuffer<SpawnEntityElement> spawnEntities, in SpawnTrigger spawnTrigger) =>
				{
					var random = new Random(seed);
					var amount = spawnTrigger.Amount;
					var label = spawnTrigger.Label;
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
						EntityManager.SetComponentData(spawnedEntity, new Position { Value = position });
						EntityManager.SetComponentData(spawnedEntity, new Rotation { Value = rotation });
						EntityManager.SetComponentData(spawnedEntity, new Scale { Value = scale });
					}

					EntityManager.AddComponent<MakeStatic>(spawnedEntities);
				})
				.WithStructuralChanges()
				.Run();

			EntityManager.RemoveComponent<SpawnTrigger>(spawnTrigger);
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