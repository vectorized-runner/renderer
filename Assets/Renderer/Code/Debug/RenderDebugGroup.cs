using Unity.Entities;

namespace Renderer
{
	[UpdateAfter(typeof(RenderingSystem))]
	[UpdateInGroup(typeof(PresentationSystemGroup))]
	public partial class RenderDebugGroup : ComponentSystemGroup
	{
	}
}