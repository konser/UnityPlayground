using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public struct VoxelSpan
{
    public int x;
    public int z;
    /// <summary>
    /// 上下表面高度 x-下 y-上
    /// </summary>
    public List<Vector3> spanList;

    public bool isEmpty
    {
        get { return spanList == null || spanList.Count == 0; }
    }
    public void SetFirstSpanBottomToZero()
    {
        if (spanList != null && spanList.Count >= 1)
        {
            spanList[0] = new Vector3(0f,spanList[0].y);
        }
    }
}


/// <summary>
/// 体素化 - 单个方块合并阶段
/// </summary>
public class VoxelMergeStage
{
    private float _voxelHeight = 0f;
    private bool[,,] _voxelArray = null;
    private int _xMax;
    private int _yMax;
    private int _zMax;
    private VoxelSpan[,] _mergedVoxelsSpan;
    private static object _locker = new Object();
    public void SetParams(Vector3 voxelSize, bool[,,] voxelArray)
    {
        _voxelHeight = voxelSize.y;
        _voxelArray = voxelArray;
        _xMax = voxelArray.GetLength(0);
        _yMax = voxelArray.GetLength(1);
        _zMax = voxelArray.GetLength(2);
        _mergedVoxelsSpan = new VoxelSpan[_xMax,_zMax];
    }
    private int complete = 0;
    public VoxelSpan[,] Merge()
    {
        List<Task>  tasks = new List<Task>();
        for (int x = 0; x < _xMax; x++)
        {
            for (int z = 0; z < _zMax; z++)
            {
                //MergeColunm(new Vector2Int(x,z));
                tasks.Add(Task.Factory.StartNew(MergeColunm, new Vector2Int(x, z)));
            }
        }
        return _mergedVoxelsSpan;
    }
    
    // 每一列体素的合并
    private void MergeColunm(object obj)
    {
        Vector2Int index =(Vector2Int) obj;
        int x = index.x;
        int z = index.y;

        VoxelSpan voxelSpan = new VoxelSpan();
        voxelSpan.x = x;
        voxelSpan.z = z;
        voxelSpan.spanList = new List<Vector3>();

        int voxelCount = 0;
        for (int i = 0; i < _yMax; i++)
        {
            if (_voxelArray[x, i, z])
            {
                voxelCount++;
            }
            else
            {
                if (voxelCount != 0)
                {
                    voxelSpan.spanList.Add(new Vector2((i-voxelCount)*_voxelHeight,i*_voxelHeight));
                }
                voxelCount = 0;
            }

            if (i == _yMax - 1 && voxelCount != 0)
            {
                voxelSpan.spanList.Add(new Vector2((i - voxelCount) * _voxelHeight, _yMax * _voxelHeight));
            }
        }
        voxelSpan.SetFirstSpanBottomToZero();
        _mergedVoxelsSpan[x, z] = voxelSpan;
        lock (_locker)
        {
            complete++;
        }
    }
}
