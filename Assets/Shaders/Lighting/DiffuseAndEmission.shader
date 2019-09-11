Shader "Custom/Lighting/DiffuseAndEmission"
{
    Properties
    {
        [Header(_Diffuse)]
        _Color("Color",Color) = (1.0,1.0,1.0,1.0)
        _Diffuse("Diffuse Value",Range(0,1)) = 1.0
        [Header(_Emission)]
        _MainTex ("Emission Map", 2D) = "white" {}
        [HDR]
        _EmissionColor("Emission Color",Color) = (0,0,0)
    }
    SubShader
    {
        Tags { "LightMode"="ForwardBase"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "Lighting.cginc"
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD;
                float3 normal :NORMAL;
                fixed4 col : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Diffuse;
            float4 _EmissionColor;
            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float dot_n_l = dot(o.normal,lightDir);
                float4 diff = _Color * dot_n_l*_LightColor0*_Diffuse;
                
                o.col = diff;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 emi = tex2D(_MainTex,i.uv).r*_EmissionColor.rgb*(abs(sin(_Time))*0.5 + 0.5);
                i.col.rgb += emi;
                return i.col;
            }
            ENDCG
        }
    }
}
