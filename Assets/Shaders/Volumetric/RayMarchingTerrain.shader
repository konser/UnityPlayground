// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "RayMarching/RayMarchingTerrain"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite On ZTest Always

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
			};


			float hash1( float2 p )
			{
				p  = 50.0*frac( p*0.3183099 );
				return frac( p.x*p.y*(p.x+p.y) );
			}

			float hash1( float n )
			{
				return frac( n*17.0*frac( n*0.3183099 ) );
			}

			float2 hash2( float n ) { return frac(sin(float2(n,n+1.0))*float2(43758.5453123,22578.1459123)); }


			float2 hash2( float2 p ) 
			{
				const float2 k = float2( 0.3183099, 0.3678794 );
				p = p*k + k.yx;
				return frac( 16.0 * k*frac( p.x*p.y*(p.x+p.y)) );
			}

			float4 noised( in float3 x )
			{
				float3 p = floor(x);
				float3 w = frac(x);
				
				float3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
				float3 du = 30.0*w*w*(w*(w-2.0)+1.0);

				float n = p.x + 317.0*p.y + 157.0*p.z;
				
				float a = hash1(n+0.0);
				float b = hash1(n+1.0);
				float c = hash1(n+317.0);
				float d = hash1(n+318.0);
				float e = hash1(n+157.0);
				float f = hash1(n+158.0);
				float g = hash1(n+474.0);
				float h = hash1(n+475.0);

				float k0 =   a;
				float k1 =   b - a;
				float k2 =   c - a;
				float k3 =   e - a;
				float k4 =   a - b - c + d;
				float k5 =   a - c - e + g;
				float k6 =   a - b - e + f;
				float k7 = - a + b + c - d + e - f - g + h;

				return float4( -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z), 
								2.0* du * float3( k1 + k4*u.y + k6*u.z + k7*u.y*u.z,
												k2 + k5*u.z + k4*u.x + k7*u.z*u.x,
												k3 + k6*u.x + k5*u.y + k7*u.x*u.y ) );
			}

			// returns 3D fbm and its 3 derivatives
			float4 fbm( in float3 x, int octaves )
			{
				float f = 1.98;  // could be 2.0
				float s = 0.49;  // could be 0.5
				float a = 0.0;
				float b = 0.5;
				float3  d = float3(0.0,0.0,0.0);
				float3x3  mat = float3x3(1.0,0.0,0.0,
				0.0,1.0,0.0,
				0.0,0.0,1.0);
				for( int i=0; i < octaves; i++ )
				{
					float4 n = noised(x);
					a += b*n.x;          // accumulate values
					d +=mul(b*mat,n.yzw);      // accumulate derivatives
					b *= s;
					x = f*mat[2]*x;
					mat = f*mat[2][0]*mat; 
				}
				return float4( a, d );
			}

			float terrain( in float2 p )
			{
				const float2x2 m = float2x2(0.8,-0.6,0.6,0.8);
				float a = 0.0;
				float b = 1.0;
				float2  d = float2(0,0);
				for( int i=0; i<10; i++ )
				{
					float3 n=noised(float3(p.x,0,p.y));
					d +=n.yz;
					a +=b*n.x/(1.0+dot(d,d));
					b *=0.5;
					p=mul(m,p.xy)*2;
				}
				return a;
			}

			float height(float3 p) {
				return terrain(p.xz);
				float4 val = fbm(p,3);
				return val.z*val.x*sin(0.2*val.y*p.x) *  val*cos(0.2*val.w*p.z)+3;
			}

			float3 calcNormal(float3 pos) {
				const float2 epsilon = float2(0.02,0.0);
				float3 normal = float3(
					height(pos + epsilon.xyy) - height(pos - epsilon.xyy),
					0.04,
					//height(pos + epsilon.yxy) - height(pos - epsilon.yxy),
					height(pos + epsilon.yyx) - height(pos - epsilon.yyx));
				return normalize(normal);
			}

			fixed4 caculateLighting(float3 p) {
				float3 diffuse = float3(0.8	,0.5,0.35);
				float3 normal = calcNormal(p);
				float dotN_L = dot(_WorldSpaceLightPos0.xyz,normal);
				return fixed4(dotN_L * (_LightColor0.xyz),1);
			}
			// ray march
			bool castRay(float3 rayOrigin,float3 rayDir,out float dist) {
				float mint = 0.01;
				float maxt = 1000;
				float lastY = 0;
				float lastH  = 0;
				float dt = 0.02;
				fixed4 ret = fixed4(0,0,0,0);
				float travelDistance = 0;
				for(float t = mint;t<maxt;t+=dt){
					float3 p = rayOrigin + rayDir * t;
					float h = height(p);
					if(p.y < h){
						dist = t - dt + dt * ((lastH - lastY)/(p.y - lastY + lastH - h));
						return true;
					}
					dt = 0.05 * t; 
					lastH = h;
					lastY = p.y;
				}
				return false;
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
				// 传入的是屏幕空间的屏幕坐标，此时转换为世界空间，在像素着色器中使用
				o.ray = mul(_CameraInvViewMatrix,o.ray);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float3 rayDir = normalize(i.ray.xyz);
				float3 rayOrigin = _CameraWorldPos;
				float dist = 0;
				bool isHit = castRay(rayOrigin,rayDir,dist);
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture,i.uv).r);
				float3 hitPoint = rayOrigin + rayDir * dist;
				depth *= length(i.ray.xyz);
				fixed3 beforeColor = tex2D(_MainTex,i.uv);
				if(isHit){
					return caculateLighting(hitPoint);
				}else{
					return fixed4(beforeColor,1);
				}
			}
			ENDCG
		}
	}
}
