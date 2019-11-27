using UnityEngine;
public class SimpleRandom : TerrainBase
{
    void Start()
    {
        heightMap = new float[width, length];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                heightMap[x, z] = Random.Range(0, maxHeight);
                SetColor(x,z);
            }
        }

    }
}
