Shader "Custom/UnlitStripeAdvanced"
{
    Properties
    {
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (0,0,0,1)

        _Density ("Stripe Density", Float) = 10
        _Width ("Stripe Width", Range(0.01,0.99)) = 0.5

        _Angle ("Stripe Angle (Degrees)", Range(0,180)) = 45
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        // =========================
        // ✅ 主渲染
        // =========================
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
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            float4 _ColorA;
            float4 _ColorB;
            float _Density;
            float _Width;
            float _Angle;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // ✅ 角度转弧度
                float angle = radians(_Angle);

                // ✅ 方向向量
                float2 dir = float2(cos(angle), sin(angle));

                // ✅ 投影（决定条纹方向）
                float coord = dot(i.uv, dir) * _Density;

                float stripe = frac(coord);

                float mask = step(stripe, _Width);

                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, mask);

                return float4(col, 1);
            }
            ENDHLSL
        }

        // =========================
        // ✅ DepthOnly（遮挡）
        // =========================
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

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vertDepth (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 fragDepth (Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =========================
        // ✅ DepthNormals（关键！给 edge detection 用）
        // =========================
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

                // ✅ 写入 URP normal buffer
                return float4(NormalizeNormalPerPixel(normalWS), 0);
            }
            ENDHLSL
        }
    }
}