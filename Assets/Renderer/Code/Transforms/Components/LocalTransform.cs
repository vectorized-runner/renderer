using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[Serializable]
	public struct LocalTransform : IComponentData
	{
		public float3 Position;
		public float Scale;
		public quaternion Rotation;
	}
}