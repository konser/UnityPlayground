using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VoxelBound
{
    public AABBBoundBox boundBox;
    public bool overlapWithTriangle;
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
        IntersectTest();
        Display();
    }

    private void Display()
    {
        Vector3 size = _halfVector * 2;
        foreach (VoxelBound bound in _voxelArray)
        {
            if (bound.overlapWithTriangle)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = size;
                go.transform.position = bound.boundBox.center;
            }
        }
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
                        overlapWithTriangle =  false,
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

    private Vector3[] _etestCache = new Vector3[9];
    public void IntersectTest()
    {
        Vector3 e0 = Vector3.right;
        Vector3 e1 = Vector3.up;
        Vector3 e2 = Vector3.forward;
        foreach (TriangleInfo triangle in _triangleList)
        {
            foreach (VoxelBound voxelBound in _voxelArray)
            {
                // 已经相交的 不再检查
                if (voxelBound.overlapWithTriangle == true)
                {
                    continue;
                }

                // Triangle , Voxel AABB Test
                if (!voxelBound.boundBox.Overlap(triangle.boundBox))
                {
                    continue;
                }

                Vector3 v0 = triangle.p1 - voxelBound.boundBox.center;
                Vector3 v1 = triangle.p2 - voxelBound.boundBox.center;
                Vector3 v2 = triangle.p3 - voxelBound.boundBox.center;
                Vector3 f0 = v1 - v0, f1 = v2 - v1, f2 = v0 - v2;


                // Normal Plane Test
                //float d = Vector3.Dot(v0, triangle.normal);
                //float signedDistance = d;
                //float e = voxelBound.boundBox.half.x * Mathf.Abs(triangle.normal.x) +
                //          voxelBound.boundBox.half.y * Mathf.Abs(triangle.normal.y) +
                //          voxelBound.boundBox.half.z * Mathf.Abs(triangle.normal.z);
                //if (signedDistance - e > 0 || signedDistance - e < 0)
                //{
                //    continue;
                //}

                // Nine other tests
                _etestCache[0] = Vector3.Cross(e0, f0);
                _etestCache[1] = Vector3.Cross(e0, f1);
                _etestCache[2] = Vector3.Cross(e0, f2);
                _etestCache[3] = Vector3.Cross(e1, f0);
                _etestCache[4] = Vector3.Cross(e1, f1);
                _etestCache[5] = Vector3.Cross(e1, f2);
                _etestCache[6] = Vector3.Cross(e2, f0);
                _etestCache[7] = Vector3.Cross(e2, f1);
                _etestCache[8] = Vector3.Cross(e2, f2);

                bool intersect = true;
                for (int i = 0; i < 9; i++)
                {
                    float p0 = Vector3.Dot(_etestCache[i], v0);
                    float p1 = Vector3.Dot(_etestCache[i], v1);
                    float p2 = Vector3.Dot(_etestCache[i], v2);
                    float r = voxelBound.boundBox.half.x * Mathf.Abs(_etestCache[i].x) +
                              voxelBound.boundBox.half.y * Mathf.Abs(_etestCache[i].y) +
                              voxelBound.boundBox.half.z * Mathf.Abs(_etestCache[i].z);

                    if (Mathf.Min(Mathf.Min(p0, p1), p2) > r || Mathf.Max(Mathf.Max(p0, p1), p2) < -r)
                    {
                        intersect = false;
                        break;
                    }
                }

                if (intersect == false)
                {
                    continue;
                }
                // Pass all test above, the voxel overlap with the triangle
                voxelBound.overlapWithTriangle = true;
                Debug.Log("相交");
                break;
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
            if (tBound.overlapWithTriangle == true)
            {
                Handles.DrawWireCube(tBound.boundBox.center, tBound.boundBox.half * 2);

            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
