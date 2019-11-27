using UnityEngine;
using UnityEngine.AI;

namespace RuntimePathfinding
{
    /// <summary>
    /// 用来生成TileLink的Navmesh区域
    /// 利用NavmeshSurface来计算两块Tile之间的可通行区域，在可通行区域上生成TileLink
    /// </summary>
    public class NavmeshLinkBakeArea : MonoBehaviour
    {
        private NavMeshSurface _surface;
        private Vector3[] _samplePosArrayOne;
        private Vector3[] _samplePosArrayTwo;
        private int _sampleCount;
        private float _offset;
        private Vector3 _bakeAreaCenter;
        private NavMeshPath _navmeshPath;
        private NavmeshTile _temp = null; // 用来交换两个tile先后顺序
        public void Init(PathfindingSetting setting)
        {
            _surface = gameObject.GetComponent<NavMeshSurface>();
            if (!_surface)
            {
                _surface = gameObject.AddComponent<NavMeshSurface>();
            }
            _surface.size = new Vector3(setting.tileSize*0.98f, setting.tileSize * 8.0f, setting.bakeLinkAreaWidth);
            _surface.center = Vector3.zero;
            _surface.collectObjects = CollectObjects.Volume;
            _surface.defaultArea = RuntimePathfinding.areaBakeLink;
            _sampleCount = setting.linkBakeSampleCount;
            _navmeshPath = new NavMeshPath();
            _samplePosArrayOne = new Vector3[_sampleCount];
            _samplePosArrayTwo = new Vector3[_sampleCount];
            _offset = _surface.size.z * 0.25f;
        }


        public void BakeTileLink(NavmeshTile tileFrom, NavmeshTile tileTo, ObjectPool linkPool)
        {
            if (!tileTo || !tileFrom)
            {
                Debug.LogError("BakeLinkArea : Tile is null");
                return;
            }
            // 如果tileFrom在后 则与tileTo进行交互 
            if (tileFrom.sequenceIndex > tileTo.sequenceIndex)
            {
                _temp = tileTo;
                tileTo = tileFrom;
                tileFrom = _temp;
            }

            _bakeAreaCenter = 0.5f * (tileTo.tileCenterPos + tileFrom.tileCenterPos);
            _bakeAreaCenter.y = Utility.GetTerrainHeight(_bakeAreaCenter);
            this.transform.position = _bakeAreaCenter;
            int dx = tileTo.tileCoordX - tileFrom.tileCoordX;
            int dz = tileTo.tileCoordZ - tileFrom.tileCoordZ;
            SamplePosition(dx, dz);

            GenerateTileLink(linkPool, ref tileFrom, ref tileTo);
        }

        // 在接缝两侧分别采样处于navmesh内的位置点
        private void SamplePosition(int dx, int dz)
        {
            Vector3 startPos = _bakeAreaCenter;
            float step = _surface.size.x / _sampleCount;
            //    Tile | Tile
            if (dx != 0 && dz == 0)
            {
                this.transform.forward = Vector3.right;
                _surface.BuildNavMesh();
                startPos.z -= _surface.size.x * 0.5f;

                for (int i = 0; i < _sampleCount; i++)
                {
                    _samplePosArrayOne[i] = new Vector3(startPos.x + _offset, 0, startPos.z + step);
                    _samplePosArrayOne[i].y = Utility.GetTerrainHeight(_samplePosArrayOne[i]);
                    _samplePosArrayTwo[i] = new Vector3(startPos.x - _offset, 0, startPos.z + step);
                    _samplePosArrayTwo[i].y = Utility.GetTerrainHeight(_samplePosArrayTwo[i]);
                    startPos.z += step;
                }
            }
            //  Tile
            //  ----
            //  Tile
            else if (dz != 0 && dx == 0)
            {
                this.transform.forward = Vector3.forward;
                _surface.BuildNavMesh();
                startPos.x -= _surface.size.x * 0.5f;
                for (int i = 0; i < _sampleCount; i++)
                {
                    _samplePosArrayOne[i] = new Vector3(startPos.x + step, 0, startPos.z + _offset);
                    _samplePosArrayOne[i].y = Utility.GetTerrainHeight(_samplePosArrayOne[i]);
                    _samplePosArrayTwo[i] = new Vector3(startPos.x + step, 0, startPos.z - _offset);
                    _samplePosArrayTwo[i].y = Utility.GetTerrainHeight(_samplePosArrayTwo[i]);
                    startPos.x += step;
                }
            }
        }

        private void GenerateTileLink(ObjectPool linkPool, ref NavmeshTile tileFrom, ref NavmeshTile tileTo)
        {
            int linkCount = 0;
            for (int i = 0; i < _sampleCount; i++)
            {
                bool hasNavmeshAtStartPos = NavMesh.SamplePosition(_samplePosArrayOne[i], out NavMeshHit hitResultOne,0.5f, RuntimePathfinding.areaMaskBakeLink);
                bool hasNavmeshAtEndPos = NavMesh.SamplePosition(_samplePosArrayTwo[i], out NavMeshHit hitResultTwo, 0.5f, RuntimePathfinding.areaMaskBakeLink);
               // Debug.DrawLine(_samplePosArrayOne[i],_samplePosArrayTwo[i],Color.red,5.0f);
                if (hasNavmeshAtStartPos && hasNavmeshAtEndPos)
                {
                    bool hasPath = NavMesh.CalculatePath(hitResultOne.position, hitResultTwo.position, RuntimePathfinding.areaMaskBakeLink, _navmeshPath);
                    if (true)
                    {
                        NavmeshTileLink link = linkPool.GetObject<NavmeshTileLink>();
                        link.Init();

                        //tileFrom--Link-- > tileTo
                        if (tileFrom.ContainsPosition2D(hitResultOne.position))
                        {
                            link.SetLinkPoint(hitResultOne.position, hitResultTwo.position);
                        }
                        else
                        {
                            link.SetLinkPoint(hitResultTwo.position, hitResultOne.position);
                        }

                        tileFrom.AddNextNavmeshLink(link);
                        //Debug.Log($"Create link for {tileFrom} -> {tileTo}");
                        //link.gameObject.name = $"Link {tileFrom}--->{tileTo}";
                        linkCount++;
                    }
                }
            }

            if (linkCount == 0)
            {
                Debug.LogError("No link generate -> " + tileFrom +" " + tileTo);
            }
        }

    }
}