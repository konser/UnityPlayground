using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MonoTest:MonoBehaviour
{
    public uint x, y, z;
    [ContextMenu("Test MortonEncoding")]
    public void TestMortonEncoding()
    {
        TestMorton(x,y,z);
    }

    public void TestMorton(uint x,uint y,uint z)
    {
        ulong morton = MortonEncode.MortonEncode64(x, y, z);
        var decodes = MortonEncode.MortonDecode64(morton);
        Debug.Log($"{x} {y} {z} 编码为 {morton},解码为 {decodes[0]} {decodes[1]} {decodes[2]}");
        if (decodes[0] != x || decodes[1] != y || decodes[2] != z)
        {
            Debug.LogError($"错误：{x} {y} {z} 编码为 {morton},解码为 {decodes[0]} {decodes[1]} {decodes[2]}");
        }
    }
}

public struct Grid
{
    public int x;
    public int y;
    public double cost;
}