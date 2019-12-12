using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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
    SamplePoint
}

enum ETileRelativeDir
{
    NotNeibour,
    Left,
    Right,
    Up,
    Bottom
}

class TileConnection
{
    public TileIdentifier[] connectedTiles; // 相邻的区块
    public Bounds bound; // 范围
    public List<Triangle> triangleList = new List<Triangle>(128);
    public List<Vector3> randomPointList = new List<Vector3>(512);
    public List<BakedRegion> regionList = new List<BakedRegion>(16);
    public TileConnection(TileIdentifier tileOne, TileIdentifier tileTwo)
    {
        connectedTiles = new[] { tileOne, tileTwo };
    }
}

class BakedPoint : IConvexPoint
{
    public TileIdentifier ownerTile;
    public Vector3 pos;
    public int regionID;
    public int subregionID;

    public Vector3 position
    {
        get { return pos; }
    }
}

class BakedRegion
{
    public int regionID;
    public List<Vector3> downsamplePoints  = new List<Vector3>(256);
    public List<BakedPoint> subregionAPoints = new List<BakedPoint>(128);
    public List<BakedPoint> subregionBPoints = new List<BakedPoint>(128);
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
        Vector3 center = new Vector3((tileID.coordX + 0.5f) * _config.tileSize.x, VERTICAL_POS_OFFSET_RATIO * _config.tileSize.y, (tileID.coordZ + 0.5f) * _config.tileSize.z);
        center.y += Utility.GetTerrainHeight(center);
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

    #endregion

    [MenuItem("Tools/Bake Map")]
    static void RenderCubemap()
    {
        _s_windowInstance = EditorWindow.GetWindow<LinkNodeBakeWindow>();
        _s_windowInstance.minSize = new Vector2(_s_windowWidth, _s_windowHeight);
        _s_windowInstance.maxSize = new Vector2(_s_windowWidth, _s_windowHeight);
        _s_windowInstance.ShowTab();
    }
    private static float _s_windowWidth = 768f;
    private static float _s_windowHeight = 768f;
    private static LinkNodeBakeWindow _s_windowInstance;
    private void Awake()
    {
        if (_inited == false)
        {
            Initialize();
            GUIInitialize();
            _inited = true;
        }
    }

