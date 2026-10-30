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
    ///   经验 / 金钱两张大卡、经验条，战利品 / 新法术的空格子；
    ///   奖励领取演出：压暗，奖励图标一个个在中间炸开亮相，再碎成一群小图标飞进各自的卡片 / 格子，
    ///   每落进去一个，那边数字就涨一截、弹一下；经验飞满了升级；
    ///   升级了（见 DrawLevelUp）：蓄力 → 爆开 → 余韵，再各项数值逐行滚动，升级学会的法术最后单独亮相飞进格子。
    /// 演出中点一下快进（升级爆开那一下不让跳）；演完停 AutoClose 秒自己出场回地图，不用点（等不及也可以点）。
    /// </summary>
    public partial class GameRoot
    {
        float resultAt;
        int resultFromLevel, resultFromMaxTP;
        float resultFromExp;                                                       // 打之前经验条走到几成
        readonly List<KeyValuePair<General, int>> resultMaxBefore = new List<KeyValuePair<General, int>>();
        float resultLevelAt, resultLevelEnd, resultDoneAt;                         // 升级爆开的时刻；升级数值滚完的时刻；奖励全部飞完的时刻
        const float LevelCharge = 0.45f;                                           // 经验条涨满到爆开之间蓄力多久
        const float AutoClose = 1.2f;                                              // 演完再停多久自动出场
        readonly List<string> resultLearned = new List<string>();                 // 新学会的法术 Id
        readonly Dictionary<string, PuppetActor> resultFigs = new Dictionary<string, PuppetActor>();
        readonly List<ResultLoot> resultLoot = new List<ResultLoot>();

        /// <summary>一样奖励：经验、金钱、一种掉落物品、一个新法术。Target 是它要飞进去的格子，每帧排版时填。</summary>
        class ResultLoot
        {
            public string Icon, Item, Tactic;   // Icon = 贴图路径（经验/金钱）；Item / Tactic 二选一
            public int Total, Bits;             // 总数；碎成几个小图标飞
            public Color Tint;
            public Rect Target;
            public float PopAt;                 // 在中间炸开亮相的时刻（相对结算开始）
            public float ShowAt, FlyAt;         // 这一轮开始压暗、一起碎开的时刻
            public int Slot, RowN;              // 这一轮亮相时排第几个、一共几个
            public float[] Land;                // 每个小图标落进格子的时刻

            /// <summary>到 t 为止落进去几成。</summary>
            public float Arrived(float t) { int n = 0; foreach (var a in Land) if (t >= a) n++; return (float)n / Land.Length; }
            public float LastLand(float t) { float l = -9; foreach (var a in Land) if (t >= a) l = a; return l; }
            public int Shown(float t) { return Mathf.RoundToInt(Total * Arrived(t)); }
        }

        const float LootShowAt = 0.55f, LootPopGap = 0.13f, LootHold = 0.85f, LootScatter = 0.25f, LootFly = 0.4f;

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

            resultLoot.Clear();
            var drops = new List<ResultLoot>();
            foreach (var id in battle.RewardItems)
            {
                var hit = drops.Find(l => l.Item == id);
                if (hit != null) { hit.Total++; continue; }
                drops.Add(new ResultLoot { Item = id, Total = 1, Tint = new Color(1f, 0.6f, 0.95f) });
            }
            foreach (var l in drops) l.Bits = Mathf.Clamp(l.Total * 3, 3, 6);
            var spells = new List<ResultLoot>();   // 战斗里学会的法术都是升级学的，放到升级演完再亮相
            foreach (var id in resultLearned) spells.Add(new ResultLoot { Tactic = id, Total = 1, Bits = 4, Tint = new Color(0.75f, 0.6f, 1f) });
            var money = new List<ResultLoot>();
            if (battle.RewardExp > 0) money.Add(new ResultLoot { Icon = "Art/icon_exp", Total = battle.RewardExp, Bits = Mathf.Clamp(battle.RewardExp, 1, 8), Tint = new Color(0.55f, 0.8f, 1f) });
            if (battle.RewardGold > 0) money.Add(new ResultLoot { Icon = "Art/icon_gold", Total = battle.RewardGold, Bits = Mathf.Clamp(battle.RewardGold, 1, 8), Tint = Accent });

            // 排时间，分三轮：先物品，飞完再经验 / 金钱，经验飞满蓄力爆开升级，数值滚完再亮相新法术。
            // 每轮：一个个亮相 → 停一下 → 一起碎开 → 小图标排着队飞进去（后一样比前一样晚一点）
            bool up = party.Level > resultFromLevel;
            resultDoneAt = 0.9f;
            resultLevelAt = 0.95f;
            resultLevelEnd = 0;
            float start = LootShowAt;
            foreach (var round in new[] { drops, money, spells })
            {
                int n = round.Count;
                if (n == 0) continue;
                if (round == spells && up) start = Mathf.Max(start, resultLevelEnd + 0.2f);
                float flyAt = start + (n - 1) * LootPopGap + LootHold, end = start;
                for (int i = 0; i < n; i++)
                {
                    var l = round[i];
                    l.Slot = i; l.RowN = n; l.ShowAt = start; l.FlyAt = flyAt;
                    l.PopAt = start + i * LootPopGap;
                    l.Land = new float[l.Bits];
                    for (int j = 0; j < l.Bits; j++) l.Land[j] = flyAt + LootScatter + i * 0.08f + j * 0.045f + LootFly;
                    end = Mathf.Max(end, l.Land[l.Bits - 1]);
                    if (l.Icon == "Art/icon_exp")
                    {
                        resultLevelAt = l.Land[l.Bits - 1] + LevelCharge;
                        resultLevelEnd = resultLevelAt + 0.3f + (2 + resultMaxBefore.Count) * 0.15f + 0.6f;
                    }
                    resultLoot.Add(l);
                }
                resultDoneAt = Mathf.Max(resultDoneAt, end);
                start = end + 0.15f;
            }

            resultAt = Time.time;
            bstate = BState.Result;
        }

        void CloseResult()
        {
            float t = Time.time - resultAt;
            bool up = party.Level > resultFromLevel;
            float charge = resultLevelAt - LevelCharge, end = Mathf.Max(resultDoneAt, up ? resultLevelEnd : 0);
            if (up && t > charge && t < resultLevelAt + 0.6f) return;   // 蓄力到爆开这一下不让跳
            // 还在演：点一下快进——升级了先跳到蓄力，否则跳到全部演完；刚弹出来的一下不吃，免得连点
            if (t < end) { if (t > 0.3f) resultAt = Time.time - (up && t < charge ? charge : end); return; }
            if (t < end + 0.3f) return;
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
            var exp = resultLoot.Find(l => l.Icon == "Art/icon_exp");
            var gold = resultLoot.Find(l => l.Icon == "Art/icon_gold");
            float expK = exp != null ? exp.Arrived(t) : 1f;
            var drops = resultLoot.FindAll(l => l.Item != null || l.Tactic != null);
            bool extra = drops.Count > 0;

            float w = Mathf.Min(Gfx.VW - 24, 720);
            int statRows = up ? 2 + resultMaxBefore.Count : 0;
            float h = Mathf.Min(Gfx.VH - 16, 314 + (up ? 12 + statRows * 30 : 0) + (extra ? 92 : 0));
            var r = new Rect((Gfx.VW - w) * 0.5f, (Gfx.VH - h) * 0.5f, w, h);
            float pop = EaseOut(resultAt, 0.25f);
            r.y += (1f - pop) * 30f;

            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, 0.55f * pop));
            float lu = t - resultLevelAt;   // 升级过去了多久（< 0 还没到）

            // 震屏：蓄力时越抖越厉害，爆开那一下猛震再衰减。只震面板，背景压暗和全屏闪光不动
            var baseM = GUI.matrix;
            if (up && lu > -LevelCharge)
            {
                float c = Mathf.Clamp01((lu + LevelCharge) / LevelCharge);
                float amp = lu < 0 ? 3f * c * c : 18f * Mathf.Exp(-lu * 7f);
                var off = new Vector3((Mathf.PerlinNoise(Time.time * 45f, 0.3f) - 0.5f) * 2f, (Mathf.PerlinNoise(0.7f, Time.time * 45f) - 0.5f) * 2f, 0) * amp;
                GUI.matrix = baseM * Matrix4x4.Translate(off);
            }

            // 外框：升级后变成亮金色，爆开时最亮，之后一直轻轻呼吸
            var frame = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            if (up && lu > 0) frame = new Color(1f, 0.85f, 0.4f, 0.5f + 0.5f * Mathf.Exp(-lu * 2.5f) + 0.12f * Mathf.Sin(Time.time * 5f));
            Gfx.RoundRect(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), frame);
            Gfx.Modern(r, 0.96f);
            bool repaint = Event.current.type == EventType.Repaint;

            // ---- 毛笔大字“胜利”：先放大再落定；升级那一下换成“升级”（DrawLevelUp 画） ----
            var title = new Rect(r.x, r.y + 4, r.width, 70);
            bool showLevel = up && lu > 0;
            if (!showLevel)
            {
                float s = 1f + Mathf.Max(0f, 1f - t / 0.2f) * 0.6f;
                if (up && lu > -LevelCharge)
                {
                    // 蓄力：“胜利”背后的光越聚越亮
                    float c = Mathf.Clamp01((lu + LevelCharge) / LevelCharge);
                    var old = GUI.color;
                    GUI.color = new Color(1f, 0.9f, 0.6f, c * 0.9f);
                    float gs = 60 + 90 * c;
                    Gfx.Image(new Rect(title.center.x - gs * 1.6f, title.center.y - gs, gs * 3.2f, gs * 2), GlowTex());
                    GUI.color = old;
                    s += 0.08f * c * Mathf.Sin(Time.time * 60f);
                }
                BrushTitle(title, "胜利", Mathf.RoundToInt(58 * s));
            }
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

            // ---- 经验 / 金钱两张大卡：小图标飞进来一个，数字涨一截、弹一下 ----
            float cw = (w - 60) * 0.5f;
            var expCard = new Rect(r.x + 20, y, cw, 72);
            var goldCard = new Rect(r.x + 40 + cw, y, cw, 72);
            if (exp != null) exp.Target = new Rect(expCard.x + 12, expCard.y + 6, 60, 60);
            if (gold != null) gold.Target = new Rect(goldCard.x + 12, goldCard.y + 6, 60, 60);
            RewardCard(expCard, "Art/icon_exp", "经验", "+" + (exp != null ? exp.Shown(t) : 0), new Color(0.55f, 0.8f, 1f), LandBump(exp, t));
            RewardCard(goldCard, "Art/icon_gold", "金钱", "+" + (gold != null ? gold.Shown(t) : 0), Accent, LandBump(gold, t));
            y += 80;
            // 经验条：跟着飞进来的经验涨；升级的话先涨满、一闪，再从头涨到现在的位置
            var bar = new Rect(r.x + 20, y + 4, w - 40, 10);
            float now = (float)party.Exp / party.ExpToNext, fill;
            if (!up) fill = Mathf.Lerp(resultFromExp, now, expK);
            else if (lu < 0) fill = Mathf.Lerp(resultFromExp, 1f, expK);
            else fill = lu < 0.25f ? 1f : Mathf.Lerp(0, now, Mathf.Clamp01((lu - 0.25f) / 0.4f));
            Gfx.Bar(bar, fill, new Color(0.45f, 0.75f, 1f));
            if (up && lu > 0 && lu < 0.25f) Gfx.Rect(bar, new Color(1, 1, 1, 1 - lu / 0.25f));   // 涨满那一闪
            if (up && lu > -LevelCharge && lu < 0)
            {
                // 蓄力：涨满的经验条发白、一跳一跳地发烫
                float c = Mathf.Clamp01((lu + LevelCharge) / LevelCharge);
                var old = GUI.color;
                GUI.color = new Color(0.8f, 0.95f, 1f, c * (0.55f + 0.45f * Mathf.Sin(Time.time * 50f)));
                Gfx.Image(new Rect(bar.x - 30, bar.y - 22, bar.width + 60, bar.height + 44), GlowTex());
                GUI.color = old;
                Gfx.Rect(bar, new Color(1, 1, 1, c * 0.6f));
            }
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

            // ---- 战利品、新法术：先是空格子，飞进来才亮出图标和名字 ----
            if (extra)
            {
                Gfx.Text(new Rect(r.x + 24, y, 80, 20), drops.Exists(l => l.Item != null) ? "战利品" : "习得", 15, new Color(1, 1, 1, 0.5f));
                float ix = r.x + 100;
                float iw = Mathf.Min(170, (r.xMax - 16 - ix) / drops.Count);   // 竖屏放不下就挤一挤，名字跟着收窄
                foreach (var l in drops)
                {
                    var cell = new Rect(ix, y - 10, 76, 76);
                    l.Target = cell;
                    Gfx.RoundRect(cell, l.Item != null ? new Color(0.55f, 0.42f, 0.25f, 0.5f) : new Color(1, 1, 1, 0.06f));
                    int got = l.Shown(t);
                    if (l.Arrived(t) > 0)
                    {
                        float b = 1 + 0.35f * LandBump(l, t);
                        var big = Scaled(cell, b);
                        LootIcon(l, big);
                        if (l.Item != null)
                        {
                            if (got > 1) Gfx.Text(new Rect(cell.x, cell.yMax - 18, cell.width - 4, 16), got.ToString(), 13, Color.white, TextAnchor.LowerRight);
                            Gfx.Text(new Rect(cell.xMax + 4, cell.y, iw - 82, cell.height), ItemDb.Get(l.Item).Name, 15, Color.white, TextAnchor.MiddleLeft);
                        }
                        else Gfx.Text(new Rect(cell.xMax + 4, cell.y, Mathf.Max(iw, 220) - 82, cell.height), "习得「" + TacticDb.Get(l.Tactic).Name + "」", 15, Accent, TextAnchor.MiddleLeft);
                    }
                    ix += l.Item != null ? iw : Mathf.Max(iw, 220);
                }
            }

            if (up) DrawLevelUp(r, title, bar, lu);
            DrawLootShow(r, t);

            float end = Mathf.Max(resultDoneAt, up ? resultLevelEnd : 0);
            GUI.matrix = baseM;
            // 升级爆开那一下全屏白闪：头几帧几乎全白，再褪成暖色
            if (up && lu > 0 && lu < 0.5f)
                Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(1, 0.97f, 0.88f, lu < 0.05f ? 0.9f : 0.9f * Mathf.Pow(1 - (lu - 0.05f) / 0.45f, 2)));
            // 全部演完停一会儿（看清数值）自己关，不用点
            if (t > end + AutoClose) { EndBattle(); return; }
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) CloseResult();
        }

        /// <summary>
        /// 奖励领取演出（画在面板最上面）：
        ///   亮相：压暗，奖励在面板中间一排，一个个先炸出白金光团 + 光芒 + 星星，图标放大落定，背后一圈光晕、底下写数量；
        ///   碎开：大图标缩没，原地迸出一群小图标散开；
        ///   飞入：小图标排着队沿弧线加速飞向各自格子，落进去的那一下闪颗星。
        /// </summary>
        void DrawLootShow(Rect panel, float t)
        {
            int n = resultLoot.Count;
            if (n == 0 || t < LootShowAt) return;
            var old = GUI.color;

            // 压暗：每轮亮相时压，碎开后慢慢放亮
            float dim = 0;
            foreach (var l in resultLoot)
                dim = Mathf.Max(dim, Mathf.Clamp01((t - l.ShowAt) / 0.15f) * (1 - Mathf.Clamp01((t - l.FlyAt) / 0.3f)));
            if (dim > 0) Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, 0.6f * dim));

            for (int i = 0; i < n; i++)
            {
                var l = resultLoot[i];
                float gap = Mathf.Min(210, (panel.width - 20) / l.RowN), flyAt = l.FlyAt;
                var c = new Vector2(panel.center.x + (l.Slot - (l.RowN - 1) * 0.5f) * gap, panel.center.y - 10);
                float k = t - l.PopAt;
                if (k < 0) continue;

                if (t < flyAt + 0.2f)
                {
                    float gone = Mathf.Clamp01((t - flyAt) / 0.2f);   // 碎开时缩没
                    // 先炸开：白金光团一胀一收，光芒转，星星迸
                    if (k < 0.45f)
                    {
                        float f = k / 0.45f;
                        float gs = 260 * Mathf.Sin(Mathf.Min(1, f * 1.6f) * Mathf.PI * 0.5f) * (1.15f - f * 0.4f);
                        GUI.color = new Color(1f, 0.95f, 0.75f, 1 - f * f);
                        Gfx.Image(new Rect(c.x - gs, c.y - gs, gs * 2, gs * 2), GlowTex());
                        RotatedImage(new Rect(c.x - 170, c.y - 170, 340, 340), RayTex(), k * 160 + i * 30, new Color(1f, 0.9f, 0.6f, 0.8f * (1 - f)));
                        for (int j = 0; j < 4; j++)
                        {
                            float ang = j * 1.7f + i, d = 60 + f * 90;
                            float ss = 38 * (1 - f) * (j % 2 == 0 ? 1 : 0.6f);
                            var p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * d;
                            GUI.color = new Color(1, 1, 0.9f, 1 - f);
                            Gfx.Image(new Rect(p.x - ss, p.y - ss, ss * 2, ss * 2), StarTex());
                        }
                    }
                    if (k > 0.12f)
                    {
                        float a = Mathf.Clamp01((k - 0.12f) / 0.1f) * (1 - gone);
                        float s = (1 + 0.6f * (1 - BackEase(Mathf.Clamp01((k - 0.12f) / 0.3f)))) * (1 - gone * 0.7f);
                        // 背后一圈慢慢呼吸的光晕
                        float halo = 135 + 10 * Mathf.Sin(Time.time * 5 + i);
                        GUI.color = new Color(l.Tint.r, l.Tint.g, l.Tint.b, 0.75f * a);
                        Gfx.Image(new Rect(c.x - halo, c.y - halo, halo * 2, halo * 2), GlowTex());
                        GUI.color = new Color(1, 1, 1, a);
                        LootIcon(l, new Rect(c.x - 75 * s, c.y - 75 * s, 150 * s, 150 * s));
                        GUI.color = old;
                        string count = l.Tactic != null ? TacticDb.Get(l.Tactic).Name : l.Item != null ? "×" + l.Total : "+" + l.Total;
                        Gfx.Text(new Rect(c.x - 100, c.y + 70, 200, 40), count, 34, new Color(1, 1, 1, a), TextAnchor.MiddleCenter);
                    }
                }

                // 碎开、飞入：每个小图标先往外散，再沿弧线加速飞进格子
                if (t >= flyAt)
                {
                    for (int j = 0; j < l.Bits; j++)
                    {
                        float land = l.Land[j];
                        if (t >= land) continue;
                        float ang = (i * 7 + j) * 2.39996f, rad = 70 + (i * 13 + j * 29) % 60;
                        var scatter = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                        float sk = Mathf.Clamp01((t - flyAt) / LootScatter);
                        Vector2 p;
                        float u = 0;   // 飞了几成
                        if (t < land - LootFly) p = Vector2.Lerp(c, scatter, 1 - (1 - sk) * (1 - sk));
                        else
                        {
                            u = 1 - (land - t) / LootFly;
                            u *= u;   // 加速冲进去
                            var to = l.Target.center;
                            var dir = to - scatter;
                            var ctrl = (scatter + to) * 0.5f + new Vector2(-dir.y, dir.x).normalized * ((i % 2 == 0 ? 1 : -1) * (50 + j * 17 % 40));
                            p = (1 - u) * (1 - u) * scatter + 2 * (1 - u) * u * ctrl + u * u * to;
                        }
                        float bs = Mathf.Lerp(44 * (0.6f + 0.4f * sk), l.Target.width * 0.5f, u * u);   // 散开时大，飞到跟前缩成格子里那么大
                        GUI.color = new Color(l.Tint.r, l.Tint.g, l.Tint.b, 0.6f);
                        Gfx.Image(new Rect(p.x - bs * 1.6f, p.y - bs * 1.6f, bs * 3.2f, bs * 3.2f), GlowTex());
                        GUI.color = old;
                        LootIcon(l, new Rect(p.x - bs, p.y - bs, bs * 2, bs * 2));
                    }
                }

                // 落进去那一下闪颗星
                float since = t - l.LastLand(t);
                if (since >= 0 && since < 0.25f)
                {
                    float ss = 50 * (1 - since / 0.25f);
                    var p = l.Target.center;
                    GUI.color = new Color(1, 1, 0.85f, 1 - since / 0.25f);
                    Gfx.Image(new Rect(p.x - ss, p.y - ss, ss * 2, ss * 2), StarTex());
                }
            }
            GUI.color = old;
        }

        /// <summary>奖励的图标：经验/金钱贴图、物品、法术。</summary>
        static void LootIcon(ResultLoot l, Rect r)
        {
            if (l.Item != null) { ItemIcon(Scaled(r, 1.2f), ItemDb.Get(l.Item)); return; }   // ItemIcon 四周留了边，放大补回来，和经验金钱一样大
            var tex = Gfx.Tex(l.Icon ?? "Art/tactic_" + l.Tactic);
            if (tex != null) Gfx.Image(r, tex);
        }

        /// <summary>刚落进一个小图标时的回弹量，1 → 0（0.15 秒）。</summary>
        static float LandBump(ResultLoot l, float t)
        {
            if (l == null) return 0;
            float since = t - l.LastLand(t);
            return since < 0 || since > 0.15f ? 0 : 1 - since / 0.15f;
        }

        static Rect Scaled(Rect r, float s) { return new Rect(r.center.x - r.width * s * 0.5f, r.center.y - r.height * s * 0.5f, r.width * s, r.height * s); }

        static Texture2D starTex;

        /// <summary>四角星闪光：十字两道细光束 + 中心一点亮。</summary>
        static Texture2D StarTex()
        {
            if (starTex != null) return starTex;
            const int n = 64;
            starTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / n * 2 - 1), v = Mathf.Abs((y + 0.5f) / n * 2 - 1);
                    float beam = Mathf.Max((1 - u) * (1 - u) * Mathf.Exp(-v * 18), (1 - v) * (1 - v) * Mathf.Exp(-u * 18));
                    float core = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(u * u + v * v) * 2.5f), 2);
                    starTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(beam + core)));
                }
            starTex.Apply();
            return starTex;
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
            if (k > 0.5f && k < 0.75f)
            {
                // 滚到头闪颗星
                float f = (k - 0.5f) / 0.25f, ss = 22 * (1 - f);
                var sc = GUI.color;
                GUI.color = new Color(1, 0.95f, 0.7f, a * (1 - f));
                Gfx.Image(new Rect(vx + 120 - ss, r.center.y - ss, ss * 2, ss * 2), StarTex());
                GUI.color = sc;
            }
            if (to > from && k > 0.5f)
            {
                float f = Mathf.Clamp01((k - 0.5f) / 0.8f);
                Gfx.Text(new Rect(vx + 170, r.y - f * 8, 60, r.height), "+" + (to - from), 16, new Color(0.5f, 1f, 0.55f, 1 - f * 0.5f), TextAnchor.MiddleLeft);
            }
            GUI.color = old;
        }

        /// <summary>
        /// 升级演出，三段：
        ///   蓄力（爆开前 LevelCharge 秒）：一串能量光点从经验条被吸进标题（经验条发烫、“胜利”背后聚光、面板越抖越厉害在 DrawResult 里）；
        ///   爆开：全屏白闪、面板猛震，三道冲击波环往外扩，一圈速度线射出去，几十颗火星带重力迸开，
        ///         “胜利”放大散掉，毛笔“升级”从三倍大砸下来回弹，背后两层光芒对转；
        ///   余韵：“Lv.n”徽记跟着砸下来再炸一小圈星，金色星屑从面板上方飘落，外框一直呼吸。
        /// lu = 升级过去了多久（< 0 还在蓄力）。
        /// </summary>
        void DrawLevelUp(Rect panel, Rect title, Rect bar, float lu)
        {
            var c = title.center;
            var old = GUI.color;

            if (lu < 0)
            {
                // 蓄力：光点从经验条各处被吸向标题，越近越快
                for (int i = 0; i < 18; i++)
                {
                    float u = Mathf.Clamp01((lu + LevelCharge - i * 0.012f) / 0.24f);
                    if (u <= 0 || u >= 1) continue;
                    var from = new Vector2(bar.x + bar.width * ((i * 0.618f) % 1f), bar.center.y);
                    var ctrl = new Vector2((from.x + c.x) * 0.5f + (i % 2 == 0 ? 80 : -80), (from.y + c.y) * 0.5f);
                    float e = u * u;
                    var p = (1 - e) * (1 - e) * from + 2 * (1 - e) * e * ctrl + e * e * c;
                    float ps = 9 + 5 * (1 - u);
                    GUI.color = Color.Lerp(new Color(0.6f, 0.85f, 1f), new Color(1f, 0.9f, 0.5f), u);
                    Gfx.Image(new Rect(p.x - ps, p.y - ps, ps * 2, ps * 2), GlowTex());
                }
                GUI.color = old;
                return;
            }

            // 背后两层光芒对转，爆开时又大又亮，之后收成常亮
            float boost = Mathf.Exp(-lu * 3f);
            float ra = Mathf.Clamp01(lu / 0.1f) * (0.45f + 0.5f * boost + 0.08f * Mathf.Sin(Time.time * 4f));
            float rs = 230 + 120 * boost;
            RotatedImage(new Rect(c.x - rs, c.y - rs, rs * 2, rs * 2), RayTex(), Time.time * 25f, new Color(1f, 0.82f, 0.35f, ra));
            RotatedImage(new Rect(c.x - rs * 0.7f, c.y - rs * 0.7f, rs * 1.4f, rs * 1.4f), RayTex(), -Time.time * 40f, new Color(1f, 0.95f, 0.7f, ra * 0.7f));

            // 冲击波：三道环先后往外扩
            for (int k = 0; k < 3; k++)
            {
                float f = (lu - k * 0.08f) / 0.65f;
                if (f <= 0 || f >= 1) continue;
                float rr = 40 + 560 * (1 - (1 - f) * (1 - f));
                GUI.color = k == 1 ? new Color(1f, 0.8f, 0.35f, Mathf.Pow(1 - f, 1.5f)) : new Color(1f, 1f, 0.9f, Mathf.Pow(1 - f, 1.5f));
                Gfx.Image(new Rect(c.x - rr, c.y - rr * 0.75f, rr * 2, rr * 1.5f), RingTex());
            }

            // 速度线：一圈细长光条往外射
            if (lu < 0.4f)
            {
                float f = lu / 0.4f;
                for (int i = 0; i < 28; i++)
                {
                    float ang = i * 360f / 28 + (i % 3) * 4f;
                    float d = 70 + f * (420 + (i * 53 % 160));
                    float len = 160 * (1 - f) + 30;
                    var dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad) * 0.75f);
                    var p = c + dir * d;
                    RotatedImage(new Rect(p.x - len * 0.5f, p.y - 3, len, 6), GlowTex(), Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, new Color(1f, 0.95f, 0.75f, 1 - f));
                }
            }

            // 火星：往外迸，带重力往下坠，金 / 白 / 橙三色，掺几颗四角星
            if (lu < 1.6f)
            {
                for (int i = 0; i < 48; i++)
                {
                    float ang = i * 2.39996f, speed = 260 + (i * 37 % 300);
                    var v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f - 0.35f) * speed;
                    var p = c + v * lu * (1 - lu * 0.3f) + new Vector2(0, 380 * lu * lu);
                    float life = 1 - lu / 1.6f;
                    float ps = (i % 4 == 0 ? 16 : 8) * (0.4f + 0.6f * life);
                    GUI.color = i % 3 == 0 ? new Color(1, 1, 0.9f, life) : i % 3 == 1 ? new Color(1f, 0.8f, 0.3f, life) : new Color(1f, 0.55f, 0.2f, life);
                    Gfx.Image(new Rect(p.x - ps, p.y - ps, ps * 2, ps * 2), i % 4 == 0 ? StarTex() : GlowTex());
                }
            }
            GUI.color = old;

            // “胜利”放大散掉，“升级”从三倍大砸下来回弹
            if (lu < 0.25f)
            {
                GUI.color = new Color(1, 1, 1, 1 - lu / 0.25f);
                BrushTitle(title, "胜利", Mathf.RoundToInt(58 * (1 + lu * 4)));
                GUI.color = old;
            }
            float s = 1f + Mathf.Max(0, 2f * (1 - BackEase(Mathf.Clamp01(lu / 0.3f))));
            BrushTitle(title, "升级", Mathf.RoundToInt(58 * s));

            // “Lv.n”徽记：晚一拍砸在“升级”右边，落地再炸一小圈星
            float bk = lu - 0.3f;
            if (bk > 0)
            {
                float bs = 1f + Mathf.Max(0, 1.5f * (1 - BackEase(Mathf.Clamp01(bk / 0.25f))));
                var badge = new Rect(c.x + 70, title.y + 14, 120, 50);
                BrushTitle(badge, "Lv." + party.Level, Mathf.RoundToInt(34 * bs));
                float hk = bk - 0.2f;
                if (hk > 0 && hk < 0.5f)
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i * Mathf.PI / 4 + 0.3f, d = 20 + hk * 140;
                        var p = badge.center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * d;
                        float ps = 14 * (1 - hk / 0.5f);
                        GUI.color = new Color(1, 0.95f, 0.7f, 1 - hk / 0.5f);
                        Gfx.Image(new Rect(p.x - ps, p.y - ps, ps * 2, ps * 2), StarTex());
                    }
                GUI.color = old;
            }

            // 金色星屑从面板上方飘落，一闪一闪，三秒后淡掉
            float fk = lu - 0.1f;
            if (fk > 0 && fk < 3.5f)
            {
                float fade = Mathf.Clamp01((3.5f - fk) / 0.8f);
                for (int i = 0; i < 36; i++)
                {
                    float x = panel.x + panel.width * ((i * 0.618f + 0.1f) % 1f) + Mathf.Sin(fk * 2.5f + i) * 12;
                    float py = panel.y - 20 - (i * 47 % 120) + fk * (110 + i * 31 % 120);
                    if (py > panel.yMax) continue;
                    float tw = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f + i * 1.7f);
                    float ps = (i % 3 == 0 ? 11 : 6) * (0.6f + 0.4f * tw);
                    GUI.color = new Color(1f, 0.88f, 0.45f, fade * (0.4f + 0.6f * tw));
                    Gfx.Image(new Rect(x - ps, py - ps, ps * 2, ps * 2), i % 3 == 0 ? StarTex() : GlowTex());
                }
                GUI.color = old;
            }
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

        /// <summary>奖励卡：左边图标（Art/icon_exp、icon_gold，缺图就空着），标签，右边大数字；bump 是刚落进奖励时的回弹（1 → 0）。</summary>
        static void RewardCard(Rect r, string iconPath, string label, string value, Color accent, float bump)
        {
            Gfx.RoundRect(r, new Color(1, 1, 1, 0.07f));
            Gfx.RoundRect(new Rect(r.x, r.y, 4, r.height), accent);
            var icon = Gfx.Tex(iconPath);
            float x = r.x + 14;
            if (icon != null) { Gfx.Image(Scaled(new Rect(r.x + 12, r.y + 6, 60, 60), 1 + 0.25f * bump), icon); x = r.x + 80; }
            Gfx.Text(new Rect(x, r.y, 80, r.height), label, 18, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            Gfx.Text(new Rect(r.x, r.y - 6, r.width - 16, r.height + 12), value, Mathf.RoundToInt(30 + bump * 8), Color.Lerp(accent, Color.white, bump * 0.6f), TextAnchor.MiddleRight);
        }
    }
}
