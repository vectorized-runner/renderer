using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
	public partial class RenderSetupGroup : ComponentSystemGroup
	{
	}
}