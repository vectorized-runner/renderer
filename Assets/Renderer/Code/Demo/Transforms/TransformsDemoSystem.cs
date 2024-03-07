using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Renderer
{
	[UpdateAfter(typeof(TransformsGroup))]
	public partial class TransformsDemoSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			if (SceneManager.GetActiveScene().name != "Demo-Transforms")
			{
				return;
			}

			if (Input.GetKeyDown(KeyCode.X))
			{
				// Remove all Parent components
				EntityManager.RemoveComponent<Parent>(GetEntityQuery(typeof(Parent)));
				Debug.Log("Removed Parent Component from All Entities.");
			}

			if (Input.GetKeyDown(KeyCode.Space))
			{
				// Destroy all Root Entities
				EntityManager.DestroyEntity(
					GetEntityQuery(
						ComponentType.ReadOnly<LocalToWorld>(),
						ComponentType.Exclude<Parent>()));

				Debug.Log("Destroyed All Root Entities.");
			}
		}
	}
}