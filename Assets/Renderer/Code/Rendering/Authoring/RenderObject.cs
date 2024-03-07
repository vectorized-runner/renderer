using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Renderer
{
	public class RenderObject : MonoBehaviour
	{
		public bool IsStatic;

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
					BakeDynamicRecursive(root, Entity.Null);
				}
			}

			private void BakeDynamicRecursive(GameObject go, Entity parentEntity)
			{
				var transform = go.transform;
				var entityName = go.name;
				var meshRenderer = go.GetComponent<MeshRenderer>();
				Entity[] createdEntities;
				var isRoot = go.transform.parent == null;

				if (meshRenderer != null)
				{
					createdEntities = BakeMeshRenderer(isRoot, meshRenderer, true);

					foreach (var entity in createdEntities)
					{
						var (localTransform, _) = GetTransformComponents(go);
						AddComponent(entity, localTransform);

						if (parentEntity != Entity.Null)
						{
							AddComponent(entity, new Parent { Value = parentEntity });
							AddComponent(entity, new PreviousParent { Value = parentEntity });
						}
					}
				}
				else
				{
					// Single Entity with only Transform components (No MeshRenderer, but still have to Create Entity for Transform Hierarchy)
					var entity = isRoot
						? GetEntity(TransformUsageFlags.None)
						: CreateAdditionalEntity(TransformUsageFlags.None, false, entityName);

					createdEntities = new[] { entity };
					var (localTransform, matrix) = GetTransformComponents(go);
					AddComponent(entity, localTransform);
					AddComponent(entity, matrix);

					if (parentEntity != Entity.Null)
					{
						AddComponent(entity, new Parent { Value = parentEntity });
						AddComponent(entity, new PreviousParent { Value = parentEntity });
					}
				}

				var childCount = transform.childCount;
				for (int i = 0; i < childCount; i++)
				{
					var child = transform.GetChild(i).gameObject;
					var mainEntity = createdEntities[0];
					BakeDynamicRecursive(child, mainEntity);
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
				var isRoot = meshRenderer.transform.parent == null;
				var entities = BakeMeshRenderer(isRoot, meshRenderer, false);

				foreach (var entity in entities)
				{
					AddComponent(entity, new Static());
				}
			}

			private Entity[] BakeMeshRenderer(bool isRoot, MeshRenderer meshRenderer, bool addRenderBounds)
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
				var materialCount = sharedMaterials.Length;

				if (materialCount != 1)
				{
					// Problem with multiple materials: Dynamic Objects.
					// We need to Link the Same Transform for these objects (if one moves, the other one moves as well)
					// TODO: Properly test this in our Test-Scene.
					throw new NotSupportedException("Multiple Materials aren't supported yet.");
				}

				var isSingleMaterial = materialCount == 1;
				var createdEntities = new Entity[materialCount];

				for (var index = 0; index < materialCount; index++)
				{
					var sharedMaterial = sharedMaterials[index];
					var entityName = isSingleMaterial
						? meshRenderer.gameObject.name
						: $"{meshRenderer.gameObject.name}-{index}";

					var entity = isRoot
						? GetEntity(TransformUsageFlags.None)
						: CreateAdditionalEntity(TransformUsageFlags.None, false, entityName);
					const int subMeshIndex = 0;
					var renderMesh = new RenderMesh(mesh, sharedMaterial, subMeshIndex);

					if (addRenderBounds)
					{
						AddComponent(entity, renderBounds);
					}

					AddComponent(entity, localToWorld);
					AddSharedComponentManaged(entity, renderMesh);
					AddComponent(entity, worldBounds);
					AddComponent(entity, new RenderObjectTag());
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