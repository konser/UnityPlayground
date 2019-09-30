using UnityEngine;
using System.Collections;
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


public static class IntersectionTest
{
    public static bool AABBTriangle(AABBBoundBox aabb, TriangleInfo triange)
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

    public static TriangleInfo[] GetTriangles(MeshFilter filter)
    {
        Mesh mesh = filter.sharedMesh;
        if (mesh == null)
        {
            return null;
        }
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
            };
        }
        return triangles;
    }

    private static bool IsSeparateAxi(Vector3 axi, Vector3 aabbExtent, Vector3 v0, Vector3 v1, Vector3 v2)
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
}
