using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class VoxelBound : ICollsionObject
{
    public AABBBoundBox boundBox;
    public bool overlapWithTriangle;
    public AABBBoundBox GetBoundBox()
    {
        return this.boundBox;
    }
    public bool Intersect(ICollsionObject other)
    {
        return this.boundBox.Overlap(other.GetBoundBox());
    }
}

public class TriangleInfo
{
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public Vector3 normal;
    public AABBBoundBox boundBox;
}

public class VoxelizerWithOctree : MonoBehaviour
{
    public BoxCollider voxelBoundBox;

    public int xDimense;

    public int yDimense;

    public int zDimense;

    private Vector3 _halfVector;
    private VoxelBound[,,] _voxelArray;
    private Dictionary<GameObject,TriangleInfo[]> objDic = new Dictionary<GameObject, TriangleInfo[]>();
    // Start is called before the first frame update
    void Start()
    {
        //Display();
    }
    private void InitTriangleInfoDic()
    {
        MeshFilter[] meshFilters = this.transform.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in meshFilters)
        {
            Mesh mesh = filter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] tris = mesh.triangles;
            TriangleInfo[] triangles = new TriangleInfo[tris.Length / 3];
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v1 = filter.transform.TransformPoint(vertices[tris[i]]);
                Vector3 v2 = filter.transform.TransformPoint(vertices[tris[i + 1]]);
                Vector3 v3 = filter.transform.TransformPoint(vertices[tris[i + 2]]);
                AABBBoundBox box = new AABBBoundBox(Vector3.positiveInfinity, Vector3.negativeInfinity);
                box.Extend(v1);
                box.Extend(v2);
                box.Extend(v3);
                triangles[i / 3] = new TriangleInfo
                {
                    p1 = v1,
                    p2 = v2,
                    p3 = v3,
                    boundBox = box
                };
            }
            objDic.Add(filter.gameObject, triangles);
        }
    }

    #region BoundVolumeOctree
    private OctTree<VoxelBound> octree;
    [ContextMenu("VoxelizeWithBoundVolumeOctree")]
    public void VoxelizeWithBoundVolumeOctree()
    {
        octree = new OctTree<VoxelBound>(new AABBBoundBox(voxelBoundBox.bounds));
        InitVoxelArrayWithBoundVolumeOctree();
        //foreach (VoxelBound tBound in _voxelArray)
        //{
        //    if(Random.Range(0f,1f) > 0.99f)
        //        octree.Insert(tBound);
        //}
        octree.UpdateTree();
        InitTriangleInfoDic();
        IntersectTestWithBoundVolumeOctree();
    }
    private void InitVoxelArrayWithBoundVolumeOctree()
    {
        Vector3 originPos = voxelBoundBox.bounds.min;
        Vector3 size = voxelBoundBox.bounds.size;
        _halfVector = new Vector3(size.x / xDimense, size.y / yDimense, size.z / zDimense) * 0.5f;
        _voxelArray = new VoxelBound[xDimense, yDimense, zDimense];
        for (int i = 0; i < xDimense; i++)
        {
            for (int j = 0; j < yDimense; j++)
            {
                for (int z = 0; z < zDimense; z++)
                {
                    Vector3 minPos = originPos + new Vector3(_halfVector.x * 2 * i, _halfVector.y * 2 * j, _halfVector.z * 2 * z);
                    VoxelBound bound = new VoxelBound()
                    {
                        overlapWithTriangle = false,
                        boundBox = new AABBBoundBox(minPos, minPos + 2 * _halfVector)
                    };
                    if (Random.Range(0f, 1f) > 0.95f)
                    {
                        octree.Insert(bound);
                    }
                    //_voxelArray[i, j, z] = bound;
                }
            }
        }
    }
    private void IntersectTestWithBoundVolumeOctree()
    {
        GameObject go;

        // -- for debug
        Stopwatch stopwatch = Stopwatch.StartNew();
        int objIndex = 0;
        int intersectCount = 0;

        foreach (KeyValuePair<GameObject, TriangleInfo[]> tPair in objDic)
        {
            objIndex++;
            intersectCount = 0;
            go = tPair.Key;
            Bounds bound = go.GetComponent<Renderer>().bounds;
            AABBBoundBox objBox = new AABBBoundBox(bound.min, bound.max);
            var l = octree.GetIntersections(objBox);
            for (int i = 0; i < l.Count; i++)
            {
                foreach (TriangleInfo tInfo in tPair.Value)
                {
                    if (l[i].overlapWithTriangle)
                    {
                        continue;
                    }
                    if (Intersect(l[i].boundBox, tInfo))
                    {
                        l[i].overlapWithTriangle = true;
                    }
                }
                intersectCount++;
                bool cancel = EditorUtility.DisplayCancelableProgressBar("Voxelize", $"No.{objIndex} Object,Intersect Count {intersectCount}/{l.Count}",
                    (float)intersectCount / l.Count);
                if (cancel)
                {
                    break;
                }
            }
        }
        stopwatch.Stop();
        EditorUtility.ClearProgressBar();
        Debug.Log($"体素化完成，耗时:{stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region AABB与Box相交测试
    private bool Intersect(AABBBoundBox aabb, TriangleInfo triange)
    {
        // 转换三角形顶点至以aabb的中心为原点的坐标系下
        Vector3 v0 = triange.p1 - aabb.center;
        Vector3 v1 = triange.p2 - aabb.center;
        Vector3 v2 = triange.p3 - aabb.center;
        // 三角形边的向量形式
        Vector3 f0 = v1 - v0;
        Vector3 f1 = v2 - v1;
        Vector3 f2 = v0 - v2;
        // AABB的法线
        Vector3 u0 = new Vector3(1.0f, 0f, 0f);
        Vector3 u1 = new Vector3(0, 1.0f, 0);
        Vector3 u2 = new Vector3(0, 0, 1.0f);
        Vector3[] axiArray = new Vector3[]
        {
            // 叉积轴
            Vector3.Cross(u0, f0),
            Vector3.Cross(u0, f1),
            Vector3.Cross(u0, f2),
            Vector3.Cross(u1, f0),
            Vector3.Cross(u1, f1),
            Vector3.Cross(u1, f2),
            Vector3.Cross(u2, f0),
            Vector3.Cross(u2, f1),
            Vector3.Cross(u2, f2),
            // AABB面法线
            u0,
            u1,
            u2,
            // 三角形面法线
            Vector3.Cross(f0,f1)
        };
        for (int i = 0; i < axiArray.Length; i++)
        {
            // 判定该轴是不是一个分离轴 如果是的话可判定不相交
            if (IsSeparateAxi(axiArray[i], aabb.half, v0, v1, v2))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSeparateAxi(Vector3 axi, Vector3 aabbExtent, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 u0 = new Vector3(1.0f, 0f, 0f);
        Vector3 u1 = new Vector3(0, 1.0f, 0);
        Vector3 u2 = new Vector3(0, 0, 1.0f);
        float p0 = Vector3.Dot(v0, axi);
        float p1 = Vector3.Dot(v1, axi);
        float p2 = Vector3.Dot(v2, axi);
        float r = aabbExtent.x * Mathf.Abs(Vector3.Dot(u0, axi)) +
                  aabbExtent.y * Mathf.Abs(Vector3.Dot(u1, axi)) +
                  aabbExtent.z * Mathf.Abs(Vector3.Dot(u2, axi));

        // aabb投影区间[-r,r] 三角形投影区间[min(p0,p1,p2),max(p0,p1,p2)],如果两个区间没有交集则是分离轴
        // 这里的比较是一种高效方式 代替 if(min > r || max < -r)
        if (Mathf.Max(
            -Mathf.Max(Mathf.Max(p0, p1), p2),
            Mathf.Min(Mathf.Min(p0, p1), p2)
            ) > r)
        {
            return true;
        }

        return false;
    }
    #endregion
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying == false || this.enabled == false)
        {
            return;
        }
        if (_voxelArray == null)
        {
            return;
        }
        foreach (VoxelBound tBound in _voxelArray)
        {
            if (tBound.overlapWithTriangle == true)
            {
                Handles.DrawWireCube(tBound.boundBox.center, tBound.boundBox.half * 2);

            }
        }
        octree.DebugDraw();
    }

    private void Display()
    {
        Vector3 size = _halfVector * 2;
        foreach (VoxelBound bound in _voxelArray)
        {
            if (bound.overlapWithTriangle)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = size;
                go.transform.position = bound.boundBox.center;
            }
        }
    }
}
