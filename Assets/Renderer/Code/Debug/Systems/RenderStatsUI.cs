using System.Text;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Renderer
{
	public class RenderStatsUI : MonoBehaviour
	{
		public TextMeshProUGUI Text;
		public Toggle DebugModeToggle;

		private void Start()
		{
			DebugModeToggle.isOn = RenderSettings.DebugMode;
			DebugModeToggle.onValueChanged.AddListener(OnDebugToggleChanged);
		}

		private void OnDestroy()
		{
			DebugModeToggle.onValueChanged.RemoveListener(OnDebugToggleChanged);
		}

		private void OnDebugToggleChanged(bool newValue)
		{
			RenderSettings.DebugMode = newValue;
		}

		private void LateUpdate()
		{
			var world = World.DefaultGameObjectInjectionWorld;
			var renderStatsSystem = world.GetExistingSystemManaged<RenderStatsSystem>();
			var entityManager = world.EntityManager;
			var renderStats = entityManager.GetComponentData<RenderStats>(renderStatsSystem.SystemHandle);

			var sb = new StringBuilder();
			var culledPercentage = (int)math.floor(100 * (float)renderStats.CulledCount / renderStats.TotalObjectCount);
			sb.AppendLine($"{renderStats.AverageMs:n2} ms");
			sb.AppendLine($"FPS: {renderStats.AverageFps}");
			sb.AppendLine($"Total Objects: {renderStats.TotalObjectCount:N0}");
			sb.AppendLine($"Render Objects: {renderStats.RenderedCount:N0}");
			sb.AppendLine($"Culled Objects: {renderStats.CulledCount:N0}");
			sb.AppendLine($"Culled Percentage: {culledPercentage}%");
			sb.AppendLine($"Render Verts: {renderStats.RenderVertsCount:N0}");
			sb.AppendLine($"Render Tris: {renderStats.RenderTrisCount:N0}");
			sb.AppendLine($"Render Batches: {renderStats.RenderBatchCount:N0}");
			sb.AppendLine($"ECS Chunks: {renderStats.ChunkCount}");

			Text.text = sb.ToString();
		}
	}
}