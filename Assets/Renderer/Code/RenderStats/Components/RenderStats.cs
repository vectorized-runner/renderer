using Unity.Entities;

namespace Renderer
{
	public struct RenderStats : IComponentData
	{
		public float AverageFps;
		public float AverageMs;
		public int RenderedCount;
		public int CulledCount;
		public int VertexCount;
		public int TrisCount;
	}
}