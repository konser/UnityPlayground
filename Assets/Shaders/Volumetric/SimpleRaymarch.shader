Shader "Custom/Volumetric/SimpleRaymarch"
{
    Properties
    {
        _Radius("Radius",float) = 1.0
        _MinSDFDistance("SDF Dist",float) = 0.01
        _SphereColor("Sphere Color",Color) = (1.0,1.0,1.0,1.0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 100

        Pass
        {
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #define STEPS 64
            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Radius;
            float _MinSDFDistance;
            float4 _CubeColor;
            float4 _SphereColor;
            //float4 _LightColor0;

            float3 getObjectWorldPos(){
                return unity_ObjectToWorld._m03_m13_m23;
            }

            float sdf(float3 pos){
                return distance(pos,getObjectWorldPos()) - _Radius;
            }

            float3 estimateNormal(float3 pos){
                const float diff = 0.01;
                return normalize(float3(
                    ( (sdf(pos+float3(diff,0,0)))-(sdf(pos-(float3(diff,0,0)))) ),
                    ( (sdf(pos+float3(0,diff,0)))-(sdf(pos-(float3(0,diff,0)))) ),
                    ( (sdf(pos+float3(0,0,diff)))-(sdf(pos-(float3(0,0,diff)))) )
                ));
            }

            float4 simpleLambert(float3 normal){
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float dot_n_l = max(0.0,dot(lightDir,normal));
                float4 diffColor =  _SphereColor * dot_n_l * _LightColor0;
                diffColor.a = 1;
                return diffColor;
            }

            float4 raymarchHit(float3 position,float3 dir){
                for(int i=0;i<STEPS;i++){
                    float distance = sdf(position);
                    if(distance < _MinSDFDistance){
                        return simpleLambert(estimateNormal(position));
                    }
                    position += distance * dir;
                }
                return float4(1,1,1,0);
            }

            v2f vert (appdata_full v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld,v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPos = i.worldPos;
                float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                float4 factor = raymarchHit(worldPos,viewDir);
                return factor;
            }
            ENDCG
        }
    }
}
