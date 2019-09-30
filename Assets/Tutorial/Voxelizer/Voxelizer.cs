using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class Voxelizer : MonoBehaviour
{
    public static Voxelizer Instance { get; set; }
    public BoxCollider voxelizeBox;
    public Vector3 voxelSize;
    public GameObject spanPrefab;
    public GameObject antiSpanPrefab;
    [Header("Params")]
    public int octreeGroupTriangleCount = 16;
    public int intersectionDivideAreaLength = 32;
    [Header("Gizmos Config")]
    public bool showAntiSpan = false;
    public bool showVoxelWire = false;
    public bool showOctreeWire = false;

    private TraverseSceneTriangleStage _traverseSceneTriangleStage;
    private OctreeSceneStage _octreeSceneStage;
    private IntersectTestStage _intersectTestStage;
    private VoxelMergeStage _voxelMergeStage;
    private AntiSpanStage _antiSpanStage;
    private SpanMergeStage _spanMergeStage;

    private void Awake()
    {
        Instance = this;
    }


    [ContextMenu("Voxelize")]
    public void Voxelize()
    {
        InitComponents();
        // ground plane
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.transform.SetParent(this.transform, false);
        go.transform.position = new Vector3(voxelizeBox.bounds.center.x, voxelizeBox.bounds.min.y + 0.01f, voxelizeBox.bounds.center.z);
        go.transform.localScale = new Vector3(100, 0, 100);

        // 遍历场景
        List<TriangleInfo> triangleList = _traverseSceneTriangleStage.RetreiveSceneTriangles();
        //_traverseSceneTriangleStage = null;
        EditorUtility.ClearProgressBar();
        // 构建八叉树
        OctTree<TriangleBound> octree = _octreeSceneStage.ConstructSceneOctree(triangleList, voxelizeBox.bounds);
        //_octreeSceneStage = null;
        EditorUtility.ClearProgressBar();
        // 相交测试W
        _intersectTestStage.SetParams(voxelSize,octree);
        bool[,,] intersectionResult = _intersectTestStage.IntersectTest();
        //_intersectTestStage = null;
        EditorUtility.ClearProgressBar();
        // 合并
        _voxelMergeStage.SetParams(voxelSize, intersectionResult);
        VoxelSpan[,] spans = _voxelMergeStage.Merge();
        //_voxelMergeStage = null;

        // 反体素 这里传入的高度是相对于包围盒最小点的 即下表面为0(所以不传入) 上表面为包围盒高度
        VoxelSpan[,] antiSpans = _antiSpanStage.ConstructAntiSpan(spans, 2*voxelizeBox.bounds.extents.y);

        // 连通区域测试以及不可走区域填充


        SaveResult(spans);
        if (showAntiSpan)
        {
            CreateSpanCube(antiSpans, antiSpanPrefab);
        }
        CreateSpanCube(spans,spanPrefab);
        Destroy(go);
    }

    public void CreateFromFile()
    {
      var b =  File.ReadAllBytes("Voxel.bin");
      BinaryFormatter bf = new BinaryFormatter();
      VoxelSpan[,] spans = bf.Deserialize(new MemoryStream(b)) as VoxelSpan[,];
      CreateSpanCube(spans,spanPrefab);
    }

    private void SaveResult(VoxelSpan[,] spanArray)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms,spanArray);
            byte[] voxelBin = ms.ToArray();
            File.WriteAllBytes("Voxel.bin",voxelBin);
        }
    }

    private void InitComponents()
    {
        if (_traverseSceneTriangleStage == null)
        {
            _traverseSceneTriangleStage = new TraverseSceneTriangleStage();
            _traverseSceneTriangleStage.validAreaBox = new AABBBoundBox(voxelizeBox.bounds);
        }

        if (_octreeSceneStage == null)
        {
            _octreeSceneStage = new OctreeSceneStage();
            _octreeSceneStage.triangleCountInOneGroup = octreeGroupTriangleCount;
        }

        if (_intersectTestStage == null)
        {
            _intersectTestStage = new IntersectTestStage();
            _intersectTestStage.divide = intersectionDivideAreaLength;
        }

        if (_voxelMergeStage == null)
        {
            _voxelMergeStage = new VoxelMergeStage();
        }

        if (_antiSpanStage == null)
        {
            _antiSpanStage = new AntiSpanStage();
        }

        if (_spanMergeStage == null)
        {
            _spanMergeStage = new SpanMergeStage();
        }
    }

    private void CreateSpanCube(VoxelSpan[,] voxelSpans,GameObject prefab)
    {
        GameObject voxelHolder = new GameObject("VoxelHolder");
        foreach (VoxelSpan span in voxelSpans)
        {
            if (span.isEmpty)
            {
                continue;
            }
            for (int i = 0; i < span.spanList.Count; i++)
            {
                GameObject cube = GameObject.Instantiate(prefab, voxelHolder.transform);
                Vector3 xzPos = GetSpanCenter(span.x, span.z);
                Vector2 height = span.spanList[i];
                SetSpanCubeHeight(cube,xzPos,height.x,height.y);
            }
        }
    }

    private void SetSpanCubeHeight(GameObject span,Vector3 xzPos,float bottomHeight,float topHeight)
    {
        Vector3 pos = xzPos;
        span.transform.localScale = new Vector3(voxelSize.x, topHeight - bottomHeight, voxelSize.z);
        xzPos.y += bottomHeight + 0.5f * (topHeight - bottomHeight);
        span.transform.localPosition = xzPos;
    }

    /// <summary>
    /// 注意 这里坐标是以包围盒左下角为原点，按X,Z坐标获取的，取单元格子的中心位置
    /// </summary>
    private Vector3 GetSpanCenter(int x, int z)
    {
        return voxelizeBox.bounds.min +  new Vector3(voxelSize.x*(x+0.5f),0f,voxelSize.z*(z+0.5f));
    }

    private void OnDrawGizmos()
    {
        if (showOctreeWire && _octreeSceneStage != null)
        {
            _octreeSceneStage.DebugDraw();
        }

        if (showVoxelWire && _intersectTestStage != null)
        {
            _intersectTestStage.DebugDraw();
        }
    }
}
