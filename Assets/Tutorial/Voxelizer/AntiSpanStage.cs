using System.Collections.Generic;
using UnityEngine;


public class AntiSpanStage
{
    private VoxelSpan[,] _antiSpans;
    
    public VoxelSpan[,] ConstructAntiSpan(VoxelSpan[,] voxels,float maxHeight)
    {
        _antiSpans = new VoxelSpan[voxels.GetLength(0),voxels.GetLength(1)];
        foreach (VoxelSpan voxel in voxels)
        {
            //该坐标没有体素 则为一个完整的反体素
            if (voxel.isEmpty)
            {
                _antiSpans[voxel.x, voxel.z] = new VoxelSpan
                {
                    x = voxel.x,
                    z = voxel.z,
                    spanList = new List<Vector3>()
                    {
                        new Vector2(0f, maxHeight)
                    }
                };
            }
            else
            {
                _antiSpans[voxel.x, voxel.z] = GetAntiSpan(voxel,maxHeight);
            }
        }
        return _antiSpans;
    }

    private VoxelSpan GetAntiSpan(VoxelSpan voxelSpan,float maxHeight)
    {
        VoxelSpan antiSpan = new VoxelSpan();
        antiSpan.x = voxelSpan.x;
        antiSpan.z = voxelSpan.z;
        antiSpan.spanList = new List<Vector3>();
        int spanCount = voxelSpan.spanList.Count;

        for (int i = 0; i < spanCount; i++)
        {
            Vector2 spanHeight = voxelSpan.spanList[i];

            if (spanHeight.x != 0)
            {
                if (i + 1 < spanCount)
                {
                    if (i == 0)
                    {
                        antiSpan.spanList.Add(new Vector2(0f,voxelSpan.spanList[i].x));
                    }
                    else
                    {
                        antiSpan.spanList.Add(new Vector2(voxelSpan.spanList[i].y, voxelSpan.spanList[i + 1].x));
                    }
                }
                
            }
            else
            {
                if (i + 1 < spanCount)
                {
                    antiSpan.spanList.Add(new Vector2(spanHeight.y,voxelSpan.spanList[i+1].x));
                }
            }

            if (i == spanCount - 1)
            {
                if (spanHeight.y < maxHeight)
                {
                    antiSpan.spanList.Add(new Vector2(spanHeight.y, maxHeight));
                }
            }
        }
        return antiSpan;
    }
}
