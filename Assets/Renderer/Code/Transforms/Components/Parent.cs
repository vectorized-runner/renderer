using System;
using Unity.Entities;

namespace Renderer
{
	[Serializable]
	public struct Parent : IComponentData
	{
		public Entity Value;
	}
}