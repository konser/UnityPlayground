using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu]
public class MapBakeConfig : ScriptableObject
{
    public Vector3 mapSize;
    public Vector3 sectorSize;
    public Vector3 tileSize;
    public float linkBakeWidth;
    public LayerMask collectLayers;
    public string mapInfoFileSaveName = "MapInfo_";
    [Range(0,80)]
    public float maxAngleAllowed;

    public bool bakeAllSector;
    public List<Vector2Int> sectorIndexList;
}
