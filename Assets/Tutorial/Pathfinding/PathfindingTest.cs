using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class TileInfo
{
    public int x;
    public int y;
    public Vector3 enterPos;
    public Vector3 exitPos;
}

public class LinkInfo
{
    public LinkInfo(NavMeshLink l)
    {
        link = l;
    }

    public NavMeshLink link;
    public Vector2Int tile1;
    public Vector2Int tile2;
    public bool isActive = false;

    public void EnableLink(Vector2Int tileOne, Vector2Int tileTwo)
    {
        isActive = true;
        link.enabled = true;
        tile1 = tileOne;
        tile2 = tileTwo;
    }

    public void DisableLink()
    {
        isActive = false;
        link.enabled = false;
    }

    public bool IsLinked(Vector2Int tileOne, Vector2Int tileTwo)
    {
        if ((tile1 == tileOne && tile2 == tileTwo) || (tile1 == tileTwo && tile2 == tileOne))
        {
            return true;
        }
        return false;
    }
}

// todo 生成离散的Link,Link点的选择过程
public class PathfindingTest : MonoBehaviour
{
    public const int MAX_WAYPOINT = 200;
    public float tileSize;
    public NavMeshAgent agent;
    public GameObject dest;
    private NavMeshPath path;
    public NavmeshTile currentTile;
    private WaitForEndOfFrame waitForEndFrame = new WaitForEndOfFrame();
    public int abstractPathWayPointCount = 0;
    private Vector3[] abstractPath = new Vector3[MAX_WAYPOINT];
    public int abstractPathVisitedIndex = 0;

    public List<TileInfo> tileInfoList = new List<TileInfo>();
    private List<Vector2Int> _tempTileIndexList = new List<Vector2Int>();
    private HashSet<Vector2Int> indexSet = new HashSet<Vector2Int>();
    private int mapGridSize;
    private NavmeshTile navmeshTilePrefab;
    private Transform detailedNavmesh;
    private int areaMaskDetail;
    private int areaMaskWalkable;
    public bool hasAbstractPath = false;

    public NavmeshTileLink tileLink;
    private void Awake()
    {
        mapGridSize =(int) (8192.0f / tileSize);
        detailedNavmesh = new GameObject("DetailedNavmesh").transform;

    }

    private void Start()
    {
        navmeshTilePrefab = Resources.Load<NavmeshTile>("NavmeshTile");
        navmeshTilePrefab.size = tileSize;
        areaMaskDetail = 1 << NavMesh.GetAreaFromName("Detail");
        areaMaskWalkable = 1 << NavMesh.GetAreaFromName("Walkable");
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            FindAbstractPath();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            FindDetailedPath();
        }
        if (hasAbstractPath == false)
        {
            return;
        }
        if (Vector3.Distance(agent.transform.position, dest.transform.position) < 2.5f)
        {
            agent.enabled = false;
            hasAbstractPath = false;
            return;
        }

        CheckUnusedLink();

        GenerateDetailedNavmesh();
        
        if ((!agent.hasPath && hasAbstractPath) || agent.isStopped)
        {
            //Debug.Log("Find path");
            FindDetailedPath();
        }
        // debug
        return;
        if (agent.isOnNavMesh == false)
        {
            Debug.LogError("Agent not on navmesh");
        }

