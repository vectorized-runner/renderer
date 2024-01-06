using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	public class RenderStatsUI : MonoBehaviour
	{
		public TextMeshProUGUI AverageMsText;
		public TextMeshProUGUI AverageFpsText;

		private void LateUpdate()
		{
			var world = World.DefaultGameObjectInjectionWorld;
			var renderStatsSystem = world.GetExistingSystemManaged<RenderStatsSystem>();
			var entityManager = world.EntityManager;
			var renderStats = entityManager.GetComponentData<RenderStats>(renderStatsSystem.SystemHandle);

			AverageMsText.text = $"{renderStats.AverageMs:n2} ms";
			AverageFpsText.text = $"FPS: {renderStats.AverageFps:n2}";
		}
	}
}