using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 战斗表现：粒子（血、火星、落石、妖雾、回复光点、尘土）、伤害数字、刀光、震屏、白闪、顿帧、击退、倒地、下令光圈、开战标题。
    /// 都是“记下出生时间，画的时候按经过的时间直接算”的写法，不用逐帧更新。
    /// </summary>
    public partial class GameRoot
    {
        void ResetBattleFx()
        {
            sparks.Clear();
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

        /// <summary>从 at 往 dir（+1 右 / -1 左，背离出手的人）喷一蓬血，中间先爆一团大的。</summary>
        void SprayBlood(Vector2 at, float dir, bool crit)
        {
            var red = new Color(0.72f, 0.03f, 0.03f);
            int n = crit ? 70 : 40;
            for (int i = 0; i < n; i++)
                AddSpark(at + new Vector2(Rnd(-8f, 8f), Rnd(-14f, 14f)), new Vector2(dir * Rnd(80f, crit ? 600f : 460f), Rnd(-420f, -60f)),
                    0.8f, Rnd(5f, 15f), 900f, red);
            AddSpark(at, new Vector2(dir * 60f, -40f), 0.25f, crit ? 70f : 46f, 0f, red);
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

        // ---------------- 倒地：败走先倒下，敌人再淡出 ----------------

        readonly Dictionary<General, float> downAt = new Dictionary<General, float>();

        void FallDown(General g)
        {
            if (downAt.ContainsKey(g)) return;
            downAt[g] = Time.time;
            PuppetActorOf(g, a => a.Play("down", true));
        }

        /// <summary>倒地的敌人还剩多少不透明度：倒下 0.5 秒后用 0.5 秒淡出，≤ 0 就不画了。
        /// 兵力已经归零、“败走”战报还没到（还没倒）的，照常画。</summary>
        float DownAlpha(General g)
        {
            float t0;
            if (!downAt.TryGetValue(g, out t0)) return 1f;
            return 1f - Mathf.Clamp01((Time.time - t0 - 0.5f) / 0.5f);
        }

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
