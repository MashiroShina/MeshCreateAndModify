Shader "Sandbox/VolumetricLight"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGINCLUDE

		#if SHADOWS_DEPTH || defined(SHADOWS_CUBE)
		
		#endif
		#include "VolumetricShadowLibrary.cginc"
		#include "UnityDeferredLibrary.cginc"

		float4 _LightFinalColor;
		struct appdata
		{
			float4 vertex : POSITION;
		};
		
		float4x4 _WorldViewProj;
		float4x4 _MyLightMatrix0;
		float4x4 _MyWorld2Shadow;
		float3 _CameraForward;
		float4x4 _InvVP;

		// x: scattering coef, y: extinction coef, z: range w: skybox extinction coef
		float4 _VolumetricLight;
        // x: 1 - g^2, y: 1 + g^2, z: 2*g, w: 1/4pi
        float4 _MieG;

		// x: scale, y: intensity, z: intensity offset
		float4 _NoiseData;
        // x: x velocity, y: z velocity
		float4 _NoiseVelocity;
		// x:  ground level, y: height scale, z: unused, w: unused
		float4 _HeightFog;
		//float4 _LightDir;

		float _MaxRayLength;

		int _SampleCount;
		sampler3D _NoiseTexture;
		sampler2D _VolumeRandomTex;
		float4 _CameraDepthTexture_TexelSize;
		struct v2f
		{
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
			float3 wpos : TEXCOORD1;
		};

		v2f vert(appdata v)
		{
			v2f o;
			o.pos = mul(_WorldViewProj, v.vertex);
			o.uv = ComputeScreenPos(o.pos);
			o.wpos = mul(unity_ObjectToWorld, v.vertex);
			return o;
		}


		#define fogFunc(height, intensity) exp(-height * intensity)

		inline float fogFuncIntegret(float2 height, float intensity){
			float2 result = exp(-height * intensity) / (-intensity);
			return result.x - result.y;
		}

		inline float getFog(float3 startPos, float3 endPos, float intensity, float height){
			float3 rayDir = endPos - startPos;
			rayDir = normalize(rayDir);
			float dotValue = dot(rayDir, float3(0,1,0));
		/*	if(abs(dotValue) < 0.002)		//Consider use 
			{
				 float average = dot(float2(rayStartHeight, rayEndHeight), 0.5);
				 float3 fogIntensity = float3(
					 fogFunc(rayStartHeight, intensity),
					 fogFunc(average, intensity),
					 fogFunc(rayEndHeight, intensity)
				 );
				 return dot(fogIntensity, 0.33333333);
			}
			else
			{*/
				return abs(fogFuncIntegret(float2(startPos.y, endPos.y) + height, intensity) / dotValue);
		//	}
		}



		//-----------------------------------------------------------------------------------------
		// GetCascadeWeights_SplitSpheres
		//-----------------------------------------------------------------------------------------
		inline half4 GetCascadeWeights_SplitSpheres(float3 wpos)
		{
			half3 fromCenter0 = wpos.xyz - unity_ShadowSplitSpheres[0].xyz;
			half3 fromCenter1 = wpos.xyz - unity_ShadowSplitSpheres[1].xyz;
			half3 fromCenter2 = wpos.xyz - unity_ShadowSplitSpheres[2].xyz;
			half3 fromCenter3 = wpos.xyz - unity_ShadowSplitSpheres[3].xyz;
			half4 distances2 = half4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

			half4 weights = half4(distances2 < unity_ShadowSplitSqRadii);
			weights.yzw = saturate(weights.yzw - weights.xyz);
			return weights;
		}

		//-----------------------------------------------------------------------------------------
		// GetCascadeShadowCoord
		//-----------------------------------------------------------------------------------------
		inline float3 GetCascadeShadowCoord(float4 wpos, half4 cascadeWeights)
		{
			float3 sc0 = mul(unity_WorldToShadow[0], wpos).xyz;
			float3 sc1 = mul(unity_WorldToShadow[1], wpos).xyz;
			float3 sc2 = mul(unity_WorldToShadow[2], wpos).xyz;
			float3 sc3 = mul(unity_WorldToShadow[3], wpos).xyz;
			
			float3 shadowMapCoordinate = float3(sc0 * cascadeWeights[0] + sc1 * cascadeWeights[1] + sc2 * cascadeWeights[2] + sc3 * cascadeWeights[3]);
#if defined(UNITY_REVERSED_Z)
			float  noCascadeWeights = 1 - dot(cascadeWeights, 1);
			shadowMapCoordinate.z += noCascadeWeights;
#endif
			return shadowMapCoordinate;
		}
		
		UNITY_DECLARE_SHADOWMAP(_CascadeShadowMapTexture);
		
		//-----------------------------------------------------------------------------------------
		// GetLightAttenuation
		//-----------------------------------------------------------------------------------------
		half GetLightAttenuation(float3 wpos)
		{
			half atten = 1;
#if SHADOWS_DEPTH_ON
			// sample cascade shadow map
			float4 cascadeWeights = GetCascadeWeights_SplitSpheres(wpos);
		//	float2 weightSum = cascadeWeights.xy + cascadeWeights.zw;
		//	weightSum.x += weightSum.y;
			float3 samplePos = GetCascadeShadowCoord(float4(wpos, 1), cascadeWeights);
			atten = UNITY_SAMPLE_SHADOW(_CascadeShadowMapTexture, samplePos.xyz);
#endif
			return atten;
		}

        //-----------------------------------------------------------------------------------------
        // ApplyHeightFog
        //-----------------------------------------------------------------------------------------
        inline void ApplyHeightFog(float3 wpos, inout half density)
        {
            density *= exp(-(wpos.y + _HeightFog.x) * _HeightFog.y);
			//density *= -2 * exp(-(wpos.y + _HeightFog.x) * _HeightFog.y)
        }

        //-----------------------------------------------------------------------------------------
        // GetDensity
        //-----------------------------------------------------------------------------------------
		float GetDensity(float3 wpos)
		{
            float density = 1;
			float noise = tex3D(_NoiseTexture, frac(wpos * _NoiseData.x + float3(_Time.y * _NoiseVelocity.x, 0, _Time.y * _NoiseVelocity.y)));
			noise = saturate(noise - _NoiseData.z) * _NoiseData.y;
			density = saturate(noise);
            return density;
		}        

		//-----------------------------------------------------------------------------------------
		// MieScattering
		//-----------------------------------------------------------------------------------------
		#define MieScattering(cosAngle, g) g.w * (g.x / (pow(g.y - g.z * cosAngle, 1.5)))
		#define random(seed) sin(seed * half2(641.5467987313875, 3154.135764) + half2(1.943856175, 631.543147))
		#define highQualityRandom(seed) cos(sin(seed * half2(641.5467987313875, 3154.135764) + half2(1.943856175, 631.543147)) * half2(4635.4668457, 84796.1653) + half2(6485.15686, 1456.3574563))
		half2 _RandomNumber;
		//-----------------------------------------------------------------------------------------
		// RayMarch
		//-----------------------------------------------------------------------------------------
		half3 RayMarch(half2 screenPos, float3 rayStart, float3 final, float3 rayDir)
		{
			float4 vlight = 0;

			float cosAngle;
#if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
			cosAngle = dot(_LightDir.xyz, -rayDir);
#else
			// we don't know about density between camera and light's volume, assume 0.5
#endif
			//half3 final = rayStart + rayDir * rayLength;
			half3 step = 1.0 / _SampleCount;
			step.yz *= half2(0.25, 0.2);
			half2 seed = random((_ScreenParams.y * screenPos.y + screenPos.x) * _ScreenParams.x + _RandomNumber);
			[loop]
			for (half i = step.x; i < 1; i += step.x)
			{
				seed = random(seed);
				half lerpValue = i + seed.y* step.y + seed.x * step.z;
				float3 currentPosition = lerp(rayStart, final, lerpValue);
				half atten = GetLightAttenuation(currentPosition);
#ifdef HEIGHT_FOG
		ApplyHeightFog(currentPosition, atten);
#endif
				vlight += atten;				
			}

#if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
			// apply phase function for dir light
			vlight *= MieScattering(cosAngle, _MieG);
#endif

			// apply light's color
			vlight *= _LightFinalColor;

			vlight = max(0, vlight);
			return vlight;
		}

		ENDCG
		Pass
		{
			ZTest Off
			Cull Off
			ZWrite Off
			Blend off

			CGPROGRAM

			#pragma vertex vertDir
			#pragma fragment fragDir
			#pragma target 4.0

			#define UNITY_HDR_ON

			#pragma shader_feature HEIGHT_FOG
			#pragma shader_feature NOISE
			#pragma multi_compile SHADOWS_DEPTH_OFF SHADOWS_DEPTH_ON
			#pragma shader_feature DIRECTIONAL_COOKIE
			#pragma shader_feature DIRECTIONAL

			#ifdef SHADOWS_DEPTH
			
			#endif

			struct VSInput
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint vertexId : SV_VertexID;
			};

			struct PSInput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			float2 _JitterOffset;			
			PSInput vertDir(VSInput i)
			{
				PSInput o;

				o.pos = UnityObjectToClipPos(i.vertex);
				o.uv = i.uv;

				return o;
			}
		
			half3 fragDir(PSInput i) : SV_Target
			{
				float2 uv = i.uv;
				float2 randomOffset = highQualityRandom((_ScreenParams.y * uv.y + uv.x) * _ScreenParams.x + _RandomNumber) * _JitterOffset;
				uv += randomOffset;
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				float4 wpos = mul(_InvVP, float4(uv * 2 -1,depth, 1));
				wpos /= wpos.w;
				float3 rayDir = wpos.xyz - _WorldSpaceCameraPos;
				float rayLength = length(rayDir);
				rayDir /= rayLength;
				rayLength = min(rayLength, _MaxRayLength);
				float3 final = _WorldSpaceCameraPos + rayDir * rayLength;
				half3 color = RayMarch(uv, _WorldSpaceCameraPos, final, rayDir);

				if (Linear01Depth(depth) > 0.9999)
				{
					color *= _VolumetricLight.w;
				}
				return color;
			}
			ENDCG
		}
	}
}