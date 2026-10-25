using System;

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
        public int Tong;  // 统率：减少受到的伤害
        public int Su;    // 速度：行动顺序

        public int BaseSoldiers;        // 1 级兵力（吞食天地式：兵力 = 血量）
        public int SoldiersPerLevel;    // 每升一级增加的兵力，0 表示不成长

        public int ExpReward;           // 击败后获得经验
        public int GoldReward;          // 击败后获得金钱
        public string[] Tactics = new string[0]; // 敌方可用的策略
        public bool IsBoss;
    }

    /// <summary>战斗中/队伍中的武将实例。</summary>
    public class General
    {
        public readonly GeneralDef Def;
        public int MaxSoldiers;
        public int Soldiers;
        public int WeaponBonus;   // 武器加成（武力）
        public bool Defending;

        public string Name { get { return Def.Name; } }
        public int Wu { get { return Def.Wu + WeaponBonus; } }
        public int Zhi { get { return Def.Zhi; } }
        public int Tong { get { return Def.Tong; } }
        public int Su { get { return Def.Su; } }
        public bool Alive { get { return Soldiers > 0; } }

        public General(GeneralDef def, int level = 1)
        {
            Def = def;
            MaxSoldiers = def.BaseSoldiers + def.SoldiersPerLevel * Math.Max(0, level - 1);
            Soldiers = MaxSoldiers;
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
