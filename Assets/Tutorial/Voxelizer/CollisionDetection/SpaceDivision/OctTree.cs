﻿using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public interface ICollsionObject
{
    AABBBoundBox GetBoundBox();
    bool Intersect(ICollsionObject other);
}

public class OctTree<T> where T:ICollsionObject
{
    private int treeDepth = 1;
    // 队列里还有未插入对象时，为false
    private static bool s_treeReady = false;
    // 有没有已经建立好的树
    private static bool s_treeBuilt = false;
    // 即将插入到树结构的对象
    static  Queue<T> s_pendingInsertion = new Queue<T>();
    private AABBBoundBox _region;
    private List<T> objectList;
    private OctTree<T> parent;
    // 子节点
    private OctTree<T>[] childNodes = new OctTree<T>[8];
    // bitmask 指示哪些子节点被使用
    private byte activeNodesMask = 0;
    // 最小区域 1x1x1 的方块
    private const int MIN_SIZE = 1;
    // 删除空分支的等待帧数，最大为64，每当节点被使用时，该时间翻倍
    public int _maxLifespan = 8;
    // countdown
    private int _curLife = -1;

    public AABBBoundBox region
    {
        get { return _region; }
    }

    public OctTree(AABBBoundBox region, List<T> objList)
    {
        this._region = region;
        objectList = objList;
        _curLife = -1;
    }

    public OctTree()
    {
        _region = new AABBBoundBox(Vector3.zero, Vector3.zero);
        objectList = new List<T>();
        _curLife = -1;
    }

    public OctTree(AABBBoundBox region)
    {
        s_treeBuilt = false;
        s_treeReady = false;
        this._region = region;
        objectList = new List<T>();
        _curLife = -1;
    }

    public void UpdateTree()
    {
        Stopwatch stopWatch = Stopwatch.StartNew();
        // 没建好树，建树
        if (s_treeBuilt == false)
        {
            while (s_pendingInsertion.Count != 0)
            {
                objectList.Add(s_pendingInsertion.Dequeue());
            }
            BuildTree();
        }
        // 已经建好，插入
        else
        {
            while (s_pendingInsertion.Count != 0)
            {
                Insert(s_pendingInsertion.Dequeue());
            }
        }
        s_treeReady = true;
        stopWatch.Stop();
        Debug.Log($"Octree构造耗时{stopWatch.ElapsedMilliseconds}");
    }

    private void BuildTree()
    {
        if (objectList.Count <= 1)
        {
            return;
        }

        Vector3 dimense = _region.size;
        if (dimense == Vector3.zero)
        {
            //如果该节点的region为0，则找它的包含区域
            // _region = FindEnclosingRegion();
            dimense = _region.size;
        }

        if (dimense.x <= MIN_SIZE && dimense.y <= MIN_SIZE && dimense.z <= MIN_SIZE)
        {
            return;
        }

        Vector3 half = _region.half;
        Vector3 center = _region.center;

        AABBBoundBox[] octant = new AABBBoundBox[8];
        octant[0] = new AABBBoundBox(_region.min,center);
        octant[1] = new AABBBoundBox(new Vector3(center.x,_region.min.y,_region.min.z), new Vector3(_region.max.x,center.y,center.z));
        octant[2] = new AABBBoundBox(new Vector3(center.x,_region.min.y,center.z),new Vector3(_region.max.x,center.y,_region.max.z));
        octant[3] = new AABBBoundBox(new Vector3(_region.min.x,_region.min.y,center.z),new Vector3(center.x,center.y,_region.max.z));

        octant[4] = new AABBBoundBox(new Vector3(_region.min.x,center.y,_region.min.z),new Vector3(center.x,_region.max.y,center.z));
        octant[5] = new AABBBoundBox(new Vector3(center.x,center.y,_region.min.z),new Vector3(_region.max.x,_region.max.y,center.z));
        octant[6] = new AABBBoundBox(center,_region.max);
        octant[7] = new AABBBoundBox(new Vector3(_region.min.x,center.y,center.z), new Vector3(center.x,_region.max.y,_region.max.z));

        List<T>[] octListArray = new List<T>[8];
        for (int i = 0; i < 8; i++)
        {
            octListArray[i] = new List<T>();
        }

        List<T> delist = new List<T>();

        // 当前节点的对象划分至子区域中
        foreach (T obj in objectList)
        {
            if (obj.GetBoundBox().size != Vector3.zero)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (octant[i].Contains(obj.GetBoundBox()))
                    {
                        octListArray[i].Add(obj);
                        delist.Add(obj);
                        break;
                    }
                }
            }
        }

        // 被添加子区域的对象从当前的节点中移除
        foreach (T obj in delist)
        {
            objectList.Remove(obj);
        }

        // 对象数不为0的区域建立八叉树节点
        for (int i = 0; i < 8; i++)
        {
            if (octListArray[i].Count != 0)
            {
                childNodes[i] = CreateNode(octant[i], octListArray[i]);
                activeNodesMask |= (byte)(1 << i);
                childNodes[i].BuildTree();
            }
        }

        s_treeBuilt = true;
        s_treeReady = true;
    }

    private OctTree<T> CreateNode(AABBBoundBox region, List<T> objList)
    {
        if (objList.Count == 0)
        {
            return null;
        }
        OctTree<T> ret = new OctTree<T>(region,objList);
        ret.parent = this;
        ret.treeDepth = treeDepth + 1;
        return ret;
    }
    public void Insert(T item)
    {
        s_pendingInsertion.Enqueue(item);
    }

    public List<T> GetIntersections(AABBBoundBox box)
    {
        List<T> intersections = new List<T>();
        for (int i = 0; i < objectList.Count; i++)
        {
            if (objectList[i].GetBoundBox().Overlap(box))
            {
                intersections.Add(objectList[i]);
            }
        }

        for (int i = 0; i < 8; i++)
        {
            if (childNodes[i] != null && (box.Contains(childNodes[i]._region) || box.Overlap(childNodes[i]._region)))
            {
                intersections.AddRange(childNodes[i].GetIntersections(box));
            }
        }
        return intersections;
    }

    public int GetMaxTreeDepth()
    {
        int maxTreeDepth = treeDepth;
        for (int i = 0; i < childNodes.Length; i++)
        {
            if (childNodes[i] != null)
            {
                int d = childNodes[i].GetMaxTreeDepth();
                if (d > maxTreeDepth)
                {
                    maxTreeDepth = d;
                }
            }
        }
        return maxTreeDepth;
    }

    public void DebugDraw(int depth = 0)
    {
        _region.DebugDraw(new Color(0,1-depth*0.1f,0));
        for (int i = 0; i < childNodes.Length; i++)
        {
            if ((activeNodesMask & (1 << i)) != 0)
            {
                childNodes[i].DebugDraw(depth+1);
            }
        }
    }

}
