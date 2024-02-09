using Unity.Entities;

namespace Renderer
{
	public struct RenderStats : IComponentData
	{
		public int AverageFps;
		public float AverageMs;
		public int RenderedCount;
		public int CulledCount;
		public int TotalObjectCount;
		public int RenderVertsCount;
		public int RenderTrisCount;
		public int TotalVertsCount;
		public int TotalTrisCount;
		public int RenderBatchCount;
		public int ChunkCount;
	}
}