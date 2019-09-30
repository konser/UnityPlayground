using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 体素化 - 相交测试阶段
/// </summary>
public class IntersectTestStage
{
    public int divide = 16;
    private Vector3 _voxelSize;
    private Vector3 _originPos;
    private bool[,,] _voxels;
    private AABBBoundBox _region;
    private static object _locker = new object();
    private int _xMax;
    private int _yMax;
    private int _zMax;
    private OctTree<TriangleBound> _octree;
    private List<Task> tasks;
    private int _completeCount;


    public void SetParams(Vector3 voxelSize, OctTree<TriangleBound> tree)
    {
        _octree = tree;
        _voxelSize = voxelSize;
        _region = tree.region;
        _originPos = _region.min;
        _xMax = Mathf.RoundToInt(_region.size.x / _voxelSize.x);
        _yMax = Mathf.RoundToInt(_region.size.y / _voxelSize.y);
        _zMax = Mathf.RoundToInt(_region.size.z / _voxelSize.z);
        _voxels = new bool[_xMax, _yMax, _zMax];
    }

    public bool[,,] IntersectTest()
    {
        Stopwatch watch = Stopwatch.StartNew();
        int currentX = 0, currentY = 0, currentZ = 0;
        List<Tuple<int,int,int>> startIndex = new List<Tuple<int, int, int>>();
        for (int x = 0; x < _xMax; x+= divide)
        {
            for (int y = 0; y < _yMax; y+=divide)
            {
                for (int z = 0; z < _zMax; z+=divide)
                {
                    startIndex.Add(Tuple.Create(x,y,z));
                }
            }
        }
        tasks = new List<Task>();
        for (int i = 0; i < startIndex.Count; i++)
        {
            tasks.Add(Task.Factory.StartNew(IntersectTestDivide, startIndex[i]));
        }

        while (_completeCount < tasks.Count)
        {
            UnityEditor.EditorUtility.DisplayProgressBar("Intersection","",(float)_completeCount/tasks.Count);
        }
        return _voxels;
    }

    private void IntersectTestDivide(object obj)
    {
        Tuple<int, int, int> indexTuple = (Tuple<int, int, int>)obj;
        int startX = indexTuple.Item1;
        int startY = indexTuple.Item2;
        int startZ = indexTuple.Item3;
        int xRange = startX + divide;
        int yRange = startY + divide;
        int zRange = startZ + divide;

        xRange = xRange < _xMax ? xRange : _xMax;
        yRange = yRange < _yMax ? yRange : _yMax;
        zRange = zRange < _zMax ? zRange : _zMax;

        #region Naive Simple Violent Loop...
        for (int x = startX; x < xRange; x++)
        {
            for (int y = startY; y < yRange; y++)
            {
                for (int z = startZ; z < zRange; z++)
                {
                    if (_voxels[x, y, z])
                    {
                        continue;
                    }
                    AABBBoundBox boundBox = new AABBBoundBox(GetVoxelBoundMin(x, y, z), GetVoxelBoundMax(x, y, z));
                    List<TriangleBound> triangleBoundList = _octree.GetIntersections(boundBox);

                    for (int i = 0; i < triangleBoundList.Count; i++)
                    {
                        for (int j = 0; j < triangleBoundList[i].triangles.Length; j++)
                        {
                            if (IntersectionTest.AABBTriangle(boundBox, triangleBoundList[i].triangles[j]))
                            {
                                _voxels[x, y, z] = true;
                            }
                        }
                    }

                }
            }
        }
        #endregion

        lock (_locker)
        {
            _completeCount++;
        }
    }

    private Vector3 GetVoxelBoundMin(int x, int y, int z)
    {
        return _originPos + new Vector3(_voxelSize.x * x, _voxelSize.y * y, _voxelSize.z * z);
    }

    private Vector3 GetVoxelBoundMax(int x, int y, int z)
    {
        return _originPos + new Vector3(_voxelSize.x * x, _voxelSize.y * y, _voxelSize.z * z) + _voxelSize;
    }

    private Dictionary<Vector3Int, AABBBoundBox> _drawDic = new Dictionary<Vector3Int, AABBBoundBox>();
    public void DebugDraw()
    {
        if (_voxels != null)
        {
            for (int x = 0; x < _voxels.GetLength(0); x++)
            {
                for (int y = 0; y < _voxels.GetLength(1); y++)
                {
                    for (int z = 0; z < _voxels.GetLength(2); z++)
                    {
                        if (_voxels[x, y, z] == false)
                        {
                            continue;
                        }
                        Vector3Int vec = new Vector3Int(x, y, z);
                        if (!_drawDic.ContainsKey(vec))
                        {
                            _drawDic.Add(vec,
                                new AABBBoundBox(GetVoxelBoundMin(x, y, z),GetVoxelBoundMax(x, y, z)));
                        }
                        _drawDic[vec].DebugDraw(Color.white);
                    }
                }
            }
        }
    }
}
