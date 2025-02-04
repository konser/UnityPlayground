﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputationalGeometry;
using DataStructure;
using RuntimePathfinding;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using NavMeshBuilder = UnityEditor.AI.NavMeshBuilder;
using Object = System.Object;

enum EBakeStage
{
    CollectTriangle,
    SamplePoint,
    ConstructGraph
}

enum ETileRelativeDir
{
    NotNeibour,
    Left,
    Right,
    Up,
    Bottom
}

public class TileConnection
{
    public TileIdentifier[] connectedTiles; // 相邻的区块
    public Bounds bound; // 范围
    public List<Triangle> triangleList = new List<Triangle>(128);
    public List<Vector3> randomPointList = new List<Vector3>(512);
    public List<BakedRegion> regionList = new List<BakedRegion>(16);
    public NavMeshData cachedNavmeshData;
    public TileConnection(TileIdentifier tileOne, TileIdentifier tileTwo)
    {
        connectedTiles = new[] { tileOne, tileTwo };
    }

    public void GetBakedPointInTile(TileIdentifier tileID, List<BakedPoint> result)
    {
        if (ContainsTileID(tileID) == false)
        {
            return;
        }
        for (int i = 0; i < regionList.Count; i++)
        {
            BakedPoint bakedPointA = regionList[i].bakedPointA;
            BakedPoint bakedPointB = regionList[i].bakedPointB;
            if (bakedPointA != null && bakedPointB != null)
            {
                if (bakedPointA.ownerTile.Equals(tileID))
                {
                    result.Add(bakedPointA);
                }
                else if (bakedPointB.ownerTile.Equals(tileID))
                {
                    result.Add(bakedPointB);
                }
            }
        }
    }

    private bool ContainsTileID(TileIdentifier tileID)
    {
        bool contains = false;
        for (int i = 0; i < connectedTiles.Length; i++)
        {
            if (connectedTiles[i].Equals(tileID))
            {
                contains = true;
                break;
            }
        }
        return contains;
    }
}

public class BakedPoint : IConvexPoint
{
    public TileIdentifier ownerTile;
    public Vector3 pos;
    public int regionID;
    public int subregionID;
    private NavigationNode _graphNode;

    public NavigationNode ToGraphNode()
    {
        if (_graphNode == null)
        {
            _graphNode = new NavigationNode(new LinkPoint
            {
                ownerTileID = ownerTile,
                position = pos
            });
        }
        return _graphNode;
    }

    public Vector3 position
    {
        get { return pos; }
    }
}
public class DownsamplingVoxel
{
    public int x;
    public int y;
    public int z;
    public Vector3 averagePos;
    public bool isEmptyVoxel = true;
    public bool hasAssignedRegion;
    public void SetAvgPosition(Vector3 newPos)
    {
        averagePos = 0.5f * (averagePos + newPos);
    }
}

public class TerrainHeightSampler
{
    private Vector3[] terrainPosMin;
    private Vector3[] terrainPosMax;
    private float[][,] terrainHeightMaps;
    private float terrainTileSizeX;
    private float terrainTileSizeZ;
    private Vector3 heightmapScale;
    private int heightmapRes;
    public TerrainHeightSampler()
    {
        InitTerrainData();
    }

    void InitTerrainData()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains.Length == 0)
        {
            return;
        }
        // 此处假定每块terrain的尺寸是相等的
        terrainTileSizeX = terrains[0].terrainData.size.x;
        terrainTileSizeZ = terrains[0].terrainData.size.z;
        heightmapScale = terrains[0].terrainData.heightmapScale;
        heightmapScale.y = terrains[0].terrainData.size.y;
        heightmapRes = terrains[0].terrainData.heightmapResolution;
        terrainPosMin = new Vector3[terrains.Length];
        terrainPosMax = new Vector3[terrains.Length];
        terrainHeightMaps = new float[terrains.Length][,];
        for (int i = 0; i < terrains.Length; i++)
        {
            TerrainData data = terrains[i].terrainData;
            terrainPosMin[i] = terrains[i].GetPosition();
            terrainPosMax[i] = terrainPosMin[i] + data.size;
            terrainHeightMaps[i] = data.GetHeights(0, 0, heightmapRes, heightmapRes);
        }
    }

    public float GetTerrainHeight(Vector3 worldPos)
    {
        int index = -1;
        for (int i = 0; i < terrainPosMin.Length; i++)
        {
            Vector3 min = terrainPosMin[i];
            Vector3 max = terrainPosMax[i];
            if (worldPos.x >= min.x && worldPos.x <= max.x && worldPos.z >= min.z && worldPos.z <= max.z)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
        {
            Debug.LogError($"GetTerrainHeight : Invalid world pos {worldPos}");
            return 0;
        }
        Vector3 relativePos = worldPos - terrainPosMin[index];
        int x = (int)((relativePos.x / terrainTileSizeX) * heightmapRes);
        int z = (int)((relativePos.z / terrainTileSizeZ) * heightmapRes);
        return terrainHeightMaps[index][z, x] * heightmapScale.y + terrainPosMin[index].y;
    }
}

public class BakedRegion
{
    public int regionID = -1;
    public List<Vector3> downsamplePoints = new List<Vector3>(256);
    public List<BakedPoint> subregionAPoints = new List<BakedPoint>(128);
    public List<BakedPoint> subregionBPoints = new List<BakedPoint>(128);
    public List<BakedPoint> subRegionAConvexHull = new List<BakedPoint>();
    public List<BakedPoint> subRegionBConvexHull = new List<BakedPoint>();
    public Vector3[] convexHullA;
    public Vector3[] convexHullB;

    public float distanceAB = Single.MaxValue;
    public BakedPoint bakedPointA;
    public BakedPoint bakedPointB;

    public void CreateConvexRegion()
    {
        if (subregionAPoints.Count != 0)
        {
            if (ConvexHull2D<BakedPoint>.GetConvexHull2D(subregionAPoints, subRegionAConvexHull))
            {
                convexHullA = new Vector3[subRegionAConvexHull.Count];
                for (int i = 0; i < subRegionAConvexHull.Count; i++)
                {
                    convexHullA[i] = subRegionAConvexHull[i].position;
                }
            }
        }

        if (subregionBPoints.Count != 0)
        {
            if (ConvexHull2D<BakedPoint>.GetConvexHull2D(subregionBPoints, subRegionBConvexHull))
            {
                convexHullB = new Vector3[subRegionBConvexHull.Count];
                for (int i = 0; i < subRegionBConvexHull.Count; i++)
                {
                    convexHullB[i] = subRegionBConvexHull[i].position;
                }
            }
        }

        if (convexHullA != null && convexHullB != null)
        {
            float minDist = Single.MaxValue;
            for (int i = 0; i < convexHullA.Length; i++)
            {
                for (int j = 0; j < convexHullB.Length; j++)
                {
                    float d = (convexHullB[j] - convexHullA[i]).sqrMagnitude;
                    // 不要距离过近的点
                    if (d < minDist && d > 1.0f)
                    {
                        minDist = d;
                        bakedPointA = subRegionAConvexHull[i];
                        bakedPointB = subRegionBConvexHull[j];
                    }
                }
            }
            distanceAB = minDist;
        }
    }
}

