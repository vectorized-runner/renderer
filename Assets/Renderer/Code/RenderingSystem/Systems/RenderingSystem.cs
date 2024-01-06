using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
    // TODO: Handle object layers later
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class RenderingSystem : SystemBase
    {
        protected override void OnCreate()
        {
        }

        protected override void OnUpdate()
        {
            var camera = Camera.main;

            Entities.ForEach((in LocalToWorld worldMatrix, in RenderMeshIndex renderMeshIndex) =>
            {
                var renderMesh = RenderMeshRegisterSystem.GetRenderMesh(renderMeshIndex);

                Graphics.DrawMesh(renderMesh.Mesh, worldMatrix.Value, renderMesh.Material, renderMesh.Layer,
                    camera,
                    renderMesh.SubMeshIndex);
            }).WithoutBurst().Run();
        }
    }
}
