using UnityEngine;
using System.Collections;

public static class ComputerShaderExtend
{
    public static int FindKernel(this ComputeShader comp, string name)
    {
        return comp.HasKernel(name) ? comp.FindKernel(name) : -1;
    }

    public static void DispatchScaled(this ComputeShader comp, int kernel, int xCount, int yCount, int zCount)
    {
        uint xs, ys, zs;
        comp.GetKernelThreadGroupSizes(kernel, out xs, out ys, out zs);
        comp.Dispatch(kernel,
            Mathf.CeilToInt(xCount / (float)xs),
            Mathf.CeilToInt(yCount / (float)ys),
            Mathf.CeilToInt(zCount / (float)zs));
    }
}

public struct ScatterKernel
{
    private int[] _kernels;

    public enum EShadowMode
    {
        None,
        Shadowed,
        Cascaded
    }

    public ScatterKernel(ComputeShader comp, string baseName)
    {
        string shadowName = baseName + "Shadow";
        string cascadeName = baseName + "ShadowCascade";
        _kernels = new int[3];
        _kernels[0] = comp.FindKernel(baseName);
        _kernels[1] = comp.FindKernel(shadowName);
        _kernels[2] = comp.FindKernel(cascadeName);
    }

    public int GetKernel(EShadowMode mode)
    {
        return _kernels[(int) mode];
    }
}
