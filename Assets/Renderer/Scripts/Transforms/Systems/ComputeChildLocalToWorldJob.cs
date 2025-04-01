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
	public struct ComputeChildLocalToWorldJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<Child> ChildBufferHandle;

		[ReadOnly]
		public ComponentLookup<LocalTransform> LocalTransformLookup;

		[ReadOnly]
		public BufferLookup<Child> ChildLookup;

		[ReadOnly]
		public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

		[NativeDisableContainerSafetyRestriction]
		public ComponentLookup<LocalToWorld> LocalToWorldLookup;

		public uint LastSystemVersion;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			Debug.Assert(!useEnabledMask);

			var childBufferArray = chunk.GetBufferAccessor(ref ChildBufferHandle);
			var localToWorldArray = chunk.GetNativeArray(ref LocalToWorldHandle);
			var entityCount = chunk.Count;

			for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
			{
				var localToWorld = localToWorldArray[entityIndex];
				var childBuffer = childBufferArray[entityIndex];
				var childCount = childBuffer.Length;
				var updateTransform = chunk.DidChange(ref ChildBufferHandle, LastSystemVersion) ||
				                      chunk.DidChange(ref LocalToWorldHandle, LastSystemVersion);

				for (int childIndex = 0; childIndex < childCount; childIndex++)
				{
					var childEntity = childBuffer[childIndex].Value;
					ComputeChildWorldMatrix(localToWorld.Value, childEntity, updateTransform);
				}
			}
		}

		private void ComputeChildWorldMatrix(float4x4 parentLocalToWorld, Entity entity, bool updateTransform)
		{
			var childLocalTransform = LocalTransformLookup[entity];

			// The Transform needs to be updated if
			// 1. If the root transform has child component changed, then all children needs to update.
			// 2. If parent transform is changed, then all children definitely needs to update.
			// 3. If the LocalToWorld type handle is updated, then that children (and all of its children recursively) needs to update. 
			updateTransform = updateTransform || LocalTransformLookup.DidChange(entity, LastSystemVersion);
			float4x4 localToWorld;

			if (updateTransform)
			{
				var localToParent = float4x4.TRS(childLocalTransform.Position, childLocalTransform.Rotation,
					childLocalTransform.Scale);
				localToWorld = math.mul(parentLocalToWorld, localToParent);
				LocalToWorldLookup[entity] = new LocalToWorld { Value = localToWorld };
			}
			else
			{
				localToWorld = LocalToWorldLookup[entity].Value;
			}

			if (ChildLookup.TryGetBuffer(entity, out var childBuffer))
			{
				var childCount = childBuffer.Length;

				for (int i = 0; i < childCount; i++)
				{
					var childEntity = childBuffer[i].Value;
					ComputeChildWorldMatrix(localToWorld, childEntity, updateTransform);
				}
			}
		}
	}
}