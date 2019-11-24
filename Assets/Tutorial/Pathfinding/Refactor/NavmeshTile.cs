using UnityEngine;
using System.Collections;
using UnityEngine.AI;
namespace RuntimePathfinding
{
    /// <summary>
    /// 运行时计算的部分区域详细Navmesh
    /// </summary>
    public class NavmeshTile : PooledObject
    {
        private NavMeshSurface _surface;
        public void Init(PathfindingSetting setting)
        {
            // 设置NavmeshSurface的参数
            _surface = gameObject.GetComponent<NavMeshSurface>();
            if(_surface == null)
            {
                _surface = gameObject.AddComponent<NavMeshSurface>();
            }
            _surface.collectObjects = CollectObjects.Volume;
            _surface.defaultArea = NavMesh.GetAreaFromName("Detail");
        }
    }
}