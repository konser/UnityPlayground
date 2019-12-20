using System;
using System.Collections.Generic;
using DataStructure;
using RuntimePathfinding;

[System.Serializable]
public class NavigationNode : GraphNode<LinkPoint>
{
    public NavigationNode previousNode;
    public bool isVisited;
    public float costToNode;
    public NavigationNode(LinkPoint value, IEnumerable<GraphNode<LinkPoint>> neibours = null) : base(value, neibours)
    {
        ResetPathStatus();
    }
    public NavigationNode(LinkPoint value, GraphNode<LinkPoint>[] neibours) : base(value, neibours)
    {
        ResetPathStatus();
    }

    public void ResetPathStatus()
    {
        previousNode = null;
        isVisited = false;
        costToNode = Single.MaxValue;
    }
}
