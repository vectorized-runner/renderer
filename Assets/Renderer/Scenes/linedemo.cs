using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class linedemo : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		var dataArray = Mesh.AllocateWritableMeshData(1);
		var data = dataArray[0];
		data.SetVertexBufferParams(12, new VertexAttributeDescriptor(VertexAttribute.Position));
		var p0 = new Vector3(0, 0, 0);
		var p1 = new Vector3(1, 1, 1);
		var pos = data.GetVertexData<Vector3>();
		pos[0] = p0;
		pos[1] = p1;
		data.SetIndexBufferParams(64, IndexFormat.UInt32);
		var ib = data.GetIndexData<int>();

		Debug.Log(ib.Length);
		Debug.Log("aaa");
		
		
		for (ushort i = 0; i < ib.Length; ++i)
			ib[i] = i;

		
		// One sub-mesh with all the indices.
		data.subMeshCount = 1;
		Debug.Log($"start {ib.Length}");
		data.SetSubMesh(0, new SubMeshDescriptor(0, ib.Length, MeshTopology.Lines));
		
		// Create the mesh and apply data to it:
		var mesh = new Mesh();
		Mesh.ApplyAndDisposeWritableMeshData(dataArray, mesh);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		GetComponent<MeshFilter>().mesh = mesh;
	}
}