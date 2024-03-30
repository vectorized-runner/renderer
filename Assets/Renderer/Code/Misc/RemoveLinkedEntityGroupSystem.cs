using Unity.Entities;

namespace Renderer
{
	/// <summary>
	/// Why this system is required:
	/// Runtime-Spawned Entities (Not authored through sub-scenes) have a LinkedEntityGroup unnecessarily,
	/// which takes extra Chunk space for no reason.
	/// </summary>
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial class RemoveLinkedEntityGroupSystem : SystemBase
	{
		private EntityQuery _query;

		protected override void OnCreate()
		{
			_query = GetEntityQuery(typeof(RenderObject), typeof(LinkedEntityGroup));
		}

		protected override void OnUpdate()
		{
			EntityManager.RemoveComponent<LinkedEntityGroup>(_query);
		}
	}
}