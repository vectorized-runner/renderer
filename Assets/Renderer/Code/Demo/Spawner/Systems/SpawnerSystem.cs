using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	public partial class SpawnerSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			var seed = (uint)Stopwatch.GetTimestamp();

			Entities.ForEach((ref DynamicBuffer<SpawnTriggerBuffer> spawnTriggerBuffer, in SpawnerData spawnerData) =>
				{
					var random = new Random(seed);

					for (int triggerIndex = 0; triggerIndex < spawnTriggerBuffer.Length; triggerIndex++)
					{
						var trigger = spawnTriggerBuffer[triggerIndex].Value;
						var amount = trigger.Amount;
						var label = trigger.Label;
						var prefab = spawnerData.PrefabByLabel[label];
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
	}
}