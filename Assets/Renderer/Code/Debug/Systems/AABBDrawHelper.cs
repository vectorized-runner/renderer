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
		private Material _lineMaterial;

		private void Start()
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			Shader shader = Shader.Find("Hidden/Internal-Colored");
			_lineMaterial = new Material(shader);
			_lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			_lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			_lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			_lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			_lineMaterial.SetInt("_ZWrite", 0);
		}

		private void OnRenderObject()
		{
			var aabbSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<AABBDebugDrawSystem>();
			_lineMaterial.SetPass(0);

			GL.PushMatrix();
			GL.MultMatrix(transform.localToWorldMatrix);
			GL.Begin(GL.LINES);
			{
				aabbSystem.DrawAABBs();
			}
			GL.End();
			GL.PopMatrix();
		}
	}
}