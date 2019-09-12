Shader "Custom/Other/TexureSwitch"{
    Properties{
        _PlayerPos("Player Position",vector) = (0,0,0,0)
        _Dist("Distance",float) = 5
        _MainTex("Texture",2D) = "white" {}
        _SecondaryTex("Secondary Texture",2D) = "white" {}
    }

    SubShader{
        Tags { "RenderType" = "Opaque" }
        
        pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 worldPos : TEXCOORD1;
			};

            v2f vert(appdata_base input){
                v2f output;
                output.worldPos = mul(unity_ObjectToWorld,input.vertex);
                output.pos = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                return output;
            }

            float4 _PlayerPos;
            sampler2D _MainTex;
            sampler2D _SecondaryTex;
            float _Dist;

            float4 frag(v2f input):SV_TARGET{
                if(distance(_PlayerPos.xyz,input.worldPos.xyz) > _Dist){
                    return tex2D(_MainTex,input.uv);
                }else{
                    return tex2D(_SecondaryTex,input.uv);
                }
            }
            ENDCG
        }
    }
}