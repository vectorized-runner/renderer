using Unity.Entities;
using UnityEngine;

namespace Renderer
{
	/// <summary>
	/// Using GL draw methods only work properly when called within 'OnRenderObject' method,
	/// therefore we use an empty gameObject as a helper class.
	/// </summary>
	public class AABBDrawHelper : MonoBehaviour
	{
		private void OnRenderObject()
		{
			Debug.Log("AAAAAA");

			var aabbSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<AABBDebugDrawSystem>();
			aabbSystem._lineMaterial.SetPass(0);

			GL.PushMatrix();
			GL.MultMatrix(transform.localToWorldMatrix);
			GL.Begin(GL.LINES);
			{
			}
			GL.End();
			GL.PopMatrix();
		}
	}
}