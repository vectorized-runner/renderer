using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace Renderer.Demo
{
	public class EntityModifyHelper : MonoBehaviour
	{
		[ShowInInspector]
		[ValueDropdown(nameof(GetAllEntityNames))]
		private string _entityName;

		[Button]
		public void DestroyEntity()
		{
			var em = GetEntityManager();

			foreach (var entity in em.GetAllEntities())
			{
				if (em.GetName(entity) == _entityName)
				{
					em.DestroyEntity(entity);
					Debug.Log("Done.");
					return;
				}
			}

			Debug.LogError("Couldn't do it.");
		}

		private EntityManager GetEntityManager()
		{
			return World.DefaultGameObjectInjectionWorld.EntityManager;
		}

		private string[] GetAllEntityNames()
		{
			var em = GetEntityManager();
			var allEntities = em.GetAllEntities();
			var names = new string[allEntities.Length];

			for (int i = 0; i < allEntities.Length; i++)
			{
				names[i] = em.GetName(allEntities[i]);
			}

			return names;
		}
	}
}