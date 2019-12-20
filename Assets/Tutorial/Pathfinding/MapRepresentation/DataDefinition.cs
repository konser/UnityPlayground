using System;
using System.Collections.Generic;
using UnityEngine;

using ComputationalGeometry;
using DataStructure;
using JetBrains.Annotations;

namespace RuntimePathfinding
{
    /// <summary>
    /// Navmesh区块标识
    /// </summary>
    [System.Serializable]
    public struct TileIdentifier : IEquatable<TileIdentifier>
    {
        public TileIdentifier(int x, int z)
        {
            coordX = x;
            coordZ = z;
            sequenceIndex = 0;
        }
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

        public bool Equals(TileIdentifier other)
        {
            return coordX == other.coordX && coordZ == other.coordZ;
        }

        public override bool Equals(object obj)
        {
            return obj is TileIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var tHashCode = coordX;
                tHashCode = (tHashCode * 397) ^ coordZ;
                tHashCode = (tHashCode * 397) ^ sequenceIndex;
                return tHashCode;
            }
        }
        public override string ToString()
        {
            return $"Tile({coordX},{coordZ})";
        }

        public static bool operator ==(TileIdentifier t1, TileIdentifier t2)
        {
            return t1.Equals(t2);
        }

        public static bool operator !=(TileIdentifier t1, TileIdentifier t2)
        {
            return !t1.Equals(t2);
        }
    }

    [System.Serializable]
    public struct LinkPoint
    {
        public TileIdentifier ownerTileID;
        public SerializableVec3 position;

    }

    [System.Serializable]
    public class NavigationMapInfo
    {
        public readonly SerializableVec3 mapSize;
        public readonly SerializableVec3 tileSize;
        public NavigationGraph navigationGraph;
        private int _tileCountX;
        private int _tileCountZ;

        public void InitMapInfo()
        {
            if (navigationGraph != null)
            {
                navigationGraph.InitGraph();
            }
        }

        public NavigationMapInfo(Vector3 mapSize, Vector3 tileSize)
        {
            this.mapSize = mapSize;
            this.tileSize = tileSize;
            _tileCountX = Mathf.CeilToInt(mapSize.x / tileSize.x);
            _tileCountZ = Mathf.CeilToInt(mapSize.z / tileSize.z);
        }

        public void AddLinkInfo(GraphNode<LinkPoint> first, GraphNode<LinkPoint> second)
        {
            navigationGraph.AddPair(first, second, 0);
        }

        public TileIdentifier GetTileIdentifier(Vector3 pos)
        {
            int x = (int)(pos.x / tileSize.x);
            int z = (int)(pos.z / tileSize.z);
            if (x >= 0 && x < _tileCountX && z >= 0 && z < _tileCountZ)
            {
                return new TileIdentifier(x,z);
            }
            Debug.LogError($"越界 Position: {pos}, TileID:({x},{z})");
            return default;
        }
    }

    
    [System.Serializable]
    public class NavigationGraph : UndirectedGraph<LinkPoint>
    {
        private Dictionary<TileIdentifier, List<NavigationNode>> _tileNodeDic = new Dictionary<TileIdentifier, List<NavigationNode>>();
        [System.NonSerialized]
        private PriorityQueue<NavigationNode,float> _priorityQueue;
        private List<GraphNode<LinkPoint>> _searchNeibourCacheList;

        public void InitGraph()
        {
            if (_priorityQueue == null)
            {
                _priorityQueue = new PriorityQueue<NavigationNode, float>(0f);
            }
        }

        public void AddNodeToTile(TileIdentifier id, NavigationNode node)
        {
            if (_tileNodeDic.ContainsKey(id) == false)
            {
                _tileNodeDic[id] = new List<NavigationNode>();
            }
            else
            {
                if (_tileNodeDic[id].Contains(node) == false)
                {
                    _tileNodeDic[id].Add(node);
                }
            }
        }

        public List<NavigationNode> GetTileNodes(TileIdentifier tileID)
        {
            if (_tileNodeDic.ContainsKey(tileID))
            {
                return _tileNodeDic[tileID];
            }
            return null;
        }

        public bool IsSameTileNeibour(NavigationNode node, NavigationNode neibour)
        {
            if (node.value.ownerTileID == neibour.value.ownerTileID)
            {
                return true;
            }
            return false;
        }

        public bool IsCrossTileNeibour(NavigationNode node, NavigationNode neibour)
        {
            int dx = node.value.ownerTileID.coordX - neibour.value.ownerTileID.coordX;
            int dz = node.value.ownerTileID.coordZ - neibour.value.ownerTileID.coordZ;
            dx = dx > 0 ? dx : -dx;
            dz = dz > 0 ? dz : -dz;

            if ((dx == 0 && dz == 1) || (dx == 1) && (dz==0))
            {
                return true;
            }
            return false;
        }

        private NavigationNode _currentNode;
        private NavigationNode _neibourNode;
        public bool Search(NavigationNode startNode, NavigationNode endNode,List<NavigationNode> result)
        {
            result.Clear();

            if (startNode == endNode)
            {
                result.Add(startNode);
                return true;
            }
            ResetGraphStatus();
            _priorityQueue.ClearQueue();
            _priorityQueue.Insert(startNode,0f); // 该优先队列的实现不包括查询节点是否存在
            startNode.costToNode = 0f;

            while (_priorityQueue.Count() != 0)
            {
                _currentNode = _priorityQueue.Pop();
                if (_currentNode == endNode)
                {
                    NavigationNode pathNode = endNode;
                    while (pathNode.previousNode != null)
                    {
                        result.Add(pathNode);
                        pathNode = pathNode.previousNode;
                    }
                    result.Add(pathNode);
                    result.Reverse(0,result.Count);
                    return true;
                }
                //Debug.DrawLine(currentNode.value.position,currentNode.value.position+Vector3.up*50f,Color.red,5f);
                // 当前节点添加至已访问列表
                _currentNode.isVisited = true;

                // 获取邻接节点
                _searchNeibourCacheList = _currentNode.neibours;

                for (int i = 0; i < _searchNeibourCacheList.Count; i++)
                {
                    _neibourNode = _searchNeibourCacheList[i] as NavigationNode;
                    // 已访问或者已经在队列中 跳过
                    if (_neibourNode.isVisited || _priorityQueue.Contains(_neibourNode))
                    {
                        continue;
                    }

                    // 估算邻接节点至终点距离
                    float estimateCostToEnd = Vector3.Distance(_neibourNode.value.position.XZ(),endNode.value.position.XZ());
                    // 到邻接节点的实际消耗
                    float newCost = _currentNode.costToNode + _currentNode.CostToNeibour(_neibourNode);
                    // 如果待访问节点不包括邻接节点，或者新的路径消耗更低
                    if (!_priorityQueue.Contains(_neibourNode))
                    {
                        _neibourNode.costToNode = newCost;
                        _neibourNode.previousNode = _currentNode;
                        _priorityQueue.Insert(_neibourNode,_neibourNode.costToNode + estimateCostToEnd);
                    }else if (newCost < _neibourNode.costToNode)
                    {
                        _neibourNode.costToNode = newCost;
                        _neibourNode.previousNode = _currentNode;
                        _priorityQueue.Decrease(_neibourNode,_neibourNode.costToNode);
                    }
                }
            }
            return false;
        }

        public void ResetGraphStatus()
        {
            for (int i = 0; i < nodeList.Count; i++)
            {
                ResetNode(nodeList[i]);
            }
        }

        private void ResetNode(GraphNode<LinkPoint> node)
        {
            (node as NavigationNode)?.ResetPathStatus();
        }
    }


    // --------------old test code,to be deleted----------------
    public class LinkNode : IConvexPoint
    {
        public int x;
        public int y;
        public int z;
        public int regionID = -1;
        public bool hasPoint;
        public Vector3 pos;
        public Bounds bound;
        public bool isConnectedRegion;
        public bool inRegionOne;
        public bool inRegionTwo;
        public bool isLinkPoint;
        public LinkNode peer;

        public Vector3 position
        {
            get { return pos; }
        }
    }

    public class PathConnectInfo
    {
        public List<Vector3> regionOnePos = new List<Vector3>();
        public List<Vector3> regionTwoPos = new List<Vector3>();
    }
}