public class LinkNodeBakeWindow : EditorWindow
{
    #region Helper Method
    // 线程控制相关变量
    Object _locker = new Object(); // 线程锁对象
    private int _threadAvailableCount = THREAD_COUNT;
    private int _threadProcessedCount = 0;
    private int _threadTargetProcessCount = 0;
    // 每次尝试去分配任务时，加1，无论成功与否
    private int _threadHelperIndex = 0;
    private void ResetThreadParams(int totalCountToBeProcessed)
    {
        _threadAvailableCount = THREAD_COUNT;
        _threadProcessedCount = 0;
        _threadTargetProcessCount = totalCountToBeProcessed;
        _threadHelperIndex = 0;
    }

    private IEnumerator AssignThreadTasksRoutine(Func<int, object> getThreadParamFunc, Func<object, bool> checkCanAssignTaskFunc, Action<object> action, string guiUpdateMessage)
    {
        _threadHelperIndex = 0;
        while (_threadProcessedCount < _threadTargetProcessCount)
        {
            if (_threadAvailableCount <= 0)
            {
                Debug.Log("Wait for idle thread...");
                yield return null;
            }
            else
            {
                // 每次yield中分配线程任务
                for (int i = 0; i < _threadAvailableCount; i++)
                {
                    if (_threadHelperIndex >= _tileCountTotal)
                    {
                        break;
                    }
                    object obj = getThreadParamFunc.Invoke(_threadHelperIndex);
                    bool canAssignTask = checkCanAssignTaskFunc.Invoke(obj);
                    _threadHelperIndex++;

                    if (!canAssignTask)
                    {
                        continue;
                    }
                    //Debug.Log("");
                    Task.Factory.StartNew(action, obj);

                    lock (_locker)
                    {
                        _threadAvailableCount--;
                    }
                }
                yield return null;
            }
            GUIUpdateProgress((float)_threadProcessedCount / _threadTargetProcessCount, guiUpdateMessage, true);
        }
        // wait for thread execute
        while (_threadAvailableCount != THREAD_COUNT)
        {
            yield return null;
        }
    }

    private Vector3 GetTileCenterPosition(TileIdentifier tileID)
    {
        Vector3 center = new Vector3(
            (tileID.coordX + 0.5f) * _config.tileSize.x+_sectorOffsetX, 
            VERTICAL_POS_OFFSET_RATIO * _config.tileSize.y, 
            (tileID.coordZ + 0.5f) * _config.tileSize.z +_sectorCountZ);
        center.y += _terrianHeightSampler.GetTerrainHeight(center);
        return center;
    }

    private static Dictionary<Vector2Int, ETileRelativeDir> _s_relativeDirDic = new Dictionary<Vector2Int, ETileRelativeDir>
    {
        {new Vector2Int(1,0),ETileRelativeDir.Right },
        {new Vector2Int(-1,0),ETileRelativeDir.Left },
        {new Vector2Int(0,1),ETileRelativeDir.Up },
        {new Vector2Int(0,-1),ETileRelativeDir.Bottom },
    };
    /// <summary>
    /// B相对于A的方向
    /// </summary>
    private ETileRelativeDir TileRelativeDirBtoA(TileIdentifier tileA, TileIdentifier tileB)
    {
        Vector2Int dir = new Vector2Int(tileB.coordX - tileA.coordX, tileB.coordZ - tileA.coordZ);
        if (_s_relativeDirDic.ContainsKey(dir))
        {
            return _s_relativeDirDic[dir];
        }
        return ETileRelativeDir.NotNeibour;
    }

    public TileConnection[] GetTileConnections(TileIdentifier id)
    {
        if (_tileConnectionDic.ContainsKey(id))
        {
            return _tileConnectionDic[id];
        }
        return null;
    }

    #endregion

    [MenuItem("Tools/Bake Map")]
    static void BakeMapWindow()
    {
        _s_windowInstance = EditorWindow.GetWindow<LinkNodeBakeWindow>();
        _s_windowInstance.minSize = new Vector2(_s_windowWidth, _s_windowHeight);
        _s_windowInstance.maxSize = new Vector2(_s_windowWidth, _s_windowHeight);
        _s_windowInstance.ShowTab();
    }
    private static float _s_windowWidth = 768f;
    private static float _s_windowHeight = 960;
    private static LinkNodeBakeWindow _s_windowInstance;


    private TerrainHeightSampler _terrianHeightSampler;
    private void Awake()
    {
        if (_inited == false)
        {
            Initialize();
            GUIInitialize();
            _terrianHeightSampler = new TerrainHeightSampler();
            _inited = true;
        }
    }

    private void OnDestroy()
    {
        DestroyImmediate(_bakeLinkSurface.gameObject);
        DestroyImmediate(_bakeTileOneSurface.gameObject);
        DestroyImmediate(_bakeTileTwoSurface.gameObject);

        if (_mainRoutine != null)
        {
            EditorCoroutineUtility.StopCoroutine(_mainRoutine);
        }
        EditorApplication.update -= EditorUpdateCallback;
        SceneView.duringSceneGui -= this.OnSceneGUI;
        Resources.UnloadUnusedAssets();
        _s_windowInstance = null;
    }

    #region --------------------------------GUI-------------------------------------------------------

    private float _guiProgress;
    private string _guiProgressMsg = "Nothing happens";
    private void GUIInitialize()
    {
        GUIClearProgress();
    }

    private void OnFocus()
    {
        EditorApplication.update -= EditorUpdateCallback;
        EditorApplication.update += EditorUpdateCallback;
        // Remove delegate listener if it has previously
        // been assigned.
        SceneView.duringSceneGui -= this.OnSceneGUI;
        // Add (or re-add) the delegate.
        SceneView.duringSceneGui += this.OnSceneGUI;
    }
    /*
    ************************************   Editor     GUI  *************************************************
    ********************************************************************************************************
    */
    private void OnGUI()
    {
        if (_inited == false)
        {
            GUILayout.Label("Init Failed!");
            return;
        }
        if (GUILayout.Button("BakeMap") && !_bakeStarted)
        {
            _bakeStarted = true;
            BakeMap();
        }
        EditorGUI.ProgressBar(new Rect(new Vector2(60f,20f), new Vector2(640f, 30f)), _guiProgress, _guiProgressMsg);
        EditorGUILayout.Space(60);
        EditorGUILayout.LabelField($"Sector Count {_sectorCountX} - {_sectorCountZ}  | Tile Count {_tileCountX} - {_tileCountZ}");
        GUIDrawSectorIndex();
        GUIDrawTileGrid();
    }

    private void GUIDrawTileGrid()
    {
        float width = 640f;
        float height = 640f;
        Vector2 gridSize = new Vector2(width / _tileCountX, height / _tileCountZ);
        Vector2 startPos = new Vector2(60f, _s_windowInstance.position.size.y - height - 15f);
        Color succeedColor = new Color(0.2f, 0.8f, 0.2f);
        for (int i = 0; i < _tileCountX; i++)
        {
            for (int j = 0; j < _tileCountZ; j++)
            {
                Rect rect = new Rect(
                    new Vector2(startPos.x + i * gridSize.x, startPos.y + (_tileCountZ - 1 - j) * gridSize.y),
                    gridSize * 0.8f
                );
                if (_tileBakedArray[i, j])
                {
                    EditorGUI.DrawRect(rect, succeedColor);
                }
                else
                {
                    EditorGUI.DrawRect(rect, Color.gray);
                }
            }
        }
    }

    private void GUIDrawSectorIndex()
    {
        StringBuilder bakedSector = new StringBuilder(64);
        bakedSector.Append("Baked: \n");
        for (int i = 0; i < _bakedSector.Count; i++)
        {
            bakedSector.Append($"{_bakedSector[i]} ");
        }
        bakedSector.Append("Remain: \n");
        for (int i = 0; i < _sectorList.Count; i++)
        {
            bakedSector.Append($"{_sectorList[i]} ");
        }
        EditorGUILayout.LabelField(" ");
    }

