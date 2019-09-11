Shader "Custom/Lighting/Unlit"
{
	Properties
	{
		_Color("Color",Color) = (1.0,1.0,1.0,1.0)
	}
		SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata {
				float4 vertexPosition : POSITION;
			};
            
			struct v2f {
				float4 position : SV_POSITION;
			};
            
            float4 _Color;

            v2f vert(appdata appData){
                v2f o;
                o.position = UnityObjectToClipPos(appData.vertexPosition);
                return o;
            }

            float4 frag(v2f o):SV_TARGET{
                return _Color;
            }
            ENDCG
        }
    }
}
