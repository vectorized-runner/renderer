using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup))]
	public partial class CalculateCameraFrustumPlanesSystem : SystemBase
	{
		private Camera _camera;

		private Plane[] _frustumPlanes;

		// TODO: Include this NativeArray on Entity data (Read/Write checks)
		public NativeArray<Plane> NativeFrustumPlanes;

		protected override void OnCreate()
		{
			_camera = Camera.main;
			if (_camera == null)
				_camera = Object.FindObjectOfType<Camera>();
			if (_camera == null)
				throw new Exception("Couldn't find any camera!");


			_frustumPlanes = new Plane[6];
			NativeFrustumPlanes = new NativeArray<Plane>(6, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			NativeFrustumPlanes.Dispose();
		}

		protected override void OnUpdate()
		{
			GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);
			NativeFrustumPlanes.CopyFrom(_frustumPlanes);
		}

		public void DebugDrawCameraFrustum()
		{
			var corners = new Vector3[4];
			_camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane,
				Camera.MonoOrStereoscopicEye.Mono,
				corners);
			for (var i = 0; i < corners.Length; i++)
			{
				var worldSpaceCorner = _camera.transform.TransformVector(corners[i]);
				Debug.DrawRay(_camera.transform.position, worldSpaceCorner, Color.blue);
			}
		}
	}
}