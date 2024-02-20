using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderDebugGroup))]
	public partial class AABBDebugDrawSystem : SystemBase
	{
		NativeList<AABB> objectAABBs;

		protected override void OnCreate()
		{
			objectAABBs = new NativeList<AABB>(0, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			objectAABBs.Dispose();
		}

		public void DrawAABBs()
		{
			if (!RenderSettings.Instance.DebugMode)
				return;

			using (new ProfilerMarker("DebugDrawAABB").Auto())
			{
				if (RenderSettings.Instance.UseGLDraw)
				{
					GL.Color(Color.cyan);
					
					foreach (var aabb in objectAABBs)
					{
						DebugDrawAABB_GL(aabb);
					}

					// foreach (var aabb in chunkAABBs)
					// {
					// 	DebugDrawAABB_GL(aabb, Color.green);
					// }

					// DebugDrawCameraFrustum(Color.yellow);
				}
				else
				{
					foreach (var aabb in objectAABBs)
					{
						DebugDrawAABB(aabb, Color.cyan);
					}

					// foreach (var aabb in chunkAABBs)
					// {
					// 	DebugDrawAABB(aabb, Color.green);
					// }

					DebugDrawCameraFrustum(Color.yellow);
				}
			}
		}

		protected override void OnUpdate()
		{
			if (!RenderSettings.Instance.DebugMode)
				return;

			objectAABBs.Clear();

			var objAABBs = objectAABBs;

			Entities.ForEach((in WorldRenderBounds worldRenderBounds) => { objAABBs.Add(worldRenderBounds.AABB); })
				.Run();

			var query = GetEntityQuery(ComponentType.ChunkComponent<ChunkWorldRenderBounds>());
			var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
			var chunkAABBs = new NativeArray<AABB>(chunks.Length, Allocator.Temp);

			for (var index = 0; index < chunks.Length; index++)
			{
				var chunk = chunks[index];
				chunkAABBs[index] = EntityManager.GetChunkComponentData<ChunkWorldRenderBounds>(chunk).AABB;
			}
		}

		public void DebugDrawCameraFrustum(Color color)
		{
			var cam = RenderSettings.Instance.RenderCamera;
			var farClipCorners = new Vector3[4];
			var nearClipCorners = new Vector3[4];
			cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono,
				farClipCorners);
			cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono,
				nearClipCorners);

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
			// Debug.Log($"Drawing AABB: {aabb}");

			var center = aabb.Center;
			var extents = aabb.Extents;
			var ex = extents.x;
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DrawLine_GL(float3 begin, float3 end)
		{
			GL.Vertex(begin);
			GL.Vertex(end);
		}

		private void DebugDrawAABB_GL(AABB aabb)
		{
			var marker = new ProfilerMarker("Calculation");
			marker.Begin();
			var center = aabb.Center;
			var extents = aabb.Extents;
			var ex = extents.x;
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
			marker.End();
			
			using (new ProfilerMarker("GLPart").Auto())
			{
				DrawLine_GL(p0, p1);
				DrawLine_GL(p0, p3);
				DrawLine_GL(p2, p3);
				DrawLine_GL(p1, p2);
				DrawLine_GL(p4, p5);
				DrawLine_GL(p4, p7);
				DrawLine_GL(p5, p6);
				DrawLine_GL(p6, p7);
				DrawLine_GL(p3, p4);
				DrawLine_GL(p0, p7);
				DrawLine_GL(p2, p5);
				DrawLine_GL(p1, p6);
			}
		}
	}
}