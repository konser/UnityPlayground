using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;

public class SampleTexture : MonoBehaviour
{
    public string fileName;
    private VoxelBuffer buffer;
    public int istart;
    public int jstart;
    public int kstart;

    public int size = 0;
    private void Start()
    {
        buffer = GetData();
        Debug.Log(buffer.data.Length);
    }
    public VoxelBuffer GetData()
    {
        var b = File.ReadAllBytes("Assets/Resources/VoxelData/" + fileName + ".bytes");
        BinaryFormatter bf = new BinaryFormatter();
        VoxelBuffer buffer = bf.Deserialize(new MemoryStream(b)) as VoxelBuffer;
        return buffer;
    }

    private void Update()
    {
        if (buffer != null)
        {
            Shader.SetGlobalFloatArray("_VoxelBuffer",buffer.data);
        }
    }
    
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (buffer != null && Application.isPlaying)
        {
            for (int k = kstart; k < kstart+ size; k++)
            {
                for (int j = jstart; j < jstart+ size; j++)
                {
                    for (int i = istart; i < istart+ size; i++)
                    {
                        float val = buffer[i, j, k];
                        if (val > float.Epsilon)
                        {
                            Handles.color = new Color(val, val, val, 1f);
                            Handles.DrawWireCube(new Vector3((float)i / 5, (float)j / 5, (float)k / 5), new Vector3(0.2f, 0.2f, 0.2f));
                        }
                    }
                }
            }
        }
#endif
    }
}
