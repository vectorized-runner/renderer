using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Renderer
{
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
			var totalEntityCount = _cullingSystem.CullingQuery.CalculateEntityCount();
			var culledEntityCount = totalEntityCount - visibleObjectCount;
			var frustumInCount = _cullingSystem.FrustumInCount;
			var frustumOutCount = _cullingSystem.FrustumOutCount;
			var frustumPartialCount = _cullingSystem.FrustumPartialCount;
			var inEntityLinePoints = new NativeArray<float3>(PointsPerAABB * visibleObjectCount, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory);
			var inEntityLineIndices = new NativeArray<int>(PointsPerAABB * visibleObjectCount, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory);
			var outEntityLinePoints = new NativeArray<float3>(PointsPerAABB * culledEntityCount, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory);
			var outEntityLineIndices = new NativeArray<int>(PointsPerAABB * culledEntityCount, Allocator.TempJob,
				NativeArrayOptions.UninitializedMemory);
			var inEntityPointsCounter = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			var outEntityPointsCounter = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			var jobs = new NativeList<JobHandle>(Allocator.Temp);

			jobs.Add(new CollectAABBLinesJob
			{
				WorldBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(true),
				ChunkWorldBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(true),
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(true),
				InEntityLinePoints = inEntityLinePoints,
				InEntityPointsCounter = inEntityPointsCounter.Ptr,
				OutEntityLinePoints = outEntityLinePoints,
				OutEntityPointsCounter = outEntityPointsCounter.Ptr,
			}.ScheduleParallel(_cullingSystem.CullingQuery, Dependency));

			jobs.Add(new FillIndicesJob
			{
				IndexArray = inEntityLineIndices
			}.Schedule(inEntityLineIndices.Length, 64, Dependency));

			JobHandle.CompleteAll(jobs.AsArray());

			DrawAABBMesh(_go, inEntityLinePoints, inEntityLineIndices);
			DebugDrawCameraFrustum(Color.yellow);

			inEntityLineIndices.Dispose();
			inEntityLinePoints.Dispose();
			inEntityPointsCounter.Dispose();
			outEntityLineIndices.Dispose();
			outEntityLinePoints.Dispose();
			outEntityPointsCounter.Dispose();
		}

		private void DrawAABBMesh(GameObject go, NativeArray<float3> linePoints, NativeArray<int> lineIndices)
		{
			var mesh = new Mesh();

			using (new ProfilerMarker("SetFormat").Auto())
			{
				mesh.indexFormat = IndexFormat.UInt32;
			}

			// TODO: This can be optimized with MeshData API
			// https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Mesh.MeshData.html
			using (new ProfilerMarker("SetVertices").Auto())
			{
				mesh.SetVertices(linePoints.Reinterpret<Vector3>());
			}

			using (new ProfilerMarker("SetIndices").Auto())
			{
				mesh.SetIndices(lineIndices, MeshTopology.Lines, 0);
			}

			using (new ProfilerMarker("SetMesh").Auto())
			{
				var meshFilter = go.GetComponent<MeshFilter>();
				meshFilter.sharedMesh = mesh;
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
	}
}