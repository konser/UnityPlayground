using UnityEngine;
using System.Collections;

namespace RuntimePathfinding
{
    [System.Serializable]
    public struct PathfindingSetting
    {
        public float mapSize;
        [Header("区块尺寸")]
        public float tileSize;

        public float bakeLinkAreaWidth;
        public NavmeshTile tilePrefab;
        public NavmeshTileLink tileLinkPrefab;
        public NavmeshLinkBakeArea linkBakeArea;
        public int linkBakeSampleCount;
    }
}