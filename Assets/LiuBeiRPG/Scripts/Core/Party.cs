using System;
using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    /// <summary>
    /// 玩家队伍。和吞食天地一样，等级/经验/法力是全队共享的。
    /// 队伍最多 7 人：活着的前 5 个上场，后面的是后备；再多的放到后方（Stored）。
    /// 法术只有军师能放，威力看军师的智力。
    /// </summary>
    public class Party
    {
        public const int MaxFighters = 5, MaxMembers = 7;

        public readonly List<General> Members = new List<General>();
        public readonly List<General> Stored = new List<General>();   // ponytail: 后方武将还没有换人的地方，等有第 8 个人入队时做营地
        General strategist;
        public int Level = 1;
        public int Exp;
        public int Gold;
        public int TP;       // 策略点
        public int MaxTP;
        public readonly List<string> Items = new List<string>();        // 物品 Id，一格一个
        public readonly HashSet<string> Learned = new HashSet<string>(); // 读兵书学会的策略 Id

        public Party()
        {
            RecalcMaxTP();
            TP = MaxTP;
        }

        public int ExpToNext { get { return 20 + Level * Level * 10; } }

        public void RecalcMaxTP() { MaxTP = 8 + Level * 4; }

        public void Add(General g)
        {
            // 新入队的成长型武将按当前等级补足兵力
            if (g.Def.SoldiersPerLevel > 0)
            {
                g.MaxSoldiers = g.Def.BaseSoldiers + g.Def.SoldiersPerLevel * (Level - 1);
                g.Soldiers = g.MaxSoldiers;
            }
            if (Members.Count >= MaxMembers) Stored.Add(g);
            else Members.Add(g);
        }

        /// <summary>这一仗上场的：活着的前 5 个。</summary>
        public List<General> Fighters()
        {
            var list = new List<General>();
            foreach (var m in Members) if (m.Alive && list.Count < MaxFighters) list.Add(m);
            return list;
        }

        /// <summary>军师：没任命过（或任命的人离队了）就是队里智力最高的。</summary>
        public General Strategist
        {
            get
            {
                if (strategist != null && Members.Contains(strategist)) return strategist;
                General best = null;
                foreach (var m in Members) if (best == null || m.Zhi > best.Zhi) best = m;
                return best;
            }
            set { strategist = value; }
        }

        public bool AllDefeated
        {
            get
            {
                foreach (var m in Members) if (m.Alive) return false;
                return true;
            }
        }

        public void FullRestore()
        {
            foreach (var m in Members) { m.Soldiers = m.MaxSoldiers; m.Defending = false; }
            TP = MaxTP;
        }

        /// <summary>获得经验，返回升级提示。</summary>
        public List<string> GainExp(int amount)
        {
            var msgs = new List<string>();
            Exp += amount;
            while (Exp >= ExpToNext)
            {
                Exp -= ExpToNext;
                Level++;
                RecalcMaxTP();
                TP = Math.Min(MaxTP, TP + 4);
                msgs.Add("全军升至 " + Level + " 级！");
                foreach (var m in Members)
                {
                    if (m.Def.SoldiersPerLevel <= 0) continue;
                    m.MaxSoldiers += m.Def.SoldiersPerLevel;
                    if (m.Alive) m.Soldiers += m.Def.SoldiersPerLevel;
                    msgs.Add(m.Name + " 兵力上限 +" + m.Def.SoldiersPerLevel);
                }
                foreach (var t in TacticDb.All)
                    if (t.PlayerUsable && !t.Book && t.MinLevel == Level)
                        msgs.Add("习得法术「" + t.Name + "」！");
            }
            return msgs;
        }

        public List<TacticDef> AvailableTactics()
        {
            var list = new List<TacticDef>();
            foreach (var t in TacticDb.All)
                if (t.PlayerUsable && t.MinLevel <= Level && (!t.Book || Learned.Contains(t.Id))) list.Add(t);
            return list;
        }

        /// <summary>用掉第 index 格的物品，返回提示文字。</summary>
        public string UseItem(int index)
        {
            var item = ItemDb.Get(Items[index]);
            var t = item.Teaches == null ? null : TacticDb.Get(item.Teaches);
            if (t == null) return "现在用不了。";
            if (Learned.Contains(t.Id)) return "已经学会「" + t.Name + "」了。";
            Learned.Add(t.Id);
            Items.RemoveAt(index);
            return "研读《" + item.Name + "》，全军习得法术「" + t.Name + "」！";
        }
    }
}
