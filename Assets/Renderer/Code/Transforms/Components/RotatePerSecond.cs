using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	public struct RotatePerSecond : IComponentData
	{
		public float3 Value;
	}
}