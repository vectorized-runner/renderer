using System;
using Unity.Entities;

namespace Renderer
{
	[Serializable]
	[InternalBufferCapacity(8)]
	public struct Child : IBufferElementData
	{
		public Entity Value;
	}
}