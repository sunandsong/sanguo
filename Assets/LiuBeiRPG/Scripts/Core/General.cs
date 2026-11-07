using System;
using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    /// <summary>
    /// 武将的静态数据（相当于数据表里的一行）。
    /// 以后可以改成 ScriptableObject 或从 CSV/JSON 读取。
    /// </summary>
    public class GeneralDef
    {
        public string Id;
        public string Name;
        public string Glyph;            // 地图/战斗里显示的单字，例如“刘”
        public string Color = "#ffffff"; // 显示颜色

        public int Wu;    // 武力：普通攻击伤害
        public int Zhi;   // 智力：策略伤害/命中/回复
        public int Tong;  // 防御力：减少受到的伤害
        public int Su;    // 速度：行动顺序

        public int BaseSoldiers;        // 1 级兵力（吞食天地式：兵力 = 血量），兵力上限 = 基础兵力 × G(等级)

        public bool IsElite;            // 精英（头目之类）：经验金钱 ×2；首领 ×10。数值按等级算，见 Formula.ExpReward
        public string Drop;             // 掉落的物品 Id
        public double DropRate;         // 掉落几率 0..1
        public string Junk;             // 必掉的材料杂物 Id（每个敌人掉一件），null = 不掉
        public string[] Tactics = new string[0]; // 敌方可用的策略
        public bool IsBoss;
        public bool Recruitable;        // 打败后有机会劝降入队（剧情敌将、首领不能收）
        public string WantsHorse;       // 要先送这匹名马（物品 Id）才肯来
        public string Skin;             // 骨骼角色名，不填就用 Id（还没做专属角色的先借一个现成的）
    }

    /// <summary>战斗中/队伍中的武将实例。</summary>
    public class General
    {
        public readonly GeneralDef Def;
        public int Level;    // 我方跟全队等级走（Party 同步），敌人按出场时给的等级
        public int MaxSoldiers;
        public int Soldiers;
        public const int GearSlots = 6;
        public string[] Gear = new string[GearSlots];   // 装备的物品 Id，按 ItemDef.Slot 放，null = 空着
        public bool Defending;
        // 战斗里的状态 → 还剩几个回合。施加时多记 1，当前这个没打完的回合不算
        public readonly Dictionary<Buff, int> Buffs = new Dictionary<Buff, int>();

        public bool Has(Buff b) { return Buffs.ContainsKey(b); }
        public void AddBuff(Buff b, int turns) { Buffs[b] = turns + 1; }
        public void ClearBuffs() { Buffs.Clear(); Defending = false; }

        /// <summary>回合结束：每个状态少一回合，到 0 就消。返回这回合消掉的。</summary>
        public List<Buff> TickBuffs()
        {
            var gone = new List<Buff>();
            foreach (var b in new List<Buff>(Buffs.Keys))
                if (--Buffs[b] <= 0) { Buffs.Remove(b); gone.Add(b); }
            return gone;
        }

        public string Name { get { return Def.Name; } }
        // 法器（九节杖、羽扇）占武器格但加的是智力
        public int Wu { get { return Def.Wu + (MagicWeapon ? 0 : Bonus(ItemKind.Weapon)); } }
        public int Zhi { get { return Def.Zhi + Bonus(ItemKind.Trinket) + (MagicWeapon ? Bonus(ItemKind.Weapon) : 0); } }
        bool MagicWeapon { get { var w = ItemDb.Get(Weapon); return w != null && w.Magic; } }
        public int Tong { get { return Def.Tong + Bonus(ItemKind.Helmet) + Bonus(ItemKind.Armor); } }
        public int Su { get { return Def.Su + Bonus(ItemKind.Boots) + Bonus(ItemKind.Horse) + (Has(Buff.Haste) ? 30 : 0); } }
        // 战斗里真正用的：四维（含装备）× 成长倍率。速度不乘，只管出手顺序
        public int Atk { get { return Grown(Wu); } }
        public int Defense { get { return Grown(Tong); } }
        public int Mou { get { return Grown(Zhi); } }
        int Grown(int v) { return (int)Math.Round(v * Formula.Growth(Level)); }
        public string Weapon { get { return Gear[0]; } set { Gear[0] = value; } }
        public string WeaponSprite { get { var w = ItemDb.Get(Weapon); return w == null ? null : w.Sprite; } }

        int Bonus(ItemKind k) { var i = ItemDb.Get(Gear[k - ItemKind.Weapon]); return i == null ? 0 : i.Power; }
        public bool Alive { get { return Soldiers > 0; } }

        public General(GeneralDef def, int level = 1)
        {
            Def = def;
            SetLevel(level);
            Soldiers = MaxSoldiers;
        }

        /// <summary>改等级：兵力上限跟着变，活着的人兵力同涨同跌。</summary>
        public void SetLevel(int level)
        {
            int gain = (int)Math.Round(Def.BaseSoldiers * Formula.Growth(level) * (Def.IsBoss ? Formula.BossSoldiersMul : 1)) - MaxSoldiers;
            Level = level;
            MaxSoldiers += gain;
            if (Alive) Soldiers = Math.Max(1, Math.Min(MaxSoldiers, Soldiers + gain));
        }

        public int TakeDamage(int amount)
        {
            int before = Soldiers;
            Soldiers = Math.Max(0, Soldiers - amount);
            return before - Soldiers;
        }

        public int Heal(int amount)
        {
            if (!Alive) return 0;
            int before = Soldiers;
            Soldiers = Math.Min(MaxSoldiers, Soldiers + amount);
            return Soldiers - before;
        }
    }
}
