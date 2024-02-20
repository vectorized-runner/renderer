using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	public struct EulerAngles : IComponentData
	{
		public float3 Value;
	}
}