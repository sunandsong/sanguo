using System;
using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第七章「三顾茅庐」（演义第三十四～三十九回）：刘表赠的卢、屯新野 → 单福（徐庶）来投、刘封认义子 → 新野郊外破曹仁八门金锁 → 徐庶归曹、走马荐诸葛 → 三顾隆中 → 博望坡火烧夏侯惇。
    /// 等级段 38～45。徐庶在破曹仁后离队（从队伍和后方都删掉）。
    /// </summary>
    public class Chapter7 : Chapter
    {
        public const int InnCost = 500;

        public readonly MapData Town, Yamen, Inn, Inn2F, Field, Longzhong, Bowang;

        const string F_Dilu = "c7_dilu", F_XuShu = "c7_xushu", F_LiuFeng = "c7_liufeng", F_CaoRen = "c7_caoren", F_XuShuGone = "c7_xushu_gone";
        const string F_Visit1 = "c7_visit1", F_Visit2 = "c7_visit2", F_Kongming = "c7_kongming";
        public const string F_Boss = "c7_boss";
        const string F_ChestTown1 = "c7_chest_town1", F_ChestTown2 = "c7_chest_town2", F_ChestInn = "c7_chest_inn2f";
        const string F_ChestF1 = "c7_chest_f1", F_ChestF2 = "c7_chest_f2", F_ChestF3 = "c7_chest_f3";
        const string F_ChestLz1 = "c7_chest_lz1", F_ChestLz2 = "c7_chest_lz2", F_ChestBw1 = "c7_chest_bw1", F_ChestBw2 = "c7_chest_bw2";

        // 新野商店（设计文档 8.3）
        static readonly string[] ShopGoods = { "caoyao", "jinchuangyao", "zhixuesan", "qingjiu", "huanhundan", "langyan", "yanmudan", "nuqiwan", "feiniaoshan", "qingfengjian", "langyabang", "liangyinqiang", "fuchen", "suozijia", "yulinjia", "junma" };
        static readonly GeneralDef[] CaoJun = { GameDatabase.CaoJunBuBing, GameDatabase.CaoJunGongBing, GameDatabase.QingZhouBing, GameDatabase.QingZhouBing, GameDatabase.HuBaoQi, GameDatabase.HuBaoQi };

        public Chapter7(IGameHost host) : base(host)
        {
            Town = BuildTown();
            Yamen = BuildYamen();
            var inn = BuildInn("xinye_inn", InnCost, () => host.Warp(Town, 5, 7), Chest(F_ChestInn, 3, 3, 0, "zhixuesan", 2));
            Inn = inn[0]; Inn2F = inn[1];
            Field = BuildField();
            Longzhong = BuildLongzhong();
            Bowang = BuildBowang();
        }

        public override int Number { get { return 7; } }
        public override MapData[] Maps { get { return new[] { Town, Yamen, Inn, Inn2F, Field, Longzhong, Bowang }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Town, X = 14, Y = 18 } }; } }

        public override void Begin()
        {
            host.Party.FullRestore();
            host.Warp(Town, 14, 18);
            host.Say(new List<DialogueLine>
            {
                L(null, "刘备投荆州刘表。刘表待之甚厚，拨新野一县给刘备屯兵。"),
                L(null, "蔡瑁忌刘备，设宴欲害之。刘备席间脱身，跃马过檀溪——坐下的卢一跃三丈，飞上西岸。"),
                L("刘备", "的卢，的卢，今日妨吾！……不，今日救吾。"),
            }, () =>
            {
                if (!host.Flags.Has(F_Dilu)) { host.Flags.Set(F_Dilu); Give("dilu"); Say(L(null, "获得的卢！")); }
            });
        }

        public override void RespawnAfterDefeat()
        {
            host.Party.Gold /= 2;
            host.Party.FullRestore();
            host.Warp(Town, 5, 7);
            Say(L(null, "胜败乃兵家常事。刘备一行退回新野休整……"), L(null, "（金钱减半，兵力已恢复）"));
        }

        public override void SkipToField()
        {
            host.Flags.Set(F_Dilu);
            host.Party.FullRestore();
            host.Warp(Field, 15, 1);
        }

        void RemoveFromParty(GeneralDef def)
        {
            host.Party.Members.RemoveAll(g => g.Def == def);
            host.Party.Stored.RemoveAll(g => g.Def == def);
        }

        // =====================================================================
        // 新野城：县衙在最上头，客栈西、商铺东，单福在街上唱歌，南门出城
        // =====================================================================
        MapData BuildTown()
        {
            var m = new MapData
            {
                Id = "xinye", Name = "新野", Art = "xinye",
                Grade = "#fff3e4", Weather = "leaf", EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T..........HHHHHHHH..........T",
                    "T..HHHHH...HHHHHHHH....HHHH..T",
                    "T..HHHHH...HHHHHHHH....HHHH..T",
                    "T..HHHHH...HHHHHHHH....HHHH..T",
                    "T..HHHHH...HHHHHHHH....HHHH..T",
                    "T.....=........=.........=...T",
                    "T.....====================...T",
                    "T..............==............T",
                    "T..HHHHHH......==.....HHHHH..T",
                    "T..HHHHHH......==.....HHHHH..T",
                    "T..HHHHHH......==.....HHHHH..T",
                    "T..............==............T",
                    "T....=====================...T",
                    "T..............==............T",
                    "T..HHHHH...S.S.==.S.S.HHHHH..T",
                    "T..HHHHH.......==.....HHHHH..T",
                    "T..HHHHH.......==.....HHHHH..T",
                    "T..............==............T",
                    "TTTTTTTTTTTTTTggTTTTTTTTTTTTTT",
                }
            };
            m.DecorFile = "xinye";
            m.Emitters.Add(new MapEmitter("smoke", 5.3f, 8.95f));
            m.Emitters.Add(new MapEmitter("smoke", 24.3f, 14.95f));
            m.Critters.Add(new CritterSpawn("chicken", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 5));
            m.Critters.Add(new CritterSpawn("butterfly", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestTown1, 28, 1, 800));
            m.Npcs.Add(Chest(F_ChestTown2, 1, 18, 0, "liangyinqiang"));
            m.Npcs.Add(Person("xy_merchant", "掌柜", "shangren", null, 25, 6, () =>
                host.Say(new List<DialogueLine> { L("掌柜", "荆州富庶，襄阳来的货都在这儿。亮银枪是新到的，看看？") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Person("xushu", "单福", GameDatabase.XuShu.Skin, "jian", 12, 12, () =>
            {
                if (flags.Has(F_XuShu)) return;
                flags.Set(F_XuShu);
                host.Say(new List<DialogueLine>
                {
                    L("单福", "（放声高歌）天地反覆兮，火欲殂；大厦将崩兮，一木难扶。山谷有贤兮，欲投明主；明主求贤兮，却不知吾。"),
                    L("刘备", "先生歌中之意……莫非是来找备的？"),
                    L("单福", "颍川单福，略通兵法。使君那匹的卢妨主，不如送人……哈哈，说笑。使君肯用我，我便留下。"),
                    L(null, "单福加入队伍！"),
                }, () => Join(GameDatabase.XuShu));
            }, () => !flags.Has(F_XuShu)));
            m.Npcs.Add(Person("xy_guard", "新野兵", "shouwei", "qiang", 13, 18, () =>
                Say(L("新野兵", !flags.Has(F_CaoRen) ? "曹仁的大军在城外北边扎营，摆了个怪阵。" : !flags.Has(F_Kongming) ? "隆中在城外西边山里，卧龙先生就住那儿。" : "博望坡在城外东边，夏侯惇的十万大军到了！"))));
            m.Npcs.Add(Walker("xy_patrol", "巡街兵", "shouwei", "qiang", 0.9f, new[] { 15, 14, 15, 8 }, new[] { 14, 18 },
                "刘皇叔来了，新野的日子好过多了。", "单先生摆的阵，咱们看都看不懂。", "听说皇叔去隆中，一连去了好几趟。"));
            m.Npcs.Add(Walker("xy_woman", "村妇", "cunmin_nv", null, 1.0f, new[] { 3, 12, 10, 12 }, new[] { 5, 12 },
                "新野人都唱：「新野牧，刘皇叔；自到此，民丰足。」", "隆中有个卧龙先生，躬耕陇亩，好为梁父吟。", "蔡夫人的弟弟蔡瑁，不是好东西。"));
            m.Npcs.Add(Walker("xy_elder", "老人", "laoren", "guaizhang", 1.3f, new[] { 17, 12, 24, 12 }, new[] { 22, 12 },
                "司马徽先生说：卧龙、凤雏，得一可安天下。", "老朽活了七十年，没见过皇叔这样的好官。", "的卢马眼下有泪槽，相书上说是妨主的……"));
            m.Npcs.Add(Walker("xy_kid", "小孩", "xiaohai", null, 0.7f, new[] { 3, 18, 10, 18 }, new[] { 5, 18 },
                "刘封哥哥射箭可准了！", "皇叔的马能飞！", "我也要去隆中看卧龙！"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 'g') { host.Warp(Field, 15, 1); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 5 && y == 5) { host.Warp(Inn, 4, 5); return true; }
                if (x == 15 && y == 5) { host.Warp(Yamen, 5, 7); return true; }
                return false;
            };
            return m;
        }

        MapData BuildYamen()
        {
            var flags = host.Flags;
            return BuildHall("xinye_yamen", "新野县衙", () => host.Warp(Town, 15, 6),
                Person("liufeng", "刘封", GameDatabase.LiuFeng.Skin, "qiang", 5, 3, () =>
                {
                    if (flags.Has(F_LiuFeng)) return;
                    flags.Set(F_LiuFeng);
                    host.Say(new List<DialogueLine>
                    {
                        L(null, "县衙里候着一个青年，是长沙刘氏的外甥寇封，父母双亡，投奔新野。"),
                        L("寇封", "寇封拜见皇叔。小人愿随皇叔左右，效犬马之劳。"),
                        L("刘备", "此子器宇不凡。我尚无子嗣，收你为义子，从此改名刘封，如何？"),
                        L("关羽", "兄长已有亲子阿斗，何必再收螟蛉？日后恐生祸乱。"),
                        L("刘备", "吾待之如子，彼必事吾如父，何乱之有？"),
                        L("刘封", "刘封拜见父亲！"),
                        L(null, "刘封加入队伍！"),
                    }, () => Join(GameDatabase.LiuFeng));
                }, () => !flags.Has(F_LiuFeng)));
        }

        // =====================================================================
        // 新野郊外：北边回城，北头正中曹仁的八门金锁阵，西边山路去隆中，东边大路去博望坡
        // =====================================================================
        MapData BuildField()
        {
            var m = new MapData
            {
                Id = "xinye_field", Name = "新野郊外", Art = "xinye_field",
                Grade = "#f0f4e8", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09, EnemyLevel = 40, ActorScale = 0.8f, Ground = ',', EnemyPool = CaoJun,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMtMMMMMMMMMMMMMM",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,T,,,,,,,,,,,=,,,,,,,T,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,T,,M",
                    "M,,,,,,,T,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,===============",
                    "================,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,CCCCCCCC,,,,,,,,,,M",
                    "M,,T,,,,,,,CCCCCCCC,,,,,T,,,,M",
                    "M,,,,,,,,,,CCCCCCCC,,,,,,,,,,M",
                    "M,,,,~~~,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,~~~~~,,,,,,,,,,,,,,,,MM,,M",
                    "M,,,,~~~,,,,,,,,,,T,,,,,,MM,,M",
                    "M,T,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "xinye_field";
            m.Emitters.Add(new MapEmitter("fire", 12.7f, 14.45f));
            m.Emitters.Add(new MapEmitter("fire", 17.7f, 14.45f));
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("fish", 3));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestF1, 28, 1, 0, "zhixuesan", 2));
            m.Npcs.Add(Chest(F_ChestF2, 2, 17, 0, "yulinjia"));
            m.Npcs.Add(Chest(F_ChestF3, 22, 18, 1000));
            m.Npcs.Add(Person("caoren_npc", "曹仁", GameDatabase.CaoRen.Skin, "dao", 15, 11, CaoRenFight, () => !flags.Has(F_CaoRen)));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't') { host.Warp(Town, 14, 18); return true; }
                if (x == 0 && y == 10)
                {
                    if (flags.Has(F_XuShuGone)) host.Warp(Longzhong, 28, 10);
                    else Say(L("关羽", "兄长，曹仁的大军就在城外，先退了敌兵再说。"));
                    return true;
                }
                if (x == 29 && y == 9)   // 东边大路（底图上这段路比西边高一格）
                {
                    if (flags.Has(F_Kongming)) host.Warp(Bowang, 1, 10);
                    else Say(L("赵云", "主公，东边是博望坡，眼下还没有敌情。"));
                    return true;
                }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;
                Say(L(null, flags.Has(F_CaoRen) ? "八门金锁阵已破，营里只剩些丢下的旗鼓。" : "曹仁的营盘排成八门：休、生、伤、杜、景、死、惊、开。"));
                return true;
            };
            return m;
        }

        void CaoRenFight()
        {
            bool hasXuShu = host.Party.Has(GameDatabase.XuShu);
            host.Say(new List<DialogueLine>
            {
                L("曹仁", "刘备，你可识得我这八门金锁阵？"),
                hasXuShu ? L("单福", "此阵八门，从生门、景门、开门入则吉；从伤门、惊门、休门入则伤，从杜门、死门入则亡。") : L("刘备", "……这阵势，看着便不好破。"),
                hasXuShu ? L("单福", "只是中军无主持。子龙从东南角生门杀入，往正西景门杀出，此阵自乱！") : L("关羽", "管他什么阵，杀进去便是！"),
                L("赵云", "得令！"),
            }, () => host.StartBattle(GameDatabase.Chapter7CaoRen(), true, () =>
            {
                host.Flags.Set(F_CaoRen);
                host.Flags.Set(F_XuShuGone);
                RemoveFromParty(GameDatabase.XuShu);
                host.Say(new List<DialogueLine>
                {
                    L(null, "赵云从生门杀入，往西杀出，又回马从东南杀出。八门金锁阵大乱，曹仁大败而走。"),
                    L(null, "曹操得知是单福设谋，查明其真名徐庶，便将徐庶老母接到许都，令其作书召子。"),
                    L("徐庶", "主公……庶本名徐庶，单福是化名。家母在曹营，庶不得不去。"),
                    L("刘备", "（泣）元直此去，备如断左右手……"),
                    L(null, "徐庶策马走出数里，又勒马回来。"),
                    L("徐庶", "庶几乎忘了一事：此间有一奇士，就在新野西边隆中。此人复姓诸葛，名亮，字孔明，号卧龙先生。"),
                    L("徐庶", "庶比他，譬犹驽马并麒麟、寒鸦配鸾凤。主公得此人，何愁天下不定！"),
                    L(null, "徐庶离开了队伍。"),
                    L("刘备", "城外往西的山路，便是隆中。"),
                });
            }));
        }

        // =====================================================================
        // 隆中：东边山路进来，竹林小溪，西头山坳里一座茅庐（诸葛亮）。不遇敌。三顾：头两回都没见着，被送回新野
        // =====================================================================
        MapData BuildLongzhong()
        {
            var m = new MapData
            {
                Id = "longzhong", Name = "隆中", Art = "longzhong",
                Grade = "#f2f6ee", Light = "#f4f2ea", Weather = "leaf",
                EncounterRate = 0, ActorScale = 0.8f, Ground = ',',
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                    "MMMM,,,,,,,,T,,,,,,,,,,T,,,,MM",
                    "MMM,,,HHHHH,,,,,,,,,,,,,,,,,,M",
                    "MM,,,,HHHHH,,,,T,,,,,,,,,,T,,M",
                    "MM,,,,HHHHH,,,,,,,,,,,,,,,,,,M",
                    "MM,,,,,,=,,,,,,,,,~~,,,,,,,,,M",
                    "MM,,T,,,=,,,,,,,,~~,,,,,T,,,,M",
                    "MM,,,,,,=,,,,,,,~~,,,,,,,,,,,M",
                    "MM,,,,,,=,,,,,,~~,,,,,,,,,,,,M",
                    "MM,,,,,,=,,,,,~~,,,,,,,,,,,,,M",
                    "MM,,,,,,=====w================",
                    "MM,,,,,,,,,,~~,,,,,,,,,,,,,,,M",
                    "MM,,T,,,,,,~~,,,,,,,T,,,,,,,,M",
                    "MM,,,,,,,,~~,,,,,,,,,,,,,,,,,M",
                    "MMM,,,,,,~~,,,,,,,,,,,,,T,,,,M",
                    "MMMM,,,,~~,,,,T,,,,,,,,,,,,,MM",
                    "MMMMM,,,,,,,,,,,,,,,,,,,,,,MMM",
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "longzhong";
            m.Emitters.Add(new MapEmitter("smoke", 9.5f, 2.2f));
            m.Critters.Add(new CritterSpawn("sparrow", 5));
            m.Critters.Add(new CritterSpawn("butterfly", 3));
            m.Critters.Add(new CritterSpawn("butterfly2", 2));
            m.Critters.Add(new CritterSpawn("fish", 3));
            m.Critters.Add(new CritterSpawn("rabbit", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestLz1, 27, 2, 0, "qingjiu", 3));
            m.Npcs.Add(Chest(F_ChestLz2, 5, 15, 0, "huanhundan"));
            m.Npcs.Add(Person("tongzi", "童子", "xiaohai", null, 8, 5, Visit, () => !flags.Has(F_Kongming)));
            m.Npcs.Add(Walker("lz_farmer", "农夫", "cunmin_nan", "chutou", 1.1f, new[] { 15, 13, 22, 13, 22, 15, 15, 15 }, null,
                "苍天如圆盖，陆地似棋局……这歌是卧龙先生教的。", "先生常抱膝长吟，自比管仲、乐毅。", "这几日先生在家，听说午后要睡觉。"));

            m.OnStep = (x, y) =>
            {
                if (x == 29 && y == 10) { host.Warp(Field, 1, 10); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) == 'H') { Say(L(null, "一座草庐，柴门半掩，门前有竹，院里有琴。")); return true; }
                return false;
            };
            return m;
        }

        void Visit()
        {
            var flags = host.Flags;
            if (!flags.Has(F_Visit1))
            {
                flags.Set(F_Visit1);
                host.Say(new List<DialogueLine>
                {
                    L("刘备", "汉左将军、宜城亭侯、领豫州牧、皇叔刘备，特来拜见先生。"),
                    L("童子", "我记不得许多名字。先生今早出去了，踪迹不定，不知几时回来。"),
                    L("张飞", "既不见，自去罢了。"),
                    L(null, "刘备怏怏而回。"),
                }, () => host.Warp(Town, 14, 18));
                return;
            }
            if (!flags.Has(F_Visit2))
            {
                flags.Set(F_Visit2);
                host.Say(new List<DialogueLine>
                {
                    L(null, "时值隆冬，朔风凛凛，瑞雪霏霏。"),
                    L("张飞", "天寒地冻，尚不用兵，岂宜远见无益之人？"),
                    L("刘备", "吾正欲使孔明知我殷勤之意。"),
                    L("童子", "先生昨日被崔州平邀去闲游了。屋里这位是先生的弟弟。"),
                    L("诸葛均", "家兄不在，皇叔请回吧。"),
                    L(null, "刘备留书一封，冒雪而回。"),
                }, () => host.Warp(Town, 14, 18));
                return;
            }
            flags.Set(F_Kongming);
            host.Say(new List<DialogueLine>
            {
                L(null, "又是一年春暖。刘备第三次来到隆中，离草庐半里便下马步行。"),
                L("童子", "先生今日在家，只是在草堂上昼寝未醒。"),
                L("刘备", "既如此，且休通报。"),
                L(null, "刘备拱立阶下。半晌，先生翻身朝里又睡。张飞大怒，要去屋后放一把火，被关羽死死拉住。"),
                L(null, "又立了一个时辰，孔明才醒，口吟诗曰：「大梦谁先觉？平生我自知。草堂春睡足，窗外日迟迟。」"),
                L("诸葛亮", "南阳诸葛亮，有劳将军久待。"),
                L("刘备", "汉室倾颓，奸臣窃命。备不量力，欲伸大义于天下，而智术浅短。惟先生开其愚而拯其厄！"),
                L("诸葛亮", "曹操已拥百万之众，挟天子以令诸侯，此诚不可与争锋。孙权据有江东，已历三世，可以为援而不可图也。"),
                L("诸葛亮", "荆州北据汉沔，西通巴蜀，此用武之国；益州险塞，沃野千里，天府之土。将军若跨有荆益，保其岩阻……则霸业可成，汉室可兴矣。"),
                L("刘备", "先生之言，顿开茅塞！愿先生不弃鄙贱，出山相助！"),
                L("诸葛亮", "亮久乐耕锄，懒于应世……将军既不相弃，愿效犬马之劳。"),
                L(null, "诸葛亮加入队伍！自带羽扇、八卦道袍。"),
                L(null, "（提示：可在人物页把诸葛亮任命为军师。）"),
            }, () =>
            {
                Join(GameDatabase.ZhuGeLiang, "yushan", "baguadaopao");
                host.Warp(Town, 14, 18);
                Say(L("诸葛亮", "主公，探马来报，夏侯惇引兵十万，已到博望坡。出城往东便是。亮自有安排。"));
            });
        }

        // =====================================================================
        // 博望坡：西边进来，两山夹一条窄道，道两旁芦苇。东头夏侯惇的大营。
        // =====================================================================
        MapData BuildBowang()
        {
            var m = new MapData
            {
                Id = "bowang", Name = "博望坡", Art = "bowang",
                Grade = "#f4eee2", Light = "#f2ece2", Weather = "leaf",
                EncounterRate = 0.1, EnemyLevel = 43, ActorScale = 0.8f, Ground = ',', EnemyPool = CaoJun,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                    "M,,,,T,,,,,MMMMMMMM,,,,,,,,,,M",
                    "M,,,,,,,,,MMMMMMMMMM,,,CCCC,,M",
                    "M,T,,,,,,,MMMMMMMMMM,,,CCCC,,M",
                    "M,,,,,,,,,,MMMMMMMM,,,,CCCC,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,T,,,,,,,,,,,,,,,,,T,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "MMMMMMMM,,,,,,,,,,,,,,MMMMMMMM",
                    "MMMMMMMMM,,,,,,,,,,,,MMMMMMMMM",
                    "=========================,,,,M",
                    "MMMMMMMMM,,,,,,,,,,,,MMMMMMMMM",
                    "MMMMMMMM,,,,,,,,,,,,,,MMMMMMMM",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,T,,,,,,,,,,,,,,,,,,,,T,,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,T,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,T,,,,,,,,,,,,,,,,,,,,,,,T,,M",
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "bowang";
            m.Emitters.Add(new MapEmitter("fire", 25.7f, 4.45f));
            m.Critters.Add(new CritterSpawn("sparrow", 3));
            m.Critters.Add(new CritterSpawn("rabbit", 2));
            m.Critters.Add(new CritterSpawn("dragonfly", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestBw1, 2, 17, 0, "zhixuesan", 2));
            m.Npcs.Add(Chest(F_ChestBw2, 27, 14, 1200));
            m.Npcs.Add(Person("xiahoudun_npc", "夏侯惇", GameDatabase.XiaHouDun.Skin, "qiang", 25, 5, XiaHouDunFight, () => !flags.Has(F_Boss)));

            m.OnStep = (x, y) =>
            {
                if (x == 0 && y == 10) { host.Warp(Field, 28, 9); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) == 'C') { Say(L(null, "曹军大营，旗上一个大大的「夏侯」。")); return true; }
                return false;
            };
            return m;
        }

        void XiaHouDunFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("夏侯惇", "哈哈哈！刘备就这点兵马？诸葛村夫竟以此等人马为前部，正如驱犬羊与虎豹斗耳！"),
                L("诸葛亮", "（羽扇轻摇）元让将军，博望坡两边都是芦苇，今日风大，将军可要小心火烛。"),
                L("夏侯惇", "休要胡言！给我冲！"),
            }, () => host.StartBattle(GameDatabase.Chapter7XiaHouDun(), true, () =>
            {
                host.Flags.Set(F_Boss);
                host.Say(new List<DialogueLine>
                {
                    L(null, "曹军追入窄道，忽听一声炮响，两边芦苇齐着，四面八方尽是火。夏侯惇冒烟突火而走，十万大军折了大半。"),
                    L("关羽", "（拱手）孔明真英杰也！"),
                    L("张飞", "军师……俺服了。往后军师说怎么打，俺就怎么打。"),
                    L("诸葛亮", "此不过小胜。曹操必亲率大军南下，新野小县，不可久居。"),
                    L("刘备", "百姓随我多年，备怎忍弃之……"),
                    L("诸葛亮", "那便带着百姓走。往南，过长坂，去江陵。"),
                }, () => host.ChapterClear("第七章 三顾茅庐 · 完", "第八章：长坂·赤壁（未完待续）"));
            }));
        }
    }
}
