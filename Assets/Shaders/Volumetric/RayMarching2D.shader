// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "RayMarching/RayMarching2D"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			sampler2D _MainTex;
			uniform float4x4 _FrustumCornersEyeSpace;
			uniform float4 _MainTex_TexelSize;
			uniform float4x4 _CameraInvViewMatrix;
			uniform float3 _CameraWorldPos;
			sampler2D _CameraDepthTexture;
			float4x4 _SDFTransform_1;
			float4x4 _SDFTransform_2;
			float4x4 _SDFTransform_3;
			#define MAXSTEP 64
			#define EPSILON 0.01
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 ray : TEXCOORD1;
				float2 pos2 : TEXCOORD3;
			};

			float sdf(float2 p){
				return p.x ;
			}

			// ray march
			fixed4 rayMarching(float3 rayOrigin,float3 rayDir,float viewDepth) {
				const int maxStep = 64;
				const fixed epsilon = 0.02;
				fixed4 ret = fixed4(0,0,0,0);
				float travelDistance = 0;
				for (int i = 0; i < maxStep; i++) {
					float3 p = rayOrigin + rayDir * travelDistance;
				}
				return ret;
			}

			v2f vert(appdata v)
			{
				v2f o;
				half index = v.vertex.z;
				v.vertex.z = 0.1;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif
				// 利用渲染管线中像素位置自动在顶点之间插值的特性
				// 由四个角的方向自动得出每个像素位置的射线方向
				// 后处理实际上只渲染了一个QUAD
				o.ray = _FrustumCornersEyeSpace[(int)index].xyz;
				o.ray /= abs(o.ray.z);
				o.pos2 =o.pos.xy;
				// 传入的是屏幕空间的屏幕坐标，此时转换为世界空间，在像素着色器中使用
				o.ray = mul(_CameraInvViewMatrix,o.ray);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(i.pos2.xy/_ScreenParams.xy,0,1);
			}
			ENDCG
		}
	}
}
