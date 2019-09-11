Shader "Custom/Lighting/DiffusePerFragment"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);

                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dif = max(0.0,dot(i.normal,normalize(_WorldSpaceLightPos0.xyz)));
                fixed4 col = tex2D(_MainTex,i.uv);
                return fixed4(col.rgb*dif* _LightColor0.rgb,1);
            }
            ENDCG
        }
    }
}
