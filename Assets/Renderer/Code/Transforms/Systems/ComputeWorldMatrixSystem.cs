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
					(ref LocalToWorld worldMatrix, in LocalTransform localTransform) =>
					{
						worldMatrix.Value = float4x4.TRS(localTransform.Position, localTransform.Rotation,
							localTransform.Scale);
					})
				.ScheduleParallel();

			// TODO: Handle objects with Parent component
		}
	}
}