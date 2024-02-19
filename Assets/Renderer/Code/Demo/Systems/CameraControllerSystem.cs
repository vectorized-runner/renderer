using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup))]
	[UpdateBefore(typeof(CalculateCameraFrustumPlanesSystem))]
	public partial class CameraControllerSystem : SystemBase
	{
		private float2 _lastMousePosition;

		protected override void OnCreate()
		{
			_lastMousePosition = GetCurrentMousePos();
		}

		private float2 GetCurrentMousePos()
		{
			var pos = Input.mousePosition;
			return new float2(pos.x, pos.y);
		}

		protected override void OnUpdate()
		{
			var mousePos = GetCurrentMousePos();
			var mousePosDiff = mousePos - _lastMousePosition;
			var camera = RenderSettings.Instance.RenderCamera;
			var panAmount = GetPanAmount(mousePosDiff);
			var moveInput = GetMoveInput();
			var currentRot = camera.transform.rotation;
			var moveAmount = GetMoveAmount(currentRot, moveInput);
			
			var rotateY = quaternion.RotateY(panAmount.x);
			var rotateX = quaternion.RotateX(-panAmount.y);

			var newRot = math.mul(math.mul(currentRot, rotateX), rotateY);
			camera.transform.rotation = newRot;

			camera.transform.position += moveAmount;

			_lastMousePosition = mousePos;
		}

		private float2 GetPanAmount(float2 mousePosDiff)
		{
			if (math.length(mousePosDiff) <= 0.0f)
				return float2.zero;
			if (!Input.GetMouseButton(1))
				return float2.zero;

			return mousePosDiff * RenderSettings.Instance.PanSpeed * UnityEngine.Time.deltaTime * 0.001f;
		}

		private Vector3 GetMoveAmount(quaternion cameraRot, float3 moveInput)
		{
			if (math.length(moveInput) <= 0.0f)
				return float3.zero;

			var moveDirLocal = math.normalize(moveInput);
			var moveDirWorld = math.mul(cameraRot, moveDirLocal);

			return moveDirWorld * UnityEngine.Time.deltaTime * RenderSettings.Instance.MoveSpeed;
		}

		private float3 GetMoveInput()
		{
			var result = float3.zero;

			if (Input.GetKey(KeyCode.W))
				result += math.forward();
			if (Input.GetKey(KeyCode.S))
				result += math.back();
			if (Input.GetKey(KeyCode.A))
				result += math.left();
			if (Input.GetKey(KeyCode.D))
				result += math.right();

			return result * RenderSettings.Instance.MoveSpeed;
		}
	}
}