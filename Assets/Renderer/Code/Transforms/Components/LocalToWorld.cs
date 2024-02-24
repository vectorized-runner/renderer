using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[Serializable]
	public struct LocalToWorld : IComponentData
	{
		public float4x4 Value;

		public float3 Right => Value.c0.xyz;
		public float3 Up => Value.c1.xyz;
		public float3 Forward => Value.c2.xyz;
		public float3 Position => Value.c3.xyz;
	}
}