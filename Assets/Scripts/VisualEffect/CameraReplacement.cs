using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraReplacement : MonoBehaviour
{
    public Shader ReplacementShader;

    public Color OverDrawColor;
    // Start is called before the first frame update

    private void OnValidate()
    {
        Shader.SetGlobalColor("_OverDrawColor",OverDrawColor);
    }

    private void OnEnable()
    {
        if (ReplacementShader != null)
        {
            GetComponent<Camera>().SetReplacementShader(ReplacementShader,"");
        }
    }

    private void OnDisable()
    {
        GetComponent<Camera>().ResetReplacementShader();
    }
}
