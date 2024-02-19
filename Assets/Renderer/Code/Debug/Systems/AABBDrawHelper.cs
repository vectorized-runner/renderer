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

			int lineCount = 100;
			float radius = 3.0f;

			aabbSystem._lineMaterial.SetPass(0);

			GL.PushMatrix();
			// Set transformation matrix for drawing to
			// match our transform
			GL.MultMatrix(transform.localToWorldMatrix);

			// Draw lines
			GL.Begin(GL.LINES);
			for (int i = 0; i < lineCount; ++i)
			{
				float a = i / (float)lineCount;
				float angle = a * Mathf.PI * 2;
				// Vertex colors change from red to green
				GL.Color(new Color(a, 1 - a, 0, 0.8F));
				// One vertex at transform position
				GL.Vertex3(0, 0, 0);
				// Another vertex at edge of circle
				GL.Vertex3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
			}

			GL.End();
			GL.PopMatrix();
		}
	}
}