        if (agent.isOnOffMeshLink)
        {
            Debug.LogError("Agent on off mesh link");
        }
    }

    public void FindAbstractPath()
    {
        visitedTiles.Clear();
        abstractPathVisitedIndex = 0;
        GetPathFromArea("Walkable", ref abstractPath, ref abstractPathWayPointCount);
        if (abstractPath == null || abstractPath.Length <= 1)
        {
            Debug.Log("No valid path");
            return;
        }
        PreCalculateTileAlongPath(abstractPath);
        hasAbstractPath = true;
    }

    public void FindDetailedPath()
    {
        if(currentTile == null)
        {
            return;
        }
        agent.areaMask = areaMaskDetail;
        NavmeshTile farActiveTile = null;
        Vector2Int currentIndex = new Vector2Int(currentTile.tileIndexX, currentTile.tileIndexZ);
        foreach (KeyValuePair<Vector2Int, NavmeshTile> tPair in loadedTileDic)
        {
            NavmeshTile tile = tPair.Value;
            if (tile.isActive && tile.ContainsPosition(dest.transform.position))
            {
                agent.SetDestination(dest.transform.position);
                return;
            }
        }
        if (currentTile.ContainsPosition(dest.transform.position))
        {
            agent.SetDestination(dest.transform.position);
            return;
        }
        Vector3 curPos = currentTile.TileCenterPos().XZ();
        //Vector3 curPos = agent.transform.position.XZ();
        Vector3 dir = (abstractPath[abstractPathVisitedIndex].XZ() - curPos).normalized;
        float maxDist = float.NegativeInfinity;
        foreach (KeyValuePair<Vector2Int, NavmeshTile> tPair in loadedTileDic)
        {
            // 防止回到走过的寻路网格
            Vector3 nearTileDir = (tPair.Value.TileCenterPos().XZ() - curPos).normalized;
            float dot = Vector3.Dot(dir, nearTileDir);
            
            if (dot < 0)
            {
                continue;
            }
            float curDist = dot;
           // float curDist = Vector2Int.Distance(currentIndex, tPair.Value.tileIndex);
            if (curDist > maxDist)
            {
                farActiveTile = tPair.Value;
                maxDist = curDist;
            }
        }
        Vector3 pos = farActiveTile.GetNavmeshPosNearTileCenter();
        agent.ResetPath();
        agent.SetDestination(pos);
        Debug.Log(agent.pathStatus + " " + farActiveTile.tileIndex);
        Debug.DrawLine(pos,pos+Vector3.up*60.0f,Color.blue,5.0f);
        //GetPathFromArea("Detail",ref detailedPath,ref lastIndex);
    }

    private void GetPathFromArea(string areaName,ref Vector3[] pathResult,ref int lastIndex)
    {
        if (path == null)
        {
            path = new NavMeshPath();
        }

        int areaMask = 1 << NavMesh.GetAreaFromName(areaName);
        agent.areaMask = areaMask;
        lastIndex = 0;
        pathResult[lastIndex] = agent.transform.position;
        Vector3 endPos = dest.transform.position;
        // CalculatePath计算路径时不知道有啥限制算不完整，需要从中断点继续朝目标位置计算
        while (true)
        {
            Vector3 startPos = lastIndex == 0 ? pathResult[lastIndex] : pathResult[lastIndex - 1];
            float distance = Vector3.Distance(startPos, dest.transform.position);
            if (distance > 5.0f)
            {
                path.ClearCorners();
                bool succeed = NavMesh.CalculatePath(startPos, endPos, areaMask, path);
                if (succeed == false)
                {
                    Debug.LogError("Calculate path failed");
                    break;
                }

                if (path.status != NavMeshPathStatus.PathInvalid)
                {
                    int currentPathLength = path.corners.Length;
                    for (int i = lastIndex; i < currentPathLength + lastIndex; i++)
                    {
                        pathResult[i] = path.corners[i - lastIndex];
                    }
                    lastIndex += currentPathLength;
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

    private void PreCalculateTileAlongPath(Vector3[] path)
    {
        tileInfoList.Clear();
        indexSet.Clear();
        for (int i = 0; i < abstractPathWayPointCount - 1; i++)
        {
            Vector3 lineStart = path[i];
            Vector3 lineEnd = path[i + 1];
            Utility.GridTraverse(lineStart, lineEnd, tileSize, ref _tempTileIndexList);
            _tempTileIndexList.ForEach(v => { indexSet.Add(v); });
        }
    }

    #region Detailed path calculate
    private List<Vector2Int> nearbyTiles = new List<Vector2Int>();
    private HashSet<Vector2Int> visitedTiles = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, NavmeshTile> loadedTileDic = new Dictionary<Vector2Int, NavmeshTile>();
    private List<NavmeshTile> tilePool = new List<NavmeshTile>();
    private List<LinkInfo> linkPool = new List<LinkInfo>();
    private Vector2Int[] activeTiles = new Vector2Int[10];
    private List<LinkInfo> activeLinks = new List<LinkInfo>();
    private int activeTileCount = 0;
    private NavmeshTile GetOneNavmeshTile(int x, int z)
    {
        for (int i = 0; i < tilePool.Count; i++)
        {
            if (tilePool[i].isActive == false)
            {
                tilePool[i].EnableTile(x, z);
                return tilePool[i];
            }
        }
        NavmeshTile newTile = Instantiate(navmeshTilePrefab, detailedNavmesh);
        tilePool.Add(newTile);
        newTile.EnableTile(x, z);
        return newTile;
    }

    private LinkInfo GetOneNavmeshLink()
    {
        for (int i = 0; i < linkPool.Count; i++)
        {
            if (linkPool[i].isActive == false)
            {
                return linkPool[i];
            }
        }

        GameObject go = new GameObject("NavmeshLink");
        NavMeshLink navmeshlink = go.AddComponent<NavMeshLink>();
        go.transform.SetParent(detailedNavmesh, false);
        navmeshlink.width = tileSize*0.8f;
        navmeshlink.autoUpdate = true;
        navmeshlink.bidirectional = true;
        navmeshlink.area = NavMesh.GetAreaFromName("Walkable");
        navmeshlink.startPoint = new Vector3(0,0,-1f);
        navmeshlink.endPoint = new Vector3(0,0,1f);
        LinkInfo link = new LinkInfo(navmeshlink);
        linkPool.Add(link);
        link.DisableLink();
        return link;
    }

    private void GenerateDetailedNavmesh()
    {
        int x = (int)(agent.transform.position.x / tileSize);
        int z = (int)(agent.transform.position.z / tileSize);
        GetNeighbours(x, z, ref nearbyTiles);
        // 当前所处的位置也加进去
        nearbyTiles.Add(new Vector2Int(x, z));
        // 生成新的navmesh
        for (int i = nearbyTiles.Count - 1; i >= 0; i--)
        {
            if (indexSet.Contains(nearbyTiles[i]))
            {
                if (!loadedTileDic.ContainsKey(nearbyTiles[i]) && !visitedTiles.Contains(nearbyTiles[i]))
                {
                    NavmeshTile tile = GetOneNavmeshTile(nearbyTiles[i].x, nearbyTiles[i].y);
                    tile.OnTileExit -= OnTileExitCallback;
                    tile.OnTileEnter -= OnTileEnterCallback;
                    tile.OnTileExit += OnTileExitCallback;
                    tile.OnTileEnter += OnTileEnterCallback;
                    loadedTileDic.Add(nearbyTiles[i], tile);
                    //Debug.Log($"Load Tile ({x},{z})");
                }
            }
        }

        // 生成navmesh link
        loadedTileDic.Keys.CopyTo(activeTiles, 0);
        activeTileCount = loadedTileDic.Count;

        for (int i = 0; i < activeTileCount; i++)
        {
            for (int j = i + 1; j < activeTileCount; j++)
            {
                int dx = Mathf.Abs(activeTiles[i].x - activeTiles[j].x);
                int dy = Mathf.Abs(activeTiles[i].y - activeTiles[j].y);
                if ((dx == 1 && dy == 0) ||
                    (dy == 1 && dx == 0) )
                {
                    bool hasLinked = false;
                    for (int k = 0; k < activeLinks.Count; k++)
                    {
                        if (activeLinks[k].IsLinked(activeTiles[i], activeTiles[j]))
                        {
                            hasLinked = true;
                        }
                    }

                    if (!hasLinked)
                    {
                        LinkInfo link = GetOneNavmeshLink();
                        Vector3 centerPos = new Vector3(
                            0.5f * (activeTiles[i].x + activeTiles[j].x + 1) * tileSize,
                            0,
                            0.5f * (activeTiles[i].y + activeTiles[j].y + 1) * tileSize);
                        centerPos.y = Utility.GetTerrainHeight(centerPos);
                        link.link.transform.position = centerPos;
                        if (dy == 0)
                        {
                            link.link.transform.forward = Vector3.right;
                        }
                        else
                        {
                            link.link.transform.forward = Vector3.forward;
                        }
                        link.EnableLink(activeTiles[i], activeTiles[j]);
                        activeLinks.Add(link);
                        var linkInfo = tileLink.GenerateNavmeshLink(loadedTileDic[activeTiles[i]], loadedTileDic[activeTiles[j]]);
                        linkInfo.CreateNavmeshLink();
                    }
                }
            }
        }
    }
    private void CheckUnusedLink()
    {
        for (int i = 0; i < linkPool.Count; i++)
        {
            Vector2Int tile1 = linkPool[i].tile1;
            Vector2Int tile2 = linkPool[i].tile2;
            if (linkPool[i].isActive && !loadedTileDic.ContainsKey(tile1) && !loadedTileDic.ContainsKey(tile2))
            {
                linkPool[i].DisableLink();
                if (activeLinks.Contains(linkPool[i]))
                {
                    activeLinks.Remove(linkPool[i]);
                    Debug.Log($"Remove from active link -> ({linkPool[i].tile1}) - ({linkPool[i].tile2})");
                }
            }
        }
    }

    // Tile进出时的回调
    private void OnTileEnterCallback(int x, int z)
    {
        Debug.Log($"Enter tile ({x},{z})");
        currentTile = loadedTileDic[new Vector2Int(x, z)];
        if (currentTile.ContainsPosition(dest.transform.position))
        {
            agent.enabled = false;
            agent.enabled = true;
            FindDetailedPath(); 
        }
        if (currentTile.ContainsPosition(abstractPath[abstractPathVisitedIndex]))
        {
            abstractPathVisitedIndex++;
        }
        //FindDetailedPath();
    }
    private List<Vector2Int> tileToUnload = new List<Vector2Int>();
    private void OnTileExitCallback(int x, int z)
    {
        Vector2Int currentExit = new Vector2Int(x, z);
        tileToUnload.Add(currentExit);
        for (int i = tileToUnload.Count-1; i >=0; i--)
        {
            Vector2Int waitForUnload = tileToUnload[i];
            if (loadedTileDic.ContainsKey(waitForUnload))
            {
                visitedTiles.Add(new Vector2Int(x, z));
                loadedTileDic[waitForUnload].DisableTile();
                loadedTileDic.Remove(waitForUnload);
            }
            tileToUnload.RemoveAt(i);
        }
        Debug.Log($"Exit tile ({x},{z})");
    }

    private bool InRange(int x, int z)
    {
        return x >= 0 && z >= 0 && x < mapGridSize && z < mapGridSize;
    }

    private void GetNeighbours(int x, int z, ref List<Vector2Int> neibourIndexs)
    {
        neibourIndexs.Clear();

        if (InRange(x - 1, z)) neibourIndexs.Add(new Vector2Int(x - 1, z));
        if (InRange(x, z + 1)) neibourIndexs.Add(new Vector2Int(x, z + 1));
        if (InRange(x + 1, z)) neibourIndexs.Add(new Vector2Int(x + 1, z));
        if (InRange(x, z - 1)) neibourIndexs.Add(new Vector2Int(x, z - 1));

        if (InRange(x - 1, z - 1)) neibourIndexs.Add(new Vector2Int(x - 1, z - 1));
        if (InRange(x - 1, z + 1)) neibourIndexs.Add(new Vector2Int(x - 1, z + 1));
        if (InRange(x + 1, z + 1)) neibourIndexs.Add(new Vector2Int(x + 1, z + 1));
        if (InRange(x + 1, z - 1)) neibourIndexs.Add(new Vector2Int(x + 1, z - 1));
    }


    #endregion

    private void Draw(Vector2Int index)
    {
        Handles.color = Color.white;
        int x = index.x;
        int z = index.y;
        Vector3 center = new Vector3(x * tileSize + 0.5f * tileSize, 0, z * tileSize + 0.5f * tileSize);
        Handles.DrawWireCube(center, new Vector3(tileSize, 0.01f, tileSize));
        if (currentTile != null && index.x == currentTile.tileIndexX && index.y == currentTile.tileIndexZ)
        {
            Handles.Label(center, String.Format("Current ({0},{1})", x, z));
        }
        else
        {
            Handles.Label(center, String.Format("({0},{1})", x, z));
        }
    }

    public bool drawAbstractPath;
    private void OnDrawGizmos()
    {
        if (drawAbstractPath && indexSet != null && indexSet.Count != 0)
        {
            foreach (Vector2Int index in indexSet)
            {
                Draw(index);
            }
        }

        Handles.color = Color.yellow;
        for (int i = 0; i < activeLinks.Count; i++)
        {
            Handles.Label(activeLinks[i].link.transform.position,$"Link ({activeLinks[i].tile1})-({activeLinks[i].tile2})");
        }

        Handles.color = new Color(0.4f,0.2f,0.89f);
        Handles.DrawWireDisc(agent.transform.position,Vector3.up, 2.0f);
        Handles.color = new Color(0.95f,0.2f,0.1f);
        Handles.DrawWireDisc(dest.transform.position,Vector3.up,2.0f);
    }
}
