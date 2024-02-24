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
				
				var entity = GetEntity(TransformUsageFlags.None);

				if (!authoring.TryGetComponent(out MeshRenderer meshRenderer))
					return;

				var material = meshRenderer.sharedMaterial;
				var mesh = authoring.GetComponent<MeshFilter>().sharedMesh;
				var subMeshCount = mesh.subMeshCount;
				var isStatic = authoring.IsStatic;
				var addEulerAngles = authoring.AddEulerAngles;
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
						var tf = authoring.gameObject.transform;
						var pos = new Position { Value = tf.position };
						var rot = new Rotation { Value = tf.rotation };
						var scale = new Scale { Value = tf.localScale.x };

						AddComponents(entity, pos, rot, scale, renderMesh, renderBounds, isStatic, addEulerAngles);
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