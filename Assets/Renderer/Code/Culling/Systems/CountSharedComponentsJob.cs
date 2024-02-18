using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	[BurstCompile]
	public struct CountSharedComponentsJob : IJobChunk
	{
		public SharedComponentTypeHandle<RenderMesh> RenderMeshHandle;

		public NativeParallelHashSet<int> Counter;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			var sharedComponentIndex = chunk.GetSharedComponentIndex(RenderMeshHandle);
			Debug.Log($"SharedComponentIndexValue: {sharedComponentIndex}");
			Counter.Add(sharedComponentIndex);
		}
	}
}