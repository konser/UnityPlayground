using UnityEngine;
using System.Collections;
using UnityEditor;

//public class BoundVolume
//{

//}

public class AABBBoundBox /*: BoundVolume*/
{
    public Vector3 center;
    public Vector3 min;
    public Vector3 max;

    public Vector3 half;
    public Vector3 size;

    public AABBBoundBox()
    {
        this.max = Vector3.zero;
        this.min = Vector3.zero;
        SetSize(max, min);
    }
    public AABBBoundBox(Bounds bound)
    {
        this.max = bound.max;
        this.min = bound.min;
        SetSize(max, min);
    }

    public AABBBoundBox(Vector3 min, Vector3 max)
    {
        this.max = max;
        this.min = min;
        SetSize(max, min);
    }

    public void Extend(Vector3 pos)
    {
        min.x = Mathf.Min(pos.x, min.x);
        min.y = Mathf.Min(pos.y, min.y);
        min.z = Mathf.Min(pos.z, min.z);

        max.x = Mathf.Max(pos.x, max.x);
        max.y = Mathf.Max(pos.y, max.y);
        max.z = Mathf.Max(pos.z, max.z);

        SetSize(max,min);
    }

    private void SetSize(Vector3 max, Vector3 min)
    {
        this.center = 0.5f * (max + min);
        this.size = max - min;
        this.half = 0.5f * (max - min);
    }

    public bool Contains(AABBBoundBox b)
    {
        if (this.min.x <= b.min.x && this.min.y <= b.min.y && this.min.z <= b.min.z
            && this.max.x >= b.max.x && this.max.y >= b.max.y && this.max.z >= b.max.z)
        {
            return true;
        }
        return false;
    }

    public bool Overlap(AABBBoundBox b)
    {
        if (this.min.x > b.max.x || b.min.x > this.max.x)
        {
            return false;
        }

        if (this.min.y > b.max.y || b.min.y > this.max.y)
        {
            return false;
        }

        if (this.min.z > b.max.z || b.min.z > this.max.z)
        {
            return false;
        }
        return true;
    }

    public void DebugDraw(Color color)
    {
        Handles.color = color;
        Handles.DrawWireCube(center,size);
    }
}
