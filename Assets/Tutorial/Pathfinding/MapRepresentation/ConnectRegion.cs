using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RuntimePathfinding
{
    public class ConnectRegion
    {
        public List<LinkNode> nodes = new List<LinkNode>();
        public Vector3 seedPosition;
        public ConnectRegion(Vector3 seedPos)
        {
            seedPosition = seedPos;
        }


    }
}