    private void OnEnable()
    {
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
        GUILayout.Label($"Tile Count {_tileCountX} - {_tileCountZ}");
        if (GUILayout.Button("BakeMap") && !_bakeStarted)
        {
            _bakeStarted = true;
            BakeMap();
        }

        EditorGUI.ProgressBar(new Rect(Vector2.one * 60f, new Vector2(640f, 30f)), _guiProgress, _guiProgressMsg);
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
    private bool _sceneDrawWorldBound;
    private bool _drawTileBound;
    private bool _drawTileConnectionBound;

    private bool _drawTileTriangles;
    private bool _drawTileConnectionTriangles;

    private bool _drawRandomPoints;
    private Vector2Int _tileID;
    private void OnSceneGUI(SceneView sceneView)
    {
        // GUI
        Handles.BeginGUI();
        _sceneDrawWorldBound = GUILayout.Toggle(_sceneDrawWorldBound, "Draw World Bound");
        _drawTileBound = GUILayout.Toggle(_drawTileBound, "Draw Tile Bound");
        _drawTileConnectionBound = GUILayout.Toggle(_drawTileConnectionBound, "Draw Tile Connection Bound");
        GUILayout.Space(10);
        _drawTileTriangles = GUILayout.Toggle(_drawTileTriangles, "Draw Tile Triangles");
        _drawTileConnectionTriangles = GUILayout.Toggle(_drawTileConnectionTriangles, "Draw Tile Connection Triangles");
        GUILayout.Space(10);

        _drawRandomPoints = GUILayout.Toggle(_drawRandomPoints, "Draw Random Points");
        Handles.EndGUI();

        // Handles
        if (_sceneDrawWorldBound)
        {
            Handles.color = Color.blue;
            Handles.DrawWireCube(_worldBound.center, _worldBound.size);
        }

        if (_drawTileBound && _tileBoundsDic != null)
        {
            Handles.color = Color.green;
            foreach (KeyValuePair<TileIdentifier, Bounds> tPair in _tileBoundsDic)
            {
                // 只画包括了三角面的区块
                if (_tileTriangleDic[tPair.Key].Count > 0)
                {
                    Handles.DrawWireCube(tPair.Value.center, tPair.Value.size);
                }
            }
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

        if (_drawRandomPoints && _tileConnectionDic != null)
        {
            Handles.color = Color.cyan;
            foreach (KeyValuePair<TileIdentifier, TileConnection[]> tPair in _tileConnectionDic)
            {
                TileConnection[] connects = tPair.Value;
                for (int i = 0; i < connects.Length; i++)
                {
                }
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

    private const float VERTICAL_POS_OFFSET_RATIO = 0.45f;
    private const int THREAD_COUNT = 32;
    private bool _inited; // 初始化完成标志
    private bool _bakeStarted; // 开始烘培标志
    private TextAsset _haltonSequenceData; // 随机数序列数据
    private HaltonSequenceData _haltonSequence; // 随机数序列
    private MapBakeConfig _config; // 烘培配置

    private EBakeStage _bakeStage; // 当前阶段

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
    private int _collectedTiles; // 当前已完成的tile数
    private int _remainTiles; // 剩余tile数量
    private UndirectedGraph<TileIdentifier> _tileGraph; // tileID的图
    private List<GraphNode<TileIdentifier>> _tileIDNodeList; // tile的图节点
    private int _tileCountX; // X方向tile数量
    private int _tileCountZ; // Z方向tile数量
    private int _tileCountTotal; // 总数量

    private EditorCoroutine _mainRoutine; // 主协程
    private EditorWaitForSeconds _waitSomeTime;

    private float _maxAngleCosine;


    private void Initialize()
    {
        _haltonSequenceData = Resources.Load<TextAsset>("HaltonSequence");
        _config = Resources.Load<MapBakeConfig>("MapBakeConfig");
        if (_haltonSequenceData == null || _config == null)
        {
            _inited = false;
            return;
        }
        BinaryFormatter bf = new BinaryFormatter();
        _haltonSequence = bf.Deserialize(new MemoryStream(_haltonSequenceData.bytes)) as HaltonSequenceData;

        _bakeLinkSurface = new GameObject("BakeLinkSurface").AddComponent<NavMeshSurface>();
        _bakeTileOneSurface = new GameObject("BakeTileOneSurface").AddComponent<NavMeshSurface>();
        _bakeTileTwoSurface = new GameObject("BakeTileTwoSurface").AddComponent<NavMeshSurface>();

        _bakeLinkArea = 1 << NavMesh.GetAreaFromName("BakeLink");
        _bakeTileOneArea = 1 << NavMesh.GetAreaFromName("BakeTileOne");
        _bakeTileTwoArea = 1 << NavMesh.GetAreaFromName("BakeTileTwo");

        _bakeLinkSurface.defaultArea = _bakeLinkArea;
        _bakeTileOneSurface.defaultArea = _bakeTileOneArea;
        _bakeTileTwoSurface.defaultArea = _bakeTileTwoArea;

        _bakeLinkSurface.collectObjects = CollectObjects.Volume;
        _bakeLinkSurface.layerMask = _config.collectLayers;

        _bakeTileOneSurface.collectObjects = CollectObjects.Volume;
        _bakeTileOneSurface.layerMask = _config.collectLayers;

        _bakeTileTwoSurface.collectObjects = CollectObjects.Volume;
        _bakeTileTwoSurface.layerMask = _config.collectLayers;

        _tileCountX = Mathf.CeilToInt(_config.mapSize.x / _config.tileSize.x);
        _tileCountZ = Mathf.CeilToInt(_config.mapSize.z / _config.tileSize.z);
        _tileCountTotal = _tileCountX * _tileCountZ;
        _remainTiles = _tileCountTotal;
        _tileIDNodeList = new List<GraphNode<TileIdentifier>>(_remainTiles);

        _waitSomeTime = new EditorWaitForSeconds(0.001f);

        _worldBound = new Bounds(new Vector3(0.5f * _config.mapSize.x, VERTICAL_POS_OFFSET_RATIO * _config.mapSize.y, 0.5f * _config.mapSize.z), _config.mapSize);

        _maxAngleCosine = Mathf.Cos(_config.maxAngleAllowed);

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
        _bakeTileOneSurface.BuildNavMesh();

        yield return InitSerachGraph();
        _bakeStage = EBakeStage.CollectTriangle;
        yield return CollectTriangleRoutine();
        _bakeStage = EBakeStage.SamplePoint;
        yield return SamplePointRoutine();
    }

    private IEnumerator InitSerachGraph()
    {
        GUIClearProgress();
        for (int x = 0; x < _tileCountX; x++)
        {
            for (int z = 0; z < _tileCountZ; z++)
            {
                _tileIDNodeList.Add(new GraphNode<TileIdentifier>(new TileIdentifier(x, z)));
            }
        }
        _tileGraph = new UndirectedGraph<TileIdentifier>();

        for (int x = 0; x < _tileCountX; x++)
        {
            bool rightNeibourExist = x + 1 < _tileCountX;
            bool leftNeibourExist = x - 1 > 0;
            for (int z = 0; z < _tileCountZ; z++)
            {
                bool upNeibourExist = z + 1 < _tileCountZ;
                bool bottomNeibourExist = z - 1 > 0;

                if (leftNeibourExist)
                {
                    _tileGraph.AddPair(_tileIDNodeList[x * _tileCountZ + z], _tileIDNodeList[(x - 1) * _tileCountZ + z]);
                }

                if (rightNeibourExist)
                {
                    _tileGraph.AddPair(_tileIDNodeList[x * _tileCountZ + z], _tileIDNodeList[(x + 1) * _tileCountZ + z]);
                }

                if (upNeibourExist)
                {
                    _tileGraph.AddPair(_tileIDNodeList[x * _tileCountZ + z], _tileIDNodeList[x * _tileCountZ + z + 1]);
                }

                if (bottomNeibourExist)
                {
                    _tileGraph.AddPair(_tileIDNodeList[x * _tileCountZ + z], _tileIDNodeList[x * _tileCountZ + z - 1]);
                }
                GUISetTileSucceed(x, z);
            }
            yield return null;
            GUIUpdateProgress((float)x / _tileCountZ, "Create tile graph");
        }
    }

    #endregion

    #region ------------------------------------------------ Collect Triangles ----------------------------------------------------------

    private bool[,] _tileBakedArray;
    private Dictionary<TileIdentifier, List<Triangle>> _tileTriangleDic;
    private Dictionary<TileIdentifier, Bounds> _tileBoundsDic;
    // 取tile的右侧与上侧
    private Dictionary<TileIdentifier, TileConnection[]> _tileConnectionDic;
    private List<NavMeshBuildSource> _navmeshBuildSourceList;
    private List<NavMeshBuildMarkup> _navmeshBuildMarkupList;
    private List<Triangle> _cacheTriangleList;

    private int _collectProgressCount = 0;

    // 收集三角形 
    private IEnumerator CollectTriangleRoutine()
    {
        CollectTriangle_Init();
        // Collect all mesh in world
        //_navmeshBuildMarkupList.Clear();
        //_navmeshBuildSourceList.Clear();
        //// collect mesh
        //NavMeshBuilder.CollectSourcesInStage(_worldBound, _config.collectLayers, NavMeshCollectGeometry.RenderMeshes,
        //    _bakeTileOneArea, _navmeshBuildMarkupList, _bakeTileOneSurface.gameObject.scene, _navmeshBuildSourceList);
        GUIClearProgress();
        yield return _tileGraph.IterateBFSCoroutine(_tileIDNodeList[0], CollectTileTrianglesStep);
        GUIClearProgress();
        yield return _tileGraph.IterateBFSCoroutine(_tileIDNodeList[0], CollectTileConnectionTrianglesStep);
        GUIClearProgress();
        yield return RemoveUselessTrianglesInTile();
        GUIClearProgress();
        yield return RemoveUselessTriangleInConnection();
        Debug.Log("Collect Triangle Done!");
    }

    // 收集三角形阶段 初始化变量
    private void CollectTriangle_Init()
    {
        _tileTriangleDic = new Dictionary<TileIdentifier, List<Triangle>>(_tileCountTotal);
        _tileBoundsDic = new Dictionary<TileIdentifier, Bounds>(_tileCountTotal);
        _tileConnectionDic = new Dictionary<TileIdentifier, TileConnection[]>(_tileCountTotal);
        _navmeshBuildSourceList = new List<NavMeshBuildSource>(1000);
        _navmeshBuildMarkupList = new List<NavMeshBuildMarkup>(0);
        _cacheTriangleList = new List<Triangle>(100);
    }

    // Collect Triangle : Step 1
    private void CollectTileTrianglesStep(GraphNode<TileIdentifier> node)
    {
        // init
        TileIdentifier tileID = node.value;
        _tileBoundsDic[tileID] = new Bounds(GetTileCenterPosition(tileID), _config.tileSize);
        _tileTriangleDic[tileID] = new List<Triangle>(100);
        GetTrianglesInBound(_tileBoundsDic[tileID], _tileTriangleDic[tileID]);

        //------Debug info-------------
        _collectProgressCount++;
        GUISetTileSucceed(tileID.coordX, tileID.coordZ);
        GUIUpdateProgress((float)_collectProgressCount / _tileGraph.nodeCount, "[Collect Triangle] Get triangle from tile range.");
    }

    // Collect Triangle : Step 2
    private void CollectTileConnectionTrianglesStep(GraphNode<TileIdentifier> node)
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

    // Collect Triangle : Step 3
    private IEnumerator RemoveUselessTrianglesInTile()
    {
        string guimsg = "[Collect Triangle] Remove useless triangles in tile";
        ResetThreadParams(_tileCountTotal);
        yield return AssignThreadTasksRoutine(
            GetTileIdentifierForThread,
            (obj) => { return true; },
            ConcurrentRemoveTriangleInTile,
            guimsg);
    }

    // Collect Triangle : Step 4
    private IEnumerator RemoveUselessTriangleInConnection()
    {
        string guiMsg = "[Collect Triangle] Remove useless triangles in connection";
        ResetThreadParams(_tileCountTotal-1);
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
                case NavMeshBuildSourceShape.Terrain:
                    GetTriangleFromTerrain(source, resultList);
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
            vertices[i] = source.transform.MultiplyPoint(vertices[i]);
        }

        Bounds meshBound = new Bounds(source.transform.MultiplyPoint(mesh.bounds.center), mesh.bounds.size);

        if (tileBound.Intersects(meshBound) == false)
        {
            return;
        }

        if (tileBound.Contains(meshBound.min) && _worldBound.Contains(meshBound.max))
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

    // todo 从地形中取三角形
    private void GetTriangleFromTerrain(NavMeshBuildSource source, List<Triangle> triangleList)
    {
    }

    private void ConcurrentRemoveTriangleInTile(object tileIdentifier)
    {
        TileIdentifier tileID = (TileIdentifier)tileIdentifier;
        Bounds bound = _tileBoundsDic[tileID];
        int deleteCount = 0;
        int totalCount = _tileTriangleDic[tileID].Count;
        for (int i = _tileTriangleDic[tileID].Count - 1; i >= 0; i--)
        {
            if (!TriangleBoundsIntersect(bound, _tileTriangleDic[tileID][i]))
            {
                deleteCount++;
                _tileTriangleDic[tileID].RemoveAt(i);
            }
        }
        //Debug.Log($"{tileID} Total {totalCount} Delete {deleteCount}");
        GUISetTileSucceed(tileID.coordX, tileID.coordZ);
        lock (_locker)
        {
            _threadProcessedCount++;
            _threadAvailableCount++;
        }
    }

    private void ConcurrentRemoveTriangleInConnection(object tileIdentifier)
    {
        TileIdentifier tileID = (TileIdentifier)tileIdentifier;
        TileConnection[] tileConnections = _tileConnectionDic[tileID];

        for (int i = 0; i < tileConnections.Length; i++)
        {
            Bounds bound = tileConnections[i].bound;
            int deleteCount = 0;
            int totalCount = tileConnections[i].triangleList.Count;
            for (int triangleIndex = totalCount - 1; triangleIndex >= 0; triangleIndex--)
            {
                if (!TriangleBoundsIntersect(bound, tileConnections[i].triangleList[triangleIndex]))
                {
                    deleteCount++;
                    tileConnections[i].triangleList.RemoveAt(triangleIndex);
                }
            }
            //Debug.Log($"Connection_{tileID} Total {totalCount} Delete {deleteCount}");
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

    #region ------------------------------------------- Sample Point----------------------------------------------------------------
    private Vector3 _voxelSizeInv= new Vector3(2f,0.5f,2f); // voxel size 0.5,2,0.5
    private IEnumerator SamplePointRoutine()
    {
        GUIClearProgress();
        yield return PickRandomPointInTriangle();
        GUIClearProgress();
        yield return DividePointsInDifferentRegion();
        GUIClearProgress();
        yield return CheckRegionConnectivity();
    }

    // Sample Point : Step 1
    private IEnumerator PickRandomPointInTriangle()
    {
        Debug.Log("Sample piont");
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
        if (!_tileConnectionDic.ContainsKey(tileID))
        {
            // do nothing
        }
        else
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
    private IEnumerator DividePointsInDifferentRegion()
    {
        ResetThreadParams(_tileCountTotal-1);
        foreach (KeyValuePair<TileIdentifier, TileConnection[]> pair in _tileConnectionDic)
        {
            for (int tileConnectionIndex = 0; tileConnectionIndex < pair.Value.Length; tileConnectionIndex++)
            {
                TileConnection tileConnection = pair.Value[tileConnectionIndex];
            }

        }
        yield return null;

    }

    class DownsamplingVoxel
    {
        public int x;
        public int y;
        public int z;
        public Vector3 averagePos;
        public bool isEmptyVoxel;
        public bool hasAssignedRegion;
        public void SetAvgPosition(Vector3 newPos)
        {
            averagePos = 0.5f * averagePos + 0.5f * newPos;
        }
    }
    private void ConcurrentDosamplingPoints(object obj)
    {
        TileIdentifier tileID = (TileIdentifier)obj;
        if (_tileConnectionDic.ContainsKey(tileID) == false)
        {
            return;
        }
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

                if (voxelArray[indexX, indexY, indexZ] == null)
                {
                    voxelArray[indexX, indexY, indexZ] = new DownsamplingVoxel
                    {
                        x = indexX,
                        y = indexY,
                        z = indexZ,
                        isEmptyVoxel = false
                    };
                }
                else
                {
                    voxelArray[indexX, indexY, indexZ].SetAvgPosition(pos);
                }
            }

            // 划分连通区域
            int regionID = 0;
            Queue<DownsamplingVoxel> queue = new Queue<DownsamplingVoxel>(20);
            foreach (DownsamplingVoxel voxel in voxelArray)
            {
                if (voxel.isEmptyVoxel || voxel.hasAssignedRegion)
                {
                    continue;
                }

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
            }
        }
    }

    private void QueryFourNeibours2D(DownsamplingVoxel voxel, DownsamplingVoxel[,,] array , Queue<DownsamplingVoxel> queue)
    {
        int x = voxel.x;
        int y = voxel.y;
        int z = voxel.z;
        DownsamplingVoxel currentVoxel;
        if (x - 1 >= 0)
        {
            currentVoxel = array[x - 1, y, z];
            if (!currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }

        if (x + 1 < array.GetLength(0))
        {
            currentVoxel = array[x + 1, y, z];
            if (!currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }

        if (z - 1 >= 0)
        {
            currentVoxel = array[x, y, z - 1];
            if (!currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }

        if (z + 1 < array.GetLength(2))
        {
            currentVoxel = array[x, y, z + 1];
            if (!currentVoxel.isEmptyVoxel && !currentVoxel.hasAssignedRegion)
            {
                queue.Enqueue(currentVoxel);
            }
        }
    }

    // Sample Point : Step 3
    private IEnumerator CheckRegionConnectivity()
    {
        int totalCount = _tileConnectionDic.Count;
        int currentCount = 0;
        foreach (KeyValuePair<TileIdentifier, TileConnection[]> pair in _tileConnectionDic)
        {
            for (int tileConnectionIndex = 0; tileConnectionIndex < pair.Value.Length; tileConnectionIndex++)
            {
                TileConnection tileConnection = pair.Value[tileConnectionIndex];
                TileIdentifier[] connectedTiles = tileConnection.connectedTiles;
                BakeNavmesh(_bakeLinkSurface, tileConnection.bound.center, tileConnection.bound.size);
                BakeNavmesh(_bakeTileOneSurface, _tileBoundsDic[connectedTiles[0]].center, _tileBoundsDic[connectedTiles[0]].size);
                BakeNavmesh(_bakeTileTwoSurface, _tileBoundsDic[connectedTiles[1]].center, _tileBoundsDic[connectedTiles[1]].size);
            }

            yield return null;
            currentCount++;
            GUISetTileSucceed(pair.Key.coordX, pair.Key.coordZ);
            GUIUpdateProgress((float)currentCount / totalCount, "[Sample Point] Check point connectivity");
        }
    }

    private void BakeNavmesh(NavMeshSurface surface,Vector3 pos,Vector3 size)
    {
        surface.center = pos;
        surface.size = size;
        surface.BuildNavMesh();
    }
    #endregion

    #region -----------------------------------------------Create Node Graph-------------------------------------------------------------------------



    #endregion
}