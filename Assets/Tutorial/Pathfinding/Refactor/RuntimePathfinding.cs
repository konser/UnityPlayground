using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace RuntimePathfinding
{
    public struct PathfindingRequest
    {
        public Vector3 destination;
    }

    public class RuntimePathfinding : MonoBehaviour
    {
        static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();

        /// <summary>
        /// 寻路设置
        /// </summary>
        public PathfindingSetting setting;

        /// <summary>
        /// 寻路组件是否初始化完成
        /// </summary>
        public bool isInited
        {
            get
            {
                return _initialized;
            }
        }

        private const int FRAME_HANDLE_COUNT = 3;
        private bool _initialized;
        private ObjectPool _tilePool;
        private ObjectPool _tileLinkPool;
        private Queue<PathfindingRequest> _requestQueue;

        public void Initialize()
        {
            if(setting.tilePrefab == null || setting.tileLinkPrefab == null)
            {
                Debug.LogError("初始化设置中prefab为空");
                return;
            }
            _requestQueue = new Queue<PathfindingRequest>();
            _tilePool = ObjectPool.GetPool(setting.tilePrefab);
            _tileLinkPool = ObjectPool.GetPool(setting.tileLinkPrefab);
            _initialized = true;
        }

        private void Update()
        {
            if (!isInited)
            {
                return;
            }
            int count = 0;
            while(_requestQueue.Count != 0 && count < FRAME_HANDLE_COUNT)
            {
                PathfindingRequest req = _requestQueue.Dequeue();
                PathfindingCoroutine pathfindingCoroutine = new PathfindingCoroutine(ProcessRequest(req));
                StartCoroutine(pathfindingCoroutine);
                count++;
            }
        }

        public void RequestPathfinding(PathfindingRequest request)
        {
            _requestQueue.Enqueue(request);
        }

        #region Pathfinding Request Process
        private IEnumerator ProcessRequest(PathfindingRequest request)
        {
            Vector3 destination = request.destination;
            Vector3[] abstractPathCorners = new Vector3[36];
            GenerateAbstractPath();
            yield return _endOfFrame;
            MarkPassedNavmeshTile();
            yield return _endOfFrame;
            while (!HasReachedDestination())
            {
                GenerateNavmeshTile();
                GenerateTileLink();
                yield return _endOfFrame;
                GenerateDetailedPath();
                yield return _endOfFrame;
                TryMoveAgent();
            }
            yield return _endOfFrame;
        }

        // ---------------生成粗略路径-----------------
        private void GenerateAbstractPath()
        {

        }

        // --------------标记粗略路径经过的区块------------
        private void MarkPassedNavmeshTile()
        {

        }

        // --------------检查是否到达目标----------------
        private bool HasReachedDestination()
        {
            return false;
        }

        // --------------生成详细Navmesh区块--------------
        private void GenerateNavmeshTile()
        {

        }

        // ---------------生成区块间的连接点----------------
        private void GenerateTileLink()
        {

        }

        // ----------------生成经过区块的详细路径--------------------
        private void GenerateDetailedPath()
        {

        }

        // ----------------根据寻路信息移动Agent---------------------
        private void TryMoveAgent()
        {

        }
        #endregion
    }
}