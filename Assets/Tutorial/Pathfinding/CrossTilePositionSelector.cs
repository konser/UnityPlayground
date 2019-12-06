using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RuntimePathfinding
{
    public class CrossTilePositionSelector
    {
        private List<CNode> _tempBuffer = new List<CNode>(20);
        private NavMeshPath _resultCachePath;
        public CrossTilePositionSelector()
        {
            _resultCachePath = new NavMeshPath();
        }

        public bool GetValidPosition(Vector3 currentPos,List<CNode> currentTileNodes,List<CNode> nextTileNodes,out CNode targetNode)
        {
            /*
             * +--------------------------------------------------------++-----------|-----------------------------------------------++---------------------------------------------------------------+
|                                                        ||           |找到当前穿越点对应点中与下个Tile中的穿越点的连通的几个 ||                                                               |
|                                               Peer  <------> Node   |                                               ||                                                             |
|                                                        ||           |                                   /- Peer <-------->  Node <-                                                  |
|                                                        ||           |                                /--            ||              \--    从所处位置可以到达的当前Tile中的穿越点           |
|                                                        ||           |                             /--               ||                \--                                            |
|                                                        ||           |                          /--                  ||                   \-                                          |
|                                                        ||           |                        /-                     ||   最终选择的点        \--                                       |
|                                                        ||           |                     /--                       ||    +-----+             \--                                    |
|                                                        ||           |                  /--               /-Peer <-------->| Node|--              \--                                 |
|                                               Peer  <------> Node   |               /--               /--           ||    +-----+  \-----           \--                              |
|                                                        ||           |            /--                /-              ||                   \-----        \-                            |
|                                                        ||           |         /--               +/------------------+|                         \-----    \--                         |
|                                                        ||------------      /--                /--                   ||                               \----- \--                      |
|                                                        ||                /-                 /-  |                   ||                                     \-- \-CurrentPos          |
|                                                        ||             /--                /--    |                   ||                                                               |
|                                                        ||          /--                /--       |          Peer <-------->  Node                                                     |
|                                              Peer  <-------> Node<-                 /-          |                   ||                                                               |
|                                                        ||                        /--            |                   ||                                                               |
|                                                        ||                     /--               |                   ||               这两个没有可行路径                                 |
|                                                        ||                   /-                  |                   ||                                                               |
|                                                        ||                /--                    |                   ||                                                               |
|                                                        ||             /--                       |          Peer <-------->  Node                                                     |
|                                                        ||           /-                          |                   ||                                                               |
|                                               Peer <-------->Node <-                            |                   ||                                                               |
|                                                        ||                                       |                   ||                                                               |
|                                                        ||                                       |                   ||                                                               |
+--------------------------------------------------------++---------------------------------------|-------------------++---------------------------------------------------------------+
             */
            // 从所处位置可以到达的当前Tile中的穿越点
            _tempBuffer.Clear();
            for (int i = 0; i < currentTileNodes.Count; i++)
            {
                if (HasPath(currentPos, currentTileNodes[i].position))
                {
                    _tempBuffer.Add(currentTileNodes[i]);
                }
            }

            // 找到当前穿越点对应点(node.peer)中与下个Tile中的穿越点的连通的几个
            for (int i = _tempBuffer.Count-1; i >= 0; i--)
            {
                bool hasPath = false;
                for (int j = 0; j < nextTileNodes.Count; j++)
                {
                    if (HasPath(_tempBuffer[i].peer.position, nextTileNodes[j].position))
                    {
                        hasPath = true;
                    }
                }

                // 移除不连通的点
                if (hasPath == false)
                {
                    _tempBuffer.RemoveAt(i);
                }
            }

            targetNode = null;

            // 没有可行的穿越点
            if (_tempBuffer.Count == 0)
            {
                return false;
            }

            // 找到最近点 之后移动到这个点的peer即可到达下一个tile 
            float minDist = Single.MaxValue;
            for (int i = 0; i < _tempBuffer.Count; i++)
            {
                float d = (currentPos - _tempBuffer[i].position).sqrMagnitude;
                if (d < minDist)
                {
                    minDist = d;
                    targetNode = _tempBuffer[i];
                }
            }

            return true;
        }

        private bool HasPath(Vector3 startPos, Vector3 endPos)
        {
            NavMesh.CalculatePath(startPos, endPos, RuntimePathfinding.areaWalkable, _resultCachePath);
            if (_resultCachePath.status == NavMeshPathStatus.PathComplete)
            {
                return true;
            }
            return false;
        }
    }
}