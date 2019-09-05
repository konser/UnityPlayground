using UnityEngine;
using System.Collections;

public class CircleHill : TerrainBase
{
    public int numCircle;

    public float radius;

    public float circleHeightIncrement;

    // Use this for initialization
    void Start()
    {
        heightMap = new float[width,length];
        int randomX;
        int randomZ;
        for (int i = 0; i < numCircle; i++)
        {
            randomX = Random.Range(0, width);
            randomZ = Random.Range(0, length);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    float d = (randomX - x) * (randomX - x) + (randomZ - z) * (randomZ - z);
                    if (d < radius * radius)
                    {
                        float a = circleHeightIncrement * 0.5f * (1 + Mathf.Cos(Mathf.PI * d / (radius * radius)));
                        heightMap[x, z] = Mathf.Min(heightMap[x, z] + a, maxHeight);
                    }
                }
            }
        }
        SetColor();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