    private void GUIClearProgress()
    {
        _guiProgress = 0f;
        _guiProgressMsg = "Nothing happens";
        _tileBakedArray = new bool[_tileCountX, _tileCountZ];
    }

    private void GUISetTileSucceed(int x, int z)
    {
        _tileBakedArray[x, z] = true;
    }

    private void GUISetTileSucceed(TileIdentifier id)
    {
        _tileBakedArray[id.coordX, id.coordZ] = true;
    }

    private void GUIUpdateProgress(float val, string msg, bool repaint = true)
    {
        _guiProgress = val;
        _guiProgressMsg = msg;
        if (repaint)
        {
            _s_windowInstance.Repaint();
        }
    }

    /*
     ************************************ 画在场景里的辅助视图 *************************************************
     ********************************************************************************************************
     */
    private bool _enableDebugDraw;

    private bool _sceneDrawWorldBound;
    private bool _drawTileBound;
    private bool _drawTileConnectionBound;

    private bool _drawTileTriangles;
    private bool _drawTileConnectionTriangles;

    private bool _drawRandomPoints;
    private bool _drawTerrainTriangle;
    private bool _drawConvexPolygon;

    private bool _drawFinalGraph;
    private Vector2Int _tileID;
    private void OnSceneGUI(SceneView sceneView)
    {
        // GUI
        Handles.BeginGUI();
        _enableDebugDraw = GUILayout.Toggle(_enableDebugDraw, "Enable Debug Draw");
        _drawTileBound = GUILayout.Toggle(_drawTileBound, "Draw Tile Bound");
        _drawFinalGraph = GUILayout.Toggle(_drawFinalGraph, "Draw Final Graph");
        _drawConvexPolygon = GUILayout.Toggle(_drawConvexPolygon, "Draw Convex Polygons");
        GUILayout.Space(20);
        _sceneDrawWorldBound = GUILayout.Toggle(_sceneDrawWorldBound, "Draw World Bound");
        _drawTileConnectionBound = GUILayout.Toggle(_drawTileConnectionBound, "Draw Tile Connection Bound");
        GUILayout.Space(10);
        _drawTileTriangles = GUILayout.Toggle(_drawTileTriangles, "Draw Tile Triangles");
        _drawTileConnectionTriangles = GUILayout.Toggle(_drawTileConnectionTriangles, "Draw Tile Connection Triangles");
        GUILayout.Space(10);
        _drawTerrainTriangle = GUILayout.Toggle(_drawTerrainTriangle, "Draw Terrain Triangles");
        _drawRandomPoints = GUILayout.Toggle(_drawRandomPoints, "Draw Random Points");
        Handles.EndGUI();

        if (!_enableDebugDraw)
        {
            return;
        }

        // Handles
        if (_sceneDrawWorldBound)
        {
            Handles.color = Color.blue;
            Handles.DrawWireCube(_worldBound.center, _worldBound.size);
        }


        if (_drawTileTriangles && _tileTriangleDic != null)
        {
            Handles.color = Color.green;
            foreach (KeyValuePair<TileIdentifier, List<Triangle>> tPair in _tileTriangleDic)
            {
                foreach (Triangle triangle in tPair.Value)
                {
                    DrawTriangle(triangle);
                }
            }
        }

        if (_drawTileBound && _tileBoundsDic != null)
        {
            foreach (KeyValuePair<TileIdentifier, Bounds> tPair in _tileBoundsDic)
            {
                Handles.DrawWireCube(tPair.Value.center, tPair.Value.size);
            }
        }

        if (_drawTileConnectionTriangles && _tileConnectionDic != null)
        {
            Handles.color = Color.blue;
            foreach (var tPair in _tileConnectionDic)
            {
                foreach (TileConnection tTileConnection in tPair.Value)
                {
                    for (int i = 0; i < tTileConnection.triangleList.Count; i++)
                    {
                        DrawTriangle(tTileConnection.triangleList[i]);
                    }
                }
            }
        }

        if (_drawTileConnectionBound && _tileConnectionDic != null)
        {
            Handles.color = Color.blue;
            foreach (KeyValuePair<TileIdentifier, TileConnection[]> tPair in _tileConnectionDic)
            {
                for (int i = 0; i < tPair.Value.Length; i++)
                {
                    Handles.DrawWireCube(tPair.Value[i].bound.center, tPair.Value[i].bound.size);
                }
            }
        }

        if (_drawConvexPolygon)
        {
            ItreateTileConnection(DrawTileConnectionPoints);
        }

        if (_drawFinalGraph && navigationGraph != null)
        {
            navigationGraph.IterateBFS(navigationGraph.nodeList[0], DrawNodeConnections);
        }

        Handles.color = Color.magenta;
        for (int i = 0; i < debugPoints.Count; i++)
        {
            Handles.DrawWireCube(debugPoints[i], new Vector3(0.05f, 0.01f, 0.05f));
        }
    }

    private void ItreateTileConnection(Action<TileConnection> action)
    {
        if (_tileConnectionDic != null)
        {
            foreach (KeyValuePair<TileIdentifier, TileConnection[]> tPair in _tileConnectionDic)
            {
                TileConnection[] connects = tPair.Value;
                for (int i = 0; i < connects.Length; i++)
                {
                    action?.Invoke(connects[i]);
                }
            }
        }
    }

    private void DrawNodeConnections(GraphNode<LinkPoint> linkPointNode)
    {
        for (int i = 0; i < linkPointNode.neiboursCount; i++)
        {
            if (i % 2 == 0)
            {
                Handles.color = Color.red;
            }
            else
            {
                Handles.color = Color.blue;
            }
            Handles.DrawLine(linkPointNode.value.position, linkPointNode.neibours[i].value.position);
        }
    }

    private void DrawTileConnectionPoints(TileConnection connection)
    {
        Color convexAColor = new Color(0.6f, 0.2f, 0.2f);
        Color convexBColor = new Color(0.3f, 0.5f, 0.2f);
        string connectionInfo = $"{connection.connectedTiles[0]} - {connection.connectedTiles[1]} ";
        foreach (BakedRegion region in connection.regionList)
        {
            if (region.convexHullA != null && region.convexHullB != null)
            {
                Handles.color = convexAColor;

                //Handles.Label(region.convexHullA[0], connectionInfo + region.regionID + " A");
                Handles.DrawAAConvexPolygon(region.convexHullA);

                Handles.color = convexBColor;

                //Handles.Label(region.convexHullB[0], connectionInfo + region.regionID + " B");
                Handles.DrawAAConvexPolygon(region.convexHullB);

                Handles.color = Color.blue;
                Handles.DrawLine(region.bakedPointA.pos, region.bakedPointB.pos);
            }
        }
    }

    private void DrawTriangle(Triangle triangle)
    {
        Handles.DrawLine(triangle.vertA, triangle.vertB);
        Handles.DrawLine(triangle.vertB, triangle.vertC);
        Handles.DrawLine(triangle.vertC, triangle.vertA);
    }

    #endregion

    /*
    ************************************        主流程      *************************************************
    ********************************************************************************************************
    */
    #region -------------------------------------------Main-------------------------------------------------------- 

    private const float VERTICAL_POS_OFFSET_RATIO = 0.3f;
    private const int THREAD_COUNT = 64;
    private const float NAVMESH_SAMPLE_RADIUS = 0.15f;
    private bool _inited; // 初始化完成标志
    private bool _bakeStarted; // 开始烘培标志
    private TextAsset _haltonSequenceData; // 随机数序列数据
    private HaltonSequenceData _haltonSequence; // 随机数序列
    private MapBakeConfig _config; // 烘培配置

