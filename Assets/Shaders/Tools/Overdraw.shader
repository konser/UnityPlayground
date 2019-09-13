// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Tools/OverDraw"
{
	Properties
	{
		_OverDrawColor("OverDraw Color ",Color) = (0.8,0.2,0.2,1.0)
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent"}
		ZTest Always
		ZWrite Off
		Blend One One
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f {
				float4 pos : SV_POSITION;
			};

            v2f vert(appdata_base input){
                v2f o;
                o.pos = UnityObjectToClipPos(input.vertex);
                return o;
            }
			half4 _OverDrawColor;
            float4 frag(v2f input) : SV_TARGET
			{
				return _OverDrawColor;
            }
            ENDCG
        }
    }
}