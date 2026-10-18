Shader "Custom/DrawingSRP"
{   
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "DrawingSRP"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float3 _MouseData;

            float4 Frag (Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                if(_MouseData.z > 0.5){
                    //color += (clamp(distance(input.texcoord,_MouseData.xy),0,0.1));
                }
                return float4(input.texcoord,0,1);//distance(input.texcoord,_MouseData.xy)/10.;
            }
            
            ENDHLSL
        }
    }
}