    private EBakeStage _bakeStage; // 当前阶段
    private EditorCoroutine _mainRoutine; // 主协程

    private int _bakeLinkArea;
    private int _bakeTileOneArea;
    private int _bakeTileTwoArea;
    private int _bakeLinkMask; // mask 两块tile之间的区域
    private int _bakeTileOneMask; // mask tile A
    private int _bakeTileTwoMask; // mask tile B
    private NavMeshSurface _bakeLinkSurface;
    private NavMeshSurface _bakeTileOneSurface;
    private NavMeshSurface _bakeTileTwoSurface;

    private Bounds _worldBound; // 整个地图范围包围盒
    private float _sectorOffsetX; // 该区块的x轴起始位置相对于0的偏移量
    private float _sectorOffsetZ;// 该区块的z轴起始位置相对于0的偏移量
    private int _sectorCountX;
    private int _sectorCountZ;
    private Vector2Int _currentSector;
    private List<Vector2Int> _sectorList = new List<Vector2Int>();
    private List<Vector2Int> _bakedSector = new List<Vector2Int>();
    private int _tileCountX; // X方向tile数量
    private int _tileCountZ; // Z方向tile数量
    private int _tileCountTotal; // 总数量
    private UndirectedGraph<TileIdentifier> _tileGraph; // tileID的图
    private List<GraphNode<TileIdentifier>> _tileIDNodeList; // tile的图节点
    private bool[,] _tileBakedArray;
    private Dictionary<TileIdentifier, List<Triangle>> _tileTriangleDic;
    private Dictionary<TileIdentifier, Bounds> _tileBoundsDic;

    // 取tile的右侧与上侧
    private Dictionary<TileIdentifier, TileConnection[]> _tileConnectionDic;
    private List<NavMeshBuildSource> _navmeshBuildSourceList;
    private List<NavMeshBuildMarkup> _navmeshBuildMarkupList;

    private int _createdGraphCount;
    public NavigationGraph navigationGraph;
    public NavigationMapInfo mapInfo;

    private float _maxAngleCosine;

    private void Initialize()
    {
        _config = Resources.Load<MapBakeConfig>("MapBakeConfig");
        _haltonSequence = (HaltonSequenceData)Utility.DeserializeBinaryData("HaltonSequence");
        _bakeLinkSurface = new GameObject("BakeLinkSurface").AddComponent<NavMeshSurface>();
        _bakeTileOneSurface = new GameObject("BakeTileOneSurface").AddComponent<NavMeshSurface>();
        _bakeTileTwoSurface = new GameObject("BakeTileTwoSurface").AddComponent<NavMeshSurface>();
        _bakeLinkSurface.RemoveData();
        _bakeTileOneSurface.RemoveData();
        _bakeTileTwoSurface.RemoveData();

        _bakeLinkArea = NavMesh.GetAreaFromName("BakeLink");
        _bakeTileOneArea = NavMesh.GetAreaFromName("BakeTileOne");
        _bakeTileTwoArea = NavMesh.GetAreaFromName("BakeTileTwo");

        _bakeLinkMask = 1 << _bakeLinkArea;
        _bakeTileOneMask = 1 << _bakeTileOneArea;
        _bakeTileTwoMask = 1 << _bakeTileTwoArea;

        _bakeLinkSurface.defaultArea = _bakeLinkArea;
        _bakeTileOneSurface.defaultArea = _bakeTileOneArea;
        _bakeTileTwoSurface.defaultArea = _bakeTileTwoArea;

        _bakeLinkSurface.collectObjects = CollectObjects.Volume;
        _bakeLinkSurface.layerMask = _config.collectLayers;

        _bakeTileOneSurface.collectObjects = CollectObjects.Volume;
        _bakeTileOneSurface.layerMask = _config.collectLayers;

        _bakeTileTwoSurface.collectObjects = CollectObjects.Volume;
        _bakeTileTwoSurface.layerMask = _config.collectLayers;

        _worldBound = new Bounds(new Vector3(0.5f * _config.mapSize.x, VERTICAL_POS_OFFSET_RATIO * _config.mapSize.y, 0.5f * _config.mapSize.z), _config.mapSize);
        _maxAngleCosine = Mathf.Cos(_config.maxAngleAllowed);
        _navmeshBuildSourceList = new List<NavMeshBuildSource>(1000);
        _navmeshBuildMarkupList = new List<NavMeshBuildMarkup>(0);

        _sectorCountX = Mathf.CeilToInt(_config.mapSize.x / _config.sectorSize.x);
        _sectorCountZ = Mathf.CeilToInt(_config.mapSize.z / _config.sectorSize.z);

        _tileCountX = Mathf.CeilToInt(_config.mapSize.x / _config.tileSize.x);
        _tileCountZ = Mathf.CeilToInt(_config.mapSize.z / _config.tileSize.z);
        _tileCountTotal = _tileCountX * _tileCountZ;

        _sectorList = new List<Vector2Int>();
        if (_config.bakeAllSector)
        {
            for (int x = 0; x < _sectorCountX; x++)
            {
                for (int z = 0; z < _sectorCountZ; z++)
                {
                    _sectorList.Add(new Vector2Int(_sectorCountX - x - 1, _sectorCountZ - z - 1));
                }
            }
        }
        else
        {
            _sectorList = _config.sectorIndexList.
                Where(index => { return index.x >= 0 && index.x < _sectorCountX && index.y >= 0 && index.y < _sectorCountZ; })
                .ToList();
        }
    }

    private void EditorUpdateCallback()
    {
    }

    private void BakeMap()
    {
        _mainRoutine = EditorCoroutineUtility.StartCoroutine(MainRoutine(), this);
    }

    // 整个烘焙流程
    private IEnumerator MainRoutine()
    {
        for (int i = _sectorList.Count-1; i >=0 ;i--)
        {
            Vector2Int sector = _sectorList[i];
            InitSectorParams(sector);
            yield return null;
            //_bakeTileOneSurface.BuildNavMesh();
            yield return Stage_InitSerachGraph();
            _bakeStage = EBakeStage.CollectTriangle;
            yield return Stage_CollectTriangle();
            _bakeStage = EBakeStage.SamplePoint;
            yield return Stage_SamplePoint();
            _bakeStage = EBakeStage.ConstructGraph;
            yield return Stage_ConstructPathNode();
            _guiProgressMsg = $">>>>>> Sector {sector} baking complete";
            _bakedSector.Add(sector);
        }
        _guiProgressMsg = ">>> Map baking complete <<<";
    }

    private void InitSectorParams(Vector2Int sector)
    {
        _sectorOffsetX = sector.x * _config.sectorSize.x;
        _sectorOffsetZ = sector.x * _config.sectorSize.z;
        _tileIDNodeList = new List<GraphNode<TileIdentifier>>(_tileCountTotal);
        _tileTriangleDic = new Dictionary<TileIdentifier, List<Triangle>>(_tileCountTotal);
        _tileBoundsDic = new Dictionary<TileIdentifier, Bounds>(_tileCountTotal);
        _tileConnectionDic = new Dictionary<TileIdentifier, TileConnection[]>(_tileCountTotal);
    }

