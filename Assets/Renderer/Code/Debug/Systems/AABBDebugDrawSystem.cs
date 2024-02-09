using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderDebugGroup))]
	public partial class AABBDebugDrawSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			if (!RenderSettings.DebugMode)
				return;

			var objectAABBs = new NativeList<AABB>(Allocator.Temp);

			Entities.ForEach((in WorldRenderBounds worldRenderBounds) => { objectAABBs.Add(worldRenderBounds.AABB); })
				.Run();

			var query = GetEntityQuery(ComponentType.ChunkComponent<ChunkWorldRenderBounds>());
			var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
			var chunkAABBs = new NativeArray<AABB>(chunks.Length, Allocator.Temp);

			for (var index = 0; index < chunks.Length; index++)
			{
				var chunk = chunks[index];
				chunkAABBs[index] = EntityManager.GetChunkComponentData<ChunkWorldRenderBounds>(chunk).AABB;
			}

			foreach (var aabb in objectAABBs)
			{
				DebugDrawAABB(aabb, Color.cyan);
			}

			foreach (var aabb in chunkAABBs)
			{
				DebugDrawAABB(aabb, Color.green);
			}
			
			DebugDrawCameraFrustum(Color.yellow);

			// Debug.Log($"ObjectBounds: {objectAABBs.Length} ChunkBounds: {chunkAABBs.Length}");
		}

		public void DebugDrawCameraFrustum(Color color)
		{
			var cam = RenderSettings.RenderCamera;
			var farClipCorners = new Vector3[4];
			var nearClipCorners = new Vector3[4];
			cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farClipCorners);
			cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearClipCorners);
			
			for (var i = 0; i < farClipCorners.Length; i++)
			{
				farClipCorners[i] = cam.transform.TransformPoint(farClipCorners[i]);
			}
			for (var i = 0; i < nearClipCorners.Length; i++)
			{
				nearClipCorners[i] = cam.transform.TransformPoint(nearClipCorners[i]);
			}
			
			Debug.DrawLine(nearClipCorners[0], nearClipCorners[1], color);
			Debug.DrawLine(nearClipCorners[1], nearClipCorners[2], color);
			Debug.DrawLine(nearClipCorners[2], nearClipCorners[3], color);
			Debug.DrawLine(nearClipCorners[3], nearClipCorners[0], color);
			
			Debug.DrawLine(farClipCorners[0], farClipCorners[1], color);
			Debug.DrawLine(farClipCorners[1], farClipCorners[2], color);
			Debug.DrawLine(farClipCorners[2], farClipCorners[3], color);
			Debug.DrawLine(farClipCorners[3], farClipCorners[0], color);
			
			Debug.DrawLine(nearClipCorners[0], farClipCorners[0], color);
			Debug.DrawLine(nearClipCorners[1], farClipCorners[1], color);
			Debug.DrawLine(nearClipCorners[2], farClipCorners[2], color);
			Debug.DrawLine(nearClipCorners[3], farClipCorners[3], color);
		}

		private void DebugDrawAABB(AABB aabb, Color color)
		{
			var center = aabb.Center;
			var extents = aabb.Extents;
			var ex = extents.y;
			var ey = extents.y;
			var ez = extents.z;
			var p0 = center + new float3(-ex, -ey, -ez);
			var p1 = center + new float3(ex, -ey, -ez);
			var p2 = center + new float3(ex, ey, -ez);
			var p3 = center + new float3(-ex, ey, -ez);
			var p4 = center + new float3(-ex, ey, ez);
			var p5 = center + new float3(ex, ey, ez);
			var p6 = center + new float3(ex, -ey, ez);
			var p7 = center + new float3(-ex, -ey, ez);

			Debug.DrawLine(p0, p1, color);
			Debug.DrawLine(p0, p3, color);
			Debug.DrawLine(p2, p3, color);
			Debug.DrawLine(p1, p2, color);
			Debug.DrawLine(p4, p5, color);
			Debug.DrawLine(p4, p7, color);
			Debug.DrawLine(p5, p6, color);
			Debug.DrawLine(p6, p7, color);
			Debug.DrawLine(p3, p4, color);
			Debug.DrawLine(p0, p7, color);
			Debug.DrawLine(p2, p5, color);
			Debug.DrawLine(p1, p6, color);
		}
	}
}