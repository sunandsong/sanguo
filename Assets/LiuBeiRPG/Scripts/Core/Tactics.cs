using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    public enum TacticKind { Damage, Heal }

    /// <summary>目标是相对施放者而言的：Foe = 对方，Ally = 己方。</summary>
    public enum TacticTarget { OneFoe, AllFoes, OneAlly, AllAllies }

    public class TacticDef
    {
        public string Id;
        public string Name;
        public string Desc;
        public int Cost;
        public int MinLevel;
        public TacticKind Kind;
        public TacticTarget Target;
        public double Power;
        public bool PlayerUsable = true;
        public bool Book;   // 只能靠兵书学（见 ItemDb），升级不会自动学

        public bool TargetsFoe { get { return Target == TacticTarget.OneFoe || Target == TacticTarget.AllFoes; } }
        public bool TargetsAll { get { return Target == TacticTarget.AllFoes || Target == TacticTarget.AllAllies; } }
    }

    public static class TacticDb
    {
        public static readonly List<TacticDef> All = new List<TacticDef>
        {
            new TacticDef { Id = "huoji",  Name = "火计", Desc = "火攻敌军一队",       Cost = 3, MinLevel = 1, Kind = TacticKind.Damage, Target = TacticTarget.OneFoe,   Power = 1.0 },
            new TacticDef { Id = "liaobing", Name = "疗兵", Desc = "恢复我方一人兵力", Cost = 4, MinLevel = 1, Kind = TacticKind.Heal,   Target = TacticTarget.OneAlly,  Power = 1.0 },
            new TacticDef { Id = "luoshi", Name = "落石", Desc = "攻击全体敌军",       Cost = 8, MinLevel = 1, Book = true, Kind = TacticKind.Damage, Target = TacticTarget.AllFoes,  Power = 0.6 },
            new TacticDef { Id = "guwu",   Name = "鼓舞", Desc = "恢复我方全体兵力",   Cost = 10, MinLevel = 1, Book = true, Kind = TacticKind.Heal,  Target = TacticTarget.AllAllies, Power = 0.5 },
            // 敌方专用
            new TacticDef { Id = "yaoshu", Name = "妖术", Desc = "黄巾妖术，攻击全体", Cost = 0, MinLevel = 99, Kind = TacticKind.Damage, Target = TacticTarget.AllFoes, Power = 0.45, PlayerUsable = false },
        };

        public static TacticDef Get(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }
    }
}
