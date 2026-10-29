// 世界层的两种特殊混合（World.Glow / World.Tint 用）：
//   _Mode 0 = 叠加光（灯笼、篝火的光晕）：颜色 × 透明度加到画面上
//   _Mode 1 = 正片叠底（天色、夜色、雨天压暗）：透明处等于白色，不改画面
// 混合方式按材质设（_SrcBlend / _DstBlend），World 里建两个材质。
Shader "LiuBeiRPG/WorldBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Mode ("Mode", Float) = 0
        _SrcBlend ("Src", Float) = 5
        _DstBlend ("Dst", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Mode;
            struct appdata { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata i) { v2f o; o.pos = UnityObjectToClipPos(i.pos); o.uv = TRANSFORM_TEX(i.uv, _MainTex); return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv) * _Color;
                if (_Mode > 0.5) return fixed4(lerp(fixed3(1, 1, 1), t.rgb, t.a), 1);
                return t;
            }
            ENDCG
        }
    }
}
