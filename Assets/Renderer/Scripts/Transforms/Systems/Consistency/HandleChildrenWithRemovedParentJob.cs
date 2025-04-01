using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	[BurstCompile]
	public unsafe struct HandleChildrenWithRemovedParentJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PreviousParent> PreviousParentHandle;

		[ReadOnly]
		public EntityTypeHandle EntityHandle;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public ComponentLookup<LocalToWorld> LocalToWorldLookup;

		public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

		public ComponentTypeHandle<LocalTransform> LocalTransformHandle;

		public NativeParallelMultiHashMap<Entity, Entity>.ParallelWriter ParentByRemovedChildren;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);

			var entityCount = chunk.Count;
			var previousParentArray = chunk.GetNativeArray(ref PreviousParentHandle);
			var chunkEntityArrayPtr = chunk.GetEntityDataPtrRO(EntityHandle);
			var localToWorldArray = chunk.GetNativeArray(ref LocalToWorldHandle);
			var localTransformArray = chunk.GetNativeArray(ref LocalTransformHandle);

			for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
			{
				var childEntity = chunkEntityArrayPtr[entityIndex];
				var parentEntity = previousParentArray[entityIndex].Value;
				var parentMatrix = LocalToWorldLookup[parentEntity].Value;
				var localTransform = localTransformArray[entityIndex];
				var localToParent =
					float4x4.TRS(localTransform.Position, localTransform.Rotation, localTransform.Scale);
				var newLocalToWorld = new LocalToWorld { Value = math.mul(parentMatrix, localToParent) };
				localToWorldArray[entityIndex] = newLocalToWorld;
				var newLocalTransform = new LocalTransform
				{
					Position = newLocalToWorld.Position,
					Rotation = newLocalToWorld.Rotation,
					Scale = newLocalToWorld.Scale
				};

				localTransformArray[entityIndex] = newLocalTransform;

				ParentByRemovedChildren.Add(parentEntity, childEntity);
			}
		}
	}
}