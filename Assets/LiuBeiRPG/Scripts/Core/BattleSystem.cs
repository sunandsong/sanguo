using System;
using System.Collections.Generic;
using System.Linq;

namespace LiuBeiRPG.Core
{
    public enum CommandType { Attack, Tactic, Defend, Item }
    public enum BattleResult { None, Victory, Defeat, Fled }

    public class BattleCommand
    {
        public General Actor;
        public CommandType Type;
        public General Target;      // 可为空：自动选目标
        public TacticDef Tactic;
        public string ItemId;       // 物品：用哪一件
    }

    /// <summary>战斗里发生的一件事，界面按顺序播放。</summary>
    public class BattleEvent
    {
        public string Text;
        public General Target;          // 受影响的武将（可空）
        public General Actor;           // 出手的人：普通攻击（画面上冲过去砍）和“施展「X」”那条（画面上播施法动作）带
        public bool IsCrit;             // 会心一击
        public string Fx;               // 策略特效：策略 Id（huoji / luoshi / yaoshu / liaobing / guwu）
        public string Popup;            // 目标头上冒的字：识破 / 状态名；没掉兵力的战报靠它出字
        public int SoldiersAfter = -1;  // 播放到这条时该武将应显示的兵力
        public bool IsDamage;
        public bool IsHeal;
        public bool IsDefeat;

        public BattleEvent(string text) { Text = text; }
    }

    /// <summary>
    /// 回合制战斗逻辑（不依赖 Unity，可以单独测试）。
    /// 流程：玩家给每个武将下令 → ExecuteTurn → 敌方 AI 下令 → 按速度排序结算 → 判断胜负。
    /// 规则照吞食天地：队长（上场第一人）才能下撤退令，撤退失败全军被追杀；军师在后备时上场的人都能借他放法术；状态见 Buff。
    /// </summary>
    public class BattleSystem
    {
        public readonly Party Party;
        public readonly List<General> Players;   // 这一仗上场的（开打时活着的前 5 个），后备不参战
        public readonly List<General> Enemies;
        public readonly bool IsBoss;
        public BattleResult Result = BattleResult.None;
        /// <summary>>0：撑过这么多回合就算胜（虎牢关战吕布，打满 10 回合即胜）。</summary>
        public int TurnLimit;
        public int Turn = 1;

        readonly Random rng;
        bool pursued;   // 上回合撤退失败，这回合敌军追杀（伤害 ×1.5）

        public BattleSystem(Party party, List<General> enemies, bool isBoss, Random rng)
        {
            Party = party;
            Players = party.Fighters();
            Enemies = enemies;
            IsBoss = isBoss;
            this.rng = rng;
            foreach (var m in party.Members) m.ClearBuffs();
        }

        public List<General> AlivePlayers { get { return Players.Where(m => m.Alive).ToList(); } }
        public List<General> AliveEnemies { get { return Enemies.Where(e => e.Alive).ToList(); } }

        /// <summary>队长：活着的上场第一人。撤退令只有他能下。</summary>
        public General Leader { get { return AlivePlayers.FirstOrDefault(); } }

        /// <summary>军师在场上：只有他能下法术令；不在场上（后备供法术）：谁都能下，威力还是按军师算。</summary>
        public bool StrategistFielded { get { return Players.Contains(Party.Strategist); } }
        public General Caster { get { var s = Party.Strategist; return s != null && s.Alive ? s : null; } }
        public bool CanCast(General commander) { return Caster != null && (!StrategistFielded || commander == Caster); }

        // ---------------- 玩家指令 ----------------

        /// <summary>一齐攻击：全员普通攻击，目标自动。</summary>
        public List<BattleEvent> AllOut()
        {
            var cmds = AlivePlayers.Select(g => new BattleCommand { Actor = g, Type = CommandType.Attack }).ToList();
            var events = new List<BattleEvent> { new BattleEvent("全军突击！") };
            events.AddRange(ExecuteTurn(cmds));
            return events;
        }

