using System;

namespace LiuBeiRPG.Core
{
    /// <summary>
    /// 伤害公式。核心思路来自吞食天地：兵力既是血量也影响输出，
    /// 兵力越少打得越疼不起来（按对数衰减，100/1000/10000 是明显的台阶）。
    /// 调平衡只改这个文件。
    /// </summary>
    public static class Formula
    {
        public const double CritRate = 0.08;

        public static double TroopScale(int soldiers)
        {
            if (soldiers <= 0) return 0;
            return 0.6 + 0.3 * Math.Log10(soldiers + 1);   // 100兵≈1.2  1000兵≈1.5  10000兵≈1.8
        }

        public static int Attack(General atk, General def, Random rng, out bool crit)
        {
            double dmg = atk.Wu * 1.2 * TroopScale(atk.Soldiers) * 100.0 / (100 + def.Tong);
            dmg *= 0.85 + rng.NextDouble() * 0.3;
            crit = rng.NextDouble() < CritRate;
            if (crit) dmg *= 1.5;
            if (def.Defending) dmg *= 0.5;
            return Math.Max(1, (int)Math.Round(dmg));
        }

        /// <summary>策略命中率：智力差决定。</summary>
        public static bool TacticHits(General caster, General target, Random rng)
        {
            double chance = 0.8 + (caster.Zhi - target.Zhi) / 200.0;
            chance = Math.Max(0.3, Math.Min(0.98, chance));
            return rng.NextDouble() < chance;
        }

        public static int TacticDamage(General caster, General target, TacticDef t, Random rng)
        {
            double dmg = caster.Zhi * 1.6 * t.Power * 120.0 / (100 + target.Zhi);
            dmg *= 0.9 + rng.NextDouble() * 0.2;
            if (target.Defending) dmg *= 0.75;
            return Math.Max(1, (int)Math.Round(dmg));
        }

        public static int TacticHeal(General caster, TacticDef t, Random rng)
        {
            double heal = caster.Zhi * 3.0 * t.Power * (0.9 + rng.NextDouble() * 0.2);
            return Math.Max(1, (int)Math.Round(heal));
        }

        /// <summary>撤退成功率：比速度。Boss 战不可撤退。</summary>
        public static double RetreatChance(double partySpeed, double enemySpeed)
        {
            double c = 0.55 + (partySpeed - enemySpeed) / 100.0;
            return Math.Max(0.2, Math.Min(0.9, c));
        }
    }
}
