using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	[BurstCompile]
	public struct ChunkCullingJob : IJobChunk
	{
		[ReadOnly] public ComponentTypeHandle<WorldRenderBounds> WorldRenderBoundsHandle;
		[ReadOnly] public ComponentTypeHandle<ChunkWorldRenderBounds> ChunkWorldRenderBoundsHandle;
		[ReadOnly] public NativeArray<Plane> FrustumPlanes;

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
			// TODO: Rethink the culling logic here (partial, full-in, full-out)
			// TODO: If ChunkRenderBounds isn't visible, don't check the Entities for visibility

			// All entities have different RenderMeshIndex value
			var worldRenderBoundsArray = chunk.GetNativeArray(ref WorldRenderBoundsHandle);
			var cullResult = new ChunkCullResult();

			var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
			while (enumerator.NextEntityIndex(out var entityIndex))
			{
				var aabb = worldRenderBoundsArray[entityIndex].AABB;
				var isVisible = IsVisibleByCameraFrustum(FrustumPlanes, aabb);
				// TODO: Remove Lower/Upper branch here. Could inline ChunkEntityEnumerator here
				var lower = entityIndex < 64;

				if (lower)
					cullResult.Lower.SetBits(entityIndex, isVisible);
				else
					cullResult.Upper.SetBits(entityIndex, isVisible);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetPlaneSignedDistanceToPoint(Plane plane, float3 point)
		{
			// From Unity's plane implementation
			// The value returned is positive if the point is on the side of the plane into which the plane's normal is facing, and negative otherwise.
			// public float GetDistanceToPoint(Vector3 point) => Vector3.Dot(this.m_Normal, point) + this.m_Distance;
			return math.dot(plane.normal, point) + plane.distance;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsVisibleByCameraFrustum(NativeArray<Plane> frustumPlanes6, AABB aabb)
		{
			return
				IsOnForwardOrOnPlane(frustumPlanes6[0], aabb) &&
				IsOnForwardOrOnPlane(frustumPlanes6[1], aabb) &&
				IsOnForwardOrOnPlane(frustumPlanes6[2], aabb) &&
				IsOnForwardOrOnPlane(frustumPlanes6[3], aabb) &&
				IsOnForwardOrOnPlane(frustumPlanes6[4], aabb) &&
				IsOnForwardOrOnPlane(frustumPlanes6[5], aabb);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsOnForwardOrOnPlane(Plane plane, AABB aabb)
		{
			var normalDotExtents = math.dot(aabb.Extents, math.abs(plane.normal));
			var planeDistanceToCenter = GetPlaneSignedDistanceToPoint(plane, aabb.Center);
			return planeDistanceToCenter >= -normalDotExtents;
		}
	}
}