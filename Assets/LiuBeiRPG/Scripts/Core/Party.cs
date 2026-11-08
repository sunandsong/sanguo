using System;
using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    /// <summary>
    /// 玩家队伍。和吞食天地一样，等级/经验/法力是全队共享的。
    /// 队伍最多 10 人，活着的都上场；再多的放到后方（Stored）。
    /// 法术只有军师能放，威力看军师的智力。
    /// </summary>
    public class Party
    {
        public const int MaxFighters = 10, MaxMembers = 10;

        public readonly List<General> Members = new List<General>();
        public readonly List<General> Stored = new List<General>();   // 后方待命的武将，城里「队伍」页调进调出
        public readonly List<General> Away = new List<General>();     // 剧情里暂时离队的（关羽降曹、张飞走散），装备跟着人走，归队原样回来
        General strategist;
        public int Level = 1;
        public int Exp;
        public int Gold;
        public int TP;       // 策略点
        public int MaxTP;
        public int TPBonus;  // 每学一个新法术法力上限随机 +3～5（吞食天地），累计在这里
        public readonly List<string> Items = new List<string>();        // 物品 Id，一格一个
        public readonly HashSet<string> Learned = new HashSet<string>(); // 学会的法术 Id（升级学的、读兵书学的都在）

        static readonly Random learnRng = new Random();

        public Party()
        {
            RecalcMaxTP();
            TP = MaxTP;
        }

        public int ExpToNext { get { return 20 + Level * Level * 10; } }

        public void RecalcMaxTP() { MaxTP = 8 + Level * 4 + TPBonus; }

        public void Add(General g)
        {
            // 新入队的按全队等级补足兵力
            g.SetLevel(Level);
            g.Soldiers = g.MaxSoldiers;
            if (Members.Count >= MaxMembers) Stored.Add(g);
            else Members.Add(g);
            LearnByLevel();   // 来了个更聪明的军师，之前智力不够没学会的法术这时补上
        }

        public bool Has(GeneralDef def)
        {
            foreach (var m in Members) if (m.Def == def) return true;
            foreach (var m in Stored) if (m.Def == def) return true;
            foreach (var m in Away) if (m.Def == def) return true;
            return false;
        }

        /// <summary>剧情离队：从队里或后方拿出来放进 Away，装备、兵力都留在他身上。</summary>
        public void Leave(GeneralDef def)
        {
            var g = Members.Find(m => m.Def == def) ?? Stored.Find(m => m.Def == def);
            if (g == null) return;
            Members.Remove(g);
            Stored.Remove(g);
            Away.Add(g);
            while (Members.Count < MaxMembers && Stored.Count > 0) { Members.Add(Stored[0]); Stored.RemoveAt(0); }   // 空出来的位置后方补上
        }

        /// <summary>剧情归队：按全队等级补足兵力，队里满了放后方。</summary>
        public void Rejoin(GeneralDef def)
        {
            var g = Away.Find(m => m.Def == def);
            if (g == null) return;
            Away.Remove(g);
            g.SetLevel(Level);
            g.Soldiers = g.MaxSoldiers;
            Members.Insert(Math.Min(Members.Count, Members.FindAll(m => m.Def.Id == "liubei" || m.Def.Id == "guanyu" || m.Def.Id == "zhangfei").Count), g);   // 兄弟排在主公后面
            while (Members.Count > MaxMembers) { var last = Members[Members.Count - 1]; Members.RemoveAt(Members.Count - 1); Stored.Insert(0, last); }   // 挤出来的到后方
        }

        /// <summary>这一仗上场的：活着的前 MaxFighters 个。队里比上场人数多时军师不上场，留在后备只供法术（吞食天地）。</summary>
        public List<General> Fighters()
        {
            var list = new List<General>();
            bool benchStrategist = Members.Count > MaxFighters;
            foreach (var m in Members)
                if (m.Alive && list.Count < MaxFighters && !(benchStrategist && m == Strategist)) list.Add(m);
            return list;
        }

        // ---------------- 队伍调整（城里的「队伍」页） ----------------

        /// <summary>前排 / 后备里换顺序：第 i 个和第 i+d 个对调。</summary>
        public void Swap(int i, int d)
        {
            int j = i + d;
            if (i < 0 || j < 0 || i >= Members.Count || j >= Members.Count) return;
            var t = Members[i]; Members[i] = Members[j]; Members[j] = t;
        }

        /// <summary>调到后方。主公不能走，返回提示。</summary>
        public string ToStored(General g)
        {
            if (g.Def.Id == "liubei") return "主公不能离队。";
            if (Members.Count <= 1) return "队里不能没有人。";
            Members.Remove(g);
            Stored.Add(g);
            return null;
        }

        public string ToMembers(General g)
        {
            if (Members.Count >= MaxMembers) return "队伍已满（最多 " + MaxMembers + " 人）。";
            Stored.Remove(g);
            Members.Add(g);
            return null;
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
            foreach (var m in Members) { m.Soldiers = m.MaxSoldiers; m.ClearBuffs(); }
            foreach (var m in Stored) m.Soldiers = m.MaxSoldiers;
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
                    int max = m.MaxSoldiers, atk = m.Atk;
                    m.SetLevel(Level);
                    msgs.Add(m.Name + " 兵力上限 +" + (m.MaxSoldiers - max) + "，攻击 +" + (m.Atk - atk));
                }
                foreach (var m in Stored) m.SetLevel(Level);
                msgs.AddRange(LearnByLevel());
            }
            return msgs;
        }

        /// <summary>升级学法术（吞食天地）：等级到了、军师智力够就学会，每学一个法力上限 +3～5。返回「习得法术「X」！」提示。</summary>
        public List<string> LearnByLevel()
        {
            var msgs = new List<string>();
            var s = Strategist;
            foreach (var t in TacticDb.All)
                if (t.PlayerUsable && !t.Book && t.MinLevel <= Level && !Learned.Contains(t.Id) && s != null && s.Zhi >= t.MinZhi)
                    msgs.Add(Learn(t));
            return msgs;
        }

        string Learn(TacticDef t)
        {
            Learned.Add(t.Id);
            int bonus = 3 + learnRng.Next(3);
            TPBonus += bonus;
            RecalcMaxTP();
            TP += bonus;
            return "习得法术「" + t.Name + "」！法力上限 +" + bonus;
        }

        public List<TacticDef> AvailableTactics()
        {
            var list = new List<TacticDef>();
            foreach (var t in TacticDb.All)
                if (t.PlayerUsable && Learned.Contains(t.Id)) list.Add(t);
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
                    Items.Remove(id);
                    return "研读《" + item.Name + "》，全军" + Learn(t);
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
                case ItemKind.Tool:   // 地图上的道具由 GameRoot.UseTool 处理，战斗里的由 BattleSystem.DoItem 处理；走到这儿就是用错地方
                    return item.Name + (item.InBattle ? "只能在战斗中使用。" : "只能在地图上使用。");
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

        /// <summary>剧情给的（不标价的）东西不能扔：兵书、专属兵器、任务物品。</summary>
        public static bool CanDiscard(string id) { var i = ItemDb.Get(id); return i != null && i.Price > 0; }

        /// <summary>扔掉 id：all = 整格都扔，否则扔一个。返回提示；扔不了返回原因。</summary>
        public string Discard(string id, bool all)
        {
            var item = ItemDb.Get(id);
            if (!CanDiscard(id)) return "「" + item.Name + "」是要紧的东西，不能扔。";
            int n = all ? Count(id) : 1;
            for (int k = 0; k < n; k++) Items.Remove(id);
            return "扔掉了「" + item.Name + "」" + (n > 1 ? " × " + n : "") + "。";
        }

        /// <summary>放生招降来的人：身上装备先卸回行囊，行囊放不下就不放生。剧情武将不能放。返回原因，成功返回 null。</summary>
        public string Release(General g)
        {
            if (!g.Def.Recruitable) return g.Name + "不是招降来的，不能放生。";
            for (int s = 0; s < General.GearSlots; s++) Unequip(g, s);
            foreach (var id in g.Gear) if (id != null) return "行囊满了，" + g.Name + "身上的装备卸不下来。先腾出地方再放生。";
            Members.Remove(g);
            Stored.Remove(g);
            return null;
        }

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
