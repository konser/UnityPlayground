using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;

namespace RuntimePathfinding
{
    /// <summary>
    /// 寻路请求数据
    /// </summary>
    public class PathfindingRequest
    {
        public Vector3 start;
        public Vector3 destination;
        public NavMeshAgent agent;
    }

    /// <summary>
    /// Navmesh区块标识
    /// </summary>
    public struct TileIdentifier
    {
        public TileIdentifier(int x, int z, int seqId)
        {
            coordX = x;
            coordZ = z;
            sequenceIndex = seqId;
        }
        /// <summary>
        /// 格子坐标X
        /// </summary>
        public int coordX;

        /// <summary>
        /// 格子坐标Z
        /// </summary>
        public int coordZ;

        /// <summary>
        /// 该格子在整个路径的顺序序号
        /// </summary>
        public int sequenceIndex;
    }

    // todo bugfix
    // todo custom ai movement
    public class RuntimePathfinding : MonoBehaviour
    {
        static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
        public static int areaDetail;
        public static int areaMaskDetail;
        public static int areaWalkable;
        public static int areaMaskWalkable;
        public static int areaBakeLink;
        public static int areaMaskBakeLink;
        public static int areaTileConnection;
        public static int areaMaskTileConnection;
        /// <summary>
        /// 寻路设置
        /// </summary>
        public PathfindingSetting setting;

        /// <summary>
        /// 寻路组件是否初始化完成
        /// </summary>
        public bool isInited
        {
            get
            {
                return _initialized;
            }
        }

        public NavMeshAgent testAgent;
        public Transform destination;
        private const int FRAME_HANDLE_COUNT = 3;
        private bool _initialized;
        private ObjectPool _tilePool;
        private ObjectPool _tileLinkPool;
        private NavmeshLinkBakeArea _linkBakeArea;
        private Queue<PathfindingRequest> _requestQueue;
        // 计算路径未达到终点的重算距离
        private const float RECALCULATE_PATH_DISTANCE = 5.0f;

        #region =========================Debug==========================
        private List<TileIdentifier> identifiers = new List<TileIdentifier>();
        private void OnDrawGizmos()
        {
            if (Application.isPlaying && identifiers.Count != 0)
            {
                foreach (TileIdentifier tIdentifier in identifiers)
                {
                    Vector3 pos = new Vector3((tIdentifier.coordX+0.5f) *setting.tileSize,0,(tIdentifier.coordZ+0.5f) *setting.tileSize);
#if UNITY_EDITOR
                    Handles.DrawWireCube(pos,new Vector3(setting.tileSize,0.1f,setting.tileSize));
#endif
                }
            }
        }

        #endregion
        private IEnumerator Start()
        {
            Initialize();
            yield return new WaitForSeconds(5);
            //RequestPathfinding(new PathfindingRequest
            //{
            //    agent = testAgent,
            //    destination = destination.position,
            //    start = testAgent.transform.position
            //});
        }

        public void Initialize()
        {
            areaDetail = NavMesh.GetAreaFromName("Detail");
            areaWalkable = NavMesh.GetAreaFromName("Walkable");
            areaBakeLink = NavMesh.GetAreaFromName("BakeLink");
            areaTileConnection = NavMesh.GetAreaFromName("TileConnection");

            areaMaskDetail = 1 << areaDetail;
            areaMaskWalkable = 1 << areaWalkable;
            areaMaskBakeLink = 1 << areaBakeLink;
            areaMaskTileConnection = 1 << areaTileConnection;

            if (setting.tilePrefab == null || setting.tileLinkPrefab == null)
            {
                Debug.LogError("初始化设置中prefab为空");
                return;
            }
            _requestQueue = new Queue<PathfindingRequest>();
            _tilePool = ObjectPool.GetPool(setting.tilePrefab);
            _tileLinkPool = ObjectPool.GetPool(setting.tileLinkPrefab);
            _linkBakeArea = Instantiate(setting.linkBakeArea,this.transform,false);
            _linkBakeArea.Init(setting);
            _initialized = true;
        }

        private void Update()
        {
            if (!isInited)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                RequestPathfinding(new PathfindingRequest
                {
                    agent = testAgent,
                    destination =  destination.position,
                    start = testAgent.transform.position
                });
            }
            int count = 0;
            while (_requestQueue.Count != 0 && count < FRAME_HANDLE_COUNT)
            {
                PathfindingRequest req = _requestQueue.Dequeue();
                PathfindingCoroutine pathfindingCoroutine = new PathfindingCoroutine(ProcessRequest(req));
                StartCoroutine(pathfindingCoroutine);
                count++;
            }
        }

