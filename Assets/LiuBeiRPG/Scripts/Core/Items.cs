using System.Collections.Generic;

namespace LiuBeiRPG.Core
{
    // 装备六格的顺序就是人物页上的顺序：武器 头盔 防具 / 鞋子 坐骑 饰品（Weapon 往后都是装备）
    // Material = 材料杂物：打怪必掉，不能用，只能卖钱；Tool = 专用道具，效果看 Effect
    public enum ItemKind { Book, Heal, Revive, Wine, Material, Tool, Weapon, Helmet, Armor, Boots, Horse, Trinket }

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
        public string Effect;    // 道具：smoke 烟幕（地图）/ fan 飞回城（地图）/ fury 怒气（战斗，一人）/ flee 狼烟（战斗，全军撤）
        public bool Magic;       // 法器：占武器格，Power 加的是智力

        public bool Equip { get { return Kind >= ItemKind.Weapon; } }
        public bool InBattle { get { return Kind == ItemKind.Heal || Kind == ItemKind.Revive || Kind == ItemKind.Wine || Effect == "fury" || Effect == "flee"; } }
        public int Slot { get { return Kind - ItemKind.Weapon; } }   // 装备在第几格
    }

    /// <summary>数值见 Docs/设计文档.md 第 5、7 节。图标：兵器用引擎贴图，其余 Art/item_&lt;Id&gt;。</summary>
    public static class ItemDb
    {
        public static readonly List<ItemDef> All = new List<ItemDef>
        {
            new ItemDef { Id = "liutao", Name = "六韬", Kind = ItemKind.Book, Desc = "太公望所传兵法。研读后全军习得「落石」。", Teaches = "luoshi" },
            new ItemDef { Id = "wuzi",   Name = "吴子", Kind = ItemKind.Book, Desc = "吴起兵法，重在励士。研读后全军习得「鼓舞」。", Teaches = "guwu" },

            new ItemDef { Id = "taipingyaoshu", Name = "太平要术", Kind = ItemKind.Book, Desc = "张角所传的天书。研读后全军习得「落雷」。", Teaches = "luolei" },

            new ItemDef { Id = "caoyao",  Name = "草药", Kind = ItemKind.Heal, Power = 150, Price = 10, Desc = "山野常见的止血草。一人兵力 +150。" },
            new ItemDef { Id = "aiye",    Name = "艾叶", Kind = ItemKind.Heal, Power = 400, Price = 60, Desc = "晒干的艾草，灸伤止痛。一人兵力 +400。" },
            new ItemDef { Id = "huanhundan", Name = "还魂丹", Kind = ItemKind.Revive, Price = 1500, Desc = "起死回生的灵丹。救回败走的一人，兵力回到一半。" },
            new ItemDef { Id = "zhuojiu", Name = "浊酒", Kind = ItemKind.Wine, Power = 10,  Price = 30, Desc = "乡间自酿的浊酒，饮之提神。法力 +10。" },

            new ItemDef { Id = "huangjinbu", Name = "黄巾", Kind = ItemKind.Material, Price = 4, Desc = "从黄巾贼头上扯下的黄布巾。县里差役按条收，十条换赏；商店也收。" },

            // 专用道具（吞食天地：烟幕弹、飞鸟扇、怒气丸；狼烟是设计文档 7.4 的必撤道具）
            new ItemDef { Id = "langyan",     Name = "狼烟",   Kind = ItemKind.Tool, Effect = "flee",  Price = 100, Desc = "点起狼烟，全军必定撤退。首领战无效。" },
            new ItemDef { Id = "yanmudan",    Name = "烟幕弹", Kind = ItemKind.Tool, Effect = "smoke", Price = 200, Desc = "撒下烟幕，接下来走 60 步不会遇敌。" },
            new ItemDef { Id = "feiniaoshan", Name = "飞鸟扇", Kind = ItemKind.Tool, Effect = "fan",   Price = 450, Desc = "一扇生风，飞回去过的城池。" },
            new ItemDef { Id = "nuqiwan",     Name = "怒气丸", Kind = ItemKind.Tool, Effect = "fury",  Price = 300, Desc = "一人服下，这一仗攻击伤害翻倍，和奋战叠加。" },

            new ItemDef { Id = "qimeigun",    Name = "齐眉棍", Kind = ItemKind.Weapon, Sprite = "gun",    Power = 6, Price = 40,  Desc = "齐眉高的硬木棍。武力 +6。" },
            new ItemDef { Id = "tiejian",     Name = "铁剑",   Kind = ItemKind.Weapon, Sprite = "jian",   Power = 8, Price = 100, Desc = "寻常铁剑。武力 +8。" },
            new ItemDef { Id = "huanshoudao", Name = "环首刀", Kind = ItemKind.Weapon, Sprite = "dao",    Power = 10, Price = 120, Desc = "汉军制式长刀。武力 +10。" },
            new ItemDef { Id = "changqiang",  Name = "长枪",   Kind = ItemKind.Weapon, Sprite = "qiang",  Power = 10, Price = 120, Desc = "白蜡杆长枪。武力 +10。" },
            new ItemDef { Id = "shoufu",      Name = "手斧",   Kind = ItemKind.Weapon, Sprite = "shoufu", Power = 12, Price = 150, Desc = "短柄阔刃斧。武力 +12。" },
            // 第二章（青州）
            new ItemDef { Id = "gangcha",   Name = "钢叉",   Kind = ItemKind.Weapon, Sprite = "cha",    Power = 7,  Price = 300, Desc = "精钢打的三股叉。武力 +7。" },
            new ItemDef { Id = "tongchui",  Name = "铜锤",   Kind = ItemKind.Weapon, Sprite = "chui",   Power = 8,  Price = 400, Desc = "沉甸甸的铜锤。武力 +8。" },
            new ItemDef { Id = "podao",     Name = "朴刀",   Kind = ItemKind.Weapon, Sprite = "podao",  Power = 10, Price = 600, Desc = "长柄阔刃的朴刀。武力 +10。" },
            new ItemDef { Id = "tieqiang",  Name = "铁枪",   Kind = ItemKind.Weapon, Sprite = "qiang",  Power = 12, Price = 750, Desc = "通体精铁的长枪。武力 +12。" },
            new ItemDef { Id = "langyabang", Name = "狼牙棒", Kind = ItemKind.Weapon, Sprite = "langya", Power = 16, Price = 1500, Desc = "张梁的铁棒，布满倒刺。武力 +16。" },
            new ItemDef { Id = "jiujiezhang", Name = "九节杖", Kind = ItemKind.Weapon, Sprite = "jiujie", Power = 15, Magic = true, Desc = "张宝作法用的九节铜杖。法器，智力 +15。" },
            // ponytail: 名器先固定 +5，设计文档里的随等级成长（5 + 0.5×(Lv−1)）等第 2 章再做
            new ItemDef { Id = "shuanggu", Name = "双股剑",     Kind = ItemKind.Weapon, Sprite = "shuanggu", Power = 5, Owner = "liubei",   Desc = "张世平所赠镔铁打造。刘备专属。武力 +5。" },
            new ItemDef { Id = "yanyue",   Name = "青龙偃月刀", Kind = ItemKind.Weapon, Sprite = "yanyue",   Power = 5, Owner = "guanyu",   Desc = "张世平所赠镔铁打造。关羽专属。武力 +5。" },
            new ItemDef { Id = "shemao",   Name = "丈八蛇矛",   Kind = ItemKind.Weapon, Sprite = "shemao",   Power = 5, Owner = "zhangfei", Desc = "张世平所赠镔铁打造。张飞专属。武力 +5。" },

            new ItemDef { Id = "pikui", Name = "皮盔", Kind = ItemKind.Helmet, Power = 8, Price = 80, Desc = "牛皮缝的软盔，护住头脸。防御力 +8。" },
            new ItemDef { Id = "pijia", Name = "皮甲", Kind = ItemKind.Armor, Power = 15, Price = 150, Desc = "鞣制牛皮缝成的甲。防御力 +15。" },
            new ItemDef { Id = "tiejia", Name = "铁甲", Kind = ItemKind.Armor, Power = 30, Price = 900, Desc = "铁片缀成的札甲。防御力 +30。" },
            new ItemDef { Id = "buxue", Name = "布靴", Kind = ItemKind.Boots, Power = 2, Price = 60, Desc = "千层底的布靴，走得轻快。速度 +2。" },
            new ItemDef { Id = "numa",  Name = "驽马", Kind = ItemKind.Horse, Power = 3, Price = 200, Desc = "脚力平平的乘马。速度 +3。" },
            new ItemDef { Id = "yupei", Name = "玉佩", Kind = ItemKind.Trinket, Power = 5, Price = 150, Desc = "温润的青玉佩，佩之静心。智力 +5。" },

            // ---------- 第三～七章（设计文档 5、7 节）。图标见 ArtSrc/物品图标提示词.md 第八～十张；兵器用引擎贴图 ----------
            new ItemDef { Id = "jinchuangyao", Name = "金创药", Kind = ItemKind.Heal, Power = 1000, Price = 120, Desc = "军中常备的伤药，敷上立刻止血。一人兵力 +1000。" },
            new ItemDef { Id = "zhixuesan",    Name = "止血散", Kind = ItemKind.Heal, Power = 2500, Price = 400, Desc = "名医调配的药散。一人兵力 +2500。" },
            new ItemDef { Id = "qingjiu",      Name = "清酒",   Kind = ItemKind.Wine, Power = 30,   Price = 120, Desc = "洛阳酿的清酒，入口绵软。法力 +30。" },
            new ItemDef { Id = "pojia",        Name = "破甲片", Kind = ItemKind.Material, Price = 10, Desc = "从官军身上扯下来的甲片，卖给铁匠能换几个钱。" },
            new ItemDef { Id = "sunzibingfa",  Name = "孙子兵法", Kind = ItemKind.Book, Teaches = "shuiji", Desc = "兵圣孙武所著。研读后全军习得「水计」。" },
            new ItemDef { Id = "sanlue",       Name = "三略",   Kind = ItemKind.Book, Teaches = "shanbeng", Desc = "黄石公所传兵书。研读后全军习得「山崩」。" },
            // 兵器
            new ItemDef { Id = "changji",      Name = "长戟",   Kind = ItemKind.Weapon, Sprite = "ji",     Power = 9,  Price = 350,  Desc = "枪头旁伸出一道横刃，能刺能钩。" },
            new ItemDef { Id = "dakandao",     Name = "大砍刀", Kind = ItemKind.Weapon, Sprite = "podao",  Power = 18, Price = 1500, Desc = "华雄用过的宽背大刀，分量极足。" },
            new ItemDef { Id = "gangbian",     Name = "钢鞭",   Kind = ItemKind.Weapon, Sprite = "bian",   Power = 12, Price = 600,  Desc = "一节节的钢鞭，专破甲胄。" },
            new ItemDef { Id = "tiejian_bian", Name = "铁锏",   Kind = ItemKind.Weapon, Sprite = "tiejian", Power = 12, Price = 700, Desc = "四棱铁锏，沉重无锋，砸在甲上一样要命。" },
            new ItemDef { Id = "qingfengjian", Name = "青锋剑", Kind = ItemKind.Weapon, Sprite = "jian",   Power = 14, Price = 900,  Desc = "剑身泛着青光的好剑。" },
            new ItemDef { Id = "fuchen",       Name = "拂尘",   Kind = ItemKind.Weapon, Sprite = "fuchen", Power = 6,  Price = 500,  Magic = true, Desc = "道士拿的拂尘，法器。智力 +6。" },
            new ItemDef { Id = "liangyinqiang", Name = "亮银枪", Kind = ItemKind.Weapon, Sprite = "qiang", Power = 24, Price = 2500, Desc = "枪杆缠银丝，枪头亮得晃眼。" },
            new ItemDef { Id = "sanjiandao",   Name = "三尖刀", Kind = ItemKind.Weapon, Sprite = "cha",    Power = 30, Desc = "纪灵的三尖两刃刀，重五十斤。" },
            new ItemDef { Id = "zhanchui",     Name = "战锤",   Kind = ItemKind.Weapon, Sprite = "zhanchui", Power = 36, Desc = "许褚的大铁锤，一锤能砸碎石磨。" },
            new ItemDef { Id = "shuangtieji",  Name = "双铁戟", Kind = ItemKind.Weapon, Sprite = "ji",     Power = 38, Desc = "典韦的双戟，一支就有四十斤。" },
            new ItemDef { Id = "fangtianhuaji", Name = "方天画戟", Kind = ItemKind.Weapon, Sprite = "huaji", Power = 60, Desc = "吕布的方天画戟，天下无双。" },
            new ItemDef { Id = "yajiaoqiang",  Name = "涯角枪", Kind = ItemKind.Weapon, Sprite = "qiang",  Power = 46, Owner = "zhaoyun", Desc = "赵云的长枪，取「海角天涯无对」之意。赵云专属。" },
            new ItemDef { Id = "yushan",       Name = "羽扇",   Kind = ItemKind.Weapon, Sprite = "yushan", Power = 25, Magic = true, Owner = "zhugeliang", Desc = "诸葛亮的白羽扇，法器。诸葛亮专属。智力 +25。" },
            // 防具、坐骑
            new ItemDef { Id = "suozijia",     Name = "锁子甲", Kind = ItemKind.Armor, Power = 48,  Price = 1800, Desc = "铁环套成的软甲，刀砍不透。" },
            new ItemDef { Id = "yulinjia",     Name = "鱼鳞甲", Kind = ItemKind.Armor, Power = 66,  Price = 4500, Desc = "甲片像鱼鳞一样层层叠叠，将官才穿得起。" },
            new ItemDef { Id = "shoumiankai",  Name = "兽面吞头铠", Kind = ItemKind.Armor, Power = 126, Desc = "吕布的铠甲，肩上两只兽头吞口，威风无比。" },
            new ItemDef { Id = "baguadaopao",  Name = "八卦道袍", Kind = ItemKind.Armor, Power = 36, Owner = "zhugeliang", Desc = "诸葛亮的鹤氅道袍，绣着八卦。诸葛亮专属。" },
            new ItemDef { Id = "baima",        Name = "白马",   Kind = ItemKind.Horse, Power = 8,  Desc = "公孙瓒白马义从的坐骑，通身雪白。" },
            new ItemDef { Id = "junma",        Name = "骏马",   Kind = ItemKind.Horse, Power = 10, Price = 1200, Desc = "脚力不错的好马。" },
            new ItemDef { Id = "chitu",        Name = "赤兔",   Kind = ItemKind.Horse, Power = 30, Owner = "guanyu", Desc = "吕布的赤兔马，日行千里。曹操赠与关羽。关羽专属。" },
            new ItemDef { Id = "dilu",         Name = "的卢",   Kind = ItemKind.Horse, Power = 20, Owner = "liubei", Desc = "眼下有泪槽的名马，檀溪一跃三丈。刘备专属。" },
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
