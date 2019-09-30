using System.Collections.Generic;
using UnityEngine;

public struct TriangleBound : ICollsionObject
{
    public AABBBoundBox boundBox;
    public TriangleInfo[] triangles;
    public AABBBoundBox GetBoundBox()
    {
        return this.boundBox;
    }

    public bool Intersect(ICollsionObject other)
    {
        return this.boundBox.Overlap(other.GetBoundBox());
    }
}

/// <summary>
/// 体素化 - 空间分割阶段
/// </summary>
public class OctreeSceneStage
{
    public int triangleCountInOneGroup = 32;
    public OctTree<TriangleBound> sceneOctree
    {
        get
        {
            if (_octree == null)
            {
                Debug.LogError("未生成Octree");
            }
            return _octree;
        }
    }
    private OctTree<TriangleBound> _octree;
    public OctTree<TriangleBound> ConstructSceneOctree(List<TriangleInfo> sceneTriangles,Bounds bound)
    {
        int count = 0;
        AABBBoundBox maxBound = new AABBBoundBox(bound);
        _octree = new OctTree<TriangleBound>(maxBound);
        List<TriangleInfo> triangleList = new List<TriangleInfo>(triangleCountInOneGroup);

        foreach (TriangleInfo tri in sceneTriangles)
        {
            triangleList.Add(tri);
            if (triangleList.Count == triangleCountInOneGroup)
            {
                TriangleBound triangleBound = GroupBoundBox(triangleList);

                //boundList.Add(triangleBound);
                if (maxBound.Contains(triangleBound.GetBoundBox()) || maxBound.Overlap(triangleBound.boundBox))
                {
                    _octree.Insert(triangleBound);
                    count++;
                }
                triangleList.Clear();
            }
        }
        if (triangleList.Count != 0)
        {
            _octree.Insert(GroupBoundBox(triangleList));
            count++;
        }

        _octree.UpdateTree();
        return _octree;
    }

    private List<TriangleBound> boundList = new List<TriangleBound>();
    public void DebugDraw()
    {
        _octree?.DebugDraw();
    }

    private TriangleBound GroupBoundBox(List<TriangleInfo> infoList)
    {
        TriangleBound triangeBound = new TriangleBound();
        AABBBoundBox bound = new AABBBoundBox(Vector3.positiveInfinity,Vector3.negativeInfinity);
        triangeBound.triangles = new TriangleInfo[infoList.Count];

        for (int i = 0; i < infoList.Count; i++)
        {
            triangeBound.triangles[i] = infoList[i];
            bound.Extend(infoList[i].p1);
            bound.Extend(infoList[i].p2);
            bound.Extend(infoList[i].p3);
        }

        triangeBound.boundBox = bound;
        return triangeBound;
    }
}
