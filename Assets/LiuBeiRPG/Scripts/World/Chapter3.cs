using System;
using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第三章「讨伐董卓」（演义第五回）：十八路诸侯会盟 → 汜水关关羽温酒斩华雄 → 虎牢关三英战吕布（撑满 10 回合即胜）→ 董卓焚洛阳西迁，联军散。
    /// 等级段 10～18。底图和人物还没出，清单见 ArtTask/出图任务-第三到七章.md；没图时按地块画色块、借现成角色。
    /// </summary>
    public class Chapter3 : Chapter
    {
        public const int InnCost = 80;

        public readonly MapData Out, Camp, Inn, Inn2F, Sishui, Hulao;

        const string F_HuaXiong = "c3_huaxiong", F_Baima = "c3_baima";
        public const string F_Boss = "c3_boss", F_Begun = "c3_begun";
        const string F_ChestCamp = "c3_chest_camp", F_ChestInn = "c3_chest_inn2f";
        const string F_ChestSs1 = "c3_chest_ss1", F_ChestSs2 = "c3_chest_ss2", F_ChestSs3 = "c3_chest_ss3";
        const string F_ChestHl1 = "c3_chest_hl1", F_ChestHl2 = "c3_chest_hl2", F_ChestHl3 = "c3_chest_hl3", F_ChestHl4 = "c3_chest_hl4";

        // 随军货郎（设计文档 8.3「洛阳」那一行，洛阳被烧了，货郎跟着大营走）
        static readonly string[] ShopGoods = { "caoyao", "aiye", "qingjiu", "langyan", "yanmudan", "nuqiwan", "feiniaoshan", "changji", "guitoudao", "kaishanfu", "tiejia", "pikui", "buxue" };
        static readonly GeneralDef[] XiLiang = { GameDatabase.XiLiangBing, GameDatabase.XiLiangBing, GameDatabase.XiLiangBing, GameDatabase.XiLiangTieQi, GameDatabase.FeiXiongJun };
        static readonly GeneralDef[] XiLiangElite = { GameDatabase.XiLiangBing, GameDatabase.XiLiangTieQi, GameDatabase.XiLiangTieQi, GameDatabase.FeiXiongJun, GameDatabase.FeiXiongJun };

        public Chapter3(IGameHost host) : base(host)
        {
            Out = BuildOut();
            Camp = BuildCamp();
            var inn = BuildInn("camp_inn", InnCost, () => host.Warp(Camp, 7, 9), Chest(F_ChestInn, 3, 3, 0, "qingjiu", 2));
            Inn = inn[0]; Inn2F = inn[1];
            Sishui = BuildSishui();
            Hulao = BuildHulao();
        }

        public override int Number { get { return 3; } }
        public override MapData[] Maps { get { return new[] { Out, Camp, Inn, Inn2F, Sishui, Hulao }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Camp, X = 13, Y = 11 } }; } }

        public override void Begin()
        {
            host.Flags.Set(F_Begun);
            host.Party.FullRestore();
            host.Warp(Out, 6, 1);
            host.Say(new List<DialogueLine>
            {
                L(null, "黄巾既平，朝廷却赏罚不明。刘备只得了个安喜县尉，又因张飞鞭打督邮弃官而去，投奔旧友公孙瓒。"),
                L(null, "不久，董卓废少帝、立献帝，专擅朝政。曹操发檄文，天下十八路诸侯起兵讨董，会盟于汜水关下。"),
                L("公孙瓒", "玄德，诸侯皆在前面大营，随我去见盟主袁本初。"),
                L("刘备", "备愿效犬马之劳。"),
                L("张飞", "董卓那厮早该杀了！哥哥，俺们这回可要露露脸。"),
            });
        }

        public override void RespawnAfterDefeat()
        {
            host.Party.Gold /= 2;
            host.Party.FullRestore();
            host.Warp(Camp, 7, 9);
            Say(L(null, "胜败乃兵家常事。刘备一行退回联军大营休整……"), L(null, "（金钱减半，兵力已恢复）"));
        }

        public override void SkipToField()
        {
            host.Flags.Set(F_Baima);
            host.Party.FullRestore();
            host.Warp(Sishui, 1, 10);
        }

        // =====================================================================
        // 联军大营外：北边土路回广宗（第二章），右上是联军大营的栅栏和营门，进门到大营南门；东边出图去汜水关
        // =====================================================================
        MapData BuildOut()
        {
            var m = new MapData
            {
                Id = "lianjun_out", Name = "联军大营外", Art = "lianjun_out",
                Grade = "#fff1e0", Weather = "leaf",
                EncounterRate = 0.06, EnemyLevel = 10, ActorScale = 0.8f, Ground = ',',
                EnemyPool = XiLiang,
                Rows = new[]
                {
                    "TTTTTT=TTTTTTTTTTTTTTTTTTTTTTT",
                    "T,,,,,=,,,,,,,,,CCCCCCCCCCCC,T",
                    "T,,,,,=,,,,,,,,,CCCCCCCCCCCC,T",
                    "T,,T,,=,,,,,,,,,CCCCCCCCCCCC,T",
                    "T,,,,,=,,,,,T,,,CCCCCCCCCCCC,T",
                    "T,,,,,=,,,,,,,,,CCCCCCCCCCCC,T",
                    "T,,,,,=,,,,,,,,,CCCCCCCCCCCC,T",
                    "T,,,,,=,,,,,,,,,CCCCCgCCCCCC,T",
                    "T,,,,,=,,,,,,,,,,,,,,=,,,,,,,T",
                    "T,,,,,=,,,,,,,,,,,,,,=,,,,,,,T",
                    "T,,,,,========================",
                    "T,,,,,,,,,,,,,,,,,,,,,,,,,,,,T",
                    "T,,T,,,,,,,,,,,,,,,,,,,,,T,,,T",
                    "T,,,,,,,,,,~~~,,,,,,,,,,,,,,,T",
                    "T,,,,,,,,,~~~~~,,,,,,T,,,,,,,T",
                    "T,,,,,,,,,,~~~,,,,,,,,,,,,,,,T",
                    "T,,,,,T,,,,,,,,,,,,,,,,,,,T,,T",
                    "T,,,,,,,,,,,,,,,,,T,,,,,,,,,,T",
                    "T,,,,,,,,,,,,,,,,,,,,,,,,,,,,T",
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                }
            };
            m.DecorFile = "lianjun_out";
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("rabbit", 2));
            m.Npcs.Add(Person("out_guard", "守营军士", "shouwei", "qiang", 22, 8, () =>
                Say(L("守营军士", "这里是十八路诸侯会盟的大营，进门往北就是中军帐。"), L("守营军士", "往东出去就是汜水关，华雄守在关前。北边那条土路通河北广宗。"))));

            m.OnStep = (x, y) =>
            {
                if (x == 6 && y == 0) { host.GoChapter(2, "guangzong", 22, 20); return true; }   // 北边土路 ↔ 广宗南口
                if (x == 21 && y == 7) { host.Warp(Camp, 13, 18); return true; }   // 营门 ↔ 大营南门
                if (x == 29 && y == 10) { host.Warp(Sishui, 1, 10); return true; }   // 东口 ↔ 汜水关西口
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) == 'C') { Say(L(null, "联军大营的木栅栏，栅栏后帐篷连绵，旌旗蔽日。")); return true; }
                return false;
            };
            return m;
        }

        // =====================================================================
        // 联军大营：中军帐前曹操、袁绍，公孙瓒送白马，货郎摆摊；只有南门一个口，出去是联军大营外
        // =====================================================================
        MapData BuildCamp()
        {
            var m = new MapData
            {
                Id = "lianjun_camp", Name = "联军大营", Art = "lianjun_camp",
                Grade = "#fff1e0", Weather = "leaf", EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T............=...............T",
                    "T..CCCC......=......CCCC.....T",
                    "T..CCCC......=......CCCC.....T",
                    "T............=...............T",
                    "T....HHHHH...=....CCCCCC.....T",
                    "T....HHHHH...=....CCCCCC.....T",
                    "T....HHHHH...=....CCCCCC.....T",
                    "T....HHHHH...=....CCCCCC.....T",
                    "T............=...............T",
                    "T..==========================T",
                    "T..=.........=...........=...T",
                    "T..=..CCC....=....CCC....=...T",
                    "T..=..CCC....=....CCC....=...T",
                    "T..=.........=...........=...T",
                    "T..=..S.S....=....S.S....=...T",
                    "T..=.........=...........=...T",
                    "T..=======================...T",
                    "T............=...............T",
                    "TTTTTTTTTTTTT=TTTTTTTTTTTTTTTT",
                }
            };
            m.DecorFile = "lianjun_camp";
            m.Emitters.Add(new MapEmitter("fire", 7.5f, 13.9f));
            m.Emitters.Add(new MapEmitter("fire", 19.5f, 13.9f));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("chicken", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestCamp, 27, 2, 150));
            m.Npcs.Add(Person("caocao", "曹操", "caocao", "jian", 20, 9, () =>
            {
                if (flags.Has(F_Boss)) Say(L("曹操", "董卓西逃，诸侯却只顾饮宴。竖子不足与谋！玄德，你我日后再会。"));
                else if (flags.Has(F_HuaXiong)) Say(L("曹操", "云长一刀斩了华雄，痛快！董卓已遣吕布率十五万大军守虎牢关。"), L("曹操", "过了汜水关便是虎牢关。吕布有万夫不当之勇，诸位务要小心。"));
                else Say(L("曹操", "在下曹操，字孟德。玄德公虽是白身，却是汉室宗亲，操久闻大名。"), L("曹操", "汜水关守将华雄连斩我军数将，诸侯束手。玄德若肯出战，操为诸位斟酒壮行！"));
            }));
            m.Npcs.Add(Person("yuanshao", "袁绍", "yuanshao", null, 22, 9, () =>
            {
                if (flags.Has(F_HuaXiong)) Say(L("袁绍", "既是汉室宗亲，赏座。虎牢关的事，就有劳诸位了。"));
                else Say(L("袁绍", "此人是谁？……哦，中山靖王之后。既是宗亲，站到帐下听令便是。"), L(null, "袁术在一旁冷笑：「一个县令手下的小卒，也敢在此喧哗？」"));
            }));
            m.Npcs.Add(Person("gongsunzan", "公孙瓒", "gongsunzan", "qiang", 8, 11, () =>
            {
                if (flags.Has(F_Baima)) { Say(L("公孙瓒", "玄德，出南门往东便是汜水关，去吧，我在大营等你的好消息。")); return; }
                flags.Set(F_Baima);
                if (Give("baima")) Say(L("公孙瓒", "玄德，我白马义从的坐骑送你一匹，日行八百，上阵好用。"), L(null, "获得白马！"));
            }));
            m.Npcs.Add(Person("camp_peddler", "随军货郎", "huolang", "biandan", 19, 14, () =>
                host.Say(new List<DialogueLine> { L("随军货郎", "小的跟着大军走，洛阳城里的货都在这儿了。清酒、伤药、长戟、开山大斧，诸位看看？") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Walker("camp_soldier", "联军士卒", "shouwei", "qiang", 0.9f, new[] { 5, 11, 11, 11 }, null,
                "华雄连斩了俞涉、潘凤两位将军，谁还敢出战？", "十八路诸侯，十八个心思。", "曹校尉倒是个明白人。"));
            m.Npcs.Add(Walker("camp_cook", "伙夫", "cunmin_nan", "chutou", 1.1f, new[] { 5, 16, 11, 16 }, null,
                "几十万人吃饭，粮草可是大事。", "听说董卓在洛阳夜宿龙床，无恶不作。", "客栈就在西边，将军们歇脚都去那儿。"));

            m.OnStep = (x, y) =>
            {
                if (x == 13 && y == 19) { host.Flags.Set(F_Begun); host.Warp(Out, 21, 8); return true; }   // 南门 ↔ 大营外营门
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 7 && y == 8) { host.Warp(Inn, 4, 5); return true; }
                if (m.At(x, y) == 'C') { Say(L(null, y <= 8 && x >= 18 ? "中军大帐，帐前立着各路诸侯的旗号。" : "联军的营帐，帐里堆着兵器甲胄。")); return true; }
                return false;
            };
            return m;
        }

        // =====================================================================
        // 汜水关：西边官道进来，东头山口就是关门，华雄堵在关前
        // =====================================================================
        MapData BuildSishui()
        {
            var m = new MapData
            {
                Id = "sishui", Name = "汜水关", Art = "sishui",
                Grade = "#f4f0e4", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09, EnemyLevel = 12, ActorScale = 0.8f, Ground = ',', EnemyPool = XiLiang,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                    "M,,,,T,,,,,,,,,,,,,,T,,,,MMMMM",
                    "M,,,,,,,,,,,T,,,,,,,,,,,,,MMMM",
                    "M,T,,,,,,,,,,,,,,,,,,,,,,,,MMM",
                    "M,,,,,,,CCC,,,,,,,,T,,,,,,,MMM",
                    "M,,,,,,,CCC,,,,,,,,,,,,,,,,,MM",
                    "M,,T,,,,,,,,,,,,,,,,,,,,,,,,MM",
                    "M,,,,,,,,,,,,,T,,,,,,,,,,,,,MM",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,MMM",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,MMMMM",
                    "============================gM",
                    "M,,,~~~~,,,,,,,,,,,,,,,,,MMMMM",
                    "M,,,~~~,,,,,,T,,,,,,,,,,,,,MMM",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,MM",
                    "M,T,,,,,,,,,,,,,,,T,,,,,,,,,MM",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,MMM",
                    "M,,,,,,,T,,,,,,,,,,,,,,,,,,MMM",
                    "M,,,,,,,,,,,,,,,,,T,,,,,,,MMMM",
                    "M,,,T,,,,,,,,,,,,,,,,,,,,MMMMM",
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "sishui";
            m.Emitters.Add(new MapEmitter("fire", 8.3f, 5.45f));
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("dragonfly", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestSs1, 13, 2, 200));
            m.Npcs.Add(Chest(F_ChestSs2, 2, 17, 0, "aiye", 2));
            m.Npcs.Add(Chest(F_ChestSs3, 24, 13, 0, "changji"));
            m.Npcs.Add(Person("huaxiong_npc", "华雄", "huaxiong", "podao", 26, 10, HuaXiongFight, () => !flags.Has(F_HuaXiong)));
            m.Npcs.Add(Person("ss_soldier", "联军士卒", "shouwei", "qiang", 2, 9, () =>
                Say(L("联军士卒", flags.Has(F_HuaXiong) ? "华雄死了！关门开了，诸位快进关吧。" : "关前那个使大刀的就是华雄，俞涉、潘凤两位将军都是不到三合就被他斩了。"))));

            m.OnStep = (x, y) =>
            {
                if (x == 0 && y == 10) { host.Warp(Out, 28, 10); return true; }   // 西口 ↔ 联军大营外东口
                if (m.At(x, y) == 'g') { host.Warp(Hulao, 14, 18); return true; }   // 关门 → 虎牢关南口
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) == 'C') { Say(L(null, "西凉军的哨营，营里喊声震天。")); return true; }
                return false;
            };
            return m;
        }

        void HuaXiongFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("华雄", "关东鼠辈，谁敢与我决一死战？"),
                L(null, "曹操斟了一杯热酒，递与关羽。"),
                L("关羽", "酒且斟下，某去便来！"),
            }, () => host.StartBattle(GameDatabase.Chapter3HuaXiong(), true, () =>
            {
                host.Flags.Set(F_HuaXiong);
                host.Say(new List<DialogueLine>
                {
                    L(null, "鸾铃响处，关羽提着华雄的首级回到帐前，掷于地上。"),
                    L(null, "那杯酒，尚温。"),
                    L("张飞", "俺哥哥斩了华雄，不就此时杀入关去，还等什么？"),
                    L(null, "袁术大怒：「一个县令手下的小卒，也敢在此耀武扬威！」曹操连忙劝住。"),
                    L("曹操", "得功者赏，何计贵贱？玄德，董卓已遣吕布守虎牢关，还得劳烦三位。"),
                });
            }));
        }

        // =====================================================================
        // 虎牢关：南口从汜水关进来，北头关门前的山口只容一人，吕布一夫当关
        // =====================================================================
        MapData BuildHulao()
        {
            var m = new MapData
            {
                Id = "hulao", Name = "虎牢关", Art = "hulao",
                Grade = "#eceef4", Light = "#eeeae4", Weather = "leaf",
                EncounterRate = 0.1, EnemyLevel = 16, ActorScale = 0.8f, Ground = ',', EnemyPool = XiLiangElite,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMgMMMMMMMMMMMMMMM",
                    "MMMMMMMMMMMMM,=,MMMMMMMMMMMMMM",
                    "M,,,,,,,,,,,,,=,,,,,,,,,,,,,,M",
                    "M,,,,T,,,CCC,,=,,,,CCC,,,,,T,M",
                    "M,,,,,,,,CCC,,=,,,,CCC,,,,,,,M",
                    "M,,T,,,,,CCC,,=,,,,CCC,,,T,,,M",
                    "M,,,,,,,,,,,,,=,,,,,,,,,,,,,,M",
                    "M,,,,,T,,,,,,,=,,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,================",
                    "M,MMM,,,,,,,,,=,,,,,,,,,,,,,,M",
                    "M,MMM,,,,,,,,,=,,,,,,,,,,,,,,M",
                    "M,,,,,,,T,,,,,=,,,,T,,,,,,,,,M",
                    "M,,,,,,,,,,,,,=,,,,,,,,MM,,,,M",
                    "M,,,~~~~,,,,,,=,,,,,,,,MM,,,,M",
                    "M,,,~~~~,,,,,,=,,,T,,,,,,,,,,M",
                    "M,,,,,,,,,,T,,=,,,,,,,,,,,,,,M",
                    "M,T,,,,,,,,,,,=,,,,,,,,,,,T,,M",
                    "M,,,,,,,,,,,,,=,,,,,,,,,T,,,,M",
                    "M,,,,,,,,,,,,,=,,,,,,,,,,,,,,M",
                    "MMMMMMMMMMMMMM=MMMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "hulao";
            m.Emitters.Add(new MapEmitter("fire", 10.4f, 4.6f));
            m.Emitters.Add(new MapEmitter("fire", 20.4f, 4.6f));
            m.Critters.Add(new CritterSpawn("sparrow", 3));
            m.Critters.Add(new CritterSpawn("rabbit", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestHl1, 2, 17, 300));
            m.Npcs.Add(Chest(F_ChestHl2, 27, 5, 0, "aiye", 2));
            m.Npcs.Add(Chest(F_ChestHl3, 25, 17, 0, "qingjiu", 3));
            m.Npcs.Add(Chest(F_ChestHl4, 12, 4, 0, "huanhundan"));
            m.Npcs.Add(Person("lvbu_npc", "吕布", "lvbu", "huaji", 14, 1, LvBuFight, () => !flags.Has(F_Boss)));
            m.Npcs.Add(Person("hl_scout", "联军哨兵", "shouwei", "qiang", 13, 17, () =>
                Say(L("联军哨兵", "关前那人头戴三叉束发紫金冠，骑赤兔马，手持方天画戟，便是吕布。"), L("联军哨兵", "方悦、穆顺两位将军都死在他戟下，武安国被他斩断了手腕。诸位能撑住十个回合，就是大功！"))));

            m.OnStep = (x, y) =>
            {
                if (x == 14 && y == 19) { host.Warp(Sishui, 27, 10); return true; }   // 南口 ↔ 汜水关关门
                if (x == 29 && y == 8) { if (flags.Has(F_Boss)) Cross(4, "beihai_field", 15, 18); else Say(L("关羽", "兄长，吕布还守在关前，此时岂能离去？")); return true; }   // 吕布退走、联军散，东口往东去北海 → 第四章
                if (x == 14 && y == 0 && flags.Has(F_Boss)) { Say(L("关羽", "董卓已焚洛阳西去，诸侯各自散了。兄长，往东走吧。")); return true; }   // 关门里不去，第四章走东口
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) == 'C') { Say(L(null, "西凉军的营帐，帐外拴着一排战马。")); return true; }
                return false;
            };
            return m;
        }

        void LvBuFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("吕布", "吾乃吕奉先！关东诸侯，谁敢与我一战？"),
                L("张飞", "三姓家奴休走！燕人张飞在此！"),
                L("关羽", "三弟稍待，待我助你！"),
                L("刘备", "两位贤弟，备也来了！"),
                L(null, "三人围住吕布，转灯儿般厮杀。撑过十个回合，吕布便无力再战。"),
            }, () => host.StartBattle(GameDatabase.Chapter3LvBu(), true, () =>
            {
                host.Flags.Set(F_Boss);
                host.Say(new List<DialogueLine>
                {
                    L(null, "吕布招架不住，虚刺一戟，拨马退入关中。八路诸侯一齐杀上，西凉军溃退。"),
                    L(null, "董卓见联军势大，挟天子迁都长安，临行火烧洛阳，二百里内鸡犬不留。"),
                    L(null, "诸侯入洛阳，见宫阙尽成焦土。各怀异心，不久便各自散去。"),
                    L("刘备", "汉室陵庙，尽成焦土……"),
                    L("关羽", "兄长，诸侯已散，公孙瓒也回了北平。咱们先回平原，再图后计。"),
                    L("张飞", "下回再碰上那三姓家奴，俺定要刺他个透心凉！"),
                    L(null, "（出虎牢关东口，往东去北海）"),
                });
            }, 10));
        }
    }
}
