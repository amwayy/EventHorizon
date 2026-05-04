Shader "Hidden/BlitCopy"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaAware ("Alpha Aware", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _AlphaAware;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // If alpha-aware mode is enabled, output 1 for opaque pixels, 0 for transparent
                if (_AlphaAware > 0.5)
                {
                    return col.a > 0.5 ? 1.0 : 0.0;
                }

                // Otherwise, just copy the texture as-is
                return col;
            }
            ENDCG
        }
    }
}
