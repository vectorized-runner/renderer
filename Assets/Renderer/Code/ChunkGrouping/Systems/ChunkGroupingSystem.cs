using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	/// <summary>
	/// Why this system is required:
	/// Normally Entities would have good spatial distribution with their Scene Shared Component
	/// But for our Demos we only have one SubScene, and entities end up having very big ChunkRenderBounds
	/// This helps us Force the Spatial Grouping
	/// </summary>
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial class ChunkGroupingSystem : SystemBase
	{
		private EntityQuery _query;

		protected override void OnCreate()
		{
			_query = GetEntityQuery(typeof(SpatialGroupIndex));
		}

		protected override void OnStartRunning()
		{
			Debug.Log("Chunk Grouping System is Running. Press C Key to Trigger it.");
		}

		protected override void OnUpdate()
		{
			if (!Input.GetKeyDown(KeyCode.C))
				return;

			EntityManager.RemoveComponent<SpatialGroupIndex>(_query);

			var uniqueValues = new NativeHashSet<int>(64, Allocator.Temp);

			Entities.ForEach((Entity entity, in LocalToWorld localToWorld) =>
				{
					const int maxAbsPosition = 100_000;
					const int groupSize = 1_000;
					const int multiplier = 2 * maxAbsPosition / groupSize;
					var position = localToWorld.Position;

					if (math.any(math.abs(position) > maxAbsPosition))
					{
						throw new ArgumentException($"Position out of bounds: {position}");
					}

					// Remap (-max, max) to (0, 2 * max) for better understanding
					var remapPosition = position + maxAbsPosition;
					var ids = (int3)math.floor(remapPosition / groupSize);
					var id = ids.x + ids.y * multiplier + ids.z * multiplier * multiplier;
					EntityManager.AddSharedComponent(entity, new SpatialGroupIndex { Value = id });

					uniqueValues.Add(id);
				})
				.WithStructuralChanges()
				.Run();

			Debug.Log("Chunk Grouping System Run.");
			Debug.Log($"Unique Generated Id Count: {uniqueValues.Count}");
		}
	}
}