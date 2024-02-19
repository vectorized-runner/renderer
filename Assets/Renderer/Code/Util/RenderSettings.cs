using UnityEngine;

namespace Renderer
{
	[CreateAssetMenu]
	public class RenderSettings : ScriptableObject
	{
		public float PanSpeed = 20.0f;
		public float MoveSpeed = 10.0f;

		public bool DebugMode = true;
		public Camera RenderCamera;

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