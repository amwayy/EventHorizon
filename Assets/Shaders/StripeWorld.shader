Shader "Custom/UnlitStripeWorld"
{
    Properties
    {
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (0,0,0,1)

        _Density ("Stripe Density", Float) = 10
        _Width ("Stripe Width", Range(0.01,0.99)) = 0.5

        // ✅ 用 int 控制轴向（0=X,1=Y,2=Z,3=XZ）
        _AxisMode ("Axis Mode", Float) = 3

        _CustomDir ("Custom Direction", Vector) = (1,0,0,0)
        
        _AnchorWorldPos ("AnchorWorldPos", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
            };

            float4 _ColorA;
            float4 _ColorB;
            float _Density;
            float _Width;
            float _AxisMode;
            float4 _CustomDir;
            float3 _AnchorWorldPos;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            float GetCoord(float3 posWS)
            {
                // Calculate position relative to anchor
                float3 relativePos = posWS - _AnchorWorldPos;

                if (_AxisMode == 0) return relativePos.x;
                if (_AxisMode == 1) return relativePos.y;
                if (_AxisMode == 2) return relativePos.z;
                if (_AxisMode == 3) return dot(relativePos.xz, float2(0.7071, 0.7071));

                float3 dir = normalize(_CustomDir.xyz);
                return dot(relativePos, dir);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float coord = GetCoord(i.positionWS) * _Density;

                float stripe = frac(coord);

                // 抗锯齿（0.3是表示程度的系数）
                float w = max(fwidth(coord) * 0.3, 0.001);
                float mask = smoothstep(_Width - w, _Width + w, stripe);

                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, mask);

                return float4(col, 1);
            }

            ENDHLSL
        }

        // ===== DepthOnly =====
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vertDepth (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 fragDepth (Varyings i) : SV_Target { return 0; }

            ENDHLSL
        }

        // ===== DepthNormals =====
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vertDN
            #pragma fragment fragDN

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };

            Varyings vertDN (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 fragDN (Varyings i) : SV_Target
            {
                float3 normalWS = normalize(i.normalWS);
                return float4(NormalizeNormalPerPixel(normalWS), 0);
            }

            ENDHLSL
        }
    }
}