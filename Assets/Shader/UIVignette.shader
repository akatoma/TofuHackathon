// スロー演出用のVignetteシェーダー。Built-in Render Pipeline向け。
// 画面全体を覆うUI Imageのマテリアルに使用する。
Shader "Custom/UIVignette"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Intensity ("Intensity (0-1)", Range(0,1)) = 0
        _Smoothness ("Edge Smoothness", Range(0.01, 1)) = 0.5
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            float _Intensity;
            float _Smoothness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // UV中心(0.5,0.5)からの距離。角に近いほど大きい値になる
                float2 centered = (IN.texcoord - 0.5) * 2.0;
                float dist = length(centered);

                // Intensityが強いほど、暗くなる境界が中心側へ広がってくる
                float edge = lerp(1.4, 0.2, _Intensity);
                float vignette = smoothstep(edge - _Smoothness, edge, dist);

                fixed4 col = _Color;
                col.a *= vignette * _Intensity;
                return col;
            }
            ENDCG
        }
    }
}
