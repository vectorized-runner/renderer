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
			
			DebugDrawCameraFrustum();

			// Debug.Log($"ObjectBounds: {objectAABBs.Length} ChunkBounds: {chunkAABBs.Length}");
		}

		public void DebugDrawCameraFrustum()
		{
			var cam = RenderSettings.RenderCamera;
			var corners = new Vector3[4];
			cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane,
				Camera.MonoOrStereoscopicEye.Mono,
				corners);
			
			for (var i = 0; i < corners.Length; i++)
			{
				var worldSpaceCorner = cam.transform.TransformVector(corners[i]);
				Debug.DrawRay(cam.transform.position, worldSpaceCorner, Color.blue);
			}
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