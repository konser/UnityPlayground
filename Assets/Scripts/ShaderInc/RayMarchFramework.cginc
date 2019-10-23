#ifndef RAYMARCH_H
#define RAYMARCH_H

#ifndef MAP_NORMAL
#define MAP_NORMAL Map
#endif


float3 GuessNormal(float3 p){
	float d = 0.001;
	return float3(
		MAP_NORMAL(p + float3(d,0,0)) - MAP_NORMAL(p + float3(-d,0,0)),
		MAP_NORMAL(p + float3(0,d,0)) - MAP_NORMAL(p + float3(0,-d,0)),
		MAP_NORMAL(p + float3(0,0,d)) - MAP_NORMAL(p + float3(0,0,-d))
	);
}

void RayMarching(inout RayMarchData data){
	// rayPos为世界坐标
	float3 rayDir = normalize(data.rayPos - GetCameraPosition());
	float prev = 0.0;
	for(int i=0; i<RAYMARCH_MAX_STEPS;i++){
		prev = data.lastDistance;
		data.lastDistance = Map(data.rayPos);
		data.totalDistance += data.lastDistance;
		data.rayPos += rayDir * data.lastDistance;
		data.numSteps += 1.0;
		if(data.lastDistance < 0.0001){ break; }
	}

	if (_Clipping == 1) {
		float3 pl = localize(data.rayPos);
		float d = sdBox(pl, _Scale * 0.5);
		if (d > _CutoutDistance) { discard; }
	}
	else if (_Clipping == 2) {
		float3 pl = localize(data.rayPos);
		float d = sdSphere(pl, _Scale.x * 0.5);
		if (d > _CutoutDistance) { discard; }
	}
}
 
GBufferOut FragGBuffer(VsOut i){
	float2 coord = i.screenPos.xy / i.screenPos.w;
	float3 worldPos = i.worldPos.xyz;

	RayMarchData data;
	UNITY_INITIALIZE_OUTPUT(RayMarchData,data);
	data.rayPos = worldPos;
	Initialize(data);
	RayMarching(data);

	float3 normal = i.worldNormal;
	if(data.totalDistance > 0){
		normal = GuessNormal(data.rayPos);
	}
	
	GBufferOut O;
	O.diffuse = float4(_Color.rgb, 1.0); 
	O.specular = float4(_SpecularColor.rgb, _Smoothness);
	O.normal = float4(normal * 0.5 + 0.5, 1.0);
	O.emission = float4(_EmissionColor.rgb, 1.0);

#if ENABLE_DEPTH_OUTPUT
	O.depth = ComputeDepth(mul(UNITY_MATRIX_VP, float4(data.rayPos, 1.0)));
#endif

	PostEffect(O, i, data);

#ifndef UNITY_HDR_ON
	O.emission = exp2(-O.emission);
#endif
	return O;
}

struct v2f_shadow {
    float4 pos : SV_POSITION;
    LIGHTING_COORDS(0, 1)
};

v2f_shadow vert_shadow(appdata_full I)
{
    v2f_shadow O;
    O.pos = UnityObjectToClipPos(I.vertex);
    TRANSFER_VERTEX_TO_FRAGMENT(O);
    return O;
}

half4 frag_shadow(v2f_shadow IN) : SV_Target
{
    return 0.0;
}
#endif