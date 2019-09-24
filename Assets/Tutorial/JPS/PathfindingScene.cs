using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Node
{
    public int x;
    public int z;
    public float costSoFar;
    public Node previous;
    public GameObject block;
    
    public Node(int x, int z, float cost)
    {
        this.x = x;
        this.z = z;
        this.costSoFar = cost;
        previous = null;
    }
}

public enum EHeuristic
{
    EulerDistance,
    ManhattonDistance,
    DiagonalDistance
}

public class PathfindingScene : MonoBehaviour
{
    public Texture2D texture;
    public Vector2Int startPos;
    public Vector2Int endPos;
    [Header("距离估算方式")]
    public EHeuristic heuristic = EHeuristic.EulerDistance;
    [Header("距离估算调节参数")]
    public float adjust_1;
    public float adjust_2;
    public bool drawPath;
    public bool showProcess;
    private bool[,] isObstacle;
    private GameObject blockPrefab;
    private MaterialPropertyBlock props;
    private int size;
    private Node[,] nodeMap;
    private WaitForSeconds waitTime = new WaitForSeconds(0.02f);
    private Func<int, int, int, int, float> Heuristic;

    private void Start()
    {
        if (props == null)
        {
            props = new MaterialPropertyBlock();
        }

        blockPrefab = Resources.Load<GameObject>("ColorCube");
        CreateScene(texture);
    }

    private void CreateScene(Texture2D texture)
    {
        GameObject p = new GameObject("Pathfinding Scene");
        isObstacle = new bool[texture.width, texture.height];
        size = texture.width;
        nodeMap = new Node[size, size];
        Color c;
        for (int i = 0; i < texture.width; i++)
        {
            for (int j = 0; j < texture.height; j++)
            {
                float value = texture.GetPixel(i, j).r;
                // 阻挡
                if (value == 0)
                {
                    isObstacle[i, j] = true;
                    c = Color.black;
                }
                else
                {
                    isObstacle[i, j] = false;
                    c = Color.white;
                }
                nodeMap[i, j] = new Node(i, j, Single.PositiveInfinity);
                GameObject go = Instantiate(blockPrefab, p.transform);
                go.transform.position = new Vector3(i, 0, j);
                SetBlockColor(go, c);
                nodeMap[i, j].block = go;
            }
        }
    }

    private void SetBlockColor(GameObject block, Color color,bool draw = true)
    {
        props.SetColor("_Color", color);
        Renderer render = block.transform.GetComponent<Renderer>();
        render.SetPropertyBlock(props);
    }
    private void Reset()
    {
        foreach (Node node in nodeMap)
        {
            node.costSoFar = Single.PositiveInfinity;
            if (isObstacle[node.x, node.z])
            {
                SetBlockColor(node.block,Color.black);
            }
            else
            {
                SetBlockColor(node.block,Color.white);
            }
        }
    }

    #region AStar

    [ContextMenu("Test ASTAR")]
    public void Test()
    {
        this.StopAllCoroutines();
        Reset();
        StartCoroutine(FindPathAstar(startPos.x, startPos.y, endPos.x, endPos.y));
    }
    public IEnumerator FindPathAstar(int xstart, int zstart, int xend, int zend)
    {
        // 一些设置
        if (!InRange(xstart, zstart) || !InRange(xend, zend))
        {
            throw new ArgumentOutOfRangeException();
        }
        if (isObstacle[xend, zend] || isObstacle[xstart, zstart])
        {
            yield break;
        }
        switch (heuristic)
        {
            case EHeuristic.EulerDistance:
                Heuristic = EulerDistance;
                break;
            case EHeuristic.ManhattonDistance:
                Heuristic = ManhattonDistance;
                break;
            case EHeuristic.DiagonalDistance:
                Heuristic = DiagonalDistance;
                break;
            default:
                Heuristic = EulerDistance;
                break;
        }
        SetBlockColor(nodeMap[xstart, zstart].block,Color.yellow,drawPath);
        SetBlockColor(nodeMap[xend,zend].block,Color.green,drawPath);

        // 寻路流程
        Stopwatch watch = Stopwatch.StartNew();
        List<Node> reachedList = new List<Node>();
        List<Node> exploredList = new List<Node>();
        PriorityQueue<Node, float> pq = new PriorityQueue<Node, float>(0f);
        Node[] neibours = new Node[8];

        nodeMap[xstart, zstart].costSoFar = 0f;
        pq.Insert(nodeMap[xstart,zstart], 0f);
        reachedList.Add(nodeMap[xstart, zstart]);

        while (pq.Count() != 0)
        {
            Node currNode = pq.Pop();
            if (currNode.x == xend && currNode.z == zend)
            {
                watch.Stop();
                Debug.Log($"寻路完成 耗时{watch.ElapsedMilliseconds}ms");
                ConstructPath(currNode);
                yield break;
                //return ConstructPath(currNode);
            }
            exploredList.Add(currNode);
            SetBlockColor(currNode.block,Color.gray,drawPath);
            neibours = GetNeibours(currNode);

            for (int i = 0; i < 8; i++)
            {
                if (neibours[i] != null && !exploredList.Contains(neibours[i]))
                {
                    float step = i > 4 ? 1.41421356f : 1f;
                    float newCost = currNode.costSoFar + step;
                    if (reachedList.Contains(neibours[i]) == false || newCost < neibours[i].costSoFar)
                    {
                        reachedList.Add(neibours[i]);
                        neibours[i].previous = currNode;
                        neibours[i].costSoFar = newCost;
                        pq.Insert(neibours[i],neibours[i].costSoFar + Heuristic(neibours[i].x,neibours[i].z,xend,zend));
                        SetBlockColor(neibours[i].block,Color.yellow,drawPath);
                    }
                }
            }
            if (showProcess)
            {
                yield return null;
            }
        }
        //EditorUtility.ClearProgressBar();
    }

