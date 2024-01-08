using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using FrustumPlanes = Renderer.UnityPackages.FrustumPlanes;
using Object = UnityEngine.Object;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup))]
	public partial class CalculateCameraFrustumPlanesSystem : SystemBase
	{
		public NativeArray<FrustumPlanes.PlanePacket4> PlanePackets;

		private Camera _camera;
		private Plane[] _frustumPlanes;
		private NativeArray<Plane> _nativeFrustumPlanes;

		protected override void OnCreate()
		{
			_camera = Object.FindObjectOfType<Camera>();
			_frustumPlanes = new Plane[6];
			_nativeFrustumPlanes = new NativeArray<Plane>(6, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			_nativeFrustumPlanes.Dispose();
			PlanePackets.Dispose();
		}

		protected override void OnUpdate()
		{
			GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);
			_nativeFrustumPlanes.CopyFrom(_frustumPlanes);
			PlanePackets = FrustumPlanes.BuildSOAPlanePackets(_nativeFrustumPlanes, Allocator.TempJob);
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