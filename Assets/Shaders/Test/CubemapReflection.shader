// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Test/CubemapReflection"{
    Properties{
        _MainTex("Base RGB",2D) = "white"{}
        _Cube("Reflection Map",Cube) = ""{}
		_AmbientColor("Ambient",Color) = (1,1,1,1)
		_ReflAmount("Reflection Amount",Float) = 0.5
		_BBoxMin("Bound box min",vector) = (0,0,0,0)
		_BBoxMax("Bound box max",vector) = (0,0,0,0)
		_EnviCubeMapPos("Cube map pos",vector) = (0,0,0)
	}

		SubShader{

			Pass{
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag
            struct vertexInput {
                float4 vertex:POSITION;
                float3 normal:NORMAL;
                float4 texcoord:TEXCOORD0;
            };

            sampler2D _MainTex;
            samplerCUBE _Cube;
            float4 _AmbientColor;
            float _ReflAmount;
            float3 _BBoxMax;
            float3 _BBoxMin;
			float3 _EnviCubeMapPos;
            
			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 tex:TEXCOORD0;
				float3 vertexInWorld:TEXCOORD1;
				float3 viewDirInWorld :TEXCOORD2;
				float3 normalInWorld:TEXCOORD3;
			};

            float3 LocalCorrect(float3 origVec, float3 bboxMin, float3 bboxMax, float3 vertexPos, float3 cubemapPos)
            {

                // Find the ray intersection with box plane

                float3 invOrigVec = float3(1.0,1.0,1.0)/origVec;

                float3 intersecAtMaxPlane = (bboxMax - vertexPos) * invOrigVec;

                float3 intersecAtMinPlane = (bboxMin - vertexPos) * invOrigVec;

                // Get the largest intersection values (we are not intersted in negative values)

                float3 largestIntersec = max(intersecAtMaxPlane, intersecAtMinPlane);

                // Get the closest of all solutions

                 float Distance = min(min(largestIntersec.x, largestIntersec.y), largestIntersec.z);

                // Get the intersection position

                float3 IntersectPositionWS = vertexPos + origVec * Distance;

                // Get corrected vector

                float3 localCorrectedVec = IntersectPositionWS - cubemapPos;

                return localCorrectedVec;

            }

            vertexOutput vert(vertexInput input){
                vertexOutput output;
                output.tex = input.texcoord;
                // Transform vertex coordinates from local to world.
                float4 vertexWorld = mul(unity_ObjectToWorld,input.vertex);
                // Transform normal to world coordinates.
                float3 normalWorld = UnityObjectToWorldNormal(input.normal);
                // Final vertex output position.   
                output.pos = UnityObjectToClipPos(input.vertex);
                // ----------- Local correction ------------
                output.vertexInWorld = vertexWorld.xyz;
                output.viewDirInWorld = vertexWorld.xyz - _WorldSpaceCameraPos;
                output.normalInWorld = normalWorld.xyz;
                return output;
            }

            float4 frag(vertexOutput input) : COLOR

            {

                float4 reflColor = float4(1, 1, 0, 0);

                // Find reflected vector in WS.

                float3 viewDirWS = normalize(input.viewDirInWorld);

                float3 normalWS = normalize(input.normalInWorld);

                float3 reflDirWS = reflect(viewDirWS, normalWS);

                // Get local corrected reflection vector.

				float3 localCorrReflDirWS = LocalCorrect(reflDirWS, _BBoxMin, _BBoxMax, input.vertexInWorld, _EnviCubeMapPos);

                // Lookup the environment reflection texture with the right vector.

                reflColor = texCUBE(_Cube, localCorrReflDirWS);

                // Lookup the texture color.

                float4 texColor = tex2D(_MainTex, float2(input.tex.xy));
                return float4(reflColor.a,0,0,1);
                return _AmbientColor + texColor * _ReflAmount * reflColor;

            }
            ENDCG
        }
    }
}