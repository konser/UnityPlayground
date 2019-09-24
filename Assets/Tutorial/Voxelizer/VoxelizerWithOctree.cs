using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class VoxelizerWithOctree : MonoBehaviour
{
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
        GameObject go;

        // -- for debug
        Stopwatch stopwatch = Stopwatch.StartNew();
        int objIndex = 0;
        int intersectCount = 0;

        foreach (KeyValuePair<GameObject, TriangleInfo[]> tPair in objDic)
        {
            objIndex++;
            intersectCount = 0;
            go = tPair.Key;
            Bounds bound = go.GetComponent<Renderer>().bounds;
            AABBBoundBox objBox = new AABBBoundBox(bound.min, bound.max);
            var l = octree.GetIntersections(objBox);
            for (int i = 0; i < l.Count; i++)
            {
                foreach (TriangleInfo tInfo in tPair.Value)
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
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Voxelize", $"No.{objIndex} Object,Intersect Count {intersectCount}/{l.Count}",
                    (float)intersectCount / l.Count);
                if (cancel)
                {
                    break;
                }
            }
        }
        stopwatch.Stop();
        EditorUtility.ClearProgressBar();
        Debug.Log($"体素化完成，耗时:{stopwatch.ElapsedMilliseconds}ms");
    }

    private void OnDrawGizmosSelected()
    {
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
        octree.DebugDraw();
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
