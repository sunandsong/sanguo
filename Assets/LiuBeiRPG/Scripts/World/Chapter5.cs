using System;
using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第五章「许都」（演义第二十～二十一回）：随曹操入许都，青梅煮酒、曹操赠赤兔、陈到来投 → 校场斗典韦（可选）→ 借讨袁术出走，许褚追赶 → 回徐州斩车胄。
    /// 等级段 26～32。徐州城沿用第四章底板（Chapter4.XuzhouTown）。
    /// Boss 等级和设计文档略有出入：剧情顺序是典韦(30) → 许褚(31) → 车胄(33)，让最后一仗最难。
    /// </summary>
    public class Chapter5 : Chapter
    {
        public const int InnCost = 250;

        public readonly MapData Town, Fu, Inn, Inn2F, Field, XuzhouField, Xuzhou, XuzhouFu;

        const string F_MeiJiu = "c5_meijiu", F_ChenDao = "c5_chendao", F_DianWei = "c5_dianwei", F_Leave = "c5_leave", F_XuChu = "c5_xuchu";
        public const string F_Boss = "c5_boss";
        const string F_ChestTown1 = "c5_chest_town1", F_ChestTown2 = "c5_chest_town2", F_ChestInn = "c5_chest_inn2f";
        const string F_ChestF1 = "c5_chest_f1", F_ChestF2 = "c5_chest_f2", F_ChestF3 = "c5_chest_f3", F_ChestF4 = "c5_chest_f4";

        // 许都商店 = 徐州 + 鱼鳞甲、止血散
        static readonly string[] ShopGoods = { "caoyao", "jinchuangyao", "zhixuesan", "qingjiu", "huanhundan", "langyan", "yanmudan", "nuqiwan", "feiniaoshan", "diangangqiang", "liuxingchui", "zhanmadao", "jiuzhang", "suozijia", "yulinjia", "junma", "pikui", "buxue" };
        static readonly GeneralDef[] CaoJun = { GameDatabase.CaoJunBuBing, GameDatabase.CaoJunBuBing, GameDatabase.CaoJunGongBing, GameDatabase.QingZhouBing, GameDatabase.QingZhouBing, GameDatabase.HuBaoQi };

        public Chapter5(IGameHost host) : base(host)
        {
            Town = BuildTown();
            Fu = BuildFu();
            var inn = BuildInn("xudu_inn", InnCost, () => host.Warp(Town, 4, 7), Chest(F_ChestInn, 3, 3, 0, "qingjiu", 3));
            Inn = inn[0]; Inn2F = inn[1];
            Field = BuildField();
            XuzhouField = BuildXuzhouField();
            Xuzhou = BuildXuzhou();
            XuzhouFu = BuildXuzhouFu();
        }

        public override int Number { get { return 5; } }
        public override MapData[] Maps { get { return new[] { Town, Fu, Inn, Inn2F, Field, XuzhouField, Xuzhou, XuzhouFu }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Town, X = 14, Y = 18 }, new TownSpot { Map = Xuzhou, X = 14, Y = 18 } }; } }

        public override void Begin()
        {
            host.Party.FullRestore();
            host.Warp(Field, 15, 18);   // 从南口进许都郊外（来路通徐州郊外下邳门）
            host.Say(new List<DialogueLine>
            {
                L(null, "刘备随曹操回到许都。曹操表奏天子，封刘备为左将军、宜城亭侯。"),
                L(null, "献帝认刘备为皇叔，曹操心中不悦，却也只好以礼相待。"),
                L("关羽", "兄长，许都是曹操的地方，咱们如在虎口。"),
                L("刘备", "韬光养晦，静待时机。先去相府拜见曹操吧。"),
                L("张飞", "哼，俺去街上逛逛，闷死了。"),
            });
        }

        public override void RespawnAfterDefeat()
        {
            host.Party.Gold /= 2;
            host.Party.FullRestore();
            host.Warp(Town, 4, 7);
            Say(L(null, "胜败乃兵家常事。刘备一行退回许都休整……"), L(null, "（金钱减半，兵力已恢复）"));
        }

        public override void SkipToField()
        {
            host.Flags.Set(F_MeiJiu);
            host.Flags.Set(F_Leave);
            host.Party.FullRestore();
            host.Warp(Field, 15, 1);
        }

        // =====================================================================
        // 许都城：相府在最上头正中，客栈西、商铺东，街两旁民居和市集，南门出城
        // =====================================================================
        MapData BuildTown()
        {
            var m = new MapData
            {
                Id = "xudu", Name = "许都", Art = "xudu",
                Grade = "#fff3e4", Weather = "leaf", EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T.........HHHHHHHHHHHH.......T",
                    "T.........HHHHHHHHHHHH.......T",
                    "T.HHHH....HHHHHHHHHHHH....HHHT",
                    "T.HHHH....HHHHHHHHHHHH....HHHT",
                    "T.HHHH....HHHHHHHHHHHH....HHHT",
                    "T.HHHH.........=.............T",
                    "T..=...........=.........=...T",
                    "T..==========================T",
                    "T..=..........==..........=..T",
                    "T..=.HHHHHH...==...HHHHHH.=..T",
                    "T..=.HHHHHH...==...HHHHHH.=..T",
                    "T..=.HHHHHH...==...HHHHHH.=..T",
                    "T..=..........==..........=..T",
                    "T..==========================T",
                    "T.............==.............T",
                    "T..HHHHH..S.S.==.S.S..HHHHH..T",
                    "T..HHHHH......==......HHHHH..T",
                    "T..HHHHH......==......HHHHH..T",
                    "TTTTTTTTTTTTTTggTTTTTTTTTTTTTT",
                }
            };
            m.DecorFile = "xudu";
            m.Emitters.Add(new MapEmitter("smoke", 4.3f, 15.95f));
            m.Emitters.Add(new MapEmitter("smoke", 24.3f, 15.95f));
            m.Critters.Add(new CritterSpawn("sparrow", 5));
            m.Critters.Add(new CritterSpawn("chicken", 3));
            m.Critters.Add(new CritterSpawn("butterfly", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestTown1, 27, 1, 500));
            m.Npcs.Add(Chest(F_ChestTown2, 1, 17, 0, "zhixuesan"));
            m.Npcs.Add(Person("xd_merchant", "掌柜", "shangren", null, 27, 6, () =>
                host.Say(new List<DialogueLine> { L("掌柜", "天子脚下，货色自然是最全的。鱼鳞甲、止血散，别处可买不到。") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Person("chendao", "陈到", "chendao", "qiang", 20, 13, () =>
            {
                if (flags.Has(F_ChenDao)) return;
                flags.Set(F_ChenDao);
                host.Say(new List<DialogueLine>
                {
                    L("陈到", "汝南陈到，字叔至。久闻皇叔仁德，愿效死力！"),
                    L("刘备", "叔至肯来，备之幸也。"),
                    L(null, "陈到加入队伍！"),
                }, () => Join(GameDatabase.ChenDao));
            }, () => !flags.Has(F_ChenDao)));
            m.Npcs.Add(Person("dongcheng", "董承", "dongcheng", null, 8, 13, () =>
                Say(L("董承", "（低声）皇叔……天子有密诏在此，衣带之中。曹操专权，还望皇叔日后……"), L("刘备", "（低声）国舅放心，备记下了。"))));
            m.Npcs.Add(Person("xd_guard", "许都兵", "shouwei", "qiang", 13, 18, () =>
                Say(L("许都兵", !flags.Has(F_MeiJiu) ? "曹丞相在相府等候皇叔，顺大街一直往北。" : !flags.Has(F_Leave) ? "城外校场在南门外西边，典将军天天在那儿练兵。" : "皇叔要出征？出南门，东边大路去徐州。"))));
            m.Npcs.Add(Walker("xd_patrol", "巡街兵", "shouwei", "qiang", 0.9f, new[] { 14, 15, 14, 9 }, new[] { 14, 18 },
                "丞相有令，城中不得夜行。", "许田打围那天，丞相竟替天子受了百官的拜。", "关将军那把刀，比典将军的戟还长。"));
            m.Npcs.Add(Walker("xd_woman", "村妇", "cunmin_nv", null, 1.0f, new[] { 5, 13, 12, 13 }, new[] { 7, 13 },
                "皇叔在后园种菜，倒是个安分人。", "相府天天有宴，鼓乐声半城都听得见。", "典将军一只手举起牙门旗，吓死人。"));
            m.Npcs.Add(Walker("xd_elder", "老人", "laoren", "guaizhang", 1.3f, new[] { 17, 13, 24, 13 }, new[] { 20, 13 },
                "天子虽在许都，说话的却是丞相。", "老朽从洛阳逃来的，那场火啊……", "董国舅这几日常往皇叔宅里去。"));
            m.Npcs.Add(Walker("xd_kid", "小孩", "xiaohai", null, 0.7f, new[] { 3, 15, 9, 15 }, new[] { 5, 15 },
                "红脸叔叔的马是赤色的！", "我见过丞相，坐在高头大马上。", "许将军能举起大石头！"));
            m.Npcs.Add(Walker("xd_peddler", "货郎", "huolang", "biandan", 1.1f, new[] { 16, 9, 24, 9 }, new[] { 20, 9 },
                "青梅、青梅，新摘的青梅！", "许都城里，什么都比别处贵一倍。", "北边袁绍、南边袁术，就许都太平。"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 'g')
                {
                    if (flags.Has(F_MeiJiu)) host.Warp(Field, 15, 1);
                    else Say(L("关羽", "兄长，先去相府拜见曹操，不然他要起疑。"));
                    return true;
                }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 4 && y == 6) { host.Warp(Inn, 4, 5); return true; }
                if (x == 15 && y == 5) { host.Warp(Fu, 5, 7); return true; }
                return false;
            };
            return m;
        }

        MapData BuildFu()
        {
            var flags = host.Flags;
            return BuildHall("xudu_fu", "相府", () => host.Warp(Town, 15, 6),
                Person("caocao2", "曹操", "caocao", "jian", 5, 3, () =>
                {
                    if (flags.Has(F_Leave)) { Say(L("曹操", "玄德去截袁术，操拨五万兵与你。速去速回。")); return; }
                    if (flags.Has(F_MeiJiu))
                    {
                        flags.Set(F_Leave);
                        Say(L("曹操", "玄德来得正好。袁术在淮南待不住了，欲往河北投袁绍，必经徐州。"),
                            L("曹操", "你熟悉徐州，便替操去截他一截吧。"),
                            L("刘备", "备领命，这便动身。"),
                            L(null, "（刘备心中暗喜：此一去，如鱼入大海、鸟上青霄，再不受笼网之羁绊。）"),
                            L("曹操", "……许褚，你去送送皇叔。"));
                        return;
                    }
                    flags.Set(F_MeiJiu);
                    host.Say(new List<DialogueLine>
                    {
                        L("曹操", "玄德，在家做得好大事啊！"),
                        L("刘备", "（大惊）……"),
                        L("曹操", "哈哈，学种菜不易啊。后园青梅正熟，煮酒一樽，你我小亭一叙。"),
                        L(null, "酒至半酣，天边龙挂。"),
                        L("曹操", "龙能大能小，能升能隐……玄德久历四方，当今天下英雄，有谁？"),
                        L("刘备", "淮南袁术？河北袁绍？刘表、孙策、刘璋……"),
                        L("曹操", "皆碌碌小人！夫英雄者，胸怀大志，腹有良谋。今天下英雄，惟使君与操耳！"),
                        L(null, "刘备手中匕箸，落于地下。恰好雷声大作。"),
                        L("刘备", "一震之威，乃至于此……"),
                        L("曹操", "哈哈哈，丈夫亦畏雷乎？来，云长，吕布的赤兔马，操留着也无用，便赠与你。"),
                        L(null, "获得赤兔！"),
                    }, () => Give("chitu"));
                }));
        }

        // =====================================================================
        // 许都郊外：北边回城，西边校场典韦练兵（可选单挑），东头山口许褚拦路，过了山口是去徐州的大路
        // =====================================================================
        MapData BuildField()
        {
            var m = new MapData
            {
                Id = "xudu_field", Name = "许都郊外", Art = "xudu_field",
                Grade = "#f2f4ea", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09, EnemyLevel = 29, ActorScale = 0.8f, Ground = ',', EnemyPool = CaoJun,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMtMMMMMMMMMMMMMM",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,T,,,,,,,,,,,=,,,,,,T,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,T,,,=,,,,,,,,,T,,,M",
                    "M,,,,CCCC,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,CCCC,,,,,,=,,,,~~~,,,,,,M",
                    "M,,,,CCCC,,,,,,=,,,~~~~~,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,~~~,,,,,,M",
                    "M,,T,,,,,,,,,,,=,,,,,,,,,MMMMM",
                    "M,,,,,,,,,,,,,,===============",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,MMMMM",
                    "M,T,,,,,,,,,T,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,T,,,,,,,,,,,M",
                    "M,,,MM,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,MM,,,,,,T,,=,,,,,,,,T,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,T,,,,,,=,,,,,,,,,,,,,M",
                    "MMMMMMMMMMMMMMM=MMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "xudu_field";
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("fish", 3));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestF1, 27, 2, 500));
            m.Npcs.Add(Chest(F_ChestF2, 2, 18, 0, "yulinjia"));
            m.Npcs.Add(Chest(F_ChestF3, 12, 17, 0, "zhixuesan"));
            m.Npcs.Add(Chest(F_ChestF4, 22, 13, 0, "huanhundan"));
            m.Npcs.Add(Person("dianwei_npc", "典韦", "dianwei", "ji", 9, 6, DianWeiDuel, () => !flags.Has(F_DianWei)));
            m.Npcs.Add(Person("xuchu_npc", "许褚", "xuchu", "zhanchui", 26, 10, XuChuFight, () => flags.Has(F_Leave) && !flags.Has(F_XuChu)));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't') { host.Warp(Town, 14, 18); return true; }
                if (x == 29 && y == 10) { Say(L("关羽", "兄长，东边是山间小道。去徐州走南边大路。")); return true; }
                if (x == 15 && y == 19) { host.Warp(XuzhouField, 27, 10); return true; }   // 南口 ↔ 徐州郊外下邳门
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) == 'C') { Say(L(null, "曹军校场，兵器架上插着一排长戟。")); return true; }
                return false;
            };
            return m;
        }

        void DianWeiDuel()
        {
            host.Choose("典韦：「皇叔麾下猛将如云，可敢与典韦较量一场？点到为止。」", new[] { "比试", "改日" }, i =>
            {
                if (i != 0) { Say(L("典韦", "哈哈，皇叔随时来。")); return; }
                host.StartBattle(GameDatabase.Chapter5DianWei(), true, () =>
                {
                    host.Flags.Set(F_DianWei);
                    Say(L("典韦", "好身手！这对铁戟就送给诸位，权当见面礼。"), L(null, "典韦拱手而去。"));
                });
            });
        }

        void XuChuFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("许褚", "皇叔留步！丞相有命，请皇叔回府再议。"),
                L("刘备", "将在外，君命有所不受。吾奉天子之诏，丞相之命，岂能半路折回？"),
                L("张飞", "许胖子，想拦路就先问问俺的蛇矛！"),
                L("许褚", "既如此，褚只好硬请了！"),
            }, () => host.StartBattle(GameDatabase.Chapter5XuChu(), true, () =>
            {
                host.Flags.Set(F_XuChu);
                host.Say(new List<DialogueLine>
                {
                    L(null, "许褚大战张飞数十合，不分胜负，终究拦不住，只得回去复命。"),
                    L("关羽", "兄长，曹操反悔了。咱们快去徐州，不可耽搁。"),
                });
            }));
        }

        // =====================================================================
        // 徐州郊外（第五章）：和第四章同一张图。北口进徐州城，西口回北海（第四章），下邳门通许都郊外南口，
        // 南边大路杀车胄后离开徐州 → 第六章汝南郊外
        // =====================================================================
        MapData BuildXuzhouField()
        {
            var m = Chapter4.XuzhouFieldBase();
            m.EnemyLevel = 28;   // 第五章的等级段（袁术残兵）
            var flags = host.Flags;
            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't') { host.Warp(Xuzhou, 14, 18); return true; }
                if (x == 0 && y == 10) { host.GoChapter(4, "beihai_field", 28, 10); return true; }   // 西口 ↔ 北海东口（第四章）
                if (m.At(x, y) == 'g') { host.Warp(Field, 15, 18); return true; }   // 下邳门 ↔ 许都郊外南口
                if (x == 15 && y == 19) { if (flags.Has(F_Boss)) Cross(6, "runan_field", 15, 18); else Say(L("关羽", "兄长，先进徐州城，车胄那厮还在州牧府。")); return true; }   // 南边大路 → 第六章
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;
                Say(L(null, "淮南军留下的空营，栅栏倒了一半。"));
                return true;
            };
            return m;
        }

        // =====================================================================
        // 徐州（第五章）：车胄暗受曹操之命，在州牧府等刘备自投罗网
        // =====================================================================
        MapData BuildXuzhou()
        {
            var m = Chapter4.XuzhouTown();
            var flags = host.Flags;
            m.Npcs.Add(Person("xz_merchant2", "掌柜", "shangren", null, 25, 6, () =>
                host.Say(new List<DialogueLine> { L("掌柜", "使君回来了！小店还是老样子，诸位看看。") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Person("xz_guard2", "徐州兵", "shouwei", "qiang", 13, 18, () =>
                Say(L("徐州兵", flags.Has(F_Boss) ? "车胄死了，徐州又是使君的了！" : "车刺史在州牧府……使君小心，他这几日调了好些兵进府。"))));
            m.Npcs.Add(Walker("xz_patrol2", "巡街兵", "shouwei", "qiang", 0.9f, new[] { 14, 15, 14, 9 }, new[] { 14, 18 },
                "车刺史是曹丞相派来的。", "使君回来了，大家都盼着呢。", "府里这几日戒备得紧，不知要干什么。"));
            m.Npcs.Add(Walker("xz_woman2", "村妇", "cunmin_nv", null, 1.0f, new[] { 6, 13, 12, 13 }, new[] { 9, 13 },
                "车刺史来了以后，税又重了。", "使君家眷还在城里，车刺史没敢动。", "红脸的关将军，一看就不是好惹的。"));
            m.Npcs.Add(Walker("xz_elder2", "老人", "laoren", "guaizhang", 1.3f, new[] { 17, 13, 24, 13 }, new[] { 19, 13 },
                "陶使君去了，徐州就没安生过。", "曹操派来的人，没一个好东西。", "使君这回回来，怕是不走了吧？"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 'g') { host.Warp(XuzhouField, 15, 1); return true; }   // 城门 ↔ 徐州郊外北口
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 5 && y == 6) { Say(L("店小二", "使君！车刺史征了小店做兵营，今日不能住店，对不住……")); return true; }
                if (x == 15 && y == 5) { host.Warp(XuzhouFu, 5, 7); return true; }
                return false;
            };
            return m;
        }

        MapData BuildXuzhouFu()
        {
            var flags = host.Flags;
            return BuildHall("xuzhou_fu5", "州牧府", () => host.Warp(Xuzhou, 15, 6),
                Person("chezhou_npc", "车胄", "chezhou", "dao", 5, 3, CheZhouFight, () => !flags.Has(F_Boss)));
        }

        void CheZhouFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("车胄", "刘使君回来了？胄……胄已备下酒宴。"),
                L("关羽", "酒宴？这满府的刀斧手，也是给我兄长备的？"),
                L(null, "帐后伏兵尽出——车胄受曹操密令，要害刘备！"),
                L("车胄", "拿下刘备，丞相重赏！"),
            }, () => host.StartBattle(GameDatabase.Chapter5CheZhou(), true, () =>
            {
                host.Flags.Set(F_Boss);
                host.Say(new List<DialogueLine>
                {
                    L(null, "关羽一刀斩车胄于堂前。刘备重据徐州，杀车胄的消息传到许都，曹操大怒，亲率二十万大军来攻。"),
                    L(null, "刘备兵少，兵败小沛，只得北投袁绍。"),
                    L(null, "关羽困守下邳，为保刘备家眷，暂降曹操；张飞败走芒砀山，不知去向。"),
                    L("刘备", "两位贤弟……备对不住你们。"),
                    L("赵云", "主公，兄弟终有相会之日。此刻先保全性命，再图后计。"),
                    L(null, "（出徐州城，从徐州郊外南边的大路离开）"),
                });
            }));
        }
    }
}
