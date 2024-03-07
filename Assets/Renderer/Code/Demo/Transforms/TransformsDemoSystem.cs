using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	[UpdateAfter(typeof(TransformsGroup))]
	public partial class TransformsDemoSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.X))
			{
				// Remove all Parent components
				EntityManager.RemoveComponent<Parent>(GetEntityQuery(typeof(Parent)));
				Debug.Log("Removed Parent component");
			}
		}
	}
}