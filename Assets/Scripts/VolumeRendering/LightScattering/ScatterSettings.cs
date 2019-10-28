using UnityEngine;
using System.Collections;

[System.Serializable]
public class ScatterGradient
{
    public float start;
    public float end;
    public Gradient gradient = new Gradient();
}

[CreateAssetMenu(fileName = "LightScattering")]
public class ScatterSettings : ScriptableObject
{
    /// <summary>
    /// 三种光(r,g,b)的反照率 反照率 = 散射系数/消光系数 σs/σt
    /// </summary>
    public Color albedo = new Color(0.1f,0.1f,0.1f);

    /// <summary>
    /// 消光系数 σt
    /// </summary>
    public float extinction;

    /// <summary>
    /// 自发光
    /// </summary>
    public Color emission = Color.black;

    /// <summary>
    /// 环境光
    /// </summary>
    public Color ambient = Color.black;
    
    /// <summary>
    /// 渐变分辨率
    /// </summary>
    private const int GRADIENT_RESOLUTION = 128;

    /// <summary>
    /// 高度渐变
    /// </summary>
    public ScatterGradient heightGradient = new ScatterGradient();

    /// <summary>
    /// 距离渐变
    /// </summary>
    public ScatterGradient distanceGradient = new ScatterGradient();

    private Texture2D _gradientTex;

    public Texture2D gradientTex
    {
        get {
            if (_gradientTex == null)
            {
                UpdateGradients();
            }

            return _gradientTex;
        }
    }

    public void UpdateGradients()
    {
        if (_gradientTex == null)
        {
            _gradientTex = new Texture2D(GRADIENT_RESOLUTION, GRADIENT_RESOLUTION, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] colorBuffer = new Color[GRADIENT_RESOLUTION*GRADIENT_RESOLUTION];

            for (int x = 0; x < GRADIENT_RESOLUTION; x++)
            {
                for (int y = 0; y < GRADIENT_RESOLUTION; y++)
                {
                    float tx = (float) x / (GRADIENT_RESOLUTION - 1);
                    float ty = (float) y / (GRADIENT_RESOLUTION - 1);

                    Color colorDist = distanceGradient.gradient.Evaluate(tx);
                    Color colorHeight = heightGradient.gradient.Evaluate(ty);
                    colorBuffer[x + y * GRADIENT_RESOLUTION] = colorDist * colorHeight;
                }
            }

            // 将渐变色立即应用
            _gradientTex.SetPixels(colorBuffer);
            _gradientTex.Apply();
        }
    }

    public void Bind(ComputeShader comp, int kernel, ScatterSettings blendTo, float blendTime)
    {
        // 在两个配置间插值
        Color lerpAledo = Color.Lerp(this.albedo, blendTo.albedo, blendTime);
        float lerpExtinction = Mathf.Lerp(this.extinction, blendTo.extinction, blendTime);
        Color lerpEmission = Color.Lerp(this.emission,blendTo.emission,blendTime);
        Color lerpAmbient = Color.Lerp(this.ambient, blendTo.ambient, blendTime);

        // 自发光调整
        lerpEmission = lerpEmission * lerpEmission.a;
        lerpEmission.r /= lerpAledo.r + 1;
        lerpEmission.g /= lerpAledo.g + 1;
        lerpEmission.b /= lerpAledo.b + 1;
        Color ambientEmission = lerpAmbient * lerpAmbient.a;
        Color totalEmission = (lerpEmission + ambientEmission) * 5.0f;

        comp.SetVector("_AlbedoExt",new Vector4(lerpAledo.r,lerpAledo.g,lerpAledo.b,lerpExtinction));
        comp.SetFloat("_Extinction",lerpExtinction);
        comp.SetVector("_Emission",new Vector4(totalEmission.r,totalEmission.g,totalEmission.b));
        comp.SetFloat("_Time",Time.time * 10.0f);

        float heightStart = Mathf.Lerp(heightGradient.start, blendTo.heightGradient.start, blendTime);
        float heightEnd = Mathf.Lerp(heightGradient.end, blendTo.heightGradient.end, blendTime);

        float distanceStart = Mathf.Lerp(distanceGradient.start, blendTo.distanceGradient.start, blendTime);
        float distanceEnd = Mathf.Lerp(distanceGradient.end, blendTo.distanceGradient.end, blendTime);

        float heightSize = Mathf.Max(0, heightEnd - heightStart);
        float distSize = Mathf.Max(0, distanceEnd - distanceStart);

        Vector4 gradientSettings = new Vector4(1.0f/heightSize,-heightStart/heightSize,1.0f/distSize,-distanceStart/distSize);
        comp.SetVector("_GradientSettings",gradientSettings);
        comp.SetTexture(kernel,"_GradientTexture",gradientTex);

        if (blendTime > 0)
        {
            comp.SetTexture(kernel,"_GradientTextureBlend",blendTo.gradientTex);
            comp.SetFloat("_SettingBlend",blendTime);
        }
        else
        {
            comp.SetTexture(kernel, "_GradientTextureBlend", Texture2D.whiteTexture);
            comp.SetFloat("_SettingBlend", 0.0f);
        }
    }
}
