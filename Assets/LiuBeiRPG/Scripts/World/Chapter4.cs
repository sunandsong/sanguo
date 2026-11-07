using System;
using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第四章「徐州风云」（演义第十一～十九回）：公孙瓒遣赵云相助 → 北海解围斩管亥 → 陶谦三让徐州、糜竺孙乾来投 → 小沛破纪灵 → 吕布袭下邳，与吕布决战。
    /// 等级段 18～26。徐州城的地图第五章还要用（Chapter5），所以底板写成静态的 XuzhouTown()。
    /// </summary>
    public class Chapter4 : Chapter
    {
        public const int InnCost = 150;

        public readonly MapData Beihai, Town, Fu, Inn, Inn2F, Field;

        const string F_GuanHai = "c4_guanhai", F_TaoQian = "c4_taoqian", F_JiLing = "c4_jiling";
        public const string F_Boss = "c4_boss";
        const string F_ChestBh1 = "c4_chest_bh1", F_ChestBh2 = "c4_chest_bh2", F_ChestBh3 = "c4_chest_bh3";
        const string F_ChestTown1 = "c4_chest_town1", F_ChestTown2 = "c4_chest_town2", F_ChestInn = "c4_chest_inn2f";
        const string F_ChestF1 = "c4_chest_f1", F_ChestF2 = "c4_chest_f2", F_ChestF3 = "c4_chest_f3", F_ChestF4 = "c4_chest_f4";

        // 徐州商店（设计文档 8.3）
        public static readonly string[] ShopGoods = { "caoyao", "jinchuangyao", "qingjiu", "huanhundan", "langyan", "yanmudan", "nuqiwan", "feiniaoshan", "gangbian", "tiejian_bian", "qingfengjian", "langyabang", "fuchen", "suozijia", "junma", "pikui", "buxue" };
        static readonly GeneralDef[] HuangJinRemnant = { GameDatabase.HuangJinBing, GameDatabase.HuangJinLiShi, GameDatabase.HuangJinLiShi, GameDatabase.HuangJinShuShi, GameDatabase.HuangJinTouMu };
        static readonly GeneralDef[] HuaiNan = { GameDatabase.HuaiNanBing, GameDatabase.HuaiNanBing, GameDatabase.HuaiNanBing, GameDatabase.XiLiangTieQi, GameDatabase.FeiXiongJun };

        public Chapter4(IGameHost host) : base(host)
        {
            Beihai = BuildBeihai();
            if (host.Flags.Has(F_GuanHai)) BurnCamps();
            Town = BuildTown();
            Fu = BuildFu();
            var inn = BuildInn("xuzhou_inn", InnCost, () => host.Warp(Town, 5, 7), Chest(F_ChestInn, 3, 3, 0, "jinchuangyao", 2));
            Inn = inn[0]; Inn2F = inn[1];
            Field = BuildField();
        }

        void BurnCamps()
        {
            host.Flags.Set(F_GuanHai);
            Beihai.Decor.RemoveAll(d => d.Gone == F_GuanHai);
            Beihai.Emitters.RemoveAll(e => e.Kind == "fire");
            for (int y = 0; y < Beihai.Rows.Length; y++) Beihai.Rows[y] = Beihai.Rows[y].Replace('C', ',');
        }

        public override int Number { get { return 4; } }
        public override MapData[] Maps { get { return new[] { Beihai, Town, Fu, Inn, Inn2F, Field }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Town, X = 14, Y = 18 } }; } }

        public override void Begin()
        {
            host.Party.FullRestore();
            host.Warp(Beihai, 15, 18);
            if (!host.Party.Has(GameDatabase.ZhaoYun)) Join(GameDatabase.ZhaoYun, "yajiaoqiang");
            host.Say(new List<DialogueLine>
            {
                L(null, "刘备回到平原，不久北海太守孔融遣太史慈求救：黄巾余党管亥围了北海城。"),
                L(null, "公孙瓒借兵三千相助，又遣一员白袍小将随行。"),
                L("赵云", "常山赵子龙，奉公孙将军之命，听玄德公调遣。"),
                L("刘备", "子龙来得正好！"),
                L(null, "赵云加入队伍！"),
                L("张飞", "北海就在前头，俺先去会会那个管亥！"),
            });
        }

        public override void RespawnAfterDefeat()
        {
            host.Party.Gold /= 2;
            host.Party.FullRestore();
            if (host.Flags.Has(F_GuanHai)) { host.Warp(Town, 5, 7); Say(L(null, "胜败乃兵家常事。刘备一行退回徐州城休整……"), L(null, "（金钱减半，兵力已恢复）")); }
            else { host.Warp(Beihai, 15, 18); Say(L(null, "胜败乃兵家常事。刘备一行退回官道口重整……"), L(null, "（金钱减半，兵力已恢复）")); }
        }

        public override void SkipToField()
        {
            if (!host.Party.Has(GameDatabase.ZhaoYun)) Join(GameDatabase.ZhaoYun, "yajiaoqiang");
            host.Party.FullRestore();
            host.Warp(Beihai, 15, 18);
        }

        // =====================================================================
        // 北海郊外：南边进来，北头是北海城门，管亥带两座营盘围城；东边大路去徐州
        // =====================================================================
        MapData BuildBeihai()
        {
            var m = new MapData
            {
                Id = "beihai_field", Name = "北海郊外", Art = "beihai_field",
                Grade = "#f2f4ea", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09, EnemyLevel = 19, ActorScale = 0.8f, Ground = ',', EnemyPool = HuangJinRemnant,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMtMMMMMMMMMMMMMM",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,T,,,,CCCC,,,=,,,CCCC,,,T,,M",
                    "M,,,,,,,CCCC,,,=,,,CCCC,,,,,,M",
                    "M,,,,,,,CCCC,,,=,,,CCCC,,,,,,M",
                    "M,,,,T,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,T,,,,,,M",
                    "M,,MM,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,MM,,,T,,,,,,=,,,,,,,,,,T,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,===============",
                    "M,,,,~~~,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,~~~~~,,,,,,=,,,,,T,,,,,,,M",
                    "M,,,,~~~,,,,,,,=,,,,,,,,,MM,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,MM,,M",
                    "M,T,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,T,,,,,=,,T,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,T,,,M",
                    "MMMMMMMMMMMMMMM=MMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "beihai_field";
            m.Emitters.Add(new MapEmitter("fire", 9.7f, 4.45f));
            m.Emitters.Add(new MapEmitter("fire", 20.7f, 4.45f));
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("duck", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestBh1, 2, 17, 0, "jinchuangyao"));
            m.Npcs.Add(Chest(F_ChestBh2, 27, 13, 0, "gangbian"));
            m.Npcs.Add(Chest(F_ChestBh3, 12, 16, 300));

            Action siege = GuanHaiFight;
            Func<bool> besieged = () => !flags.Has(F_GuanHai);
            m.Npcs.Add(Person("guanhai_npc", "管亥", "guanhai", "langya", 15, 1, siege, besieged));
            m.Npcs.Add(Person("bh_siege_a", "黄巾兵", "huangjin", "cha", 14, 2, siege, besieged));
            m.Npcs.Add(Person("bh_siege_b", "黄巾兵", "huangjin", "cha", 16, 2, siege, besieged));
            m.Npcs.Add(Person("kongrong", "孔融", "kongrong", null, 15, 2, () =>
                Say(L("孔融", "玄德公解北海之围，融感激不尽！"),
                    L("孔融", "方才徐州陶谦遣人来报：曹操父亲曹嵩在徐州境内被害，曹操兴兵报仇，徐州危在旦夕。"),
                    L("孔融", "陶恭祖是仁人君子，还望玄德公再走一趟。出东边大路，便是徐州。")), () => flags.Has(F_GuanHai)));
            m.Npcs.Add(Person("taishici", "太史慈", "taishici", "qiang", 14, 17, () =>
                Say(L("太史慈", flags.Has(F_GuanHai) ? "管亥已除，孔北海在城门口候着诸位。" : "管亥就在城下，带了两座营盘的人马。孔北海在城里快撑不住了，请诸位速速出手！"))));
            m.Npcs.Add(Walker("bh_refugee", "难民", "nanmin", null, 1.2f, new[] { 3, 16, 8, 16, 8, 17, 3, 17 }, null,
                "黄巾又回来了，这日子什么时候是个头……", "管亥说要孔太守借他一万石粮。", "东边大路通徐州，听说那边曹操在打仗。"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't') { Say(L(null, flags.Has(F_GuanHai) ? "北海城里的百姓正在清理战场，孔融已备下酒宴。" : "北海城门紧闭。")); return true; }
                if (x == 29 && y == 10)
                {
                    if (flags.Has(F_GuanHai)) host.Warp(Town, 14, 18);
                    else Say(L("关羽", "兄长，先解北海之围，再东去徐州。"));
                    return true;
                }
                if (x == 15 && y == 19) { Say(L("赵云", "主公，回平原的路远，北海的事还没了结。")); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;
                Say(L(null, "黄巾余党的营盘，比当年张角的还破。"));
                return true;
            };
            return m;
        }

        void GuanHaiFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("管亥", "孔融小儿不肯借粮，倒搬来几个救兵？"),
                L("关羽", "黄巾余孽，还敢猖狂！"),
            }, () => host.StartBattle(GameDatabase.Chapter4GuanHai(), true, () =>
            {
                BurnCamps();
                host.Say(new List<DialogueLine>
                {
                    L(null, "关羽纵马舞刀，数十合斩管亥于马下。黄巾余党四散而逃。"),
                    L(null, "北海城门大开，孔融亲自出城迎接。"),
                    L("孔融", "玄德公，徐州有急，容融细说……"),
                });
            }));
        }

        // =====================================================================
        // 徐州城（第四、五章共用底板）：州牧府在最上头，客栈在西、商铺在东，民居两排，南门出城
        // =====================================================================
        public static MapData XuzhouTown()
        {
            var m = new MapData
            {
                Id = "xuzhou", Name = "徐州", Art = "xuzhou",
                Grade = "#fff3e4", Weather = "leaf", EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T..........HHHHHHHHHH........T",
                    "T..........HHHHHHHHHH........T",
                    "T..HHHHH...HHHHHHHHHH...HHHH.T",
                    "T..HHHHH...HHHHHHHHHH...HHHH.T",
                    "T..HHHHH...HHHHHHHHHH...HHHH.T",
                    "T..HHHHH.....=======.........T",
                    "T...=........=.....=.....=...T",
                    "T...=========================T",
                    "T...=........==..........=...T",
                    "T...=..HHHHH.==..HHHHH...=...T",
                    "T...=..HHHHH.==..HHHHH...=...T",
                    "T...=..HHHHH.==..HHHHH...=...T",
                    "T...=........==..........=...T",
                    "T...=========================T",
                    "T.............==.............T",
                    "T..HHHHHH.....==.....HHHHHH..T",
                    "T..HHHHHH.....==.....HHHHHH..T",
                    "T..HHHHHH.....==.....HHHHHH..T",
                    "TTTTTTTTTTTTTTggTTTTTTTTTTTTTT",
                }
            };
            m.DecorFile = "xuzhou";
            m.Emitters.Add(new MapEmitter("smoke", 5.3f, 15.95f));
            m.Emitters.Add(new MapEmitter("smoke", 23.3f, 15.95f));
            m.Critters.Add(new CritterSpawn("chicken", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 5));
            m.Critters.Add(new CritterSpawn("butterfly", 2));
            return m;
        }

        MapData BuildTown()
        {
            var m = XuzhouTown();
            var flags = host.Flags;
            m.Npcs.Add(Chest(F_ChestTown1, 27, 1, 0, "qingjiu", 2));
            m.Npcs.Add(Chest(F_ChestTown2, 1, 17, 200));
            m.Npcs.Add(Person("xz_merchant", "掌柜", "shangren", null, 25, 6, () =>
                host.Say(new List<DialogueLine> { L("掌柜", "徐州是水陆码头，什么好货都有。钢鞭铁锏青锋剑，锁子甲，还有金创药，诸位随便看。") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Person("xz_guard", "徐州兵", "shouwei", "qiang", 13, 18, () =>
                Say(L("徐州兵", !flags.Has(F_TaoQian) ? "陶使君在州牧府等候，顺着大街一直往北。" : !flags.Has(F_JiLing) ? "纪灵的淮南兵扎在南边小沛一带，出南门往南走。" : !flags.Has(F_Boss) ? "吕布夺了下邳！出南门，东边大路尽头就是下邳。" : "使君……"))));
            m.Npcs.Add(Walker("xz_patrol", "巡街兵", "shouwei", "qiang", 0.9f, new[] { 14, 15, 14, 9 }, new[] { 14, 18 },
                "曹操退兵了，多亏玄德公一封书信。", "陶使君病重，三次要把徐州让给玄德公。", "吕布前些日子来投，使君让他屯在小沛。"));
            m.Npcs.Add(Walker("xz_woman", "村妇", "cunmin_nv", null, 1.0f, new[] { 6, 13, 12, 13 }, new[] { 9, 13 },
                "曹操的兵退了，总算能睡个安稳觉。", "糜家是徐州首富，糜竺先生最是仗义。", "听说刘使君的两个兄弟都是万人敌。"));
            m.Npcs.Add(Walker("xz_elder", "老人", "laoren", "guaizhang", 1.3f, new[] { 17, 13, 24, 13 }, new[] { 19, 13 },
                "陶使君是个好人，就是太老实。", "吕布那人反复无常，留他在小沛，迟早是祸。", "老朽年轻时去过洛阳，如今都烧成白地了。"));
            m.Npcs.Add(Walker("xz_kid", "小孩", "xiaohai", null, 0.7f, new[] { 3, 15, 10, 15 }, new[] { 5, 15 },
                "白袍叔叔的枪真好看！", "我长大要当赵将军！", "娘说曹操的兵不来了。"));
            m.Npcs.Add(Walker("xz_peddler", "货郎", "huolang", "biandan", 1.1f, new[] { 16, 9, 24, 9 }, new[] { 19, 9 },
                "海盐、鱼干，徐州特产！", "打仗归打仗，买卖还得做。", "南边淮南兵过来，货都不敢往南运了。"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 'g')
                {
                    if (flags.Has(F_TaoQian)) host.Warp(Field, 15, 1);
                    else Say(L("关羽", "兄长，陶使君还在府里等着。"));
                    return true;
                }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 5 && y == 6) { host.Warp(Inn, 4, 5); return true; }
                if (x == 15 && y == 5) { host.Warp(Fu, 5, 7); return true; }
                return false;
            };
            return m;
        }

        MapData BuildFu()
        {
            var flags = host.Flags;
            return BuildHall("xuzhou_fu", "州牧府", () => host.Warp(Town, 15, 6),
                Person("taoqian", "陶谦", "taoqian", null, 5, 3, () =>
                {
                    if (flags.Has(F_Boss)) { Say(L("陶谦", "吕布已除，徐州总算安稳了。玄德，曹操邀你去许都，去吧，老夫替你守着。")); return; }
                    if (flags.Has(F_JiLing)) { Say(L("陶谦", "纪灵刚退，吕布竟趁虚夺了下邳！玄德，老夫当初就不该收留他。"), L("陶谦", "出南门，东边大路尽头便是下邳。")); return; }
                    if (flags.Has(F_TaoQian)) { Say(L("陶谦", "袁术遣纪灵引兵十万犯境，屯在小沛一带。玄德，徐州就托付给你了。出南门往南走便是。")); return; }
                    flags.Set(F_TaoQian);
                    host.Party.Gold += 500;
                    host.Say(new List<DialogueLine>
                    {
                        L("陶谦", "玄德公一封书信，曹操竟真退兵了！老夫年迈多病，二子不才，这徐州，便让与玄德公吧。"),
                        L("刘备", "备何德何能，敢受此大郡？使君莫要再提。"),
                        L("陶谦", "玄德若不受，老夫死不瞑目！"),
                        L(null, "陶谦三让徐州，刘备再三推辞，只答应暂领州事。"),
                        L(null, "获得金钱 500！"),
                        L("糜竺", "东海糜竺，愿以家财助玄德公。"),
                        L("孙乾", "北海孙乾，字公祐，孔北海荐我来投。"),
                        L(null, "糜竺、孙乾加入队伍！"),
                        L("陶谦", "眼下袁术遣大将纪灵引兵犯境，屯在小沛一带。玄德，徐州就托付给你了。"),
                    }, () =>
                    {
                        Join(GameDatabase.MiZhu);
                        Join(GameDatabase.SunQian);
                    });
                }));
        }

        // =====================================================================
        // 徐州郊外：北边回城，西南角纪灵的淮南军大营，东头山口是下邳城门（吕布）
        // =====================================================================
        MapData BuildField()
        {
            var m = new MapData
            {
                Id = "xuzhou_field", Name = "徐州郊外", Art = "xuzhou_field",
                Grade = "#f0f3ea", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.1, EnemyLevel = 22, ActorScale = 0.8f, Ground = ',', EnemyPool = HuaiNan,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMtMMMMMMMMMMMMMM",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,T,,,,,,,,,,,=,,,,,T,,,,,,,M",
                    "M,,,,,,,,T,,,,,=,,,,,,,,,,T,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,~~~~,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,~~~~~~,,,,,,=,,,T,,,,,,,,,M",
                    "M,,,~~~~,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,MMMMM",
                    "M,,,,,,,T,,,,,,=,,,,,,,,,MMMMM",
                    "M,,,,,,,,,,,,,,===========,,gM",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,MMMMM",
                    "M,T,,,,,,,,,,,,,,,,,,,,,,MMMMM",
                    "M,,,,,,,,,,,,,,,,,T,,,,,,,,,,M",
                    "M,,,,CCCC,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,CCCC,,,,,,,,,,,,,,,T,,,,M",
                    "M,,,,CCCC,,,,,,,T,,,,,,,,,,,,M",
                    "M,,T,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,,,,,,,,,,,,,,,M",
                    "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "xuzhou_field";
            m.Emitters.Add(new MapEmitter("fire", 6.7f, 16.45f));
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("fish", 3));
            m.Critters.Add(new CritterSpawn("duck", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestF1, 27, 2, 0, "qingjiu", 2));
            m.Npcs.Add(Chest(F_ChestF2, 2, 18, 0, "suozijia"));
            m.Npcs.Add(Chest(F_ChestF3, 20, 17, 400));
            m.Npcs.Add(Chest(F_ChestF4, 12, 5, 0, "jinchuangyao"));
            m.Npcs.Add(Person("jiling_npc", "纪灵", "jiling", "cha", 9, 15, JiLingFight, () => !flags.Has(F_JiLing)));
            m.Npcs.Add(Person("lvbu_npc2", "吕布", "lvbu", "huaji", 27, 10, LvBuFight, () => flags.Has(F_JiLing) && !flags.Has(F_Boss)));
            m.Npcs.Add(Person("xzf_scout", "徐州哨兵", "shouwei", "qiang", 14, 2, () =>
                Say(L("徐州哨兵", !flags.Has(F_JiLing) ? "纪灵的淮南军在西南边扎营，那口三尖刀重五十斤。" : !flags.Has(F_Boss) ? "吕布夺了下邳，就在东边山口的城门前。使君小心，他那方天画戟……" : "下邳收复了！"))));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't') { host.Warp(Town, 14, 18); return true; }
                if (m.At(x, y) == 'g') { Say(L(null, "下邳城门紧闭。")); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;
                Say(L(null, flags.Has(F_JiLing) ? "淮南军的空营，人都退回淮南了。" : "淮南军的大营，纪灵的帅旗立在正中。"));
                return true;
            };
            return m;
        }

        void JiLingFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("纪灵", "刘备村夫，也敢占据徐州？袁公路大军十万在此，还不速速献城！"),
                L("张飞", "袁术算什么东西，俺先宰了你这个开路的！"),
            }, () => host.StartBattle(GameDatabase.Chapter4JiLing(), true, () =>
            {
                host.Flags.Set(F_JiLing);
                host.Say(new List<DialogueLine>
                {
                    L(null, "纪灵大败，丢了三尖刀，退回淮南。"),
                    L(null, "正要回城，糜竺飞马来报——"),
                    L("糜竺", "主公！吕布趁我军出征，袭取了下邳，家眷都在城中！"),
                    L("张飞", "那三姓家奴！俺早说留他不得！"),
                    L("刘备", "……东边大路尽头便是下邳。走！"),
                });
            }));
        }

        void LvBuFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("吕布", "玄德公，布不过是借你的城暂住，何必动怒？"),
                L("张飞", "三姓家奴！上回让你跑了，这回看你往哪儿逃！"),
                L("吕布", "环眼贼！屡次欺我，今日便与你决个生死！"),
            }, () => host.StartBattle(GameDatabase.Chapter4LvBu(), true, () =>
            {
                host.Flags.Set(F_Boss);
                Give("shoumiankai");
                host.Say(new List<DialogueLine>
                {
                    L(null, "吕布力竭败走，方天画戟、兽面吞头铠尽落刘备手中。"),
                    L(null, "此时曹操亦率大军赶到，吕布被部下缚于白门楼，身死。"),
                    L("曹操", "玄德公，此番讨平吕布，功劳不小。随我回许都，奏请天子封赏！"),
                    L("刘备", "……备遵命。"),
                    L("关羽", "兄长，曹操此人，不可不防。"),
                }, () => host.ChapterClear("第四章 徐州风云 · 完", "第五章：许都"));
            }));
        }
    }
}
