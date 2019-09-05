using UnityEngine;
using Random = UnityEngine.Random;

public class LimitedRandom : TerrainBase
{

    public float heightDifference = 64f;

    void Start()
    {
        heightMap = new float[width, length];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                float h;
                float averageHeight;
                if (x != 0 && z != 0)
                {
                    averageHeight = (heightMap[x - 1, z] + heightMap[x, z - 1]) * 0.5f;
                }else if (x != 0 && z == 0)
                {
                    averageHeight = heightMap[x - 1, z];
                }
                else
                {
                    averageHeight = Random.Range(0, maxHeight);
                }
                h = averageHeight + heightDifference * (Random.Range(-0.5f, 0.5f));
                heightMap[x, z] = Mathf.Min(h, maxHeight);
                SetColor(x,z);
            }
        }

    }
}
