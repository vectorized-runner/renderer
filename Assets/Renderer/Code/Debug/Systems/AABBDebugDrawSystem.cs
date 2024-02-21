using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	// Make it work fast now -- that's the important part
	// Coloring is easy

	[UpdateInGroup(typeof(RenderDebugGroup))]
	public unsafe partial class AABBDebugDrawSystem : SystemBase
	{
		private ChunkCullingSystem _cullingSystem;
		private GameObject _go;
		
		public const int PointsPerAABB = 24;

		protected override void OnCreate()
		{
			_cullingSystem = World.GetExistingSystemManaged<ChunkCullingSystem>();

			_go = new GameObject("AABBDebugDraw");
			_go.AddComponent<MeshFilter>();
			var meshRenderer = _go.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = Resources.Load<Material>("LineMaterial");
		}

		protected override void OnDestroy()
		{
			Object.Destroy(_go);
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
			var inEntityLineIndices = new NativeArray<int>(pointCount, Allocator.TempJob);
			var lineIndicesMem = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			
			new CollectAABBLinesJob
			{
				ChunkWorldBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(true),
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(true),
				InEntityLineIndices = inEntityLineIndices,
				LineIndicesLengthPtr = lineIndicesMem.Ptr,
				InEntityLinePoints = inEntityLinePoints.AsParallelWriter(),
			}.Run(_cullingSystem.CullingQuery);
			
			var mesh = new Mesh();

			var verticesAsVector3 = inEntityLinePoints.AsArray().Reinterpret<Vector3>();
			mesh.SetVertices(verticesAsVector3);
			mesh.SetIndices(inEntityLineIndices, MeshTopology.Lines, 0);

			var meshFilter = _go.GetComponent<MeshFilter>();
			meshFilter.sharedMesh = mesh;

			DebugDrawCameraFrustum(Color.yellow);

			inEntityLineIndices.Dispose();
			inEntityLinePoints.Dispose();
			lineIndicesMem.Dispose();
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