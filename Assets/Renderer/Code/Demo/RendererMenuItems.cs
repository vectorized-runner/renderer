using UnityEditor;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Renderer
{
	public static class RendererMenuItems
	{
		[MenuItem("Renderer/Populate Scene")]
		private static void PopulateScene()
		{
			var config =
				AssetDatabase.LoadAssetAtPath<RendererDemoConfig>("Assets/Renderer/RendererDemoConfig.asset");

			Debug.Assert(config != null);

			var random = new Random(1);

			for (var i = 0; i < config.SpawnCount; i++)
			{
				var position = config.Center + random.NextFloat3Direction() * config.Radius;
				var rotation = random.NextQuaternionRotation();
				var scale = 1.0f;
				var go = Object.Instantiate(config.Prefab);
				go.transform.position = position;
				go.transform.rotation = rotation;
				go.transform.localScale = new Vector3(scale, scale, scale);
			}
		}
	}
}