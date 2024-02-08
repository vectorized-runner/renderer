using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	public class RenderStatsUI : MonoBehaviour
	{
		public TextMeshProUGUI AverageMsText;
		public TextMeshProUGUI AverageFpsText;
		public TextMeshProUGUI TotalObjectsText;
		public TextMeshProUGUI RenderedCountText;
		public TextMeshProUGUI CulledCountText;
		public TextMeshProUGUI RenderTrisText;
		public TextMeshProUGUI RenderVertsText;

		private void LateUpdate()
		{
			var world = World.DefaultGameObjectInjectionWorld;
			var renderStatsSystem = world.GetExistingSystemManaged<RenderStatsSystem>();
			var entityManager = world.EntityManager;
			var renderStats = entityManager.GetComponentData<RenderStats>(renderStatsSystem.SystemHandle);

			AverageMsText.text = $"{renderStats.AverageMs:n2} ms";
			AverageFpsText.text = $"FPS: {renderStats.AverageFps:n2}";
			RenderedCountText.text = $"Rendered: {renderStats.RenderedCount}";
			CulledCountText.text = $"Culled: {renderStats.CulledCount}";
			RenderVertsText.text = $"Verts: {renderStats.VertexCount}";
			RenderTrisText.text = $"Tris: {renderStats.TrisCount}";
			TotalObjectsText.text = $"Total Objects: {renderStats.TotalObjectCount}";
		}
	}
}