// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/NewImageEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            float4x4 _SDFTransform;
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
        

            // signed distance field function
            // torus: t.x 直径 t.y 厚度
            float sdTorus(float3 p,float2 t){
                float2 q = float2(length(p.xz) - t.x,p.y);
                return length(q) - t.y;
            }
            
            // 这里可以根据到图形表面的距离进行图形的组合
            // 交:max(a,b) 并:min(a,b) 差:max(a,-b)
            float map(float3 p){
                float4 q = mul(_SDFTransform,float4(p,1));
                float ring1 = max(length(q)-5.1,sdTorus(q,float2(5,0.2)));
                float ring2 = max(length(q)-4.1,sdTorus(q,float2(4,0.2)));
                return min(length(q)-2,min(ring1,ring2));
            }
            float3 calcNormal(in float3 pos){
                const float2 epsilon = float2(0.001,0.0);
                float3 normal = float3(
                    map(pos+epsilon.xyy) - map(pos-epsilon.xyy),
                    map(pos+epsilon.yxy) - map(pos-epsilon.yxy),
                    map(pos+epsilon.yyx) - map(pos-epsilon.yyx));
                return normalize(normal);
            }

            // ray march
            fixed4 rayMarching(float3 rayOrigin,float3 rayDir,float viewDepth){
                const int maxStep = 64;
                const fixed epsilon = 0.02;
                fixed4 ret = fixed4(0,0,0,0);
                float travelDistance = 0;
                for(int i=0;i<maxStep;i++){
                    if(travelDistance > viewDepth){
                        ret = fixed4(0,0,0,0);
                        break;
                    }
                    float3 p = rayOrigin + rayDir*travelDistance;
                    float d = map(p);
                    if(d < epsilon){
                        float3 normal = calcNormal(p);
                        float dotNL = dot(_WorldSpaceLightPos0.xyz,normal);
                        ret = fixed4(dotNL,dotNL,dotNL,1);
                        break;
                    }
                    travelDistance += d;
                }
				return ret;
            }

            v2f vert (appdata v)
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
                // 相似三角形，根据深度计算出当前渲染位置与相机之间的实际距离，然后与sdf比较得出前后关系
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture,i.uv).r);
                depth *= length(i.ray.xyz);

                fixed3 color = tex2D(_MainTex,i.uv);
                fixed4 add = rayMarching(rayOrigin,rayDir,depth);
                return fixed4(color*(1.0-add.w)+add.xyz*add.w,1.0);
            }
            ENDCG
        }
    }
}
