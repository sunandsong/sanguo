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
            Wu = 75, Zhi = 78, Tong = 80, Su = 70, BaseSoldiers = 300
        };
        public static readonly GeneralDef GuanYu = new GeneralDef
        {
            Id = "guanyu", Name = "关羽", Glyph = "关", Color = "#6bd47a",
            Wu = 97, Zhi = 75, Tong = 88, Su = 72, BaseSoldiers = 400
        };
        public static readonly GeneralDef ZhangFei = new GeneralDef
        {
            Id = "zhangfei", Name = "张飞", Glyph = "张", Color = "#9aa8ff",
            Wu = 98, Zhi = 30, Tong = 60, Su = 80, BaseSoldiers = 400
        };

        // ---------- 黄巾军 ----------
        // 弱点靠防御力 / 智力拉开：力士甲厚脑笨（砍不动，怕策略），术士身弱智高（策略打不动，怕砍），兵和头目两样都行
        public static readonly GeneralDef HuangJinBing = new GeneralDef
        {
            Id = "hjb", Name = "黄巾兵", Glyph = "兵", Color = "#e8c547",
            Wu = 75, Zhi = 20, Tong = 40, Su = 50, BaseSoldiers = 160,
            Drop = "caoyao", DropRate = 0.2, Junk = "huangjinbu"
        };
        public static readonly GeneralDef HuangJinLiShi = new GeneralDef
        {
            Id = "hjls", Name = "黄巾力士", Glyph = "力", Color = "#e89a47",
            Wu = 90, Zhi = 15, Tong = 110, Su = 40, BaseSoldiers = 230,
            Drop = "zhuojiu", DropRate = 0.15, Junk = "huangjinbu"
        };
        public static readonly GeneralDef HuangJinShuShi = new GeneralDef
        {
            Id = "hjss", Name = "黄巾术士", Glyph = "术", Color = "#c47be8",
            Wu = 35, Zhi = 110, Tong = 35, Su = 60, BaseSoldiers = 130,
            Tactics = new[] { "yaoshu" }, Drop = "zhuojiu", DropRate = 0.25, Junk = "huangjinbu"
        };
        public static readonly GeneralDef HuangJinTouMu = new GeneralDef
        {
            Id = "hjtm", Name = "黄巾头目", Glyph = "头", Color = "#ff7a5c",
            Wu = 82, Zhi = 60, Tong = 70, Su = 55, BaseSoldiers = 380, IsElite = true,
            Drop = "caoyao", DropRate = 0.5, Junk = "huangjinbu", Recruitable = true   // 打败后有机会劝降（吞食天地的战后收编）
        };

        // ---------- 第二章：张角三兄弟（设计文档 4.3），简雍来投（4.1）。骨骼角色在 sanguoguge 的 Puppet 引擎里（2026-11-03 已做） ----------
        public static readonly GeneralDef JianYong = new GeneralDef
        {
            Id = "jianyong", Name = "简雍", Glyph = "简", Color = "#c9d8f0",
            Wu = 30, Zhi = 68, Tong = 35, Su = 50, BaseSoldiers = 200
        };
        public static readonly GeneralDef ZhangBao = new GeneralDef
        {
            Id = "zhangbao", Name = "张宝", Glyph = "宝", Color = "#d49cff",
            Wu = 95, Zhi = 92, Tong = 78, Su = 60, BaseSoldiers = 3400, IsBoss = true,
            Tactics = new[] { "yaoshu", "luolei" }, Drop = "jiujiezhang", DropRate = 1
        };
        public static readonly GeneralDef ZhangLiang = new GeneralDef
        {
            Id = "zhangliang", Name = "张梁", Glyph = "梁", Color = "#ffb060",
            Wu = 108, Zhi = 50, Tong = 82, Su = 62, BaseSoldiers = 3000, IsBoss = true,
            Drop = "langyabang", DropRate = 1
        };
        public static readonly GeneralDef ZhangJiao = new GeneralDef
        {
            Id = "zhangjiao", Name = "张角", Glyph = "角", Color = "#ffe066",
            Wu = 75, Zhi = 100, Tong = 88, Su = 65, BaseSoldiers = 3400, IsBoss = true,
            Tactics = new[] { "yaoshu", "luolei", "hunluan" }, Drop = "taipingyaoshu", DropRate = 1
        };

        /// <summary>所有武将表，读档按 Id 找人。</summary>
        public static readonly GeneralDef[] All = { LiuBei, GuanYu, ZhangFei, JianYong, HuangJinBing, HuangJinLiShi, HuangJinShuShi, HuangJinTouMu, ChengYuanZhi, DengMao, ZhangBao, ZhangLiang, ZhangJiao };

        public static GeneralDef Find(string id) { return Array.Find(All, d => d.Id == id); }

        // ---------- 第一章 Boss（演义第一回：刘关张初战程远志） ----------
        public static readonly GeneralDef ChengYuanZhi = new GeneralDef
        {
            Id = "chengyuanzhi", Name = "程远志", Glyph = "程", Color = "#ff4d4d",
            Wu = 118, Zhi = 40, Tong = 65, Su = 60, BaseSoldiers = 950, IsBoss = true,
            Drop = "yupei", DropRate = 1
        };
        public static readonly GeneralDef DengMao = new GeneralDef
        {
            Id = "dengmao", Name = "邓茂", Glyph = "邓", Color = "#ff8a4d",
            Wu = 108, Zhi = 30, Tong = 55, Su = 65, BaseSoldiers = 600, IsBoss = true,
            Drop = "pikui", DropRate = 1
        };

        /// <summary>野外随机遇敌。mapPool 是这张图的兵种表（空 = 第一章的黄巾表，等级越高越容易遇到头目）。敌人等级 = 地图等级随机 ±1。</summary>
        public static List<General> RandomEncounter(Random rng, int partyLevel, int mapLevel, GeneralDef[] mapPool = null)
        {
            var pool = mapPool != null ? new List<GeneralDef>(mapPool) : new List<GeneralDef> { HuangJinBing, HuangJinBing, HuangJinBing, HuangJinLiShi, HuangJinShuShi };
            if (mapPool == null && partyLevel >= 2) pool.Add(HuangJinLiShi);
            if (mapPool == null && partyLevel >= 3) pool.Add(HuangJinTouMu);

            int count = 1 + rng.Next(partyLevel >= 2 ? 5 : 3);   // 和吞食天地一样最多 5 队：1级 1~3 队，2级起 1~5 队
            var list = new List<General>();
            for (int i = 0; i < count; i++) list.Add(new General(pool[rng.Next(pool.Count)], Math.Max(1, mapLevel + rng.Next(-1, 2))));
            return list;
        }

        public static List<General> Chapter1Boss()
        {
            return new List<General>
            {
                new General(HuangJinBing, 3),
                new General(DengMao, 3),
                new General(ChengYuanZhi, 4),
                new General(HuangJinShuShi, 3),
                new General(HuangJinLiShi, 3),
            };
        }

        public static List<General> Chapter2Siege()
        {
            return new List<General> { new General(HuangJinTouMu, 10), new General(HuangJinShuShi, 10), new General(ZhangBao, 10), new General(HuangJinShuShi, 10), new General(HuangJinTouMu, 10) };
        }

        public static List<General> Chapter2Bridge()
        {
            return new List<General> { new General(HuangJinTouMu, 10), new General(HuangJinLiShi, 10), new General(ZhangLiang, 10), new General(HuangJinLiShi, 10), new General(HuangJinTouMu, 10) };
        }

        public static List<General> Chapter2Boss()
        {
            return new List<General> { new General(HuangJinShuShi, 10), new General(HuangJinTouMu, 10), new General(ZhangJiao, 10), new General(HuangJinTouMu, 10), new General(HuangJinShuShi, 10) };
        }
    }
}
