// 波紋(セーブ演出)用シェーダー。Built-in Render Pipeline向け。
// Quad(地面に水平に寝かせたもの)に貼るマテリアルに使用する。
Shader "Custom/RippleRing"
{
    Properties
    {
        _Color ("Color", Color) = (0.4, 0.85, 1, 1)
        _Progress ("Progress (0-1)", Range(0,1)) = 0
        _RingWidth ("Ring Width", Range(0.01, 1)) = 0.15
        _MaxRadius ("Max Radius", Float) = 5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Progress;
            float _RingWidth;
            float _MaxRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
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
                // UV(0-1)を中心基準の-1〜1に変換し、中心からの距離を出す
                float2 centered = (i.uv - 0.5) * 2.0;
                float dist = length(centered) * _MaxRadius;

                // 現在の波紋の半径(時間経過で広がる)
                float ringRadius = _Progress * _MaxRadius;
                float d = abs(dist - ringRadius);

                // リング状にする(中心から離れるほど薄くなる帯)
                float ring = 1.0 - smoothstep(0.0, _RingWidth, d);

                // 広がるにつれて全体的にも薄くしていく
                float fade = 1.0 - _Progress;

                fixed4 col = _Color;
                col.a *= ring * fade;
                return col;
            }
            ENDCG
        }
    }
}
