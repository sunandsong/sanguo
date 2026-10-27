using System;
using System.Collections.Generic;
using System.Linq;

namespace LiuBeiRPG.Core
{
    public enum CommandType { Attack, Tactic, Defend }
    public enum BattleResult { None, Victory, Defeat, Fled }

    public class BattleCommand
    {
        public General Actor;
        public CommandType Type;
        public General Target;      // 可为空：自动选目标
        public TacticDef Tactic;
    }

    /// <summary>战斗里发生的一件事，界面按顺序播放。</summary>
    public class BattleEvent
    {
        public string Text;
        public General Target;          // 受影响的武将（可空）
        public General Actor;           // 出手的人（只有普通攻击带，画面上要冲过去砍）
        public bool IsCrit;             // 会心一击
        public string Fx;               // 策略特效：策略 Id（huoji / luoshi / yaoshu / liaobing / guwu）
        public int SoldiersAfter = -1;  // 播放到这条时该武将应显示的兵力
        public bool IsDamage;
        public bool IsHeal;
        public bool IsDefeat;

        public BattleEvent(string text) { Text = text; }
    }

    /// <summary>
    /// 回合制战斗逻辑（不依赖 Unity，可以单独测试）。
    /// 流程：玩家给每个武将下令 → ExecuteTurn → 敌方 AI 下令 → 按速度排序结算 → 判断胜负。
    /// </summary>
    public class BattleSystem
    {
        public readonly Party Party;
        public readonly List<General> Enemies;
        public readonly bool IsBoss;
        public BattleResult Result = BattleResult.None;
        public int Turn = 1;

        readonly Random rng;

        public BattleSystem(Party party, List<General> enemies, bool isBoss, Random rng)
        {
            Party = party;
            Enemies = enemies;
            IsBoss = isBoss;
            this.rng = rng;
            foreach (var m in party.Members) m.Defending = false;
        }

        public List<General> AlivePlayers { get { return Party.Members.Where(m => m.Alive).ToList(); } }
        public List<General> AliveEnemies { get { return Enemies.Where(e => e.Alive).ToList(); } }

        // ---------------- 玩家指令 ----------------

        /// <summary>一齐攻击：全员普通攻击，目标自动。</summary>
        public List<BattleEvent> AllOut()
        {
            var cmds = AlivePlayers.Select(g => new BattleCommand { Actor = g, Type = CommandType.Attack }).ToList();
            var events = new List<BattleEvent> { new BattleEvent("全军突击！") };
            events.AddRange(ExecuteTurn(cmds));
            return events;
        }

        public List<BattleEvent> TryRetreat()
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
            if (rng.NextDouble() < Formula.RetreatChance(ps, es))
            {
                Result = BattleResult.Fled;
                events.Add(new BattleEvent("我军成功撤退。"));
                return events;
            }
            events.Add(new BattleEvent("撤退失败！"));
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

            // 按速度（带随机浮动）排序
            var order = all.Where(c => c.Type != CommandType.Defend)
                           .Select(c => new { c, key = c.Actor.Su + rng.Next(0, 25) })
                           .OrderByDescending(x => x.key)
                           .Select(x => x.c)
                           .ToList();

            foreach (var cmd in order)
            {
                if (Result != BattleResult.None) break;
                if (!cmd.Actor.Alive) continue;
                if (cmd.Type == CommandType.Attack) DoAttack(cmd, events);
                else DoTactic(cmd, events);
                CheckEnd();
            }
            CheckEnd();
            Turn++;
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

        bool IsPlayer(General g) { return Party.Members.Contains(g); }

        List<General> FoesOf(General g) { return IsPlayer(g) ? AliveEnemies : AlivePlayers; }
        List<General> AlliesOf(General g) { return IsPlayer(g) ? AlivePlayers : AliveEnemies; }

        General PickRandom(List<General> list) { return list.Count == 0 ? null : list[rng.Next(list.Count)]; }

        void DoAttack(BattleCommand cmd, List<BattleEvent> events)
        {
            var foes = FoesOf(cmd.Actor);
            var target = cmd.Target != null && cmd.Target.Alive ? cmd.Target : PickRandom(foes);
            if (target == null) return;

            bool crit;
            int dmg = Formula.Attack(cmd.Actor, target, rng, out crit);
            int dealt = target.TakeDamage(dmg);
            string text = cmd.Actor.Name + " 攻击 " + target.Name + (crit ? "，会心一击！" : "，") + "敌兵减少 " + dealt;
            if (IsPlayer(target)) text = cmd.Actor.Name + " 攻击 " + target.Name + (crit ? "，会心一击！" : "，") + "我军损失 " + dealt;
            events.Add(new BattleEvent(text) { Target = target, Actor = cmd.Actor, SoldiersAfter = target.Soldiers, IsDamage = true, IsCrit = crit });
            if (!target.Alive) events.Add(new BattleEvent(target.Name + " 败走了！") { Target = target, SoldiersAfter = 0, IsDefeat = true });
        }

        void DoTactic(BattleCommand cmd, List<BattleEvent> events)
        {
            var t = cmd.Tactic;
            if (t == null) { DoAttack(cmd, events); return; }

            bool isPlayer = IsPlayer(cmd.Actor);
            if (isPlayer)
            {
                if (Party.TP < t.Cost)
                {
                    events.Add(new BattleEvent("策略点不足，" + cmd.Actor.Name + " 改为攻击。"));
                    DoAttack(cmd, events);
                    return;
                }
                Party.TP -= t.Cost;
            }

            events.Add(new BattleEvent(cmd.Actor.Name + " 施展「" + t.Name + "」！"));

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
                if (t.Kind == TacticKind.Heal)
                {
                    int healed = target.Heal(Formula.TacticHeal(cmd.Actor, t, rng));
                    events.Add(new BattleEvent(target.Name + " 兵力恢复 " + healed) { Target = target, SoldiersAfter = target.Soldiers, IsHeal = true, Fx = t.Id });
                }
                else
                {
                    if (!Formula.TacticHits(cmd.Actor, target, rng))
                    {
                        events.Add(new BattleEvent(target.Name + " 识破了计策！"));
                        continue;
                    }
                    int dealt = target.TakeDamage(Formula.TacticDamage(cmd.Actor, target, t, rng));
                    events.Add(new BattleEvent(target.Name + " 损失兵力 " + dealt) { Target = target, SoldiersAfter = target.Soldiers, IsDamage = true, Fx = t.Id });
                    if (!target.Alive) events.Add(new BattleEvent(target.Name + " 败走了！") { Target = target, SoldiersAfter = 0, IsDefeat = true });
                }
            }
        }

        void CheckEnd()
        {
            if (Result != BattleResult.None) return;
            if (AliveEnemies.Count == 0) Result = BattleResult.Victory;
            else if (AlivePlayers.Count == 0) Result = BattleResult.Defeat;
        }

        /// <summary>胜利后发放经验和金钱。</summary>
        public List<BattleEvent> GrantRewards()
        {
            var events = new List<BattleEvent>();
            int exp = Enemies.Sum(e => e.Def.ExpReward);
            int gold = Enemies.Sum(e => e.Def.GoldReward);
            Party.Gold += gold;
            events.Add(new BattleEvent("胜利！获得经验 " + exp + "，金钱 " + gold));
            foreach (var m in Party.GainExp(exp)) events.Add(new BattleEvent(m));
            return events;
        }
    }
}
