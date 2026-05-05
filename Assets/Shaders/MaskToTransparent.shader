Shader "Hidden/MaskToTransparent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Read the mask value (RFloat format, stored in red channel)
                float maskValue = tex2D(_MainTex, i.uv).r;

                // Convert to white foreground with transparent background
                // Where mask > 0.5: white (1,1,1) with alpha 1
                // Where mask <= 0.5: transparent (alpha 0)
                fixed4 col;
                if (maskValue > 0.5)
                {
                    col = fixed4(1, 1, 1, 1); // White with full alpha
                }
                else
                {
                    col = fixed4(0, 0, 0, 0); // Transparent
                }

                return col;
            }
            ENDCG
        }
    }
}
