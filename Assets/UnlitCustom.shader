Shader "Custom/UnlitCustom"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnlyPass"
            Tags { "LightMode" = "DepthOnly" }

            // Write depth to the depth buffer
            ZWrite On

            // Don't write to the color buffer
            ColorMask 0 

        }

        Pass {
			Name "DepthNormals"
			Tags { "LightMode"="DepthNormals" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex DepthNormalsVertex
			#pragma fragment DepthNormalsFragment

			// Material Keywords
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			// GPU Instancing
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			#if defined(_NORMALMAP)
				#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
			#endif

			struct Attributes {
				float4 positionOS		: POSITION;
				float4 tangentOS		: TANGENT;
				float2 texcoord			: TEXCOORD0;
				float3 normal			: NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS		: SV_POSITION;
				float2 uv 				: TEXCOORD1;
				float3 normalWS			: TEXCOORD2;

				#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
					half4 tangentWS			: TEXCOORD4;    // xyz: tangent, w: sign
				#endif

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings DepthNormalsVertex(Attributes input) {
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangentOS);
				output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);

				#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
					float sign = input.tangentOS.w * float(GetOddNegativeScale());
					half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
					output.tangentWS = tangentWS;
				#endif

				return output;
			}

			float4 DepthNormalsFragment(Varyings input) : SV_TARGET {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				//Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
                
                float3 normalWS = input.normalWS;
				
				return float4((normalWS+1)/2, 0.0);
			}
			ENDHLSL
		}
    }
}