        public List<BattleEvent> TryRetreat(bool sure = false)
        {
            var events = new List<BattleEvent>();
            if (IsBoss)
            {
                events.Add(new BattleEvent("敌军包围了我军，无法撤退！"));
                events.AddRange(ExecuteTurn(new List<BattleCommand>()));
                return events;
            }
            double ps = AlivePlayers.Average(g => (double)g.Su);
            double es = AliveEnemies.Average(g => (double)g.Su);
            if (sure || rng.NextDouble() < Formula.RetreatChance(ps, es))
            {
                Result = BattleResult.Fled;
                events.Add(new BattleEvent("我军成功撤退。"));
                return events;
            }
            events.Add(new BattleEvent("撤退失败！敌军趁势追杀！"));
            pursued = true;
            events.AddRange(ExecuteTurn(new List<BattleCommand>()));
            return events;
        }

        /// <summary>结算一个回合。playerCmds 里每个存活武将一条指令。</summary>
        public List<BattleEvent> ExecuteTurn(List<BattleCommand> playerCmds)
        {
            var events = new List<BattleEvent>();
            foreach (var g in Party.Members) g.Defending = false;
            foreach (var g in Enemies) g.Defending = false;

            var all = new List<BattleCommand>(playerCmds);
            all.AddRange(EnemyCommands());

            // 防御最先生效
            foreach (var c in all.Where(c => c.Type == CommandType.Defend && c.Actor.Alive))
            {
                c.Actor.Defending = true;
                events.Add(new BattleEvent(c.Actor.Name + " 严阵以待。"));
            }

            // 按速度（带随机浮动）排序；奇袭的人排最前
            var order = all.Where(c => c.Type != CommandType.Defend)
                           .Select(c => new { c, key = c.Actor.Su + rng.Next(0, 25) + (c.Actor.Has(Buff.First) ? 1000 : 0) })
                           .OrderByDescending(x => x.key)
                           .Select(x => x.c)
                           .ToList();

            foreach (var cmd in order)
            {
                if (Result != BattleResult.None) break;
                var a = cmd.Actor;
                if (!a.Alive) continue;
                if (a.Has(Buff.Stun)) { events.Add(new BattleEvent(a.Name + " 中了混乱，动弹不得。")); continue; }
                if (a.Has(Buff.Confuse))
                {
                    var ally = PickRandom(AlliesOf(a).Where(x => x != a).ToList()) ?? a;
                    events.Add(new BattleEvent(a.Name + " 中了离间，调转矛头——"));
                    DoAttack(cmd, events, 1, ally);
                }
                else if (cmd.Type == CommandType.Item) DoItem(cmd, events);
                else if (cmd.Type == CommandType.Tactic && a.Has(Buff.Silence))
                {
                    events.Add(new BattleEvent(a.Name + " 被封策，施展不出法术，改为攻击。"));
                    DoAttack(cmd, events);
                }
                else if (cmd.Type == CommandType.Tactic) DoTactic(cmd, events);
                else if (a.Has(Buff.Disarm) && rng.NextDouble() < 0.6) events.Add(new BattleEvent(a.Name + " 疑有伏兵，按兵不动。"));
                else
                {
                    if (a.Has(Buff.Disarm)) { a.Buffs.Remove(Buff.Disarm); events.Add(new BattleEvent(a.Name + " 识破了疑兵之计！")); }
                    DoAttack(cmd, events);
                    if (a.Has(Buff.Twice) && Result == BattleResult.None) DoAttack(cmd, events, 0.5);   // 连击：再打半力一下
                }
                CheckEnd();
            }
            CheckEnd();
            pursued = false;
            foreach (var g in Players.Concat(Enemies))
                foreach (var b in g.TickBuffs())
                    if (g.Alive && TacticDef.IsBad(b)) events.Add(new BattleEvent(g.Name + " 恢复了神智。") { Target = g, Popup = "解" });
            Turn++;
            if (TurnLimit > 0 && Result == BattleResult.None)
            {
                if (Turn > TurnLimit) { CheckEnd(); events.Add(new BattleEvent("敌军力竭，鸣金收兵！")); }
                else events.Add(new BattleEvent("再撑 " + (TurnLimit - Turn + 1) + " 回合！"));
            }
            return events;
        }

