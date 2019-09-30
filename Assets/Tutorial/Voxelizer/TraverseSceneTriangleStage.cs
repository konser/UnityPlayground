using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public struct TriangleInfo
{
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
}

/// <summary>
/// 体素化 - 场景遍历阶段
/// </summary>
public class TraverseSceneTriangleStage
{
    public AABBBoundBox validAreaBox;
    public List<TriangleInfo> triangles
    {
        get
        {
            if (_triangleList == null || _triangleList.Count == 0)
            {
                Debug.LogError("无三角形！");
                return null;
            }
            return _triangleList;
        }
    }

    private class MeshInfo
    {
        public Mesh mesh;
        public Vector3[] vertices;
        public int[] tris;
        public Matrix4x4 matrix;
    }

    private List<TriangleInfo>  _triangleList = new List<TriangleInfo>();
    private static object _locker = new object();
    private int _completeCount;
    public List<TriangleInfo> RetreiveSceneTriangles()
    {
        _triangleList.Clear();

        // 创建用于线程里的数据
        int index = 0;
        List<MeshInfo> meshList = new List<MeshInfo>();

        // MeshFilter
        MeshFilter[] meshFilters = GameObject.FindObjectsOfType<MeshFilter>();
        foreach (MeshFilter filter in meshFilters)
        {
            AABBBoundBox gameObjectBound = new AABBBoundBox(filter.gameObject.GetComponent<Renderer>().bounds);
            if (!validAreaBox.Contains(gameObjectBound) && !validAreaBox.Overlap(gameObjectBound))
            {
                continue;
            }
            MeshInfo meshInfo = FromMeshFilter(filter);
            if (meshInfo != null)
            {
                meshList.Add(meshInfo);
            }
        }

        List<Task> tasks = new List<Task>();
        foreach (MeshInfo meshInfo in meshList)
        {
            tasks.Add(Task.Factory.StartNew(GetTriangles, meshInfo));
        }

        //-----Progress bar
        int previousCount = 0;
        while (_completeCount < tasks.Count)
        {
            if (previousCount != _completeCount)
            {
                previousCount = _completeCount;
                EditorUtility.DisplayProgressBar("Traverse Triangle", "", (float)_completeCount / tasks.Count);
            }
        }
        EditorUtility.ClearProgressBar();
        //------------------------------------
        return _triangleList;
    }

    private MeshInfo FromMeshFilter(MeshFilter filter)
    {
        Mesh mesh = filter.sharedMesh;
        if (mesh == null || mesh.vertices == null)
        {
            return null;
        }
        MeshInfo meshInfo = new MeshInfo();
        meshInfo.mesh = mesh;
        meshInfo.vertices = mesh.vertices;
        meshInfo.tris = mesh.triangles;
        meshInfo.matrix = filter.transform.localToWorldMatrix;
        return meshInfo;
    }

    private void GetTriangles(object obj)
    {
        MeshInfo meshInfo = (MeshInfo)obj;
        Vector3[] vertices = meshInfo.vertices;
        int[] tris = meshInfo.tris;
        TriangleInfo[] triangles = new TriangleInfo[tris.Length / 3];
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v1 = GetVertexWorldPosition(vertices[tris[i]],meshInfo.matrix);
            Vector3 v2 = GetVertexWorldPosition(vertices[tris[i+1]], meshInfo.matrix);
            Vector3 v3 = GetVertexWorldPosition(vertices[tris[i+2]], meshInfo.matrix);
            triangles[i / 3] = new TriangleInfo
            {
                p1 = v1,
                p2 = v2,
                p3 = v3,
            };
        }
        lock (_locker)
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                _triangleList.Add(triangles[i]);
            }
            _completeCount++;
        }
    }

    private Vector3 GetVertexWorldPosition(Vector3 vertex,Matrix4x4 mat)
    {
        return mat.MultiplyPoint3x4(vertex);
    }
}
