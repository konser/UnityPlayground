#ifndef PROCEDURAL_MODELING_H
#define PROCEDURAL_MODELING_H

#include "UnityStandardCore.cginc"
#include "Utility.cginc"

#ifndef ENABLE_CUSTOM_VERTEX_SHADER

struct IaOut {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
};

struct VsOut {
	float4 vertex : SV_POSITION;
	float4 screenPos : TEXCOORD0;
	float4 worldPos : TEXCOORD1;
	float3 worldNormal : TEXCOORD2;
};

VsOut vert(IaOut i) {
	VsOut o;
	o.vertex = UnityObjectToClipPos(i.vertex);
	o.screenPos = ComputeScreenPos(o.vertex);
	o.worldPos = mul(unity_ObjectToWorld,i.vertex);
	o.worldNormal = mul(unity_ObjectToWorld,float4(i.normal,0.0));
	return o;
}

#endif // ENABLE_CUSTOM_VERTEX_SHADER

sampler2D _BackDepth;
float4 _Position;
float4 _Rotation;
float4 _Scale;
float4 _OffsetPosition;

float4 _SpecularColor;
float _Smoothness;
float _CutoutDistance;
int _Clipping;

#ifndef _LocalTime
    float _LocalTime;
#endif
#ifndef _ObjectID
    float _ObjectID;
#endif

/*
- https://docs.unity3d.com/Manual/RenderTech-DeferredShading.html
The default layout of the render targets (RT0 - RT4) in the geometry buffer (g-buffer) is listed below. 
Data types are placed in the various channels of each render target. The channels used are shown in parentheses.
	RT0, ARGB32 format: Diffuse color (RGB), occlusion (A).
	RT1, ARGB32 format: Specular color (RGB), roughness (A).
	RT2, ARGB2101010 format: World space normal (RGB), unused (A).
	RT3, ARGB2101010 (non-HDR) or ARGBHalf (HDR) format: Emission + lighting + lightmaps + reflection probes buffer.
	Depth+Stencil buffer
*/
struct GBufferOut{
	half4 diffuse : SV_TARGET0;
	half4 specular : SV_TARGET1;
	half4 normal : SV_TARGET2;
	half4 emission : SV_TARGET3;
#if ENABLE_DEPTH_OUTPUT
    float depth :
    #if SHADER_TARGET >= 50
        SV_DepthGreaterEqual;
    #else
        SV_Depth;
    #endif
#endif
};

struct RayMarchData {
	float3 rayPos;
	float numSteps;
	float totalDistance;
	float lastDistance;
};
// sd functions

float sdBox(float3 p, float3 b)
{
    float3 d = abs(p) - b;
    return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
}
float sdSphere(float3 p, float radius)
{
    return length(p) - radius;
}

float sdHex(float2 p, float2 h)
{
    float2 q = abs(p);
    return max(q.x + q.y*0.57735, q.y*1.1547) - h.x;
}

float2 simpleRand(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p)*43758.5453);
}

float3 nrand3(float2 co)
{
    float3 a = frac(cos(co.x*8.3e-3 + co.y)*float3(1.3e5, 4.7e5, 2.9e5));
    float3 b = frac(sin(co.x*0.3e-3 + co.y)*float3(8.1e5, 1.0e5, 0.1e5));
    float3 c = lerp(a, b, 0.5);
    return c;
}

float soft_min(float a, float b, float r)
{
    float e = max(r - abs(a - b), 0);
    return min(a, b) - e*e*0.25 / r;
}

float soft_max(float a, float b, float r)
{
    float e = max(r - abs(a - b), 0);
    return max(a, b) + e*e*0.25 / r;
}

float3 localize(float3 p)
{
    return mul(unity_WorldToObject, float4(p, 1)).xyz * _Scale.xyz + _OffsetPosition.xyz;
}
#endif // PROCEDURAL_MODELING_H