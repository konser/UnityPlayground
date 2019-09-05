using UnityEngine;
using System.Collections;

public class FaultLine : TerrainBase
{
    public bool showProcessing;
    public int numFaultLine;
    public float faultChange;
    // Use this for initialization
    IEnumerator Start()
    {
        InitFlat();
        for (int i = 0; i < numFaultLine; i++)
        {
            if (showProcessing)
            {
                yield return new WaitForSeconds(0.2f);
            }
            int x0 = Random.Range(0, width);
            int z0 = Random.Range(0, length);
            int x1 = Random.Range(0, width);
            int z1 = Random.Range(0, length);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    // vec v = p1 - p0 , vec w = p - p0, result = v cross w
                    if ((x1 - x0) * (z - z0) - (z1 - z0) * (x - x0) > 0)
                    {
                        heightMap[x, z] = Mathf.Min(heightMap[x, z] + faultChange, maxHeight);
                    }
                    else
                    {
                        heightMap[x, z] = Mathf.Max(heightMap[x, z] - faultChange, 0);
                    }
                    SetColor(x, z);
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                SetColor(x,z);
            }
        }
    }

    public void InitFlat()
    {
        heightMap = new float[width,length];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                heightMap[x, z] = 0.5f * maxHeight;
            }
        }
    }

}