    // 初始化图 
    private IEnumerator Stage_InitSerachGraph()
    {
        GUIClearProgress();

        for (int x = 0; x < _tileCountX; x++)
        {
            for (int z = 0; z < _tileCountZ; z++)
            {
                TileIdentifier tileID = new TileIdentifier(x, z);
                _tileIDNodeList.Add(new GraphNode<TileIdentifier>(tileID));
            }
        }
        _tileGraph = new UndirectedGraph<TileIdentifier>();
        string guimsg = "Create tile graph...";
        ResetThreadParams(_tileCountTotal);
        yield return AssignThreadTasksRoutine(GetTileIdentifierForThread, (obj) => true, ConcurrentCreateGraph, guimsg);

        // 多线程中，分配了task,但有一部分还是完全不执行，在这里重新初始化一次
        for (int x = 0; x < _tileCountX; x++)
        {
            for (int z = 0; z < _tileCountZ; z++)
            {
                TileIdentifier id = new TileIdentifier(x, z);
                if (!_tileBoundsDic.ContainsKey(id))
                {
                    AddNodePair(id);
                }
            }
        }
    }

    private void ConcurrentCreateGraph(object obj)
    {
        TileIdentifier tileID = (TileIdentifier)obj;
        AddNodePair(tileID);
        GUISetTileSucceed(tileID);
        lock (_locker)
        {
            _threadProcessedCount++;
            _threadAvailableCount++;
        }
    }

    private void AddNodePair(TileIdentifier tileID)
    {
        _tileBoundsDic[tileID] = new Bounds(GetTileCenterPosition(tileID), _config.tileSize);
        _tileTriangleDic[tileID] = new List<Triangle>(100);
        int x = tileID.coordX;
        int z = tileID.coordZ;
        bool rightNeibourExist = x + 1 < _tileCountX;
        bool leftNeibourExist = x - 1 > 0;
        bool upNeibourExist = z + 1 < _tileCountZ;
        bool bottomNeibourExist = z - 1 > 0;
        int index = x * _tileCountZ + z;
        if (leftNeibourExist)
        {
            _tileGraph.AddPair(_tileIDNodeList[index], _tileIDNodeList[(x - 1) * _tileCountZ + z]);
        }

        if (rightNeibourExist)
        {
            _tileGraph.AddPair(_tileIDNodeList[index], _tileIDNodeList[(x + 1) * _tileCountZ + z]);
        }

        if (upNeibourExist)
        {
            _tileGraph.AddPair(_tileIDNodeList[index], _tileIDNodeList[x * _tileCountZ + z + 1]);
        }

        if (bottomNeibourExist)
        {
            _tileGraph.AddPair(_tileIDNodeList[index], _tileIDNodeList[x * _tileCountZ + z - 1]);
        }
    }

    // 收集三角形 
    private IEnumerator Stage_CollectTriangle()
    {
        Debug.Log(">>> Stage_CollectTriangle <<< Start");

        GUIClearProgress();
        yield return _tileGraph.IterateBFSCoroutine(_tileIDNodeList[0], Step_CollectTileConnectionTriangles, 64);
        Debug.Log("      Step_CollectTileConnectionTriangles Done");

        GUIClearProgress();
        yield return Step_RemoveIncorrectTriangleInConnection();

        Debug.Log(">>> Stage_CollectTriangle <<< Done");
    }

    // 采样穿越点
    private IEnumerator Stage_SamplePoint()
    {
        Debug.Log(">>> Stage_SamplePoint <<< Start");

        GUIClearProgress();
        yield return Step_PickRandomPointInTriangle();
        Debug.Log("      PickRandomPointInTriangle Done");

        GUIClearProgress();
        yield return Step_DividePointsInDifferentRegion();
        Debug.Log("      DividePointsInDifferentRegion Done");

        GUIClearProgress();
        yield return Step_CheckRegionConnectivity();
        Debug.Log("      CheckRegionConnectivity Done");

        GUIClearProgress();
        yield return Step_CreateConvexHull();
        Debug.Log("      CreateConvexHull Done!");

        Debug.Log(">>> Stage_SamplePoint <<< Done");
    }

    private IEnumerator Stage_ConstructPathNode()
    {
        Debug.Log(">>> Stage_ConstructPathNode <<< Start");
        GUIClearProgress();
        yield return Step_ConstructGraph();

        Debug.Log(">>> Stage_ConstructPathNode <<< Done");
    }

    #endregion

    #region ------------------------------------------------ Collect Triangles ----------------------------------------------------------

    // Collect Triangle : Step 1
    private void Step_CollectTileConnectionTriangles(GraphNode<TileIdentifier> node)
    {
        TileIdentifier tileID = node.value;
        if (tileID.coordX == _tileCountX - 1 && tileID.coordZ == _tileCountZ - 1)
        {
            return;
        }
        TileConnection[] tileConnections;
        if (tileID.coordZ == _tileCountZ - 1 || tileID.coordX == _tileCountX - 1)
        {
            tileConnections = new TileConnection[1];
        }
        else
        {
            tileConnections = new TileConnection[2];
        }

        int index = 0;
        for (int i = 0; i < node.neiboursCount; i++)
        {
            TileConnection t = CreateTileConnection(node.value, node.neibours[i].value);
            if (t == null)
            {
                continue;
            }
            tileConnections[index] = t;
            index++;
        }

        _tileConnectionDic.Add(tileID, tileConnections);
        GUISetTileSucceed(tileID.coordX, tileID.coordZ);
        GetTrianglesInTileConnection(tileID);

        //------Debug info-------------
        GUIUpdateProgress((float)_tileConnectionDic.Count / (_tileCountTotal - 1), "[Collect Triangle] Get triangle from connection range.");
    }

    // Collect Triangle : Step 2
    private IEnumerator Step_RemoveIncorrectTriangleInConnection()
    {
        string guiMsg = "[Collect Triangle] Remove useless triangles in connection";
        ResetThreadParams(_tileCountTotal - 1);
        yield return AssignThreadTasksRoutine(
            GetTileIdentifierForThread,
            (obj) =>
            {
                TileIdentifier id = (TileIdentifier)obj;
                if (id.coordX != _tileCountX - 1 || id.coordZ != _tileCountZ - 1)
                {
                    return true;
                }
                return false;
            },
            ConcurrentRemoveTriangleInConnection,
            guiMsg);
    }

    // 获取线程执行所需要的参数
    private object GetTileIdentifierForThread(int currentIndex)
    {
        return _tileIDNodeList[currentIndex].value;
    }

    private TileConnection CreateTileConnection(TileIdentifier tile, TileIdentifier neibour)
    {
        if (tile.coordX == neibour.coordX && tile.coordZ == neibour.coordZ)
        {
            Debug.LogError("同一个Tile");
            return null;
        }
        // tileB相对于A的方向
        ETileRelativeDir relativeDir = TileRelativeDirBtoA(tile, neibour);
        if (relativeDir != ETileRelativeDir.Right && relativeDir != ETileRelativeDir.Up)
        {
            //Debug.Log($"{neibour} -> {tile} 相对方向错误，只按右上方向 {relativeDir}");
            return null;
        }
        TileConnection tileConnection = new TileConnection(tile, neibour);
        Vector3 tileOnePos = GetTileCenterPosition(tile);
        Vector3 tileTwoPos = GetTileCenterPosition(neibour);
        Vector3 centerPos = (tileOnePos + tileTwoPos) * 0.5f;
        centerPos.y = tileOnePos.y;
        Vector3 boundSize = Vector3.zero;
        if (relativeDir == ETileRelativeDir.Right)
        {
            boundSize = new Vector3(_config.linkBakeWidth, _config.tileSize.y, _config.tileSize.z);
        }
        else
        {
            boundSize = new Vector3(_config.tileSize.x, _config.tileSize.y, _config.linkBakeWidth);
        }
        tileConnection.bound = new Bounds(centerPos, boundSize);
        return tileConnection;
    }