        // ---------------- 敌方 AI ----------------

        List<BattleCommand> EnemyCommands()
        {
            var list = new List<BattleCommand>();
            foreach (var e in AliveEnemies)
            {
                var cmd = new BattleCommand { Actor = e, Type = CommandType.Attack };
                if (e.Def.Tactics.Length > 0 && rng.NextDouble() < 0.35)
                {
                    cmd.Type = CommandType.Tactic;
                    cmd.Tactic = TacticDb.Get(e.Def.Tactics[rng.Next(e.Def.Tactics.Length)]);
                }
                list.Add(cmd);
            }
            return list;
        }

        // ---------------- 结算 ----------------

        bool IsPlayer(General g) { return Players.Contains(g); }

        List<General> FoesOf(General g) { return IsPlayer(g) ? AliveEnemies : AlivePlayers; }
        List<General> AlliesOf(General g) { return IsPlayer(g) ? AlivePlayers : AliveEnemies; }

        General PickRandom(List<General> list) { return list.Count == 0 ? null : list[rng.Next(list.Count)]; }

        void DoAttack(BattleCommand cmd, List<BattleEvent> events, double mul = 1, General forced = null)
        {
            var foes = FoesOf(cmd.Actor);
            var target = forced ?? (cmd.Target != null && cmd.Target.Alive ? cmd.Target : PickRandom(foes));
            if (target == null) return;
            if (pursued && IsPlayer(target)) mul *= 1.5;
            if (IsPlayer(target)) mul *= Formula.EnemyDamageMul;

            bool crit;
            int dmg = Formula.Attack(cmd.Actor, target, rng, out crit, mul);
            int dealt = target.TakeDamage(dmg);
            string text = cmd.Actor.Name + " 攻击 " + target.Name + (crit ? "，会心一击！" : "，") + "敌兵减少 " + dealt;
            if (IsPlayer(target)) text = cmd.Actor.Name + " 攻击 " + target.Name + (crit ? "，会心一击！" : "，") + "我军损失 " + dealt;
            events.Add(new BattleEvent(text) { Target = target, Actor = cmd.Actor, SoldiersAfter = target.Soldiers, IsDamage = true, IsCrit = crit });
            if (!target.Alive) events.Add(new BattleEvent(target.Name + " 败走了！") { Target = target, SoldiersAfter = 0, IsDefeat = true });
        }

        /// <summary>用物品：药、酒、还魂丹走 Party.UseItem（救不了的情况不扣东西）；怒气丸、狼烟在这里处理。</summary>
        void DoItem(BattleCommand cmd, List<BattleEvent> events)
        {
            var item = ItemDb.Get(cmd.ItemId);
            if (Party.Count(cmd.ItemId) == 0) { events.Add(new BattleEvent(item.Name + "用完了，" + cmd.Actor.Name + " 按兵不动。")); return; }
            var who = cmd.Target ?? cmd.Actor;
            if (item.Effect == "fury")
            {
                Party.Items.Remove(cmd.ItemId);
                who.AddBuff(Buff.Fury, 99);
                events.Add(new BattleEvent(who.Name + " 服下怒气丸，杀气腾腾！") { Target = who, Popup = "怒" });
                return;
            }
            if (item.Effect == "flee")
            {
                if (IsBoss) { events.Add(new BattleEvent("敌军包围了我军，狼烟也无济于事！")); return; }
                Party.Items.Remove(cmd.ItemId);
                events.Add(new BattleEvent(cmd.Actor.Name + " 点起狼烟，全军趁乱撤退。"));
                Result = BattleResult.Fled;
                return;
            }
            int before = who.Soldiers;
            string msg = Party.UseItem(cmd.ItemId, who);
            if (cmd.Target == null || who.Soldiers == before) events.Add(new BattleEvent(msg));
            else events.Add(new BattleEvent(msg) { Target = who, SoldiersAfter = who.Soldiers, IsHeal = true });
        }

