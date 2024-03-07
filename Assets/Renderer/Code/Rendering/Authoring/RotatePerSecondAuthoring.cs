using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	public class RotatePerSecondAuthoring : MonoBehaviour
	{
		public RotatePerSecond RotatePerSecond;

		public class RotatePerSecondBaker : Baker<RotatePerSecondAuthoring>
		{
			public override void Bake(RotatePerSecondAuthoring authoring)
			{
				var entity = GetEntity(TransformUsageFlags.None);
				AddComponent(entity, authoring.RotatePerSecond);
			}
		}
	}
}