    private void GetTrianglesInTileConnection(TileIdentifier id)
    {
        TileConnection[] tileConnections = _tileConnectionDic[id];
        for (int i = 0; i < tileConnections.Length; i++)
        {
            GetTrianglesInBound(tileConnections[i].bound, tileConnections[i].triangleList);
        }
    }

    private void GetTrianglesInBound(Bounds bound, List<Triangle> resultList)
    {
        // collect mesh
        NavMeshBuilder.CollectSourcesInStage(bound, _config.collectLayers, NavMeshCollectGeometry.RenderMeshes,
            _bakeTileOneArea, _navmeshBuildMarkupList, _bakeTileOneSurface.gameObject.scene, _navmeshBuildSourceList);
        // collect triangles
        for (int i = 0; i < _navmeshBuildSourceList.Count; i++)
        {
            NavMeshBuildSource source = _navmeshBuildSourceList[i];
            //Debug.Log($"{tileID} - {i} :{source.shape} {source.size} {source.sourceObject.name} {source.sourceObject.GetType()}");
            switch (source.shape)
            {
                case NavMeshBuildSourceShape.Mesh:
                    GetAllTriangleFromMesh(bound, source, resultList);
                    break;
            }
        }
    }

    // 从Mesh中取三角形
    private void GetAllTriangleFromMesh(Bounds tileBound, NavMeshBuildSource source, List<Triangle> triangleList)
    {
        Mesh mesh = source.sourceObject as Mesh;
        if (mesh == null)
        {
            return;
        }

        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = source.transform.MultiplyPoint3x4(vertices[i]);
        }

        Bounds meshBound = new Bounds(source.transform.MultiplyPoint3x4(mesh.bounds.center), mesh.bounds.size);

        if (tileBound.Intersects(meshBound) == false)
        {
            return;
        }

