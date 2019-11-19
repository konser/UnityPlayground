using UnityEngine;
using UnityEngine.AI;

public enum ENavmeshTileState
{
    Active,
    MarkedToBakeNavmesh,

}

public class NavmeshTile : MonoBehaviour
{
    public NavMeshSurface surface;
    public bool isActive;
    public float size = 32.0f;

    public Vector3 minPoint;

    public Vector3 maxPoint;

    private void Awake()
    {
        surface = this.GetComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.size = new Vector3(size,size*2,size);
        surface.center = Vector3.zero;
    }

    public void UseTile(int x,int z)
    {
        minPoint = new Vector3(x*size,0,z*size);
        maxPoint = minPoint + new Vector3(size, 0, size);
        Vector3 pos = new Vector3(x * size + 0.5f * size,0, z * size + 0.5f * size);
        float height = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.GetPosition().y;
        this.transform.position = new Vector3(pos.x,height,pos.z);

        surface.BuildNavMesh();
        //if (surface.navMeshData == null)
        //{
        //    surface.BuildNavMesh();
        //}
        //else
        //{
        //    surface.UpdateNavMesh(surface.navMeshData);
        //}
    }




}
