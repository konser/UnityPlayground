Shader "Custom/Sky_v1"{

    Properties{
		_Cubemap("Sky Cubemap",Cube) = "_Skybox" {}
	}

	SubShader{
		Tags { "QUEUE" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }

		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			samplerCUBE _Cubemap;

			struct a2v {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 worldPos : TEXCOORD0;
			};

			v2f vert(float4 vertex : POSITION) {
				v2f o;
				o.pos = UnityObjectToClipPos(vertex);
				o.worldPos = mul(unity_ObjectToWorld, vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_TARGET{
				fixed3 viewDir = UnityWorldSpaceViewDir(i.worldPos);
				return texCUBE(_Cubemap,-viewDir);
			}
			ENDCG
		}
    }
}
