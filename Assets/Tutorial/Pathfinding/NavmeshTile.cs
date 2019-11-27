using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
namespace RuntimePathfinding
{
    /// <summary>
    /// 运行时计算的部分区域详细Navmesh
    /// </summary>
    public class NavmeshTile : PooledObject
    {
        public Vector3 tileCenterPos
        {
            get { return _tileCenterPos; }
        }

        public int tileCoordX
        {
            get { return _tileIdentifier.coordX; }
        }

        public int tileCoordZ
        {
            get { return _tileIdentifier.coordZ; }
        }

        public int sequenceIndex
        {
            get { return _tileIdentifier.sequenceIndex; }
        }

        private NavMeshSurface _surface;
        //private List<NavmeshTileLink> _previousTileLinkList; // 假设当前区块为B，该Link指A-B
        private List<NavmeshTileLink> _nextTileLinkList = new List<NavmeshTileLink>(16); // 该Link指B-C
        private NavmeshTile nextTile;
        private TileIdentifier _tileIdentifier;
        private float _tileSize;
        private Vector3 _tileCenterPos;
        private Vector3 _tileLeftBottomPos;
        private Vector3 _tileRightTopPos;
        public void Init(PathfindingSetting setting)
        {
            // 设置NavmeshSurface的参数
            _surface = gameObject.GetComponent<NavMeshSurface>();
            if(_surface == null)
            {
                _surface = gameObject.AddComponent<NavMeshSurface>();
            }
            _tileSize = setting.tileSize;

            _surface.collectObjects = CollectObjects.Volume;
            _surface.defaultArea = RuntimePathfinding.areaDetail;
            _surface.center = Vector3.zero;
            _surface.size = new Vector3(_tileSize+0.5f, _tileSize * 8.0f, _tileSize+0.5f);
        }

        public void BakeNavmeshTile(TileIdentifier tileIdentifier)
        {
            _tileIdentifier = tileIdentifier;
            //Debug.Log("Create Tile " +this);
            SetTilePosition();
            _surface.BuildNavMesh();
        }

        public bool ContainsPosition2D(Vector3 pos)
        {
            if (pos.x >= _tileLeftBottomPos.x && pos.x <= _tileRightTopPos.x &&
                pos.z >= _tileLeftBottomPos.z && pos.z <= _tileRightTopPos.z)
            {
                return true;
            }
            return false;
        }

        public List<NavmeshTileLink> GetLinkListToNextTile()
        {
            return _nextTileLinkList;
        }

        private void SetTilePosition()
        {
            _tileCenterPos = new Vector3((_tileIdentifier.coordX+ 0.5f) * _tileSize, 0, (_tileIdentifier.coordZ + 0.5f) * _tileSize);
            float height = Utility.GetTerrainHeight(_tileCenterPos);
            _tileCenterPos.y = height;
            _tileLeftBottomPos = new Vector3(_tileSize * _tileIdentifier.coordX,0, _tileSize * _tileIdentifier.coordZ); // Y值不重要 做2D判断用的
            _tileRightTopPos = new Vector3(_tileSize * (_tileIdentifier.coordX + 1), 0, _tileSize *(_tileIdentifier.coordZ+1)); // Y值不重要 做2D判断用的
            this.transform.position = _tileCenterPos;
        }

        public virtual void ReturnToPool()
        {
            base.ReturnToPool();
            //Debug.Log($"Recycle {this} Link Count {_nextTileLinkList.Count}");
            // 将Link回收至池子里
            for (int i = 0; i < _nextTileLinkList.Count; i++)
            {
               _nextTileLinkList[i].ReturnToPool();
            }
            _nextTileLinkList.Clear();
        }

        public void AddNextNavmeshLink(NavmeshTileLink link)
        {
            if (link == null)
            {
                return;
            }
            _nextTileLinkList.Add(link);
        }

        public override string ToString()
        {
            return $"[({tileCoordX},{tileCoordZ}) - {sequenceIndex}]";
        }
    }
}