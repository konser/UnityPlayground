using UnityEngine;
using System.Collections;

[CreateAssetMenu]
public class MapBakeConfig : ScriptableObject
{
    public Vector3 mapSize;
    public Vector3 tileSize;
    public float linkBakeWidth;
    public LayerMask collectLayers;

    [Range(0,80)]
    public float maxAngleAllowed;
}
