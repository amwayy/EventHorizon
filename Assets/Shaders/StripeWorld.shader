Shader "Custom/UnlitStripeWorld_LockedAxis"
{
    Properties
    {
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (0,0,0,1)

        _Density ("Stripe Density", Float) = 10
        _Width ("Stripe Width", Range(0.01,0.99)) = 0.5

        // ✅ 轴向模式
        [KeywordEnum(X, Y, Z, XZ)] _AxisMode ("Axis Mode", Float) = 3

        // 可选：自定义方向（仅当需要扩展时用）
        _CustomDir ("Custom Direction", Vector) = (1,0,0,0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline"="UniversalRenderPipeline" 
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
            #pragma vertex vert
            #pragma fragment frag

            // 👇 轴向关键字
            #pragma multi_compile _AXISMODE_X _AXISMODE_Y _AXISMODE_Z _AXISMODE_XZ

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
            float4 _CustomDir;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS  = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            float GetCoord(float3 posWS)
            {
                #if defined(_AXISMODE_X)
                    return posWS.x;

                #elif defined(_AXISMODE_Y)
                    return posWS.y;

                #elif defined(_AXISMODE_Z)
                    return posWS.z;

                #elif defined(_AXISMODE_XZ)
                    // 常用于地面斜条纹
                    return dot(posWS.xz, float2(0.7071, 0.7071));

                #else
                    // fallback：自定义方向（世界空间）
                    float3 dir = normalize(_CustomDir.xyz);
                    return dot(posWS, dir);
                #endif
            }

            half4 frag (Varyings i) : SV_Target
            {
                float coord = GetCoord(i.positionWS) * _Density;

                float stripe = frac(coord);

                // ✅ 抗锯齿（避免你之前说的“边缘发灰”）
                float w = fwidth(coord);
                float mask = smoothstep(_Width - w, _Width + w, stripe);

                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, mask);

                return float4(col, 1);
            }

            ENDHLSL
        }

        // ================= DepthOnly =================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

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

        // ================= DepthNormals =================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN

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