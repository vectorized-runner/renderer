using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using FrustumPlanes = Renderer.UnityPackages.FrustumPlanes;

namespace Renderer
{
	[BurstCompile]
	public struct ChunkCullingJob : IJobChunk
	{
		[ReadOnly] public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;
		[ReadOnly] public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;
		[ReadOnly] public NativeArray<FrustumPlanes.PlanePacket4> PlanePackets;

		public ComponentTypeHandle<ChunkCullResult> ChunkCullResultHandle;

		private void AssertValid(AABB aabb)
		{
			Debug.Assert(!math.any(math.isnan(aabb.Min)));
			Debug.Assert(!math.any(math.isinf(aabb.Min)));
			Debug.Assert(!math.any(math.isnan(aabb.Max)));
			Debug.Assert(!math.any(math.isinf(aabb.Max)));
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
			in v128 chunkEnabledMask)
		{
			// TODO: Add culling later. Currently Just add all Entities for Rendering.
			// TODO: If ChunkRenderBounds isn't visible, don't check the Entities for visibility

			// All entities have different RenderMeshIndex value
			var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
			var cullResult = new ChunkCullResult();

			var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			while (enumerator.NextEntityIndex(out var entityIndex))
			{
				var aabb = worldRenderBoundsArray[entityIndex].AABB;
				var intersectResult = FrustumPlanes.Intersect2(PlanePackets, aabb);
				var isVisible = intersectResult != FrustumPlanes.IntersectResult.Out;
				// TODO: Remove Lower/Upper branch here. Could inline ChunkEntityEnumerator here
				var lower = entityIndex < 64;

				if (lower)
					cullResult.Lower.SetBits(entityIndex, isVisible);
				else
					cullResult.Upper.SetBits(64 - entityIndex, isVisible);
			}
			
			chunk.SetChunkComponentData(ref ChunkCullResultHandle, cullResult);
		}
	}
}