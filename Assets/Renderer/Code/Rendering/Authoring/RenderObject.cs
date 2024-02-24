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
				var root = authoring.gameObject;
				if (root.transform.parent != null)
				{
					Debug.LogError("RenderObject needs to be the root object.");
					return;
				}

				// If the object is static, we're not going to include Transform Hierarchy information
				var isStatic = authoring.IsStatic;

				if (isStatic)
				{
					var childrenWithMeshRenderer = root.GetComponentsInChildren<MeshRenderer>();
					foreach (var meshRenderer in childrenWithMeshRenderer)
					{
						BakeSingleObjectStatic(meshRenderer, authoring.AddEulerAngles);
					}
				}
				else
				{
					BakeDynamicRecursive(root, Entity.Null, authoring.AddEulerAngles);
				}
			}

			private void BakeDynamicRecursive(GameObject go, Entity parentEntity, bool addEulerAngles)
			{
				var transform = go.transform;
				var entityName = go.name;
				var entity = CreateAdditionalEntity(TransformUsageFlags.None, false, entityName);
				var childCount = transform.childCount;
				var (pos, rot, scale, matrix) = GetTransformComponents(go);

				AddComponent(entity, pos);
				AddComponent(entity, rot);
				AddComponent(entity, scale);
				AddComponent(entity, matrix);

				if (parentEntity != Entity.Null)
				{
					AddComponent(entity, new Parent { Value = parentEntity });
				}
				
				for (int i = 0; i < childCount; i++)
				{
					var child = transform.GetChild(i).gameObject;
					BakeDynamicRecursive(child, entity, addEulerAngles);
				}
				
				// TODO: Add Render Components
			}

			private void BakeSingleObjectStatic(MeshRenderer meshRenderer, bool addEulerAngles)
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

			private (Position, Rotation, Scale, LocalToWorld) GetTransformComponents(GameObject gameObject)
			{
				var pos = gameObject.transform.position;
				var rot = gameObject.transform.rotation;
				var scale = gameObject.transform.localScale.x;

				return new
				(new Position { Value = pos },
					new Rotation { Value = rot },
					new Scale { Value = scale },
					new LocalToWorld { Value = float4x4.TRS(pos, rot, scale) });
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