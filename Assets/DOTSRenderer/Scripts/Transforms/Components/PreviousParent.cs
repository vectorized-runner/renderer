using Unity.Entities;

namespace Renderer
{
	// This data is required to handle added-removed parent components
	public struct PreviousParent : IComponentData
	{
		public Entity Value;
	}
}