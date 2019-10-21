using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ProcueduralTexture : MonoBehaviour
{
    public Shader shader;
    public Texture2D pseduoNumTexture;
    public float PseduoRNG(int x)
    {
        x = (x << 13) * x;
        int Prime1 = 15731;
        int Prime2 = 789221;
        int Prime3 = 1376312589;
        return (float)(1.0f - ((x * (x * x * Prime1 + Prime2) + Prime3)
                        & 0x7fffffff) / 1073741824.0);
    }
    
    [ContextMenu("Test")]
    public void Test()
    {
        Debug.Log(pseduoNumTexture.GetPixel(12,31));
    }

#if UNITY_EDITOR
    [ContextMenu("RenderToTexture")]
    public void RenderToTexture()
    {
        int width = 512;
        int height = 512;
        int k = 0;
        Texture2D texture = new Texture2D(width, height,TextureFormat.RGBA32, false);
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float[] array = new float[4];
                for (int i = 0; i < 4; i++,k++)
                {
                    float n = PseduoRNG(k);
                    n = (n + 1.0f) / 2f;
                    if (n >= 1.0f || n <= 0.0f)
                    {
                        Debug.Log(n);
                    }
                    array[i] = n;
                }
                Color color = new Color(array[0],array[1],array[2],array[3]);
                texture.SetPixel(x,z,color);
                k++;
            }
        }
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + $"/PseduoNum_{Random.Range(1, 9999)}.png", bytes);
        Debug.Log("Saved! " + bytes.Length);
        UnityEditor.AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }
#endif
}
