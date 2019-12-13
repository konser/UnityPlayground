using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using ComputationalGeometry;
#if UNITY_EDITOR
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
#endif

namespace RuntimePathfinding
{
    [System.Serializable]
    public class HaltonSequenceData
    {
        public HaltonSequenceData(int length)
        {
            xArray = new float[length];
            yArray = new float[length];
            this.length = length;
            currentCount = 0;
        }
        public float[] xArray;
        public float[] yArray;
        public int length;
        private int currentCount = 0;
        public Vector2 GetNext()
        {
            int index = currentCount % length;
            currentCount++;
            return new Vector2(xArray[index], yArray[index]);
        }
    }

    public class Triangle
    {
        public Vector3 vertA;
        public Vector3 vertB;
        public Vector3 vertC;
        public float area;
        public Vector3 normal;
        private bool areaCalculated;
        public List<Vector3> samplePointList = new List<Vector3>(50);
        public bool isTerrainTriangle = false;
        public Triangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            vertA = p1;
            vertB = p2;
            vertC = p3;
            GetTriangleArea();
            Vector3 u = p2 - p1;
            Vector3 v = p3 - p1;
            normal = Vector3.Cross(u, v).normalized;
            //Debug.DrawLine((vertA+vertB+vertC)/3f, (vertA + vertB + vertC) / 3f + normal,Color.blue,10f);
        }
        public float GetTriangleArea()
        {
            if (areaCalculated == false)
            {
                Vector3 AB = vertB - vertA;
                Vector3 AC = vertC - vertA;
                float cosTheta = Vector3.Dot(AB.normalized, AC.normalized);
                float sinTheta = Mathf.Sqrt(1 - cosTheta * cosTheta);
                area = 0.5f * AB.magnitude * AC.magnitude * sinTheta;
                areaCalculated = true;
                return area;
            }
            return area;
        }
        public int TriangleHash()
        {
            return (vertA.GetHashCode()+ vertB.GetHashCode() + vertC.GetHashCode()).GetHashCode();
        }
    }

    public class ConvexRegion
    {
        public int regionIndex;
        public List<LinkNode> convexRegionOne;
        public List<LinkNode> convexRegionTwo;
    }

    public class NavmeshSampler : MonoBehaviour
    {
        public LayerMask collectLayers;
        public float sampleRadius = 3f;
        public float maxAngle = 30f;
        private HaltonSequenceData haltonSeqData;
        private BoxCollider boxCollider;
        private List<Vector3> validPositions;
        private List<Vector3> points = new List<Vector3>();
        private List<Triangle> triangleList = new List<Triangle>();
        int connectAreaMask;
        private int areaOneMask;
        private int areaTwoMask;

        private PathConnectInfo connectInfo;
        private Bounds bounds
        {
            get
            {
                if (boxCollider == null)
                {
                    boxCollider = this.GetComponent<BoxCollider>();
                }
                return boxCollider.bounds;
            }
        }

        private void Awake()
        {
            connectAreaMask = 1 << NavMesh.GetAreaFromName("BakeLink");
            areaOneMask = 1 << NavMesh.GetAreaFromName("Jump");
            areaTwoMask = 1 << NavMesh.GetAreaFromName("Walkable");
        }

        private void Start()
        {
            TextAsset haltonSeq = Resources.Load<TextAsset>("HaltonSequence");
            BinaryFormatter bf = new BinaryFormatter();
            haltonSeqData = bf.Deserialize(new MemoryStream(haltonSeq.bytes)) as HaltonSequenceData;
            path = new NavMeshPath();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
            }
        }

        [ContextMenu("SampleNavmesh")]
        public void SampleNavmesh()
        {
            // 采样点
            triangleList.Clear();
            points.Clear();
            List<NavMeshBuildSource> sources = CollectMesh();
            foreach (NavMeshBuildSource src in sources)
            {
                switch (src.shape)
                {
                    case NavMeshBuildSourceShape.Mesh:
                        GetTriangles(src.sourceObject as Mesh, triangleList);
                        break;
                }
            }
            SampleRandomPoint(points);
            // 分配采样点到各个格子中，构成连通区域
            CreateNavInfo();

        }

        #region Sample Points
        private List<NavMeshBuildSource> CollectMesh()
        {
            List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
            List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();

            NavMeshBuilder.CollectSources(bounds, collectLayers.value, NavMeshCollectGeometry.RenderMeshes,
                NavMesh.GetAreaFromName("BakeLink"), markups, sources);

            return sources;
        }

        private void GetTriangles(Mesh mesh, List<Triangle> triangleList)
        {
            if (mesh == null)
            {
                return;
            }

            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            Debug.DrawLine(bounds.min,bounds.max,Color.red,10f);
            Debug.DrawLine(mesh.bounds.min, mesh.bounds.max, Color.green, 10f);
            
            if (bounds.Intersects(mesh.bounds) == false)
            {
                return;
            }

            if (bounds.Contains(mesh.bounds.min) && bounds.Contains(mesh.bounds.max))
            {
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    triangleList.Add(new Triangle(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]));
                }
                return;
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 p1 = vertices[triangles[i]];
                Vector3 p2 = vertices[triangles[i + 1]];
                Vector3 p3 = vertices[triangles[i + 2]];

                if (TriangleBoundsIntersect(bounds, p1, p2, p3))
                {
                    triangleList.Add(new Triangle(p1, p2, p3));
                }
            }
            Debug.Log("三角形数量: " + triangleList.Count);
        }

        private void SampleRandomPoint(List<Vector3> result)
        {

            float cosTheta = Mathf.Cos(maxAngle);

            for (int i = 0; i < triangleList.Count; i++)
            {
                if (Vector3.Dot(triangleList[i].normal, Vector3.up) < cosTheta)
                {
                    continue;
                }
                float area = triangleList[i].GetTriangleArea();
                int sampleCount = Mathf.CeilToInt(area / 2f);
                for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    Vector2 randomValue = haltonSeqData.GetNext();
                    float u = randomValue.x;
                    float v = randomValue.y;
                    float w = 1 - (u + v);
                    Vector3 pos = u * triangleList[i].vertA + v * triangleList[i].vertB + w * triangleList[i].vertC;
                    if (bounds.Contains(pos))
                    {
                        bool inNavmesh = NavMesh.SamplePosition(pos, out NavMeshHit hit, 0.3f, connectAreaMask);
                        if (inNavmesh)
                        {
                            result.Add(hit.position);
                        }
                    }
                }
            }
        }

        private bool TriangleBoundsIntersect(Bounds bound, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            Vector3 boundHalfSize = bound.size * 0.5f;
            // 转换三角形顶点至以aabb的中心为原点的坐标系下
            Vector3 v0 = vertex1 - bound.center;
            Vector3 v1 = vertex2 - bound.center;
            Vector3 v2 = vertex3 - bound.center;
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
                if (IsSeparateAxi(axiArray[i], boundHalfSize, v0, v1, v2))
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
        private NavMeshPath path;

        // 1.三角形与哪些相交 2.将点分配至相交格子中 3.任选相交的格子进行搜索
        #region Split points to region
        private LinkNode[,,] grids;
        private void CreateNavInfo()
        {
            // create grid with sample point
            Vector3 size = new Vector3(0.5f, 2f, 0.5f);
            int x = Mathf.CeilToInt(bounds.size.x / size.x);
            int y = Mathf.CeilToInt(bounds.size.y / size.y);
            int z = Mathf.CeilToInt(bounds.size.z / size.z);
            Vector3 localMinPos = transform.InverseTransformPoint(bounds.min) + 0.5f * size;
            grids = new LinkNode[x, y, z];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    for (int k = 0; k < z; k++)
                    {
                        LinkNode n = new LinkNode();
                        n.bound = new Bounds(transform.TransformPoint(localMinPos + new Vector3(size.x * i, size.y * j, size.z * k)), size);
                        n.x = i;
                        n.y = j;
                        n.z = k;
                        grids[i, j, k] = n;
                    }
                }
            }

            for (int i = points.Count - 1; i >= 0; i--)
            {
                Vector3 localPos = transform.InverseTransformPoint(points[i]) - localMinPos;
                int idx = (int)(localPos.x / size.x);
                int idy = (int)(localPos.y / size.y);
                int idz = (int)(localPos.z / size.z);
                if (!grids[idx, idy, idz].hasPoint)
                {
                    grids[idx, idy, idz].pos = points[i];
                    grids[idx, idy, idz].hasPoint = true;
                }
                points.RemoveAt(i);
            }

            // connect area
            List<LinkNode> neibours = new List<LinkNode>();
            int id = 0;
            Queue<LinkNode> q = new Queue<LinkNode>();
            Dictionary<LinkNode, List<LinkNode>> regions = new Dictionary<LinkNode, List<LinkNode>>();
            foreach (var node in grids)
            {
                if (node.regionID == -1 && node.hasPoint == true)
                {
                    q.Clear();
                    id++;
                    node.regionID = id;
                    regions[node] = new List<LinkNode>();
                    q.Enqueue(node);
                    while (q.Count != 0)
                    {
                        LinkNode currNode = q.Dequeue();
                        QueryNeibourNode(currNode, neibours);
                        for (int i = 0; i < neibours.Count; i++)
                        {
                            if (neibours[i].hasPoint && neibours[i].regionID == -1)
                            {
                                neibours[i].regionID = id;
                                regions[node].Add(neibours[i]);
                                q.Enqueue(neibours[i]);
                            }
                        }
                    }
                }
            }
            if (path == null)
            {
                path = new NavMeshPath();
            }
            List<LinkNode> firstNodes = regions.Keys.ToList();
            foreach (LinkNode curr in firstNodes)
            {
                if (regions[curr] == null)
                {
                    continue;
                }
                int currentID = curr.regionID;

                foreach (LinkNode t in firstNodes)
                {
                    if (t == curr)
                    {
                        continue;
                    }
                    if (regions[t] == null)
                    {
                        continue;
                    }

                    Vector3 pos1 = curr.pos;
                    Vector3 pos2 = t.pos;
                    NavMesh.CalculatePath(pos1, pos2, connectAreaMask, path);
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        regions[curr].AddRange(regions[t]);
                        regions[curr].Add(t);
                        regions[t] = null;
                    }
                }

                for (int i = 0; i < regions[curr].Count; i++)
                {
                    regions[curr][i].regionID = currentID;
                }
            }

            foreach (LinkNode node in firstNodes)
            {
                if (regions[node] == null)
                {
                    regions.Remove(node);
                }
            }

            FindConnectPort(regions);
        }


        private List<ConvexRegion> _convexRegionList = new List<ConvexRegion>();
        private void FindConnectPort(Dictionary<LinkNode, List<LinkNode>> regions)
        {
            List<LinkNode> connectedRegions = new List<LinkNode>();
            foreach (KeyValuePair<LinkNode, List<LinkNode>> tPair in regions)
            {
                bool connectOne = false;
                bool connectTwo = false;
                foreach (LinkNode node in tPair.Value)
                {
                    if (NavMesh.SamplePosition(node.pos, out NavMeshHit hit1, 0.15f, areaOneMask))
                    {
                        connectOne = true;
                        node.inRegionOne = true;
                    }

                    if (NavMesh.SamplePosition(node.pos, out NavMeshHit hit2, 0.15f, areaTwoMask))
                    {
                        connectTwo = true;
                        node.inRegionTwo = true;
                    }
                }
                if (connectOne && connectTwo)
                {
                    connectedRegions.Add(tPair.Key);
                }
            }

            connectInfo = new PathConnectInfo();
            List<LinkNode> cacheOneList = new List<LinkNode>();
            List<LinkNode> cacheTwoList = new List<LinkNode>();
            _convexRegionList.Clear();

            foreach (KeyValuePair<LinkNode, List<LinkNode>> tPair in regions)
            {
                cacheOneList.Clear();
                cacheTwoList.Clear();
                if (connectedRegions.Contains(tPair.Key))
                {
                    // 区分出不属于同一Navmesh块的连通区域的点
                    tPair.Key.isConnectedRegion = true;
                    foreach (LinkNode tNode in tPair.Value)
                    {
                        tNode.isConnectedRegion = true;
                        if (tNode.inRegionOne)
                        {
                            cacheOneList.Add(tNode);
                        }

                        if (tNode.inRegionTwo)
                        {
                            cacheTwoList.Add(tNode);
                        }
                    }

                    ConvexRegion convexRegion = new ConvexRegion()
                    {
                        convexRegionOne = new List<LinkNode>(),
                        convexRegionTwo = new List<LinkNode>(),
                        regionIndex = tPair.Key.regionID
                    };

                    LinkNode node1 = null;
                    LinkNode node2 = null;
                    // 生成凸包
                    ConvexHull2D<LinkNode>.GetConvexHull2D(cacheOneList, convexRegion.convexRegionOne);
                    ConvexHull2D<LinkNode>.GetConvexHull2D(cacheTwoList, convexRegion.convexRegionTwo);

                    // 从两个凸包上找邻近点作为跨越路线
                    float minDist = Single.PositiveInfinity;
                    foreach (LinkNode n1 in convexRegion.convexRegionOne)
                    {
                        foreach (LinkNode n2 in convexRegion.convexRegionTwo)
                        {
                            float d = (n1.position - n2.position).sqrMagnitude;
                            if (d < minDist && d > 0.25f)
                            {
                                minDist = d;
                                node1 = n1;
                                node2 = n2;
                            }
                        }
                    }
                    
                    if (node1 != null && node2 != null)
                    {
                        node1.isLinkPoint = true;
                        node2.isLinkPoint = true;
                        
                        // 两侧区域都存在的才能作为连接点
                        if (convexRegion.convexRegionOne.Count != 0 && convexRegion.convexRegionTwo.Count != 0)
                        {
                            node1.peer = node2;
                            node2.peer = node1;
                            connectInfo.regionOnePos.Add(SampleNavmeshPos(node1.position, areaOneMask));
                            connectInfo.regionTwoPos.Add(SampleNavmeshPos(node2.position, areaTwoMask));
                        }
                    }
                    _convexRegionList.Add(convexRegion);
                }
            }
        }

        private Vector3 SampleNavmeshPos(Vector3 pos, int mask)
        {
            bool hasPos = NavMesh.SamplePosition(pos, out var hit, 5f, mask);
            if (hasPos)
            {
                return hit.position;
            }
            return pos;
        }

        private void QueryNeibourNode(LinkNode node, List<LinkNode> result)
        {
            result.Clear();
            int y = node.y;
            int x = node.x;
            int z = node.z;
            if (InGridRange(x + 1, y, z + 1))
            {
                result.Add(grids[x + 1, y, z + 1]);
            }
            if (InGridRange(x + 1, y, z))
            {
                result.Add(grids[x + 1, y, z]);
            }
            if (InGridRange(x + 1, y, z - 1))
            {
                result.Add(grids[x + 1, y, z - 1]);
            }
            if (InGridRange(x, y, z + 1))
            {
                result.Add(grids[x, y, z + 1]);
            }
            if (InGridRange(x, y, z - 1))
            {
                result.Add(grids[x, y, z - 1]);
            }
            if (InGridRange(x - 1, y, z + 1))
            {
                result.Add(grids[x - 1, y, z + 1]);
            }
            if (InGridRange(x - 1, y, z))
            {
                result.Add(grids[x - 1, y, z]);
            }
            if (InGridRange(x - 1, y, z - 1))
            {
                result.Add(grids[x - 1, y, z - 1]);
            }
        }

        private bool InGridRange(int x, int y, int z)
        {
            if (x >= 0 && x < grids.GetLength(0) && y >= 0 && y < grids.GetLength(1) && z >= 0 && z < grids.GetLength(2))
            {
                return true;
            }
            return false;
        }
        #endregion

        [ContextMenu("GenHaltonSequence")]
        public void GenerateHaltonSequence()
        {
#if UNITY_EDITOR
            HaltonSequenceData data = new HaltonSequenceData(3000);
            HaltonSequence seq = new HaltonSequence();
            for (int i = 0; i < 3000; i++)
            {
                Vector3 value = seq.m_CurrentPos;
                while (value.x + value.z > 1f)
                {
                    seq.Increment();
                    value = seq.m_CurrentPos;
                }
                data.xArray[i] = value.x;
                data.yArray[i] = value.z;
                seq.Increment();
            }
            using (System.IO.MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, data);
                byte[] bytes = ms.ToArray();
                File.WriteAllBytes("Assets/Tutorial/Pathfinding/Resources/HaltonSequence.bytes", bytes);
            }
            AssetDatabase.Refresh();
