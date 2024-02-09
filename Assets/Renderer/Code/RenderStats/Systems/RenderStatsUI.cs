using System.Text;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	public class RenderStatsUI : MonoBehaviour
	{
		public TextMeshProUGUI Text;

		private void LateUpdate()
		{
			var world = World.DefaultGameObjectInjectionWorld;
			var renderStatsSystem = world.GetExistingSystemManaged<RenderStatsSystem>();
			var entityManager = world.EntityManager;
			var renderStats = entityManager.GetComponentData<RenderStats>(renderStatsSystem.SystemHandle);

			var sb = new StringBuilder();
			var culledPercentage = (int)(100 * (float)renderStats.CulledCount / renderStats.TotalObjectCount);
			sb.AppendLine($"{renderStats.AverageMs:n2} ms");
			sb.AppendLine($"FPS: {renderStats.AverageFps:n2}");
			sb.AppendLine($"Total Objects: {renderStats.TotalObjectCount}");
			sb.AppendLine($"Render Objects: {renderStats.RenderedCount}");
			sb.AppendLine($"Culled Objects: {renderStats.CulledCount}");
			sb.AppendLine($"Culled Percentage: {culledPercentage}%");
			sb.AppendLine($"Total Verts: {renderStats.TotalVertsCount}");
			sb.AppendLine($"Render Verts: {renderStats.RenderVertsCount}");
			sb.AppendLine($"Total Tris: {renderStats.TotalTrisCount}");
			sb.AppendLine($"Render Tris: {renderStats.RenderTrisCount}");

			Text.text = sb.ToString();
		}
	}
}