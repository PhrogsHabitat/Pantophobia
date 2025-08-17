Shader "Custom/TextureSwap"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _SwapTex ("Swap Texture", 2D) = "white" {}
        _Amount ("Blend Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        // Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        sampler2D _SwapTex;
        float _Amount;

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 mainColor = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 swapColor = tex2D(_SwapTex, IN.uv_MainTex);
            fixed4 finalColor = lerp(mainColor, swapColor, _Amount);
            o.Albedo = finalColor.rgb;
            o.Alpha = finalColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}