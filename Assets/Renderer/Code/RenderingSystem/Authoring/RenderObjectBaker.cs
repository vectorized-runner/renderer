using System;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
    public class RenderObjectBaker : MonoBehaviour
    {
        private class RenderObjectBakerBaker : Baker<RenderObjectBaker>
        {
            // TODO: Collect SharedMaterials
            // TODO: Collect Children objects
            // TODO: Consider Transform Hierarchy
            public override void Bake(RenderObjectBaker authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                if (!authoring.TryGetComponent(out MeshRenderer meshRenderer))
                    return;

                var material = meshRenderer.sharedMaterial;
                var mesh = authoring.GetComponent<MeshFilter>().sharedMesh;
                var subMeshCount = mesh.subMeshCount;

                // What am I going to do if I have to create multiple objects here?
                for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                {
                    var renderMesh = new RenderMesh(mesh, material, subMeshIndex);
                    var renderMeshId = RenderMeshRegisterSystem.GetRenderMeshIndex(renderMesh);

                    if (subMeshIndex == 0)
                    {
                        var tf = authoring.gameObject.transform;
                        var pos = new Position { Value = tf.position };
                        var rot = new Rotation { Value = tf.rotation };
                        var scale = new Scale { Value = tf.localScale.x };

                        AddComponents(entity, pos, rot, scale, renderMeshId);
                    }
                    else
                    {
                        throw new NotImplementedException("Handle multiple sub-meshes later");
                    }
                }
            }

            private void AddComponents(Entity entity, Position position, Rotation rotation, Scale scale,
                RenderMeshIndex renderMeshIndex)
            {
                AddComponent(entity, renderMeshIndex);
                AddComponent(entity, position);
                AddComponent(entity, rotation);
                AddComponent(entity, scale);
                AddComponent(entity, new WorldMatrix());
            }
        }
    }
}