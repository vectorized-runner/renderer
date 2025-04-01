using System;
using System.Diagnostics;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup), OrderFirst = true)]
	public partial class SpawnerSystem : SystemBase
	{
		private void LogAllComponents(Entity entity)
		{
			var name = EntityManager.GetName(entity);
			var types = EntityManager.GetComponentTypes(entity);

			var sb = new StringBuilder();
			sb.AppendLine($"Components on Entity '{name}'");

			foreach (var type in types)
			{
				sb.AppendLine(type.ToString());
			}

			Debug.Log(sb.ToString());
		}

		protected override void OnStartRunning()
		{
			// Only Runs in Demo Scenes
			var activeSceneName = SceneManager.GetActiveScene().name;
			if (!activeSceneName.Contains("Demo"))
				return;
			
			Debug.Log("Spawner System is Running. Press Space to Spawn Entities.");
		}

		protected override void OnUpdate()
		{
			// Only Runs in Demo Scenes
			var activeSceneName = SceneManager.GetActiveScene().name;
			if (!activeSceneName.Contains("Demo"))
				return;

			// Fast way to trigger Spawner
			if (!Input.GetKeyDown(KeyCode.Space))
				return;

			var spawnTrigger = SystemAPI.GetSingletonEntity<SpawnEntityElement>();
			EntityManager.AddComponentData(spawnTrigger, new SpawnTrigger
			{
				Amount = RenderSettings.Instance.SpawnCount,
				Label = "Cube-Static"
			});

			var seed = (uint)Stopwatch.GetTimestamp();

			Entities.ForEach((ref DynamicBuffer<SpawnEntityElement> spawnEntities, in SpawnTrigger spawnTrigger) =>
				{
					var random = new Random(seed);
					var amount = spawnTrigger.Amount;
					var label = spawnTrigger.Label;
					var (prefab, aabb) = FindEntity(ref spawnEntities, label);
					LogAllComponents(prefab);
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

						var linkedEntityGroup = EntityManager.GetBuffer<LinkedEntityGroup>(spawnedEntity);
						var count = linkedEntityGroup.Length;

						for (int i = 0; i < count; i++)
						{
							var linkedEntity = linkedEntityGroup[i].Value;

							if (!EntityManager.HasComponent<RenderObject>(linkedEntity))
							{
								continue;
							}

							if (EntityManager.HasComponent<Static>(linkedEntity))
							{
								var localToWorld = new LocalToWorld { Value = float4x4.TRS(position, rotation, scale) };
								var renderBounds = new RenderBounds { AABB = aabb };
								var worldRenderBounds = RenderMath.ComputeWorldRenderBounds(renderBounds, localToWorld);

								EntityManager.AddComponentData(linkedEntity, worldRenderBounds);
								EntityManager.SetComponentData(linkedEntity, localToWorld);
							}
							else
							{
								EntityManager.AddComponentData(linkedEntity, new LocalTransform
								{
									Position = position,
									Rotation = rotation,
									Scale = scale
								});
							}
						}

						// I don't recall why did I do this previously.
						// EntityManager.DestroyEntity(spawnedEntity);
					}

					Debug.Log($"Spawned '{label}' entity {amount} times.");
				})
				.WithStructuralChanges()
				.Run();


			EntityManager.RemoveComponent<SpawnTrigger>(spawnTrigger);
		}

		private (Entity, AABB) FindEntity(ref DynamicBuffer<SpawnEntityElement> entities, FixedString64Bytes label)
		{
			for (int i = 0; i < entities.Length; i++)
			{
				if (entities[i].Value.Label == label)
				{
					return (entities[i].Value.Entity, entities[i].Value.AABB);
				}
			}

			throw new Exception($"Couldn't find entity by label {label}");
		}
	}
}