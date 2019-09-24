﻿Shader "Custom/InstancedColorSurfaceShader" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
	}

		SubShader{
			Tags { "RenderType" = "Opaque" }
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows
			// Use Shader model 3.0 target
			#pragma target 3.0
			struct Input {
				float2 uv_MainTex;
			};
			UNITY_INSTANCING_BUFFER_START(Props)
			   UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
			UNITY_INSTANCING_BUFFER_END(Props)
			void surf(Input IN, inout SurfaceOutputStandard o) {
				fixed4 c = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
				o.Albedo = c.rgb;
				o.Alpha = c.a;
			}
			ENDCG
		}
			FallBack "Diffuse"
}
