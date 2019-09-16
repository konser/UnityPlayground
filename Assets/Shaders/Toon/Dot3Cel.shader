Shader "Custom/Toon/ShaderX_Dot3Cel"{
    Properties{
        _ColorBandCount("Number of color shades",float) = 0.0
        _HardEdgeFactor("Number of edge",float) = 0.0
        _RenderColor("Render Color",Color) = (1.0,1.0,1.0,1.0)
        _EdgeColor("Edge Color",Color) = (1.0,1.0,1.0,1.0)
        _IntensityAdjust("Intensity",vector) = (0.0,0.0,0.0,0.0)
    }
    SubShader{
        pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _RenderColor;
            float4 _EdgeColor;
            float _Threshold;
            float _HardEdgeFactor;
            float _ColorBandCount;
            float4 _IntensityAdjust;
			struct v2f {
				float4 pos : SV_POSITION;
				float scale : TEXCOORD;
                float3 worldRefl : TEXCOORD1;
			};

            v2f vert(appdata_full input){
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld,input.vertex);
                float3 worldNormal =  UnityObjectToWorldNormal(input.normal);
                float3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                o.pos = UnityObjectToClipPos(input.vertex);
                o.scale = dot(worldNormal,viewDir);
				o.worldRefl = reflect(-viewDir, worldNormal);
                return o;
            }
            
            float4 frag(v2f input):SV_TARGET{
                float4 color;
                // input.scale [0,1] -> scale*bandCount [0,bandCount]
                // 截取整数，相当于根据法线得到的色带索引，值越小越靠近边缘（法线与视线垂直）
                int bandCount = floor(input.scale * _ColorBandCount);
                // 再除以色带数量，得到一个小数，这个值来确定有哪些部分作为边缘来上色
                float bandFactor = bandCount/_ColorBandCount;
                half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0,input.worldRefl);
                half3 skyColor = DecodeHDR(skyData,unity_SpecCube0_HDR);
                if(bandFactor < _HardEdgeFactor/_ColorBandCount){
					color = _EdgeColor;
                }
                // 非边缘部分上色
                else{
                    color = (_RenderColor*bandFactor) + (_RenderColor/_ColorBandCount);
                }
                color.rgb += skyColor.rgb;
				return color + _IntensityAdjust;
            }
            ENDCG
        }
    }
}