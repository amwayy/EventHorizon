Shader "Custom/Hedge_Leaves"
{
    Properties
    {
        _ColorA ("Dark Green", Color) = (0.15,0.4,0.15,1)
        _ColorB ("Mid Green", Color) = (0.3,0.6,0.2,1)
        _ColorC ("Light Green", Color) = (0.6,0.85,0.3,1)

        _Density ("Leaf Density", Float) = 20
        _LeafSize ("Leaf Size", Range(0.2,1)) = 0.6
        _Randomness ("Random Offset", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            float4 _ColorA, _ColorB, _ColorC;
            float _Density;
            float _LeafSize;
            float _Randomness;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453);
            }

            float2 hash2(float2 p)
            {
                return float2(hash(p), hash(p + 1.23));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // 🌿 椭圆叶子 SDF
            float leafShape(float2 uv)
            {
                uv.x *= 0.7; // 拉长一点
                return length(uv);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // ⭐ 网格（每个格子一片叶子）
                float2 gridUV = uv * _Density;
                float2 id = floor(gridUV);
                float2 f = frac(gridUV);

                // ⭐ 随机偏移（避免太整齐）
                float2 rnd = hash2(id) - 0.5;
                f += rnd * _Randomness;

                // ⭐ 居中
                float2 p = f - 0.5;

                // ⭐ 叶子大小
                p /= _LeafSize;

                // ⭐ 叶子形状
                float d = leafShape(p);

                // ⭐ 叶子边缘（硬边）
                float leaf = smoothstep(0.5, 0.45, d);

                // ⭐ 颜色随机
                float r = hash(id);

                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, r);
                col = lerp(col, _ColorC.rgb, r * r);

                // ⭐ 背景（深绿）
                float3 bg = _ColorA.rgb * 0.7;

                col = lerp(bg, col, leaf);

                return float4(col, 1);
            }
            ENDCG
        }
    }
}