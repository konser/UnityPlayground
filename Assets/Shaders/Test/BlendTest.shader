Shader "Test/Blend" {
    Properties {
        //_MainTex ("Texture to blend", 2D) = "black" {}
        _Color("Color to blend",Color) = (1.0,1.0,1.0,1.0)
    }
    SubShader {
        Tags { "Queue" = "Transparent" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            //SetTexture [_MainTex] { combine texture }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
			#include "Lighting.cginc"

            struct v2f{
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base input){
                v2f output;
                output.pos = UnityObjectToClipPos(input.vertex);
                return output;
            }
            float4 _Color;

            float4 frag(v2f input):SV_TARGET
            {
                return _Color;
            }

            ENDCG
        }
    }
}