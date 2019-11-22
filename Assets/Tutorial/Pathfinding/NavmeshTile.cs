using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class NavmeshTile : MonoBehaviour
{
    public NavMeshSurface surface;
    public bool isActive;
    public float size = 32.0f;
    private BoxCollider boxCollider;
    public int tileIndexX;
    public int tileIndexZ;
    public Action<int,int> OnTileExit;
    public Action<int, int> OnTileEnter;
    private int areaMask;
    private Vector2Int _tileIndex;

    public Vector2Int tileIndex
    {
        get
        {
            _tileIndex.x = tileIndexX;
            _tileIndex.y = tileIndexZ;
            return _tileIndex;
        }
    }
    private void Awake()
    {
        surface = this.GetComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        // 比实际网格宽一点点
        surface.size = new Vector3(size,256f,size);
        surface.center = Vector3.zero;
        boxCollider = this.gameObject.AddComponent<BoxCollider>();
        boxCollider.center = Vector3.zero;
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector3(size,size,size);
        areaMask = 1 << NavMesh.GetAreaFromName("Detail");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            surface.BuildNavMesh();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            OnTileEnter?.Invoke(tileIndexX,tileIndexZ);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            OnTileExit?.Invoke(tileIndexX, tileIndexZ);
        }
    }

    public Vector3 GetNavmeshPosNearTileCenter()
    {
        float dist = 16.0f;
        NavMeshHit hit = default;
        bool hasHit = false;
        for (int i = 0; i < 5; i++)
        {
            hasHit = NavMesh.SamplePosition(TileCenterPos(), out hit, dist, areaMask);
            if (hasHit)
            {
                break;
            }
            dist *= 2.0f;
        }
        if (!hasHit)
        {
            Debug.LogError($"Tile ({tileIndexX},{tileIndexZ}) : 没找到寻路点");
        }
        return hit.position;
    }

    public bool ContainsPosition(Vector3 pos)
    {

        int x = (int)(pos.x / size);
        int z = (int)(pos.z / size);
        if (x == tileIndexX && z == tileIndexZ)
        {
            return true;
        }
        return false;
    }

    public Vector3 TileCenterPos()
    {
        Vector3 pos = new Vector3(tileIndexX * size + 0.5f * size, 0, tileIndexZ * size + 0.5f * size);
        float height = Utility.GetTerrainHeight(pos);
        pos.y = height;
        return pos;
    }

    public void EnableTile(int x,int z)
    {
        isActive = true;
        boxCollider.enabled = true;
        surface.enabled = true;
        tileIndexX = x;
        tileIndexZ = z;
        this.transform.position = TileCenterPos();
        surface.BuildNavMesh();
        Debug.Log($"Build navmesh at ({tileIndexX},{tileIndexZ})");
    }


    public void DisableTile()
    {
        isActive = false;
        surface.enabled = false;
        boxCollider.enabled = false;
    }

    private void OnDrawGizmos()
    {
        if (boxCollider != null)
        {
            if (isActive)
            {
                Handles.color = Color.green;
            }
            else
            {
                Handles.color = Color.red;
            }
            Handles.DrawWireCube(transform.position,new Vector3(boxCollider.size.x,0.01f,boxCollider.size.z));
        }
    }

}
