using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(TransformsGroup))]
	[UpdateBefore(typeof(ComputeWorldMatrixSystem))]
	public partial class RotatePerSecondSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			Entities
				.WithNone<Static>().ForEach((ref LocalTransform transform, in RotatePerSecond eulerAngles) =>
				{
					var xyz = eulerAngles.Value;
					var xRot = quaternion.RotateX(xyz.x);
					var yRot = quaternion.RotateY(xyz.y);
					var zRot = quaternion.RotateZ(xyz.z);
					transform.Rotation = math.mul(transform.Rotation, math.mul(zRot, math.mul(xRot, yRot)));
				}).ScheduleParallel();
		}
	}
}