using System;
using UnityEngine;
using System.Collections;

public class BoxObject : MonoBehaviour,ICollsionObject
{
    public AABBBoundBox boundBox;

    public AABBBoundBox GetBoundBox()
    {
        if (boundBox == null)
        {
            Bounds bound = this.GetComponent<BoxCollider>().bounds;
            boundBox = new AABBBoundBox(bound);
        }
        return boundBox;
    }

    public bool Intersect(ICollsionObject other)
    {
        return this.boundBox.Overlap(other.GetBoundBox());
    }
}
