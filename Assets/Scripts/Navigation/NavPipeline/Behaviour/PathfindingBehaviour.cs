using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.AI;

/// <summary>
/// 计算出对象的起点到终点的寻路路径
/// </summary>
public class PathfindingBehaviour : NavigationBehaviour
{
    private NavMeshAgent navMeshAgent;

    public PathfindingBehaviour()
    {
        navMeshAgent = new GameObject("NavMeshAgent").AddComponent<NavMeshAgent>();

        navMeshAgent.updatePosition = false;
        navMeshAgent.gameObject.transform.SetParent(NavManager.instance.transform,false);
        navMeshAgent.radius = 3f;
    }

    public override void ProcessStep()
    {
        if (navMeshAgent.hasPath && navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            navHandleData.wayPointList = navMeshAgent.path.corners.ToList();
            Debug.Log("[PathfindingBehaviour] 得到路径");
            for (int i = 0; i < navHandleData.wayPointList.Count; i++)
            {
                Utility.DrawGreenUpLine(navHandleData.wayPointList[i],5f);
            }
        }
        else  
        {
            navMeshAgent.Warp(navHandleData.startPosition);
            navMeshAgent.destination = navHandleData.destination;
        }
    }

    public override bool CanExecuteProcessStep()
    {
        if (navHandleData.wayPointList != null)
        {
            navHandleData.UpdateWayPointIndex();
            navHandleData.realVelocity = navHandleData.GetPathfindingVelocity();
            // 已经有路径
            return false;
        }
        else
        {
            return true;
        }
    }
}
