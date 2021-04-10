// Infinite Plane
float sdPlane(float3 p, float4 n)
{
	return dot(p, n.xyz) + n.w;
}

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

// b: size of box in x/y/z
float sd2DBox(in float2 p, in float2 b)
{
	float2 d = abs(p) - b;
	return length(max(d, float2(0, 0))) + min(max(d.x, d.y), 0.0);
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
//float pMod1 (inout float p, float size)
//{
//	float halfsize = size * 0.5;
//	float c = floor((p+halfsize)/size);
//	p = fmod(p+halfsize,size)-halfsize;
//	p = fmod(-p+halfsize,size)-halfsize;
//	return c;
//}

// Mod function for infinite fractal
float pMod(float p, float size)
{
	float halfsize = size * 0.5;
	float c = floor((p + halfsize) / size);
	p = fmod(p + halfsize, size) - halfsize;
	p = fmod(p - halfsize, size) + halfsize;
	return p;
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

// Cross
// s: size of cross
float sdCross(in float3 p, float b)
{
	float da = sd2DBox(p.xy, float2(b, b));
	float db = sd2DBox(p.yz, float2(b, b));
	float dc = sd2DBox(p.zx, float2(b, b));
	return min(da, min(db, dc));
}

//triangle prism
float sdTriPrism(float2 p, float2 h)
{
	p.y = p.y;
	p.y += h.x;
	const float k = sqrt(3.0);
	h.x *= 0.5*k;
	p.xy /= h.x;
	p.x = abs(p.x) - 1.0;
	p.y = p.y + 1.0 / k;
	if (p.x + k * p.y > 0.0) p.xy = float2(p.x - k * p.y, -k * p.x - p.y) / 2.0;
	p.x -= clamp(p.x, -2.0, 0.0);
	float d1 = length(p.xy)*sign(-p.y)*h.x;
	float d2 = -h.y;
	return length(max(float2(d1, d2), 0.0)) + min(max(d1, d2), 0.);
}

//trianglecross
float sdtriangleCross(in float3 p, float2 b)
{
	float da = sdTriPrism(p.xy, float2(b.x, b.y* 0.2));
	float db = sdTriPrism(p.zy, float2(b.x, b.y* 0.2));

	return min(da, db);
}

//pyramid
float sdPyramid(float3 p, float h)
{
	float m2 = h * h + 0.25;

	p.xz = abs(p.xz);
	p.xz = (p.z > p.x) ? p.zx : p.xz;
	p.xz -= 0.5;

	float3 q = float3(p.z, h*p.y - 0.5*p.x, h*p.x + 0.5*p.y);

	float s = max(-q.x, 0.0);
	float t = clamp((q.y - 0.5*p.z) / (m2 + 0.25), 0.0, 1.0);

	float a = m2 * (q.x + s)*(q.x + s) + q.y*q.y;
	float b = m2 * (q.x + 0.5*t)*(q.x + 0.5*t) + (q.y - m2 * t)*(q.y - m2 * t);

	float d2 = min(q.y, -q.x*m2 - q.y*0.5) > 0.0 ? 0.0 : min(a, b);

	return sqrt((d2 + q.z*q.z) / m2) * sign(max(q.z, -p.y));
}

// Menger sponge
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

// Merger piramid
float2 sdMergerPyr(in float3 p, float b, int _iterations, float3 _modOffsetPos, float4x4 _iterationTransform, float4x4 _globalTransform, float _smoothRadius, float _scaleFactor, float4x4 rotate45)
{
	b = 2 * b;
	p = mul(_globalTransform, float4(p, 1)).xyz;

	float2 d = float2(sdPyramid(p / b, sqrt(3) / 2) * b, 0);

	float s = 1.0;
	for (int m = 0; m < _iterations; m++)
	{
		p = mul(_iterationTransform, float4(p, 1)).xyz;
		p.x = pMod(p.x, b*_modOffsetPos.x * 0.5 / s);
		p.y = pMod(p.y, b*_modOffsetPos.y * (sqrt(3) / 2) / s);
		p.z = pMod(p.z, b*_modOffsetPos.z * 0.5 / s);

		s *= _scaleFactor * 2;
		float3 r = (p)*s;
		float c = (sdtriangleCross(float3(r.x, -r.y, r.z), b / sqrt(3))) / s;

		if (-c > d.x)
		{
			d.x = -c;
			d = float2(d.x, m);

		}
	}
	return d;
}
