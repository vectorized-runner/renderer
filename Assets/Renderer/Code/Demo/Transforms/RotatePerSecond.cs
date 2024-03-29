using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[Serializable]
	public struct RotatePerSecond : IComponentData
	{
		public float3 Value;
	}
}