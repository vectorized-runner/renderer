using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
    public partial class ComputeWorldMatrixSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithNone<Static>()
                .ForEach(
                    (ref WorldMatrix worldMatrix, in Position position, in Rotation rotation, in Scale scale) =>
                    {
                        worldMatrix.Value = float4x4.TRS(position.Value, rotation.Value, scale.Value);
                    })
                .ScheduleParallel();
        }
    }
}