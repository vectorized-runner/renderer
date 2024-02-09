using UnityEngine;

namespace Renderer
{
	public static class RenderSettings
	{
		// TODO: Currently this count is low because we're initializing array per RenderMesh, even if it's not used.
		// Fix this inefficiency as we want to support as much as Meshes possible.
		public const int MaxSupportedUniqueMeshCount = ushort.MaxValue;

		public static float PanSpeed = 20.0f;
		public static float MoveSpeed = 10.0f;

		public static bool DebugMode;
		public static Camera RenderCamera;
	}
}