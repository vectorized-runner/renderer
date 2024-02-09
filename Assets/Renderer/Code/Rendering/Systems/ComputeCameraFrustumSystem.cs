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

		private Plane[] _frustumPlanes;
		private NativeArray<Plane> _nativeFrustumPlanes;
		
		protected override void OnCreate()
		{
			RenderSettings.RenderCamera = Object.FindObjectOfType<Camera>();
			_frustumPlanes = new Plane[6];
			_nativeFrustumPlanes = new NativeArray<Plane>(6, Allocator.Persistent);
		}

		protected override void OnDestroy()
		{
			_nativeFrustumPlanes.Dispose();
			PlanePackets.DisposeIfCreated();
		}

		protected override void OnUpdate()
		{
			GeometryUtility.CalculateFrustumPlanes(RenderSettings.RenderCamera, _frustumPlanes);
			_nativeFrustumPlanes.CopyFrom(_frustumPlanes);
			
			PlanePackets.DisposeIfCreated();
			PlanePackets = FrustumPlanes.BuildSOAPlanePackets(_nativeFrustumPlanes, Allocator.TempJob);
		}
	}
}