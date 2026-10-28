using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LiuBeiRPG.Core;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 战斗画面：逐个武将下令（攻击 / 策略 / 防御 / 物品 / 逃跑 / 自动），然后按顺序播放战报。
    /// 逻辑全部在 Core/BattleSystem.cs，这里只负责菜单和表现。
    /// </summary>
    public partial class GameRoot
    {
        enum BState { Playing, GeneralMenu, TacticMenu, TargetEnemy, TargetAlly, ConfirmTarget, Result }

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

        // 战场上的骨骼角色：敌我每人一份（不和地图上的共用，朝向、受击互不影响）。包里没有的（程远志等）照旧画字块
        readonly Dictionary<General, PuppetActor> battlePuppets = new Dictionary<General, PuppetActor>();

        // 敌人的专属兵器，一人一把不重样（引擎 weapons/ 下的名字）。刺还是砍由 weapon.json 的 attack 决定
        static readonly Dictionary<string, string> EnemyWeapons = new Dictionary<string, string>
        {
            { "hjb", "cha" },             // 黄巾兵：叉（农具）
            { "hjls", "dafu" },           // 黄巾力士：大斧
            { "hjss", "fuchen" },         // 黄巾术士：拂尘
            { "hjtm", "podao" },          // 黄巾头目：朴刀
            { "chengyuanzhi", "zhanchui" },   // 程远志：战锤
            { "dengmao", "huaji" },       // 邓茂：画戟
        };

        PuppetActor BattlePuppet(General g, bool enemy)
        {
            PuppetActor a;
            if (battlePuppets.TryGetValue(g, out a)) return a;
            try
            {
                a = PuppetActor.Load(g.Def.Id == "hjb" ? "huangjin" : g.Def.Id);   // 黄巾兵的骨骼角色叫 huangjin
                a.Facing = enemy ? "side" : "left";
                string w;
                if (!enemy && g.WeaponBonus > 0 && Weapons.TryGetValue(g.Def.Id, out w)) a.Equip(w);   // 和地图上一样，受赠之后才拿兵器
                if (enemy && EnemyWeapons.TryGetValue(g.Def.Id, out w)) a.Equip(w);
                if (!g.Alive) { a.Play("down"); a.Time = 10; }   // 上一仗败走的一开场就躺着
            }
            catch (Exception) { a = null; }
            return battlePuppets[g] = a;
        }

        public void StartBattle(List<General> enemies, bool isBoss, Action onVictory = null)
        {
            battle = new BattleSystem(party, enemies, isBoss, rng);
            onBattleVictory = onVictory;
            mode = Mode.Battle;
            battleEnter = Time.time;
            battleExit = -1f;
            autoBattle = false;
            log.Clear();
            shownSoldiers.Clear();
            flashUntil.Clear();
            battlePuppets.Clear();
            playQueue.Clear();
            strike = null;
            ResetBattleFx();
            rewardsGiven = false;
            foreach (var g in battle.Players.Concat(enemies)) shownSoldiers[g] = g.Soldiers;

            var intro = new List<BattleEvent>
            {
                new BattleEvent(isBoss ? "黄巾军主力杀到！" : "遭遇黄巾军！"),
            };
            Play(intro, StartCommandInput);
        }

        // ---------------- 播放 ----------------

        void Play(List<BattleEvent> events, Action then)
        {
            bstate = BState.Playing;
            foreach (var e in events) playQueue.Enqueue(e);
            afterPlay = then;
            nextEventTime = 0;
        }

        // 入场：敌我从两边走进来；出场：我方往右、剩下的敌人往左走出画面，走完才回地图
        const float EnterTime = 0.6f, ExitTime = 0.6f;
        float battleEnter, battleExit = -1f;

        /// <summary>入场 / 出场进度 0..1 → 偏离站位的比例（1 = 完全在画面外）。</summary>
        float OffstageK()
        {
            if (battleExit >= 0f) { float t = Mathf.Clamp01((Time.time - battleExit) / ExitTime); return t * t; }   // 先慢后快
            float u = 1f - Mathf.Clamp01((Time.time - battleEnter) / EnterTime);
            return u * u;                                                                                           // 先快后慢
        }

        void UpdateBattle()
        {
            if (battleExit >= 0f && Time.time - battleExit >= ExitTime) { FinishBattle(); return; }
            bool walking = OffstageK() > 0f;
            bool frozen = Frozen;
            if (strike != null)
            {
                if (frozen) strikeStart += Time.deltaTime;   // 顿帧：这一刀的时间轴停住
                UpdateStrike();
            }
            UpdateSpell();
            foreach (var kv in battlePuppets)
            {
                var a = kv.Value;
                if (a == null) continue;
                bool acting = strike != null && kv.Key == strike.Actor || spell != null && kv.Key == spell.Actor;
                if (frozen && (acting || kv.Key == slashTarget)) continue;   // 顿帧：砍人的和挨砍的都定住
                a.Tick(Time.deltaTime);
                if (acting) continue;   // 正在砍 / 施法的人动作由 UpdateStrike / Spell 管
                int shown;
                if (shownSoldiers.TryGetValue(kv.Key, out shown) && shown <= 0) continue;   // 倒地的不回待机
                if (walking) a.Play("walk");
                else if (a.Finished || a.Clip.name == "walk") a.Play("idle");   // 受击播完、走完回待机
            }
            if (bstate != BState.Playing || Time.time < nextEventTime) return;
            StepPlayback();
        }

        void StepPlayback()
        {
            if (strike != null) { FinishStrike(); return; }   // 快进：这一刀直接砍完
            if (spell != null) { FinishSpell(); flips.Clear(); return; }   // 快进：法术直接结算
            if (playQueue.Count > 0)
            {
                var e = playQueue.Dequeue();
                if (TryStartSpell(e)) return;   // 法术：施法 → 放出 → 命中时结算（GameRoot.Spell.cs）
                if (e.Actor != null && e.IsDamage && slots.ContainsKey(e.Actor) && slots.ContainsKey(e.Target))
                {
                    // 普通攻击：冲过去砍一刀，刀落下时才结算（UpdateStrike）
                    strike = e;
                    strikeStart = Time.time;
                    strikeLanded = strikeSwung = strikeDashed = false;
                    nextEventTime = float.MaxValue;
                    return;
                }
                ApplyEvent(e);
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

        /// <summary>一条战报生效：出字、改兵力条、伤害数字、策略特效；挨打的闪白 + 受击 + 击退，被砍的再喷血、顿帧、震屏；败走的倒地。</summary>
        void ApplyEvent(BattleEvent e)
        {
            log.Add(e.Text);
            if (log.Count > 50) log.RemoveAt(0);
            Vector2 mp;
            if (e.Target != null && e.SoldiersAfter < 0 && slots.TryGetValue(e.Target, out mp))   // 法术被识破
                popups.Add(new Popup { text = "识破", at = mp + new Vector2(0, -HeightOf(e.Target) - 20f), born = Time.time });
            if (e.Target == null || e.SoldiersAfter < 0) return;
            int before;
            if (!shownSoldiers.TryGetValue(e.Target, out before)) before = e.SoldiersAfter;
            shownSoldiers[e.Target] = e.SoldiersAfter;
            Vector2 at;
            bool hasAt = slots.TryGetValue(e.Target, out at);
            if (hasAt && (e.IsDamage || e.IsHeal) && before != e.SoldiersAfter)
                popups.Add(new Popup { text = Mathf.Abs(before - e.SoldiersAfter).ToString(), at = at + new Vector2(0, -HeightOf(e.Target) - 20f), born = Time.time, heal = e.IsHeal, crit = e.IsCrit });
            if (hasAt && e.Fx != null) TacticFx(e.Fx, at, HeightOf(e.Target));
            if (e.IsDefeat) { FallDown(e.Target); return; }
            if (!e.IsDamage) return;
            flashUntil[e.Target] = Time.time + 0.3f;
            PuppetActor a;
            if (battlePuppets.TryGetValue(e.Target, out a) && a != null) { a.Hit(); a.Play("hit", true); }
            float dir = battle.Enemies.Contains(e.Target) ? -1f : 1f;   // 默认往自己身后退
            Vector2 ap;
            if (e.Actor != null && hasAt && slots.TryGetValue(e.Actor, out ap))
            {
                dir = Mathf.Sign(at.x - ap.x);
                SprayBlood(at + new Vector2(0, -HeightOf(e.Target) * 0.5f), dir, e.IsCrit);
                Impact(e.Target, e.IsCrit);
            }
            Knock(e.Target, dir);
        }

        // ---------------- 砍一刀：蓄力往后一缩 → 冲过去 → 挥刀（落刀时结算）→ 退回 ----------------

        const float WindTime = 0.12f, DashTime = 0.22f, SwingTime = 0.55f, BackTime = 0.22f, HitAt = 0.5f;   // HitAt：挥刀开始后多久落刀（attack / stab 的 hit 帧约 0.55）
        BattleEvent strike;
        float strikeStart;
        bool strikeDashed, strikeSwung, strikeLanded;
        readonly Dictionary<General, Vector2> slots = new Dictionary<General, Vector2>();   // 每人的站位（脚底），DrawField 每帧更新

        void UpdateStrike()
        {
            float t = Time.time - strikeStart;
            PuppetActor a;
            battlePuppets.TryGetValue(strike.Actor, out a);
            var slot = slots[strike.Actor];
            float dir = Mathf.Sign(slots[strike.Target].x - slot.x);
            if (t >= WindTime && !strikeDashed) { strikeDashed = true; Dust(slot, dir); }   // 起步扬尘
            if (t >= WindTime && t < WindTime + DashTime) { if (a != null) a.Play("walk"); }
            else if (t >= WindTime + DashTime && !strikeSwung)
            {
                strikeSwung = true;
                if (a != null) a.Play("attack", true);   // 拿枪的自动换成 stab
                Dust(StrikePosAt(strike.Actor, slot, t), -dir);   // 急停扬尘
            }
            if (!strikeLanded && t >= WindTime + DashTime + HitAt) { strikeLanded = true; ApplyEvent(strike); }
            if (t >= WindTime + DashTime + SwingTime + BackTime) FinishStrike();
        }

        void FinishStrike()
        {
            if (!strikeLanded) ApplyEvent(strike);
            PuppetActor a;
            if (battlePuppets.TryGetValue(strike.Actor, out a) && a != null) a.Play("idle");
            strike = null;
            nextEventTime = Time.time + 0.35f;
        }

        /// <summary>出手的人在这一刀第 t 秒该站哪：站位 → 往后一缩 → 目标身前（隔 70，站在自己这一侧）→ 站位。</summary>
        Vector2 StrikePosAt(General g, Vector2 slot, float t)
        {
            if (strike == null || g != strike.Actor || !slots.ContainsKey(strike.Target) || t <= 0f) return slot;
            var tp = slots[strike.Target];
            float dir = Mathf.Sign(tp.x - slot.x);
            var back = slot - new Vector2(dir * 12f, 0f);
            var front = tp - new Vector2(dir * 70f, 0f);
            if (t < WindTime) return Vector2.Lerp(slot, back, t / WindTime);
            t -= WindTime;
            if (t < DashTime) { float u = t / DashTime; return Vector2.Lerp(back, front, 1f - (1f - u) * (1f - u)); }
            t -= DashTime;
            if (t < SwingTime) return front;
            return Vector2.Lerp(front, slot, Mathf.Clamp01((t - SwingTime) / BackTime));
        }

        /// <summary>一段战报播完后：判断胜负，或者进入下一回合。</summary>
        void AfterTurn()
        {
            foreach (var g in shownSoldiers.Keys.ToList()) shownSoldiers[g] = g.Soldiers;
            switch (battle.Result)
            {
                case BattleResult.None:
                    StartCommandInput();
                    break;
                case BattleResult.Victory:
                    if (!rewardsGiven)
                    {
                        rewardsGiven = true;
                        ShowResult();   // 胜利结算面板，点一下才出场（GameRoot.BattleResult.cs）
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

        /// <summary>战斗结束：先播出场，ExitTime 之后 FinishBattle 才真正回地图。</summary>
        void EndBattle()
        {
            if (battleExit >= 0f) return;
            battleExit = Time.time;
            bstate = BState.Playing;
            foreach (var kv in battlePuppets)
                if (kv.Value != null) kv.Value.Facing = battle.Players.Contains(kv.Key) ? "side" : "left";   // 转身往外走
        }

        void FinishBattle()
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

        // 每个武将轮流下令。物品还没有，按钮先灰着；MenuCancel 从策略退回时 menuIndex = 1 对应“策略”
        static readonly string[] GeneralItems = { "攻击", "法术", "防御", "物品", "逃跑", "自动" };

        // 自动战斗：开了以后每回合全员自动攻击兵力最少的敌人，直到按取消键或点右上角的牌子关掉；每场战斗开始时关
        bool autoBattle;

        void AutoCommands()
        {
            while (bstate != BState.Playing && CurrentCommander != null)
                CommitCommand(new BattleCommand { Actor = CurrentCommander, Type = CommandType.Attack, Target = battle.AliveEnemies.OrderBy(e => e.Soldiers).FirstOrDefault() });
        }
        static readonly string[] ConfirmItems = { "确定", "取消" };   // 选完目标再确认一次

        void StartCommandInput()
        {
            commandOrder = battle.AlivePlayers;
            commandIdx = 0;
            commands.Clear();
            reservedTP = 0;
            bstate = BState.GeneralMenu;
            menuIndex = 0;
            if (autoBattle) AutoCommands();
        }

        /// <summary>法术只有军师能放（吞食天地）。</summary>
        bool CanCast { get { return CurrentCommander != null && CurrentCommander == party.Strategist && party.AvailableTactics().Count > 0; } }

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
            if (commandIdx == 0) return;   // 第一个人没有上一位可退
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
                case BState.GeneralMenu: return GeneralItems.Length;
                case BState.TacticMenu: return party.AvailableTactics().Count;
                case BState.TargetEnemy: return battle.AliveEnemies.Count;
                case BState.TargetAlly: return battle.AlivePlayers.Count;
                case BState.ConfirmTarget: return ConfirmItems.Length;
            }
            return 0;
        }

        void BattleKey(InputKey key)
        {
            if (battle == null) return;
            if (bstate == BState.Result)
            {
                if (key == InputKey.Confirm || key == InputKey.Cancel) CloseResult();
                return;
            }
            if (bstate == BState.Playing)
            {
                if (key == InputKey.Confirm) StepPlayback();   // 按确认加速
                else if (key == InputKey.Cancel) autoBattle = false;   // 按取消关掉自动
                return;
            }
            int n = MenuCount();
            if (n > 0)
            {
                // 指令按钮是每排 3 个的格子：上下键跨一整排（越界就停在首 / 末个），左右键挨个走；选目标时上下左右都挨个走
                bool grid = bstate == BState.GeneralMenu;   // 法术是一排卡片，上下左右都挨个走
                if (grid && key == InputKey.Up) { menuIndex = Mathf.Max(0, menuIndex - 3); return; }
                if (grid && key == InputKey.Down) { menuIndex = Mathf.Min(n - 1, menuIndex + 3); return; }
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
                case BState.GeneralMenu:
                    pending = new BattleCommand { Actor = CurrentCommander };
                    if (index == 0) { pending.Type = CommandType.Attack; bstate = BState.TargetEnemy; menuIndex = 0; }
                    else if (index == 1 && CanCast) { bstate = BState.TacticMenu; menuIndex = 0; }
                    else if (index == 2) { pending.Type = CommandType.Defend; CommitCommand(pending); }
                    else if (index == 4) { commands.Clear(); reservedTP = 0; Play(battle.TryRetreat(), AfterTurn); }   // 任何一人选逃跑，全军撤退
                    else if (index == 5) { autoBattle = true; AutoCommands(); }   // 这一位和后面的人都自动攻击
                    // index 3 物品：还没有物品系统，不响应
                    break;

                case BState.TacticMenu:
                {
                    var t = party.AvailableTactics()[index];
                    if (party.TP - reservedTP < t.Cost) { log.Add("法力不足！"); return; }
                    pending.Type = CommandType.Tactic;
                    pending.Tactic = t;
                    if (t.TargetsAll) CommitCommand(pending);
                    else { bstate = t.TargetsFoe ? BState.TargetEnemy : BState.TargetAlly; menuIndex = 0; }
                    break;
                }

                case BState.TargetEnemy:
                case BState.TargetAlly:
                    pending.Target = (bstate == BState.TargetEnemy ? battle.AliveEnemies : battle.AlivePlayers)[index];
                    bstate = BState.ConfirmTarget;
                    menuIndex = 0;
                    break;

                case BState.ConfirmTarget:
                    if (index == 0) CommitCommand(pending);
                    else MenuCancel();
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
                case BState.ConfirmTarget:   // 回到选目标，光标停在刚才选的人身上
                {
                    bool foe = pending.Type != CommandType.Tactic || pending.Tactic.TargetsFoe;
                    bstate = foe ? BState.TargetEnemy : BState.TargetAlly;
                    menuIndex = Mathf.Max(0, (foe ? battle.AliveEnemies : battle.AlivePlayers).IndexOf(pending.Target));
                    break;
                }
            }
        }

        // ---------------- 绘制 ----------------

        // 战斗界面内容区：横屏 960×540 居中；竖屏铺满宽度、高 770。整屏都是战场，底下一条指令栏（播战报时显示战报）
        float bx, by, bw, bh;
        const float BarH = 40f;     // 底部指令按钮高（最多两排）
        const float MenuH = 2 * BarH + 8 + 12 + 26;   // 两排按钮 + 排间距 + 底边距 + 上方小字，战场人物要让开这么高

        void DrawBattle()
        {
            if (battle == null) return;
            bw = Gfx.Portrait ? Gfx.VW : 960f;
            bh = Gfx.Portrait ? 770f : 540f;
            bx = (Gfx.VW - bw) * 0.5f;
            by = (Gfx.VH - bh) * 0.5f;

            // 地图压暗当背景，只透一点；入场 / 出场时跟着淡入淡出
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.06f, 0.05f, 0.08f, BattleDim * (1f - OffstageK())));

            DrawField();
            DrawCommandPanel();
            if (autoBattle && battleExit < 0f)   // 右上角“自动中”的牌子，点一下关掉
            {
                var ar = new Rect(bx + bw - 172, by + 12, 160, 34);
                Gfx.RoundRect(ar, Accent);
                Gfx.Text(ar, "自动中 · 点击取消", 16, new Color(0.12f, 0.09f, 0.04f), TextAnchor.MiddleCenter, false, false);
                if (Gfx.Clicked(ar)) autoBattle = false;
            }
            DrawScreenFlash();
            DrawBattleTitle();
            if (bstate == BState.Result) DrawResult();
        }

        const float DiscAlpha = 0.7f;   // 圆盘不透明度，淡一点衬人物
        const float BattleDim = 0.6f;   // 背景压暗程度：0 全透明（地图原样），1 全黑

        const float ColGap = 280f, RowGap = 115f;   // 敌我两列的间距、同一列里每人的上下间距
        const float UnitH = 100f;   // 战场上一个人约 100 高（原图约 950 像素）
        const float BossScale = 1.35f;   // Boss 画大这么多

        float HeightOf(General g) { return g != null && g.Def.IsBoss ? UnitH * BossScale : UnitH; }
        const float OutlinePx = 1.8f;  // 人物描边粗细

        /// <summary>战场：敌人在左边竖排一列（脸朝右），我方在右边竖排一列（脸朝左），越靠下越近、越后画。</summary>
        void DrawField()
        {
            float top = by + 40, bottom = by + bh;

            var aliveE = battle.AliveEnemies;
            var aliveP = battle.AlivePlayers;
            General target = bstate == BState.TargetEnemy && menuIndex < aliveE.Count ? aliveE[menuIndex]
                           : bstate == BState.TargetAlly && menuIndex < aliveP.Count ? aliveP[menuIndex]
                           : bstate == BState.ConfirmTarget ? pending.Target : null;
            var cur = bstate != BState.Playing ? CurrentCommander : null;

            // 站位：敌我各竖排一列、面对面，列间距 ColGap，同列每人相隔 RowGap，整体在指令栏以上居中
            var units = new List<KeyValuePair<General, Vector2>>();
            // 人（头顶兵力条到脚下名字）在菜单以上的区域里居中；一列人多放不下时压缩上下间距
            float head = UnitH + 12f, name = 22f, avail = bh - MenuH - 4f;
            float cx = bx + bw * 0.5f, cy = by + 4f + avail * 0.5f + (head - name) * 0.5f;   // 脚底中线
            // 挤得上下会叠在一起时，隔一个往外让 70 交错站（outward = -1 往左，+1 往右）
            System.Action<List<General>, float, float> column = (gs, x, outward) =>
            {
                float gap = gs.Count > 1 ? Mathf.Min(RowGap, (avail - head - name) / (gs.Count - 1)) : 0f;
                bool zigzag = gap < head + name;
                for (int i = 0; i < gs.Count; i++)
                    units.Add(new KeyValuePair<General, Vector2>(gs[i],
                        new Vector2(x + (zigzag && i % 2 == 1 ? outward * 70f : 0f), cy + (i - (gs.Count - 1) * 0.5f) * gap)));
            };
            var es = battle.Enemies;
            column(es, cx - ColGap * 0.5f, -1f);
            column(battle.Players, cx + ColGap * 0.5f, 1f);
            slots.Clear();
            var shake = Shake();
            float st = strike != null ? Time.time - strikeStart : 0f;
            foreach (var u in units) slots[u.Key] = u.Value;   // 先把站位登记全，StrikePosAt 要查目标的站位（敌人排在前面，边登记边查会查不到我方目标）
            for (int i = 0; i < units.Count; i++)
                units[i] = new KeyValuePair<General, Vector2>(units[i].Key, StrikePosAt(units[i].Key, units[i].Value, st) + shake);
            units.Sort((a, b) => a.Value.y.CompareTo(b.Value.y));

            // 脚下的圆盘（Resources/Map/Art/battle_disc.png，没放图就不画）：宽盖住两列，跟着入场 / 出场淡入淡出
            var disc = Gfx.Tex("Art/battle_disc");
            if (disc != null && Event.current.type == EventType.Repaint)
            {
                float dw = ColGap + 500f, dh = dw * disc.height / disc.width;
                var old = GUI.color;
                GUI.color = new Color(1, 1, 1, DiscAlpha * (1f - OffstageK()));
                Gfx.Image(new Rect(cx - dw * 0.5f, cy - dh * 0.5f, dw, dh), disc);
                GUI.color = old;
            }

            foreach (var u in units)
            {
                var g = u.Key;
                bool enemy = es.Contains(g);
                int shown = shownSoldiers.ContainsKey(g) ? shownSoldiers[g] : g.Soldiers;
                bool dead = shown <= 0;
                float alpha = enemy && dead ? DownAlpha(g) : 1f;   // 败走的敌人倒下后淡出
                if (alpha <= 0f) continue;

                var foot = u.Value;
                float h = HeightOf(g);
                foot.x += KnockX(g);
                float off = OffstageK();   // 入场 / 出场：沿水平方向推到画面外
                if (off > 0f) foot.x += enemy ? -(foot.x + 80f) * off : (Gfx.VW - foot.x + 80f) * off;
                float fl;
                if (flashUntil.TryGetValue(g, out fl) && Time.time < fl) foot.x += Mathf.Sin(Time.time * 80f) * 4f;
                var c = Gfx.Hex(g.Def.Color);

                if (g == cur) DrawRing(foot, false, h);
                if (g == target) DrawRing(foot, true, h);
                // 脚下软阴影，人像踩在圆盘上
                if (Event.current.type == EventType.Repaint)
                {
                    var oldC = GUI.color;
                    GUI.color = new Color(0, 0, 0, 0.5f * alpha);
                    Gfx.Image(new Rect(foot.x - h * 0.4f, foot.y - h * 0.09f, h * 0.8f, h * 0.2f), PetalTex());
                    GUI.color = oldC;
                }
                var p = BattlePuppet(g, enemy);
                if (p != null)
                {
                    float flash = p.Flash;
                    p.Flash = 0f;
                    // 冲刺残影：身后三个越来越淡的蓝白影子
                    if (strike != null && g == strike.Actor && st >= WindTime && st < WindTime + DashTime + 0.08f)
                        for (int k = 3; k >= 1; k--)
                        {
                            p.Tint = new Color(0.7f, 0.85f, 1f, 0.42f - 0.11f * k);
                            p.DrawGUI(StrikePosAt(g, slots[g], st - 0.035f * k) + shake, h / 950f);
                        }
                    // 深色描边：剪影朝 8 个方向各偏一点先画（和地图上 DrawPuppet 一样），人从背景里跳出来
                    p.Tint = new Color(0.1f, 0.07f, 0.05f, alpha);
                    for (int k = 0; k < 8; k++)
                    {
                        float ang = k * Mathf.PI / 4f;
                        p.DrawGUI(foot + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * OutlinePx, h / 950f);
                    }
                    p.Flash = g == target ? Mathf.Max(flash, 0.22f + 0.12f * Mathf.Sin(Time.time * 9f)) : flash;   // 选中的目标一闪一闪发白
                    p.Tint = dead ? new Color(0.45f, 0.45f, 0.45f, alpha) : Color.white;
                    p.DrawGUI(foot, h / 950f);
                }
                else Gfx.Token(new Rect(foot.x - 28, foot.y - 60, 56, 56), g.Def.Glyph, c, dead);

                if (enemy && dead) continue;   // 倒下的敌人不再画兵力条、名字
                // 头顶兵力条、脚下名字
                Gfx.Bar(new Rect(foot.x - 30, foot.y - h - 12, 60, 6), (float)shown / g.MaxSoldiers, c);
                var nameCol = g == cur ? Color.yellow : enemy ? new Color(0.55f, 1f, 0.55f) : Color.white;
                Gfx.Text(new Rect(foot.x - 60, foot.y + 2, 120, 20), g.Name + (g.Def.IsBoss ? " ★" : ""), 15, nameCol, TextAnchor.UpperCenter);

                if (g == target)   // 头顶上下跳的大箭头
                    Gfx.Text(new Rect(foot.x - 30, foot.y - h - 52 + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * -8f, 60, 34), "▼", 30, new Color(1f, 0.35f, 0.25f), TextAnchor.MiddleCenter);

                var hit = new Rect(foot.x - 36, foot.y - h, 72, h + 20);
                if (!dead && Gfx.Clicked(hit))
                {
                    var list = enemy ? aliveE : aliveP;
                    if (bstate == (enemy ? BState.TargetEnemy : BState.TargetAlly) && list.IndexOf(g) >= 0) { menuIndex = list.IndexOf(g); MenuConfirm(menuIndex); }
                }
            }
            DrawSlash();
            DrawSparks();
            DrawFlips();
            DrawPopups();
            Gfx.Paper = false;
            if (bstate == BState.Playing && Gfx.Clicked(new Rect(bx, top, bw, bottom - top))) StepPlayback();   // 点战场快进
        }

        /// <summary>底部一排指令按钮，只在要玩家选的时候出现；选目标直接在战场上点人或按方向键。播战报时换成战报。</summary>
        void DrawCommandPanel()
        {
            var items = new List<string>();
            var enabled = new List<bool>();
            switch (bstate)
            {
                case BState.GeneralMenu: items.AddRange(GeneralItems); enabled.AddRange(new[] { true, CanCast, true, false, true, true }); break;
                case BState.ConfirmTarget: items.AddRange(ConfirmItems); break;
                case BState.TacticMenu: DrawTacticCards(); return;
                case BState.Result: return;
                case BState.TargetEnemy:
                case BState.TargetAlly:
                {
                    // 选目标：在战场上点人；底下一行提示 + 返回（手机上没有取消键）
                    var back = new Rect(bx + bw * 0.5f - 60, by + bh - 12 - 52, 120, 52);
                    string what = pending.Type == CommandType.Tactic ? pending.Tactic.Name : "攻击";
                    Gfx.Text(new Rect(bx, back.y - 30, bw, 24), CurrentCommander.Name + "：" + what + "　点选" + (bstate == BState.TargetEnemy ? "敌人" : "队友"), 16, Accent, TextAnchor.MiddleCenter);
                    Gfx.Modern(back, 0.94f);
                    Gfx.Text(back, "返回", 20, Color.white, TextAnchor.MiddleCenter);
                    if (Gfx.Clicked(back)) MenuCancel();
                    return;
                }
                default:
                    // 播战报时这条栏显示最新一句战报
                    if (log.Count == 0 || battleExit >= 0f) return;
                    var lr = new Rect(bx + 20, by + bh - 48 - 12, bw - 40, 48);
                    Gfx.Modern(lr, 0.92f);
                    Gfx.Text(lr, log[log.Count - 1], 17, Color.white, TextAnchor.MiddleCenter);
                    return;
            }

            // 现代风：独立的圆角按钮，和对话框同一套。选中的填金色、字变深、往上浮一点
            // 每排最多 PerRow 个，从上往下排，最后一排贴底
            const int PerRow = 3;
            const float itemW = 104f, gap = 10f;
            int rows = (items.Count + PerRow - 1) / PerRow;
            float yBottom = by + bh - BarH - 12;
            System.Func<int, Rect> cell = i =>
            {
                int row = i / PerRow, n = Mathf.Min(PerRow, items.Count - row * PerRow);
                float x = bx + (bw - n * itemW - (n - 1) * gap) * 0.5f + (i % PerRow) * (itemW + gap);
                return new Rect(x, yBottom - (rows - 1 - row) * (BarH + 8), itemW, BarH);
            };
            var first = cell(0);
            // 按钮上方小字：轮到谁下令（选策略时带上剩余策略点）
            string caption = bstate == BState.ConfirmTarget
                ? CurrentCommander.Name + "：" + (pending.Type == CommandType.Tactic ? pending.Tactic.Name : "攻击") + " " + pending.Target.Name + "？"
                : CurrentCommander.Name + (bstate == BState.TacticMenu ? "　法力 " + (party.TP - reservedTP) : "");
            Gfx.Text(new Rect(first.x + 4, first.y - 26, 400, 22), caption, 15, Accent);
            for (int i = 0; i < items.Count; i++)
            {
                bool sel = i == menuIndex;
                bool en = i >= enabled.Count || enabled[i];
                var ir = cell(i);
                if (sel) ir.y -= 3f;
                if (sel) Gfx.RoundRect(ir, en ? Accent : new Color(0.5f, 0.45f, 0.35f));
                else Gfx.Modern(ir, 0.92f);
                var col = sel ? new Color(0.12f, 0.09f, 0.04f) : en ? Color.white : new Color(1, 1, 1, 0.35f);
                Gfx.Text(ir, items[i], 18, col, TextAnchor.MiddleCenter, false, !sel);
                if (Gfx.Clicked(ir)) { menuIndex = i; MenuConfirm(i); return; }
            }
        }

        /// <summary>选法术：底部一排大图标卡片（右上角法力消耗），选中的抬起发金光；上方一行写名字和效果。法力不够的压暗。</summary>
        void DrawTacticCards()
        {
            var list = party.AvailableTactics();
            int tp = party.TP - reservedTP;
            const float cw = 100f, ch = 118f, gap = 12f, backW = 72f;
            float total = list.Count * cw + (list.Count - 1) * gap + gap + backW;
            float x0 = bx + (bw - total) * 0.5f, y0 = by + bh - 12 - ch;
            menuIndex = Mathf.Clamp(menuIndex, 0, Mathf.Max(0, list.Count - 1));

            if (list.Count > 0)
            {
                var cur = list[menuIndex];
                Gfx.Text(new Rect(x0, y0 - 34, total, 24), CurrentCommander.Name + "：" + cur.Name + " · " + cur.Desc, 16, Accent);
                Gfx.Text(new Rect(x0, y0 - 34, total, 24), "法力 " + tp, 16, Color.white, TextAnchor.UpperRight);
            }
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                bool sel = i == menuIndex, en = tp >= t.Cost;
                var r = new Rect(x0 + i * (cw + gap), y0 - (sel ? 10f : 0f), cw, ch);
                if (sel) Gfx.RoundRect(new Rect(r.x - 5, r.y - 5, r.width + 10, r.height + 10), new Color(Accent.r, Accent.g, Accent.b, 0.25f + 0.1f * Mathf.Sin(Time.time * 5f)));
                Gfx.Modern(r, 0.94f);
                if (sel) Gfx.Frame(r, Accent, 2);
                var icon = Gfx.Tex("Art/tactic_" + t.Id);
                if (icon != null)
                {
                    var old = GUI.color;
                    GUI.color = en ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.9f);
                    Gfx.Image(new Rect(r.x + 8, r.y + 10, cw - 16, cw - 16), icon);
                    GUI.color = old;
                }
                // 法力消耗小圆牌
                var badge = new Rect(r.center.x - 22, r.yMax - 26, 44, 20);
                Gfx.RoundRect(badge, new Color(0, 0, 0, 0.55f));
                Gfx.Text(badge, t.Cost.ToString(), 15, en ? new Color(0.55f, 0.8f, 1f) : new Color(1, 0.4f, 0.35f), TextAnchor.MiddleCenter);
                if (Gfx.Clicked(r)) { menuIndex = i; MenuConfirm(i); return; }
            }
            // 返回（手机上没有取消键）
            var back = new Rect(x0 + list.Count * (cw + gap), y0 + ch - 48, backW, 48);
            Gfx.Modern(back, 0.92f);
            Gfx.Text(back, "返回", 18, Color.white, TextAnchor.MiddleCenter);
            if (Gfx.Clicked(back)) MenuCancel();
        }
    }
}
