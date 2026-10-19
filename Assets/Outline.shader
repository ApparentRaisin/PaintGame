Shader "Custom/Outline"
{   
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

            

            float4 Frag (Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;

                float depth = Linear01Depth(SampleSceneDepth(input.texcoord +_CameraDepthTexture_TexelSize.xy*float2(1.5,0)), _ZBufferParams) - 
                Linear01Depth(SampleSceneDepth(input.texcoord-_CameraDepthTexture_TexelSize.xy*float2(1.5,0)), _ZBufferParams);
                depth += Linear01Depth(SampleSceneDepth(input.texcoord +_CameraDepthTexture_TexelSize.xy*float2(0,1.5)), _ZBufferParams) - 
                Linear01Depth(SampleSceneDepth(input.texcoord-_CameraDepthTexture_TexelSize.xy*float2(0,1.5)), _ZBufferParams);

                float3 normal = SampleSceneNormals(input.texcoord);
                float3 pos = float3(input.texcoord.xy*2.-1.,1);
                float4 p = pos.xyzz;
                float3 ray = mul(unity_CameraInvProjection, p.xyzz ).xyz;
                ray.xy *= -1;
                ray = mul(unity_CameraToWorld, ray.xyz);

                float insidance = dot(ray, normal*2-1);
                
                float normalDiff = 1-dot(SampleSceneNormals(input.texcoord +_CameraNormalsTexture_TexelSize.xy*float2(1.5,0)) * 2 -1,
                SampleSceneNormals(input.texcoord-_CameraNormalsTexture_TexelSize.xy*float2(1.5,0)) * 2 -1);

                normalDiff += 1-dot(SampleSceneNormals(input.texcoord +_CameraNormalsTexture_TexelSize.xy*float2(0,1.5)) * 2 -1,
                SampleSceneNormals(input.texcoord-_CameraNormalsTexture_TexelSize.xy*float2(0,1.5)) * 2 -1);

                return  color - (abs(normalDiff) * (Linear01Depth(SampleSceneDepth(input.texcoord),_ZBufferParams) < 1)  + (abs(insidance * depth) > 0.001)) ;
            }
            
            ENDHLSL
        }
    }
}
