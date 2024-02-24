using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	public class RenderObject : MonoBehaviour
	{
		public bool IsStatic;
		public bool AddEulerAngles;

		private class RenderObjectBaker : Baker<RenderObject>
		{
			// TODO-Renderer: Collect SharedMaterials
			// TODO-Renderer: Collect Children objects
			// TODO-Renderer: Consider Transform Hierarchy
			public override void Bake(RenderObject authoring)
			{
				var go = authoring.gameObject;
				if (go.transform.parent != null)
				{
					Debug.LogError("RenderObject needs to be the root object.");
					return;
				}

				// If the object is static, we're not going to include Transform Hierarchy information
				var isStatic = authoring.IsStatic;

				if (isStatic)
				{
					var childrenWithMeshRenderer = go.GetComponentsInChildren<MeshRenderer>();
					foreach (var meshRenderer in childrenWithMeshRenderer)
					{
						BakeMeshRendererStatic(meshRenderer, authoring.AddEulerAngles);
					}
				}
				else
				{
					var childrenWithMeshRenderer = go.GetComponentsInChildren<MeshRenderer>();
					foreach (var meshRenderer in childrenWithMeshRenderer)
					{
						BakeMeshRendererDynamic(meshRenderer, authoring.IsStatic, authoring.AddEulerAngles);
					}
				}
			}

			private void BakeMeshRendererDynamic(MeshRenderer meshRenderer, bool isStatic, bool addEulerAngles)
			{
				// TODO: Implement
			}

			private void BakeMeshRendererStatic(MeshRenderer meshRenderer, bool addEulerAngles)
			{
				var entityName = meshRenderer.gameObject.name;
				var entity = CreateAdditionalEntity(TransformUsageFlags.None, false, entityName);
				var material = meshRenderer.sharedMaterial;
				var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
				var subMeshCount = mesh.subMeshCount;
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

					if (subMeshIndex == 0)
					{
						var tf = meshRenderer.gameObject.transform;
						var pos = new Position { Value = tf.position };
						var rot = new Rotation { Value = tf.rotation };
						var scale = new Scale { Value = tf.localScale.x };

						AddComponents(entity, pos, rot, scale, renderMesh, renderBounds, true, addEulerAngles);
					}
					else
					{
						throw new NotImplementedException("Handle multiple sub-meshes later");
					}
				}
			}

			// TODO-Renderer: Consider not storing the LocalToWorld at all? Is it required with the full Transform system?
			private void AddComponents(Entity entity, Position position, Rotation rotation, Scale scale,
				RenderMesh renderMesh, RenderBounds renderBounds, bool isStatic, bool addEulerAngles)
			{
				var localToWorld = new LocalToWorld
					{ Value = float4x4.TRS(position.Value, rotation.Value, scale.Value) };
				AddComponent(entity, localToWorld);
				AddSharedComponentManaged(entity, renderMesh);
				var worldBounds = RenderMath.ComputeWorldRenderBounds(renderBounds, localToWorld);
				AddComponent(entity, worldBounds);

				if (isStatic)
				{
					AddComponent(entity, new Static());
				}
				else
				{
					if (addEulerAngles)
					{
						AddComponent(entity, new EulerAngles());
					}

					AddComponent(entity, position);
					AddComponent(entity, rotation);
					AddComponent(entity, scale);
					AddComponent(entity, renderBounds);
				}
			}
		}
	}
}