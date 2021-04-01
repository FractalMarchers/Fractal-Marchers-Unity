// Sphere
// s: radius
float sdSphere(float3 p, float s)
{
	return length(p) - s;
}

// Box
// b: size of box in x/y/z
float sdBox(float3 p, float3 b)
{
	float3 d = abs(p) - b;
	return min(max(d.x, max(d.y, d.z)), 0.0) +
		length(max(d, 0.0));
}

// BOOLEAN OPERATORS //

// Union
float opU(float d1, float d2)
{
	return min(d1, d2);
}

// Subtraction
float opS(float d1, float d2)
{
	return max(-d1, d2);
}

// Intersection
float opI(float d1, float d2)
{
	return max(d1, d2);
}

// Mod Position Axis
float pMod1 (inout float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p+halfsize)/size);
	p = fmod(p+halfsize,size)-halfsize;
	p = fmod(-p+halfsize,size)-halfsize;
	return c;
}

// Infinite Plane
float sdPlane(float3 p, float4 n)
{
	return dot(p, n.xyz) + n.w;
}

// Rounded Box
float sdRoundBox(in float3 p, in float3 b, in float r)
{
	float3 q = abs(p) - b;
	return min(max(q.x, max(q.y, q.z)), 0.0) + length(max(q, 0.0)) - r;
}

float4 opUS(float4 d1, float4 d2, float k)
{
	float h = clamp(0.5 + 0.5*(d2.w - d1.w) / k, 0.0, 1.0);
	float3 color = lerp(d2.rgb, d1.rgb, h);
	float dist = lerp(d2.w, d1.w, h) - k * h*(1.0 - h);
	return float4(color, dist);
}

float opSS(float d1, float d2, float k)
{
	float h = clamp(0.5 - 0.5*(d2 + d1) / k, 0.0, 1.0);
	return lerp(d2, -d1, h) + k * h*(1.0 - h);
}

float opIS(float d1, float d2, float k)
{
	float h = clamp(0.5 - 0.5*(d2 - d1) / k, 0.0, 1.0);
	return lerp(d2, d1, h) + k * h*(1.0 - h);
}


//mergerSponge

// InfBox
// b: size of box in x/y/z
float sd2DBox(in float2 p, in float2 b)
{
	float2 d = abs(p) - b;
	return length(max(d, float2(0, 0))) + min(max(d.x, d.y), 0.0);
}

// Cross
// s: size of cross
float sdCross(in float3 p, float b)
{
	float da = sd2DBox(p.xy, float2(b, b));
	float db = sd2DBox(p.yz, float2(b, b));
	float dc = sd2DBox(p.zx, float2(b, b));
	return min(da, min(db, dc));
}

float pMod(float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p + halfsize) / size);
	p = fmod(p + halfsize, size) - halfsize;
	p = fmod(p - halfsize, size) + halfsize;
	return p;
}

float2 sdMerger(in float3 p, float b, int _iterations, float3 _modOffsetPos, float4x4 _iterationTransform, float4x4 _globalTransform, float _smoothRadius, float _scaleFactor)
{

	p = mul(_globalTransform, float4(p, 1)).xyz;


	float2 d = float2(sdBox(p, float3(b - _smoothRadius, b - _smoothRadius, b - _smoothRadius)), 0) - _smoothRadius;

	float s = 1.0;
	for (int m = 0; m < _iterations; m++)
	{
		p = mul(_iterationTransform, float4(p, 1)).xyz;
		p.x = pMod(p.x, b*_modOffsetPos.x * 2 / s);
		p.y = pMod(p.y, b*_modOffsetPos.y * 2 / s);
		p.z = pMod(p.z, b*_modOffsetPos.z * 2 / s);

		s *= _scaleFactor * 3;
		float3 r = (p)*s;
		float c = (sdCross(r, b - _smoothRadius / s) - _smoothRadius) / s;

		if (-c > d.x)
		{
			d.x = -c;
			d = float2(d.x, m);

		}
	}
	return d;
}