using UnityEngine;
using System.Collections;

public class SnapToTerrain : MonoBehaviour
{
    void Update()
    {
        Vector3 pos = this.transform.position;
        float y = Utility.GetTerrainHeight(pos);
        this.transform.position = new Vector3(pos.x,y, pos.z);
    }
}