        if (tileBound.Contains(meshBound.min) && tileBound.Contains(meshBound.max))
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
            triangleList.Add(new Triangle(p1, p2, p3));
        }
    }

    private void ConcurrentRemoveTriangleInConnection(object tileIdentifier)
    {
        TileIdentifier tileID = (TileIdentifier)tileIdentifier;
        TileConnection[] tileConnections = _tileConnectionDic[tileID];

        for (int i = 0; i < tileConnections.Length; i++)
        {
            Bounds bound = tileConnections[i].bound;
            int totalCount = tileConnections[i].triangleList.Count;

            // Object triangles
            for (int triangleIndex = totalCount - 1; triangleIndex >= 0; triangleIndex--)
            {
                if (!TriangleBoundsIntersect(bound, tileConnections[i].triangleList[triangleIndex]))
                {
                    tileConnections[i].triangleList.RemoveAt(triangleIndex);
                }
            }
        }
        GUISetTileSucceed(tileID.coordX, tileID.coordZ);
        lock (_locker)
        {
            _threadProcessedCount++;
            _threadAvailableCount++;
        }
    }

    // 三角形与Bound相交测试
    private bool TriangleBoundsIntersect(Bounds bound, Triangle triangle)
    {
        Vector3 boundHalfSize = bound.size * 0.5f;
        Vector3 v0 = triangle.vertA - bound.center;
        Vector3 v1 = triangle.vertB - bound.center;
        Vector3 v2 = triangle.vertC - bound.center;
        Vector3 f0 = v1 - v0;
        Vector3 f1 = v2 - v1;
        Vector3 f2 = v0 - v2;
        Vector3 u0 = new Vector3(1.0f, 0f, 0f);
        Vector3 u1 = new Vector3(0, 1.0f, 0);
        Vector3 u2 = new Vector3(0, 0, 1.0f);
        Vector3[] axiArray = new Vector3[]
        {
                Vector3.Cross(u0, f0),
                Vector3.Cross(u0, f1),
                Vector3.Cross(u0, f2),
                Vector3.Cross(u1, f0),
                Vector3.Cross(u1, f1),
                Vector3.Cross(u1, f2),
                Vector3.Cross(u2, f0),
                Vector3.Cross(u2, f1),
                Vector3.Cross(u2, f2),
                u0,
                u1,
                u2,
                Vector3.Cross(f0,f1)
        };
        for (int i = 0; i < axiArray.Length; i++)
        {
            if (IsSeparateAxi(axiArray[i], boundHalfSize, v0, v1, v2))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSeparateAxi(Vector3 axi, Vector3 aabbExtent, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        float p0 = Vector3.Dot(v0, axi);
        float p1 = Vector3.Dot(v1, axi);
        float p2 = Vector3.Dot(v2, axi);
        float r = aabbExtent.x * Mathf.Abs(Vector3.Dot(Vector3.right, axi)) +
                  aabbExtent.y * Mathf.Abs(Vector3.Dot(Vector3.up, axi)) +
                  aabbExtent.z * Mathf.Abs(Vector3.Dot(Vector3.forward, axi));

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

    #region ------------------------------------------- Sample Point----------------------------------------------------------------

    private Vector3 _voxelSizeInv = new Vector3(2f, 0.5f, 2f); // voxel size 0.25,2,0.25
    // Sample Point : Step 1
    private IEnumerator Step_PickRandomPointInTriangle()
    {
        string guiMsg = "[Sample Point]  Get Random Points";
        ResetThreadParams(_tileCountTotal);
        yield return AssignThreadTasksRoutine(
                GetTileIdentifierForThread,
                (obj) => { return true; },
                ConcurrentRandomPoint,
                guiMsg
            );
    }

    private void ConcurrentRandomPoint(object obj)
    {
        TileIdentifier tileID = (TileIdentifier)obj;
        if (_tileConnectionDic.ContainsKey(tileID))
        {
            TileConnection[] tileConnections = _tileConnectionDic[tileID];
            for (int i = 0; i < tileConnections.Length; i++)
            {
                Bounds bound = tileConnections[i].bound;
                for (int triangleIndex = 0; triangleIndex < tileConnections[i].triangleList.Count; triangleIndex++)
                {
                    Triangle triangle = tileConnections[i].triangleList[triangleIndex];
                    if (Vector3.Dot(triangle.normal, Vector3.up) < _maxAngleCosine)
                    {
                        continue;
                    }
                    float area = triangle.GetTriangleArea();
                    int sampleCount = Mathf.CeilToInt(area / 2f);
                    for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                    {
                        Vector2 randomValue = _haltonSequence.GetNext();
                        float u = randomValue.x;
                        float v = randomValue.y;
                        float w = 1 - (u + v);
                        Vector3 pos = u * triangle.vertA + v * triangle.vertB + w * triangle.vertC;
                        if (triangle.isTerrainTriangle)
                        {
                            pos.y = _terrianHeightSampler.GetTerrainHeight(pos);
                        }
                        if (bound.Contains(pos))
                        {
                            tileConnections[i].randomPointList.Add(pos);
                        }
                    }
                }
            }

            GUISetTileSucceed(tileID.coordX, tileID.coordZ);
        }

        lock (_locker)
        {
            _threadProcessedCount++;
            _threadAvailableCount++;
        }
    }

    // Sample Point : Step 2
    private IEnumerator Step_DividePointsInDifferentRegion()
    {
        string guiMsg = "[Sample Point] Divide points to different region";
        ResetThreadParams(_tileCountTotal - 1);
        yield return AssignThreadTasksRoutine(GetTileIdentifierForThread,
            (obj) => { return true; }, ConcurrentDownsamplingPoints, guiMsg);
    }

    private List<Vector3> debugPoints = new List<Vector3>(1000);

    private void ConcurrentDownsamplingPoints(object obj)
    {
        TileIdentifier tileID = (TileIdentifier)obj;
        if (_tileConnectionDic.ContainsKey(tileID) == false)
        {
            lock (_locker)
            {
                _threadAvailableCount++;
            }
            return;
        }
        Debug.Assert(_tileConnectionDic.ContainsKey(tileID), $"Not Conatin {tileID}");
        TileConnection[] connections = _tileConnectionDic[tileID];
        for (int i = 0; i < connections.Length; i++)
        {
            TileConnection tileConnection = connections[i];
            Vector3 boundMin = tileConnection.bound.min;
            Vector3 boundSize = tileConnection.bound.size;
            int countX = Mathf.CeilToInt(boundSize.x * _voxelSizeInv.x);
            int countY = Mathf.CeilToInt(boundSize.y * _voxelSizeInv.y);
            int countZ = Mathf.CeilToInt(boundSize.z * _voxelSizeInv.z);
            DownsamplingVoxel[,,] voxelArray = new DownsamplingVoxel[countX, countY, countZ];
            // 取每个Voxel内的点
            for (int pointIndex = 0; pointIndex < tileConnection.randomPointList.Count; pointIndex++)
            {
                Vector3 pos = tileConnection.randomPointList[pointIndex];
                Vector3 relativePos = pos - boundMin;
                int indexX = (int)(relativePos.x * _voxelSizeInv.x);
                int indexY = (int)(relativePos.y * _voxelSizeInv.y);
                int indexZ = (int)(relativePos.z * _voxelSizeInv.z);
                if (indexX >= 0 && indexX < countX && indexY >= 0 && indexY < countY && indexZ >= 0 && indexZ < countZ)
                {
                    if (voxelArray[indexX, indexY, indexZ] == null)
                    {
                        voxelArray[indexX, indexY, indexZ] = new DownsamplingVoxel
                        {
                            x = indexX,
                            y = indexY,
                            z = indexZ,
                            isEmptyVoxel = false,
                            averagePos = pos
                        };
                    }
                    else
                    {
                        voxelArray[indexX, indexY, indexZ].SetAvgPosition(pos);
                    }
                }
                //debugPoints.Add(pos);
            }
            // 划分连通区域
            int regionID = 0;
            Queue<DownsamplingVoxel> queue = new Queue<DownsamplingVoxel>(20);
            foreach (DownsamplingVoxel voxel in voxelArray)
            {
                if (voxel == null)
                {
                    continue;
                }
                if (!voxel.isEmptyVoxel && !voxel.hasAssignedRegion)
                {
                    BakedRegion region = new BakedRegion();
                    region.regionID = regionID;
                    queue.Enqueue(voxel);
                    while (queue.Count != 0)
                    {
                        DownsamplingVoxel currentVoxel = queue.Dequeue();
                        currentVoxel.hasAssignedRegion = true;
                        // 先将点存在第一个List里，下一步里再分区
                        region.downsamplePoints.Add(currentVoxel.averagePos);
                        QueryFourNeibours2D(currentVoxel, voxelArray, queue);
                    }
                    tileConnection.regionList.Add(region);
                    regionID++;
                }
            }
        }
        lock (_locker)
        {
            _threadProcessedCount++;
            _threadAvailableCount++;
        }
        GUISetTileSucceed(tileID);
    }

    private void QueryFourNeibours2D(DownsamplingVoxel voxel, DownsamplingVoxel[,,] array, Queue<DownsamplingVoxel> queue)
    {
        int x = voxel.x;
        int y = voxel.y;
        int z = voxel.z;
        DownsamplingVoxel currentVoxel;
        if (x - 1 >= 0)
        {
            currentVoxel = array[x - 1, y, z];
            if (currentVoxel != null && !currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }

        if (x + 1 < array.GetLength(0))
        {
            currentVoxel = array[x + 1, y, z];
            if (currentVoxel != null && !currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }

        if (z - 1 >= 0)
        {
            currentVoxel = array[x, y, z - 1];
            if (currentVoxel != null && !currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }

        if (z + 1 < array.GetLength(2))
        {
            currentVoxel = array[x, y, z + 1];
            if (currentVoxel != null && !currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }
    }

    private NavMeshPath _navMeshPath;
    // Sample Point : Step 3
    private IEnumerator Step_CheckRegionConnectivity()
    {
        _navMeshPath = new NavMeshPath();
        int currentCount = 0;
        int lastCount = 0;
        foreach (KeyValuePair<TileIdentifier, TileConnection[]> pair in _tileConnectionDic)
        {
            for (int tileConnectionIndex = 0; tileConnectionIndex < pair.Value.Length; tileConnectionIndex++)
            {
                TileConnection tileConnection = pair.Value[tileConnectionIndex];
                TileIdentifier[] connectedTiles = tileConnection.connectedTiles;
                BakeNavmesh(_bakeLinkSurface, tileConnection.bound.center, tileConnection.bound.size);
                Bounds tileOneBound = _tileBoundsDic[connectedTiles[0]];
                Bounds tileTwoBound = _tileBoundsDic[connectedTiles[1]];
                // Remove not in navmesh points
                for (int i = 0; i < tileConnection.regionList.Count; i++)
                {
                    for (int j = tileConnection.regionList[i].downsamplePoints.Count - 1; j >= 0; j--)
                    {
                        Vector3 pos = tileConnection.regionList[i].downsamplePoints[j];
                        if (!NavMesh.SamplePosition(pos, out NavMeshHit hit, NAVMESH_SAMPLE_RADIUS, _bakeLinkMask))
                        {
                            tileConnection.regionList[i].downsamplePoints.RemoveAt(j);
                        }
                        else
                        {
                            // 如果该位置可以采样到navmesh，则存储navmesh采样点位置
                            tileConnection.regionList[i].downsamplePoints[j] = hit.position;
                        }
                    }
                }
                // Combine Connective Region
                for (int i = 0; i < tileConnection.regionList.Count; i++)
                {
                    for (int j = i + 1; j < tileConnection.regionList.Count; j++)
                    {
                        if (CanCombineRegion(tileConnection.regionList[i], tileConnection.regionList[j]))
                        {
                            tileConnection.regionList[i].downsamplePoints.AddRange(tileConnection.regionList[j].downsamplePoints);
                            tileConnection.regionList[j].downsamplePoints = null;
                        }
                    }
                }

                for (int i = tileConnection.regionList.Count - 1; i >= 0; i--)
                {
                    if (tileConnection.regionList[i].downsamplePoints == null ||
                        tileConnection.regionList[i].downsamplePoints.Count == 0)
                    {
                        tileConnection.regionList.RemoveAt(i);
                    }
                    //else if (tileConnection.regionList[i].downsamplePoints.Count == 0)
                    //{
                    //    tileConnection.regionList.RemoveAt(i);
                    //}
                }

                // Split region to subregions inside different tile.
                foreach (BakedRegion region in tileConnection.regionList)
                {
                    //if (region.downsamplePoints == null)
                    //{
                    //    continue;
                    //}
                    if (region.downsamplePoints.Count <= 1)
                    {
                        continue;
                    }
                    // 将点划分到各自属于的区块
                    foreach (Vector3 point in region.downsamplePoints)
                    {
                        if (NavMesh.SamplePosition(point, out NavMeshHit hitInfo, NAVMESH_SAMPLE_RADIUS, _bakeLinkMask))
                        {
                            if (tileOneBound.Contains(hitInfo.position))
                            {
                                region.subregionAPoints.Add(new BakedPoint
                                {
                                    ownerTile = connectedTiles[0],
                                    pos = hitInfo.position,
                                    regionID = region.regionID,
                                    subregionID = 0
                                });
                            }
                            else if (tileTwoBound.Contains(hitInfo.position))
                            {
                                region.subregionBPoints.Add(new BakedPoint
                                {
                                    ownerTile = connectedTiles[1],
                                    pos = hitInfo.position,
                                    regionID = region.regionID,
                                    subregionID = 1
                                });
                            }
                        }
                    }
                    //Debug.Log($"Region{region.regionID} Sub A {region.subregionAPoints.Count} Sub B {region.subregionBPoints.Count} ");
                }
            }
            GUISetTileSucceed(pair.Key); 
            currentCount++;
            if (currentCount - lastCount > 32)
            {
                lastCount = currentCount;
                GUIUpdateProgress((float)currentCount / _tileCountTotal - 1, "[Sample Point] Check point connectivity");
                yield return null;
            }
        }
    }

    private bool CanCombineRegion(BakedRegion regionA, BakedRegion regionB)
    {
        if (regionA.downsamplePoints == null || regionB.downsamplePoints == null)
        {
            return false;
        }
        if (regionA.downsamplePoints.Count == 0 || regionB.downsamplePoints.Count == 0)
        {
            return false;
        }

        NavMesh.CalculatePath(regionA.downsamplePoints[0], regionB.downsamplePoints[0], _bakeLinkMask, _navMeshPath);
        if (_navMeshPath.status == NavMeshPathStatus.PathComplete)
        {
            return true;
        }

        return false;
    }

    private void BakeNavmesh(NavMeshSurface surface, Vector3 pos, Vector3 size)
    {
        surface.center = pos;
        surface.size = size;
        surface.BuildNavMesh();
    }

    // Sample Point : Step 4
    private IEnumerator Step_CreateConvexHull()
    {
        ResetThreadParams(_tileCountTotal - 1);
        string guiMsg = "[Sample Point] Create convex hull";
        yield return AssignThreadTasksRoutine(GetTileIdentifierForThread, (obj) => { return true; }, ConcurrentCreateConvexHull, guiMsg);
    }
    private void ConcurrentCreateConvexHull(object obj)
    {
        TileIdentifier tileID = (TileIdentifier)obj;
        if (_tileConnectionDic.ContainsKey(tileID) == false)
        {
            lock (_locker)
            {
                _threadAvailableCount++;
            }
            return;
        }

        TileConnection[] tileConnection = _tileConnectionDic[tileID];

        for (int i = 0; i < tileConnection.Length; i++)
        {
            foreach (BakedRegion region in tileConnection[i].regionList)
            {
                if (region != null)
                {
                    region.CreateConvexRegion();
                }
            }
        }

        lock (_locker)
        {
            _threadProcessedCount++;
            _threadAvailableCount++;
        }
        GUISetTileSucceed(tileID);

    }
    #endregion

    #region -----------------------------------------------Create Node Graph-------------------------------------------------------------------------

    private IEnumerator Step_ConstructGraph()
    {
        navigationGraph = new NavigationGraph();
        yield return _tileGraph.IterateBFSCoroutine(_tileIDNodeList[0], ConnectGraphNodes, 32);
        mapInfo = new NavigationMapInfo(_config.mapSize, _config.tileSize);
        mapInfo.navigationGraph = navigationGraph;
        string path = $"Assets/Tutorial/Pathfinding/Resources/{_config.mapInfoFileSaveName}.bytes";
        Utility.SerializeBinaryData<NavigationMapInfo>(mapInfo, path);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("地图烘焙", $"地图烘焙完成，保存在{path}", "Ok");
    }

    private List<BakedPoint> _tileOwnerPointCacheList = new List<BakedPoint>(10);
    private void ConnectGraphNodes(GraphNode<TileIdentifier> idNode)
    {
        _tileOwnerPointCacheList.Clear();
        TileIdentifier tileID = idNode.value;

        bool connectLeft = false;
        bool connectBottom = false;
        if (tileID.coordX == 0 && tileID.coordZ == 0)
        {

        }
        else if (tileID.coordX == 0)
        {
            connectBottom = true;
        }
        else if (tileID.coordZ == 0)
        {
            connectLeft = true;
        }
        else
        {
            connectLeft = true;
            connectBottom = true;
        }

        // 烘焙Navmesh时比实际区块宽一点点，便于处理穿越点恰好处于边界的情况
        BakeNavmesh(_bakeTileOneSurface, _tileBoundsDic[tileID].center, _tileBoundsDic[tileID].size + new Vector3(0.5f, 0, 0.5f));

        // 连接跨区域的点 
        TileConnection[] tileConnections = GetTileConnections(tileID);
        if (tileConnections != null)
        {
            foreach (TileConnection tileConnect in tileConnections)
            {
                foreach (BakedRegion tRegion in tileConnect.regionList)
                {
                    if (tRegion.bakedPointA != null && tRegion.bakedPointB != null)
                    {
                        float dist = Vector3.Distance(tRegion.bakedPointA.pos, tRegion.bakedPointB.pos);
                        navigationGraph.AddPair(tRegion.bakedPointA.ToGraphNode(), tRegion.bakedPointB.ToGraphNode(), dist);
                    }
                }

                // 将连接处中属于当前处理区块的点存下来
                tileConnect.GetBakedPointInTile(tileID, _tileOwnerPointCacheList);
            }
        }

        // 查找左侧与下侧属于该区块的连接点
        if (connectBottom)
        {
            TileIdentifier bottomNeibourID = new TileIdentifier(tileID.coordX, tileID.coordZ - 1);
            tileConnections = GetTileConnections(bottomNeibourID);
            foreach (TileConnection tConnection in tileConnections)
            {
                //将连接处中属于当前处理区块的点存下来
                tConnection.GetBakedPointInTile(tileID, _tileOwnerPointCacheList);
            }
        }

        if (connectLeft)
        {
            TileIdentifier leftNeibourID = new TileIdentifier(tileID.coordX - 1, tileID.coordZ);
            tileConnections = GetTileConnections(leftNeibourID);
            foreach (TileConnection tConnection in tileConnections)
            {
                //将连接处中属于当前处理区块的点存下来
                tConnection.GetBakedPointInTile(tileID, _tileOwnerPointCacheList);
            }
        }

        // 在图中连接可通行的点
        for (int i = 0; i < _tileOwnerPointCacheList.Count; i++)
        {
            BakedPoint pointA = _tileOwnerPointCacheList[i];
            for (int j = i + 1; j < _tileOwnerPointCacheList.Count; j++)
            {
                BakedPoint pointB = _tileOwnerPointCacheList[j];
                NavMesh.CalculatePath(pointA.position, pointB.position, _bakeTileOneMask, _navMeshPath);

                if (_navMeshPath.status == NavMeshPathStatus.PathComplete)
                {
                    float dist = Vector3.Distance(pointA.pos, pointB.pos);
                    navigationGraph.AddPair(pointA.ToGraphNode(), pointB.ToGraphNode(), dist);
                }
            }
            navigationGraph.AddNodeToTile(tileID, _tileOwnerPointCacheList[i].ToGraphNode());
        }
        _createdGraphCount++;
        GUISetTileSucceed(tileID);
        GUIUpdateProgress((float)_createdGraphCount / (_tileCountTotal - 1), "Construct Graph ...");
    }
    #endregion
}