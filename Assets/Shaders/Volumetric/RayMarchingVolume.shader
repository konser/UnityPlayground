// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "RayMarching/RayMarchingVolume"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_PseduoNumTex("RNG LUT",2D) = "white"{}
	}
		SubShader
	{
		// No culling or depth
		Cull Off

		Pass
		{
			Blend OneMinusSrcAlpha OneMinusSrcAlpha
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
			sampler2D _PseduoNumTex;
			float4x4 _SDFTransform_1;
			float4x4 _SDFTransform_2;
			float4x4 _SDFTransform_3;

			#define MAXSTEP 64
			#define EPSILON 0.01
			#define TEXTURE_SIZE 512.0
            #define SAMPLE_SIZE 32.0
			#define TEXTURE_DEPTH ((TEXTURE_SIZE/SAMPLE_SIZE)*(TEXTURE_SIZE/SAMPLE_SIZE))
            #define ONE_PIXEL (1.0/SAMPLE_SIZE)
            #define TWO_PIXEL (2.0/SAMPLE_SIZE)

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
			float4 CubicHermite(float4 A,float4 B,float4 C,float4 D,float t){
                float t2 = t*t;
                float t3 = t*t*t;
                float4 a = -A/2.0 + (3.0*B)/2.0 - (3.0*C)/2.0 + D/2.0;
                float4 b = A - (5.0*B)/2.0 + 2.0*C - D / 2.0;
                float4 c = -A/2.0 + C/2.0;
                float4 d = B;
                
                return a*t3 + b*t2 + c*t + d;
            }

            float4 BicubicHermiteTextureSample(float2 P){
                float2 pixel = P * SAMPLE_SIZE + 0.5f;
                float2 fracValue = float2(frac(pixel.x),frac(pixel.y));
                pixel = floor(pixel) / SAMPLE_SIZE - float2(ONE_PIXEL/2.0,ONE_PIXEL/2.0);
                
                float4 C00 = tex2D(_PseduoNumTex, pixel + float2(-ONE_PIXEL ,-ONE_PIXEL));
                float4 C10 = tex2D(_PseduoNumTex, pixel + float2( 0.0        ,-ONE_PIXEL));
                float4 C20 = tex2D(_PseduoNumTex, pixel + float2( ONE_PIXEL ,-ONE_PIXEL));
                float4 C30 = tex2D(_PseduoNumTex, pixel + float2( TWO_PIXEL,-ONE_PIXEL));
                
                float4 C01 = tex2D(_PseduoNumTex, pixel + float2(-ONE_PIXEL , 0.0));
                float4 C11 = tex2D(_PseduoNumTex, pixel + float2( 0.0        , 0.0));
                float4 C21 = tex2D(_PseduoNumTex, pixel + float2( ONE_PIXEL , 0.0));
                float4 C31 = tex2D(_PseduoNumTex, pixel + float2( TWO_PIXEL, 0.0));    
                
                float4 C02 = tex2D(_PseduoNumTex, pixel + float2(-ONE_PIXEL , ONE_PIXEL));
                float4 C12 = tex2D(_PseduoNumTex, pixel + float2( 0.0        , ONE_PIXEL));
                float4 C22 = tex2D(_PseduoNumTex, pixel + float2( ONE_PIXEL , ONE_PIXEL));
                float4 C32 = tex2D(_PseduoNumTex, pixel + float2( TWO_PIXEL, ONE_PIXEL));    
                
                float4 C03 = tex2D(_PseduoNumTex, pixel + float2(-ONE_PIXEL , TWO_PIXEL));
                float4 C13 = tex2D(_PseduoNumTex, pixel + float2( 0.0        , TWO_PIXEL));
                float4 C23 = tex2D(_PseduoNumTex, pixel + float2( ONE_PIXEL , TWO_PIXEL));
                float4 C33 = tex2D(_PseduoNumTex, pixel + float2( TWO_PIXEL, TWO_PIXEL));    
                
                float4 CP0X = CubicHermite(C00, C10, C20, C30, fracValue.x);
                float4 CP1X = CubicHermite(C01, C11, C21, C31, fracValue.x);
                float4 CP2X = CubicHermite(C02, C12, C22, C32, fracValue.x);
                float4 CP3X = CubicHermite(C03, C13, C23, C33, fracValue.x);
                
                return CubicHermite(CP0X, CP1X, CP2X, CP3X, fracValue.y);
            }


            float4 sampleAtIndex(int x,int z,float2 uv){
                float2 minCorner = float2(x/TEXTURE_SIZE,z/TEXTURE_SIZE);
                float delta = SAMPLE_SIZE/TEXTURE_SIZE;
                float2 newUV = minCorner + uv*delta;
                return BicubicHermiteTextureSample(newUV);
            }

			float4 sampleAt(float3 uvw){
				float depth = uvw.z * TEXTURE_DEPTH;
				float minDepth = floor(depth);
				float maxDepth = ceil(depth);
				int rowCount = (int) TEXTURE_SIZE/SAMPLE_SIZE;
				float t = depth - minDepth;
				int max_indexX = (int)(maxDepth)%rowCount;
				int max_indexZ = (int)(floor(maxDepth/rowCount));
				int min_indexX = (int)(minDepth)%rowCount;
				int min_indexZ = (int)(floor(minDepth/rowCount));

				float4 resultMin = sampleAtIndex(min_indexX,min_indexZ,uvw.xy).aaaa;
				float4 resultMax = sampleAtIndex(max_indexX,max_indexZ,uvw.xy).aaaa;

				return lerp(resultMin,resultMax,t);
			}

			float sdSphere(float3 p,float3 center,float radius){
				return length(p - center) - radius;
			}

			// ray march
			bool rayMarching(float3 rayOrigin,float3 rayDir,out float tEnter,out float tExit) {
				float4 ret = 0;
				float travelDist = 0;
				bool entered = false;
				float3 p = rayOrigin + rayDir * travelDist;
				float lastd = sdSphere(p,float3(0,0,0),2);
				if(lastd < 0){
					entered = true;
				}

				for(int i=0;i<MAXSTEP;i++){
					p = rayOrigin + rayDir * travelDist;
					float d = sdSphere(p,float3(0,0,0),2);
					if(entered){
						if(d < EPSILON){
							tExit = travelDist;
							return true;
						} 
					}else{
						if(d < EPSILON){
							if(entered == false){
								tEnter = travelDist;
								entered = true;
							}else if(entered){
								tExit = travelDist;
								return true;
							}
						}
					}
					travelDist += 0.05;
				}
				return true;
			}

			float4 accumulate(float dist,float2 uv){
				float sampleStep = 0.1;
				int count = 0;
				float4 ret = 0;
				[unroll(40)]
				for(float t = 0;t < dist;t+=sampleStep,count++){
					float3 uvw = float3(uv,t/sampleStep);
					ret += sampleAt(uvw);
				}
				ret /= count;
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
				// 传入的是屏幕空间的屏幕坐标，此时转换为世界空间，在像素着色器中使用
				o.ray = mul(_CameraInvViewMatrix,o.ray);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 rayDir = normalize(i.ray.xyz);
				float3 rayOrigin = _CameraWorldPos;
				float tEnter = 0;
				float tExit = 0;
				bool hit = rayMarching(rayOrigin,rayDir,tEnter,tExit);
				float dist = abs(tEnter - tExit);
				if(dist < EPSILON || !hit){
					return tex2D(_MainTex,i.uv);
				}
				return accumulate(dist,i.uv);
			}
			ENDCG
		}
	}
}
