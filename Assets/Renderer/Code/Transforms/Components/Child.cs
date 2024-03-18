using System;
using Unity.Entities;

namespace Renderer
{
	[Serializable]
	[InternalBufferCapacity(8)]
	public struct Child : ICleanupBufferElementData
	{
		public Entity Value;
	}
}