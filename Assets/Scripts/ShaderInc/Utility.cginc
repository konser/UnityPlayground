#ifndef UTILITY_H
#define UTILITY_H

#define PI 3.1415926535897932384626433832795
#define INV_PI 0.3183098861837907

// Math
// -------------------------------------------------------------------------------------------------------------------
float modc(float a,float b) {
	return a - b * floor(a / b);
}

float2 modc(float2 a, float2 b) {
	return a - b * floor(a / b);
}

float3 modc(float3 a, float3 b) {
	return a - b * floor(a / b);
}

float4 modc(float4 a, float4 b) {
	return a - b * floor(a / b);
}


// Custom Geometry
// -------------------------------------------------------------------------------------------------------------------
struct Plane
{
	float3 normal;
	float distance;
};

struct Ray
{
	float3 origin;
	float3 direction;
};

struct Sphere
{
	float3 center;
	float radius;
};

struct Capsule
{
	float3 pos1;
	float3 pos2;
	float radius;
};

struct Box
{
	float3 center;
	Plane planes[6];
};

struct AABB
{
	float3 center;
	float3 extents;
};

// Help Functions
// -------------------------------------------------------------------------------------------------------------------
float3 GetObjectPosition() { return float3(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3]); }
float3 GetObjectForward() { return normalize(-unity_ObjectToWorld[2].xyz); }
float3 GetObjectUp() { return normalize(unity_ObjectToWorld[1].xyz); }
float3 GetObjectRight() { return normalize(unity_ObjectToWorld[0].xyz); }

float3 GetCameraPosition() { return _WorldSpaceCameraPos; }
float3 GetCameraForward() { return -UNITY_MATRIX_V[2].xyz; }
float3 GetCameraUp() { return UNITY_MATRIX_V[1].xyz; }
float3 GetCameraRight() { return UNITY_MATRIX_V[0].xyz; }
float  GetCameraFocalLength() { return abs(UNITY_MATRIX_P[1][1]); }

float2 GetScreenSize() { return _ScreenParams.xy; }
float2 GetPixelSize() { return _ScreenParams.zw - 1.0; }
float  GetAspectRatio() { return _ScreenParams.x / _ScreenParams.y; }

// screen_pos.x must be multiplies by aspect ratio
Ray GetCameraRay(float2 screen_pos)
{
	float3 cam_pos = GetCameraPosition();
	float3 cam_forward = GetCameraForward();
	float3 cam_up = GetCameraUp();
	float3 cam_right = GetCameraRight();
	float  cam_focal_len = GetCameraFocalLength();
	float3 cam_ray = normalize(cam_right * screen_pos.x + cam_up * screen_pos.y + cam_forward * cam_focal_len);
	Ray r = { _WorldSpaceCameraPos.xyz , cam_ray };
	return r;
}

bool IsOverlaped(float3 pos, AABB aabb, float r)
{
	float3 wext = aabb.extents + r;
	float3 rpos = pos - aabb.center;
	rpos = abs(rpos);
	if (rpos.x > wext.x || rpos.y > wext.y || rpos.z > wext.z)
	{
		return false;
	}
	return true;
}


struct DistanceData
{
	float3 direction;
	float distance;
};


DistanceData DistancePointSphere(float3 ppos, Sphere shape)
{
	float3 diff = ppos - shape.center;
	float distance = length(diff) - shape.radius;
	float3 dir = normalize(diff);

	DistanceData ret = { dir, distance };
	return ret;
}


DistanceData DistancePointCapsule(float3 ppos, Capsule shape)
{
	float3 pos1 = shape.pos1;
	float3 pos2 = shape.pos2;
	float3 d = pos2 - pos1;

	float t = dot(ppos - pos1, pos2 - pos1) / dot(d, d);
	float3 diff;
	if (t <= 0.0f) {
		diff = ppos - pos1;
	}
	else if (t >= 1.0f) {
		diff = ppos - pos2;
	}
	else {
		float3 nearest = pos1 + (pos2 - pos1) * t;
		diff = ppos - nearest;
	}
	float distance = length(diff) - shape.radius;
	float3 dir = normalize(diff);

	DistanceData ret = { dir, distance };
	return ret;
}


DistanceData DistancePointBox(float3 ppos, Box shape)
{
	int inside = 0;
	float closest_distance = -999.0f;
	float3 closest_normal;
	float3 rpos = ppos - shape.center;
	for (int p = 0; p < 6; ++p) {
		float3 plane_normal = shape.planes[p].normal;
		float plane_distance = shape.planes[p].distance;
		float distance = dot(rpos, plane_normal) + plane_distance;
		if (distance > closest_distance) {
			closest_distance = distance;
			closest_normal = plane_normal;
		}
	}

	DistanceData ret = { closest_normal, closest_distance };
	return ret;
}

float DistancePointPlane(float3 pos, Plane plane)
{
	return dot(pos, plane.normal) + plane.distance;
}

float3 ProjectToPlane(float3 pos, Plane plane)
{
	float d = DistancePointPlane(pos, plane);
	return pos - d * plane.normal;
}

float3 IntersectionRayPlane(Ray ray, Plane plane)
{
	float t = (-dot(ray.origin, plane.normal) - plane.distance) / dot(plane.normal, ray.direction);
	return ray.origin + ray.direction * t;
}

float ComputeDepth(float4 clippos)
{
#if defined(SHADER_TARGET_GLSL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
	return (clippos.z / clippos.w) * 0.5 + 0.5;
#else
	return clippos.z / clippos.w;
#endif
}

#endif // define UTILITY_H