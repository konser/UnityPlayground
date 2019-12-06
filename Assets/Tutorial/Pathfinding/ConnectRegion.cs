using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RuntimePathfinding
{
    public class ConnectRegion
    {
        public List<CNode> nodes = new List<CNode>();
        public Vector3 seedPosition;
        public ConnectRegion(Vector3 seedPos)
        {
            seedPosition = seedPos;
        }


    }
}