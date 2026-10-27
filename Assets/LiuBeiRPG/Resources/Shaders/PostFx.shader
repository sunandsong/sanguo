// 大地图后处理：0 取亮部并缩小，1 单方向模糊，2 合成（泛光 + 调色 + 饱和度/对比度 + 暗角）。
Shader "LiuBeiRPG/PostFx"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _BloomTex;
    float4 _MainTex_TexelSize;
    float4 _Dir, _Grade;
    float _Threshold, _Bloom, _Saturation, _Contrast, _Vignette;
    ENDCG
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag (v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                float l = max(c.r, max(c.g, c.b));
                return float4(c * saturate((l - _Threshold) / (1 - _Threshold)), 1);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 d = _Dir.xy * _MainTex_TexelSize.xy;
                float3 c = tex2D(_MainTex, i.uv).rgb * 0.227;
                c += (tex2D(_MainTex, i.uv + d * 1.38).rgb + tex2D(_MainTex, i.uv - d * 1.38).rgb) * 0.316;
                c += (tex2D(_MainTex, i.uv + d * 3.23).rgb + tex2D(_MainTex, i.uv - d * 3.23).rgb) * 0.070;
                return float4(c, 1);
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag (v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb + tex2D(_BloomTex, i.uv).rgb * _Bloom;
                c *= _Grade.rgb;
                float l = dot(c, float3(0.299, 0.587, 0.114));
                c = lerp(l, c, _Saturation);
                c = (c - 0.5) * _Contrast + 0.5;
                float2 v = i.uv - 0.5;
                c *= 1 - _Vignette * smoothstep(0.2, 0.75, dot(v, v) * 2);
                return float4(saturate(c), 1);
            }
            ENDCG
        }
    }
}
