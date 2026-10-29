using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 胜利结算面板，从上往下：
    ///   毛笔大字“胜利”砸下来；
    ///   击败：打倒的敌人小人站一排（同种合并，标 ×n）；
    ///   经验 / 金钱两张大卡，数字往上滚，下面经验条涨；
    ///   升级了金色横幅；
    ///   战利品：掉落的物品图标；新学会的法术带图标。
    /// 点一下（或确认）才出场回地图。
    /// </summary>
    public partial class GameRoot
    {
        float resultAt;
        int resultFromLevel, resultFromMaxTP;
        float resultFromExp;                                                       // 打之前经验条走到几成
        readonly List<KeyValuePair<General, int>> resultMaxBefore = new List<KeyValuePair<General, int>>();
        const float LevelUpAt = 0.95f;                                             // 经验条涨满、升级爆开的时刻
        readonly List<string> resultLearned = new List<string>();                 // 新学会的法术 Id
        readonly Dictionary<string, PuppetActor> resultFigs = new Dictionary<string, PuppetActor>();

        void ShowResult()
        {
            resultFromLevel = party.Level;
            resultFromMaxTP = party.MaxTP;
            resultFromExp = (float)party.Exp / party.ExpToNext;
            resultMaxBefore.Clear();
            foreach (var m in party.Members) resultMaxBefore.Add(new KeyValuePair<General, int>(m, m.MaxSoldiers));
            resultLearned.Clear();
            foreach (var e in battle.GrantRewards())
                foreach (var tc in TacticDb.All)
                    if (e.Text.Contains("「" + tc.Name + "」") && e.Text.StartsWith("习得")) resultLearned.Add(tc.Id);
            resultAt = Time.time;
            bstate = BState.Result;
        }

        void CloseResult()
        {
            // 刚弹出来的一下不吃，免得连点直接跳过；升级了要等升级演完
            if (Time.time - resultAt < (party.Level > resultFromLevel ? LevelUpAt + 1.2f : 0.6f)) return;
            EndBattle();
        }

        /// <summary>结算面板里站着的敌人小人：单独一份，站着待机，不是战场上倒下的那个。</summary>
        PuppetActor ResultFig(GeneralDef d)
        {
            PuppetActor a;
            if (resultFigs.TryGetValue(d.Id, out a)) return a;
            try
            {
                a = PuppetActor.Load(d.Id == "hjb" ? "huangjin" : d.Id);
                string w;
                if (EnemyWeapons.TryGetValue(d.Id, out w)) a.Equip(w);
                a.Facing = "front";
            }
            catch (System.Exception) { a = null; }
            return resultFigs[d.Id] = a;
        }

        void DrawResult()
        {
            float t = Time.time - resultAt;
            bool up = party.Level > resultFromLevel;
            var loot = battle.RewardItems;
            bool extra = loot.Count > 0 || resultLearned.Count > 0;

            float w = Mathf.Min(Gfx.VW - 24, 720);
            int statRows = up ? 2 + resultMaxBefore.Count : 0;
            float h = Mathf.Min(Gfx.VH - 16, 300 + (up ? 12 + statRows * 30 : 0) + (extra ? 60 : 0));
            var r = new Rect((Gfx.VW - w) * 0.5f, (Gfx.VH - h) * 0.5f, w, h);
            float pop = EaseOut(resultAt, 0.25f);
            r.y += (1f - pop) * 30f;

            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, 0.55f * pop));
            Gfx.RoundRect(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(Accent.r, Accent.g, Accent.b, 0.35f));
            Gfx.Modern(r, 0.96f);
            bool repaint = Event.current.type == EventType.Repaint;

            // ---- 毛笔大字“胜利”：先放大再落定；升级那一下换成“升级”，带光芒和火星爆开 ----
            var title = new Rect(r.x, r.y + 4, r.width, 70);
            float lu = t - LevelUpAt;   // 升级过去了多久（< 0 还没到）
            if (up && lu > 0)
            {
                // 背后转动的金色光芒
                float ra = Mathf.Clamp01(lu / 0.2f) * (0.45f + 0.1f * Mathf.Sin(Time.time * 4f));
                RotatedImage(new Rect(title.center.x - 210, title.center.y - 210, 420, 420), RayTex(), Time.time * 25f, new Color(1f, 0.82f, 0.35f, ra));
                RotatedImage(new Rect(title.center.x - 150, title.center.y - 150, 300, 300), RayTex(), -Time.time * 40f, new Color(1f, 0.95f, 0.7f, ra * 0.7f));
            }
            bool showLevel = up && lu > 0;
            float s = showLevel ? 1f + Mathf.Max(0, 1.4f * (1 - BackEase(Mathf.Clamp01(lu / 0.35f))))
                                : 1f + Mathf.Max(0f, 1f - t / 0.2f) * 0.6f;
            BrushTitle(title, showLevel ? "升级" : "胜利", Mathf.RoundToInt(58 * s));
            if (showLevel) Sparks(title.center, lu);
            float y = r.y + 78;

            // ---- 击败：敌人小人一排 ----
            var groups = new List<KeyValuePair<GeneralDef, int>>();
            foreach (var e in battle.Enemies)
            {
                int gi = groups.FindIndex(g => g.Key == e.Def);
                if (gi < 0) groups.Add(new KeyValuePair<GeneralDef, int>(e.Def, 1));
                else groups[gi] = new KeyValuePair<GeneralDef, int>(e.Def, groups[gi].Value + 1);
            }
            Gfx.Text(new Rect(r.x + 24, y, 60, 22), "击败", 15, new Color(1, 1, 1, 0.5f));
            float ew = Mathf.Min(110, (w - 120) / Mathf.Max(1, groups.Count));
            float ex = r.x + (w - ew * groups.Count) * 0.5f;
            for (int i = 0; i < groups.Count; i++)
            {
                var d = groups[i].Key;
                var cell = new Rect(ex + i * ew, y, ew, 86);
                float appear = Mathf.Clamp01((t - 0.15f - i * 0.08f) / 0.2f);
                var fig = ResultFig(d);
                if (fig != null && appear > 0)
                {
                    if (repaint) fig.Tick(Time.deltaTime);
                    fig.Tint = new Color(0.75f, 0.7f, 0.7f, appear);
                    fig.DrawGUI(new Vector2(cell.center.x, cell.y + 64), 60 / 950f);
                    fig.Tint = Color.white;
                }
                Gfx.Text(new Rect(cell.x, cell.y + 64, cell.width, 20), d.Name + (groups[i].Value > 1 ? " ×" + groups[i].Value : ""), 14, new Color(1, 1, 1, 0.8f * appear), TextAnchor.MiddleCenter);
            }
            y += 92;

            // ---- 经验 / 金钱两张大卡，数字滚上去 ----
            float roll = Mathf.Clamp01((t - 0.35f) / 0.7f);
            float cw = (w - 60) * 0.5f;
            RewardCard(new Rect(r.x + 20, y, cw, 58), "Art/icon_exp", "经验", "+" + Mathf.RoundToInt(battle.RewardExp * roll), new Color(0.55f, 0.8f, 1f));
            RewardCard(new Rect(r.x + 40 + cw, y, cw, 58), "Art/icon_gold", "金钱", "+" + Mathf.RoundToInt(battle.RewardGold * roll), Accent);
            y += 66;
            // 经验条：升级的话先涨满、一闪，再从头涨到现在的位置
            var bar = new Rect(r.x + 20, y + 4, w - 40, 10);
            float now = (float)party.Exp / party.ExpToNext, fill;
            if (!up) fill = Mathf.Lerp(resultFromExp, now, roll);
            else if (lu < 0) fill = Mathf.Lerp(resultFromExp, 1f, Mathf.Clamp01((t - 0.35f) / (LevelUpAt - 0.35f)));
            else fill = lu < 0.25f ? 1f : Mathf.Lerp(0, now, Mathf.Clamp01((lu - 0.25f) / 0.4f));
            Gfx.Bar(bar, fill, new Color(0.45f, 0.75f, 1f));
            if (up && lu > 0 && lu < 0.25f) Gfx.Rect(bar, new Color(1, 1, 1, 1 - lu / 0.25f));   // 涨满那一闪
            int lvShown = showLevel ? party.Level : resultFromLevel;
            Gfx.Text(new Rect(bar.x, bar.yMax + 2, bar.width, 16), "Lv " + lvShown + (showLevel || !up ? "　距下一级 " + (party.ExpToNext - party.Exp) : ""), 13, new Color(1, 1, 1, 0.65f), TextAnchor.MiddleRight);
            y += 34;

            // ---- 升级了：一行一行亮出来，旧值 → 新值滚上去，滚到头弹一下，旁边飘出绿色 +n ----
            if (up)
            {
                y += 4;
                var stats = new List<KeyValuePair<string, int[]>>
                {
                    new KeyValuePair<string, int[]>("等级", new[] { resultFromLevel, party.Level }),
                    new KeyValuePair<string, int[]>("法力上限", new[] { resultFromMaxTP, party.MaxTP }),
                };
                foreach (var kv in resultMaxBefore) stats.Add(new KeyValuePair<string, int[]>(kv.Key.Name + "　兵力上限", new[] { kv.Value, kv.Key.MaxSoldiers }));
                for (int i = 0; i < stats.Count; i++)
                    StatRow(new Rect(r.x + 40, y + i * 30, w - 80, 28), stats[i].Key, stats[i].Value[0], stats[i].Value[1], lu - 0.3f - i * 0.15f);
                y += stats.Count * 30 + 8;
            }

            // ---- 战利品、新法术：图标一排 ----
            if (extra && t > 0.8f)
            {
                Gfx.Text(new Rect(r.x + 24, y, 80, 20), loot.Count > 0 ? "战利品" : "习得", 15, new Color(1, 1, 1, 0.5f));
                float ix = r.x + 100;
                var shown = new List<string>();
                foreach (var id in loot)
                {
                    if (shown.Contains(id)) continue;
                    shown.Add(id);
                    var cell = new Rect(ix, y - 4, 48, 48);
                    Gfx.RoundRect(cell, new Color(0.55f, 0.42f, 0.25f, 0.5f));
                    ItemIcon(cell, ItemDb.Get(id));
                    int n = loot.FindAll(x => x == id).Count;
                    if (n > 1) Gfx.Text(new Rect(cell.x, cell.yMax - 18, cell.width - 4, 16), n.ToString(), 13, Color.white, TextAnchor.LowerRight);
                    Gfx.Text(new Rect(cell.xMax + 6, cell.y, 90, cell.height), ItemDb.Get(id).Name, 15, Color.white, TextAnchor.MiddleLeft);
                    ix += 150;
                }
                foreach (var id in resultLearned)
                {
                    var tc = TacticDb.Get(id);
                    var icon = Gfx.Tex("Art/tactic_" + id);
                    var cell = new Rect(ix, y - 4, 48, 48);
                    if (icon != null) Gfx.Image(cell, icon);
                    Gfx.Text(new Rect(cell.xMax + 6, cell.y, 140, cell.height), "习得「" + tc.Name + "」", 15, Accent, TextAnchor.MiddleLeft);
                    ix += 200;
                }
            }

            if (t > 0.9f && Mathf.Repeat(Time.time, 1.2f) < 0.85f)
                Gfx.Text(new Rect(r.x, r.yMax - 26, r.width, 22), "点击继续", 15, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter);
            if (up && lu > 0 && lu < 0.3f) Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(1, 0.97f, 0.85f, 0.45f * (1 - lu / 0.3f)));   // 升级那一下全屏一闪
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) CloseResult();
        }

        /// <summary>毛笔大字：深色描边 + 金色字。</summary>
        void BrushTitle(Rect r, string text, int size)
        {
            if (brushFont == null) brushFont = Resources.Load<Font>("Fonts/BrushFont");
            var st = new GUIStyle(GUI.skin.label) { font = brushFont, fontSize = size, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
            st.normal.textColor = new Color(0.2f, 0.08f, 0.02f, 0.9f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                GUI.Label(new Rect(r.x + Mathf.Cos(ang) * 3, r.y + Mathf.Sin(ang) * 3, r.width, r.height), text, st);
            }
            st.normal.textColor = Accent;
            GUI.Label(r, text, st);
        }

        /// <summary>一行属性：名字　旧值 → 新值（从旧值滚上去，到头弹大一下）　+n 往上飘。k = 这一行出场过去了多久。</summary>
        void StatRow(Rect r, string label, int from, int to, float k)
        {
            if (k <= 0) return;
            float a = Mathf.Clamp01(k / 0.15f);
            float slide = (1 - a) * 30;
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, a);
            Gfx.RoundRect(new Rect(r.x + slide, r.y, r.width, r.height), new Color(1, 1, 1, 0.06f));
            Gfx.Text(new Rect(r.x + 14 + slide, r.y, 200, r.height), label, 17, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft);
            float rollK = Mathf.Clamp01((k - 0.1f) / 0.4f);
            int shown = Mathf.RoundToInt(Mathf.Lerp(from, to, rollK));
            float bump = rollK >= 1 ? Mathf.Max(0, 1 - (k - 0.5f) / 0.2f) : 0;   // 滚到头弹一下
            float vx = r.xMax - 230 + slide;
            Gfx.Text(new Rect(vx, r.y, 70, r.height), from.ToString(), 17, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleRight);
            Gfx.Text(new Rect(vx + 72, r.y, 30, r.height), "→", 17, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(vx + 104, r.y - 4, 80, r.height + 8), shown.ToString(), Mathf.RoundToInt(22 + bump * 10), rollK >= 1 ? Accent : Color.white, TextAnchor.MiddleLeft);
            if (to > from && k > 0.5f)
            {
                float f = Mathf.Clamp01((k - 0.5f) / 0.8f);
                Gfx.Text(new Rect(vx + 170, r.y - f * 8, 60, r.height), "+" + (to - from), 16, new Color(0.5f, 1f, 0.55f, 1 - f * 0.5f), TextAnchor.MiddleLeft);
            }
            GUI.color = old;
        }

        /// <summary>升级时从标题往外迸的金色火星。</summary>
        static void Sparks(Vector2 c, float k)
        {
            if (k > 1.2f) return;
            var old = GUI.color;
            for (int i = 0; i < 26; i++)
            {
                float ang = i * 2.39996f, speed = 140 + (i * 37 % 90);
                float d = speed * k * (1 - k * 0.35f);
                var p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.7f) * d;
                float s = (i % 3 == 0 ? 14 : 9) * (1 - k / 1.2f);
                GUI.color = new Color(1f, 0.85f, 0.4f, 1 - k / 1.2f);
                Gfx.Image(new Rect(p.x - s, p.y - s, s * 2, s * 2), GlowTex());
            }
            GUI.color = old;
        }

        /// <summary>带回弹的缓出：冲过头一点再回来。</summary>
        static float BackEase(float x) { const float c = 1.7f; x -= 1; return 1 + (c + 1) * x * x * x + c * x * x; }

        /// <summary>绕中心转着画一张图（GUI.matrix 有缩放，不能用 RotateAroundPivot，见 WeaponIcon）。</summary>
        static void RotatedImage(Rect r, Texture2D tex, float deg, Color c)
        {
            var oldM = GUI.matrix;
            var oldC = GUI.color;
            var ctr = (Vector3)r.center;
            GUI.matrix = oldM * Matrix4x4.TRS(ctr, Quaternion.Euler(0, 0, deg), Vector3.one) * Matrix4x4.TRS(-ctr, Quaternion.identity, Vector3.one);
            GUI.color = c;
            Gfx.Image(r, tex);
            GUI.matrix = oldM;
            GUI.color = oldC;
        }

        static Texture2D rayTex;

        /// <summary>放射状光芒：12 道光束，中心亮、往外淡。</summary>
        static Texture2D RayTex()
        {
            if (rayTex != null) return rayTex;
            const int n = 256;
            rayTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var v = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f);
                    float d = v.magnitude / (n / 2f), ang = Mathf.Atan2(v.y, v.x);
                    float beam = Mathf.Pow(Mathf.Max(0, Mathf.Cos(ang * 6)), 6);
                    float a = beam * Mathf.Clamp01(1 - d) * Mathf.Clamp01(d * 4) + Mathf.Clamp01(1 - d * 2.2f) * 0.5f;
                    rayTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a)));
                }
            rayTex.Apply();
            return rayTex;
        }

        /// <summary>奖励卡：左边图标（Art/icon_exp、icon_gold，缺图就空着），标签，右边大数字。</summary>
        static void RewardCard(Rect r, string iconPath, string label, string value, Color accent)
        {
            Gfx.RoundRect(r, new Color(1, 1, 1, 0.07f));
            Gfx.RoundRect(new Rect(r.x, r.y, 4, r.height), accent);
            var icon = Gfx.Tex(iconPath);
            float x = r.x + 14;
            if (icon != null) { Gfx.Image(new Rect(x, r.y + 7, 44, 44), icon); x += 52; }
            Gfx.Text(new Rect(x, r.y, 80, r.height), label, 18, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            Gfx.Text(new Rect(r.x, r.y, r.width - 16, r.height), value, 30, accent, TextAnchor.MiddleRight);
        }
    }
}
