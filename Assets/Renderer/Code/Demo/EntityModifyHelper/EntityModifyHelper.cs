using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace Renderer.Demo
{
	public class EntityModifyHelper : MonoBehaviour
	{
		[ShowInInspector]
		[ValueDropdown(nameof(GetAllEntityNames))]
		private string _firstEntityName;

		[ShowInInspector]
		[ValueDropdown(nameof(GetAllEntityNames))]
		private string _secondEntityName;

		[Button]
		public void SetParent()
		{
			var em = GetEntityManager();
			var e1 = GetEntityByName(_firstEntityName);
			var e2 = GetEntityByName(_secondEntityName);

			if (em.HasComponent<Parent>(e1))
			{
				em.SetComponentData(e1, new Parent { Value = e2 });
			}
			else
			{
				em.AddComponentData(e1, new Parent { Value = e2 });
			}
		}

		[Button]
		public void DestroyEntity()
		{
			GetEntityManager().DestroyEntity(GetEntityByName(_firstEntityName));
		}

		private Entity GetEntityByName(string entityName)
		{
			var em = GetEntityManager();

			foreach (var entity in em.GetAllEntities())
			{
				if (em.GetName(entity) == entityName)
				{
					return entity;
				}
			}

			throw new Exception($"No Entity with name {entityName} exists.");
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