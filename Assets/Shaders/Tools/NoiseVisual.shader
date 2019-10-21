Shader "Tools/NoiseVisual"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PseduoNumTex("RNG LUT",2D) = "white"{}
        _SampleIndexX("x",int) = 0
        _SampleIndexY("y",int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            #define TEXTURE_SIZE 512.0
            #define SAMPLE_SIZE 32.0
            #define ONE_PIXEL (1.0/SAMPLE_SIZE)
            #define TWO_PIXEL (2.0/SAMPLE_SIZE)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _PseduoNumTex;
            float4 _MainTex_ST;
            int _SampleIndexX;
            int _SampleIndexY;

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

            float4 fractalSample(float2 uv){
                float scaleFactor = 0.5;
                float4 sum=0;
                for(int x = 0; x < 8; x++){
                    for(int z=0 ;z < 8; z++){
                        sum = sum*0.5 + scaleFactor * sampleAtIndex(x*SAMPLE_SIZE,z*SAMPLE_SIZE,uv);
                    }
                }
                return sum;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 randomNum = sampleAtIndex(_SampleIndexX,_SampleIndexY,i.uv).aaaa;
                return randomNum;
                //return fractalSample(i.uv).aaaa;
            }
            ENDCG
        }
    }
}
