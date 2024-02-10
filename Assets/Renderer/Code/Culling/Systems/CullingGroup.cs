using Unity.Entities;

namespace Renderer
{
	[UpdateAfter(typeof(RenderBoundsGroup))]
	public partial class CullingGroup : ComponentSystemGroup
	{
		
	}
}