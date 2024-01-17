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
		public int LastRenderedObjectCount { get; private set; }
		
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
			var maxRenderMeshCount = RenderMeshRegisterSystem.MaxSupportedUniqueMeshCount;
			var renderedCount = 0;
			
			for (var renderMeshIndex = 0; renderMeshIndex < maxRenderMeshCount; renderMeshIndex++)
			{
				var matrices = matricesByRenderMeshIndex[renderMeshIndex];
				var drawCount = matrices.Length;
				if (drawCount == 0)
					continue;

				var renderMesh = RenderMeshRegisterSystem.Instance.RenderMeshAssets.GetRenderMesh(new RenderMeshIndex(renderMeshIndex));
				var fullBatchCount = drawCount / _maxDrawCountPerBatch;
				int batchIndex;

				for (batchIndex = 0; batchIndex < fullBatchCount; batchIndex++)
				{
					var matrixBatch = matrices.AsSpan(batchIndex * _maxDrawCountPerBatch, _maxDrawCountPerBatch)
						.Reinterpret<float4x4, Matrix4x4>();
					DrawMeshInstanced(renderMesh, matrixBatch);

					renderedCount += matrixBatch.Length;
				}

				var lastBatchDrawCount = drawCount % _maxDrawCountPerBatch;
				if (lastBatchDrawCount > 0)
				{
					var span = matrices.AsSpan(batchIndex * _maxDrawCountPerBatch, lastBatchDrawCount);
					var m4x4 = span.Reinterpret<float4x4, Matrix4x4>();
					DrawMeshInstanced(renderMesh, m4x4);

					renderedCount += span.Length;
				}

				LastRenderedObjectCount = renderedCount;
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