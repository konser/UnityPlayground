using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Debug = UnityEngine.Debug;

public class VoxelizerWithOctree : MonoBehaviour
{
    public bool enableMultiThreading;
    public bool showOctree;
    public BoxCollider voxelBoundBox;

    public int xDimense;

    public int yDimense;

    public int zDimense;

    private Vector3 _halfVector;
    private VoxelBound[,,] _voxelArray;
    private Dictionary<GameObject,TriangleInfo[]> objDic = new Dictionary<GameObject, TriangleInfo[]>();
    private OctTree<VoxelBound> octree;
    [ContextMenu("VoxelizeWithBoundVolumeOctree")]
    public void Voxelize()
    {
        octree = new OctTree<VoxelBound>(new AABBBoundBox(voxelBoundBox.bounds));
        InitVoxelArray();
        foreach (VoxelBound tBound in _voxelArray)
        {
            octree.Insert(tBound);
        }
        octree.UpdateTree();
        InitTriangleInfoDic();
        IntersectTest();
    }

    private void InitTriangleInfoDic()
    {
        MeshFilter[] meshFilters = this.transform.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in meshFilters)
        {
            objDic.Add(filter.gameObject, IntersectionTest.GetTriangles(filter));
        }
    }

    private void InitVoxelArray()
    {
        Vector3 originPos = voxelBoundBox.bounds.min;
        Vector3 size = voxelBoundBox.bounds.size;
        _halfVector = new Vector3(size.x / xDimense, size.y / yDimense, size.z / zDimense) * 0.5f;
        _voxelArray = new VoxelBound[xDimense, yDimense, zDimense];
        for (int i = 0; i < xDimense; i++)
        {
            for (int j = 0; j < yDimense; j++)
            {
                for (int z = 0; z < zDimense; z++)
                {
                    Vector3 minPos = originPos + new Vector3(_halfVector.x * 2 * i, _halfVector.y * 2 * j, _halfVector.z * 2 * z);
                    VoxelBound bound = new VoxelBound()
                    {
                        overlapWithTriangle = false,
                        boundBox = new AABBBoundBox(minPos, minPos + 2 * _halfVector)
                    };
                    _voxelArray[i, j, z] = bound;
                }
            }
        }
    }
    private void IntersectTest()
    {
#if UNITY_EDITOR
        GameObject go;

        // -- for debug
        Stopwatch stopwatch = Stopwatch.StartNew();
        int objIndex = 0;
        int intersectCount = 0;
        List<Task> tasks = new List<Task>();
        foreach (KeyValuePair<GameObject, TriangleInfo[]> pair in objDic)
        {
            if (pair.Value == null)
            {
                continue;
            }
            // 每个物体多线程相交测试
            if (enableMultiThreading)
            {
                tasks.Add(Task.Factory.StartNew(MultiThreadingIntersectTest, new ObjInfo(pair)));
                continue;
            }
            // 单线程相交
            objIndex++;
            intersectCount = 0;
            go = pair.Key;
            Bounds bound = go.GetComponent<Renderer>().bounds;
            AABBBoundBox objBox = new AABBBoundBox(bound.min, bound.max);
            var l = octree.GetIntersections(objBox);
            for (int i = 0; i < l.Count; i++)
            {
                foreach (TriangleInfo tInfo in pair.Value)
                {
                    if (l[i].overlapWithTriangle)
                    {
                        continue;
                    }
                    if (IntersectionTest.AABBTriangle(l[i].boundBox, tInfo))
                    {
                        l[i].overlapWithTriangle = true;
                    }
                }
                intersectCount++;
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Voxelize", $"No.{objIndex} Object (of {objDic.Count}), Intersect Count {intersectCount}/{l.Count}",
                    (float)intersectCount / l.Count);
                if (cancel)
                {
                    break;
                }
            }
        }
        Task.WaitAll(tasks.ToArray());
        stopwatch.Stop();
        EditorUtility.ClearProgressBar();
        Debug.Log($"体素化完成，耗时:{stopwatch.ElapsedMilliseconds}ms");
#endif
    }

    #region 多线程
    public class ObjInfo
    {
        public AABBBoundBox boundBox;
        public TriangleInfo[] triangles;
        public ObjInfo(KeyValuePair<GameObject, TriangleInfo[]> pair)
        {
            GameObject go = pair.Key;
            Bounds bound = go.GetComponent<Renderer>().bounds;
            boundBox = new AABBBoundBox(bound.min, bound.max);
            triangles = pair.Value;
        }
    }
    void MultiThreadingIntersectTest(object p)
    {
        ObjInfo info = (ObjInfo)p;
        var l = octree.GetIntersections(info.boundBox);
        for (int i = 0; i < l.Count; i++)
        {
            foreach (TriangleInfo tInfo in info.triangles)
            {

                if (l[i].overlapWithTriangle)
                {
                    continue;
                }
                if (IntersectionTest.AABBTriangle(l[i].boundBox, tInfo))
                {
                    l[i].overlapWithTriangle = true;
                }
            }
        }
        Debug.Log($"Thread complete : {Thread.CurrentThread.ManagedThreadId}");
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (Application.isPlaying == false || this.enabled == false)
        {
            return;
        }
        if (_voxelArray == null)
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
        if (showOctree)
        {
            octree.DebugDraw();
        }
#endif
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
}
