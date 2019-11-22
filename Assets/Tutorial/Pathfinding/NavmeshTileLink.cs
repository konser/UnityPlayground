using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;

public class TileLinkInfo
{
    private int length;
    private Vector3[] arrayOne;
    private Vector3[] arrayTwo;

    public TileLinkInfo(Vector3[] one, Vector3[] two)
    {
        length = one.Length;
        arrayOne = one;
        arrayTwo = two;
    }
    public Vector3[] GetOwnPosition(NavmeshTile tile)
    {
        if (length == 0)
        {
            Debug.Log("无联通点");
            return null;
        }
        if (tile.ContainsPosition(arrayOne[0]))
        {
            return arrayOne;
        }
        else
        {
            return arrayTwo;
        }
    }

    public void CreateNavmeshLink()
    {
        for (int i = 0; i < length; i++)
        {
            Vector3 center = (arrayOne[i] + arrayTwo[i]) * 0.5f;
            GameObject go = new GameObject();
            NavMeshLink link = go.AddComponent<NavMeshLink>();
            go.transform.position = center;
            link.startPoint = arrayOne[i] - center;
            link.endPoint = arrayTwo[i] - center;
            link.autoUpdate = true;
            link.area = NavMesh.GetAreaFromName("Detail");
            link.bidirectional = true;
        }
    }
}

public class NavmeshTileLink : MonoBehaviour
{
    public NavmeshTile tileOne;
    public NavmeshTile tileTwo;
    public NavMeshSurface helperSurface;
    public List<NavMeshLink> navmeshLinks = new List<NavMeshLink>();
    private Vector3[] arrayOne = new Vector3[SAMPLE_COUNT];
    private Vector3[] arrayTwo = new Vector3[SAMPLE_COUNT];
    private const int SAMPLE_COUNT = 10;
    private int areaMask;
    private Vector3 center;
    private int dx;
    private int dy;
    private float dist;
    [ContextMenu("Test")]
    public void Test()
    {
        GenerateNavmeshLink(tileOne,tileTwo);
    }

    public TileLinkInfo GenerateNavmeshLink(NavmeshTile tile1, NavmeshTile tile2)
    {
        if (tile1 == null || tile2 == null)
        {
            return null;
        }
        tileOne = tile1;
        tileTwo = tile2;
        helperSurface = this.GetComponent<NavMeshSurface>();
        areaMask = NavMesh.GetAreaFromName("BakeLink");
        center = tileOne.TileCenterPos() + tileTwo.TileCenterPos();
        center *= 0.5f;
        dx = tile1.tileIndexX - tile2.tileIndexX;
        dy = tile1.tileIndexZ - tile2.tileIndexZ;
        this.transform.position = center;
        center.y = Utility.GetTerrainHeight(center);
        helperSurface.BuildNavMesh();
        dist = helperSurface.size.z * 0.25f;

        SamplePosition();

        return CheckConnectivity();
    }

    // 在接缝两侧分别采样处于navmesh内的位置点
    private void SamplePosition()
    {
        Vector3 startPos = center;
        float step = helperSurface.size.x / SAMPLE_COUNT;
        float width = helperSurface.size.z;
        //    Tile | Tile
        if (dx != 0 && dy == 0)
        {
            this.transform.forward = Vector3.right;
            startPos.z -= helperSurface.size.x*0.5f;

            for (int i = 0; i < SAMPLE_COUNT; i++)
            {
                arrayOne[i] = new Vector3(startPos.x + dist, 0, startPos.z + step);
                arrayOne[i].y = Utility.GetTerrainHeight(arrayOne[i]);
                arrayTwo[i] = new Vector3(startPos.x - dist, 0, startPos.z + step);
                arrayTwo[i].y = Utility.GetTerrainHeight(arrayTwo[i]);
                startPos.z += step;
            }
        }
        //  Tile
        //  ----
        //  Tile
        else if(dy !=0 && dx == 0)
        {
            this.transform.forward = Vector3.forward;
            startPos.x -= helperSurface.size.x*0.5f;
            for (int i = 0; i < SAMPLE_COUNT; i++)
            {
                arrayOne[i] = new Vector3(startPos.x + step, 0, startPos.z + dist);
                arrayOne[i].y = Utility.GetTerrainHeight(arrayOne[i]);
                arrayTwo[i] = new Vector3(startPos.x + step, 0, startPos.z - dist);
                arrayTwo[i].y = Utility.GetTerrainHeight(arrayTwo[i]);
                startPos.x += step;
            }
        }

    }

    private List<Vector3> cachePosListOne = new List<Vector3>();
    private List<Vector3> cachePosListTwo = new List<Vector3>();
    // 检查联通性 生成数对连接点
    private TileLinkInfo CheckConnectivity()
    {
        Color one = Color.red;
        Color two = Color.magenta;
        NavMeshPath path = new NavMeshPath();
        int pairCount = 0;
        cachePosListOne.Clear();
        cachePosListTwo.Clear();
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            NavMeshHit hitResOne;
            NavMeshHit hitResTwo;
            bool hitOne = NavMesh.SamplePosition(arrayOne[i], out hitResOne, dist , areaMask);
            bool hitTwo = NavMesh.SamplePosition(arrayTwo[i], out hitResTwo, dist, areaMask);
            if (hitOne && hitTwo)
            {
                bool hasPath = NavMesh.CalculatePath(hitResOne.position, hitResTwo.position, areaMask, path);
                if (hasPath)
                {
                    cachePosListOne.Add(hitResOne.position);
                    cachePosListTwo.Add(hitResTwo.position);
                    pairCount++;
                }
            }
        }

        //for (int i = 0; i < cachePosListOne.Count; i++)
        //{
        //    Debug.DrawLine(cachePosListOne[i], cachePosListOne[i] + Vector3.up * 30f, one,10.0f);
        //    Debug.DrawLine(cachePosListTwo[i], cachePosListTwo[i] + Vector3.up * 30f, two,10.0f);
        //}
        return new TileLinkInfo(cachePosListOne.ToArray(),cachePosListTwo.ToArray());
    }
}
