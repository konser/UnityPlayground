using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace RuntimePathfinding
{
    /// <summary>
    /// 相邻的Navmesh连接点
    /// </summary>
    public class NavmeshTileLink : PooledObject
    {
        private NavMeshLink _navmeshLink;

        public Vector3 linkStartPos
        {
            get { return _linkStartPos; }
        }

        private Vector3 _linkStartPos;

        public Vector3 linkEndPos
        {
            get { return _linkEndPos; }
        }

        private Vector3 _linkEndPos;

        public void Init()
        {
            _navmeshLink = gameObject.GetComponent<NavMeshLink>();
            if (_navmeshLink == null)
            {
                _navmeshLink = gameObject.AddComponent<NavMeshLink>();
            }
            _navmeshLink.area = RuntimePathfinding.areaDetail;
            _navmeshLink.bidirectional = true;
            _navmeshLink.autoUpdate = true;
        }

        public void SetLinkPoint(Vector3 startPos, Vector3 endPos)
        {
            _linkStartPos = startPos;
            _linkEndPos = endPos;
            Vector3 center = 0.5f * (startPos + endPos);
            Vector3 startOffset = startPos - center;
            Vector3 endOffset = endPos - center;
            this.transform.position = center;
            _navmeshLink.startPoint = startOffset;
            _navmeshLink.endPoint = endOffset;
        }

        public void EnableLink()
        {
            _navmeshLink.enabled = true;
        }

        public void DisableLink()
        {
            _navmeshLink.enabled = false;
        }
    }
}