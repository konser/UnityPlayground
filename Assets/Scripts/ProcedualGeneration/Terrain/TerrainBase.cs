using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainBase : MonoBehaviour
{
    public int width = 256;
    public int length = 256;
    public float maxHeight = 256f;
    public float[,] heightMap;
    private GameObject[,] cubes;
    private Color color;
    private GameObject prefab;
    private MaterialPropertyBlock matBlock;
    private Renderer render;
    public void SetColor(int x, int z)
    {
        if (matBlock == null)
        {
            matBlock = new MaterialPropertyBlock();
        }
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Cube");
        }
        if (cubes == null)
        {
            cubes = new GameObject[width,length];
        }

        if (cubes[x, z] == null)
        {
            cubes[x, z] = Instantiate(prefab);
        }
        float ratio = heightMap[x, z] / maxHeight;
        color = new Color(ratio, ratio, ratio, 1f);
        cubes[x, z].transform.localScale = new Vector3(1, heightMap[x, z], 1);
        cubes[x, z].transform.position = new Vector3(x, heightMap[x, z] / 2f - 0.5f, z);

        render = cubes[x, z].transform.GetComponent<Renderer>();
        render.GetPropertyBlock(matBlock);
        matBlock.SetColor("_MainColor", color);
        render.SetPropertyBlock(matBlock);
    }
}
