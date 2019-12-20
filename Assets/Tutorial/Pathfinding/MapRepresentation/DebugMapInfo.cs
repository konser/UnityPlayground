using System;
using System.Collections.Generic;
using System.Linq;
using DataStructure;
using RuntimePathfinding;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public class DebugMapInfo : MonoBehaviour
{
    public bool _drawPathGraph;
    public bool _drawAbstractPath;
    public bool _drawFinalPath;
    
    public string dataFileLevel1;
    public string dataFileLevel2;
    public GameObject start;
    public GameObject end;
    public NavMeshSurface surface;
    public bool findDetailedPath;
    private NavigationMapInfo mapInfoLevel_1;
    private NavigationMapInfo mapInfoLevel_2;
    private NavMeshPath navmeshPath;
    private List<NavigationNode> _currentPath = new List<NavigationNode>(128);
    private List<Vector3> finalPath = new List<Vector3>(300);
    private int areaMask;

    //------------------------------------------
    private NavMeshWorld world;
    private NavMeshLocation location;

    private void Start()
    {
        LoadMapInfo();
        areaMask = 1 << NavMesh.GetAreaFromName("Walkable");
        surface = new GameObject("Surface").AddComponent<NavMeshSurface>();
        surface.defaultArea = NavMesh.GetAreaFromName("Walkable");
        surface.collectObjects = CollectObjects.Volume;
        surface.layerMask = LayerMask.GetMask("Obstacle") | LayerMask.GetMask("Terrain");
        navmeshPath = new NavMeshPath();
        Debug.Log("Map loaded");
    }

    public void SetNavmeshSurface(TileIdentifier tileID, NavigationMapInfo mapInfo)
    {
        surface.size = mapInfo.tileSize + new Vector3(2.0f, 0, 2.0f);
        surface.center = GetTileCenterPosition(tileID, mapInfo.tileSize);
    }

    private Vector3 GetTileCenterPosition(TileIdentifier tileID, Vector3 tileSize)
    {
        Vector3 center = new Vector3((tileID.coordX + 0.5f) * tileSize.x, 0.45f * tileSize.y, (tileID.coordZ + 0.5f) * tileSize.z);
        center.y += Utility.GetTerrainHeight(center);
        return center;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SearchOnLevel1();
        }
    }

    [ContextMenu("Load")]
    public void LoadMapInfo()
    {
        mapInfoLevel_1 = Utility.DeserializeBinaryData(dataFileLevel1) as NavigationMapInfo;
        mapInfoLevel_2 = Utility.DeserializeBinaryData(dataFileLevel2) as NavigationMapInfo;
        mapInfoLevel_1.InitMapInfo();
        mapInfoLevel_2.InitMapInfo();
    }

    /*
     * 流程：
     * 确定起点终点区域
     * 相同->按navmesh寻路->结束
     * 不同->按MapInfo得到经过的结点，烘焙结点所属地块
     *     -->当前位置到结点 navmesh寻路
     *     -->该结点到下一个穿越结点，按mapinfo直接行进
     */

    [ContextMenu("Search On Level 1")]
    public void SearchOnLevel1()
    {
        finalPath.Clear();
        _currentPath.Clear();
        TileIdentifier startTile = mapInfoLevel_1.GetTileIdentifier(start.transform.position);
        TileIdentifier endTile = mapInfoLevel_1.GetTileIdentifier(end.transform.position);
        List<NavigationNode> startNodes = mapInfoLevel_1.navigationGraph.GetTileNodes(startTile);
        List<NavigationNode> endNodes = mapInfoLevel_1.navigationGraph.GetTileNodes(endTile);

        Debug.Log($"{startTile} Start Nodes Count :{startNodes.Count} \n" +
                  $"    {endTile} End Nodes Count : {endNodes.Count}");
        SetNavmeshSurface(startTile, mapInfoLevel_1);
        surface.BuildNavMesh();
        if (startTile == endTile)
        {
            NavMesh.CalculatePath(start.transform.position, end.transform.position, areaMask, navmeshPath);
            if (navmeshPath.status == NavMeshPathStatus.PathComplete)
            {
                finalPath.AddRange(navmeshPath.corners);
            }
            return;
        }

        bool hasPath = false;
        for (int i = 0; i < startNodes.Count; i++)
        {
            NavMesh.CalculatePath(start.transform.position, startNodes[i].value.position, areaMask, navmeshPath);
            Debug.Log(navmeshPath.status);
            if (navmeshPath.status != NavMeshPathStatus.PathComplete)
            {
                Debug.DrawLine(start.transform.position, startNodes[i].value.position, Color.red, 5f);
                continue;
            }
            for (int j = 0; j < endNodes.Count; j++)
            {
                hasPath = mapInfoLevel_1.navigationGraph.Search(startNodes[i], endNodes[j], _currentPath);
                if (hasPath)
                {
                    break;
                }
            }
            if (hasPath)
            {
                Debug.Log("Find abstract path! Count : " + _currentPath.Count);
                break;
            }
        }

        if (hasPath && findDetailedPath)
        {
            Vector3 startPos = start.transform.position;
            Vector3 endPos = end.transform.position;

            for (int i = 0; i < _currentPath.Count; i++)
            {
                if (_currentPath[i].value.ownerTileID == startTile && i == 0)
                {
                    finalPath.AddRange(GetPathInNavmesh(startTile, startPos, _currentPath[i].value.position));
                }

                if (_currentPath[i].value.ownerTileID == endTile && i == _currentPath.Count - 1)
                {
                    finalPath.AddRange(GetPathInNavmesh(endTile, _currentPath[i].value.position, endPos));
                    return;
                }

                bool isCrossTile = mapInfoLevel_1.navigationGraph.IsCrossTileNeibour(_currentPath[i], _currentPath[i + 1]);
                if (isCrossTile)
                {
                    finalPath.Add(_currentPath[i].value.position);
                    finalPath.Add(_currentPath[i + 1].value.position);
                }
                else
                {
                    finalPath.AddRange(GetPathInNavmesh(_currentPath[i].value.ownerTileID, _currentPath[i].value.position, _currentPath[i + 1].value.position));
                }
            }
        }
    }

    private Vector3[] GetPathInNavmesh(TileIdentifier tile, Vector3 startPos, Vector3 endPos)
    {
        SetNavmeshSurface(tile, mapInfoLevel_1);
        surface.BuildNavMesh();
        //NavMesh.SamplePosition(startPos, out var startHit, 0.4f, areaMask);
        //NavMesh.SamplePosition(startPos, out var endHit, 0.4f, areaMask);
        NavMesh.CalculatePath(startPos, endPos, areaMask, navmeshPath);
        if (navmeshPath.status == NavMeshPathStatus.PathComplete)
        {
            return navmeshPath.corners;
        }
        Debug.LogError("Error Path " + navmeshPath.status + " " + tile);
        Debug.DrawLine(startPos, endPos, Color.red, 10f);
        return new Vector3[0];
    }

    private void OnDrawGizmos()
    {
        if (_drawPathGraph)
        {
            Handles.color = Color.white;
            mapInfoLevel_1.navigationGraph.IterateBFS(mapInfoLevel_1.navigationGraph.nodeList[0], DrawNodeConnections);
            //Handles.color = Color.green;
            //mapInfoLevel_2.navigationGraph.IterateBFS(mapInfoLevel_2.navigationGraph.nodeList[0], DrawNodeConnections);
        }
        if (Application.isPlaying)
        {
            if (_currentPath != null && _drawAbstractPath)
            {
                for (int i = 0; i < _currentPath.Count - 1; i++)
                {
                    bool isCross = mapInfoLevel_1.navigationGraph.IsCrossTileNeibour(_currentPath[i], _currentPath[i + 1]);
                    if (isCross)
                    {
                        //Handles.Label(_currentPath[i].value.position,$"{_currentPath[i].value.ownerTileID} -> {_currentPath[i + 1].value.ownerTileID}");
                        Handles.Label(_currentPath[i].value.position, $"--->");
                        Gizmos.color = Color.red;
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;
                    }
                    Handles.Label(_currentPath[i].value.position, $"{_currentPath[i].value.ownerTileID}");
                    Handles.Label(_currentPath[i + 1].value.position, $"{_currentPath[i + 1].value.ownerTileID}");
                    Gizmos.DrawLine(_currentPath[i].value.position, _currentPath[i + 1].value.position);
                }
            }

            if (_drawFinalPath)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < finalPath.Count - 1; i++)
                {
                    Handles.Label(finalPath[i], i.ToString());
                    Gizmos.DrawLine(finalPath[i], finalPath[i + 1]);
                }
            }
        }
    }

    private void DrawNodeConnections(GraphNode<LinkPoint> linkPointNode)
    {
        for (int i = 0; i < linkPointNode.neiboursCount; i++)
        {
            Vector3 p1 = linkPointNode.value.position;
            Vector3 p2 = linkPointNode.neibours[i].value.position;
            Handles.DrawLine(p1, p2);
            //float d = (Vector3.Distance(p1, p2) / 10f);
            //Handles.DrawDottedLine(p1,p2,d);
        }
    }
}
