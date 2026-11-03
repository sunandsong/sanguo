using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    // Status = 加 / 解状态，Kill = 暗杀（按智力差一击败走）
    public enum TacticKind { Damage, Heal, Status, Kill }

    /// <summary>目标是相对施放者而言的：Foe = 对方，Ally = 己方。</summary>
    public enum TacticTarget { OneFoe, AllFoes, OneAlly, AllAllies }

    /// <summary>战斗中的状态（吞食天地的防御 / 控制 / 增益法术都落在这上面）。前 4 个是不良状态，安民能解。</summary>
    public enum Buff
    {
        Stun,       // 混乱：不能行动
        Confuse,    // 离间：攻击自己人
        Silence,    // 封策：放不了法术
        Disarm,     // 疑兵：不能普通攻击，每回合有机会挣脱
        Rally,      // 奋战：攻击 ×1.5
        Fury,       // 怒气丸：攻击 ×2，整场
        Guard,      // 坚守：受到伤害 ×0.7
        Haste,      // 疾风：速度 +30
        Twice,      // 连击：每回合多打半力一次
        First,      // 奇袭：先手
        FireWard,   // 水幕：免疫火系
        WaterWard,  // 筑堤：免疫水系
    }

    public class TacticDef
    {
        public string Id;
        public string Name;
        public string Desc;
        public int Cost;
        public int MinLevel;
        public int MinZhi;          // 升到 MinLevel 时军师智力要够这个数才学得会（吞食天地）；差了等换军师再补学
        public TacticKind Kind;
        public TacticTarget Target;
        public double Power;
        public string Element;      // "fire" / "water" / null：水幕挡火、筑堤挡水，藤甲兵怕火
        public Buff Buff;           // Status 类：加哪个状态
        public int Turns = 3;       // 状态持续几个完整回合
        public bool Cure;           // Status 类：是解状态而不是加状态
        public bool PlayerUsable = true;
        public bool Book;   // 只能靠兵书学（见 ItemDb），升级不会自动学

        public bool TargetsFoe { get { return Target == TacticTarget.OneFoe || Target == TacticTarget.AllFoes; } }
        public bool TargetsAll { get { return Target == TacticTarget.AllFoes || Target == TacticTarget.AllAllies; } }
        public static bool IsBad(Buff b) { return b <= Buff.Disarm; }
    }

    /// <summary>名字数值见 Docs/设计文档.md 第 6 节；结构照吞食天地（火 / 水 / 回复逐级替换，防御 / 控制 / 增益各一组）。</summary>
    public static class TacticDb
    {
        static TacticDef Dmg(string id, string name, string desc, int cost, int lv, int zhi, TacticTarget tg, double power, string element = null, bool book = false)
        { return new TacticDef { Id = id, Name = name, Desc = desc, Cost = cost, MinLevel = lv, MinZhi = zhi, Kind = TacticKind.Damage, Target = tg, Power = power, Element = element, Book = book }; }
        static TacticDef Heal(string id, string name, string desc, int cost, int lv, int zhi, TacticTarget tg, double power, bool book = false)
        { return new TacticDef { Id = id, Name = name, Desc = desc, Cost = cost, MinLevel = lv, MinZhi = zhi, Kind = TacticKind.Heal, Target = tg, Power = power, Book = book }; }
        static TacticDef Status(string id, string name, string desc, int cost, int lv, int zhi, TacticTarget tg, Buff buff, int turns = 3, bool book = false)
        { return new TacticDef { Id = id, Name = name, Desc = desc, Cost = cost, MinLevel = lv, MinZhi = zhi, Kind = TacticKind.Status, Target = tg, Buff = buff, Turns = turns, Book = book }; }

        public static readonly List<TacticDef> All = new List<TacticDef>
        {
            // ---- 攻击 ----
            Dmg("huoji",    "火计", "火攻敌军一队",         3,  1,  0,  TacticTarget.OneFoe,  1.0, "fire"),
            Dmg("liehuo",   "烈火", "烈火焚敌军一队",       7,  16, 55, TacticTarget.OneFoe,  1.8, "fire"),
            Dmg("yehuo",    "业火", "业火吞没敌军一队",     16, 40, 70, TacticTarget.OneFoe,  3.0, "fire"),
            Dmg("huolong",  "火龙", "火龙横扫全体敌军",     30, 1,  0,  TacticTarget.AllFoes, 1.6, "fire", true),
            Dmg("luoshi",   "落石", "攻击全体敌军",         8,  1,  0,  TacticTarget.AllFoes, 0.6, null, true),
            Dmg("shanbeng", "山崩", "山崩压向全体敌军",     20, 1,  0,  TacticTarget.AllFoes, 1.1, null, true),
            Dmg("shuiji",   "水计", "决水淹全体敌军",       14, 1,  0,  TacticTarget.AllFoes, 0.9, "water", true),
            Dmg("luolei",   "落雷", "落雷劈敌军一队",       12, 1,  0,  TacticTarget.OneFoe,  2.2, null, true),
            Dmg("fubing",   "伏兵", "伏兵四起，攻击全体敌军", 22, 1, 0, TacticTarget.AllFoes, 1.4, null, true),
            Dmg("tianlei",  "天雷", "天雷击全体敌军",       45, 80, 85, TacticTarget.AllFoes, 1.8),
            // ---- 回复 ----
            Heal("liaobing", "疗兵", "恢复我方一人兵力",     4,  1,  0,  TacticTarget.OneAlly,   1.0),
            Heal("yiliao",   "医疗", "大幅恢复我方一人兵力", 9,  20, 55, TacticTarget.OneAlly,   2.2),
            Heal("shenyi",   "神医", "我方一人兵力几乎回满", 20, 1,  0,  TacticTarget.OneAlly,   5.0, true),
            Heal("guwu",     "鼓舞", "恢复我方全体兵力",     10, 1,  0,  TacticTarget.AllAllies, 0.5, true),
            Heal("jili",     "激励", "大幅恢复我方全体兵力", 24, 50, 70, TacticTarget.AllAllies, 1.2),
            Heal("huitian",  "回天", "我方全体兵力大回复",   50, 85, 85, TacticTarget.AllAllies, 2.5),
            // ---- 控制 ----
            Status("hunluan", "混乱", "敌一人 2 回合不能行动",       6,  10, 45, TacticTarget.OneFoe, Buff.Stun, 2),
            Status("fengce",  "封策", "敌一人 3 回合放不了法术",     6,  24, 60, TacticTarget.OneFoe, Buff.Silence),
            Status("yibing",  "疑兵", "敌一人不敢进攻，每回合有机会挣脱", 9, 48, 70, TacticTarget.OneFoe, Buff.Disarm, 4),
            Status("lijian",  "离间", "敌一人 2 回合攻击自己人",     12, 54, 75, TacticTarget.OneFoe, Buff.Confuse, 2),
            new TacticDef { Id = "cike", Name = "刺客", Desc = "派刺客暗杀敌一人，成败看智力（首领无效）", Cost = 18, MinLevel = 70, MinZhi = 85, Kind = TacticKind.Kill, Target = TacticTarget.OneFoe },
            // ---- 防御 ----
            new TacticDef { Id = "anmin", Name = "安民", Desc = "解除我方全体的不良状态", Cost = 5, MinLevel = 14, MinZhi = 50, Kind = TacticKind.Status, Target = TacticTarget.AllAllies, Cure = true },
            Status("shuimu",   "水幕", "我方全体 3 回合不受火攻",   8,  28, 60, TacticTarget.AllAllies, Buff.FireWard),
            Status("jianshou", "坚守", "我方全体 3 回合受到伤害 ×0.7", 12, 32, 60, TacticTarget.AllAllies, Buff.Guard),
            Status("zhudi",    "筑堤", "我方全体 3 回合不受水攻",   10, 44, 70, TacticTarget.AllAllies, Buff.WaterWard),
            // ---- 增益 ----
            Status("fenzhan", "奋战", "我方一人 3 回合攻击 ×1.5",   8,  1,  0,  TacticTarget.OneAlly,   Buff.Rally, 3, true),
            Status("jifeng",  "疾风", "我方全体 3 回合速度 +30",    10, 36, 60, TacticTarget.AllAllies, Buff.Haste),
            Status("lianji",  "连击", "我方一人 3 回合每回合多打一次", 14, 58, 75, TacticTarget.OneAlly, Buff.Twice),
            Status("qixi",    "奇袭", "我方全体 3 回合抢先出手",    16, 66, 80, TacticTarget.AllAllies, Buff.First),
            // ---- 敌方专用 ----
            new TacticDef { Id = "yaoshu", Name = "妖术", Desc = "黄巾妖术，攻击全体", Cost = 0, MinLevel = 99, Kind = TacticKind.Damage, Target = TacticTarget.AllFoes, Power = 0.45, PlayerUsable = false },
        };

        public static TacticDef Get(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }
    }
}
