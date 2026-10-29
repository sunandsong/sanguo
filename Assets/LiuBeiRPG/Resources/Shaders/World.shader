// 世界层贴片：贴图 × _Color，再朝白色插值 _Flash（受击闪白）。和 Puppet/Flash 同一个式子，只是颜色走属性块不走顶点色。
// _Fx 让贴片动起来（x = 幅度，y = 频率，z = 相位，w = 模式）：
//   1 植物：根部钉住，越往上摆得越多，跟着全局风 _Wind 一阵一阵地倒；_Push 是人走过时往两边挤
//   2 垂挂：顶边钉住（灯笼、布幌、门帘），下摆荡、布面起波纹
//   3 旗帜：左边钉在旗杆上，旗面往右一波一波地飘
//   4 水面：_Mask 的 R 通道是水，水里的像素扭动、闪光
// _Clip（x = 水面的世界 y，y = 开关）：人站进浅水，水面以下染水色、变透明。
Shader "LiuBeiRPG/World"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("Mask", 2D) = "black" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Flash ("Flash", Range(0, 1)) = 0
        _Fx ("Fx", Vector) = (0, 0, 0, 0)
        _Clip ("Clip", Vector) = (0, 0, 0, 0)
        _Push ("Push", Float) = 0
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
            sampler2D _MainTex, _Mask;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Flash;
            float4 _Fx, _Clip;
            float _Wind;   // 全局风：0 = 无风，1 = 一阵大风（GameRoot 每帧设）
            float _Push;   // 植物被人挤开：-1..1，往左 / 往右倒
            struct appdata { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 quv : TEXCOORD1; float wy : TEXCOORD2; };

            v2f vert (appdata i)
            {
                v2f o;
                float4 p = i.pos;
                float t = _Time.y * _Fx.y + _Fx.z;
                // 植物整体倒伏：顶上两个顶点按风横移（剪切），根部不动。幅度按贴片宽算，矩阵会乘上去
                if (_Fx.w == 1) p.x += _Fx.x * i.uv.y * i.uv.y * (sin(t) * 0.6 + _Wind * 1.4) + _Push * i.uv.y * i.uv.y * 0.35;
                // 垂挂：底边像钟摆一样荡
                if (_Fx.w == 2) p.x += _Fx.x * (1 - i.uv.y) * sin(t);
                o.pos = UnityObjectToClipPos(p);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.quv = i.uv;
                o.wy = mul(unity_ObjectToWorld, p).y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y * _Fx.y + _Fx.z;
                float glint = 0;
                if (_Fx.w == 1)   // 叶子细碎抖动，越高越明显
                    uv.x += 0.006 * i.quv.y * sin(t * 2.7 + i.quv.y * 23 + i.quv.x * 9) * (0.5 + _Wind);
                else if (_Fx.w == 2)   // 布面从上往下传的波纹
                    uv.x += 0.02 * _Fx.x * (1 - i.quv.y) * sin(t * 1.8 - (1 - i.quv.y) * 7);
                else if (_Fx.w == 3)   // 旗面：离旗杆越远摆得越大
                {
                    float k = i.quv.x;
                    uv.y += _Fx.x * k * sin(t - k * 7) * (0.6 + 0.6 * _Wind);
                    uv.x += _Fx.x * 0.3 * k * cos(t * 1.3 - k * 5);
                }
                else if (_Fx.w == 4)
                {
                    float m = tex2D(_Mask, i.quv).r;
                    float2 q = i.quv * float2(160, 110);
                    uv += m * _Fx.x * float2(sin(q.y + t * 1.9) + sin(q.x * 0.7 + t * 1.3), cos(q.x + t * 1.6) + sin(q.y * 0.6 - t));
                    // 闪光：两组斜向波纹相乘，只留最亮的尖
                    float s = sin(q.x * 0.9 + q.y * 0.5 + t * 2.1) * sin(q.x * 0.4 - q.y * 0.8 - t * 1.7);
                    glint = m * pow(saturate(s), 12) * 0.5;
                }
                fixed4 c = tex2D(_MainTex, uv);
                c.rgb = lerp(c.rgb * _Color.rgb, 1, _Flash) + glint;
                c.a *= _Color.a;
                // 站在浅水里：水面以下染成水色、淡出，水线上留一道亮边
                if (_Clip.y > 0)
                {
                    float d = _Clip.x - i.wy;   // > 0 在水下
                    if (d > 0) { c.rgb = lerp(c.rgb, fixed3(0.25, 0.45, 0.55), 0.55); c.a *= saturate(0.45 - d * 1.5); }
                    else c.rgb += saturate(1 + d * 40) * 0.25;
                }
                return c;
            }
            ENDCG
        }
    }
}
