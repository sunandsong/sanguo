using UnityEngine;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 光和天：一天 24 时辰走一圈（现实 8 分钟），清晨 → 正午 → 黄昏 → 夜里；
    /// 户外整张图乘天色，夜里灯笼篝火亮起光晕；物件在地上投斜影，树下有晃动的光斑；
    /// 时不时下一阵雨、清晨起雾；室内飘灰尘、烛光微微闪。
    /// 户外 = map.Weather 不为空（室内没有飘落物）。
    /// </summary>
    public partial class GameRoot
    {
        float hour = 8f;                    // 0..24
        const float HoursPerSecond = 24f / 480f;

        float rain, rainTarget, fog, fogTarget;   // 0..1，慢慢过渡
        float nextWeatherRoll = 40f;

        bool Outdoor { get { return map != null && map.Weather != null; } }

        public void SetHour(float h) { hour = h; rain = rainTarget = 0; }

        // 时辰 → 天色（乘到画面上）
        static readonly float[] SkyHours = { 0, 4.5f, 6f, 7.5f, 16f, 17.5f, 18.8f, 20f, 24 };
        static readonly Color[] SkyColors =
        {
            new Color(0.30f, 0.34f, 0.55f), new Color(0.30f, 0.34f, 0.55f), new Color(0.78f, 0.66f, 0.70f), Color.white,
            Color.white, new Color(1f, 0.82f, 0.62f), new Color(0.78f, 0.55f, 0.55f), new Color(0.32f, 0.35f, 0.56f), new Color(0.30f, 0.34f, 0.55f),
        };

        Color SkyColor()
        {
            for (int i = 1; i < SkyHours.Length; i++)
                if (hour <= SkyHours[i]) return Color.Lerp(SkyColors[i - 1], SkyColors[i], Mathf.InverseLerp(SkyHours[i - 1], SkyHours[i], hour));
            return SkyColors[0];
        }

        /// <summary>夜有多深：0 = 白天，1 = 深夜。灯光、萤火按这个亮。</summary>
        float Night { get { var c = SkyColor(); return Mathf.InverseLerp(0.95f, 0.45f, (c.r + c.g + c.b) / 3f); } }

        /// <summary>白天有多亮（投影、光斑按这个淡入淡出），下雨时没有太阳。</summary>
        float Sun { get { return Mathf.Clamp01(1 - Night * 1.6f) * (1 - rain * 0.85f) * (1 - fog * 0.6f); } }

        void UpdateSky(float dt)
        {
            if (mode == Mode.Map) hour = Mathf.Repeat(hour + dt * HoursPerSecond, 24f);
            // 天气：每隔一阵掷一次，户外才有
            if (Time.time > nextWeatherRoll)
            {
                nextWeatherRoll = Time.time + Random.Range(60f, 120f);
                float r = Random.value;
                rainTarget = r < 0.2f ? Random.Range(0.6f, 1f) : 0;
                fogTarget = rainTarget == 0 && hour > 4.5f && hour < 9.5f && Random.value < 0.5f ? Random.Range(0.5f, 0.9f) : 0;
            }
            if (fog > 0 && (hour > 10f || hour < 4f)) fogTarget = 0;   // 雾到上午就散
            rain = Mathf.MoveTowards(rain, rainTarget, dt * 0.08f);
            fog = Mathf.MoveTowards(fog, fogTarget, dt * 0.05f);
        }

        // ---------------- 投影、光斑 ----------------

        /// <summary>太阳方向：早上影子往左（西），傍晚往右，正午短。</summary>
        void SunShear(out float shx, out float sy)
        {
            float k = Mathf.Clamp((hour - 12f) / 6f, -1.2f, 1.2f);
            shx = k * 0.9f;
            sy = 0.32f + 0.25f * Mathf.Abs(k);
        }

        bool CastsShadow(MapDecor d) { return !d.Ground && d.Lift == 0 && d.Frames == 1 && d.W >= 0.95f && d.Motion != LiuBeiRPG.World.Motion.Hang && d.Motion != LiuBeiRPG.World.Motion.Flag; }

        /// <summary>物件的地面投影：贴在地上，所以在所有人物和物件之前画。</summary>
        void DrawCastShadows()
        {
            float a = 0.28f * Sun;
            if (!Outdoor || a < 0.01f) return;
            float shx, sy;
            SunShear(out shx, out sy);
            foreach (var d in map.Decor)
            {
                if (!CastsShadow(d)) continue;
                var tex = Gfx.Tex(d.Sprite);
                if (tex == null) continue;
                float h = d.H > 0 ? d.H : d.W * tex.height / (float)tex.width;
                World.CastShadow(tex, new Rect(d.X - d.W * 0.5f, d.Y - h, d.W, h), shx, sy, a);
            }
        }

        /// <summary>大树投影里几点漏下来的阳光，跟着风晃。</summary>
        void DrawLeafFlecks()
        {
            float a = Sun;
            if (!Outdoor || a < 0.05f) return;
            float shx, sy, t = Time.time;
            SunShear(out shx, out sy);
            foreach (var d in map.Decor)
            {
                if (d.Motion != LiuBeiRPG.World.Motion.Plant || d.W < 1.4f) continue;
                var tex = Gfx.Tex(d.Sprite);
                if (tex == null) continue;
                float h = d.W * tex.height / (float)tex.width;
                // 树冠影子大概在：脚底往太阳反方向挪半个树高
                var c = new Vector2(d.X + shx * h * 0.55f, d.Y - sy * h * 0.55f);
                for (int i = 0; i < 4; i++)
                {
                    float ph = d.X * 3.1f + i * 2.3f;
                    var p = c + new Vector2(Mathf.Sin(ph) * d.W * 0.3f + Mathf.Sin(t * 1.4f + ph) * 0.08f * (0.4f + wind),
                                            Mathf.Cos(ph * 1.7f) * sy * h * 0.25f);
                    float s = 0.18f + 0.08f * Mathf.Sin(t * 2f + ph);
                    World.Glow(GlowTex(), new Rect(p.x - s, p.y - s * 0.5f, s * 2, s), new Color(1f, 0.92f, 0.7f, 0.10f * a));
                }
            }
        }

        // ---------------- 天色、灯光、天气 ----------------

        /// <summary>所有东西画完之后：先整张乘天色（夜、黄昏、雨），再把灯光加上去。</summary>
        void DrawSkyAndLights(Rect area)
        {
            if (!Outdoor)
            {
                DrawDust(area);
                return;
            }
            DrawFog(area);
            var sky = SkyColor() * Color.Lerp(Color.white, new Color(0.72f, 0.76f, 0.85f), rain);
            sky.a = 1;
            World.Tint(new Rect(area.x - 2, area.y - 2, area.width + 4, area.height + 4), sky);

            float night = Night, t = Time.time;
            if (night > 0.02f)
            {
                foreach (var d in map.Decor)
                {
                    if (!d.Sprite.Contains("lantern")) continue;
                    var tex = Gfx.Tex(d.Sprite);
                    if (tex == null) continue;
                    float h = d.W * tex.height / (float)tex.width;
                    var c = new Vector2(d.X, d.Y - d.Lift - h * 0.42f);
                    float flick = 0.85f + 0.15f * Mathf.PerlinNoise(t * 3f, d.X);
                    Light(c, 1.5f * flick, new Color(1f, 0.55f, 0.3f), 0.55f * night);
                    Light(c, 0.35f, new Color(1f, 0.8f, 0.5f), 0.7f * night);   // 灯笼本身发亮
                }
                // 萤火虫：夜里草地上飘几点
                for (int i = 0; i < 14; i++)
                {
                    float ph = i * 7.3f;
                    var p = new Vector2(Mathf.Repeat(ph * 1.9f + Mathf.Sin(t * 0.3f + ph) * 2f, map.Width), Mathf.Repeat(ph * 1.3f + Mathf.Cos(t * 0.25f + ph) * 1.5f, map.Height));
                    float blink = Mathf.Clamp01(Mathf.Sin(t * 1.7f + ph) * 2f);
                    Light(p, 0.12f, new Color(0.8f, 1f, 0.5f), 0.8f * night * blink * (1 - rain));
                }
            }
            foreach (var e in map.Emitters)
            {
                if (e.Kind != "fire") continue;
                float flick = 0.8f + 0.2f * Mathf.PerlinNoise(t * 6f, e.X);
                Light(new Vector2(e.X, e.Y), 2.6f * flick, new Color(1f, 0.5f, 0.2f), (0.15f + 0.6f * night) * flick);
            }
            DrawRain(area);
        }

        /// <summary>一团叠加光：中心 c（格子坐标），半径 r 格。</summary>
        static void Light(Vector2 c, float r, Color col, float a)
        {
            if (a <= 0.005f) return;
            col.a = a;
            World.Glow(GlowTex(), new Rect(c.x - r, c.y - r * 0.75f, r * 2, r * 1.5f), col);
        }

        void DrawRain(Rect area)
        {
            if (rain < 0.02f) return;
            float t = Time.time;
            int n = (int)(220 * rain);
            float slant = 0.25f + wind * 0.35f;
            for (int i = 0; i < n; i++)
            {
                float ph = i * 0.6180339f;
                float x = area.x + Mathf.Repeat(ph * 97.3f, 1f) * area.width;
                float y = area.y + Mathf.Repeat(Mathf.Repeat(ph * 13.7f, 1f) + t * (1.4f + (i % 5) * 0.08f), 1f) * area.height;
                x = area.x + Mathf.Repeat(x - area.x + (y - area.y) * slant, area.width);
                World.Sprite(Texture2D.whiteTexture, new Vector2(x, -y), new Vector2(0.025f, 0.42f), -Mathf.Atan(slant) * Mathf.Rad2Deg, new Color(0.8f, 0.86f, 1f, 0.32f * rain));
            }
            // 地上溅起的小圈；水面上的大一点
            if (Random.value < rain * 0.9f)
            {
                int tx = Random.Range(0, map.Width), ty = Random.Range(0, map.Height);
                AddRipple(new Vector2(tx + Random.value, ty + Random.value), map.IsWater(tx, ty) ? 0.45f : 0.22f);
            }
        }

        void DrawFog(Rect area)
        {
            if (fog < 0.02f) return;
            float t = Time.time;
            for (int i = 0; i < 10; i++)
            {
                float ph = i * 2.7f;
                float x = area.x + Mathf.Repeat(ph * 3.3f + t * (0.12f + i % 3 * 0.04f), area.width + 8) - 4;
                float y = area.y + Mathf.Repeat(ph * 1.9f, area.height);
                World.Quad(GlowTex(), new Rect(x - 4, y - 1.6f, 8, 3.2f), new Color(0.95f, 0.95f, 0.97f, 0.22f * fog));
            }
        }

        /// <summary>室内：光里飘的细灰，忽明忽暗。</summary>
        void DrawDust(Rect area)
        {
            float t = Time.time;
            for (int i = 0; i < 22; i++)
            {
                float ph = i * 4.1f;
                var p = new Vector2(area.x + Mathf.Repeat(ph * 1.7f + Mathf.Sin(t * 0.21f + ph) * 0.8f + t * 0.05f, area.width),
                                    area.y + Mathf.Repeat(ph * 2.3f + Mathf.Cos(t * 0.17f + ph) * 0.6f, area.height));
                float tw = 0.4f + 0.6f * Mathf.Clamp01(Mathf.Sin(t * 0.9f + ph));
                World.Glow(GlowTex(), new Rect(p.x - 0.05f, p.y - 0.05f, 0.1f, 0.1f), new Color(1f, 0.9f, 0.7f, 0.35f * tw));
            }
        }

        /// <summary>室内烛光：整体亮度轻轻地闪。</summary>
        float CandleFlicker { get { return Outdoor ? 1f : 0.97f + 0.03f * Mathf.PerlinNoise(Time.time * 4f, 0.3f); } }

        static Texture2D glowTex;

        /// <summary>光晕：白色，中心最亮，向外平滑衰减到 0。</summary>
        static Texture2D GlowTex()
        {
            if (glowTex != null) return glowTex;
            const int n = 64;
            glowTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Clamp01(new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f));
                    float a = (1 - d) * (1 - d);
                    glowTex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            glowTex.Apply();
            return glowTex;
        }
    }
}
