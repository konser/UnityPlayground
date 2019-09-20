using UnityEngine;
using System.Collections;

public class AABBBoundBox
{
    public Vector3 center;
    public Vector3 min;
    public Vector3 max;

    public Vector3 half;
    public Vector3 size;

    public AABBBoundBox(Vector3 min, Vector3 max)
    {
        this.max = max;
        this.min = min;
        this.center = 0.5f * (max + min);
        this.size = max - min;
        this.half = 0.5f * (max - min);
    }

    public void Extend(Vector3 pos)
    {
        min.x = Mathf.Min(pos.x, min.x);
        min.y = Mathf.Min(pos.y, min.y);
        min.z = Mathf.Min(pos.z, min.z);

        max.x = Mathf.Max(pos.x, max.x);
        max.y = Mathf.Max(pos.y, max.y);
        max.z = Mathf.Max(pos.z, max.z);

        this.center = 0.5f * (max + min);
        this.size = max - min;
        this.half = 0.5f * (max - min);
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
}
