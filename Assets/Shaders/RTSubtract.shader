Shader "Custom/BinarySubtract"
{
    Properties
    {
        _MainTex ("Main Texture (A)", 2D) = "white" {}
        _MaskTex ("Mask Texture (B)", 2D) = "black" {}
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv  : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 mainCol = tex2D(_MainTex, i.uv);
                fixed4 maskCol = tex2D(_MaskTex, i.uv);

                float mainA = mainCol.a;
                float maskA = maskCol.a;

                float resultA = mainA * (1 - maskA);

                resultA = resultA > 0.5 ? 1.0 : 0.0;

                return fixed4(1, 1, 1, resultA);
            }
            ENDCG
        }
    }
}