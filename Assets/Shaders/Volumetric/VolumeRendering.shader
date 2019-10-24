Shader "Volume/VolumeRendering"
{
	SubShader
	{
		Cull Back
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex SetVertexShader
			#pragma fragment SetPixelShader
			#pragma target 5.0

			sampler3D _Volume;
			half _Intensity, _Threshold;
			half3 _SliceMin, _SliceMax;
			float4x4 _AxisRotationMatrix;

			float SampleVolume(float3 uv, float3 p)
			{
				float3 axis = mul(_AxisRotationMatrix, float4(p, 0)).xyz + 0.5;
				float min = step(_SliceMin.x, axis.x) * step(_SliceMin.y, axis.y) * step(_SliceMin.z, axis.z);
				float max = step(axis.x, _SliceMax.x) * step(axis.y, _SliceMax.y) * step(axis.z, _SliceMax.z);
				return tex3D(_Volume, uv).r * _Intensity * min * max;
			}

			void SetVertexShader(inout float4 vertex:POSITION, out float3 world : TEXCOORD1)
			{
				world = mul(unity_ObjectToWorld, vertex).xyz;
				vertex = UnityObjectToClipPos(vertex);
			}

			fixed4 SetPixelShader(in float4 vertex : POSITION, in float3 world : TEXCOORD1) : SV_Target
			{
				float3 ray_origin = mul(unity_WorldToObject, float4(world, 1)).xyz;
				float3 ray_dir = normalize(mul((float3x3) unity_WorldToObject, normalize(world - _WorldSpaceCameraPos)));
				float3 AABBmin = float3(-0.5, -0.5, -0.5);
				float3 AABBmax = float3(0.5, 0.5, 0.5);
				float3 tbot = (1.0 / ray_dir) * (AABBmin - ray_origin);
				float3 ttop = (1.0 / ray_dir) * (AABBmax - ray_origin);
				float3 tmin = min(ttop, tbot);
				float3 tmax = max(ttop, tbot);
				float2 a = max(tmin.xx, tmin.yz);
				float tnear = max(0.0,max(a.x, a.y));
				float2 b = min(tmax.xx, tmax.yz);
				float tfar = min(b.x, b.y);
				float3 end = ray_origin + ray_dir * tfar;
				float3 d = normalize(end - ray_origin) * (abs(tfar - tnear) / float(150));
				float4 t = float4(0, 0, 0, 0);
				[unroll]
				for (int i = 0; i < 150; i++)
				{
					float v = SampleVolume(ray_origin + 0.5, ray_origin);
					float4 s = float4(v, v, v, v);
					s.a *= 0.5;
					s.rgb *= s.a;
					t = (1.0 - t.a) * s + t;
					ray_origin += d;
					if (t.a > _Threshold) break;
				}
				return saturate(t);
			}

			ENDCG
		}
	}
}