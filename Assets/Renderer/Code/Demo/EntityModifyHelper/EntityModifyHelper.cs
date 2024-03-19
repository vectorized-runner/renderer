using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
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
		public void ScaleUp()
		{
			var em = GetEntityManager();
			var e1 = GetEntityByName(_firstEntityName);
			var tf = em.GetComponentData<LocalTransform>(e1);
			tf.Scale *= 2;
			em.SetComponentData(e1, tf);
		}

		[Button]
		public void ScaleDown()
		{
			var em = GetEntityManager();
			var e1 = GetEntityByName(_firstEntityName);
			var tf = em.GetComponentData<LocalTransform>(e1);
			tf.Scale *= 0.5f;
			em.SetComponentData(e1, tf);
		}

		[Button]
		public void RotateAround()
		{
			var em = GetEntityManager();
			var e1 = GetEntityByName(_firstEntityName);
			var tf = em.GetComponentData<LocalTransform>(e1);
			tf.Rotation = math.mul(tf.Rotation, quaternion.RotateY(math.radians(30.0f)));
			em.SetComponentData(e1, tf);
		}

		[Button]
		public void MoveUp()
		{
			var em = GetEntityManager();
			var e1 = GetEntityByName(_firstEntityName);
			var tf = em.GetComponentData<LocalTransform>(e1);
			tf.Position += new float3(0, 1, 0);
			em.SetComponentData(e1, tf);
		}

		[Button]
		public void RemoveParent()
		{
			var em = GetEntityManager();
			var e1 = GetEntityByName(_firstEntityName);

			if (em.HasComponent<Parent>(e1))
			{
				em.RemoveComponent<Parent>(e1);
			}
		}

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