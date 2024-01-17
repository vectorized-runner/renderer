using UnityEngine;

namespace Renderer
{
	// Consider making this class non-static (an actual system)
	public class RenderMeshRegisterSystem : MonoBehaviour
	{
		public RenderMeshAssets RenderMeshAssets;

		public static RenderMeshRegisterSystem Instance;

		private void Awake()
		{
			Instance = this;
		}

		// TODO: Ensure RenderMesh count doesn't surpass this.
		public const int MaxSupportedUniqueMeshCount = 1024;
	}
}