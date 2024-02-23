using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderDebugGroup))]
	public unsafe partial class AABBDebugDrawSystem : SystemBase
	{
		private ChunkCullingSystem _cullingSystem;
		private GameObject _inEntityGo;
		private Mesh _inEntityMesh;
		private Mesh _outEntityMesh;
		private Mesh _inChunkMesh;
		private Mesh _partialChunkMesh;
		private Mesh _outChunkMesh;

		private GameObject _outEntityGo;
		private GameObject _inChunkGo;
		private GameObject _outChunkGo;
		private GameObject _partialChunkGo;

		public const int PointsPerAABB = 24;

		protected override void OnCreate()
		{
			_cullingSystem = World.GetExistingSystemManaged<ChunkCullingSystem>();

			var renderSettings = RenderSettings.Instance;
			_inEntityGo = CreateGameObject("AABBDebug-InEntityDrawer", renderSettings.InEntityColor);
			_outEntityGo = CreateGameObject("AABBDebug-OutEntityDrawer", renderSettings.OutEntityColor);
			_inChunkGo = CreateGameObject("AABBDebug-InChunkDrawer", renderSettings.InChunkColor);
			_outChunkGo = CreateGameObject("AABBDebug-OutChunkDrawer", renderSettings.OutChunkColor);
			_partialChunkGo = CreateGameObject("AABBDebug-PartialChunkDrawer", renderSettings.PartialChunkColor);

			_inEntityMesh = new Mesh();
			_outEntityMesh = new Mesh();
			_inChunkMesh = new Mesh();
			_partialChunkMesh = new Mesh();
			_outChunkMesh = new Mesh();

			SetMesh(_inEntityGo, _inEntityMesh);
			SetMesh(_outEntityGo, _outEntityMesh);
			SetMesh(_inChunkGo, _inChunkMesh);
			SetMesh(_partialChunkGo, _partialChunkMesh);
			SetMesh(_outChunkGo, _outChunkMesh);
		}

		protected override void OnDestroy()
		{
			Object.Destroy(_inEntityGo);
			Object.Destroy(_outEntityGo);
			Object.Destroy(_inChunkGo);
			Object.Destroy(_outChunkGo);
			Object.Destroy(_partialChunkGo);
		}

		private void AllocateLineMeshData(Mesh.MeshData data, int vertexCount)
		{
			data.SetVertexBufferParams(vertexCount, new VertexAttributeDescriptor(VertexAttribute.Position));
			data.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);
			data.subMeshCount = 1;
			data.SetSubMesh(0, new SubMeshDescriptor(0, vertexCount, MeshTopology.Lines),
				MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
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

			// TODO: All five meshes
			var inEntityVertexCount = PointsPerAABB * visibleObjectCount;
			var inEntityMeshArray = Mesh.AllocateWritableMeshData(1);
			AllocateLineMeshData(inEntityMeshArray[0], inEntityVertexCount);
			var inEntityLinePoints = inEntityMeshArray[0].GetVertexData<float3>();
			var inEntityLineIndices = inEntityMeshArray[0].GetIndexData<int>();

			var outEntityVertexCount = PointsPerAABB * culledEntityCount;
			var outEntityMeshArray = Mesh.AllocateWritableMeshData(1);
			AllocateLineMeshData(outEntityMeshArray[0], outEntityVertexCount);
			var outEntityLinePoints = outEntityMeshArray[0].GetVertexData<float3>();
			var outEntityLineIndices = outEntityMeshArray[0].GetIndexData<int>();

			var frustumInVertexCount = PointsPerAABB * frustumInCount;
			var inChunkMeshArray = Mesh.AllocateWritableMeshData(1);
			AllocateLineMeshData(inChunkMeshArray[0], frustumInVertexCount);
			var inChunkLinePoints = inChunkMeshArray[0].GetVertexData<float3>();
			var inChunkLineIndices = inChunkMeshArray[0].GetIndexData<int>();

			var frustumOutVertexCount = PointsPerAABB * frustumOutCount;
			var outChunkMeshArray = Mesh.AllocateWritableMeshData(1);
			AllocateLineMeshData(outChunkMeshArray[0], frustumOutVertexCount);
			var outChunkLinePoints = outChunkMeshArray[0].GetVertexData<float3>();
			var outChunkLineIndices = outChunkMeshArray[0].GetIndexData<int>();

			var frustumPartialVertexCount = PointsPerAABB * frustumPartialCount;
			var partialChunkMeshArray = Mesh.AllocateWritableMeshData(1);
			AllocateLineMeshData(partialChunkMeshArray[0], frustumPartialVertexCount);
			var partialChunkLinePoints = partialChunkMeshArray[0].GetVertexData<float3>();
			var partialChunkLineIndices = partialChunkMeshArray[0].GetIndexData<int>();

			var inEntityPointsCounter = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			var outEntityPointsCounter = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			var inChunkPointsCounter = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			var outChunkPointsCounter = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			var partialChunkPointsCounter = UnsafeMemory<int>.Alloc(Allocator.TempJob);
			var jobs = new NativeList<JobHandle>(Allocator.Temp);

			var collectLinesJob = new CollectAABBLinesJob
			{
				WorldBoundsHandle = GetComponentTypeHandle<WorldRenderBounds>(true),
				ChunkWorldBoundsHandle = GetComponentTypeHandle<ChunkWorldRenderBounds>(true),
				CullResultHandle = GetComponentTypeHandle<ChunkCullResult>(true),
				InEntityLinePoints = inEntityLinePoints,
				InEntityPointsCounter = inEntityPointsCounter.Ptr,
				OutEntityLinePoints = outEntityLinePoints,
				OutEntityPointsCounter = outEntityPointsCounter.Ptr,
				InChunkLinePoints = inChunkLinePoints,
				InChunkPointsCounter = inChunkPointsCounter.Ptr,
				OutChunkLinePoints = outChunkLinePoints,
				OutChunkPointsCounter = outChunkPointsCounter.Ptr,
				PartialChunkLinePoints = partialChunkLinePoints,
				PartialChunkPointsCounter = partialChunkPointsCounter.Ptr,
			}.ScheduleParallel(_cullingSystem.CullingQuery, Dependency);

			jobs.Add(collectLinesJob);

			jobs.Add(new FillIndicesJob
			{
				IndexArray = inEntityLineIndices
			}.Schedule(inEntityLineIndices.Length, 64, collectLinesJob));

			jobs.Add(new FillIndicesJob
			{
				IndexArray = outEntityLineIndices,
			}.Schedule(outEntityLineIndices.Length, 64, collectLinesJob));

			jobs.Add(new FillIndicesJob
			{
				IndexArray = inChunkLineIndices,
			}.Schedule(inChunkLineIndices.Length, 64, collectLinesJob));

			jobs.Add(new FillIndicesJob
			{
				IndexArray = outChunkLineIndices,
			}.Schedule(outChunkLineIndices.Length, 64, collectLinesJob));

			jobs.Add(new FillIndicesJob
			{
				IndexArray = partialChunkLineIndices,
			}.Schedule(partialChunkLineIndices.Length, 64, collectLinesJob));

			Dependency = JobHandle.CombineDependencies(jobs);

			JobHandle.CompleteAll(jobs.AsArray());

			var flags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
			Mesh.ApplyAndDisposeWritableMeshData(inEntityMeshArray, _inEntityMesh, flags);
			Mesh.ApplyAndDisposeWritableMeshData(outEntityMeshArray, _outEntityMesh, flags);
			Mesh.ApplyAndDisposeWritableMeshData(inChunkMeshArray, _inChunkMesh, flags);
			Mesh.ApplyAndDisposeWritableMeshData(outChunkMeshArray, _outChunkMesh, flags);
			Mesh.ApplyAndDisposeWritableMeshData(partialChunkMeshArray, _partialChunkMesh, flags);

			DebugDrawCameraFrustum(Color.yellow);

			inEntityPointsCounter.Dispose();
			outEntityPointsCounter.Dispose();
			inChunkPointsCounter.Dispose();
			outChunkPointsCounter.Dispose();
			partialChunkPointsCounter.Dispose();
		}

		private GameObject CreateGameObject(string name, Color materialColor)
		{
			var go = new GameObject(name);
			go.AddComponent<MeshFilter>();
			var meshRenderer = go.AddComponent<MeshRenderer>();
			meshRenderer.material = Resources.Load<Material>("LineMaterial");
			meshRenderer.material.SetColor("_BaseColor", materialColor);

			return go;
		}

		private void SetMesh(GameObject go, Mesh mesh)
		{
			go.GetComponent<MeshFilter>().sharedMesh = mesh;
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