// 世界层贴片：贴图 × _Color，再朝白色插值 _Flash（受击闪白）。和 Puppet/Flash 同一个式子，只是颜色走属性块不走顶点色。
Shader "LiuBeiRPG/World"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Flash ("Flash", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Flash;
            struct appdata { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata i) { v2f o; o.pos = UnityObjectToClipPos(i.pos); o.uv = TRANSFORM_TEX(i.uv, _MainTex); return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                t.rgb = lerp(t.rgb * _Color.rgb, 1, _Flash);
                t.a *= _Color.a;
                return t;
            }
            ENDCG
        }
    }
}
