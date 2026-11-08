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
        public const int InnCost = 60;

        public readonly MapData Field;     // 青州郊外
        public readonly MapData Town;      // 青州城
        public readonly MapData Inn, Inn2F;
        public readonly MapData Fu;        // 太守府正堂
        public readonly MapData Guangzong; // 广宗

        const string F_Siege = "c2_siege";           // 打退了围城的张宝
        const string F_GongJing = "c2_gongjing";     // 龚景谢过、简雍入队
        const string F_ZhangLiang = "c2_zhangliang"; // 桥头斩张梁
        public const string F_Boss = "c2_boss";      // 破大营
        const string F_ChestFieldSW = "c2_chest_field_sw", F_ChestFieldNE = "c2_chest_field_ne", F_ChestTown = "c2_chest_town", F_ChestTown2 = "c2_chest_town2", F_ChestInn = "c2_chest_inn2f";
        const string F_ChestGzSW = "c2_chest_gz_sw", F_ChestGzE = "c2_chest_gz_e";

        // 青州商店（设计文档 8.3）：全城一家铺子，掌柜站门口，药、道具、兵器、甲都在他这儿买卖
        static readonly string[] ShopGoods = { "caoyao", "aiye", "zhuojiu", "langyan", "yanmudan", "nuqiwan", "feiniaoshan", "gangcha", "tongchui", "podao", "tieqiang", "tiejia", "pikui", "buxue" };

        public Chapter2(IGameHost host) : base(host)
        {
            Field = BuildField();
            Town = BuildTown();
            Fu = BuildFu();
            if (host.Flags.Has(F_Siege)) BurnCamps();
            var inn = BuildInn("qz_inn", InnCost, () => host.Warp(Town, 5, 13), Chest(F_ChestInn, 3, 3, 0, "zhuojiu", 2));
            Inn = inn[0];
            Inn2F = inn[1];
            Guangzong = BuildGuangzong();
        }

        /// <summary>围城已解：两座黄巾营盘烧成白地——物件（gone=c2_siege）、篝火、阻挡块一起撤掉。读档进来和打赢当场都走这里。</summary>
        void BurnCamps()
        {
            host.Flags.Set(F_Siege);
            Field.Decor.RemoveAll(d => d.Gone == F_Siege);
            Field.Emitters.RemoveAll(e => e.Kind == "fire");
            for (int y = 0; y < Field.Rows.Length; y++) Field.Rows[y] = Field.Rows[y].Replace('C', ',');
        }

        public override int Number { get { return 2; } }
        public override MapData[] Maps { get { return new[] { Field, Town, Fu, Inn, Inn2F, Guangzong }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Town, X = 19, Y = 26 } }; } }

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
            host.Warp(Town, 5, 13);   // 全灭回最近的有客栈的城：这一章只有青州城（解围前也进得去）
            Say(L(null, "胜败乃兵家常事。刘备一行退回青州城休整……"), L(null, "（金钱减半，兵力已恢复）"));
        }

        /// <summary>调试：直接当作围城已解。</summary>
        public override void SkipToField()
        {
            BurnCamps();
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
                EnemyLevel = 5,   // 第二章等级段 4～10：郊外 4～6 级，广宗 8～10 级
                ActorScale = 0.8f,
                Ground = ',',
                EnemyPool = new[] { GameDatabase.HuangJinBing, GameDatabase.HuangJinBing, GameDatabase.HuangJinLiShi, GameDatabase.HuangJinShuShi, GameDatabase.HuangJinTouMu },
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMtMMMMMMMMMMMMMM",
                    "M,,,,T,,,,,,,,,=,,,,,,,T,,,,,M",
                    "M,T,,,,,CCCCC,,=,,CCCCC,,,T,,M",
                    "M,,,,,,,CCCCC,,=,,CCCCC,,,,,,M",
                    "M,,,T,,,CCCCC,,=,,CCCCC,T,,,,M",
                    "M,,,,,,,CCCCC,,=,,CCCCC,,,,,,M",
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
            m.Emitters.Add(new MapEmitter("fire", 10.2f, 5.45f));
            m.Emitters.Add(new MapEmitter("fire", 20.2f, 5.45f));
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
                if (m.At(x, y) == 't') { host.Warp(Town, 19, 26); return true; }
                if (x == 29 && y == 8) { host.Warp(Guangzong, 1, 10); return true; }   // 东边大路 → 广宗
                if (x == 15 && y == 19) { host.GoChapter(1, "zhuojun_field", 19, 22); return true; }   // 南口 ↔ 涿郡郊外官道口（第一章）
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;   // 烧掉之后 C 都成了草地，碰不到了
                Say(L(null, "黄巾的营盘，帐篷里全是人。"));
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
                BurnCamps();
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
        // 太守府正堂：龚景在公案前，谢过、简雍入队都在这里
        // =====================================================================
        MapData BuildFu()
        {
            var m = new MapData
            {
                Id = "qz_fu", Name = "太守府", Art = "fu_qz", Grade = "#fff0dc", Light = "#e8d6c0", ArtMargin = 0,
                // 12×8：上三排是屏风和台基（公案、香炉、鼓、戟架），下面青石板地，两边红墙；从最下面走出去回府前广场
                Rows = new[] { "HHHHHHHHHHHH", "HHHHHHHHHHHH", "HHHHHHHHHHHH", "H..........H", "H..........H", "H..........H", "H..........H", "H..........H" }
            };
            var flags = host.Flags;
            m.Npcs.Add(new Npc
            {
                Id = "gongjing", Name = "龚景", Glyph = "龚", X = 5, Y = 3,   // 正堂公案前
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
                        L(null, "屏风后走出一名青衫文士，笑吟吟地拱手。"),
                        L("简雍", "涿郡简雍，字宪和。闻兄长起兵讨贼，特来相投。军中文书钱粮，就交给小弟吧。"),
                        L("刘备", "宪和来得正好！"),
                        L(null, "简雍加入队伍！"),
                        L("龚景", "眼下张角亲率主力屯兵广宗，卢植卢中郎将与之相持数月，正缺人手。"),
                        L("龚景", "玄德曾师事卢公，何不前去相助？出城往东，沿大路走到头便是广宗。"),
                    });
                }
            });
            m.OnTouch = (x, y) =>
            {
                if (y >= m.Height) { host.Warp(Town, 19, 8); return true; }
                return false;
            };
            return m;
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
                // 40×28，四屏大城（布局图和出图任务见 ArtTask/出图任务-青州城扩大.md）。
                // 太守府 10×7 在正上方居中（深宅，图比地皮高出去的部分在地图上边界外，镜头到不了），门 (19,7)；客栈 (3-7,9-12) 门 (5,12)；酒馆 (32-36,9-11)；商铺 (25-28,16-18) 门 (26,18)，西边对称那块地皮空着种草丛
                // 小庙 (10-13,1-3)、粮仓 (27-29,1-3)、六处民居（上排 1-6 / 32-38 × 16-20，下排 1-7 / 11-16 / 23-29 / 33-37 × 23-26，房子图比原来大一半到七成，再大就压到街上了）；西北角水塘园子，东北角桃园；S = 市集摊位 / 井，走不上去
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T.........HHHH.HHHHHHHHHH..HHH..fPffPf.T",
                    "T.fffff...HHHH.HHHHHHHHHH..HHH..ffffff.T",
                    "T.fw~~f...HHHH.HHHHHHHHHH..HHH..PffPff.T",
                    "T.f~~wf........HHHHHHHHHH.......fffffP.T",
                    "T.fffff........HHHHHHHHHH.......ffPfff.T",
                    "T..............HHHHHHHHHH..............T",
                    "T..............HHHHHHHHHH..............T",
                    "T............==============............T",
                    "T..HHHHH=....==============....=HHHHH..T",
                    "T..HHHHH=..........==..........=HHHHH..T",
                    "T..HHHHH=..........==..........=HHHHH..T",
                    "T..HHHHH=....S.S.S.==.S.S.S....=.......T",
                    "T.......=..........==..........=.......T",
                    "T======================================T",
                    "T======================================T",
                    "THHHHHH.=..........==....HHHH..=.......T",
                    "THHHHHH.=..........==....HHHH..=HHHHHHHT",
                    "THHHHHH.=..........==....HHHH..=HHHHHHHT",
                    "THHHHHH.=....S.S.S.==.S.S...S..=HHHHHHHT",
                    "THHHHHH.=..........==.........S=HHHHHHHT",
                    "T.====================================.T",
                    "T.......=..........==..........=.......T",
                    "THHHHHHH=..HHHHHH..==..HHHHHHH.=.HHHHH.T",
                    "THHHHHHH=..HHHHHH..==..HHHHHHH.=.HHHHH.T",
                    "THHHHHHH...HHHHHH..==..HHHHHHH...HHHHH.T",
                    "THHHHHHH...HHHHHH..==..HHHHHHH...HHHHH.T",
                    "TTTTTTTTTTTTTTTTTTTggTTTTTTTTTTTTTTTTTTT",
                }
            };
            m.DecorFile = "qingzhou";
            m.Emitters.Add(new MapEmitter("smoke", 3.3f, 15.95f));
            m.Emitters.Add(new MapEmitter("smoke", 36.3f, 15.95f));
            m.Emitters.Add(new MapEmitter("smoke", 4.3f, 22.95f));
            m.Critters.Add(new CritterSpawn("chicken", 4));
            m.Critters.Add(new CritterSpawn("sparrow", 6));
            m.Critters.Add(new CritterSpawn("butterfly", 3));
            m.Critters.Add(new CritterSpawn("butterfly2", 2));
            m.Critters.Add(new CritterSpawn("fish", 3));
            m.Critters.Add(new CritterSpawn("duck", 1));

            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestTown, 37, 2, 80));                 // 桃园角上
            m.Npcs.Add(Chest(F_ChestTown2, 38, 26, 0, "zhuojiu", 2));  // 东南角巷子里
            // 市集：一家铺子，掌柜站在门口，买卖都找他
            m.Npcs.Add(Person("qz_merchant", "掌柜", "shangren", null, 26, 19, () =>
                host.Say(new List<DialogueLine> { L("掌柜", "围城的日子可算熬过去了。药材酒水、刀枪甲胄都齐，诸位缺什么？换下来的旧物小店也收。") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Person("qz_soldier", "青州兵", "shouwei", "qiang", 18, 26, () =>
                Say(L("青州兵", flags.Has(F_GongJing) ? "张角兄弟屯兵广宗，出城往东走大路就是。" : "太守在府里候着诸位呢，顺着大街一直往北，最上头那座大宅。"))));
            // 街上的人：巡街的兵、挎篮的村妇、拄拐的老人、跑着玩的小孩、挑担的货郎、扛锄头的农夫
            m.Npcs.Add(Walker("qz_patrol", "巡街兵", "shouwei", "qiang", 0.9f, new[] { 19, 23, 19, 16 }, new[] { 19, 26 },
                "太守有令，城中不得喧哗。", "黄巾退了，城门总算能开了。", "东门外大路通广宗，那边还在打。"));
            m.Npcs.Add(Walker("qz_citizen", "村妇", "cunmin_nv", null, 1.0f, new[] { 10, 13, 18, 13 }, new[] { 5, 13 },
                "多亏诸位将军，城里总算能开城门了。", "围城这些天，米价涨了十倍。", "太守府里来了位姓简的先生，说是找刘将军的。"));
            m.Npcs.Add(Walker("qz_elder", "老人", "laoren", "guaizhang", 1.3f, new[] { 22, 13, 30, 13 }, new[] { 34, 12 },
                "老朽在青州住了六十年，头回见贼兵围城。", "西北角那口塘，年轻时常去钓鱼。", "太守是个好官，就是胆子小了点。"));
            m.Npcs.Add(Walker("qz_kid", "小孩", "xiaohai", null, 0.7f, new[] { 2, 21, 7, 21, 7, 22, 2, 22 }, new[] { 4, 21 },
                "大胡子叔叔的刀好长！", "娘说贼人都被打跑了，我能出来玩了！", "我长大也要当将军！"));
            m.Npcs.Add(Walker("qz_peddler", "货郎", "huolang", "biandan", 1.1f, new[] { 21, 16, 29, 16, 29, 19, 21, 19 }, new[] { 26, 19 },
                "针头线脑、胭脂水粉，便宜卖了！", "这几日生意好，城里人都出来逛了。", "客官要不要看看南边来的蜀锦？"));
            m.Npcs.Add(Walker("qz_farmer", "农夫", "cunmin_nan", "chutou", 1.0f, new[] { 32, 21, 38, 21, 38, 22, 32, 22 }, new[] { 35, 21 },
                "地里的麦子让贼兵踩了一半。", "酒馆的酒不错，就是贵。", "听说刘将军是中山靖王之后？"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 'g') { host.Warp(Field, 15, 1); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 5 && y == 12) { host.Warp(Inn, 4, 5); return true; }   // 客栈门（门口 (5,13) 往上撞）
                if (x == 19 && y == 7) { host.Warp(Fu, 5, 7); return true; }   // 府门 → 正堂
                if (x == 34 && y == 11) { Say(L(null, "酒馆里闹哄哄的，全是在议论黄巾退兵的人。")); return true; }
                if (x == 11 && y == 3) { Say(L(null, "一座小庙，供着土地。香炉里的香还是热的。")); return true; }
                if (x == 28 && y == 3) { Say(L(null, "官仓。围城半月，里面早空了。")); return true; }
                return false;
            };
            return m;
        }

        // =====================================================================
        // 广宗：西边进来，河上一座桥（张梁把守），河北岸黄巾大营（张角）；桥头往南的大路出图去洛阳（第三章）
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
                EnemyLevel = 9,   // 8～10 级
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
                    "M,,,,,T,,,,,,,,,,,,,,,=,,,,,,,,M",
                    "M,,,,,,,,,,,T,,,,,,,,,=,,,T,,,,M",
                    "M,,MM,,,,,,,,,,,,,,,,,=,,,,,,,,M",
                    "M,,MM,,,,,,T,,,,,,,,,,=,,,,,,,,M",
                    "M,,,,,,,,,,,,,,,,,,T,,=,,,,,,,,M",
                    "M,T,,,,,,,,,,,,,,,,,,,=,,,T,,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,=,,,,,,,,M",
                    "M,,,,,,,,T,,,,,,,,,,,,=,,,,,,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,=,,,,,,,,M",
                    "M,,,,T,,,,,,,,,,,,,,,,=,,,,,,,,M",
                    "MMMMMMMMMMMMMMMMMMMMMM=MMMMMMMMM",
                }
            };
            m.DecorFile = "guangzong";
            m.Emitters.Add(new MapEmitter("fire", 26.62f, 3.1f));
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
                if (x == 22 && y == 21) { if (flags.Has(F_Boss)) Cross(3, "lianjun_out", 6, 1); else Say(L("关羽", "兄长，张角还在河北岸，贼还没灭。")); return true; }   // 南边大路 → 第三章联军大营外（第三章开过头就直接走过去，不重放开场）
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
                    L(null, "（过桥回南岸，沿桥头往南的大路去洛阳）"),
                });
            }));
        }
    }
}
