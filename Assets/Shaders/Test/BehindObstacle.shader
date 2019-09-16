Shader "Custom/Test/BehindObstacle"{
    Properties{
        _MainTex("Main Texture",2D) = "white"{}
        _OccludedColor("Occulude Color",Color)=(1.0,1.0,1.0,1.0)
        _OutlineVal ("Outline value", Range(0., 2.)) = 1.
        _OutlineCol ("Outline color", color) = (1., 1., 1., 1.)
    }

    SubShader{
        Pass{
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 200
        ZWrite On
        ZTest LEqual

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION; // vertex position
                float2 uv : TEXCOORD0; // texture coordinate
            };

            struct v2f
            {
                float2 uv : TEXCOORD0; // texture coordinate
                float4 vertex : SV_POSITION; // clip space position
            };


            v2f vert (appdata v)
            {

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG

        }

        Pass
        {  
            ZTest Greater
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct v2f {
                float4 pos : SV_POSITION;
            };
 
            float _OutlineVal;
 
            v2f vert(appdata_base v) {
                v2f o;
 
                // Convert vertex to clip space
                o.pos = UnityObjectToClipPos(v.vertex);
 
                // Convert normal to view space (camera space)
                // https://stackoverflow.com/questions/13654401/why-transforming-normals-with-the-transpose-of-the-inverse-of-the-modelview-matr
                float3 normal = mul((float3x3) UNITY_MATRIX_IT_MV, v.normal);
                // Compute normal value in clip space
                normal.x *= UNITY_MATRIX_P[0][0];
                normal.y *= UNITY_MATRIX_P[1][1];
 
                // Scale the model depending the previous computed normal and outline value
                // 就是把要写入到裁剪空间的顶点位置按法线方向扩大了一圈
                o.pos.xy += _OutlineVal * normal.xy;
                return o;
            }
 
            fixed4 _OutlineCol;
 
            fixed4 frag(v2f i) : SV_Target {
                return _OutlineCol;
            }
 
            ENDCG
        }

        Pass{
          
            ZTest Greater
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "UnityCG.cginc"

            struct v2f{
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD;
            };
            
            float4 _OccludedColor;
            sampler2D _MainTex;
            v2f vert(appdata_base input){
                v2f o;
                o.pos =  UnityObjectToClipPos(input.vertex);
                o.uv = input.texcoord;
                return o;
            }

            float4 frag(v2f input):SV_TARGET{
                return _OccludedColor * tex2D(_MainTex,input.uv);
            }
            ENDCG
        }

    }

    FallBack "Diffuse"
}