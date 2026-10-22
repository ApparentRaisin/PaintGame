Shader "Custom/PaintShader"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 positionOS : TEXCOORD1;
            };

            TEXTURE2D(_CurrentPaintingTexture);
            SAMPLER(sampler_CurrentPaintingTexture);

            TEXTURE2D_ARRAY(_TextureArray);
            TEXTURE2D(_ColorLookup);

            TEXTURE2D(_BrushTexture);
            float4 _MouseData;

            struct CameraData{
                float4x4 _CameraTransform;
                float4x4 _FrustrumMatrix;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CurrentPaintingTexture_TexelSize;
                float4 _TextureArray_TexelSize;

            CBUFFER_END

            StructuredBuffer<CameraData> _CameraDataBuffer;
            int _TextureCount;
            int _IsDrawing;
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS;
                return OUT;
            }

            float decode_float(float2 inp){
                float m = inp.g; //base 8, 1/Math.pow(2,8), 1/256
                float e = floor(inp.r * 255.);
                bool i_sign = false;

                // this is if it was a fraction
                if (e >= 128.) { // if 128-255
                  i_sign = true; // inverse e below
                  e -= 128.;
                }
                // this is if it was a negative number
                if (e >= 64.) { // if 64-127 or 192-255, can't be over 128 thought or above would have changed it
                  m = -m; // inverse m
                  e -= 64.; // subtract 64
                }
                if (i_sign)e = -e; //inverse e if was over 128
            
                return m * exp2(e);
            }

            float3 GetColor(float v){
                v = (v*256.);
                return (SAMPLE_TEXTURE2D(_ColorLookup, sampler_PointClamp, float2((v/16.) + 0.5,(v%16.) + 0.5)/16.));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 final = 1;
                for(int i = 0; i < _TextureCount; i++){
                    float4x4 cam = _CameraDataBuffer[i]._CameraTransform;
                    float4 pos = mul(_CameraDataBuffer[i]._FrustrumMatrix,mul(cam,mul(unity_ObjectToWorld, float4(IN.positionOS.xyz,1))));
                
                    float4 currentScreenPos = ComputeScreenPos(pos);
                    float2 sc = currentScreenPos.xy/currentScreenPos.w;
                    
                    #if UNITY_UV_STARTS_AT_TOP
                        sc.y = 1-sc.y;
                    #endif
                    
                    half4 color = SAMPLE_TEXTURE2D_ARRAY(_TextureArray, sampler_PointClamp, sc.xy, i);
                    float depth = (LinearEyeDepth(decode_float(color.ba), _ZBufferParams) - (currentScreenPos.z));
                    //depth += fwidth(depth);
                    float f = 0;
                    if(sc.y > _TextureArray_TexelSize.y*2 && sc.y < 1-_TextureArray_TexelSize.y*2 && sc.x > _TextureArray_TexelSize.x*2 && sc.x < 1 -_TextureArray_TexelSize.x*2 
                        && currentScreenPos.z > 0 && depth > 0)
                    {
                        final.rgb = lerp(final.rgb, GetColor(color.r), color.g);//float4(sc.xy,0,1);
                    }
                }

                if(_IsDrawing > 0.5){
                    float4 pos = mul(_CameraDataBuffer[_TextureCount]._FrustrumMatrix,mul(_CameraDataBuffer[_TextureCount]._CameraTransform,mul(unity_ObjectToWorld, float4(IN.positionOS.xyz,1))));
                
                    float4 currentScreenPos = ComputeScreenPos(pos);
                    float2 sc = currentScreenPos.xy/currentScreenPos.w;

                    #if UNITY_UV_STARTS_AT_TOP
                        sc.y = 1-sc.y;
                    #endif
                    //UNITY_SAMPLE_TEX2DARRAY(_MyArr, i.uv);
                    half4 color = SAMPLE_TEXTURE2D(_CurrentPaintingTexture, sampler_PointClamp, sc.xy);
                    float depth = (LinearEyeDepth(decode_float(color.ba), _ZBufferParams) - (currentScreenPos.z));
                    //depth += fwidth(depth);
                    if(sc.y > _CurrentPaintingTexture_TexelSize.y*2 && sc.y < 1-_CurrentPaintingTexture_TexelSize.y*2 && sc.x > _CurrentPaintingTexture_TexelSize.x*2 && sc.x < 1 -_CurrentPaintingTexture_TexelSize.x*2 
                        && currentScreenPos.z > 0 && depth > 0){
                    
                        //color.rg *= depth;
                        final.rgb = lerp(final.rgb, GetColor(color.r), color.g);//float4(sc.xy,0,1);
                    }
    
                    float2 mouse = _MouseData.xy/_ScreenParams.xy;
                    
                    mouse -= sc - float2(1/_MouseData.w,1/_MouseData.w)*float2(_ScreenParams.y/_ScreenParams.x,1)*0.5;
                    mouse.x *= (_ScreenParams.x/_ScreenParams.y);
                    mouse *= _MouseData.w;

                    final = lerp(final, 0.5, SAMPLE_TEXTURE2D(_BrushTexture,sampler_PointClamp,mouse).a * 0.5);
                }
                
                return max(final, fwidth(final));
                
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
