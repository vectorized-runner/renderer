using Unity.Entities;

namespace Renderer
{
	[UpdateAfter(typeof(TransformsGroup))]
	public partial class RenderBoundsGroup : ComponentSystemGroup
	{
	}
}