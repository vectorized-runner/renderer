using UnityEngine;

namespace Renderer
{
	public class RuntimeRenderingSettings : MonoBehaviour
	{
		private void Awake()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 10_000;

			Debug.Log($"{nameof(RuntimeRenderingSettings)} initialized.");
		}
	}
}