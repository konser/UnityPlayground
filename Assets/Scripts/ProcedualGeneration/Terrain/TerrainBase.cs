using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

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

    public bool saveToPNG;
    public void SetColor()
    {
        if (saveToPNG == false)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    SetColor(i, j);
                }
            }
        }
        else
        {
            Save();
        }
    }

    [Button]
    public void Save()
    {
        Texture2D texture = new Texture2D(width,length,TextureFormat.ARGB32,false);
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                float ratio = heightMap[x, z] / maxHeight;
                color = new Color(ratio, ratio, ratio, 1f);
                texture.SetPixel(x,z,color);
            }
        }
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + $"/test_{Random.Range(1,9999)}.png",bytes);
        Debug.Log("Saved! " + bytes.Length);
    }

}
