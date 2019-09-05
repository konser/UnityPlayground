using System;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class ParticleDeposition : TerrainBase
{
    public int movements;


    private void Start()
    {
    }

    public Vector3 BrownianMovement(Vector3 position)
    {
        int index = Random.Range(0, 4);
        return Vector3.zero;
    }

}