        public void RequestPathfinding(PathfindingRequest request)
        {
            _requestQueue.Enqueue(request);
        }

        #region Pathfinding Request Process
        private IEnumerator ProcessRequest(PathfindingRequest request)
        {
            NavMeshPath navmeshPath = new NavMeshPath();
            Vector3 start = request.start;
            Vector3 destination = request.destination;
            NavMeshAgent agent = request.agent;
            Vector3[] abstractPath = new Vector3[1000];
            int wayPointCount = 0;
            int currentSeqIndex = 0;
            int lastSeqIndex = -1;
            NavmeshTile[] activeTiles = null;
            GenerateAbstractPath(start,destination,ref abstractPath,ref wayPointCount,ref navmeshPath);
            yield return _endOfFrame;

            List<TileIdentifier> passedTileList = MarkPassedNavmeshTile(ref abstractPath,ref wayPointCount);
            yield return _endOfFrame;
            identifiers = passedTileList;
            while (!HasReachedDestination(agent,destination))
            {
                // 检查当前所处区块是否变化
                CheckAgentCurrentPosition(agent,activeTiles,ref currentSeqIndex);

                if (lastSeqIndex == currentSeqIndex)
                {
                    // 仍然处于当前区块
                    yield return _endOfFrame;
                }
                // 需要加载新区块
                else
                {
                    lastSeqIndex = currentSeqIndex;

                    GenerateNavmeshTile(passedTileList, ref activeTiles, currentSeqIndex);
                    yield return _endOfFrame;

                    GenerateTileLink(ref activeTiles);
                    yield return _endOfFrame;

                    GenerateDetailedPath(agent,activeTiles,destination);
                    yield return _endOfFrame;
                    TryMoveAgent();
                }
            }
            for (int i = 0; i < activeTiles.Length; i++)
            {
                if (activeTiles[i] != null)
                {
                    activeTiles[i].ReturnToPool();
                }
            }
            yield return _endOfFrame;
        }

        // ---------------生成粗略路径-----------------
        private void GenerateAbstractPath(Vector3 start,Vector3 end,ref Vector3[] abstractPath,ref int count,ref NavMeshPath path)
        {
            GetPathFromArea(areaMaskWalkable,start,end,ref abstractPath,ref count,ref path);
        }

