using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(TransformsGroup))]
	public partial class ComputeWorldMatrixSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			Entities
				.WithNone<Static, Parent>()
				.ForEach(
					(ref LocalToWorld worldMatrix, in Position position, in Rotation rotation, in Scale scale) =>
					{
						worldMatrix.Value = float4x4.TRS(position.Value, rotation.Value, scale.Value);
					})
				.ScheduleParallel();
			
			// TODO: Handle objects with Parent component
		}
	}
}