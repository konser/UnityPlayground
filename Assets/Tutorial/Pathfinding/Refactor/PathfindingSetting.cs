using UnityEngine;
using System.Collections;

namespace RuntimePathfinding
{
    [System.Serializable]
    public struct PathfindingSetting
    {
        [Header("区域尺寸")]
        public float tileSize;
        public NavmeshTile tilePrefab;
        public NavmeshTileLink tileLinkPrefab;
        public NavmeshLinkBakeArea linkBakeArea;
        public void InitPrefab()
        {
            tilePrefab.Init(this);
            tileLinkPrefab.Init(this);
        }
    }
}