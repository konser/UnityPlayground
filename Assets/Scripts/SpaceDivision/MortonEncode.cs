using System;

/// <summary>
/// 莫顿编码 32位 + 64位 由C++版本中最简单的一版翻译而来
/// 原作者C++代码 https://github.com/Forceflow/libmorton
/// </summary>
public static class MortonEncode
{
    /// <summary>
    /// 编码为一个uint
    /// </summary>
    /// <param name="x">范围为0-1023</param>
    /// <param name="y">范围为0-1023</param>
    /// <param name="z">范围为0-1023</param>
    public static uint MortonEncode32(uint x, uint y, uint z)
    {
        x = (x | (x << 16)) & 0x030000FF;
        x = (x | (x << 8)) & 0x0300F00F;
        x = (x | (x << 4)) & 0x030C30C3;
        x = (x | (x << 2)) & 0x09249249;

        y = (y | (y << 16)) & 0x030000FF;
        y = (y | (y << 8)) & 0x0300F00F;
        y = (y | (y << 4)) & 0x030C30C3;
        y = (y | (y << 2)) & 0x09249249;

        z = (z | (z << 16)) & 0x030000FF;
        z = (z | (z << 8)) & 0x0300F00F;
        z = (z | (z << 4)) & 0x030C30C3;
        z = (z | (z << 2)) & 0x09249249;

        return x | (y << 1) | (z << 2);
    }

    public static uint[] MortonDecode32(uint input)
    {
        uint x, y, z;
        x = input & 0x09249249;
        y = (input >> 1) & 0x09249249;
        z = (input >> 2) & 0x09249249;

        x = ((x >> 2) | x) & 0x030C30C3;
        x = ((x >> 4) | x) & 0x0300F00F;
        x = ((x >> 8) | x) & 0x030000FF;
        x = ((x >> 16) | x) & 0x000003FF;

        y = ((y >> 2) | y) & 0x030C30C3;
        y = ((y >> 4) | y) & 0x0300F00F;
        y = ((y >> 8) | y) & 0x030000FF;
        y = ((y >> 16) | y) & 0x000003FF;

        z = ((z >> 2) | z) & 0x030C30C3;
        z = ((z >> 4) | z) & 0x0300F00F;
        z = ((z >> 8) | z) & 0x030000FF;
        z = ((z >> 16) | z) & 0x000003FF;
        //return Tuple.Create(x,y,z);
        return new uint[]{x,y,z};
    }
    /// <summary>
    /// 编码为一个ulong
    /// </summary>
    /// <param name="x">范围为0-2097151</param>
    /// <param name="y">范围为0-2097151</param>
    /// <param name="z">范围为0-2097151</param>
    public static ulong MortonEncode64(uint x, uint y, uint z)
    {
        ulong t = 0;
        t |= SplitBy3(x) | SplitBy3(y) << 1 | SplitBy3(z) << 2;
        return t;
    }

    public static uint[] MortonDecode64(ulong m)
    {
        uint x = GetThirdBits(m);
        uint y = GetThirdBits(m >> 1);
        uint z = GetThirdBits(m >> 2);
        return new uint[]{x,y,z};
    }

    private static ulong SplitBy3(uint a)
    {
        ulong x = a & 0x1fffff;
        x = (x | x << 32) & 0x1f00000000ffff; // shift left 32 bits, OR with self, and 00011111000000000000000000000000000000001111111111111111
        x = (x | x << 16) & 0x1f0000ff0000ff; // shift left 32 bits, OR with self, and 00011111000000000000000011111111000000000000000011111111
        x = (x | x << 8) & 0x100f00f00f00f00f; // shift left 32 bits, OR with self, and 0001000000001111000000001111000000001111000000001111000000000000
        x = (x | x << 4) & 0x10c30c30c30c30c3; // shift left 32 bits, OR with self, and 0001000011000011000011000011000011000011000011000011000100000000
        x = (x | x << 2) & 0x1249249249249249;
        return x;
    }

    private static uint GetThirdBits(ulong morton)
    {
        ulong x = morton & 0x1249249249249249;
        x = (x ^ (x >> 2)) & 0x10c30c30c30c30c3;
        x = (x ^ (x >> 4)) & 0x100f00f00f00f00f;
        x = (x ^ (x >> 8)) & 0x1f0000ff0000ff;
        x = (x ^ (x >> 16)) & 0x1f00000000ffff;
        x = (x ^ (x >> 32)) & 0x1fffff;
        return (uint)x;
    }
}
