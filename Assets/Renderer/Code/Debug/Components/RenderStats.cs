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
		public int RenderBatchCount;
		public int ChunkCount;
		public int OutChunks;
		public int InChunks;
		public int PartialChunks;
	}
}