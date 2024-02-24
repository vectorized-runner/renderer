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
		
		// TODO: Learn about filter usage here. Could be a really good optimization.
		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
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
				
				for (int childIndex = 0; childIndex < childCount; childIndex++)
				{
					var childEntity = childBuffer[childIndex].Value;
					ComputeChildWorldMatrix(localToWorld.Value, childEntity);
				}
			}
		}

		private void ComputeChildWorldMatrix(float4x4 parentLocalToWorld, Entity entity)
		{
			var childLocalTransform = LocalTransformLookup[entity];
			var localToParent = float4x4.TRS(childLocalTransform.Position, childLocalTransform.Rotation, childLocalTransform.Scale);
			var localToWorld = math.mul(parentLocalToWorld, localToParent);

			LocalToWorldLookup[entity] = new LocalToWorld { Value = localToWorld };

			if (ChildLookup.TryGetBuffer(entity, out var childBuffer))
			{
				var childCount = childBuffer.Length;

				for (int i = 0; i < childCount; i++)
				{
					var childEntity = childBuffer[i].Value;
					ComputeChildWorldMatrix(localToWorld, childEntity);
				}
			}
		}
	}
}