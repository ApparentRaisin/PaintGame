Shader "Custom/Outline"
{   
    Properties
    {
        _DepthNormalThreshold("Threshold",float) = 1
        _DepthNormalThresholdScale("Scale",float) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float _DepthNormalThreshold;
            float _DepthNormalThresholdScale;

            
            TEXTURE2D(_ColorLookup);
            TEXTURE2D(_LayerTexture);

            float3 GetColor(float v){
                v = (v*255.);
                return (SAMPLE_TEXTURE2D(_ColorLookup, sampler_PointClamp, float2((v/16.) + 0.5,(v%16.) + 0.5)/16.));
            }

            float4 _MouseData;
            TEXTURE2D(_BrushTexture);

            float _PlayerDrawing[8];

            float _Player;

            float4 Frag (Varyings input) : SV_Target
            {
                float halfScaleFloor = floor(2 * 0.5);
                float halfScaleCeil = ceil(2 * 0.5);

                float2 bottomLeftUV = input.texcoord - float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y) * halfScaleFloor;
                float2 topRightUV = input.texcoord + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y) * halfScaleCeil;  
                float2 bottomRightUV = input.texcoord + float2(_CameraDepthTexture_TexelSize.x * halfScaleCeil, -_CameraDepthTexture_TexelSize.y * halfScaleFloor);
                float2 topLeftUV = input.texcoord + float2(-_CameraDepthTexture_TexelSize.x * halfScaleFloor, _CameraDepthTexture_TexelSize.y * halfScaleCeil);

                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;

                float depth0 = (SampleSceneDepth(bottomLeftUV)) - (SampleSceneDepth(topRightUV));
                float depth1 = (SampleSceneDepth(bottomRightUV)) - (SampleSceneDepth(topLeftUV));

                float edgeDepth = sqrt(pow(depth0, 2) + pow(depth1, 2)) * 100;
                
                float3 normals0 = (SampleSceneNormals(bottomLeftUV)) - (SampleSceneNormals(topRightUV));
                float3 normals1 = (SampleSceneNormals(bottomRightUV)) - (SampleSceneNormals(topLeftUV));
                
                float edgeNormal = sqrt(dot(normals0, normals0) + dot(normals1, normals1));

                float3 normal = SampleSceneNormals(input.texcoord);
                float3 pos = float3(input.texcoord.xy*2.-1.,1);
                float4 p = pos.xyzz;
                float3 ray = mul(unity_CameraInvProjection, p.xyzz ).xyz;
                ray.xy *= -1;
                ray.z = 1;
                ray = mul(unity_CameraToWorld, ray.xyz);

                float NdotV = dot(ray, normal*2-1);
                float normalThreshold01 = saturate((NdotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold));
                float normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1;
                
                float blue0 = (SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp,bottomLeftUV).b) - (SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp,topRightUV).b);
                float blue1 = (SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp,bottomRightUV).b) - (SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp,topLeftUV).b);
                
                float edgeBlue = sqrt(pow(blue0, 2) + pow(blue1, 2)) * 10 * (1-color.a);
                
                if(color.a < 0.5){
                    color = float4(GetColor(color.r),1);
                }

                if(_PlayerDrawing[_Player] > 0.5){
                    float2 mouse = _MouseData.xy/_ScreenParams.xy;

                    mouse -= input.texcoord - float2(1/_MouseData.w,1/_MouseData.w)*float2(_ScreenParams.y/_ScreenParams.x,1)*0.5;
                    mouse.x *= (_ScreenParams.x/_ScreenParams.y);
                    mouse *= _MouseData.w;
                
                    color = lerp(color, 0.5, SAMPLE_TEXTURE2D(_BrushTexture,sampler_PointClamp,mouse).a * 0.8); 
                }
                
                return color - max(max(edgeDepth > normalThreshold * 0.9, edgeNormal > 0.9), edgeBlue > 0.01);
            }
            ENDHLSL
        }
    }
}
