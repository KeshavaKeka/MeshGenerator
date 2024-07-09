Shader "Custom/HighlightShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha

        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}