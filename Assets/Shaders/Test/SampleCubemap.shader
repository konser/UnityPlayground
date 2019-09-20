Shader "Custom/Test/SampleCubemap"{
 Properties
    {
        [Header(Shadow)]
        _Cubemap("Cubemap",Cube) = ""{}
        _LightPos("Light Pos",vector) = (0,0,0,0)
        _BoundMin("BoundMin",vector) = (0,0,0,0)
        _BoundMax("BoundMax",vector) = (0,0,0,0)
        _CubePos("Cube pos",vector) = (0,0,0,0)

        _MainTex ("Texture", 2D) = "white" {}
        [Header(Ambient)]
        _Ambient ("Intensity", Range(0., 1.)) = 0.1
        _AmbColor ("Color", color) = (1., 1., 1., 1.)
 
        [Header(Diffuse)]
        _Diffuse ("Val", Range(0., 1.)) = 1.
        _DifColor ("Color", color) = (1., 1., 1., 1.)
 
        [Header(Specular)]
        _Shininess ("Shininess", Range(0.1, 10)) = 1.
        _SpecularColor ("Specular color", color) = (1., 1., 1., 1.)
 
        [Header(Emission)]
        _EmissionTex ("Emission texture", 2D) = "gray" {}
        _EmiVal ("Intensity", float) = 0.
        [HDR]_EmiColor ("Color", color) = (1., 1., 1., 1.)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
        
        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
			#include "Lighting.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };
            samplerCUBE _Cubemap;
            float3 _LightPos;
            float3 _BoundMax;
            float3 _BoundMin;
            float3 _CubePos;
            sampler2D _MainTex;
			float4 _MainTex_ST;
            float _Diffuse;
            float4 _DifColor;

            float _Shininess;
            float4 _SpecularColor;

            float _Ambient;
            float4 _AmbColor;

            sampler2D _EmissionTex;
            float4 _EmiColor;
            float _EmiVal;
            v2f vert (appdata_full input)
            {
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                output.normal = normalize(UnityObjectToWorldNormal(input.normal));
                output.worldPos = mul(unity_ObjectToWorld,input.vertex);
                return output;
            }

            bool isInShadowRange(float3 worldPos){
                float3 vertexToLight = normalize(_LightPos - worldPos);
                float3 intersectMax = (_BoundMax-worldPos)/vertexToLight;
                float3 intersectMin = (_BoundMin - worldPos)/vertexToLight;
                float3 largeRayParams = max(intersectMax,intersectMin);
                float dist = min(min(largeRayParams.x,largeRayParams.y),largeRayParams.z);
                float3 intersectPos = worldPos + vertexToLight * dist;
                float3 lp = intersectPos - _CubePos;
                float4 value = texCUBE(_Cubemap,lp);
                float3 comp = float3(0.01,0.01,0.01);
                if(value.x < comp.x && value.y < comp.y && value.z < comp.z){
                    return true;
                }
                return false;
            }

            fixed4 frag (v2f input) : SV_Target
            {
                // Directions
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz-input.worldPos.xyz);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = input.normal;
                float3 reflectLightDir = reflect(-lightDir,normal);
                float3 halfVec = normalize(normal+lightDir);
                float dot_n_l = max(0.0,dot(lightDir,normal));
                float dot_r_v = max(0.0,dot(viewDir,reflectLightDir));
                float dot_n_h = max(0.0,dot(normal,halfVec));
                //  Color
                float4 ambColor = _Ambient * _AmbColor;
				float4 diffColor = _Diffuse * _DifColor * dot_n_l * _LightColor0;
                float4 specColor = _Shininess*_SpecularColor*pow(dot_r_v,_Shininess)*_LightColor0;
                float4 emiColor = tex2D(_EmissionTex, input.uv).r * _EmiColor * _EmiVal;
                float4 c = tex2D(_MainTex,input.uv);
                float4 resultColor = c*(ambColor+ diffColor + specColor) + emiColor;
                // sample cubemap
                if(isInShadowRange(input.worldPos.xyz)){
                    return resultColor/10;
                }
                return resultColor;
            }


            ENDCG
        }
    }
}