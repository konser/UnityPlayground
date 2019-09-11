Shader "Custom/Lighting/DiffuseAndSpecular"
{
    Properties
    {
        [Header(Diffuse)]
        _DiffuseColor("Diffuse Color",Color) = (1.0,1.0,1.0,1.0)
        _DiffuseFactor("Diffuse Factor",Range(0,1))  = 0.8 
        [Header(Specular)]
        _SpecularColor("Specular Color",Color) = (1.0,1.0,1.0,1.0)
        _Shininess("Specular Factor",Range(0,10))  = 1
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
                float3 normal :NORMAL;
                float4 color:COLOR;
            };

            float4 _DiffuseColor;
            float4 _SpecularColor;
			float _DiffuseFactor;
			float _Shininess;
            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                float3 lightDir  = normalize(_WorldSpaceLightPos0.xyz);
                // diffuse 与光照方向有关 法线与光线的夹角
                float dot_n_l = max(0.0,dot(o.normal,lightDir));
                float4 diff = _DiffuseColor * dot_n_l*_LightColor0*_DiffuseFactor;
                
                // specular 与反射光线与视线有关 夹角越小（点乘值越大） 越亮
                float4 worldPos = mul(unity_ObjectToWorld,v.vertex);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz-worldPos.xyz);                
                float3 reflectLightDir = reflect(-lightDir,o.normal);
                float dot_r_v = max(0.0,dot(viewDir,reflectLightDir));
                float4 spec = ceil(dot_n_l)*_LightColor0*_SpecularColor*pow(dot_r_v,_Shininess);
                
                o.color = diff + spec;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
