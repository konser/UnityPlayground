using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

public enum EShaderParamType
{
    Int,
    Float,
    Vector4,
    Color,
    Texture,
    Matrix
}

[Serializable]
public abstract class ShaderParam
{
    public abstract EShaderParamType paramType { get; }
    public string paramName;
}

[Serializable]
public class ShaderParamVector4 : ShaderParam
{
    public Vector4 value;

    public override EShaderParamType paramType
    {
        get { return EShaderParamType.Vector4; }
    }
}

[Serializable]
public class ShaderParamTexture : ShaderParam
{
    public Texture value;
    public override EShaderParamType paramType
    {
        get { return EShaderParamType.Texture; }
    }
}

[Serializable]
public class ShaderParamMatrix : ShaderParam
{
    public Matrix4x4 value;
    public override EShaderParamType paramType
    {
        get { return EShaderParamType.Matrix; }
    }
}

public class ShaderParams : MonoBehaviour
{
    public Material material;
    [SerializeReference]
    public List<ShaderParam> shaderParamList = new List<ShaderParam>();

    [ContextMenu("Add Vector4")]
    void AddVector4()
    {
        shaderParamList.Add(new ShaderParamVector4());
    }

    [ContextMenu("Add Texture")]
    void AddTexture()
    {
        shaderParamList.Add(new ShaderParamTexture());
    }

    [ContextMenu("Add Matrix")]
    void AddMatrix()
    {
        shaderParamList.Add(new ShaderParamMatrix());
    }

    private void Update()
    {
        for (int i = 0; i < shaderParamList.Count; i++)
        {
            switch (shaderParamList[i].paramType)
            {
                case EShaderParamType.Int:
                    break;
                case EShaderParamType.Float:
                    break;
                case EShaderParamType.Vector4:
                    break;
            }
        }
    }
}
