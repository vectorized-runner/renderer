using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Create a new mesh
        var mesh = new Mesh();

        // Define vertices for the lines
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),   // Start point of line 1
            new Vector3(1, 0, 0),   // End point of line 1
            new Vector3(0, 1, 0),   // Start point of line 2
            new Vector3(1, 1, 0)    // End point of line 2
            // Add more vertices for additional lines if needed
        };

        // Define indices for the lines
        int[] indices = new int[]
        {
            0, 1,   // Indices of line 1
            2, 3    // Indices of line 2
            // Add more indices for additional lines if needed
        };

        // Assign vertices and indices to the mesh
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);

        // Ensure that the mesh has normals (required for rendering)
        // mesh.RecalculateNormals();

        // Assign the mesh to a MeshFilter component
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = mesh;
        }
        else
        {
            Debug.LogError("MeshFilter component not found!");
        }
        
        Debug.Log("Running hello");
    }
}
