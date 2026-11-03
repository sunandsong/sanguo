using System;

namespace LiuBeiRPG.Core
{
    /// <summary>
    /// 成长和伤害公式，见 Docs/设计文档.md 第 1、2 节。核心思路来自吞食天地：兵力既是血量也影响输出，残兵打不疼，兵过百、过千、过万各涨一档。
    /// 攻防都按等级同比例放大，用「攻防比」算伤害，同级之间几刀砍死一个人不随等级变。调平衡只改这个文件。
    /// </summary>
    public static class Formula
    {
        public const double CritRate = 0.08;

        /// <summary>成长倍率 G(L)：兵力、攻、防、谋都乘它。1 级 1.00，5 级 1.35，10 级 1.88，99 级 28。</summary>
        public static double Growth(int level)
        {
            int l = Math.Max(0, level - 1);
            return 1 + 0.08 * l + 0.002 * l * l;
        }

        /// <summary>兵力系数：满兵 1.0，残兵 0.6；再按当前兵力分档，不到 100 ×0.8，过千 ×1.15，过万 ×1.3（吞食天地的「兵多打得疼」）。</summary>
        public static double TroopScale(General g)
        {
            if (g.MaxSoldiers <= 0) return 0;
            int s = g.Soldiers;
            double tier = s < 100 ? 0.8 : s < 1000 ? 1.0 : s < 10000 ? 1.15 : 1.3;
            return (0.6 + 0.4 * s / g.MaxSoldiers) * tier;
        }

        /// <summary>普通攻击。mul：额外倍率（撤退失败被追杀 ×1.5、连击的第二下 ×0.5）。</summary>
        public static int Attack(General atk, General def, Random rng, out bool crit, double mul = 1)
        {
            double a = atk.Atk, d = def.Defense;
            double dmg = 1.5 * a * a / (a + d) * TroopScale(atk) * mul;
            dmg *= 0.85 + rng.NextDouble() * 0.3;
            crit = rng.NextDouble() < CritRate;
            if (crit) dmg *= 1.5;
            if (atk.Has(Buff.Rally)) dmg *= 1.5;
            if (atk.Has(Buff.Fury)) dmg *= 2;
            if (def.Has(Buff.Guard)) dmg *= 0.7;
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

        /// <summary>暗杀成功率：智力差决定，35% 起，夹在 5%～90%。</summary>
        public static bool Assassinates(General caster, General target, Random rng)
        {
            double chance = 0.35 + (caster.Zhi - target.Zhi) / 150.0;
            return rng.NextDouble() < Math.Max(0.05, Math.Min(0.9, chance));
        }

        public static int TacticDamage(General caster, General target, TacticDef t, Random rng)
        {
            double m = caster.Mou, mt = target.Mou;
            double dmg = 1.5 * m * m / (m + mt) * t.Power;
            dmg *= 0.9 + rng.NextDouble() * 0.2;
            if (target.Has(Buff.Guard)) dmg *= 0.7;
            if (target.Defending) dmg *= 0.75;
            return Math.Max(1, (int)Math.Round(dmg));
        }

        public static int TacticHeal(General caster, TacticDef t, Random rng)
        {
            double heal = caster.Mou * 3.0 * t.Power * (0.9 + rng.NextDouble() * 0.2);
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