        private void GetPathFromArea(int areaMask,Vector3 startPos ,Vector3 endPos,ref Vector3[] pathResult, ref int count,ref NavMeshPath path)
        {
            count = 0;
            pathResult[count] = startPos;
            // CalculatePath计算太长的路径时会算不完整，需要从中断点继续朝目标位置计算
            while (true)
            {
                Vector3 currentPos = count == 0 ? pathResult[count] : pathResult[count - 1];
                float distance = Vector3.Distance(currentPos, endPos);
                if (distance > RECALCULATE_PATH_DISTANCE)
                {
                    path.ClearCorners();
                    bool succeed = NavMesh.CalculatePath(currentPos, endPos, areaMask, path);
                    if (succeed == false)
                    {
                        Debug.LogError("Calculate path failed");
                        break;
                    }

                    if (path.status != NavMeshPathStatus.PathInvalid)
                    {
                        int currentPathLength = path.corners.Length;
                        for (int i = count; i < currentPathLength + count; i++)
                        {
                            pathResult[i] = path.corners[i - count];
                        }
                        count += currentPathLength;
                    }
                    else
                    {
                        Debug.Log($"Path Invalid !");
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // --------------标记粗略路径经过的区块------------
        private List<TileIdentifier> MarkPassedNavmeshTile(ref Vector3[] abstractPath,ref int count)
        {
            List<TileIdentifier> passedTileList = new List<TileIdentifier>();
            for (int i = 0; i < count - 1; i++)
            {
                // 路径中的线段起点与终点
                Vector3 lineSegmentStart = abstractPath[i];
                Vector3 lineSegmentEnd = abstractPath[i + 1];
                GridTraverse(lineSegmentStart,lineSegmentEnd,ref passedTileList);
            }
            return passedTileList;
        }

        private float Frac0(float x)
        {
            return x - Mathf.Floor(x);
        }

        private float Frac1(float x)
        {
            return 1 - x + Mathf.Floor(x);
        }

        /// <summary>
        /// 给定起点 终点 网格尺寸计算出经过的格子索引 (x,y)
        /// </summary>
        private void GridTraverse(Vector3 start, Vector3 end, ref List<TileIdentifier> indexList)
        {
            start /= setting.tileSize;
            end /= setting.tileSize;
            float tMaxX, tMaxY, tDeltaX, tDeltaY;
            float dx = end.x - start.x;
            float dy = end.z - start.z;
            int signX = (dx > 0 ? 1 : (dx < 0 ? -1 : 0));
            int signY = (dy > 0 ? 1 : (dy < 0 ? -1 : 0));
            if (dx != 0)
            {
                tDeltaX = Mathf.Min(signX / dx, float.MaxValue);
            }
            else
            {
                tDeltaX = float.MaxValue;
            }

            if (signX > 0)
            {
                tMaxX = tDeltaX * Frac1(start.x);
            }
            else
            {
                tMaxX = tDeltaX * Frac0(start.x);
            }


            if (dy != 0)
            {
                tDeltaY = Mathf.Min(signY / dy, float.MaxValue);
            }
            else
            {
                tDeltaY = float.MaxValue;
            }

            if (signY > 0)
            {
                tMaxY = tDeltaY * Frac1(start.z);
            }
            else
            {
                tMaxY = tDeltaY * Frac0(start.z);
            }

            int idx = (int)start.x;
            int idy = (int)start.z;

            AddTile(indexList, idx, idy);

            while (true)
            {
                if (tMaxX < tMaxY)
                {
                    tMaxX = tMaxX + tDeltaX;
                    idx += signX;
                }
                else
                {
                    tMaxY = tMaxY + tDeltaY;
                    idy += signY;
                }
                if (tMaxX > 1 && tMaxY > 1)
                {
                    break;
                }
                AddTile(indexList, idx, idy);
            }
            AddTile(indexList, (int)end.x, (int)end.z);
        }

        private bool AddTile(List<TileIdentifier> list, int idx, int idz)
        {
            bool contains = list.Any(l => { return l.coordX == idx && l.coordZ == idz; });
            if (contains)
            {
                return false;
            }
            list.Add(new TileIdentifier(idx,idz,list.Count));
            return true;
        }

        // --------------检查对象位置----------------
        // 判断寻路对象当前处于哪个tile
        private void CheckAgentCurrentPosition(NavMeshAgent agent, NavmeshTile[] tiles,ref int currentSeqIndex)
        {
            if (agent.isOnNavMesh == false || agent.isStopped)
            {
                Debug.Log(agent.pathStatus);
                Debug.DrawLine(agent.transform.position,agent.steeringTarget,Color.magenta,10f);
            }
            if (tiles == null)
            {
                currentSeqIndex = 0;
            }
            else
            {
                Vector3 currentPos = agent.transform.position;
                int nextIndex = int.MaxValue;
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (tiles[i] != null && tiles[i].ContainsPosition2D(currentPos))
                    {
                        if (tiles[i].sequenceIndex < nextIndex && tiles[i].sequenceIndex > currentSeqIndex)
                        {
                            nextIndex = tiles[i].sequenceIndex;
                        }
                    }
                }
                if (nextIndex != int.MaxValue)
                {
                    currentSeqIndex = nextIndex;
                }
            }
        }

        // 判断寻路对象是否到达终点
        private bool HasReachedDestination(NavMeshAgent agent,Vector3 dest)
        {
            if (Vector3.Distance(agent.transform.position, dest) < 2.5f)
            {
                return true;
            }
            return false;
        }

        // --------------生成详细Navmesh区块--------------
        private void GenerateNavmeshTile(List<TileIdentifier> passedTileList,ref NavmeshTile[] activeTiles,int currentSeqIndex)
        {
            // 第一次从当前位置开始建立连续的三个tile
            if (activeTiles == null && currentSeqIndex == 0)
            {
                activeTiles = new NavmeshTile[3];
                for (int i = 0; i < 3; i++)
                {
                    if (i < passedTileList.Count)
                    {
                        NavmeshTile tile = _tilePool.GetObject<NavmeshTile>();
                        tile.Init(setting);
                        tile.BakeNavmeshTile(passedTileList[i]);
                        activeTiles[i] = tile;
                    }
                }
                return;
            }

            // 每经过一个tile(currentSeqIndex增加)，将最旧的一个删除，后续两个tile挪至数组前两位，加载新Tile至数组第三个位置
            activeTiles[0].ReturnToPool();
            activeTiles[0] = activeTiles[1]; // currentSeqIndex
            activeTiles[1] = activeTiles[2]; // currentSeqIndex + 1
            if (currentSeqIndex + 2 < passedTileList.Count)
            {
                activeTiles[2] = _tilePool.GetObject<NavmeshTile>();
                activeTiles[2].Init(setting);
                activeTiles[2].BakeNavmeshTile(passedTileList[currentSeqIndex + 2]);
            }
            else
            {
                activeTiles[2] = null;
                Debug.Log($"即将到达终点 当前区块为{currentSeqIndex} 最后区块为{passedTileList.Count - 1}");
            }
        }

        // ---------------生成区块间的连接点----------------
        private void GenerateTileLink(ref NavmeshTile[] activeTiles)
        {
            // 数组中第一个始终为当前所处的区块
            if (activeTiles[0] != null && activeTiles[1] != null)
            {
                if (activeTiles[0].GetLinkListToNextTile().Count == 0)
                {
                    _linkBakeArea.BakeTileLink(activeTiles[0],activeTiles[1],_tileLinkPool);
                }
            }
            if (activeTiles[1] != null && activeTiles[2] != null)
            {
                if (activeTiles[1].GetLinkListToNextTile().Count == 0)
                {
                    _linkBakeArea.BakeTileLink(activeTiles[1], activeTiles[2], _tileLinkPool);
                }
            }
        }

        // ----------------生成经过区块的详细路径--------------------
        // ---todo 这里先没有处理自定义对象的移动，先用NavmeshAgent进行测试
        private void GenerateDetailedPath(NavMeshAgent agent,NavmeshTile[] activeTiles,Vector3 finalDest)
        {
            bool nearFinalDestination = false;
            // 检查是否有3个存在的区块，如果不是则说明接近终点了
            for (int i = 0; i < activeTiles.Length; i++)
            {
                if (activeTiles[i] == null)
                {
                    nearFinalDestination = true;
                    break;
                }
            }
            // 若区块数为3 A->B->C 当处于A时，选取B->C连接点中属于C的某个点，进行寻路
            if (!nearFinalDestination)
            {
                // TileB 的navmesh link
                Vector3 pos = SelectPositionFromTileLink(agent,activeTiles[0],activeTiles[1]);
                agent.SetDestination(pos);
                Debug.DrawLine(pos,pos + Vector3.up*30.0f,Color.white,5.0f);
            }
            else
            {
                agent.SetDestination(finalDest);
            }
        }
        private NavMeshPath _navmeshPath;
        private Vector3 SelectPositionFromTileLink(NavMeshAgent agent,NavmeshTile nearTile,NavmeshTile farTile)
        {
            if (_navmeshPath == null)
            {
                _navmeshPath = new NavMeshPath();
            }
            Vector3 agentPos = agent.transform.position;
            Vector3 targetPos = agentPos;
            float minDist = float.MaxValue;
            var nearLinkList = nearTile.GetLinkListToNextTile();
            var farLinkList = farTile.GetLinkListToNextTile();

            nearLinkList.ForEach(t => t.DisableLink());
            farLinkList.ForEach(t => t.DisableLink());

            for (int i = 0; i < nearLinkList.Count; i++)
            {
                Vector3 nearLinkEndPos = nearLinkList[i].linkEndPos;
                for (int j = 0; j < farLinkList.Count; j++)
                {
                    Vector3 farLinkStartPos = farLinkList[j].linkStartPos;
                    bool hasPath = NavMesh.CalculatePath(nearLinkEndPos, farLinkStartPos, areaMaskDetail, _navmeshPath);
                    if(hasPath && _navmeshPath.status == NavMeshPathStatus.PathComplete)
                    {
                        float dist = Vector3.SqrMagnitude(farLinkStartPos - nearLinkEndPos);
                        if (dist < minDist)
                        {
                            Debug.DrawLine(nearLinkEndPos, farLinkStartPos, Color.red, 5.0f);
                            targetPos = nearLinkEndPos;
                            minDist = dist;
                        }
                    }
                }
            }

            nearLinkList.ForEach(t => t.EnableLink());
            farLinkList.ForEach(t => t.EnableLink());
            //Debug.LogError("没有路径");
            return targetPos;
        }

        // ----------------根据寻路信息移动Agent---------------------
        private void TryMoveAgent()
        {
            //todo 现在用的是NavmeshAgent 不需要处理
        }
        #endregion
    }
}