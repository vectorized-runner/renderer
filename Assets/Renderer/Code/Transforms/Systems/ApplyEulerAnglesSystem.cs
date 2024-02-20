using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(TransformsGroup))]
	[UpdateBefore(typeof(ComputeWorldMatrixSystem))]
	public partial class ApplyEulerAnglesSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			Entities
				.WithNone<Static>().ForEach((ref Rotation rotation, in EulerAngles eulerAngles) =>
				{
					rotation.Value = quaternion.EulerZXY(eulerAngles.Value);
				}).ScheduleParallel();
		}
	}
}