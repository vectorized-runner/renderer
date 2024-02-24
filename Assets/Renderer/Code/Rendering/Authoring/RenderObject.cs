using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	public class RenderObject : MonoBehaviour
	{
		public bool IsStatic;
		public bool AddRotatePerSecond;

		private class RenderObjectBaker : Baker<RenderObject>
		{
			// TODO-Renderer: Collect SharedMaterials
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
						BakeStaticObject(meshRenderer);
					}
				}
				else
				{
					BakeDynamicRecursive(root, Entity.Null, authoring.AddRotatePerSecond);
				}
			}

			private void BakeDynamicRecursive(GameObject go, Entity parentEntity, bool addRotatePerSecond)
			{
				var transform = go.transform;
				var entityName = go.name;
				var meshRenderer = go.GetComponent<MeshRenderer>();
				Entity mainEntity;

				if (meshRenderer != null)
				{
					mainEntity = Entity.Null;
				}
				else
				{
					// Single Entity with only Transform components
					var entity = CreateAdditionalEntity(TransformUsageFlags.None, false, entityName);
					var (pos, rot, scale, matrix) = GetTransformComponents(go);
					AddComponent(entity, pos);
					AddComponent(entity, rot);
					AddComponent(entity, scale);
					AddComponent(entity, matrix);

					if (parentEntity != Entity.Null)
					{
						AddComponent(entity, new Parent { Value = parentEntity });
					}

					if (addRotatePerSecond)
					{
						AddComponent(entity, new RotatePerSecond());
					}

					mainEntity = entity;
				}

				// TODO: Add RenderBounds, if Object is Dynamic
				// TODO: Add Render Components

				var childCount = transform.childCount;
				for (int i = 0; i < childCount; i++)
				{
					var child = transform.GetChild(i).gameObject;
					BakeDynamicRecursive(child, mainEntity, addRotatePerSecond);
				}

				// TODO: Add Render Components
			}

			private void BakeStaticObject(MeshRenderer meshRenderer)
			{
				var entities = BakeMeshRenderer(meshRenderer);

				foreach (var entity in entities)
				{
					AddComponent(entity, new Static());
				}
			}

			private Entity[] BakeMeshRenderer(MeshRenderer meshRenderer)
			{
				var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
				var subMeshCount = mesh.subMeshCount;
				if (subMeshCount == 0)
				{
					throw new Exception("At least one SubMesh is expected.");
				}

				if (subMeshCount != 1)
				{
					throw new NotImplementedException("Multiple sub-meshes isn't supported yet.");
				}

				var sharedMaterials = meshRenderer.sharedMaterials;
				if (sharedMaterials.Length == 0)
				{
					throw new Exception("At least one SharedMaterial is expected");
				}

				var go = meshRenderer.gameObject;
				var (_, _, _, localToWorld) = GetTransformComponents(go);
				var aabb = RenderMath.ComputeMeshAABB(mesh);
				var renderBounds = new RenderBounds { AABB = aabb };
				var worldBounds = RenderMath.ComputeWorldRenderBounds(renderBounds, localToWorld);
				var createdEntities = new Entity[sharedMaterials.Length];

				for (var index = 0; index < sharedMaterials.Length; index++)
				{
					var sharedMaterial = sharedMaterials[index];
					var entityName = $"{meshRenderer.gameObject.name}-{index}";
					var entity = CreateAdditionalEntity(TransformUsageFlags.None, false, entityName);
					const int subMeshIndex = 0;
					var renderMesh = new RenderMesh(mesh, sharedMaterial, subMeshIndex);

					AddComponent(entity, localToWorld);
					AddSharedComponentManaged(entity, renderMesh);
					AddComponent(entity, worldBounds);
					createdEntities[index] = entity;
				}

				return createdEntities;
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
		}
	}
}