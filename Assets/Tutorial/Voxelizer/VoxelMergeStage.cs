﻿using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 为了序列化
/// </summary>
[System.Serializable]
public struct SerializableVec3
{
    public float x;
    public float y;
    public float z;
    public SerializableVec3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializableVec3(float x, float y)
    {
        this.x = x;
        this.y = y;
        this.z = 0f;
    }

    public SerializableVec3(Vector3 v3)
    {
        x = v3.x;
        y = v3.y;
        z = v3.z;
    }

    public SerializableVec3(Vector2 v2)
    {
        x = v2.x;
        y = v2.y;
        z = 0f;
    }
    
    public SerializableVec3 XZ()
    {
        return new SerializableVec3(x,0,z);
    }

    public static implicit operator SerializableVec3(Vector3 v)
    {
        return new SerializableVec3(v);
    }

    public static implicit operator SerializableVec3(Vector2 v)
    {
        return new SerializableVec3(v);
    }


    public static implicit operator Vector3(SerializableVec3 v)
    {
        return new Vector3(v.x,v.y,v.z);
    }

    public static implicit operator Vector2(SerializableVec3 v)
    {
        return new Vector2(v.x,v.y);
    }
}

[System.Serializable]
public struct VoxelSpan
{
    public int x;
    public int z;
    /// <summary>
    /// 上下表面高度 x-下 y-上
    /// </summary>
    public List<SerializableVec3> spanList;

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
    public bool bFirstBottomToZero = true;
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
        voxelSpan.spanList = new List<SerializableVec3>();

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
        if (bFirstBottomToZero)
        {
            voxelSpan.SetFirstSpanBottomToZero();
        }
        _mergedVoxelsSpan[x, z] = voxelSpan;
        lock (_locker)
        {
            complete++;
        }
    }
}
