using System;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography.X509Certificates;

[System.Serializable]
public class Box3i
{
    public MyVec3 minP;
    public MyVec3 maxP;
    public float xRange;
    public float yRange;
    public float zRange;
    public Box3i(Vector3 min, Vector3 max)
    {
        minP = min;
        maxP = max;
        xRange = (float)max.x - min.x;
        yRange = (float)max.y - min.y;
        zRange = (float)max.z - min.z;
    }

    public Vector3Int GetMinPoint()
    {
        return new Vector3Int(Mathf.FloorToInt(minP.x), Mathf.FloorToInt(minP.y), Mathf.FloorToInt(minP.z));
    }

    public Vector3Int GetMaxPoint()
    {
        return new Vector3Int(Mathf.FloorToInt(maxP.x), Mathf.FloorToInt(maxP.y), Mathf.FloorToInt(maxP.z));
    }
}

[System.Serializable]
public class VoxelBuffer
{
    public float[] data;
    public Box3i extent;
    public Box3i dataWindow;
    public int xSize;
    public int ySize;
    public int zSize;

    public VoxelBuffer(int sx, int sy, int sz)
    {
        xSize = sx;
        ySize = sy;
        zSize = sz;
        data = new float[xSize * ySize * zSize];
        SetSize(new Box3i(new Vector3Int(0, 0, 0), new Vector3Int(xSize, ySize, zSize)) ,
                new Box3i(new Vector3Int(0, 0, 0), new Vector3Int(xSize, ySize, zSize)) );
    }

    public void SetSize(Box3i extent, Box3i dataWindow)
    {
        this.extent = extent;
        this.dataWindow = dataWindow;
    }

    public Vector3 VoxelToLocalSpace(float coordX,float coordY,float coordZ)
    {
        return new Vector3(coordX/extent.xRange,coordY/extent.yRange,coordZ/extent.zRange);
    }

    public int ContinusToDiscrete(float coord)
    {
        return Mathf.FloorToInt(coord);
    }

    public float DiscreteToContinus(int coord)
    {
        return coord + 0.5f;
    }

    public float this[int x,int y,int z]
    {
        get => GetValue(x,y,z);
        set => SetValue(x,y,z, value);
    }

    private float GetValue(int i, int j, int k)
    {
        return data[i + j * xSize + k * ySize * zSize];
    }

    private float SetValue(int i, int j, int k,float value)
    {
        data[i + j * xSize + k * ySize * zSize] = value;
        return value;
    }

    #region WriteData
    public static void WriteAdditive(ref VoxelBuffer buffer, int i, int j, int k, float value)
    {
        buffer[i, j, k] = buffer[i, j, k] + value;
    }


    #endregion
}
