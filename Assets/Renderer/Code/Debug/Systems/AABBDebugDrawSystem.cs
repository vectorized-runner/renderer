using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	// Make it work fast now -- that's the important part
	// Coloring is easy

	[UpdateInGroup(typeof(RenderDebugGroup))]
	public partial class AABBDebugDrawSystem : SystemBase
	{
		private ChunkCullingSystem _cullingSystem;
		
		public const int PointsPerAABB = 24;

		protected override void OnCreate()
		{
			_cullingSystem = World.GetExistingSystemManaged<ChunkCullingSystem>();
		}

		protected override void OnDestroy()
		{
		}

		protected override void OnUpdate()
		{
			using var marker = new AutoProfilerMarker("AABBDebugDraw");

			if (!RenderSettings.Instance.DebugMode)
				return;

			var visibleObjectCount = _cullingSystem.VisibleObjectCount;
			var pointCount = PointsPerAABB * visibleObjectCount;
			
			// TODO: Dispose this
			var inEntityLinePoints = new NativeList<float3>(pointCount, Allocator.TempJob);
			var inEntityLineIndices = new NativeList<int>(pointCount, Allocator.TempJob);
			
			new CollectAABBLinesJob
			{
				ChunkWorldBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(true),
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(true),
				InEntityLineIndices = inEntityLineIndices.AsParallelWriter(),
				InEntityLinePoints = inEntityLinePoints.AsParallelWriter()
			}.Run(_cullingSystem.CullingQuery);
			
			// TODO: Continue from here -- Create Render gameObject
			// Create a new mesh
			var mesh = new Mesh();

			var verticesAsVector3 = inEntityLinePoints.AsArray().Reinterpret<Vector3>();
			mesh.SetVertices(verticesAsVector3);
			mesh.SetIndices(inEntityLineIndices.AsArray(), MeshTopology.Lines, 0);

			MeshFilter meshFilter = GetComponent<MeshFilter>();
			if (meshFilter != null)
			{
				meshFilter.sharedMesh = mesh;
			}
			else
			{
				Debug.LogError("MeshFilter component not found!");
			}

			// foreach (var aabb in chunkAABBs)
			// {
			// 	DebugDrawAABB(aabb, Color.green);
			// }

			DebugDrawCameraFrustum(Color.yellow);
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
	}
}