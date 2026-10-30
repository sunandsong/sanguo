using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 战斗表现：溅血（血滴、血雾、血花、地上血渍）、粒子（火星、落石、妖雾、回复光点、尘土）、伤害数字、刀光、震屏、白闪、顿帧、击退、倒地、下令光圈、开战标题。
    /// 都是“记下出生时间，画的时候按经过的时间直接算”的写法，不用逐帧更新。
    /// </summary>
    public partial class GameRoot
    {
        void ResetBattleFx()
        {
            sparks.Clear();
            blood.Clear();
            popups.Clear();
            knock.Clear();
            downAt.Clear();
            shakeUntil = hitstopUntil = 0f;
            slashAt = flashAt = -10f;
            slashTarget = null;
            ResetSpellFx();
        }

        static float Rnd(float a, float b) { return Random.Range(a, b); }

        // ---------------- 粒子 ----------------

        struct Spark { public Vector2 p, v; public float born, life, size, grav; public Color c; }
        readonly List<Spark> sparks = new List<Spark>();

        void AddSpark(Vector2 p, Vector2 v, float life, float size, float grav, Color c)
        {
            sparks.Add(new Spark { p = p, v = v, born = Time.time, life = life, size = size, grav = grav, c = c });
        }

        // ---------------- 溅血 ----------------

        // Kind：0 = 血滴（顺着速度拉长成泪滴，落地变成地上的血渍），1 = 血雾（软团慢慢胀开），2 = 伤口那一下炸开的血花
        struct Blood { public Vector2 p, v; public float born, size, ground, rot; public int kind; public Color c; }
        readonly List<Blood> blood = new List<Blood>();
        const float BloodGrav = 1500f, StainLife = 1.4f;

        /// <summary>
        /// 从 at（胸口）炸一蓬血：伤口先爆一朵血花 + 一团血雾，血滴一大半顺着 dir（+1 右 / -1 左，背离出手的人）喷，
        /// 剩下的往四周溅；血滴飞的时候顺着速度拉长，落到 footY 附近的地上摊成血渍再慢慢淡掉。会心更多更远，致命一击（kill）最猛。
        /// </summary>
        void SprayBlood(Vector2 at, float footY, float dir, bool crit, bool kill)
        {
            float power = kill ? 1.6f : crit ? 1.3f : 1f;
            int n = Mathf.RoundToInt(70 * power * power);
            for (int i = 0; i < n; i++)
            {
                Vector2 v;
                if (i % 5 < 3)
                {
                    // 顺着挨打方向的一个扇面，往上偏
                    float a = Rnd(-55f, 40f) * Mathf.Deg2Rad;
                    v = new Vector2(dir * Mathf.Cos(a), -Mathf.Sin(a) - 0.35f) * Rnd(250f, 820f) * power;
                }
                else
                {
                    // 四周乱溅
                    float a = Rnd(0f, Mathf.PI * 2f);
                    v = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f - 0.45f) * Rnd(120f, 480f) * power;
                }
                float dark = Rnd(0f, 1f);
                blood.Add(new Blood
                {
                    kind = 0, p = at + new Vector2(Rnd(-10f, 10f), Rnd(-16f, 16f)), v = v, born = Time.time,
                    size = Rnd(4f, i % 7 == 0 ? 17f : 11f) * Mathf.Sqrt(power), ground = footY + Rnd(-26f, 22f), rot = Rnd(0f, 360f),
                    c = new Color(Mathf.Lerp(0.78f, 0.42f, dark), Mathf.Lerp(0.05f, 0.01f, dark), Mathf.Lerp(0.05f, 0.03f, dark)),
                });
            }
            for (int i = 0; i < (kill ? 7 : 4); i++)
                blood.Add(new Blood
                {
                    kind = 1, p = at + new Vector2(Rnd(-14f, 14f), Rnd(-14f, 14f)), v = new Vector2(dir * Rnd(30f, 120f), Rnd(-60f, 10f)), born = Time.time,
                    size = Rnd(40f, 70f) * power, c = new Color(0.6f, 0.03f, 0.03f, 0.35f),
                });
            blood.Add(new Blood { kind = 2, p = at + new Vector2(dir * 10f, 0), born = Time.time, size = 70f * power, rot = Rnd(0f, 360f), c = new Color(0.62f, 0.02f, 0.02f) });
        }

        /// <summary>血滴哪一刻落地（解 p.y + v.y t + g t²/2 = ground）。</summary>
        static float LandTime(Blood b)
        {
            float d = b.ground - b.p.y;
            if (d <= 0) d = 4f;
            return (-b.v.y + Mathf.Sqrt(b.v.y * b.v.y + 2f * BloodGrav * d)) / BloodGrav;
        }

        /// <summary>地上的血渍：压扁的不规则血斑，画在人底下。</summary>
        void DrawBloodStains()
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            foreach (var b in blood)
            {
                if (b.kind != 0) continue;
                float tl = LandTime(b), t = Time.time - b.born;
                if (t < tl) continue;
                float k = (t - tl) / StainLife;
                var p = new Vector2(b.p.x + b.v.x * tl, b.ground);
                float spread = Mathf.Min(1f, (t - tl) / 0.08f);   // 落地一下摊开
                float w = b.size * 2.2f * (0.6f + 0.4f * spread), h = w * 0.38f;
                GUI.color = new Color(b.c.r * 0.8f, b.c.g, b.c.b, 0.85f * (1f - k * k));
                Gfx.Image(new Rect(p.x - w * 0.5f, p.y - h * 0.5f, w, h), SplatTex());
            }
            GUI.color = old;
        }

        /// <summary>空中的血：血花、血雾、拉长的血滴（带一点高光）。</summary>
        void DrawBlood()
        {
            blood.RemoveAll(b => Time.time - b.born > (b.kind == 0 ? LandTime(b) + StainLife : b.kind == 1 ? 0.7f : 0.3f));
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            foreach (var b in blood)
            {
                float t = Time.time - b.born;
                if (b.kind == 2)
                {
                    // 血花：一下胀开、转一点，再淡掉
                    float k = t / 0.3f, s = b.size * (0.5f + 0.7f * Mathf.Min(1f, t / 0.06f));
                    RotatedImage(new Rect(b.p.x - s * 0.5f, b.p.y - s * 0.5f, s, s), SplatTex(), b.rot + k * 20f, new Color(b.c.r, b.c.g, b.c.b, 1f - k * k));
                }
                else if (b.kind == 1)
                {
                    float k = t / 0.7f, s = b.size * (0.6f + 0.9f * k);
                    var p = b.p + b.v * t;
                    GUI.color = new Color(b.c.r, b.c.g, b.c.b, b.c.a * (1f - k));
                    Gfx.Image(new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, s), GlowTex());
                }
                else if (t < LandTime(b))
                {
                    var p = b.p + b.v * t + new Vector2(0, 0.5f * BloodGrav * t * t);
                    var v = b.v + new Vector2(0, BloodGrav * t);
                    float len = b.size * (1.2f + v.magnitude / 260f), wid = b.size;
                    float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                    RotatedImage(new Rect(p.x - len * 0.5f, p.y - wid * 0.5f, len, wid), DropTex(), ang, b.c);
                    if (b.size > 8f)
                        RotatedImage(new Rect(p.x - len * 0.15f, p.y - wid * 0.3f, len * 0.35f, wid * 0.3f), GlowTex(), ang, new Color(1f, 0.45f, 0.4f, 0.55f));
                }
            }
            GUI.color = old;
        }

        static Texture2D dropTex, splatTex;

        /// <summary>泪滴：右边是圆头，往左拖一条渐细的尾巴（画的时候转到速度方向，头朝前）。</summary>
        static Texture2D DropTex()
        {
            if (dropTex != null) return dropTex;
            const int w = 64, h = 32;
            dropTex = new Texture2D(w, h) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                    float r = u > 0.7f ? Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((u - 0.7f) / 0.3f, 2))) : Mathf.Pow(u / 0.7f, 1.6f);   // 这一列的半宽
                    float a = Mathf.Clamp01((r - v) * 8f);
                    dropTex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            dropTex.Apply();
            return dropTex;
        }

        /// <summary>血斑：一团边缘不规则的血，四周甩出几颗小血点。</summary>
        static Texture2D SplatTex()
        {
            if (splatTex != null) return splatTex;
            const int n = 128;
            splatTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var dots = new Vector3[10];
            var rng = new System.Random(7);
            for (int i = 0; i < dots.Length; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, d = 0.55f + 0.35f * (float)rng.NextDouble();
                dots[i] = new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0.04f + 0.07f * (float)rng.NextDouble());
            }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var q = new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
                    float ang = Mathf.Atan2(q.y, q.x);
                    float edge = 0.42f + 0.045f * Mathf.Sin(ang * 3f + 1f) + 0.04f * Mathf.Sin(ang * 7f + 2f) + 0.025f * Mathf.Sin(ang * 13f + 0.5f);
                    float a = Mathf.Clamp01((edge - q.magnitude) * 30f);
                    foreach (var dt in dots) a = Mathf.Max(a, Mathf.Clamp01((dt.z - (q - new Vector2(dt.x, dt.y)).magnitude) * 30f));
                    splatTex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            splatTex.Apply();
            return splatTex;
        }

        /// <summary>脚下扬尘：起步、急停时，往 dir 的反方向扬。</summary>
        void Dust(Vector2 foot, float dir)
        {
            for (int i = 0; i < 9; i++)
                AddSpark(foot + new Vector2(Rnd(-12f, 12f), Rnd(-4f, 2f)), new Vector2(-dir * Rnd(20f, 100f), Rnd(-70f, -10f)),
                    0.45f, Rnd(8f, 18f), 120f, new Color(0.62f, 0.55f, 0.42f, 0.55f));
        }

        /// <summary>策略落在一个人身上的特效（foot = 这个人的脚底）。</summary>
        void TacticFx(string id, Vector2 foot, float h)
        {
            var chest = foot + new Vector2(0, -h * 0.5f);
            switch (id)
            {
                case "huoji":   // 火计：一团往上窜的火星
                    for (int i = 0; i < 36; i++)
                        AddSpark(chest + new Vector2(Rnd(-26f, 26f), Rnd(-10f, 40f)), new Vector2(Rnd(-60f, 60f), Rnd(-260f, -80f)),
                            Rnd(0.4f, 0.8f), Rnd(6f, 16f), -150f, Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.25f, 0.05f), Rnd(0f, 1f)));
                    AddSpark(chest, Vector2.zero, 0.3f, 80f, 0f, new Color(1f, 0.55f, 0.1f, 0.7f));
                    break;
                case "luoshi":  // 落石：从头顶砸下来
                    for (int i = 0; i < 12; i++)
                        AddSpark(foot + new Vector2(Rnd(-30f, 30f), -h - Rnd(60f, 140f)), new Vector2(Rnd(-20f, 20f), Rnd(150f, 260f)),
                            0.55f, Rnd(10f, 20f), 900f, new Color(0.45f, 0.4f, 0.36f));
                    Dust(foot, 1f);
                    Dust(foot, -1f);
                    break;
                case "yaoshu":  // 妖术：一团慢慢散开的紫雾
                    for (int i = 0; i < 14; i++)
                        AddSpark(chest + new Vector2(Rnd(-20f, 20f), Rnd(-20f, 20f)), new Vector2(Rnd(-50f, 50f), Rnd(-50f, 20f)),
                            Rnd(0.7f, 1.0f), Rnd(28f, 50f), 0f, new Color(0.55f, 0.2f, 0.75f, 0.45f));
                    break;
                case "liaobing":
                case "guwu":    // 回复：脚下往上冒绿光点
                    for (int i = 0; i < 24; i++)
                        AddSpark(foot + new Vector2(Rnd(-28f, 28f), Rnd(-6f, 4f)), new Vector2(Rnd(-10f, 10f), Rnd(-140f, -60f)),
                            Rnd(0.6f, 1.0f), Rnd(5f, 10f), -40f, new Color(0.5f, 1f, 0.55f));
                    break;
                default:
                    for (int i = 0; i < 16; i++)
                        AddSpark(chest, new Vector2(Rnd(-200f, 200f), Rnd(-200f, 100f)), 0.4f, Rnd(4f, 9f), 300f, Color.white);
                    break;
            }
        }

        void DrawSparks()
        {
            sparks.RemoveAll(s => Time.time - s.born > s.life);
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            foreach (var s in sparks)
            {
                float t = Time.time - s.born, k = t / s.life;
                var p = s.p + s.v * t + new Vector2(0, 0.5f * s.grav * t * t);
                float size = s.size * (1f - 0.4f * k);
                GUI.color = new Color(s.c.r, s.c.g, s.c.b, s.c.a * (1f - k));
                Gfx.Image(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), PetalTex());
            }
            GUI.color = old;
        }

        // ---------------- 伤害数字 ----------------

        struct Popup { public string text; public Vector2 at; public float born; public bool heal, crit; }
        readonly List<Popup> popups = new List<Popup>();

        /// <summary>先弹大再缩回、往上飘、淡出。伤害红，回复绿，会心金色加大。</summary>
        void DrawPopups()
        {
            popups.RemoveAll(q => Time.time - q.born > 0.9f);
            foreach (var q in popups)
            {
                float t = Time.time - q.born;
                float baseSize = q.crit ? 44f : 30f;
                int size = Mathf.RoundToInt(baseSize + baseSize * 0.5f * Mathf.Max(0f, 1f - t / 0.12f));
                var c = q.heal ? new Color(0.45f, 1f, 0.45f) : q.crit ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 0.3f, 0.2f);
                c.a = Mathf.Clamp01((0.9f - t) / 0.3f);
                var r = new Rect(q.at.x - 100, q.at.y - 40f * t - 24, 200, 48);
                if (q.crit) Gfx.Text(new Rect(r.x, r.y - 26, r.width, 24), "会心", 18, c, TextAnchor.MiddleCenter);
                Gfx.Text(r, q.text, size, c, TextAnchor.MiddleCenter);
            }
        }

        // ---------------- 刀光 / 震屏 / 白闪 / 顿帧 ----------------

        float shakeUntil, shakeAmp, hitstopUntil, slashAt = -10f, flashAt = -10f;
        General slashTarget;

        /// <summary>落刀那一下：顿帧、震屏、刀光；会心再加长、加重、白闪。</summary>
        void Impact(General target, bool crit)
        {
            hitstopUntil = Time.time + (crit ? 0.12f : 0.07f);
            shakeUntil = Time.time + (crit ? 0.35f : 0.18f);
            shakeAmp = crit ? 8f : 4f;
            slashAt = Time.time;
            slashTarget = target;
            if (crit) flashAt = Time.time;
        }

        bool Frozen { get { return Time.time < hitstopUntil; } }

        Vector2 Shake()
        {
            return Time.time < shakeUntil ? new Vector2(Rnd(-1f, 1f) * shakeAmp, Rnd(-1f, 1f) * shakeAmp * 0.7f) : Vector2.zero;
        }

        /// <summary>刀光：被砍的人身上划一道斜的白光，0.15 秒。</summary>
        void DrawSlash()
        {
            float t = Time.time - slashAt;
            Vector2 at;
            if (t > 0.15f || slashTarget == null || !slots.TryGetValue(slashTarget, out at) || Event.current.type != EventType.Repaint) return;
            float h = HeightOf(slashTarget);
            at += new Vector2(0, -h * 0.55f);
            var oldM = GUI.matrix;
            var oldC = GUI.color;
            GUIUtility.RotateAroundPivot(-35f, at);
            GUI.color = new Color(1f, 0.97f, 0.85f, 1f - t / 0.15f);
            float len = h * (0.7f + 2f * t);
            GUI.DrawTexture(new Rect(at.x - len * 0.5f, at.y - 3f, len, 6f), PetalTex());
            GUI.DrawTexture(new Rect(at.x - len * 0.4f, at.y - 1f, len * 0.8f, 2f), Texture2D.whiteTexture);
            GUI.matrix = oldM;
            GUI.color = oldC;
        }

        /// <summary>会心一击的全屏白闪，0.12 秒。</summary>
        void DrawScreenFlash()
        {
            float t = Time.time - flashAt;
            if (t < 0.12f) Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(1, 1, 1, 0.55f * (1f - t / 0.12f)));
        }

        // ---------------- 击退：被打往后滑一下再慢慢回来 ----------------

        readonly Dictionary<General, Vector2> knock = new Dictionary<General, Vector2>();   // x = 出生时间，y = 方向（+1 右 / -1 左）

        void Knock(General g, float dir) { knock[g] = new Vector2(Time.time, dir); }

        float KnockX(General g)
        {
            Vector2 k;
            if (!knock.TryGetValue(g, out k)) return 0f;
            float t = Time.time - k.x;
            float m = t < 0.06f ? t / 0.06f : Mathf.Max(0f, 1f - (t - 0.06f) / 0.3f);
            return k.y * 15f * m;
        }

        // ---------------- 败走：我方倒地；敌人被打飞出画面 ----------------

        readonly Dictionary<General, float> downAt = new Dictionary<General, float>();
        const float FlyHold = 0.05f, FlyTime = 0.7f;   // 打中那一下顿几帧再飞；飞出画面要多久

        void FallDown(General g)
        {
            if (downAt.ContainsKey(g)) return;
            downAt[g] = Time.time;
            bool enemy = battle.Enemies.Contains(g);
            PuppetActorOf(g, a => { if (enemy) { a.Hit(); a.Play("hit", true); } else a.Play("down", true); });
        }

        /// <summary>
        /// 敌人被打飞：先顿 FlyHold 秒（闪白），再朝挨打的方向往斜上方抛出去，边飞边转边缩小，飞出画面；
        /// 飞没了在出画面的地方闪一颗星。返回 false = 已经飞没了，不用再画人。
        /// 兵力已经归零、“败走”战报还没到（还没飞）的，off = 0 照常画。
        /// </summary>
        bool FlyOut(General g, Vector2 foot, out Vector2 off, out float spin, out float scale)
        {
            off = Vector2.zero; spin = 0; scale = 1;
            float t0;
            if (!downAt.TryGetValue(g, out t0)) return true;
            float t = Time.time - t0 - FlyHold;
            if (t <= 0) return true;
            Vector2 k;
            float dir = knock.TryGetValue(g, out k) ? k.y : -1f;
            float u = t / FlyTime;
            off = FlyPath(dir, t);
            spin = dir * 900f * t;
            scale = 1f - 0.45f * Mathf.Clamp01(u);
            if (u >= 1)
            {
                // 飞出去的地方（夹回画面边上）闪一颗星
                float s = u - 1;
                if (s < 0.45f)
                {
                    var p = foot + FlyPath(dir, FlyTime * 0.8f);
                    p.x = Mathf.Clamp(p.x, 40, Gfx.VW - 40);
                    p.y = Mathf.Clamp(p.y, 40, Gfx.VH - 40);
                    float f = s / 0.45f, ss = 46 * Mathf.Sin(f * Mathf.PI);
                    RotatedImage(new Rect(p.x - ss, p.y - ss, ss * 2, ss * 2), StarTex(), f * 90f, new Color(1, 1, 0.85f, 1 - f * f));
                }
                return false;
            }
            return true;
        }

        /// <summary>打飞的轨迹：往 dir 那边猛冲，先往上抛再略往下坠（这么快基本是一条往斜上的弧）。</summary>
        static Vector2 FlyPath(float dir, float t) { return new Vector2(dir * 1300f * t, -900f * t + 600f * t * t); }

        void PuppetActorOf(General g, System.Action<PuppetActor> f)
        {
            PuppetActor a;
            if (battlePuppets.TryGetValue(g, out a) && a != null) f(a);
        }

        // ---------------- 下令光圈 ----------------

        static Texture2D ringTex;

        /// <summary>白色圆环（软边），画的时候压扁、染色。</summary>
        static Texture2D RingTex()
        {
            if (ringTex != null) return ringTex;
            const int n = 64;
            ringTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    ringTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f)));
                }
            ringTex.Apply();
            return ringTex;
        }

        /// <summary>脚下一圈呼吸的光环：轮到下令的人金色，选中的目标红色（更大、闪得更快）。</summary>
        void DrawRing(Vector2 foot, bool target, float h)
        {
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            var c = target ? new Color(1f, 0.3f, 0.2f) : Accent;
            c.a = 0.55f + 0.3f * Mathf.Sin(Time.time * (target ? 9f : 5f));
            GUI.color = c;
            float w = h * (target ? 1.3f : 1.1f);
            Gfx.Image(new Rect(foot.x - w * 0.5f, foot.y - w * 0.14f, w, w * 0.28f), RingTex());
            GUI.color = old;
        }

        // ---------------- 开战标题 ----------------

        /// <summary>入场时屏幕中间一条暗带 + 金色大字，先放大砸下来，停一下再淡出（共 1.5 秒）。</summary>
        void DrawBattleTitle()
        {
            float t = Time.time - battleEnter;
            if (t > 1.5f || battleExit >= 0f) return;
            float a = t < 1.1f ? 1f : 1f - (t - 1.1f) / 0.4f;
            float pop = Mathf.Max(0f, 1f - t / 0.22f);
            float y = by + bh * 0.38f;
            Gfx.Rect(new Rect(0, y - 36, Gfx.VW, 72), new Color(0, 0, 0, 0.5f * a));
            var line = Accent;
            line.a = 0.8f * a;
            Gfx.Rect(new Rect(0, y - 37, Gfx.VW, 2), line);
            Gfx.Rect(new Rect(0, y + 35, Gfx.VW, 2), line);
            var c = Accent;
            c.a = a * (1f - pop * 0.6f);
            Gfx.Text(new Rect(bx, y - 50, bw, 100), battle.IsBoss ? "黄巾军主力来袭" : "遭遇黄巾军",
                Mathf.RoundToInt(50f + 34f * pop), c, TextAnchor.MiddleCenter);
        }
    }
}
