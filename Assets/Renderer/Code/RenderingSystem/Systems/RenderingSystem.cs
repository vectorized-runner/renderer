using Unity.Entities;
using UnityEngine;

namespace Renderer
{
    public class RenderData : IComponentData
    {
        public Camera Camera;
    }


    // TODO: Handle object layers later
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class RenderingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var camera = Camera.main;

            // Let's run DrawMesh first, non-burst
            Entities.ForEach((in WorldMatrix worldMatrix, in RenderMeshIndex renderMeshIndex) =>
            {
                var renderMesh = RenderMeshRegisterSystem.GetRenderMesh(renderMeshIndex);

                Graphics.DrawMesh(renderMesh.Mesh, worldMatrix.Value, renderMesh.Material, renderMesh.Layer, camera,
                    renderMesh.SubMeshIndex);
            }).WithoutBurst().Run();
        }
    }
}