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
        private Vector3[] _startPosArray;
        private Vector3[] _endPosArray;
        private int _sampleCount;
        private float _offset;
        private Vector3 _bakeAreaCenter;
        private NavMeshPath _navmeshPath;
        public void Init(PathfindingSetting setting)
        {
            _surface = gameObject.GetComponent<NavMeshSurface>();
            if (!_surface)
            {
                _surface = gameObject.AddComponent<NavMeshSurface>();
            }
            _surface.size = new Vector3(setting.tileSize,setting.tileSize*2.0f,setting.bakeLinkAreaWidth);
            _surface.center = Vector3.zero;
            _surface.collectObjects = CollectObjects.Volume;
            _surface.defaultArea = RuntimePathfinding.areaBakeLink;
            _sampleCount = setting.linkBakeSampleCount;
            _navmeshPath = new NavMeshPath();
            _startPosArray = new Vector3[_sampleCount];
            _endPosArray = new Vector3[_sampleCount];
            _offset = _surface.size.z * 0.25f;
        }

        public void BakeTileLink(NavmeshTile tileOne, NavmeshTile tileTwo,ObjectPool linkPool)
        {
            if (!tileOne || !tileTwo)
            {
                Debug.LogError("BakeLinkArea : Tile is null");
                return;
            }


            _bakeAreaCenter = 0.5f * (tileOne.tileCenterPos + tileTwo.tileCenterPos);
            _bakeAreaCenter.y = Utility.GetTerrainHeight(_bakeAreaCenter);
            this.transform.position = _bakeAreaCenter;
            _surface.BuildNavMesh();
            int dx = tileOne.tileCoordX - tileTwo.tileCoordX;
            int dz = tileOne.tileCoordZ - tileTwo.tileCoordZ;
            SamplePosition(dx,dz);
            GenerateTileLink(linkPool,ref tileOne,ref tileTwo);
        }

        // 在接缝两侧分别采样处于navmesh内的位置点
        private void SamplePosition(int dx,int dz)
        {
            Vector3 startPos = _bakeAreaCenter;
            float step = _surface.size.x / _sampleCount;
            //    Tile | Tile
            if (dx != 0 && dz == 0)
            {
                this.transform.forward = Vector3.right;
                startPos.z -= _surface.size.x * 0.5f;

                for (int i = 0; i < _sampleCount; i++)
                {
                    _startPosArray[i] = new Vector3(startPos.x + _offset, 0, startPos.z + step);
                    _startPosArray[i].y = Utility.GetTerrainHeight(_startPosArray[i]);
                    _endPosArray[i] = new Vector3(startPos.x - _offset, 0, startPos.z + step);
                    _endPosArray[i].y = Utility.GetTerrainHeight(_endPosArray[i]);
                    startPos.z += step;
                }
            }
            //  Tile
            //  ----
            //  Tile
            else if (dz != 0 && dx == 0)
            {
                this.transform.forward = Vector3.forward;
                startPos.x -= _surface.size.x * 0.5f;
                for (int i = 0; i < _sampleCount; i++)
                {
                    _startPosArray[i] = new Vector3(startPos.x + step, 0, startPos.z + _offset);
                    _startPosArray[i].y = Utility.GetTerrainHeight(_startPosArray[i]);
                    _endPosArray[i] = new Vector3(startPos.x + step, 0, startPos.z - _offset);
                    _endPosArray[i].y = Utility.GetTerrainHeight(_endPosArray[i]);
                    startPos.x += step;
                }
            }

        }

        private void GenerateTileLink(ObjectPool linkPool,ref NavmeshTile tileOne,ref NavmeshTile tileTwo)
        {
            for (int i = 0; i < _sampleCount; i++)
            {
                NavMeshHit hitResOne;
                NavMeshHit hitResTwo;
                bool hitOne = NavMesh.SamplePosition(_startPosArray[i], out hitResOne, 5f, RuntimePathfinding.areaMaskBakeLink);
                bool hitTwo = NavMesh.SamplePosition(_endPosArray[i], out hitResTwo, 5f, RuntimePathfinding.areaMaskBakeLink);
                if (hitOne && hitTwo)
                {
                    bool hasPath = NavMesh.CalculatePath(hitResOne.position, hitResTwo.position, RuntimePathfinding.areaMaskBakeLink, _navmeshPath);
                    if (hasPath)
                    {
                        NavmeshTileLink link = linkPool.GetObject<NavmeshTileLink>();
                        link.Init();
                        // tileTwo ——> tileOne link由two指向one
                        if (tileOne.sequenceIndex > tileTwo.sequenceIndex)
                        {
                            // 判断哪个位置是起始点，哪个位置是终点
                            if (tileTwo.ContainsPosition2D(hitResOne.position))
                            {
                                link.SetLinkPoint(hitResOne.position, hitResTwo.position);
                            }
                            else
                            {
                                link.SetLinkPoint(hitResTwo.position, hitResOne.position);
                            }
                            tileTwo.AddNextNavmeshLink(link);
                            Debug.Log($"Create link for {tileTwo} -> {tileOne}");
                            link.gameObject.name = $"Link {tileTwo} -> {tileOne}";
                        }
                        // tileOne ——> tileTwo
                        else
                        {
                            if (tileOne.ContainsPosition2D(hitResOne.position))
                            {
                                link.SetLinkPoint(hitResOne.position, hitResTwo.position);
                            }
                            else
                            {
                                link.SetLinkPoint(hitResTwo.position, hitResOne.position);
                            }
                            tileOne.AddNextNavmeshLink(link);
                            Debug.Log($"Create link for {tileOne} -> {tileTwo}");
                            link.gameObject.name = $"Link {tileOne}--->{tileTwo}";
                        }
                    }
                }
            }
        }

    }
}