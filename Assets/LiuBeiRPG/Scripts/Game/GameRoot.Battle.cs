using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 战斗画面：吞食天地式菜单（下令 / 一齐攻击 / 撤退），逐个武将下令，然后按顺序播放战报。
    /// 逻辑全部在 Core/BattleSystem.cs，这里只负责菜单和表现。
    /// </summary>
    public partial class GameRoot
    {
        enum BState { Playing, TopMenu, GeneralMenu, TacticMenu, TargetEnemy, TargetAlly }

        const float EventDelay = 0.75f;

        BattleSystem battle;
        Action onBattleVictory;
        BState bstate;
        int menuIndex;

        // 下令阶段
        List<General> commandOrder;
        int commandIdx;
        readonly List<BattleCommand> commands = new List<BattleCommand>();
        BattleCommand pending;
        int reservedTP;

        // 播放阶段
        readonly Queue<BattleEvent> playQueue = new Queue<BattleEvent>();
        readonly List<string> log = new List<string>();
        readonly Dictionary<General, int> shownSoldiers = new Dictionary<General, int>();
        readonly Dictionary<General, float> flashUntil = new Dictionary<General, float>();
        float nextEventTime;
        Action afterPlay;
        bool rewardsGiven;

        public void StartBattle(List<General> enemies, bool isBoss, Action onVictory = null)
        {
            battle = new BattleSystem(party, enemies, isBoss, rng);
            onBattleVictory = onVictory;
            mode = Mode.Battle;
            log.Clear();
            shownSoldiers.Clear();
            flashUntil.Clear();
            playQueue.Clear();
            rewardsGiven = false;
            foreach (var g in party.Members.Concat(enemies)) shownSoldiers[g] = g.Soldiers;

            var intro = new List<BattleEvent>
            {
                new BattleEvent(isBoss ? "黄巾军主力杀到！" : "遭遇黄巾军！"),
            };
            Play(intro, OpenTopMenu);
        }

        // ---------------- 播放 ----------------

        void Play(List<BattleEvent> events, Action then)
        {
            bstate = BState.Playing;
            foreach (var e in events) playQueue.Enqueue(e);
            afterPlay = then;
            nextEventTime = 0;
        }

        void UpdateBattle()
        {
            if (bstate != BState.Playing || Time.time < nextEventTime) return;
            StepPlayback();
        }

        void StepPlayback()
        {
            if (playQueue.Count > 0)
            {
                var e = playQueue.Dequeue();
                log.Add(e.Text);
                if (log.Count > 50) log.RemoveAt(0);
                if (e.Target != null && e.SoldiersAfter >= 0)
                {
                    shownSoldiers[e.Target] = e.SoldiersAfter;
                    if (e.IsDamage) flashUntil[e.Target] = Time.time + 0.3f;
                }
                nextEventTime = Time.time + EventDelay;
                return;
            }
            if (afterPlay != null)
            {
                var a = afterPlay;
                afterPlay = null;
                a();
            }
        }

        /// <summary>一段战报播完后：判断胜负，或者进入下一回合。</summary>
        void AfterTurn()
        {
            foreach (var g in shownSoldiers.Keys.ToList()) shownSoldiers[g] = g.Soldiers;
            switch (battle.Result)
            {
                case BattleResult.None:
                    OpenTopMenu();
                    break;
                case BattleResult.Victory:
                    if (!rewardsGiven)
                    {
                        rewardsGiven = true;
                        Play(battle.GrantRewards(), EndBattle);
                    }
                    break;
                case BattleResult.Defeat:
                    Play(new List<BattleEvent> { new BattleEvent("我军全军覆没……") }, () => { battle = null; mode = Mode.GameOver; });
                    break;
                case BattleResult.Fled:
                    EndBattle();
                    break;
            }
        }

        void EndBattle()
        {
            var victory = battle.Result == BattleResult.Victory;
            var cb = onBattleVictory;
            battle = null;
            onBattleVictory = null;
            foreach (var m in party.Members) m.Defending = false;
            mode = Mode.Map;
            stepsSinceBattle = 0;
            if (victory && cb != null) cb();
        }

        // ---------------- 菜单 ----------------

        static readonly string[] TopItems = { "下令", "一齐攻击", "撤退" };
        static readonly string[] GeneralItems = { "攻击", "策略", "防御" };

        void OpenTopMenu()
        {
            bstate = BState.TopMenu;
            menuIndex = 0;
        }

        void StartCommandInput()
        {
            commandOrder = battle.AlivePlayers;
            commandIdx = 0;
            commands.Clear();
            reservedTP = 0;
            bstate = BState.GeneralMenu;
            menuIndex = 0;
        }

        General CurrentCommander { get { return commandOrder != null && commandIdx < commandOrder.Count ? commandOrder[commandIdx] : null; } }

        void CommitCommand(BattleCommand cmd)
        {
            commands.Add(cmd);
            if (cmd.Type == CommandType.Tactic) reservedTP += cmd.Tactic.Cost;
            commandIdx++;
            menuIndex = 0;
            if (commandIdx >= commandOrder.Count)
            {
                var events = battle.ExecuteTurn(new List<BattleCommand>(commands));
                commands.Clear();
                reservedTP = 0;
                Play(events, AfterTurn);
                return;
            }
            bstate = BState.GeneralMenu;
        }

        void UndoCommand()
        {
            if (commandIdx == 0) { OpenTopMenu(); return; }
            commandIdx--;
            var last = commands[commands.Count - 1];
            if (last.Type == CommandType.Tactic) reservedTP -= last.Tactic.Cost;
            commands.RemoveAt(commands.Count - 1);
            bstate = BState.GeneralMenu;
            menuIndex = 0;
        }

        int MenuCount()
        {
            switch (bstate)
            {
                case BState.TopMenu: return TopItems.Length;
                case BState.GeneralMenu: return GeneralItems.Length;
                case BState.TacticMenu: return party.AvailableTactics().Count;
                case BState.TargetEnemy: return battle.AliveEnemies.Count;
                case BState.TargetAlly: return battle.AlivePlayers.Count;
            }
            return 0;
        }

        void BattleKey(InputKey key)
        {
            if (battle == null) return;
            if (bstate == BState.Playing)
            {
                if (key == InputKey.Confirm) StepPlayback();   // 按确认加速
                return;
            }
            int n = MenuCount();
            if (n > 0)
            {
                if (key == InputKey.Up || key == InputKey.Left) { menuIndex = (menuIndex + n - 1) % n; return; }
                if (key == InputKey.Down || key == InputKey.Right) { menuIndex = (menuIndex + 1) % n; return; }
            }
            if (key == InputKey.Confirm && n > 0) MenuConfirm(menuIndex);
            else if (key == InputKey.Cancel) MenuCancel();
        }

        void MenuConfirm(int index)
        {
            switch (bstate)
            {
                case BState.TopMenu:
                    if (index == 0) StartCommandInput();
                    else if (index == 1) Play(battle.AllOut(), AfterTurn);
                    else Play(battle.TryRetreat(), AfterTurn);
                    break;

                case BState.GeneralMenu:
                    pending = new BattleCommand { Actor = CurrentCommander };
                    if (index == 0) { pending.Type = CommandType.Attack; bstate = BState.TargetEnemy; menuIndex = 0; }
                    else if (index == 1) { bstate = BState.TacticMenu; menuIndex = 0; }
                    else { pending.Type = CommandType.Defend; CommitCommand(pending); }
                    break;

                case BState.TacticMenu:
                {
                    var t = party.AvailableTactics()[index];
                    if (party.TP - reservedTP < t.Cost) { log.Add("策略点不足！"); return; }
                    pending.Type = CommandType.Tactic;
                    pending.Tactic = t;
                    if (t.TargetsAll) CommitCommand(pending);
                    else { bstate = t.TargetsFoe ? BState.TargetEnemy : BState.TargetAlly; menuIndex = 0; }
                    break;
                }

                case BState.TargetEnemy:
                    pending.Target = battle.AliveEnemies[index];
                    CommitCommand(pending);
                    break;

                case BState.TargetAlly:
                    pending.Target = battle.AlivePlayers[index];
                    CommitCommand(pending);
                    break;
            }
        }

        void MenuCancel()
        {
            switch (bstate)
            {
                case BState.GeneralMenu: UndoCommand(); break;
                case BState.TacticMenu: bstate = BState.GeneralMenu; menuIndex = 1; break;
                case BState.TargetEnemy:
                case BState.TargetAlly:
                    bstate = pending != null && pending.Type == CommandType.Tactic ? BState.TacticMenu : BState.GeneralMenu;
                    menuIndex = 0;
                    break;
            }
        }

        // ---------------- 绘制 ----------------

        // 战斗界面内容区：横屏 960×540 居中；竖屏铺满宽度，指令栏挪到武将栏下面
        float bx, by, bw;

        void DrawBattle()
        {
            if (battle == null) return;
            bw = Gfx.Portrait ? Gfx.VW : 960f;
            bx = (Gfx.VW - bw) * 0.5f;
            by = (Gfx.VH - (Gfx.Portrait ? 770f : 540f)) * 0.5f;

            // 背景
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.09f, 0.08f, 0.13f));
            Gfx.Rect(new Rect(0, by + 150, Gfx.VW, 50), new Color(0.22f, 0.3f, 0.16f));
            Gfx.Rect(new Rect(0, 0, Gfx.VW, by + 150), new Color(0.13f, 0.12f, 0.2f));

            Gfx.Text(new Rect(bx + 20, by + 8, 400, 24), battle.IsBoss ? "【Boss战】黄巾营寨" : "【野战】涿郡郊外", 16, new Color(1, 1, 1, 0.6f));
            Gfx.Text(new Rect(bx + bw - 220, by + 8, 200, 24), "第 " + battle.Turn + " 回合", 16, new Color(1, 1, 1, 0.6f), TextAnchor.UpperRight);

            DrawEnemies();
            DrawLog();
            DrawPartyPanel();
            DrawCommandPanel();
        }

        void DrawEnemies()
        {
            var es = battle.Enemies;
            float h = 150, gap = 16;
            float w = Mathf.Min(160, (bw - 40 - (es.Count - 1) * gap) / es.Count);
            float total = es.Count * w + (es.Count - 1) * gap;
            float x0 = bx + (bw - total) / 2;
            var alive = battle.AliveEnemies;
            General targeted = bstate == BState.TargetEnemy && menuIndex < alive.Count ? alive[menuIndex] : null;

            for (int i = 0; i < es.Count; i++)
            {
                var e = es[i];
                int shown = shownSoldiers.ContainsKey(e) ? shownSoldiers[e] : e.Soldiers;
                bool dead = shown <= 0;
                var r = new Rect(x0 + i * (w + gap), by + 36, w, h);
                var c = Gfx.Hex(e.Def.Color);

                Gfx.Rect(r, new Color(0, 0, 0, dead ? 0.2f : 0.45f));
                if (!dead)
                {
                    float fl;
                    bool flashing = flashUntil.TryGetValue(e, out fl) && Time.time < fl;
                    var tr = new Rect(r.x + (w - 70) / 2, r.y + 8, 70, 70);
                    if (flashing) tr.x += Mathf.Sin(Time.time * 80f) * 4f;
                    Gfx.Token(tr, e.Def.Glyph, flashing ? Color.red : c);
                    Gfx.Text(new Rect(r.x, r.y + 84, w, 22), e.Name + (e.Def.IsBoss ? " ★" : ""), 17, Color.white, TextAnchor.MiddleCenter);
                    Gfx.Bar(new Rect(r.x + 14, r.y + 110, w - 28, 10), (float)shown / e.MaxSoldiers, c);
                    Gfx.Text(new Rect(r.x, r.y + 122, w, 20), "兵力 " + shown, 14, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
                }
                else
                {
                    Gfx.Text(new Rect(r.x, r.y + 50, w, 30), e.Name + "　败走", 16, new Color(1, 1, 1, 0.3f), TextAnchor.MiddleCenter);
                }

                if (e == targeted)
                {
                    Gfx.Frame(r, Color.yellow, 3);
                    Gfx.Text(new Rect(r.x, r.y - 22, w, 20), "▼", 18, Color.yellow, TextAnchor.MiddleCenter);
                }

                if (bstate == BState.TargetEnemy && !dead && Gfx.Clicked(r))
                {
                    int idx = alive.IndexOf(e);
                    if (idx >= 0) { menuIndex = idx; MenuConfirm(idx); }
                }
            }
        }

        void DrawLog()
        {
            var r = new Rect(bx + 20, by + 204, bw - 40, 92);
            Gfx.Window(r, 0.85f);
            int start = Mathf.Max(0, log.Count - 3);
            for (int i = start; i < log.Count; i++)
            {
                bool latest = i == log.Count - 1;
                Gfx.Text(new Rect(r.x + 18, r.y + 10 + (i - start) * 25, r.width - 36, 24), log[i], 18,
                    latest ? Color.white : new Color(1, 1, 1, 0.55f));
            }
            if (bstate == BState.Playing && Gfx.Clicked(r)) StepPlayback();
        }

        void DrawPartyPanel()
        {
            var r = new Rect(bx + 20, by + 304, Gfx.Portrait ? bw - 40 : 560, 222);
            Gfx.Window(r);
            var cur = (bstate == BState.GeneralMenu || bstate == BState.TacticMenu || bstate == BState.TargetEnemy || bstate == BState.TargetAlly) ? CurrentCommander : null;
            var aliveP = battle.AlivePlayers;
            General allyTarget = bstate == BState.TargetAlly && menuIndex < aliveP.Count ? aliveP[menuIndex] : null;

            for (int i = 0; i < party.Members.Count; i++)
            {
                var m = party.Members[i];
                int shown = shownSoldiers.ContainsKey(m) ? shownSoldiers[m] : m.Soldiers;
                var row = new Rect(r.x + 12, r.y + 12 + i * 40, r.width - 24, 36);
                if (m == cur) Gfx.Rect(row, new Color(1, 0.85f, 0.3f, 0.15f));
                if (m == allyTarget) Gfx.Frame(row, Color.yellow, 2);

                var c = Gfx.Hex(m.Def.Color);
                float fl;
                bool flashing = flashUntil.TryGetValue(m, out fl) && Time.time < fl;
                Gfx.Token(new Rect(row.x + 4, row.y + 2, 32, 32), m.Def.Glyph, flashing ? Color.red : c, shown <= 0);
                Gfx.Text(new Rect(row.x + 46, row.y + 7, 80, 22), m.Name, 18, shown > 0 ? Color.white : Color.gray);
                float barW = row.width - 356;   // 横屏 180，竖屏变窄
                Gfx.Bar(new Rect(row.x + 120, row.y + 12, barW, 12), (float)shown / m.MaxSoldiers, c);
                Gfx.Text(new Rect(row.x + 128 + barW, row.y + 8, 110, 22), shown + "/" + m.MaxSoldiers, 16, Color.white);

                string status = shown <= 0 ? "败走" : m.Defending ? "防御" : "";
                var cmd = commands.FirstOrDefault(cm => cm.Actor == m);
                if (bstate != BState.Playing && cmd != null) status = DescribeCommand(cmd);
                Gfx.Text(new Rect(row.x + 230 + barW, row.y + 8, 130, 22), status, 15, new Color(0.7f, 0.9f, 1f));

                if (bstate == BState.TargetAlly && shown > 0 && Gfx.Clicked(row))
                {
                    int idx = aliveP.IndexOf(m);
                    if (idx >= 0) { menuIndex = idx; MenuConfirm(idx); }
                }
            }

            Gfx.Text(new Rect(r.x + 16, r.yMax - 30, 300, 22),
                "策略点 " + (party.TP - reservedTP) + " / " + party.MaxTP + "　　等级 " + party.Level, 15, new Color(1, 1, 1, 0.7f));
        }

        static string DescribeCommand(BattleCommand c)
        {
            switch (c.Type)
            {
                case CommandType.Attack: return "→ 攻击";
                case CommandType.Defend: return "→ 防御";
                default: return "→ " + c.Tactic.Name;
            }
        }

        void DrawCommandPanel()
        {
            var r = Gfx.Portrait ? new Rect(bx + 20, by + 534, bw - 40, 222) : new Rect(bx + 594, by + 304, 346, 222);
            Gfx.Window(r);
            float x = r.x + 18, y = r.y + 14;

            string header;
            List<string> items = new List<string>();
            List<bool> enabled = new List<bool>();
            string footer = null;

            switch (bstate)
            {
                case BState.TopMenu:
                    header = "全军如何行动？";
                    items.AddRange(TopItems);
                    footer = battle.IsBoss ? "Boss 战无法撤退" : "一齐攻击：全员自动攻击";
                    break;
                case BState.GeneralMenu:
                    header = CurrentCommander.Name + " 的行动";
                    items.AddRange(GeneralItems);
                    footer = "X：返回上一位";
                    break;
                case BState.TacticMenu:
                    header = CurrentCommander.Name + "（智力 " + CurrentCommander.Zhi + "）施展";
                    foreach (var t in party.AvailableTactics())
                    {
                        items.Add(t.Name + "　" + t.Cost + "点　" + t.Desc);
                        enabled.Add(party.TP - reservedTP >= t.Cost);
                    }
                    break;
                case BState.TargetEnemy:
                    header = "选择目标（←→）";
                    foreach (var e in battle.AliveEnemies) items.Add(e.Name + "　兵力 " + e.Soldiers);
                    break;
                case BState.TargetAlly:
                    header = "选择我方武将";
                    foreach (var a in battle.AlivePlayers) items.Add(a.Name + "　" + a.Soldiers + "/" + a.MaxSoldiers);
                    break;
                default:
                    header = "……";
                    if (playQueue.Count > 0) footer = "Z：快进";
                    break;
            }

            Gfx.Text(new Rect(x, y, r.width - 36, 24), header, 18, new Color(1f, 0.85f, 0.35f));
            y += 34;

            int fontSize = bstate == BState.TacticMenu ? 15 : 19;
            float lineH = bstate == BState.TacticMenu ? 28 : 34;
            for (int i = 0; i < items.Count; i++)
            {
                bool sel = i == menuIndex;
                bool en = i >= enabled.Count || enabled[i];
                var ir = new Rect(x - 6, y + i * lineH, r.width - 24, lineH - 2);
                if (sel) Gfx.Rect(ir, new Color(1, 1, 1, 0.1f));
                var col = !en ? new Color(1, 1, 1, 0.35f) : sel ? Color.yellow : Color.white;
                Gfx.Text(new Rect(ir.x + 6, ir.y + 4, ir.width - 6, ir.height), (sel ? "▶ " : "　 ") + items[i], fontSize, col);
                if (Gfx.Clicked(ir)) { menuIndex = i; MenuConfirm(i); return; }
            }

            if (footer != null)
                Gfx.Text(new Rect(x, r.yMax - 30, r.width - 36, 22), footer, 14, new Color(1, 1, 1, 0.5f));
        }
    }
}
