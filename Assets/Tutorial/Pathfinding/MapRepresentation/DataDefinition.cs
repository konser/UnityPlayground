using System.Collections.Generic;
using UnityEngine;

using ComputationalGeometry;
using DataStructure;

namespace RuntimePathfinding
{
    /// <summary>
    /// Navmesh区块标识
    /// </summary>
    public struct TileIdentifier
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

        public override string ToString()
        {
            return $"Tile({coordX},{coordZ})";
        }
    }

    public struct LinkPoint
    {
        public TileIdentifier ownerTileID;
        public Vector3 position;
    }

    public struct CrossTileInfo
    {
        public Vector3 posFrom;
        public Vector3 posTo;
    }

    [System.Serializable]
    public class NavigationMapInfo
    {
        public readonly Vector3 mapSize;
        public readonly Vector3 tileSize;
        public UndirectedGraph<LinkPoint> mapGraph;
        
        public NavigationMapInfo(Vector3 mapSize, Vector3 tileSize)
        {
            this.mapSize = mapSize;
            this.tileSize = tileSize;
        }

        public void AddLinkInfo(GraphNode<LinkPoint> first,GraphNode<LinkPoint> second)
        {
            mapGraph.AddPair(first,second,0);
        }

        public void QueryValidLinkPosition(TileIdentifier from,TileIdentifier to,List<CrossTileInfo> crossInfoList)
        {

        }

        public bool QueryAbstractPath(Vector3 from, Vector3 to, List<LinkPoint> path)
        {
            return false;
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