using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class PathfindingTest : MonoBehaviour
{
    public NavMeshAgent agent;
    public GameObject dest;
    public float preCalculateRadius = 64.0f;
    private NavMeshPath path;
    public NavmeshTile currentTile;
    private WaitForEndOfFrame waitForEndFrame = new WaitForEndOfFrame();
    
    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            GetPathFromArea("Walkable");
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            Vector2Int index = GetTileIndex(agent.transform.position);
            currentTile.UseTile(index.x,index.y);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            GetPathFromArea("Detail");
        }

    }

    public Vector3 GetNearestValidNavmeshPosition()
    {
        return Vector3.zero;
    }

    public Vector3[] GetPathFromArea(string areaName)
    {
        if (path == null)
        {
            path = new NavMeshPath();
        }
        path.ClearCorners();
        NavMesh.CalculatePath(agent.transform.position, dest.transform.position, 1 << NavMesh.GetAreaFromName(areaName), path);
        if (path.status != NavMeshPathStatus.PathInvalid)
        {
            if (path.corners.Length == 1)
            {
                return null;
            }
            for (int i = 0; i < path.corners.Length; i++)
            {
                Debug.DrawLine(path.corners[i], path.corners[i] + Vector3.up * 100f, Color.red, 5.0f);
            }
        }
        else
        {
            Debug.Log($"Path Invalid {path.corners.Length}");
        }
        return path.corners;
    }

    public Vector2Int GetTileIndex(Vector3 agentPos)
    {
        int x = (int)(agentPos.x /currentTile.size);
        int z = (int)(agentPos.z / currentTile.size);
        return new Vector2Int(x,z);
    }

    public void SetNeibourTile()
    {

    }

    public IEnumerator PreCalculateTileAlongPath()
    {
            
        yield return waitForEndFrame;
    }


    private void OnDrawGizmos()
    {
        
    }
}