    // --距离估算
    private float EulerDistance(int x, int z, int xend, int zend)
    {
        float dx = (float)x - xend;
        float dy = (float)z - zend;
        return adjust_1 * Mathf.Sqrt(dx * dx + dy * dy);
    }
    private float ManhattonDistance(int x, int z, int xend, int zend)
    {
        return  adjust_1 *(Mathf.Abs(x - xend) + Mathf.Abs(z - zend));
    }

    private float DiagonalDistance(int x, int z, int xend, int zend)
    {
        float dx = Mathf.Abs((float)x - xend);
        float dy = Mathf.Abs((float)z - zend);
        return adjust_1 * (dx + dy) + (adjust_2 - 2 * adjust_1) * Mathf.Min(dx, dy);
    }

    private List<Node> ConstructPath(Node n)
    {
        List<Node> nodeList = new List<Node>();
        nodeList.Add(n);
        SetBlockColor(n.block,Color.green);
        while (n.previous != null)
        {
            n = n.previous;
            SetBlockColor(n.block,Color.green);
            nodeList.Add(n);
        }
        return nodeList;
    }

    private Node[] GetNeibours(Node n)
    {
        int x = n.x;
        int z = n.z;
        Node[] neibours = new Node[8];

        if (InRange(x - 1, z) && !isObstacle[x-1,z]) neibours[0] = nodeMap[x - 1, z];
        if (InRange(x, z + 1) && !isObstacle[x, z + 1]) neibours[1] = nodeMap[x, z + 1];
        if (InRange(x + 1, z) && !isObstacle[x + 1, z]) neibours[2] = nodeMap[x + 1, z];
        if (InRange(x, z - 1) && !isObstacle[x, z - 1]) neibours[3] = nodeMap[x, z - 1];
        
        if (InRange(x - 1, z - 1) && !isObstacle[x - 1, z - 1]) neibours[4] = nodeMap[x - 1, z - 1];
        if (InRange(x - 1, z + 1) && !isObstacle[x - 1, z + 1]) neibours[5] = nodeMap[x - 1, z + 1];
        if (InRange(x + 1, z + 1) && !isObstacle[x + 1, z + 1]) neibours[6] = nodeMap[x + 1, z + 1];
        if (InRange(x + 1, z - 1) && !isObstacle[x + 1, z - 1]) neibours[7] = nodeMap[x + 1, z - 1];
        return neibours;
    }

    private bool InRange(int x, int z)
    {
        return x >= 0 && z >= 0 && x < size && z < size;
    }
    #endregion

    #region JPS



    #endregion

    private void OnDrawGizmosSelected()
    {
        if (InRange(startPos.x,startPos.y) == false)
        {
            startPos = Vector2Int.zero;
        }

        if (InRange(endPos.x, endPos.y) == false)
        {
            endPos = Vector2Int.zero;
        }

        Handles.color = Color.yellow;
        Handles.DrawSolidDisc(new Vector3(startPos.x,0.1f,startPos.y),Vector3.up,0.8f);
        Handles.color = Color.green;
        Handles.DrawSolidDisc(new Vector3(endPos.x, 0.1f, endPos.y), Vector3.up, 0.8f);
    }
}