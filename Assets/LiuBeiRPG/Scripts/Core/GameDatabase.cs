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

        // ---------- 第三～七章（设计文档 4.1～4.3）。骨骼角色清单见 ArtTask/出图任务-第三到七章.md，没做之前 Skin 先借现成的 ----------
        // 我方
        public static readonly GeneralDef ZhaoYun = new GeneralDef { Id = "zhaoyun", Name = "赵云", Glyph = "赵", Color = "#e8f4ff", Wu = 96, Zhi = 76, Tong = 91, Su = 88, BaseSoldiers = 450, Skin = "shouwei" };
        public static readonly GeneralDef SunQian = new GeneralDef { Id = "sunqian", Name = "孙乾", Glyph = "孙", Color = "#d8e8c8", Wu = 28, Zhi = 72, Tong = 30, Su = 55, BaseSoldiers = 200, Skin = "jianyong" };
        public static readonly GeneralDef MiZhu = new GeneralDef { Id = "mizhu", Name = "糜竺", Glyph = "糜", Color = "#f0e0b0", Wu = 25, Zhi = 70, Tong = 32, Su = 45, BaseSoldiers = 220, Skin = "shangren" };
        public static readonly GeneralDef ChenDao = new GeneralDef { Id = "chendao", Name = "陈到", Glyph = "陈", Color = "#c0d0e0", Wu = 82, Zhi = 56, Tong = 80, Su = 75, BaseSoldiers = 360, Skin = "shouwei" };
        public static readonly GeneralDef ZhouCang = new GeneralDef { Id = "zhoucang", Name = "周仓", Glyph = "周", Color = "#a08060", Wu = 85, Zhi = 30, Tong = 58, Su = 72, BaseSoldiers = 350, Skin = "hjtm" };
        public static readonly GeneralDef GuanPing = new GeneralDef { Id = "guanping", Name = "关平", Glyph = "平", Color = "#8fd49a", Wu = 82, Zhi = 60, Tong = 72, Su = 70, BaseSoldiers = 380, Skin = "shouwei" };
        public static readonly GeneralDef ZhuGeLiang = new GeneralDef { Id = "zhugeliang", Name = "诸葛亮", Glyph = "诸", Color = "#ffffff", Wu = 38, Zhi = 100, Tong = 95, Su = 68, BaseSoldiers = 260, Skin = "jianyong" };
        public static readonly GeneralDef XuShu = new GeneralDef { Id = "xushu", Name = "徐庶", Glyph = "徐", Color = "#d0c8f0", Wu = 65, Zhi = 93, Tong = 85, Su = 66, BaseSoldiers = 280, Skin = "jianyong" };
        public static readonly GeneralDef LiuFeng = new GeneralDef { Id = "liufeng", Name = "刘封", Glyph = "封", Color = "#ffe0a0", Wu = 80, Zhi = 45, Tong = 68, Su = 70, BaseSoldiers = 360, Skin = "shouwei" };

        // 敌方小兵（4.2）。弱点同黄巾：铁骑 / 飞熊 / 虎豹骑甲厚怕法术，弓兵身弱怕砍
        public static readonly GeneralDef XiLiangBing = new GeneralDef { Id = "xiliangbing", Name = "西凉兵", Glyph = "凉", Color = "#d8c8a0", Wu = 85, Zhi = 25, Tong = 55, Su = 70, BaseSoldiers = 220, Drop = "aiye", DropRate = 0.2, Junk = "pojia", Skin = "huangjin" };
        public static readonly GeneralDef XiLiangTieQi = new GeneralDef { Id = "xiliangtieqi", Name = "西凉铁骑", Glyph = "骑", Color = "#b0a890", Wu = 95, Zhi = 20, Tong = 60, Su = 85, BaseSoldiers = 280, Drop = "qingjiu", DropRate = 0.15, Junk = "pojia", Skin = "hjls" };
        public static readonly GeneralDef FeiXiongJun = new GeneralDef { Id = "feixiongjun", Name = "飞熊军", Glyph = "熊", Color = "#c08050", Wu = 100, Zhi = 30, Tong = 110, Su = 75, BaseSoldiers = 320, IsElite = true, Drop = "aiye", DropRate = 0.4, Junk = "pojia", Recruitable = true, Skin = "hjtm" };
        public static readonly GeneralDef HuaiNanBing = new GeneralDef { Id = "huainanbing", Name = "淮南兵", Glyph = "淮", Color = "#c8d8a0", Wu = 78, Zhi = 35, Tong = 55, Su = 55, BaseSoldiers = 220, Drop = "jinchuangyao", DropRate = 0.15, Junk = "pojia", Skin = "huangjin" };
        public static readonly GeneralDef CaoJunBuBing = new GeneralDef { Id = "caojunbubing", Name = "曹军步兵", Glyph = "曹", Color = "#a0b8d8", Wu = 85, Zhi = 40, Tong = 65, Su = 60, BaseSoldiers = 250, Drop = "jinchuangyao", DropRate = 0.15, Junk = "pojia", Skin = "huangjin" };
        public static readonly GeneralDef CaoJunGongBing = new GeneralDef { Id = "caojungongbing", Name = "曹军弓兵", Glyph = "弓", Color = "#90a8c8", Wu = 70, Zhi = 45, Tong = 50, Su = 65, BaseSoldiers = 200, Tactics = new[] { "jianyu" }, Drop = "qingjiu", DropRate = 0.2, Junk = "pojia", Skin = "hjss" };
        public static readonly GeneralDef QingZhouBing = new GeneralDef { Id = "qingzhoubing", Name = "青州兵", Glyph = "青", Color = "#88b090", Wu = 88, Zhi = 30, Tong = 62, Su = 58, BaseSoldiers = 260, Drop = "zhixuesan", DropRate = 0.1, Junk = "pojia", Skin = "hjls" };
        public static readonly GeneralDef HuBaoQi = new GeneralDef { Id = "hubaoqi", Name = "虎豹骑", Glyph = "虎", Color = "#e0a060", Wu = 100, Zhi = 40, Tong = 115, Su = 90, BaseSoldiers = 320, IsElite = true, Drop = "zhixuesan", DropRate = 0.4, Junk = "pojia", Recruitable = true, Skin = "hjtm" };

        // Boss（4.3）。吕布两次出场共用一个 Def，等级在编队里给
        public static readonly GeneralDef HuaXiong = new GeneralDef { Id = "huaxiong", Name = "华雄", Glyph = "华", Color = "#ff6a4d", Wu = 105, Zhi = 40, Tong = 72, Su = 70, BaseSoldiers = 1200, IsBoss = true, Drop = "dakandao", DropRate = 1, Skin = "zhangliang" };
        public static readonly GeneralDef LvBu = new GeneralDef { Id = "lvbu", Name = "吕布", Glyph = "吕", Color = "#ff3030", Wu = 150, Zhi = 30, Tong = 90, Su = 95, BaseSoldiers = 4000, IsBoss = true, Drop = "fangtianhuaji", DropRate = 1, Skin = "chengyuanzhi" };
        public static readonly GeneralDef GuanHai = new GeneralDef { Id = "guanhai", Name = "管亥", Glyph = "管", Color = "#e8b040", Wu = 100, Zhi = 25, Tong = 65, Su = 68, BaseSoldiers = 1300, IsBoss = true, Drop = "jinchuangyao", DropRate = 1, Skin = "hjtm" };
        public static readonly GeneralDef JiLing = new GeneralDef { Id = "jiling", Name = "纪灵", Glyph = "纪", Color = "#d0a070", Wu = 104, Zhi = 45, Tong = 78, Su = 70, BaseSoldiers = 1500, IsBoss = true, Drop = "sanjiandao", DropRate = 1, Skin = "chengyuanzhi" };
        public static readonly GeneralDef CheZhou = new GeneralDef { Id = "chezhou", Name = "车胄", Glyph = "车", Color = "#b0b0c0", Wu = 85, Zhi = 40, Tong = 70, Su = 60, BaseSoldiers = 1500, IsBoss = true, Drop = "yulinjia", DropRate = 1, Skin = "shouwei" };
        public static readonly GeneralDef DianWei = new GeneralDef { Id = "dianwei", Name = "典韦", Glyph = "典", Color = "#e07040", Wu = 125, Zhi = 30, Tong = 85, Su = 70, BaseSoldiers = 2000, IsBoss = true, Drop = "shuangtieji", DropRate = 1, Skin = "zhangliang" };
        public static readonly GeneralDef XuChu = new GeneralDef { Id = "xuchu", Name = "许褚", Glyph = "许", Color = "#f0c060", Wu = 128, Zhi = 25, Tong = 82, Su = 65, BaseSoldiers = 2200, IsBoss = true, Drop = "zhanchui", DropRate = 1, Skin = "zhangliang" };
        public static readonly GeneralDef LiuPi = new GeneralDef { Id = "liupi", Name = "刘辟", Glyph = "辟", Color = "#d8c060", Wu = 85, Zhi = 40, Tong = 60, Su = 66, BaseSoldiers = 1300, IsBoss = true, Drop = "junma", DropRate = 1, Skin = "hjtm" };
        public static readonly GeneralDef GongDu = new GeneralDef { Id = "gongdu", Name = "龚都", Glyph = "龚", Color = "#c8b050", Wu = 85, Zhi = 40, Tong = 60, Su = 66, BaseSoldiers = 1300, IsBoss = true, Skin = "hjtm" };
        public static readonly GeneralDef CaiYang = new GeneralDef { Id = "caiyang", Name = "蔡阳", Glyph = "蔡", Color = "#80a0d0", Wu = 102, Zhi = 45, Tong = 74, Su = 62, BaseSoldiers = 1800, IsBoss = true, Drop = "sunzibingfa", DropRate = 1, Skin = "chengyuanzhi" };
        public static readonly GeneralDef XiaHouDun = new GeneralDef { Id = "xiahoudun", Name = "夏侯惇", Glyph = "惇", Color = "#6080c0", Wu = 115, Zhi = 58, Tong = 88, Su = 72, BaseSoldiers = 2400, IsBoss = true, Drop = "suozijia", DropRate = 1, Skin = "chengyuanzhi" };
        public static readonly GeneralDef CaoRen = new GeneralDef { Id = "caoren", Name = "曹仁", Glyph = "仁", Color = "#7090c8", Wu = 105, Zhi = 62, Tong = 92, Su = 66, BaseSoldiers = 2600, IsBoss = true, Tactics = new[] { "jianshou" }, Drop = "sanlue", DropRate = 1, Skin = "dengmao" };   // 八门金锁 = 坚守

        /// <summary>所有武将表，读档按 Id 找人。</summary>
        public static readonly GeneralDef[] All =
        {
            LiuBei, GuanYu, ZhangFei, JianYong, HuangJinBing, HuangJinLiShi, HuangJinShuShi, HuangJinTouMu, ChengYuanZhi, DengMao, ZhangBao, ZhangLiang, ZhangJiao,
            ZhaoYun, SunQian, MiZhu, ChenDao, ZhouCang, GuanPing, ZhuGeLiang, XuShu, LiuFeng,
            XiLiangBing, XiLiangTieQi, FeiXiongJun, HuaiNanBing, CaoJunBuBing, CaoJunGongBing, QingZhouBing, HuBaoQi,
            HuaXiong, LvBu, GuanHai, JiLing, CheZhou, DianWei, XuChu, LiuPi, GongDu, CaiYang, XiaHouDun, CaoRen,
        };

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

        // ---------- 第三～七章 Boss 编队：Boss 居中，两边是本章小兵 ----------
        static List<General> Line(int lv, GeneralDef a, GeneralDef b, GeneralDef boss, GeneralDef c, GeneralDef d)
        {
            return new List<General> { new General(a, lv), new General(b, lv), new General(boss, lv), new General(c, lv), new General(d, lv) };
        }
        public static List<General> Chapter3HuaXiong() { return Line(13, XiLiangBing, XiLiangTieQi, HuaXiong, XiLiangTieQi, XiLiangBing); }
        public static List<General> Chapter3LvBu() { return Line(18, FeiXiongJun, XiLiangTieQi, LvBu, XiLiangTieQi, FeiXiongJun); }
        public static List<General> Chapter4GuanHai() { return Line(19, HuangJinTouMu, HuangJinLiShi, GuanHai, HuangJinShuShi, HuangJinTouMu); }
        public static List<General> Chapter4JiLing() { return Line(22, HuaiNanBing, HuaiNanBing, JiLing, HuaiNanBing, HuaiNanBing); }
        public static List<General> Chapter4LvBu() { return Line(26, FeiXiongJun, XiLiangTieQi, LvBu, XiLiangTieQi, FeiXiongJun); }
        public static List<General> Chapter5DianWei() { return new List<General> { new General(DianWei, 30) }; }   // 校场单挑
        public static List<General> Chapter5XuChu() { return Line(31, HuBaoQi, CaoJunBuBing, XuChu, CaoJunBuBing, HuBaoQi); }
        public static List<General> Chapter5CheZhou() { return Line(33, QingZhouBing, CaoJunGongBing, CheZhou, CaoJunGongBing, QingZhouBing); }
        public static List<General> Chapter6LiuPi() { return new List<General> { new General(HuangJinTouMu, 34), new General(LiuPi, 34), new General(GongDu, 34), new General(HuangJinTouMu, 34) }; }
        public static List<General> Chapter6CaiYang() { return Line(36, QingZhouBing, CaoJunGongBing, CaiYang, CaoJunGongBing, QingZhouBing); }
        public static List<General> Chapter7CaoRen() { return Line(42, HuBaoQi, CaoJunGongBing, CaoRen, CaoJunGongBing, HuBaoQi); }
        public static List<General> Chapter7XiaHouDun() { return Line(45, HuBaoQi, CaoJunBuBing, XiaHouDun, CaoJunBuBing, HuBaoQi); }
    }
}
