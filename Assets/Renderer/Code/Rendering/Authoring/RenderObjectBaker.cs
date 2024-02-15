using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	public class RenderObjectBaker : MonoBehaviour
	{
		public bool IsStatic;

		private class RenderObjectBakerBaker : Baker<RenderObjectBaker>
		{
			// TODO-Renderer: Collect SharedMaterials
			// TODO-Renderer: Collect Children objects
			// TODO-Renderer: Consider Transform Hierarchy
			public override void Bake(RenderObjectBaker authoring)
			{
				var entity = GetEntity(TransformUsageFlags.None);

				if (!authoring.TryGetComponent(out MeshRenderer meshRenderer))
					return;

				var material = meshRenderer.sharedMaterial;
				var mesh = authoring.GetComponent<MeshFilter>().sharedMesh;
				var subMeshCount = mesh.subMeshCount;
				var isStatic = authoring.IsStatic;
				var bounds = meshRenderer.localBounds;
				var renderBounds = new RenderBounds
				{
					AABB = new AABB
					{
						Center = bounds.center,
						Extents = bounds.extents
					}
				};

				// What am I going to do if I have to create multiple objects here?
				for (var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
				{
					var renderMesh = new RenderMesh(mesh, material, subMeshIndex);
					var renderMeshIndex = RenderMeshDatabase.Instance.RegisterRenderMesh(renderMesh);

					if (subMeshIndex == 0)
					{
						var tf = authoring.gameObject.transform;
						var pos = new Position { Value = tf.position };
						var rot = new Rotation { Value = tf.rotation };
						var scale = new Scale { Value = tf.localScale.x };

						AddComponents(entity, pos, rot, scale, renderMeshIndex, renderBounds, isStatic);
					}
					else
					{
						throw new NotImplementedException("Handle multiple sub-meshes later");
					}
				}
			}

			// TODO-Renderer: Consider not storing the LocalToWorld at all? Is it required with the full Transform system?
			private void AddComponents(Entity entity, Position position, Rotation rotation, Scale scale,
				RenderMeshIndex renderMeshIndex, RenderBounds renderBounds, bool isStatic)
			{
				var localToWorld = new LocalToWorld
					{ Value = float4x4.TRS(position.Value, rotation.Value, scale.Value) };
				AddComponent(entity, localToWorld);
				AddSharedComponent(entity, renderMeshIndex);
				var worldBounds = ComputeWorldRenderBoundsSystem.CalculateWorldBounds(renderBounds, localToWorld);
				AddComponent(entity, worldBounds);

				if (isStatic)
				{
					AddComponent(entity, new Static());
				}
				else
				{
					AddComponent(entity, position);
					AddComponent(entity, rotation);
					AddComponent(entity, scale);
					AddComponent(entity, renderBounds);
				}
			}
		}
	}
}