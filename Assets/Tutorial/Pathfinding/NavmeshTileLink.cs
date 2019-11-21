using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class NavmeshTileLink
{
    public NavmeshTile tileOne;
    public NavmeshTile tileTwo;
    public NavMeshSurface helperSurface;
    public List<NavMeshLink> navmeshLinks = new List<NavMeshLink>();

    public void GenerateNavmeshLink(NavmeshTile tile1, NavmeshTile tile2)
    {
        if (tile1 == null || tile2 == null)
        {
            return;
        }
        tileOne = tile1;
        tileTwo = tile2;

    }

    // 在接缝两侧分别采样处于navmesh内的位置点
    private void SamplePosition(NavmeshTile tile,ref Vector3[] positions)
    {

    }

    // 检查联通性 生成数对连接点
    private void CheckConnectivity(ref Vector3[] tileOnePosArray, ref Vector3[] tileTwoPosArray)
    {

    }

    // 选取2-3对连接点，生成navmeshLink
    private void CreateNavmeshLink()
    {

    }
}
