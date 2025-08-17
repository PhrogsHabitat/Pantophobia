Shader "Custom/CustomBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PreTex ("Previous Render", 2D) = "white" {}
        _BlendMode ("Blend Mode", Int) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }

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
            sampler2D _PreTex;
            int _BlendMode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 current = tex2D(_MainTex, i.uv);
                fixed4 previous = tex2D(_PreTex, i.uv);
                
                // Blend mode calculations
                if (_BlendMode == 1) // Darken
                    return min(current, previous);
                
                else if (_BlendMode == 2) // Lighten
                    return max(current, previous);
                
                else if (_BlendMode == 3) // Overlay
                    return float4(
                        previous.r > 0.5 ? 1 - 2*(1-current.r)*(1-previous.r) : 2*current.r*previous.r,
                        previous.g > 0.5 ? 1 - 2*(1-current.g)*(1-previous.g) : 2*current.g*previous.g,
                        previous.b > 0.5 ? 1 - 2*(1-current.b)*(1-previous.b) : 2*current.b*previous.b,
                        1
                    );
                
                else if (_BlendMode == 4) // Hard Light
                    return float4(
                        current.r > 0.5 ? 1 - (1-2*(current.r-0.5))*(1-previous.r) : 2*current.r*previous.r,
                        current.g > 0.5 ? 1 - (1-2*(current.g-0.5))*(1-previous.g) : 2*current.g*previous.g,
                        current.b > 0.5 ? 1 - (1-2*(current.b-0.5))*(1-previous.b) : 2*current.b*previous.b,
                        1
                    );
                
                return current; // Normal
            }
            ENDCG
        }
    }
}