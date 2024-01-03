using System;
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
            EntityManager.AddComponentData(SystemHandle, new RenderSettings
            {
                RenderMode = RenderMode.Default
            });
        }

        protected override void OnUpdate()
        {
            var camera = Camera.main;
            var renderSettings = EntityManager.GetComponentData<RenderSettings>(SystemHandle);

            switch (renderSettings.RenderMode)
            {
                case RenderMode.Default:
                {
                    Entities.ForEach((in WorldMatrix worldMatrix, in RenderMeshIndex renderMeshIndex) =>
                    {
                        var renderMesh = RenderMeshRegisterSystem.GetRenderMesh(renderMeshIndex);

                        Graphics.DrawMesh(renderMesh.Mesh, worldMatrix.Value, renderMesh.Material, renderMesh.Layer,
                            camera,
                            renderMesh.SubMeshIndex);
                    }).WithoutBurst().Run();

                    break;
                }
                case RenderMode.InstancingOn:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Let's run DrawMesh first, non-burst
        }
    }
}