using System;
using UnityEngine;
using System.Collections;

public class SnapToTerrain : MonoBehaviour
{
    public bool alwaysUpdate;
    private void Start()
    {
        SetPos();
    }
    void Update()
    {
        if (alwaysUpdate)
        {
            SetPos();
        }
    }

    void SetPos()
    {
        Vector3 pos = this.transform.position;
        float y = Utility.GetTerrainHeight(pos);
        this.transform.position = new Vector3(pos.x, y, pos.z);
    }
}
