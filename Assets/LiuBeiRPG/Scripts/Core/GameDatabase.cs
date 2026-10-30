using System;
using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    /// <summary>
    /// 武将数据表。Demo 里直接写在代码里，方便改数值；
    /// 正式开发建议换成 ScriptableObject 或表格导出的 JSON。
    /// </summary>
    public static class GameDatabase
    {
        // ---------- 我方 ----------
        public static readonly GeneralDef LiuBei = new GeneralDef
        {
            Id = "liubei", Name = "刘备", Glyph = "刘", Color = "#ffd75e",
            Wu = 75, Zhi = 78, Tong = 80, Su = 70, BaseSoldiers = 300, SoldiersPerLevel = 60
        };
        public static readonly GeneralDef GuanYu = new GeneralDef
        {
            Id = "guanyu", Name = "关羽", Glyph = "关", Color = "#6bd47a",
            Wu = 97, Zhi = 75, Tong = 88, Su = 72, BaseSoldiers = 400, SoldiersPerLevel = 80
        };
        public static readonly GeneralDef ZhangFei = new GeneralDef
        {
            Id = "zhangfei", Name = "张飞", Glyph = "张", Color = "#9aa8ff",
            Wu = 98, Zhi = 30, Tong = 60, Su = 80, BaseSoldiers = 400, SoldiersPerLevel = 80
        };

        // ---------- 黄巾军 ----------
        // 弱点靠防御力 / 智力拉开：力士甲厚脑笨（砍不动，怕策略），术士身弱智高（策略打不动，怕砍），兵和头目两样都行
        public static readonly GeneralDef HuangJinBing = new GeneralDef
        {
            Id = "hjb", Name = "黄巾兵", Glyph = "兵", Color = "#e8c547",
            Wu = 75, Zhi = 20, Tong = 40, Su = 50, BaseSoldiers = 160, ExpReward = 10, GoldReward = 8,
            Drop = "caoyao", DropRate = 0.2, Junk = "huangjinbu"
        };
        public static readonly GeneralDef HuangJinLiShi = new GeneralDef
        {
            Id = "hjls", Name = "黄巾力士", Glyph = "力", Color = "#e89a47",
            Wu = 90, Zhi = 15, Tong = 110, Su = 40, BaseSoldiers = 230, ExpReward = 15, GoldReward = 12,
            Drop = "zhuojiu", DropRate = 0.15, Junk = "huangjinbu"
        };
        public static readonly GeneralDef HuangJinShuShi = new GeneralDef
        {
            Id = "hjss", Name = "黄巾术士", Glyph = "术", Color = "#c47be8",
            Wu = 35, Zhi = 110, Tong = 35, Su = 60, BaseSoldiers = 130, ExpReward = 14, GoldReward = 15,
            Tactics = new[] { "yaoshu" }, Drop = "zhuojiu", DropRate = 0.25, Junk = "huangjinbu"
        };
        public static readonly GeneralDef HuangJinTouMu = new GeneralDef
        {
            Id = "hjtm", Name = "黄巾头目", Glyph = "头", Color = "#ff7a5c",
            Wu = 82, Zhi = 60, Tong = 70, Su = 55, BaseSoldiers = 380, ExpReward = 30, GoldReward = 30,
            Drop = "caoyao", DropRate = 0.5, Junk = "huangjinbu"
        };

        // ---------- 第一章 Boss（演义第一回：刘关张初战程远志） ----------
        public static readonly GeneralDef ChengYuanZhi = new GeneralDef
        {
            Id = "chengyuanzhi", Name = "程远志", Glyph = "程", Color = "#ff4d4d",
            Wu = 118, Zhi = 40, Tong = 65, Su = 60, BaseSoldiers = 1200, ExpReward = 150, GoldReward = 300, IsBoss = true,
            Drop = "yupei", DropRate = 1
        };
        public static readonly GeneralDef DengMao = new GeneralDef
        {
            Id = "dengmao", Name = "邓茂", Glyph = "邓", Color = "#ff8a4d",
            Wu = 108, Zhi = 30, Tong = 55, Su = 65, BaseSoldiers = 700, ExpReward = 80, GoldReward = 100, IsBoss = true,
            Drop = "pikui", DropRate = 1
        };

        /// <summary>野外随机遇敌。等级越高越容易遇到头目。</summary>
        public static List<General> RandomEncounter(Random rng, int partyLevel)
        {
            var pool = new List<GeneralDef> { HuangJinBing, HuangJinBing, HuangJinBing, HuangJinLiShi, HuangJinShuShi };
            if (partyLevel >= 2) pool.Add(HuangJinLiShi);
            if (partyLevel >= 3) pool.Add(HuangJinTouMu);

            int count = 1 + rng.Next(partyLevel >= 2 ? 5 : 3);   // 和吞食天地一样最多 5 队：1级 1~3 队，2级起 1~5 队
            var list = new List<General>();
            for (int i = 0; i < count; i++) list.Add(new General(pool[rng.Next(pool.Count)]));
            return list;
        }

        public static List<General> Chapter1Boss()
        {
            return new List<General>
            {
                new General(HuangJinBing),
                new General(DengMao),
                new General(ChengYuanZhi),
                new General(HuangJinShuShi),
                new General(HuangJinLiShi),
            };
        }
    }
}
