using Unity.Entities;

namespace Renderer
{
	public struct RenderMeshIndex : ISharedComponentData
	{
		public int Value;

		public RenderMeshIndex(int value)
		{
			Value = value;
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}
}