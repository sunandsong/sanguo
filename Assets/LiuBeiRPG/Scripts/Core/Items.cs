using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    // 装备六格的顺序就是人物页上的顺序：武器 头盔 防具 / 鞋子 坐骑 饰品（Weapon 往后都是装备）
    // Material = 材料杂物：打怪必掉，不能用，只能卖钱
    public enum ItemKind { Book, Heal, Wine, Material, Weapon, Helmet, Armor, Boots, Horse, Trinket }

    public class ItemDef
    {
        public string Id;
        public string Name;
        public string Desc;
        public ItemKind Kind;
        public int Price;        // 0 = 不卖（剧情给的），也不能卖掉
        public int Power;        // 回兵力 / 回法力 / 装备加的数值（武器武力，头盔防具防御力，鞋子坐骑速度，饰品智力）
        public string Teaches;   // 兵书：读了全军学会的法术 Id
        public string Sprite;    // 兵器：引擎 weapons/ 下的贴图名
        public string Owner;     // 专属兵器：只有这个武将能拿

        public bool Equip { get { return Kind >= ItemKind.Weapon; } }
        public int Slot { get { return Kind - ItemKind.Weapon; } }   // 装备在第几格
    }

    /// <summary>数值见 Docs/设计文档.md 第 5、7 节。图标：兵器用引擎贴图，其余 Art/item_&lt;Id&gt;。</summary>
    public static class ItemDb
    {
        public static readonly List<ItemDef> All = new List<ItemDef>
        {
            new ItemDef { Id = "liutao", Name = "六韬", Kind = ItemKind.Book, Desc = "太公望所传兵法。研读后全军习得「落石」。", Teaches = "luoshi" },
            new ItemDef { Id = "wuzi",   Name = "吴子", Kind = ItemKind.Book, Desc = "吴起兵法，重在励士。研读后全军习得「鼓舞」。", Teaches = "guwu" },

            new ItemDef { Id = "caoyao",  Name = "草药", Kind = ItemKind.Heal, Power = 150, Price = 10, Desc = "山野常见的止血草。一人兵力 +150。" },
            new ItemDef { Id = "zhuojiu", Name = "浊酒", Kind = ItemKind.Wine, Power = 10,  Price = 30, Desc = "乡间自酿的浊酒，饮之提神。法力 +10。" },

            new ItemDef { Id = "huangjinbu", Name = "黄巾", Kind = ItemKind.Material, Price = 4, Desc = "从黄巾贼头上扯下的黄布巾。没什么用，商店收。" },

            new ItemDef { Id = "qimeigun",    Name = "齐眉棍", Kind = ItemKind.Weapon, Sprite = "gun",    Power = 2, Price = 40,  Desc = "齐眉高的硬木棍。武力 +2。" },
            new ItemDef { Id = "tiejian",     Name = "铁剑",   Kind = ItemKind.Weapon, Sprite = "jian",   Power = 4, Price = 100, Desc = "寻常铁剑。武力 +4。" },
            new ItemDef { Id = "huanshoudao", Name = "环首刀", Kind = ItemKind.Weapon, Sprite = "dao",    Power = 5, Price = 120, Desc = "汉军制式长刀。武力 +5。" },
            new ItemDef { Id = "changqiang",  Name = "长枪",   Kind = ItemKind.Weapon, Sprite = "qiang",  Power = 5, Price = 120, Desc = "白蜡杆长枪。武力 +5。" },
            new ItemDef { Id = "shoufu",      Name = "手斧",   Kind = ItemKind.Weapon, Sprite = "shoufu", Power = 6, Price = 150, Desc = "短柄阔刃斧。武力 +6。" },
            // ponytail: 名器先固定 +5，设计文档里的随等级成长（5 + 0.5×(Lv−1)）等第 2 章再做
            new ItemDef { Id = "shuanggu", Name = "双股剑",     Kind = ItemKind.Weapon, Sprite = "shuanggu", Power = 5, Owner = "liubei",   Desc = "张世平所赠镔铁打造。刘备专属。武力 +5。" },
            new ItemDef { Id = "yanyue",   Name = "青龙偃月刀", Kind = ItemKind.Weapon, Sprite = "yanyue",   Power = 5, Owner = "guanyu",   Desc = "张世平所赠镔铁打造。关羽专属。武力 +5。" },
            new ItemDef { Id = "shemao",   Name = "丈八蛇矛",   Kind = ItemKind.Weapon, Sprite = "shemao",   Power = 5, Owner = "zhangfei", Desc = "张世平所赠镔铁打造。张飞专属。武力 +5。" },

            new ItemDef { Id = "pikui", Name = "皮盔", Kind = ItemKind.Helmet, Power = 8, Price = 80, Desc = "牛皮缝的软盔，护住头脸。防御力 +8。" },
            new ItemDef { Id = "pijia", Name = "皮甲", Kind = ItemKind.Armor, Power = 15, Price = 150, Desc = "鞣制牛皮缝成的甲。防御力 +15。" },
            new ItemDef { Id = "buxue", Name = "布靴", Kind = ItemKind.Boots, Power = 2, Price = 60, Desc = "千层底的布靴，走得轻快。速度 +2。" },
            new ItemDef { Id = "numa",  Name = "驽马", Kind = ItemKind.Horse, Power = 3, Price = 200, Desc = "脚力平平的乘马。速度 +3。" },
            new ItemDef { Id = "yupei", Name = "玉佩", Kind = ItemKind.Trinket, Power = 5, Price = 150, Desc = "温润的青玉佩，佩之静心。智力 +5。" },
        };

        public static ItemDef Get(string id)
        {
            if (id == null) return null;
            foreach (var i in All) if (i.Id == id) return i;
            return null;
        }

        /// <summary>这个武将的专属兵器（刘关张），没有返回 null。</summary>
        public static ItemDef Signature(string generalId)
        {
            foreach (var i in All) if (i.Owner == generalId) return i;
            return null;
        }
    }
}