        void DoTactic(BattleCommand cmd, List<BattleEvent> events)
        {
            var t = cmd.Tactic;
            if (t == null) { DoAttack(cmd, events); return; }

            bool isPlayer = IsPlayer(cmd.Actor);
            var caster = cmd.Actor;   // 威力、命中按谁算：我方按军师（军师在后备时上场的人替他下令）
            if (isPlayer)
            {
                if (Caster == null || Party.TP < t.Cost)
                {
                    events.Add(new BattleEvent((Caster == null ? "军师已败走，" : "法力不足，") + cmd.Actor.Name + " 改为攻击。"));
                    DoAttack(cmd, events);
                    return;
                }
                Party.TP -= t.Cost;
                caster = Caster;
            }

            events.Add(new BattleEvent(cmd.Actor.Name + " 施展「" + t.Name + "」！") { Actor = cmd.Actor, Fx = t.Id });

            List<General> targets;
            if (t.TargetsFoe)
            {
                var foes = FoesOf(cmd.Actor);
                targets = t.TargetsAll ? foes : new List<General> { cmd.Target != null && cmd.Target.Alive ? cmd.Target : PickRandom(foes) };
            }
            else
            {
                var allies = AlliesOf(cmd.Actor);
                if (t.TargetsAll) targets = allies;
                else
                {
                    var tg = cmd.Target != null && cmd.Target.Alive ? cmd.Target
                           : allies.OrderBy(a => (double)a.Soldiers / a.MaxSoldiers).FirstOrDefault();
                    targets = new List<General> { tg };
                }
            }

            foreach (var target in targets)
            {
                if (target == null) continue;
                switch (t.Kind)
                {
                    case TacticKind.Heal:
                    {
                        int healed = target.Heal(Formula.TacticHeal(caster, t, rng));
                        events.Add(new BattleEvent(target.Name + " 兵力恢复 " + healed) { Target = target, SoldiersAfter = target.Soldiers, IsHeal = true, Fx = t.Id });
                        break;
                    }
                    case TacticKind.Status:
                        if (t.Cure)
                        {
                            foreach (var b in new List<Buff>(target.Buffs.Keys)) if (TacticDef.IsBad(b)) target.Buffs.Remove(b);
                            events.Add(new BattleEvent(target.Name + " 神智清明。") { Target = target, Popup = "解", Fx = t.Id });
                        }
                        else if (t.TargetsFoe && !Formula.TacticHits(caster, target, rng)) events.Add(Resisted(target, t));
                        else
                        {
                            target.AddBuff(t.Buff, t.Turns);
                            events.Add(new BattleEvent(target.Name + " " + BuffText(t.Buff)) { Target = target, Popup = t.Name, Fx = t.Id });
                        }
                        break;
                    case TacticKind.Kill:
                        if (target.Def.IsBoss) events.Add(new BattleEvent(target.Name + " 守卫森严，刺客无从下手。") { Target = target, Popup = "无效", Fx = t.Id });
                        else if (!Formula.Assassinates(caster, target, rng)) events.Add(new BattleEvent("刺客被 " + target.Name + " 发现了！") { Target = target, Popup = "失手", Fx = t.Id });
                        else
                        {
                            int dealt = target.TakeDamage(target.Soldiers);
                            events.Add(new BattleEvent(target.Name + " 被刺客斩于帐中，损失兵力 " + dealt) { Target = target, SoldiersAfter = 0, IsDamage = true, IsCrit = true, Fx = t.Id });
                            events.Add(new BattleEvent(target.Name + " 败走了！") { Target = target, SoldiersAfter = 0, IsDefeat = true });
                        }
                        break;
                    default:
                        if (t.Element == "fire" && target.Has(Buff.FireWard) || t.Element == "water" && target.Has(Buff.WaterWard))
                        {
                            events.Add(new BattleEvent(target.Name + " 有" + (t.Element == "fire" ? "水幕" : "堤坝") + "护着，毫发无损。") { Target = target, Popup = "无效", Fx = t.Id });
                            break;
                        }
                        if (!Formula.TacticHits(caster, target, rng)) { events.Add(Resisted(target, t)); break; }
                        int lost = target.TakeDamage((int)Math.Max(1, Math.Round(Formula.TacticDamage(caster, target, t, rng) * (IsPlayer(target) ? Formula.EnemyDamageMul : 1))));
                        events.Add(new BattleEvent(target.Name + " 损失兵力 " + lost) { Target = target, SoldiersAfter = target.Soldiers, IsDamage = true, Fx = t.Id });
                        if (!target.Alive) events.Add(new BattleEvent(target.Name + " 败走了！") { Target = target, SoldiersAfter = 0, IsDefeat = true });
                        break;
                }
            }
        }

