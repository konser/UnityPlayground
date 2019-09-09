using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MeshFilter meshFilter;

    public Mesh mesh;

    public Vector3[] testVertex;

    public int[] indices;
    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        meshFilter = this.GetComponent<MeshFilter>();
        meshFilter.mesh.Clear();
        meshFilter.mesh = mesh;
        mesh.vertices = testVertex;
        mesh.triangles = indices;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
