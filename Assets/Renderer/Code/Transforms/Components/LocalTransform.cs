using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	public struct LocalTransform : IComponentData
	{
		public float3 Position;
		public float Scale;
		public quaternion Rotation;
	}
}