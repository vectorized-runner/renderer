using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public partial class RenderingSystem : SystemBase
	{
		public int RenderedObjectCount { get; private set; }
		public int RenderedTris { get; private set; }
		public int RenderedVerts { get; private set; }

		private const int _maxDrawCountPerBatch = 1023;
		private static Matrix4x4[] _matrixCache;
		private ChunkCullingSystem _cullingSystem;

		protected override void OnCreate()
		{
			_cullingSystem = World.GetExistingSystemManaged<ChunkCullingSystem>();
			_matrixCache = new Matrix4x4[_maxDrawCountPerBatch];
		}

		protected override void OnUpdate()
		{
			Debug.Log("RenderingSystem running!");

			// TODO: Check the old thread. How to not call complete on this? I want to make this run like a job
			_cullingSystem.FinalJobHandle.Complete();

			var matricesByRenderMeshIndex = _cullingSystem.MatricesByRenderMeshIndex;
			var maxRenderMeshCount = RenderConstants.MaxSupportedUniqueMeshCount;
			var renderedCount = 0;
			var renderedTris = 0;
			var renderedVerts = 0;

			for (var renderMeshIndex = 0; renderMeshIndex < maxRenderMeshCount; renderMeshIndex++)
			{
				var matrices = matricesByRenderMeshIndex[renderMeshIndex];
				var drawCount = matrices.Length;
				if (drawCount == 0)
					continue;

				var renderMesh = RenderMeshDatabase.Instance.GetRenderMesh(new RenderMeshIndex(renderMeshIndex));
				var vertexCount = renderMesh.Mesh.vertexCount;
				var trisCount = renderMesh.Mesh.triangles.Length;
				var fullBatchCount = drawCount / _maxDrawCountPerBatch;
				int batchIndex;

				for (batchIndex = 0; batchIndex < fullBatchCount; batchIndex++)
				{
					var matrixBatch = matrices.AsSpan(batchIndex * _maxDrawCountPerBatch, _maxDrawCountPerBatch)
						.Reinterpret<float4x4, Matrix4x4>();
					DrawMeshInstanced(renderMesh, matrixBatch);

					var count = matrixBatch.Length;
					renderedCount += count;
					renderedVerts += vertexCount * count;
					renderedTris += trisCount * count;
				}

				var lastBatchDrawCount = drawCount % _maxDrawCountPerBatch;
				if (lastBatchDrawCount > 0)
				{
					var span = matrices.AsSpan(batchIndex * _maxDrawCountPerBatch, lastBatchDrawCount);
					var m4x4 = span.Reinterpret<float4x4, Matrix4x4>();
					DrawMeshInstanced(renderMesh, m4x4);

					var count = span.Length;
					renderedCount += count;
					renderedVerts += vertexCount * count;
					renderedTris += trisCount * count;
				}

				RenderedObjectCount = renderedCount;
				RenderedVerts = renderedVerts;
				RenderedTris = renderedTris;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void DrawMeshInstanced(in RenderMesh renderMesh, ReadOnlySpan<Matrix4x4> matrices)
		{
			Debug.Assert(matrices.Length > 0 && matrices.Length <= 1023);
			matrices.CopyTo(_matrixCache);
			Graphics.DrawMeshInstanced(renderMesh.Mesh, renderMesh.SubMeshIndex, renderMesh.Material, _matrixCache,
				matrices.Length);
		}
	}
}