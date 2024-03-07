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
				Debug.Log("Removed Parent component");
			}
		}
	}
}