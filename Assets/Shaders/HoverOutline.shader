Shader "Hidden/HoverOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float2 mouse_pos;
            float4 target_color;
            float4 _ReplaceColor;
            float tolerance;

            Texture2D _FloodFillMask;
            SamplerState sampler_FloodFillMask;
            float4 _FloodFillMask_TexelSize;
            int _OutlineWidth;

            float4 frag(Varyings input) : SV_Target
            {
                float4 col = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float mask = _FloodFillMask.Sample(sampler_FloodFillMask, input.texcoord).r;

                bool is_edge = false;

                if (mask > 0.5)
                {
                    float2 texel_size = _FloodFillMask_TexelSize.xy;

                    [unroll(10)]
                    for (int i = 1; i <= 10; i++)
                    {
                        if (i > _OutlineWidth) break;

                        float left = _FloodFillMask.Sample(sampler_FloodFillMask, input.texcoord + float2(-texel_size.x * i, 0)).r;
                        float right = _FloodFillMask.Sample(sampler_FloodFillMask, input.texcoord + float2(texel_size.x * i, 0)).r;
                        float up = _FloodFillMask.Sample(sampler_FloodFillMask, input.texcoord + float2(0, texel_size.y * i)).r;
                        float down = _FloodFillMask.Sample(sampler_FloodFillMask, input.texcoord + float2(0, -texel_size.y * i)).r;

                        if (left < 0.5 || right < 0.5 || up < 0.5 || down < 0.5)
                        {
                            is_edge = true;
                            break;
                        }
                    }

                    float2 pixelCoord = input.texcoord * _FloodFillMask_TexelSize.zw;
                    float borderThreshold = _OutlineWidth;
                    if (pixelCoord.x < borderThreshold || pixelCoord.x > _FloodFillMask_TexelSize.z - borderThreshold ||
                        pixelCoord.y < borderThreshold || pixelCoord.y > _FloodFillMask_TexelSize.w - borderThreshold)
                    {
                        is_edge = true;
                    }
                }

                if (is_edge)
                    return _ReplaceColor;

                return col;
            }

            ENDHLSL
        }
    }
}
