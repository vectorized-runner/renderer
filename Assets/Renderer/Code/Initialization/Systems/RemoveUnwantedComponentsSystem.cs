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
			EntityManager.RemoveComponent<LinkedEntityGroup>(_removeQuery);
		}
	}
}