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

        // ---------------- 物品：Items 里一件一条，同种叠在一格显示 ----------------

        public const int BagSlots = 24, StackMax = 99;

        /// <summary>行囊格子：每种一格，按拿到的先后排。</summary>
        public List<string> Slots()
        {
            var list = new List<string>();
            foreach (var id in Items) if (!list.Contains(id)) list.Add(id);
            return list;
        }

        public int Count(string id)
        {
            int n = 0;
            foreach (var i in Items) if (i == id) n++;
            return n;
        }

        /// <summary>对 who 用一件 id（兵书全军学，药给 who，装备给 who 换上），返回提示文字；换装备成功返回 null。</summary>
        public string UseItem(string id, General who)
        {
            var item = ItemDb.Get(id);
            switch (item.Kind)
            {
                case ItemKind.Book:
                    var t = TacticDb.Get(item.Teaches);
                    if (Learned.Contains(t.Id)) return "已经学会「" + t.Name + "」了。";
                    Learned.Add(t.Id);
                    Items.Remove(id);
                    return "研读《" + item.Name + "》，全军习得法术「" + t.Name + "」！";
                case ItemKind.Heal:
                    if (!who.Alive) return who.Name + "已经败走，药救不回来。";
                    if (who.Soldiers >= who.MaxSoldiers) return who.Name + "兵力是满的。";
                    Items.Remove(id);
                    return who.Name + "用了" + item.Name + "，兵力恢复 " + who.Heal(item.Power) + "。";
                case ItemKind.Revive:
                    if (who.Alive) return who.Name + "好好的，用不着还魂丹。";
                    who.Soldiers = Math.Max(1, who.MaxSoldiers / 2);
                    Items.Remove(id);
                    return who.Name + "服下" + item.Name + "，重新整军，兵力恢复 " + who.Soldiers + "。";
                case ItemKind.Wine:
                    if (TP >= MaxTP) return "法力是满的。";
                    int before = TP;
                    TP = Math.Min(MaxTP, TP + item.Power);
                    Items.Remove(id);
                    return "喝了" + item.Name + "，法力恢复 " + (TP - before) + "。";
                case ItemKind.Material:
                    return item.Name + "没什么用，拿去商店卖了吧。";
                default:
                    if (item.Owner != null && item.Owner != who.Def.Id) return item.Name + "是专属兵器，" + who.Name + "用不了。";
                    Items.Remove(id);
                    if (who.Gear[item.Slot] != null) Items.Add(who.Gear[item.Slot]);
                    who.Gear[item.Slot] = id;
                    return null;   // 换上了格子里就看得到，不用提示
            }
        }

        /// <summary>把 who 身上第 slot 格卸回行囊。</summary>
        public void Unequip(General who, int slot)
        {
            string id = who.Gear[slot];
            if (id == null || !CanHold(id)) return;
            Items.Add(id);
            who.Gear[slot] = null;
        }

        public bool CanHold(string id) { return Items.Contains(id) ? Count(id) < StackMax : Slots().Count < BagSlots; }

        public string Buy(string id)
        {
            var item = ItemDb.Get(id);
            if (Gold < item.Price) return "金钱不够。";
            if (!CanHold(id)) return "行囊满了。";
            Gold -= item.Price;
            Items.Add(id);
            return "买下" + item.Name + "，花了 " + item.Price + " 金。";
        }

        public int SellPrice(string id) { return ItemDb.Get(id).Price / 2; }

        public string Sell(string id)
        {
            var item = ItemDb.Get(id);
            if (item.Price <= 0) return item.Name + "不能卖。";
            Items.Remove(id);
            Gold += SellPrice(id);
            return "卖掉" + item.Name + "，得 " + SellPrice(id) + " 金。";
        }
    }
}
