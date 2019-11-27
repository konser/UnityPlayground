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
    public float realCost; // g cost, fcost 与hcost不存储 运行中算出来
    public Node previous;
    public GameObject block;
    public readonly bool isObstacle;
    public Node(int x, int z, float realCost, bool obstacle)
    {
        this.x = x;
        this.z = z;
        this.realCost = realCost;
        previous = null;
        isObstacle = obstacle;
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
    public float yieldWaitTime = 2f;
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
    private WaitForSeconds waitTime;
    private Func<int, int, int, int, float> Heuristic;

    private void Start()
    {
        if (props == null)
        {
            props = new MaterialPropertyBlock();
        }

        blockPrefab = Resources.Load<GameObject>("ColorCube");
        CreateScene(texture);
        waitTime = new WaitForSeconds(yieldWaitTime);
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
                nodeMap[i, j] = new Node(i, j, Single.PositiveInfinity, isObstacle[i, j]);
                GameObject go = Instantiate(blockPrefab, p.transform);
                go.transform.position = new Vector3(i, 0, j);
                SetBlockColor(go, c);
                nodeMap[i, j].block = go;
            }
        }
    }

    private void SetBlockColor(GameObject block, Color color, bool draw = true)
    {
        props.SetColor("_Color", color);
        Renderer render = block.transform.GetComponent<Renderer>();
        render.SetPropertyBlock(props);
    }
    private void Reset()
    {
        _pathForDisplay.Clear();
        foreach (Node node in nodeMap)
        {
            node.realCost = Single.PositiveInfinity;
            if (isObstacle[node.x, node.z])
            {
                SetBlockColor(node.block, Color.black);
            }
            else
            {
                SetBlockColor(node.block, Color.white);
            }
        }
    }

    private float GetDistance(Node a, Node b)
    {
        return EulerDistance(a.x, a.z, b.x, b.z);
    }

    private bool IsWalkable(Node node)
    {
        if (node == null)
        {
            return false;
        }
        if (InRange(node) == false)
        {
            return false;
        }
        if (node.isObstacle)
        {
            return false;
        }
        return true;
    }

    private Node GetNode(int x, int z)
    {
        if (InRange(x, z) == false)
        {
            return null;
        }
        return nodeMap[x, z];
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
        return adjust_1 * (Mathf.Abs(x - xend) + Mathf.Abs(z - zend));
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
        while (n.previous != null)
        {
            n = n.previous;
            nodeList.Add(n);
        }
        return nodeList;
    }
    private Node[] GetAllNeibours(Node n)
    {
        int x = n.x;
        int z = n.z;
        Node[] neibours = new Node[8];

        if (InRange(x - 1, z)) neibours[0] = nodeMap[x - 1, z];
        if (InRange(x, z + 1)) neibours[1] = nodeMap[x, z + 1];
        if (InRange(x + 1, z)) neibours[2] = nodeMap[x + 1, z];
        if (InRange(x, z - 1)) neibours[3] = nodeMap[x, z - 1];

        if (InRange(x - 1, z - 1)) neibours[4] = nodeMap[x - 1, z - 1];
        if (InRange(x - 1, z + 1)) neibours[5] = nodeMap[x - 1, z + 1];
        if (InRange(x + 1, z + 1)) neibours[6] = nodeMap[x + 1, z + 1];
        if (InRange(x + 1, z - 1)) neibours[7] = nodeMap[x + 1, z - 1];
        return neibours;
    }

    private bool InRange(int x, int z)
    {
        return x >= 0 && z >= 0 && x < size && z < size;
    }

    private bool InRange(Node node)
    {
        return node.x >= 0 && node.z >= 0 && node.x < size && node.z < size;
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
        SetBlockColor(nodeMap[xstart, zstart].block, Color.yellow, drawPath);
        SetBlockColor(nodeMap[xend, zend].block, Color.blue, drawPath);

        // 寻路流程
        Stopwatch watch = Stopwatch.StartNew();
        HashSet<Node> reachedList = new HashSet<Node>();
        HashSet<Node> exploredList = new HashSet<Node>();
        PriorityQueue<Node, float> pq = new PriorityQueue<Node, float>(0f);
        Node[] neibours = new Node[8];

        nodeMap[xstart, zstart].realCost = 0f;
        pq.Insert(nodeMap[xstart, zstart], 0f);
        reachedList.Add(nodeMap[xstart, zstart]);

        while (pq.Count() != 0)
        {
            Node currNode = pq.Pop();
            if (currNode.x == xend && currNode.z == zend)
            {
                watch.Stop();
                Debug.Log($"寻路完成 耗时{watch.ElapsedMilliseconds}ms");
                _pathForDisplay = ConstructPath(currNode);
                yield break;
                //return ConstructPath(currNode);
            }
            exploredList.Add(currNode);
            reachedList.Remove(currNode);
            SetBlockColor(currNode.block, Color.gray, drawPath);
            neibours = GetAllNeibours(currNode);

            for (int i = 0; i < 8; i++)
            {
                if (neibours[i] != null && neibours[i].isObstacle == false && !exploredList.Contains(neibours[i]))
                {
                    float step = i > 4 ? 1.41421356f : 1f;
                    float estimateCost = Heuristic(neibours[i].x, neibours[i].z, xend, zend);
                    float newCost = currNode.realCost + step;
                    if (reachedList.Contains(neibours[i]) == false || newCost < neibours[i].realCost)
                    {
                        float beforeCost = neibours[i].realCost;
                        neibours[i].previous = currNode;
                        neibours[i].realCost = newCost;
                        if (reachedList.Contains(neibours[i]) == false)
                        {
                            Debug.Log($"Astart Add {neibours[i].x},{neibours[i].z}: {beforeCost + estimateCost} -> {estimateCost + neibours[i].realCost}");
                        }
                        else
                        {
                            Debug.Log($"Astar Update {neibours[i].x},{neibours[i].z}: {beforeCost + estimateCost} -> {estimateCost + neibours[i].realCost}");
                        }
                        pq.Insert(neibours[i], neibours[i].realCost + estimateCost);
                        reachedList.Add(neibours[i]);
                        SetBlockColor(neibours[i].block, Color.yellow, drawPath);
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
    #endregion

    #region JPS

    [ContextMenu("Test JPS")]
    private void TestJPS()
    {
        Reset();
        StopAllCoroutines();
        StartCoroutine(FindPathJPS(nodeMap[startPos.x, startPos.y], nodeMap[endPos.x, endPos.y]));
    }

    public IEnumerator FindPathJPS(Node startNode, Node goalNode)
    {
        if (!InRange(startNode) || !InRange(goalNode))
        {
            throw new IndexOutOfRangeException();
        }
        if (startNode.isObstacle || goalNode.isObstacle)
        {
            Debug.LogError("起点/终点处于障碍物！");
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
        Stopwatch watch = Stopwatch.StartNew();
        // --JPS Path Finding
        PriorityQueue<Node, float> priorityQueue = new PriorityQueue<Node, float>(0f);
        HashSet<Node> openSet = new HashSet<Node>();
        HashSet<Node> explored = new HashSet<Node>();
        openSet.Add(startNode);
        startNode.realCost = 0f;
        startNode.previous = null;
        priorityQueue.Insert(startNode, 0f);
        while (priorityQueue.Count() != 0)
        {
            Node currNode = priorityQueue.Pop();
            if (currNode.x == goalNode.x && currNode.z == goalNode.z)
            {
                watch.Stop();
                //Debug.Log($"JPS寻路完成，耗时{watch.ElapsedMilliseconds}ms");
                _pathForDisplay = ConstructPath(currNode);
                yield break;
            }
            explored.Add(currNode);
            openSet.Remove(currNode);

            IdentitySuccessors(currNode, goalNode, openSet, explored, priorityQueue);
            if (showProcess)
            {
                yield return waitTime;
            }
        }
        Debug.LogError("JPS 没找到路径");
    }

    private void IdentitySuccessors(Node currNode, Node goalNode, HashSet<Node> openSet, HashSet<Node> explored, PriorityQueue<Node, float> priorityQueue)
    {
        Node[] neibours = GetNeibours_JPS(currNode);
        for (int i = 0; i < neibours.Length; i++)
        {
            if (neibours[i] == null)
            {
                continue;
            }
            if (explored.Contains(neibours[i]))
            {
                Debug.Log("Skip neibour explored.");
                continue;
            }
            Node jumpNode = Jump(currNode, neibours[i], goalNode);
            if (jumpNode == null)
            {
                continue;
            }
            float dist = Heuristic(currNode.x, currNode.z, jumpNode.x, jumpNode.z);
            float newCost = currNode.realCost + dist;
            if (openSet.Contains(jumpNode) == false || newCost < jumpNode.realCost)
            {
                jumpNode.realCost = newCost;
                jumpNode.previous = currNode;
                float estimateCost = Heuristic(jumpNode.x, jumpNode.z, goalNode.x, goalNode.z);
                priorityQueue.Insert(jumpNode, jumpNode.realCost + estimateCost);
                openSet.Add(jumpNode);
            }
        }
    }

    private Node[] GetNeibours_JPS(Node node)
    {
        Node[] nodes = new Node[8];
        if (node.previous == null)
        {
            return GetAllNeibours(node);
        }
        Node parentNode = node.previous;
        int x = node.x, z = node.z;
        int px = parentNode.x;
        int pz = parentNode.z;
        int dx = (x - px) / Mathf.Max(Mathf.Abs(x - px), 1);
        int dz = (z - pz) / Mathf.Max(Mathf.Abs(z - pz), 1);

        if (dx != 0 && dz != 0)
        {
            if (IsWalkable(GetNode(x, z + dz)))
            {
                nodes[0] = GetNode(x, z + dz);
            }

            if (IsWalkable(GetNode(x + dx, z)))
            {
                nodes[1] = GetNode(x + dx, z);
            }

            if (IsWalkable(GetNode(x + dx, z + dz)))
            {
                nodes[2] = GetNode(x + dx, z + dz);
            }

            if (!IsWalkable(GetNode(x - dx, z)))
            {
                nodes[3] = GetNode(x - dx, z + dz);
            }
            if (!IsWalkable(GetNode(x, z - dz)))
            {
                nodes[4] = GetNode(x + dx, z - dz);
            }
        }
        else
        {
            if (dx == 0)
            {
                //nodes[0] = GetNode(x, z + dz);
                //nodes[1] = GetNode(x + 1, z + dz);
                //nodes[2] = GetNode(x - 1, z + dz);
                if (IsWalkable(GetNode(x, z + dz)))
                {
                    nodes[0] = GetNode(x, z + dz);
                }

                if (IsWalkable(GetNode(x + 1, z + dz)))
                {
                    nodes[1] = GetNode(x + 1, z + dz);
                }

                if (IsWalkable(GetNode(x - 1, z + dz)))
                {
                    nodes[2] = GetNode(x - 1, z + dz);
                }
            }
            else
            {
                //nodes[0] = GetNode(x + dx, z);
                //nodes[1] = GetNode(x + dx, z + 1);
                //nodes[2] = GetNode(x + dx, z - 1);
                if (IsWalkable(GetNode(x + dx, z)))
                {
                    nodes[0] = GetNode(x + dx, z);
                }

                if (IsWalkable(GetNode(x + dx, z + 1)))
                {
                    nodes[1] = GetNode(x + dx, z + 1);
                }

                if (IsWalkable(GetNode(x + dx, z - 1)))
                {
                    nodes[2] = GetNode(x + dx, z - 1);
                }
            }
        }
        return nodes;
    }

    private Node Jump(Node parentNode, Node neibour, Node goal)
    {
        if (neibour == null || !IsWalkable(neibour))
        {
            return null;
        }
        if (neibour == goal)
        {
            return neibour;
        }

        int dx = neibour.x - parentNode.x;
        int dz = neibour.z - parentNode.z;

        // 检查对角线
        if (dx != 0 && dz != 0)
        {
            // 检查对角线时也要对水平或垂直的进行检查，如果后续检查存在跳点，则该点是跳点
            if (Jump(neibour, GetNode(neibour.x + dx, neibour.z), goal) != null ||
                Jump(neibour, GetNode(neibour.x, neibour.z + dz), goal) != null)
            {
                return neibour;
            }
            if ((!IsWalkable(GetNode(neibour.x - dx, neibour.z)) && IsWalkable(GetNode(neibour.x - dx, neibour.z + dz))) ||
                (!IsWalkable(GetNode(neibour.x, neibour.z - dz)) && IsWalkable(GetNode(neibour.x + dx, neibour.z - dz))))
            {
                return neibour;
            }
        }
        else
        {
            // 水平 
            if (dx != 0)
            {
                if ((IsWalkable(GetNode(neibour.x + dx, neibour.z + 1)) && !IsWalkable(GetNode(neibour.x, neibour.z + 1))) ||
                    (IsWalkable(GetNode(neibour.x + dx, neibour.z - 1)) && !IsWalkable(GetNode(neibour.x, neibour.z - 1))))
                {
                    return neibour;
                }
            }
            else
            {
                if ((IsWalkable(GetNode(neibour.x - 1, neibour.z + dz)) && !IsWalkable(GetNode(neibour.x - 1, neibour.z))) ||
                    (IsWalkable(GetNode(neibour.x + 1, neibour.z + dz)) && !IsWalkable(GetNode(neibour.x + 1, neibour.z))))
                {
                    return neibour;
                }
            }

        }
        //SetBlockColor(neibour.block, Color.cyan, drawPath);
        return Jump(neibour, GetNode(neibour.x + dx, neibour.z + dz), goal);
    }
    #endregion

    private List<Node> _pathForDisplay = new List<Node>();
    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return;
        }
        if (InRange(startPos.x, startPos.y) == false)
        {
            startPos = Vector2Int.zero;
        }

        if (InRange(endPos.x, endPos.y) == false)
        {
            endPos = Vector2Int.zero;
        }
        Handles.color = Color.green;
        if (_pathForDisplay.Count != 0)
        {
            for (int i = 0; i < _pathForDisplay.Count - 1; i++)
            {
                Handles.DrawLine(_pathForDisplay[i].block.transform.position + Vector3.up * 0.1f,
                    _pathForDisplay[i + 1].block.transform.position + Vector3.up * 0.1f);
            }
        }

        Handles.color = Color.yellow;
        Handles.DrawSolidDisc(new Vector3(startPos.x, 0.1f, startPos.y), Vector3.up, 0.8f);
        Handles.color = Color.blue;
        Handles.DrawSolidDisc(new Vector3(endPos.x, 0.1f, endPos.y), Vector3.up, 0.8f);
#endif
    }
}