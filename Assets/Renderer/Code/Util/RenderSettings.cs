using UnityEngine;

namespace Renderer
{
	[CreateAssetMenu]
	public class RenderSettings : ScriptableObject
	{
		public float PanSpeed = 20.0f;
		public float MoveSpeed = 10.0f;
		public int SpawnCount = 1_000;
		public bool DebugMode = true;
		public Camera RenderCamera;

		public Color InChunkColor = Color.cyan;
		public Color PartialChunkColor = new Color32(255, 165, 0, 255);
		public Color OutChunkColor = Color.magenta;
		public Color InEntityColor = Color.green;
		public Color OutEntityColor = Color.red;
		public Color CameraFrustumColor = Color.white;

		public static RenderSettings Instance
		{
			get
			{
				if (_instance == null)
				{
					var instance = Resources.Load<RenderSettings>("RenderSettings");
					if (instance == null)
						Debug.LogError("Couldn't find RenderSettings instance.");

					_instance = instance;
				}

				return _instance;
			}
			private set => _instance = value;
		}

		private static RenderSettings _instance;
	}
}