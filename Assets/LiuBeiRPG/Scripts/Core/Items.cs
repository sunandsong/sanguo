using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    public class ItemDef
    {
        public string Id;
        public string Name;
        public string Desc;
        public string Teaches;   // 兵书：读了全军学会的策略 Id
    }

    public static class ItemDb
    {
        public static readonly List<ItemDef> All = new List<ItemDef>
        {
            new ItemDef { Id = "liutao", Name = "六韬", Desc = "太公望所传兵法。研读后全军习得「落石」。", Teaches = "luoshi" },
            new ItemDef { Id = "wuzi",   Name = "吴子", Desc = "吴起兵法，重在励士。研读后全军习得「鼓舞」。", Teaches = "guwu" },
        };

        public static ItemDef Get(string id)
        {
            foreach (var i in All) if (i.Id == id) return i;
            return null;
        }
    }
}
