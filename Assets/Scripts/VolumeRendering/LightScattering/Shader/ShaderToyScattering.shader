Shader "ShaderToyScattering"{
    Properties
	{
		_MainTex("Texture", 2D) = "white" {}
        _NoiseTex("Noise ",2D) = "white" {}
        _LightPos("Light Pos",vector) = (0,0,0,0)
	}

    SubShader{
		Cull Off

        CGINCLUDE
        #include "UnityCG.cginc"
        #define D_DEMO_FREE

        #ifdef D_DEMO_FREE
            // 高度雾噪波
            #define D_FOG_NOISE 1.0

            // Height fog multiplier to show off improvement with new integration formula
            #define D_STRONG_FOG 0.05

            // Enable/disable volumetric shadow (single scattering shadow)
            #define D_VOLUME_SHADOW_ENABLE 1

            // Use imporved scattering?
            // In this mode it is full screen and can be toggle on/off.
            #define D_USE_IMPROVE_INTEGRATION 1

        //
        // Pre defined setup to show benefit of the new integration. Use D_DEMO_FREE to play with parameters
        //
        #elif defined(D_DEMO_SHOW_IMPROVEMENT_FLAT)
            #define D_STRONG_FOG 10.0
            #define D_FOG_NOISE 0.0
            #define D_VOLUME_SHADOW_ENABLE 1
        #elif defined(D_DEMO_SHOW_IMPROVEMENT_NOISE)
            #define D_STRONG_FOG 5.0
            #define D_FOG_NOISE 1.0
            #define D_VOLUME_SHADOW_ENABLE 1
        #elif defined(D_DEMO_SHOW_IMPROVEMENT_FLAT_NOVOLUMETRICSHADOW)
            #define D_STRONG_FOG 10.0
            #define D_FOG_NOISE 0.0
            #define D_VOLUME_SHADOW_ENABLE 0
        #elif defined(D_DEMO_SHOW_IMPROVEMENT_NOISE_NOVOLUMETRICSHADOW)
            #define D_STRONG_FOG 3.0
            #define D_FOG_NOISE 1.0
            #define D_VOLUME_SHADOW_ENABLE 0
        #endif



        /*
        * Other options you can tweak
        */

        // Used to control wether transmittance is updated before or after scattering (when not using improved integration)
        // If 0 strongly scattering participating media will not be energy conservative
        // If 1 participating media will look too dark especially for strong extinction (as compared to what it should be)
        // Toggle only visible zhen not using the improved scattering integration.
        #define D_UPDATE_TRANS_FIRST 1

        // Apply bump mapping on walls
        #define D_DETAILED_WALLS 0

        // Use to restrict ray marching length. Needed for volumetric evaluation.
        #define D_MAX_STEP_LENGTH_ENABLE 1

        // Light position and color
        #define LPOS float3( 20.0+15.0*sin(_Time.y), 15.0+12.0*cos(_Time.y),-20.0+12.0*sin(_Time.y))
        #define LCOL (600.0*float3( 1.0, 0.7, 0.3))

        float4x4 _FrustumCornersEyeSpace;
        float4 _MainTex_TexelSize;
        float4x4 _CameraInvViewMatrix;
        float4x4 _CameraTransform;
        float3 _CameraWorldPos;
        sampler2D _MainTex;
        sampler2D _NoiseTex;
        float3 _LightPos;

        struct appdata{
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct v2f{
            float4 vertex : SV_POSITION;
            float4 screenPos : TEXCOORD0;
            float4 worldPos : TEXCOORD1;
            float3 worldNormal : TEXCOORD2;
            float2 uv : TEXCOORD3;
        };

        v2f vert(appdata i ){
            v2f o;
            float index = i.vertex.z;
            i.vertex.z = 1.0;
            o.vertex = UnityObjectToClipPos(i.vertex);
            o.screenPos = ComputeScreenPos(o.vertex);
            o.worldPos = mul(_CameraInvViewMatrix,_FrustumCornersEyeSpace[(int)index].xyz);
            o.worldNormal = UnityObjectToWorldNormal(i.normal);
            o.uv = i.uv;
            return o;
        }
        
        // 坐标原点位于屏幕中心，x∈[-aspect,aspect] y∈[-1,1]
        float2 ConvertByAspectRatio(float2 coord){
            float aspectRatio = _ScreenParams.y/_ScreenParams.x;
            return float2(coord.x*2-1,coord.y*2*aspectRatio-1);
        }
        ENDCG

		Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            float displacementSimple(float2 p )
            {
                float f;
                f  = 0.5000* tex2D( _NoiseTex, p ).x; p = p*2.0;
                f += 0.2500* tex2D( _NoiseTex, p ).x; p = p*2.0;
                f += 0.1250* tex2D( _NoiseTex, p ).x; p = p*2.0;
                f += 0.0625* tex2D( _NoiseTex, p ).x; p = p*2.0;
                
                return f;
            }


            float3 getSceneColor(float3 p, float material)
            {
                if(material==1.0)
                {
                    return float3(1.0, 0.5, 0.5);
                }
                else if(material==2.0)
                {
                    return float3(0.5, 1.0, 0.5);
                }
                else if(material==3.0)
                {
                    return float3(0.5, 0.5, 1.0);
                }
                
                return float3(0.0, 0.0, 0.0);
            }


            float getClosestDistance(float3 p, out float material)
            {
                float d = 0.0;

            #if D_MAX_STEP_LENGTH_ENABLE
                float minD = 1; // restrict max step for better scattering evaluation
            #else
                float minD = 10000000.0;
            #endif

                material = 0.0;
                
                float yNoise = 0.0;
                float xNoise = 0.0;
                float zNoise = 0.0;

            #if D_DETAILED_WALLS
                yNoise = 1.0*clamp(displacementSimple(p.xz*0.005),0.0,1.0);
                xNoise = 2.0*clamp(displacementSimple(p.zy*0.005),0.0,1.0);
                zNoise = 0.5*clamp(displacementSimple(p.xy*0.01),0.0,1.0);
            #endif
                
                d = max(0.0, p.y - yNoise);
                if(d<minD)
                {
                    minD = d;
                    material = 2.0;
                }
                
                d = max(0.0,p.x - xNoise);
                if(d<minD)
                {
                    minD = d;
                    material = 1.0;
                }
                
                d = max(0.0,40.0-p.x - xNoise);
                if(d<minD)
                {
                    minD = d;
                    material = 1.0;
                }
                
                d = max(0.0,-p.z - zNoise);
                if(d<minD)
                {
                    minD = d;
                    material = 3.0;
                }
                
                return minD;
            }


            float3 calcNormal( in float3 pos)
            {
                float material = 0.0;
                float3 eps = float3(0.3,0.0,0.0);
                return normalize( float3(
                    getClosestDistance(pos+eps.xyy, material) - getClosestDistance(pos-eps.xyy, material),
                    getClosestDistance(pos+eps.yxy, material) - getClosestDistance(pos-eps.yxy, material),
                    getClosestDistance(pos+eps.yyx, material) - getClosestDistance(pos-eps.yyx, material) ) );

            }

			// 光的距离平方衰减
            float3 evaluateLight(in float3 pos)
            {
                float3 lightPos = LPOS;
                float3 lightCol = LCOL;
                float3 L = lightPos-pos;
                return lightCol * 1/dot(L,L);
            }

			// 光的入射角度衰减（与表面夹角越大越少）
            float3 evaluateLight(in float3 pos, in float3 normal)
            {
                float3 lightPos = LPOS;
                float3 L = lightPos-pos;
                float distanceToL = length(L);
                float3 Lnorm = L/distanceToL;
                return max(0.0,dot(normal,Lnorm)) * evaluateLight(pos);
            }

            // To simplify: wavelength independent scattering and extinction
            void getParticipatingMedia(out float sigmaS, out float sigmaE, in float3 pos)
            {
                //float heightFog = 7.0 + D_FOG_NOISE*3.0*clamp(displacementSimple(pos.xz*0.005 + _Time.y*0.01),0.0,1.0);
				float heightFog = 7.0 + D_FOG_NOISE * 3.0 * _SinTime.z*3;
                heightFog = 0.3*clamp((heightFog-pos.y)*1.0, 0.0, 1.0);
                
                const float fogFactor = 1.0 + D_STRONG_FOG * 5.0;
                
                const float sphereRadius = 5.0;
                float sphereFog = clamp((sphereRadius-length(pos-float3(20.0,19.0,-17.0)))/sphereRadius, 0.0,1.0);
                
                const float constantFog = 0.02;

                sigmaS = constantFog + heightFog*fogFactor + sphereFog;
            
                const float sigmaA = 0.0;
                sigmaE = max(0.000000001, sigmaA + sigmaS); // to avoid division by zero extinction
            }

            float phaseFunction()
            {
                return 1.0/(4.0*3.14);
            }

            float volumetricShadow(in float3 from, in float3 to)
            {
            #if D_VOLUME_SHADOW_ENABLE
                const float numStep = 16.0; // quality control. Bump to avoid shadow alisaing
                float shadow = 1.0;
                float sigmaS = 0.0;
                float sigmaE = 0.0;
                float dd = length(to-from) / numStep;
                for(float s=0.5; s<(numStep-0.1); s+=1.0)// start at 0.5 to sample at center of integral part
                {
                    float3 pos = from + (to-from)*(s/(numStep));
                    getParticipatingMedia(sigmaS, sigmaE, pos);
                    shadow *= exp(-sigmaE * dd);
                }
                return shadow;
            #else
                return 1.0;
            #endif
            }
            void traceScene(bool improveScat,float3 rO,float3 rD,inout float3 finalPos,inout float3 normal,inout float3 albedo,inout float4 scatTrans){
                const int numIter = 256;
                float sigmaS = 0.0;
                float sigmaE = 0.0;
                
                float3 lightPos = LPOS;
                 
                // Initialise volumetric scattering integration (to view)
                float transmittance = 1.0;
                float3 scatteredLight = float3(0.0, 0.0, 0.0);
                
                float d = 0.0; // hack: always have a first step of 1 unit to go further
                float material = 0.0;
                float3 p = float3(0.0, 0.0, 0.0);
                float dd = 0.0;
                for(int i=0; i<numIter;++i)
                {
                    float3 p = rO + d*rD;
                    
                    
                    getParticipatingMedia(sigmaS, sigmaE, p);
                    
            #ifdef D_DEMO_FREE
                    if(D_USE_IMPROVE_INTEGRATION>0) // freedom/tweakable version
            #else
                    if(improveScat)
            #endif
                    {
                        // See slide 28 at http://www.frostbite.com/2015/08/physically-based-unified-volumetric-rendering-in-frostbite/
                        float3 S = evaluateLight(p) * sigmaS * phaseFunction()* volumetricShadow(p,lightPos);// incoming light
                        float3 Sint = (S - S * exp(-sigmaE * dd)) / sigmaE; // integrate along the current step segment
                        scatteredLight += transmittance * Sint; // accumulate and also take into account the transmittance from previous steps

                        // Evaluate transmittance to view independentely
                        transmittance *= exp(-sigmaE * dd);
                    }
                    else
                    {
                        // Basic scatering/transmittance integration
                    #if D_UPDATE_TRANS_FIRST
                        transmittance *= exp(-sigmaE * dd);
                    #endif
                        scatteredLight += sigmaS * evaluateLight(p) * phaseFunction() * volumetricShadow(p,lightPos) * transmittance * dd;
                    #if !D_UPDATE_TRANS_FIRST
                        transmittance *= exp(-sigmaE * dd);
                    #endif
                    }
                    
                    
                    dd = getClosestDistance(p, material);
                    if(dd<0.01)
                        break; // give back a lot of performance without too much visual loss
                    d += dd;
                }
                
                albedo = getSceneColor(p, material);
                
                finalPos = rO + d*rD;
                
                normal = calcNormal(finalPos);
                
                scatTrans = float4(scatteredLight, transmittance);                
            }

            float4 frag(v2f i) : SV_TARGET{
                float2 coord = ConvertByAspectRatio(i.screenPos.xy / i.screenPos.w);
                // setup cam
                float3 camPos = mul(_CameraTransform,float4(0,0,0,1)).xyz;
                float3 camX = normalize(_CameraTransform[0].xyz);
                float3 camY = normalize(_CameraTransform[1].xyz);
                float3 camZ = normalize(_CameraTransform[2].xyz);
                // ray marching arguments
                float3 rO = camPos;
                float3 rD = normalize(coord.x*camX + coord.y*camY+camZ);
                float3 finalPos = rO;
                float3 albedo = float3(0,0,0);
                float3 normal = float3(0,0,0);
                float4 scatTrans = float4(0,0,0,0);
				 
                // trace scene
                traceScene(true,rO, rD, finalPos, normal, albedo, scatTrans);
                // lighting
                float3 color = (albedo/3.14) * evaluateLight(finalPos, normal) * volumetricShadow(finalPos, LPOS);
                // apply scat/transmittance
                color = color * scatTrans.w + scatTrans.xyz;
                // gamma correction
                color = pow(color, float3(1.0/2.2,1.0/2.2,1.0/2.2));
                //abs(i.screenPos.x - 0.5) < 0.1
                // if(abs(i.screenPos.x - 0.5) < 0.001){
                //     color = float4(1,0,0,1);
                // }
                return float4(color,1.0);
            }
            ENDCG
        }
 
        // GrabPass
        // {
        //     "_BackgroundTexture1"
        // }

        // Pass{
        //     CGPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     sampler2D _BackgroundTexture1;
        //     float4 frag(v2f i) : SV_TARGET{
        //         float2 coord = ConvertByAspectRatio(i.screenPos.xy / i.screenPos.w);
        //         if(length(coord - float2(0.4,0)) < 0.2){
        //             return float4(0,1,0,1);
        //         }
        //         return tex2D(_BackgroundTexture1,i.uv);
        //     }
        //     ENDCG
        // }

        // GrabPass
        // {
        //     "_BackgroundTexture2"
        // }

        // Pass{
        //     CGPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     sampler2D _BackgroundTexture2;
        //     float4 frag(v2f i) : SV_TARGET{
        //         float2 coord = ConvertByAspectRatio(i.screenPos.xy / i.screenPos.w);
        //         if(length(coord - float2(0.8,0)) < 0.2){
        //             return float4(0,0,1,1);
        //         }
        //         return tex2D(_BackgroundTexture2,i.uv);
        //     }
        //     ENDCG
        // }
    }
}