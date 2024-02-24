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
				Entity[] createdEntities;

				if (meshRenderer != null)
				{
					createdEntities = BakeMeshRenderer(meshRenderer, true);

					foreach (var entity in createdEntities)
					{
						var (localTransform, matrix) = GetTransformComponents(go);
						AddComponent(entity, localTransform);
						AddComponent(entity, matrix);

						if (parentEntity != Entity.Null)
						{
							AddComponent(entity, new Parent { Value = parentEntity });
						}

						if (addRotatePerSecond)
						{
							AddComponent(entity, new RotatePerSecond());
						}
					}
				}
				else
				{
					// Single Entity with only Transform components
					var entity = CreateAdditionalEntity(TransformUsageFlags.None, false, entityName);
					createdEntities = new[] { entity };
					var (localTransform, matrix) = GetTransformComponents(go);
					AddComponent(entity, localTransform);
					AddComponent(entity, matrix);

					if (parentEntity != Entity.Null)
					{
						AddComponent(entity, new Parent { Value = parentEntity });
					}

					if (addRotatePerSecond)
					{
						AddComponent(entity, new RotatePerSecond());
					}
				}

				var childCount = transform.childCount;
				for (int i = 0; i < childCount; i++)
				{
					var child = transform.GetChild(i).gameObject;
					var mainEntity = createdEntities[0];
					BakeDynamicRecursive(child, mainEntity, addRotatePerSecond);
				}

				if (parentEntity != Entity.Null)
				{
					// Only include direct children to the parent, not recursive
					var buffer = AddBuffer<Child>(parentEntity);

					foreach (var childEntity in createdEntities)
					{
						buffer.Add(new Child { Value = childEntity });
					}
				}
			}

			private void BakeStaticObject(MeshRenderer meshRenderer)
			{
				var entities = BakeMeshRenderer(meshRenderer, false);

				foreach (var entity in entities)
				{
					AddComponent(entity, new Static());
				}
			}

			private Entity[] BakeMeshRenderer(MeshRenderer meshRenderer, bool addRenderBounds)
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
				var (_, localToWorld) = GetTransformComponents(go);
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

					if (addRenderBounds)
					{
						AddComponent(entity, renderBounds);
					}

					AddComponent(entity, localToWorld);
					AddSharedComponentManaged(entity, renderMesh);
					AddComponent(entity, worldBounds);
					createdEntities[index] = entity;
				}

				return createdEntities;
			}

			private (LocalTransform, LocalToWorld) GetTransformComponents(GameObject gameObject)
			{
				var pos = gameObject.transform.position;
				var rot = gameObject.transform.rotation;
				var scale = gameObject.transform.localScale.x;

				return new
				(new LocalTransform { Position = pos, Rotation = rot, Scale = scale },
					new LocalToWorld { Value = float4x4.TRS(pos, rot, scale) });
			}
		}
	}
}