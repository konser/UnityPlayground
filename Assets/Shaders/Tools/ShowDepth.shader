// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Tools/ShowDepth"
{
	Properties
	{
	}
	SubShader
	{
		Tags{"RenderType" = "Opaque"}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f {
				float4 pos : SV_POSITION;
				float depth : DEPTH;
			};

            v2f vert(appdata_base input){
                v2f o;
                o.pos = UnityObjectToClipPos(input.vertex);
                // _ProjectionParams.w = 1/FarClipFace 
                // 转换至相机空间后，z值代表深度，乘以w值范围处于0到1
                // 取反使其为正数(z轴负向为视角方向)
                o.depth = -(UnityObjectToViewPos(input.vertex).z) * _ProjectionParams.w;
                return o;
            }

            float4 frag(v2f input) : SV_TARGET
			{
                float inv = input.depth;
                return float4(inv,inv,inv,1);
            }
            ENDCG
        }
    }
}