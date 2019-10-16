using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MonoTest:MonoBehaviour
{
    public int size;
    public uint x, y, z;
    [ContextMenu("Test MortonEncoding")]
    public void TestMortonEncoding()
    {
        TestMorton(x,y,z);
    }

    private void OnLoadComplete(AsyncOperation op)
    {
        if (op.isDone)
        {
            Debug.Log("加载完成");
        }
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

    [ContextMenu("TestLoop")]
    public void TestLoop()
    {
        VoxelBuffer field = new VoxelBuffer(size,size,size);
        Stopwatch watch = Stopwatch.StartNew();
        for (int k = 0;k < field.zSize; k++)
        {
            for (int j = 0; j < field.ySize; j++)
            {
                for (int i = 0; i < field.xSize; i++)
                {
                    field[i, j, k] = 1;
                }
            }
        }
        watch.Stop();
        Debug.Log("Fast : "+watch.ElapsedMilliseconds +" ticks "+ watch.ElapsedMilliseconds);
        //watch = Stopwatch.StartNew();
        //for (int i = 0; i < field.xSize; i++)
        //{
        //    for (int j = 0; j < field.ySize; j++)
        //    {
        //        for (int k = 0; k < field.zSize; k++)
        //        {
        //            field.Value(i, j, k);
        //        }
        //    }
        //}
        //watch.Stop();
        //Debug.Log("Normal : " + watch.ElapsedMilliseconds + " ticks " + watch.ElapsedMilliseconds);
    }

    private void Func(float a)
    {

    }
}

public struct Grid
{
    public int x;
    public int y;
    public double cost;
}