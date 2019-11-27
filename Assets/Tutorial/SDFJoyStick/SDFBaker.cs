using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class SDFBaker : MonoBehaviour
{
    private float unitLength = 1;
    public int textureSize = 256;
    public int width = 100;
    public int height = 100;
    public bool[,] obstacles;
    public float[,] edtDistance;

    public float maxDist
    {
        get
        {
            return Mathf.Sqrt((width*width)+(height*height));
        }
    }


    private void OnValidate()
    {
    }

    [ContextMenu("RenderToTexture")]
    public void RenderToTexture()
    {
        unitLength = (float)width / (float)textureSize;
        obstacles = new bool[textureSize, textureSize];
        edtDistance = new float[textureSize, textureSize];
        // find obstacles
        RayCast();
        // edt
        CaculateEDT();
        Save();

    }

    private void RayCast()
    {
        for (int i = 0; i < textureSize; i++)
        {
            float x = i * unitLength;
            //Debug.Log(unitLength +"" + i +" " +x);
            for (int j = 0; j < textureSize; j++)
            {
                float z = j * unitLength;
                Vector3 pos = new Vector3(x + unitLength / 2f, 10, z + unitLength / 2f);
                bool t = Physics.Raycast(pos, Vector3.down, 20f, 1 << LayerMask.NameToLayer("Obstacle"));
                obstacles[i, j] = t;
                Debug.DrawLine(pos,pos+Vector3.down*20,Color.red,5f);
            }
        }
    }

    private void CaculateEDT()
    {
#if UNITY_EDITOR
        for (int i = 0;i < textureSize; i++)
        {
            for (int j = 0; j < textureSize; j++)
            {
                if (obstacles[i, j])
                {
                    edtDistance[i, j] = 0f;
                }
                else
                {
                    edtDistance[i, j] = NearestDistance(i,j);
                }
            }
            EditorUtility.DisplayProgressBar("Bake sdf","Baking....",i/(float)textureSize);
        }
#endif
    }

    private float NearestDistance(int gx,int gz)
    {
        float min = Single.MaxValue;
        for (int i = 0; i < textureSize;i++)
        {
            float dx = (i * unitLength - gx * unitLength);
            for (int j = 0; j < textureSize;j++)
            {
                if (obstacles[i, j])
                {
                    float dz = (j * unitLength - gz * unitLength);
                    float dist = Mathf.Sqrt(dx*dx+dz*dz);
                    if (dist < min)
                    {
                        min = dist;
                    }
                }
                if (min <= unitLength)
                {
                    return min;
                }
            }
        }
        return min;
    }

    public void Save()
    {
#if UNITY_EDITOR
        Texture2D texture = new Texture2D(textureSize,textureSize, TextureFormat.ARGB32, false);
        for (int i = 0; i < textureSize; i++)
        {
            for (int j = 0; j < textureSize; j++)
            {
                float c = edtDistance[i, j] / maxDist;
                Color color = new Color(c, c, c, 1);
                texture.SetPixel(i, j, color);
            }
        }
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + $"/SDF_{Random.Range(1, 9999)}.png", bytes);
        Debug.Log("Saved! " + bytes.Length);
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
#endif
    }

    private Texture2D sdfTexture;
    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        return;
        if (sdfTexture == null)
        {
            sdfTexture = Resources.Load<Texture2D>("SDF");
        }
        unitLength = (float) width / textureSize;
        for (int x = 0; x < 100; x++)
        {
            for (int z = 0; z < 100; z++)
            {
                float distance = sdfTexture.GetPixel(x, z).r;
                Handles.Label(new Vector3(x*unitLength,0f,z*unitLength),distance.ToString("0.0"));
            }
        }
#endif
    }
}
