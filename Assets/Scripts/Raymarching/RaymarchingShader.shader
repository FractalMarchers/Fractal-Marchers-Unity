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
			uniform fixed4 _marbleColor;
			uniform fixed4 _skyColor;
			uniform float _scaleFactor;
			uniform float3 _modInterval;
			uniform float2 _shadowDistance;
			uniform float _shadowIntensity;
			uniform float _aoIntensity;
			uniform float4x4 _globalTransform;
			uniform float3 _globalPosition;
			uniform float4x4 _iterationTransform;
			uniform float _GlobalScale;
			uniform int sponge_iterations;
			uniform float3 _modOffsetPos;
			uniform int _infinite;
			uniform int _useShadow;
			uniform int _specular;
			uniform int _Ks;
			uniform float3 _marblePos;
			uniform float _smoothRadius;

			uniform float3 _marbleDirection;

			uniform float4x4 rotate45;
			uniform int _shape;

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

			float2 SDF(float3 pos)
			{
				float2 dist_marble = sdMarble(pos, _marblePos);

				if (_infinite == 1) 
				{
					pos.x = pMod(pos.x, _modInterval.x * _GlobalScale * 2);
					pos.y = pMod(pos.y, _modInterval.y * _GlobalScale * 2);
					pos.z = pMod(pos.z, _modInterval.z * _GlobalScale * 2);
				}
				
				float2 dist;

				if (_shape == 0) {
					dist = sdMerger(pos, _GlobalScale, sponge_iterations, _modOffsetPos, _iterationTransform, _globalTransform, _smoothRadius, _scaleFactor);
				}
				else if (_shape == 1) {
					dist = sdMergerPyr(pos, _GlobalScale, sponge_iterations, _modOffsetPos, _iterationTransform, _globalTransform, _smoothRadius, _scaleFactor, rotate45);
				}

				if (dist.x < dist_marble.x) {
					return dist;
				}
				else {
					return dist_marble;
				}
			}

			float3 getNormal(float3 pos)
			{
				const float2 offset = float2(0.001, 0.0);
				float3 normal = float3(SDF(pos + offset.xyy).x - SDF(pos - offset.xyy).x,
										SDF(pos + offset.yxy).x - SDF(pos - offset.yxy).x,
										SDF(pos + offset.yyx).x - SDF(pos - offset.yyx).x);
				return normalize(normal);
			}

			float calculateShadows(in float3 ro, in float3 rd, float mint, float maxt, float k)
			{
				float res = 1.0;
				float ph = 1e20;
				for (float t = mint; t < maxt;)
				{
					float h = SDF(ro + rd * t);
					if (h < EPSILON)
						return 0.0;
					float y = h * h / (2.0 * ph);
					float d = sqrt(h * h - y * y);
					res = min(res, k * d / max(0.0, t - y));
					ph = h;
					t += h;
				}
				return res;
			}

			fixed4 raymarch(float3 ro, float3 rd, float depth) 
			{
				fixed4 result = fixed4(1, 1, 1, 1);
				float distance_travelled = 0;

				for (int i = 0; i < MAX_ITERATIONS; i++) 
				{
					if (distance_travelled > _maxDistance || distance_travelled >= depth) 
					{
						// Render the environment (skybox)
						float theta = acos(rd.y) / -3.14f;
						float phi = atan2(rd.x, -rd.z) / -3.14f * 0.5f;
						
						result = fixed4(rd, 0);
						break;
					}
					float3 current_pos = ro + rd * distance_travelled;
					float2 dst = SDF(current_pos);
					if (dst.x < EPSILON)
					{
						float3 colorDepth;
						float light;
						float shadow;

						float3 normal = getNormal(current_pos);

						// Hit marble
						// Again raymarch from marble to get reflection
						if (dst.y == 100) {
							float3 marble_col = float3(0.0f, 0.0f, 0.0f);
							fixed4 marble_result = fixed4(1, 1, 1, 1);
							float temp_dst_travelled = 0.0f;
							float3 rd_marble = normalize(reflect(rd, normal));
							float3 specular = float3(0.0f, 0.0f, 0.0f);

							for (int j = 0; j < MAX_ITERATIONS; j++) {
								if (temp_dst_travelled > _maxDistance || temp_dst_travelled >= depth) {
									marble_result = fixed4(rd_marble, 0);
									break;
								}
								float3 current_pos_marble = current_pos + rd_marble * temp_dst_travelled;
								float2 dst_marble = SDF(current_pos_marble);

								if (dst_marble.x < EPSILON) {
									marble_col = float3(_mainColor.rgb * (sponge_iterations - dst_marble.y) / sponge_iterations + _secondaryColor.rgb * dst_marble.y / sponge_iterations);
									light = (dot(normal, -_directionalLight) * 0.5 + 0.5) *  _lightIntensity;	// N.L
									
									if (_specular == 1) {
										float3 ref = normalize(2 * _lightIntensity * normal - _directionalLight); // specular
										specular = pow(saturate(dot(ref, rd_marble)), _Ks);
									}

									float temp_ao = (1 - 2 * i / float(MAX_ITERATIONS)) * (1 - _aoIntensity) + _aoIntensity; // ambient occlusion
									float3 marble_colorLight = float3(marble_col * light * 1 * temp_ao);
									
									marble_colorLight = saturate(marble_colorLight + specular + _marbleColor);
									colorDepth = float3(marble_colorLight * (_maxDistance - temp_dst_travelled) / (_maxDistance)+_skyColor.rgb * (temp_dst_travelled) / (_maxDistance));
									marble_result = fixed4(marble_colorLight, 1.0f);
								}
								// collision with fractal
								if (dst_marble.x < EPSILON && temp_dst_travelled <= EPSILON) {
									marble_result = fixed4(0.0f, 0.0f, 0.0f, 1.0f);
									//float3 collision_normal = getNormal(current_pos_marble);
									//_marbleDirection = normalize(reflect(_marbleDirection, collision_normal));
									//_marbleDirection = float3(0, 0, 1);
								}
								temp_dst_travelled += dst_marble;
							}

							distance_travelled += dst.x;
							return marble_result;
						}

						float3 color = float3(_mainColor.rgb * (sponge_iterations - dst.y) / sponge_iterations + _secondaryColor.rgb * dst.y / sponge_iterations);
						light = (dot(normal, -_directionalLight) * 0.5 + 0.5) *  _lightIntensity;

						if (_useShadow == 1) {
							shadow = calculateShadows(current_pos, -_directionalLight, _shadowDistance.x, _shadowDistance.y, 3) * (1 - _shadowIntensity) + _shadowIntensity; // soft
							shadow = pow(shadow, _shadowIntensity);
						}
						else {
							shadow = 1;
						}

						float ao = (1 - 2 * i / float(MAX_ITERATIONS)) * (1 - _aoIntensity) + _aoIntensity; // ambient occlusion

						float3 colorLight = float3 (color * light * shadow * ao);
						colorDepth = float3 (colorLight * (_maxDistance - distance_travelled) / (_maxDistance)+_skyColor.rgb * (distance_travelled) / (_maxDistance));

						result = fixed4(colorLight, 1);
						break;
					}
					distance_travelled += dst.x;
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
