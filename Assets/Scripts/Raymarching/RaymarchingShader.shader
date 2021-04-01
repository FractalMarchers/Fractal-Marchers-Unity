Shader "Raymarch/RaymarchingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

            #include "UnityCG.cginc"
			#include "SignedDistanceFunctions.cginc"

			sampler2D _MainTex;

			uniform sampler2D _CameraDepthTexture;
			uniform float4x4 _CamFrustum, _CamToWorld;
			uniform float EPSILON;
			uniform int MAX_ITERATIONS;
			uniform float3 _directionalLight;
			uniform float3 _lightColor;
			uniform float _lightIntensity;
			uniform float _maxDistance;
			uniform fixed4 _mainColor;
			uniform fixed4 _secondaryColor;
			uniform float4 _sphere;
			uniform float4 _box;
			uniform float3 _modInterval;
			uniform float2 _shadowDistance;
			uniform float _shadowIntensity;
			uniform float4x4 _globalTransform;
			uniform float3 _globalPosition;
			uniform float4x4 _iterationTransform;
			uniform float _GlobalScale;
			uniform int sponge_iterations;
			uniform float3 _modOffsetPos;
			uniform int _infinite;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float3 ray: TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
				half index = v.vertex.z;
				v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				o.ray = _CamFrustum[(int)index].xyz;
				o.ray /= abs(o.ray.z);
				o.ray = mul(_CamToWorld, o.ray);
                return o;
            }

			float BoxSphere(float3 pos)
			{
				float dst_sphere = sdSphere(pos - _sphere.xyz, _sphere.w);
				float dst_box = sdRoundBox(pos - _box.xyz, _box.www, 1);
				float combined = opSS(dst_sphere, dst_box, 1);
				return combined;
			}

			float2 SDF(float3 pos)
			{
				if (_infinite == 1) 
				{
					pos.x = pMod(pos.x, _modInterval.x * _GlobalScale * 2);
					pos.y = pMod(pos.y, _modInterval.y * _GlobalScale * 2);
					pos.z = pMod(pos.z, _modInterval.z * _GlobalScale * 2);
				}

				float2 dist = sdMerger(pos, _GlobalScale, sponge_iterations, _modOffsetPos, _iterationTransform, _globalTransform, 0, 1);
				return dist;

				//float modX = pMod1(pos.x, _modInterval.x);
				//float modY = pMod1(pos.y, _modInterval.y);
				//float modZ = pMod1(pos.z, _modInterval.z);

				//float dst_sphere = sdSphere(pos - _sphere.xyz, _sphere.w);
				//float dst_box = sdBox(pos - _box.xyz, _box.www);
				//return opS(dst_sphere, dst_box);
			}

			float3 getNormal(float3 pos)
			{
				const float2 offset = float2(0.001, 0.0);
				float3 normal = float3(SDF(pos + offset.xyy).x - SDF(pos - offset.xyy).x,
										SDF(pos + offset.yxy).x - SDF(pos - offset.yxy).x,
										SDF(pos + offset.yyx).x - SDF(pos - offset.yyx).x);
				return normalize(normal);
			}

			float hardShadows(float ro, float3 rd, float mint, float maxt)
			{
				float res = 1.0;
				float ph = 1e20;
				for (float t = mint; t < maxt; )
				{
					float h = min(SDF(ro + rd * t).x, 0.0);
					if (h < EPSILON)
					{
						return 0.0;
					}
					float y = h * h / (2.0*ph);
					float d = sqrt(h*h - y * y);
					res = min(res, 3*d / max(0.0, t - y));
					ph = h;
					t += h;
				}
				return res;
			}

			float3 shading(float3 pos, float3 normal)
			{
				float result = (_lightColor * dot(normal, -_directionalLight) * 0.5 + 0.5) * _lightIntensity;
				// Shadows
				float shadow = hardShadows(pos, -_directionalLight, _shadowDistance.x, _shadowDistance.y) * 0.5 + 0.5;
				shadow = max(0.0, pow(shadow, _shadowIntensity));
				result *= shadow;
				return result;
			}

			fixed4 raymarch(float3 ro, float3 rd, float depth) 
			{
				fixed4 result = fixed4(1, 1, 1, 1);
				float distance_travelled = 0;
				//int MAX_ITERATIONS = 1024;
				//float EPSILON = 0.01;

				for (int i = 0; i < MAX_ITERATIONS; i++) 
				{
					if (distance_travelled > _maxDistance || distance_travelled >= depth) 
					{
						// Render the environment (skybox)
						//float theta = acos(rd.y) / -PI;
						//float phi = atan2(rd.x, -rd.z) / -PI * 0.5;
						//
						result = fixed4(rd, 0);
						break;
					}
					float3 current_pos = ro + rd * distance_travelled;
					float2 dst = SDF(current_pos);
					if (dst.x < EPSILON)
					{
						// Shading
						float3 normal = getNormal(current_pos);
						float light = max(0, dot(normal, -_directionalLight));

						//result = fixed4(_mainColor.rgb * light, 1);
						// Add secondary color
						result = fixed4(_mainColor.rgb * (sponge_iterations - dst.y) * light / sponge_iterations + _secondaryColor.rgb * dst.y * light / sponge_iterations, 1);

						// TODO: fix Shadows 
						//float3 s = shading(current_pos, normal);
						//result = fixed4(_mainColor.rgb * s, 1);

						break;
					}
					distance_travelled += dst;
				}
				return result;
			}

            fixed4 frag (v2f i) : SV_Target
            {
				float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);
				depth *= length(i.ray);
				fixed3 col = tex2D(_MainTex, i.uv);
				float3 rayDirection = normalize(i.ray.xyz);
				float3 rayOrigin = _WorldSpaceCameraPos;

				

				fixed4 result = raymarch(rayOrigin, rayDirection, depth);
				return fixed4(col * (1.0 - result.w) + result.xyz * result.w, 1.0);
            }
            ENDCG
        }
    }
}
