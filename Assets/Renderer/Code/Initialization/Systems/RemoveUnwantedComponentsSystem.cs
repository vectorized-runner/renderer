using Unity.Entities;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderSetupGroup), OrderFirst = true)]
	public partial class RemoveUnwantedComponentsSystem : SystemBase
	{
		private EntityQuery _removeQuery;
		
		protected override void OnCreate()
		{
			_removeQuery = GetEntityQuery(typeof(LinkedEntityGroup));
		}

		protected override void OnUpdate()
		{
			// TODO-Renderer: This breaks AttachChunkComponentsSystem!, Unity probably has a internal caching bug
			// If Spawning Static entities doesn't properly update the ChunkWorldRenderBounds (float3.zero value)
			// return back to here!
			// EntityManager.RemoveComponent<LinkedEntityGroup>(_removeQuery);
		}
	}
}