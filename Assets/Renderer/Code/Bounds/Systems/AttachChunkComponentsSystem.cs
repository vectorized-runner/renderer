using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;

namespace Renderer
{
	[UpdateInGroup(typeof(RenderBoundsGroup), OrderFirst = true)]
	public partial class AttachChunkComponentsSystem : SystemBase
	{
		private EntityQuery _requireChunkBoundsQuery;
		private EntityQuery _requireCullResultQuery;

		protected override void OnCreate()
		{
			_requireChunkBoundsQuery = GetEntityQuery(
				ComponentType.ChunkComponentExclude<ChunkWorldRenderBounds>(),
				ComponentType.ReadOnly<WorldRenderBounds>());

			_requireCullResultQuery = GetEntityQuery(
				ComponentType.ChunkComponentExclude<ChunkCullResult>(),
				ComponentType.ReadOnly<WorldRenderBounds>());
		}

		protected override void OnUpdate()
		{
			EntityManager.RemoveComponent<LinkedEntityGroup>(_requireChunkBoundsQuery);
			
			EntityManager.AddChunkComponentData(_requireChunkBoundsQuery,
				new ChunkWorldRenderBounds
					{ AABB = new AABB { Center = new float3(-1), Extents = new float3(-1) } });
			
			EntityManager.AddChunkComponentData(_requireCullResultQuery,
				new ChunkCullResult { EntityVisibilityMask = new BitField128(new v128(0)) });
		}
	}
}