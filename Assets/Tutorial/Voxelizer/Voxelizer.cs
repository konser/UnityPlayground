using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VoxelBound
{
    public AABBBoundBox boundBox;
    public bool isBound;
}

public class TriangleInfo
{
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public Vector3 normal;
    public AABBBoundBox boundBox;
}

public class Voxelizer : MonoBehaviour
{
    public BoxCollider voxelBoundBox;

    public int xDimense;

    public int yDimense;

    public int zDimense;

    private Vector3 _halfVector;
    private VoxelBound[,,] _voxelArray;

    private List<TriangleInfo> _triangleList;
    // Start is called before the first frame update
    void Start()
    {
        InitVoxelArray();
        InitTriangleList();
    }

    public void InitVoxelArray()
    {
        Vector3 originPos = voxelBoundBox.bounds.min;
        Vector3 size = voxelBoundBox.bounds.size;
        _halfVector = new Vector3(size.x / xDimense, size.y / yDimense, size.z / zDimense) * 0.5f;
        _voxelArray = new VoxelBound[xDimense,yDimense,zDimense];
        for (int i = 0; i < xDimense; i++)
        {
            for (int j = 0; j < yDimense; j++)
            {
                for (int z = 0; z < zDimense; z++)
                {
                    Vector3 minPos = originPos +  new Vector3(_halfVector.x*2*i, _halfVector.y * 2 * j, _halfVector.z * 2 * z);
                    _voxelArray[i, j, z] = new VoxelBound()
                    {
                        isBound =  false,
                        boundBox =  new AABBBoundBox(minPos,minPos + 2*_halfVector)
                    };
                }
            }
        }
    }

    public void InitTriangleList()
    {
        _triangleList = new List<TriangleInfo>();
        MeshFilter[] meshFilters = this.transform.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in meshFilters)
        {
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i+=3)
            {
                Vector3 v1 = filter.transform.TransformPoint(vertices[tris[i]]);
                Vector3 v2 = filter.transform.TransformPoint(vertices[tris[i+1]]);
                Vector3 v3 = filter.transform.TransformPoint(vertices[tris[i+2]]);
                Vector3 u = v2 - v3;
                Vector3 v = v3 - v1;
                Vector3 normal = Vector3.Cross(u,v).normalized;

                AABBBoundBox box = new AABBBoundBox(Vector3.positiveInfinity,Vector3.negativeInfinity);
                box.Extend(v1);
                box.Extend(v2);
                box.Extend(v3);
                _triangleList.Add(new TriangleInfo
                {
                    normal = normal,
                    p1 = v1,
                    p2 = v2,
                    p3 = v3,
                    boundBox = box
                });
            }
        }
    }

    public void IntersectTest()
    {
        Vector3 e0 = Vector3.right;
        Vector3 e1 = Vector3.up;
        Vector3 e2 = Vector3.forward;
        foreach (VoxelBound voxelBound in _voxelArray)
        {
            foreach (TriangleInfo triangle in _triangleList)
            {
                Vector3 v0 = triangle.p1 - voxelBound.boundBox.center;
                Vector3 v1 = triangle.p2 - voxelBound.boundBox.center;
                Vector3 v2 = triangle.p3 - voxelBound.boundBox.center;
                Vector3 f0 = v1 - v0, f1 = v2 - v1, f2 = v0 - v2;


                // Triangle Minium AABB Test


                // Normal Plane Test

                // Nine other tests
                Vector3 a00 = Vector3.Cross(e0, f0);
                Vector3 a01 = Vector3.Cross(e0, f1);
                Vector3 a02 = Vector3.Cross(e0, f2);

                Vector3 a10 = Vector3.Cross(e1, f0);
                Vector3 a11 = Vector3.Cross(e1, f1);
                Vector3 a12 = Vector3.Cross(e1, f2);

                Vector3 a20 = Vector3.Cross(e2, f0);
                Vector3 a21 = Vector3.Cross(e2, f1);
                Vector3 a22 = Vector3.Cross(e2, f2);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying == false)
        {
            return;
        }
        foreach (VoxelBound tBound in _voxelArray)
        {
            Handles.DrawWireCube(tBound.boundBox.center,tBound.boundBox.half*2);
        }

        foreach (TriangleInfo tTriangleInfo in _triangleList)
        {
            Handles.DrawWireCube(tTriangleInfo.boundBox.center,tTriangleInfo.boundBox.size);
            //Handles.DrawLine(tTriangleInfo.p1,tTriangleInfo.p1 + tTriangleInfo.normal);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
