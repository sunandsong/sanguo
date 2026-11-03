using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第二章「黄巾之乱」（演义第一、二回）：解青州之围（张宝）→ 太守龚景答谢、简雍来投 → 东去广宗，过桥斩张梁 → 破黄巾大营（张角）。
    /// 等级段 4～10。三张外景的底图还没出，出图任务见 ArtTask/出图任务-第二章地图.md；没图时按地块画色块。
    /// </summary>
    public class Chapter2 : Chapter
    {
        public const int InnCost = 40;

        public readonly MapData Field;     // 青州郊外
        public readonly MapData Town;      // 青州城
        public readonly MapData Inn, Inn2F;
        public readonly MapData Guangzong; // 广宗

        const string F_Siege = "c2_siege";           // 打退了围城的张宝
        const string F_GongJing = "c2_gongjing";     // 龚景谢过、简雍入队
        const string F_ZhangLiang = "c2_zhangliang"; // 桥头斩张梁
        public const string F_Boss = "c2_boss";      // 破大营
        const string F_ChestFieldSW = "c2_chest_field_sw", F_ChestFieldNE = "c2_chest_field_ne", F_ChestTown = "c2_chest_town", F_ChestInn = "c2_chest_inn2f";
        const string F_ChestGzSW = "c2_chest_gz_sw", F_ChestGzE = "c2_chest_gz_e";

        // 青州商店（设计文档 8.3）
        static readonly string[] ShopGoods = { "caoyao", "aiye", "zhuojiu", "langyan", "yanmudan", "nuqiwan", "gangcha", "tongchui", "podao", "tieqiang", "tiejia" };

        public Chapter2(IGameHost host) : base(host)
        {
            Field = BuildField();
            Town = BuildTown();
            var inn = BuildInn("qz_inn", InnCost, () => host.Warp(Town, 15, 4), Chest(F_ChestInn, 3, 3, 0, "zhuojiu", 2));
            Inn = inn[0];
            Inn2F = inn[1];
            Guangzong = BuildGuangzong();
        }

        public override int Number { get { return 2; } }
        public override MapData[] Maps { get { return new[] { Field, Town, Inn, Inn2F, Guangzong }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Town, X = 9, Y = 12 } }; } }

        public override void Begin()
        {
            host.Party.FullRestore();
            host.Warp(Field, 15, 18);
            host.Say(new List<DialogueLine>
            {
                L(null, "却说青州太守龚景被黄巾围困，遣人向幽州求救。"),
                L(null, "刘备率乡勇五百，随校尉邹靖星夜赶往青州。"),
                L("关羽", "兄长，前面就是青州。城下黄巾旗号密布，怕有数万之众。"),
                L("张飞", "管他几万，俺这丈八蛇矛正愁没处使！"),
                L("刘备", "贼众我寡，不可力敌。先沿路剿些散兵，探明虚实，再取城下。"),
            });
        }

        public override void RespawnAfterDefeat()
        {
            host.Party.Gold /= 2;
            host.Party.FullRestore();
            if (host.Flags.Has(F_Siege)) { host.Warp(Town, 15, 4); Say(L(null, "胜败乃兵家常事。刘备一行退回青州城休整……"), L(null, "（金钱减半，兵力已恢复）")); }
            else { host.Warp(Field, 15, 18); Say(L(null, "胜败乃兵家常事。刘备一行退回官道口重整……"), L(null, "（金钱减半，兵力已恢复）")); }
        }

        /// <summary>调试：直接当作围城已解。</summary>
        public override void SkipToField()
        {
            host.Flags.Set(F_Siege);
            host.Party.FullRestore();
            host.Warp(Field, 15, 6);
        }

        // =====================================================================
        // 青州郊外：南边官道进来，北边是青州城，城下黄巾围营；东边大路去广宗
        // =====================================================================
        MapData BuildField()
        {
            var m = new MapData
            {
                Id = "qingzhou_field",
                Name = "青州郊外",
                Art = "qingzhou_field",
                Grade = "#f2f4ea", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09,
                EnemyLevel = 5,   // 第二章等级段 4～10：郊外 4～6 级
                ActorScale = 0.8f,
                Ground = ',',
                EnemyPool = new[] { GameDatabase.HuangJinBing, GameDatabase.HuangJinBing, GameDatabase.HuangJinLiShi, GameDatabase.HuangJinShuShi, GameDatabase.HuangJinTouMu },
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMtMMMMMMMMMMMMMM",
                    "M,,,,T,,,,,,,,,=,,,,,,,T,,,,,M",
                    "M,T,,,,,,,,,,,,=,,,,,,,,,,T,,M",
                    "M,,,,,,,,,CC,,,=,,,,CC,,,,,,,M",
                    "M,,,T,,,,,CC,,,=,,,,CC,,T,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,MM,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,MM,,T,,,,,,,=,,,,,,,,,,T,,M",
                    "M,,,,,,,,,,,,,,===============",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,T,,,~~~,,,,,,=,,,,,T,,,,,,,M",
                    "M,,,,~~~~w,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,w~w,,,,,,=,,,,,,,,,MM,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,MM,,M",
                    "M,,,,T,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,T,,=,,T,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,T,,,,M",
                    "M,T,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "MMMMMMMMMMMMMMM=MMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "qingzhou_field";
            m.Emitters.Add(new MapEmitter("fire", 10.6f, 4.6f));
            m.Emitters.Add(new MapEmitter("fire", 20.6f, 4.6f));
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("butterfly", 2));
            m.Critters.Add(new CritterSpawn("dragonfly", 2));
            m.Npcs.Add(Chest(F_ChestFieldSW, 3, 16, 0, "aiye", 2));
            m.Npcs.Add(Chest(F_ChestFieldNE, 27, 2, 100));

            var flags = host.Flags;

            // 围城的黄巾：张宝带两个兵堵在城门口的路上，搭话就开打
            System.Action siege = SiegeFight;
            System.Func<bool> besieged = () => !flags.Has(F_Siege);
            m.Npcs.Add(Person("zhangbao_npc", "张宝", "zhangbao", "fuchen", 15, 1, siege, besieged));
            m.Npcs.Add(Person("siege_a", "黄巾兵", "huangjin", "cha", 14, 2, siege, besieged));
            m.Npcs.Add(Person("siege_b", "黄巾兵", "huangjin", "cha", 16, 2, siege, besieged));
            m.Npcs.Add(Person("zoujing", "邹靖", "shouwei", "qiang", 13, 17, () =>
            {
                if (!flags.Has(F_Siege)) Say(L("邹靖", "玄德，贼势甚大，不可轻进。城下那支是张角之弟张宝的人马，会些妖法。"), L("邹靖", "你带人先去，我随后接应。"));
                else Say(L("邹靖", "青州已解围，我这就回幽州复命。玄德若要立功，往东去广宗，卢中郎将正与张角相持。"));
            }));
            m.Npcs.Add(Walker("qz_woodcutter", "樵夫", "cunmin_nan", "chutou", 1.1f, new[] { 5, 13, 8, 13, 8, 15, 5, 15 }, new[] { 2, 17 },
                "城被围了半个月，城里怕是断粮了。", "贼人的营盘就扎在城门口，每日叫骂。", "东边那条大路通广宗，听说那边打得更凶。"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't') { host.Warp(Town, 9, 12); return true; }
                if (x == 29 && y == 8) { host.Warp(Guangzong, 1, 10); return true; }   // 东边大路 → 广宗
                if (x == 15 && y == 19) { Say(L("关羽", "兄长，回涿郡的路远，青州的事还没了结。")); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;
                Say(L(null, flags.Has(F_Siege) ? "黄巾营盘已经烧成了白地。" : "黄巾的营盘，帐篷里全是人。"));
                return true;
            };
            return m;
        }

        void SiegeFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("张宝", "地公将军张宝在此！区区几百乡勇，也敢来撩虎须？"),
                L("刘备", "反国逆贼，何不早降！"),
                L("张宝", "哼，看我妖法！"),
            }, () => host.StartBattle(GameDatabase.Chapter2Siege(), true, () =>
            {
                host.Flags.Set(F_Siege);
                host.Say(new List<DialogueLine>
                {
                    L(null, "张宝妖法被破，黄巾大乱。龚景率城中兵马杀出，内外夹攻，贼众溃散。"),
                    L(null, "张宝引残部往东逃去。"),
                    L("张飞", "哈哈，贼人的妖法也不过如此！"),
                    L("关羽", "兄长，太守龚景在城中相候。"),
                });
            }));
        }

        // =====================================================================
        // 青州城：太守府、客栈、商铺
        // =====================================================================
        MapData BuildTown()
        {
            var m = new MapData
            {
                Id = "qingzhou",
                Name = "青州",
                Art = "qingzhou_ground",
                Grade = "#fff3e4", Weather = "leaf",
                EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTT",
                    "TTHHHHHTTTTTTTHHHHTT",
                    "T.HHHHH......HHHH..T",
                    "T.HHHHH......HHHH..T",
                    "T...=.........==...T",
                    "T...==========.....T",
                    "T........=.........T",
                    "T.HHH....=....HHHH.T",
                    "T.HHH....=....HHHH.T",
                    "T.HHH....=....HHHH.T",
                    "T........=.........T",
                    "TTTTTTTT.=.TTTTTTTTT",
                    "TTTTTTTTT=TTTTTTTTTT",
                    "TTTTTTTTTggTTTTTTTTT",
                }
            };
            m.DecorFile = "qingzhou";
            m.Emitters.Add(new MapEmitter("smoke", 3.3f, 6.95f));
            m.Critters.Add(new CritterSpawn("chicken", 2));
            m.Critters.Add(new CritterSpawn("sparrow", 3));
            m.Critters.Add(new CritterSpawn("butterfly", 2));

            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestTown, 18, 2, 80));
            m.Npcs.Add(Person("qz_merchant", "青州商人", "shangren", null, 16, 10, () =>
                host.Say(new List<DialogueLine> { L("青州商人", "围城的日子可算熬过去了。刀枪甲胄、草药浊酒，诸位随便看。") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Person("qz_soldier", "青州兵", "shouwei", "qiang", 8, 11, () =>
                Say(L("青州兵", flags.Has(F_GongJing) ? "张角兄弟屯兵广宗，出城往东走大路就是。" : "太守在府前候着诸位呢，就在西北边。"))));
            m.Npcs.Add(Walker("qz_citizen", "百姓", "cunmin_nv", null, 1.0f, new[] { 11, 6, 12, 6, 12, 10, 11, 10 }, new[] { 3, 10 },
                "多亏诸位将军，城里总算能开城门了。", "围城这些天，米价涨了十倍。", "太守府里来了位姓简的先生，说是找刘将军的。"));

            m.Npcs.Add(new Npc
            {
                Id = "gongjing", Name = "龚景", Glyph = "龚", X = 6, Y = 4,
                Talk = () =>
                {
                    if (flags.Has(F_GongJing))
                    {
                        Say(L("龚景", "张角兄弟屯兵广宗，卢中郎将正与之相持。玄德若去，可解大难。"),
                            L("龚景", "出城往东，沿大路走到头便是。"));
                        return;
                    }
                    flags.Set(F_GongJing);
                    host.Party.Gold += 300;
                    for (int i = 0; i < 3 && host.Party.CanHold("aiye"); i++) host.Party.Items.Add("aiye");
                    host.Party.Add(new General(GameDatabase.JianYong));
                    host.Say(new List<DialogueLine>
                    {
                        L("龚景", "玄德公解青州之围，老夫代满城百姓谢过！些许犒军之物，万勿推辞。"),
                        L(null, "获得金钱 300、艾叶 ×3！"),
                        L("？？？", "玄德兄！"),
                        L(null, "府中走出一名青衫文士，笑吟吟地拱手。"),
                        L("简雍", "涿郡简雍，字宪和。闻兄长起兵讨贼，特来相投。军中文书钱粮，就交给小弟吧。"),
                        L("刘备", "宪和来得正好！"),
                        L(null, "简雍加入队伍！"),
                        L("龚景", "眼下张角亲率主力屯兵广宗，卢植卢中郎将与之相持数月，正缺人手。"),
                        L("龚景", "玄德曾师事卢公，何不前去相助？出城往东，沿大路走到头便是广宗。"),
                    });
                }
            });

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 'g') { host.Warp(Field, 15, 1); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if ((x == 15 || x == 16) && y == 3) { host.Warp(Inn, 4, 5); return true; }   // 客栈门（门口 (15,4)/(16,4) 往上撞）
                if (x == 4 && y == 3) { Say(L(null, "太守府。门口的卫兵说，太守正在府前。")); return true; }
                return false;
            };
            return m;
        }

        // =====================================================================
        // 广宗：西边进来，河上一座桥（张梁把守），河北岸黄巾大营（张角）
        // =====================================================================
        MapData BuildGuangzong()
        {
            var m = new MapData
            {
                Id = "guangzong",
                Name = "广宗",
                Art = "guangzong",
                Grade = "#eef0f6", Light = "#f0ece6", Weather = "leaf",
                EncounterRate = 0.1,
                EnemyLevel = 8,   // 7～9 级
                ActorScale = 0.8f,
                Ground = ',',
                EnemyPool = new[] { GameDatabase.HuangJinBing, GameDatabase.HuangJinLiShi, GameDatabase.HuangJinLiShi, GameDatabase.HuangJinShuShi, GameDatabase.HuangJinTouMu, GameDatabase.HuangJinTouMu },
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                    "M,,T,,,,,,,,,,,,,,,,,,,,CCCCC,,M",
                    "M,,,,,,,,,T,,,,,,,,,,,,,CCCCC,,M",
                    "M,T,,,,,,,,,,,,,,,,,,,,,CCCCC,,M",
                    "M,,,,,,,,,,,,,,T,,,,,,,,,,=,,,,M",
                    "M,,,,,T,,,,,,,,,,,,,,,,,,,=,,T,M",
                    "M,,,,,,,,,,,,,,,,,,,,,====,,,,,M",
                    "M,,,,,,,T,,,,,,,,,,,,,=,,,,,,,,M",
                    "M~~~~~~~~~~~~~~~~~~~~~=~~~~~~~~M",
                    "M~~~~~~~~~~~~~~~~~~~~~=~~~~~~~~M",
                    "=======================,,,,,,,,M",
                    "M,,,,,T,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,T,,,,,,,,,,,,,T,,,,M",
                    "M,,MM,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,MM,,,,,,T,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,,,,,T,,,,,,,,,,,M",
                    "M,T,,,,,,,,,,,,,,,,,,,,,,,T,,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,,,T,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,T,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "guangzong";
            m.Emitters.Add(new MapEmitter("fire", 26.1f, 3.1f));
            m.Critters.Add(new CritterSpawn("fish", 5));
            m.Critters.Add(new CritterSpawn("duck", 2));
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Npcs.Add(Chest(F_ChestGzSW, 2, 19, 0, "tiejia"));
            m.Npcs.Add(Chest(F_ChestGzE, 29, 15, 0, "zhuojiu", 2));

            var flags = host.Flags;

            m.Npcs.Add(Person("zhangliang_npc", "张梁", "zhangliang", "langya", 22, 10, BridgeFight, () => !flags.Has(F_ZhangLiang)));
            m.Npcs.Add(Person("lu_soldier", "官军士卒", "shouwei", "qiang", 3, 11, () =>
                Say(L("官军士卒", "卢中郎将的大营在西边山后。贼军主力扎在河北岸，这桥是唯一的过河处。"),
                    L("官军士卒", flags.Has(F_ZhangLiang) ? "张梁已除，大营里就剩张角那妖道了。" : "守桥的是张角三弟张梁，有万夫不当之勇，诸位小心。"))));
            m.Npcs.Add(Walker("gz_refugee", "难民", "cunmin_nan", null, 1.2f, new[] { 8, 16, 12, 16, 12, 18, 8, 18 }, new[] { 4, 19 },
                "家让黄巾烧了，只能往南边逃。", "河北岸全是黄巾的营帐，一眼望不到头。", "听说张角病了，营里天天做法事。"));

            m.OnStep = (x, y) =>
            {
                if (x == 0 && y == 10) { host.Warp(Field, 28, 8); return true; }   // 西边回青州郊外
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;
                if (flags.Has(F_Boss)) { Say(L(null, "黄巾大营已破，只剩残旗断戟。")); return true; }
                BossFight();
                return true;
            };
            return m;
        }

        void BridgeFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("张梁", "人公将军张梁在此！过桥者，死！"),
                L("张飞", "俺倒要看看谁死！"),
            }, () => host.StartBattle(GameDatabase.Chapter2Bridge(), true, () =>
            {
                host.Flags.Set(F_ZhangLiang);
                host.Say(new List<DialogueLine>
                {
                    L(null, "张飞与张梁大战三十余合，一矛刺中张梁肩头。张梁弃桥而走，退入大营。"),
                    L("关羽", "过桥便是黄巾大营。张角就在营中，兄长，一鼓作气！"),
                });
            }));
        }

        void BossFight()
        {
            host.Say(new List<DialogueLine>
            {
                L(null, "营门大开，一人披发仗剑，立于高台之上。"),
                L("张角", "苍天已死，黄天当立！岁在甲子，天下大吉！"),
                L("张角", "尔等汉室鹰犬，也配来见大贤良师？"),
                L("刘备", "妖言惑众，荼毒生灵。今日便为天下除此大害！"),
            }, () => host.StartBattle(GameDatabase.Chapter2Boss(), true, () =>
            {
                host.Flags.Set(F_Boss);
                host.Say(new List<DialogueLine>
                {
                    L(null, "张角法破兵败，仓皇遁走。不久，病死于营中。"),
                    L(null, "黄巾主力既灭，各路义军乘胜追击，黄巾之乱渐渐平息。"),
                    L(null, "营中搜出张角所传妖书《太平要术》。"),
                    L("关羽", "兄长，朝廷论功行赏，咱们该去洛阳了。"),
                    L("张飞", "哼，就怕那些当官的不认账。"),
                    L("刘备", "尽人事，听天命。走吧。"),
                }, () => host.ChapterClear("第二章 黄巾之乱 · 完", "第三章：讨伐董卓"));
            }));
        }
    }
}