        static BattleEvent Resisted(General target, TacticDef t) { return new BattleEvent(target.Name + " 识破了计策！") { Target = target, Popup = "识破", Fx = t.Id }; }

        static string BuffText(Buff b)
        {
            switch (b)
            {
                case Buff.Stun: return "陷入混乱！";
                case Buff.Confuse: return "中了离间之计！";
                case Buff.Silence: return "被封住了法术！";
                case Buff.Disarm: return "疑神疑鬼，不敢进攻。";
                case Buff.Rally: return "士气大振，攻击提升！";
                case Buff.Guard: return "结阵坚守。";
                case Buff.Haste: return "身轻如燕。";
                case Buff.Twice: return "手起刀落，连击不断！";
                case Buff.First: return "伺机抢先出手。";
                case Buff.FireWard: return "水幕护体。";
                case Buff.WaterWard: return "筑起堤坝。";
                default: return "状态变化。";
            }
        }

        void CheckEnd()
        {
            if (Result != BattleResult.None) return;
            if (AliveEnemies.Count == 0) Result = BattleResult.Victory;
            else if (AlivePlayers.Count == 0) Result = BattleResult.Defeat;
            else if (TurnLimit > 0 && Turn > TurnLimit) Result = BattleResult.Victory;
        }

        /// <summary>胜利后发放经验和金钱。</summary>
        public int RewardExp, RewardGold;   // GrantRewards 之后可读，结算面板用
        public readonly List<string> RewardItems = new List<string>();   // 掉落（已放进行囊的物品 Id，同种可重复）
        public General Recruit;             // 战后愿意归降的敌将（没有就 null），回地图后由 GameRoot 问收不收

        public List<BattleEvent> GrantRewards()
        {
            var events = new List<BattleEvent>();
            int exp = RewardExp = Enemies.Sum(e => Formula.ExpReward(e));
            int gold = RewardGold = Enemies.Sum(e => Formula.GoldReward(e));
            Party.Gold += gold;
            events.Add(new BattleEvent("胜利！获得经验 " + exp + "，金钱 " + gold));
            // 掉落：每个敌人按几率掉一件；另外每个敌人必掉一件材料杂物。行囊放不下就算了
            RewardItems.Clear();
            foreach (var e in Enemies)
                if (e.Def.Drop != null && rng.NextDouble() < e.Def.DropRate && Party.CanHold(e.Def.Drop))
                {
                    Party.Items.Add(e.Def.Drop);
                    RewardItems.Add(e.Def.Drop);
                }
            foreach (var e in Enemies)
                if (e.Def.Junk != null && Party.CanHold(e.Def.Junk))
                {
                    Party.Items.Add(e.Def.Junk);
                    RewardItems.Add(e.Def.Junk);
                }
            // 收编：可劝降的敌将里挑一个，要名马的只要我们有马就肯谈，其余三成愿意归降。队里已有同一人的不重复收
            // ponytail: 概率没查到原作数值，先 30%
            foreach (var e in Enemies)
            {
                if (!e.Def.Recruitable || Party.Has(e.Def)) continue;
                bool willing = e.Def.WantsHorse != null ? Party.Count(e.Def.WantsHorse) > 0 : rng.NextDouble() < 0.3;
                if (willing) { Recruit = e; break; }
            }
            foreach (var m in Party.GainExp(exp)) events.Add(new BattleEvent(m));
            return events;
        }
    }
}
