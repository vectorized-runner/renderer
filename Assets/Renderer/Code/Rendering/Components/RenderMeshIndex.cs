using Unity.Entities;

namespace Renderer
{
	public readonly struct RenderMeshIndex : ISharedComponentData
	{
		public readonly int Value;

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