#endif
        }


        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (_convexRegionList != null)
            {
                foreach (var tConvexRegion in _convexRegionList)
                {
                    Gizmos.color = new Color(0.8f, 0.1f, 0.4f);
                    for (int i = 0; i < tConvexRegion.convexRegionOne.Count; i++)
                    {
                        Gizmos.DrawSphere(tConvexRegion.convexRegionOne[i].pos, 0.1f);
                        Handles.Label(tConvexRegion.convexRegionOne[i].pos, tConvexRegion.convexRegionOne[i].regionID.ToString());
                        Gizmos.DrawLine(tConvexRegion.convexRegionOne[i].position, tConvexRegion.convexRegionOne[(i + 1) % tConvexRegion.convexRegionOne.Count].position);
                    }
                    Gizmos.color = Color.green;
                    for (int i = 0; i < tConvexRegion.convexRegionTwo.Count; i++)
                    {
                        Gizmos.DrawSphere(tConvexRegion.convexRegionTwo[i].pos, 0.1f);
                        Handles.Label(tConvexRegion.convexRegionTwo[i].pos, tConvexRegion.convexRegionTwo[i].regionID.ToString());
                        Gizmos.DrawLine(tConvexRegion.convexRegionTwo[i].position, tConvexRegion.convexRegionTwo[(i + 1) % tConvexRegion.convexRegionTwo.Count].position);
                    }
                }
            }

            Gizmos.color = Color.cyan;
            if (connectInfo != null)
            {
                for (int i = 0; i < connectInfo.regionOnePos.Count; i++)
                {
                    Vector3 center = 0.5f * (connectInfo.regionOnePos[i] + connectInfo.regionTwoPos[i]);
                    Gizmos.DrawLine(connectInfo.regionOnePos[i], connectInfo.regionTwoPos[i]);
                    //Gizmos.DrawLine(center,center + Vector3.up*2.0f);
                }
            }
            //return;
            Gizmos.color = Color.yellow;
            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Gizmos.DrawSphere(points[i], 0.1f);
                }
            }

            //if (grids != null)
            //{
            //    foreach (CNode b in grids)
            //    {
            //        if (b.isConnectedRegion)
            //        {
            //            //Gizmos.DrawWireCube(b.bound.center, b.bound.size);
            //            if (b.inRegionOne)
            //            {
            //                Gizmos.color = new Color(0.8f, 0.1f, 0.4f);
            //                Gizmos.DrawSphere(b.pos, 0.1f);
            //                Handles.Label(b.pos, b.regionID.ToString());
            //            }
            //            else if (b.inRegionTwo)
            //            {
            //                Gizmos.color = Color.green;
            //                Gizmos.DrawSphere(b.pos, 0.1f);
            //                Handles.Label(b.pos, b.regionID.ToString());
            //            }
            //        }
            //    }
            //}
#endif
        }


    }
}