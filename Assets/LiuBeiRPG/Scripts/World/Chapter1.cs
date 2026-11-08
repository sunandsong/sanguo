using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第一章「桃园结义」：涿县城内剧情 → 出城练级 → 黄巾营寨 Boss。
    /// 每一章都按这个模板写：地图字符串 + NPC + 剧情标记 + Boss。
    /// </summary>
    public class Chapter1 : Chapter
    {
        public const int InnCost = 20;

        public readonly MapData Town;
        public readonly MapData Field;
        public readonly MapData Home;
        public readonly MapData Tavern;
        public readonly MapData Inn;
        public readonly MapData Inn2F;

        // 剧情标记
        const string F_Notice = "c1_notice";
        const string F_MetZhangFei = "c1_zhangfei";
        const string F_MetGuanYu = "c1_guanyu";
        const string F_Oath = "c1_oath";
        const string F_Gift = "c1_gift";
        public const string F_Boss = "c1_boss";
        const string F_Liutao = "c1_liutao";   // 母亲给了《六韬》
        const string F_ChestHome = "c1_chest_home", F_ChestInn = "c1_chest_inn2f";   // 宝箱开过了
        const string F_ChestFieldNE = "c1_chest_field_ne", F_ChestFieldE = "c1_chest_field_e";
        // 支线：黄巾换赏（接了 / 交了）
        const string Q_HuangJin = "c1_q_huangjin", Q_HuangJinDone = "c1_q_huangjin_done";
        const int HuangJinNeed = 10;

        // 涿县商店（设计文档 8.3）
        static readonly string[] ShopGoods = { "caoyao", "zhuojiu", "langyan", "yanmudan", "nuqiwan", "feiniaoshan", "qimeigun", "tiejian", "huanshoudao", "changqiang", "shoufu", "pikui", "pijia", "buxue", "numa", "yupei" };

        public Chapter1(IGameHost host) : base(host)
        {
            Town = BuildTown();
            Field = BuildField();
            Home = BuildHome();
            Tavern = BuildTavern();
            var inn = BuildInn("inn", InnCost, () => host.Warp(Town, 16, 10), Chest(F_ChestInn, 3, 3, 80));   // 二楼矮桌右边的地板上
            Inn = inn[0];
            Inn2F = inn[1];
        }

        public override int Number { get { return 1; } }
        public override MapData[] Maps { get { return new[] { Town, Field, Home, Tavern, Inn, Inn2F }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Town, X = 8, Y = 12 } }; } }

        public int StartX { get { return 5; } }
        public int StartY { get { return 5; } }

        void GuardStop()
        {
            Say(L("守卫", "城外黄巾贼出没，你一个人出去就是送死。"), L("守卫", "要出城，先招些人马再说吧。"));
        }

        public override void Begin()
        {
            host.Party.Add(new General(GameDatabase.LiuBei));
            host.Party.Gold = 50;
            host.Warp(Town, StartX, StartY);
            host.Say(new List<DialogueLine>
            {
                L(null, "东汉末年，朝政腐败，天下大乱。"),
                L(null, "巨鹿人张角聚众起事，头裹黄巾，号称“黄巾军”，四方响应。"),
                L(null, "幽州涿郡涿县，有一人姓刘名备，字玄德，乃汉室宗亲，以织席贩履为生。"),
                L("刘备", "听说城里贴出了招兵的榜文……去城门口看看吧。"),
            });
        }

        /// <summary>全灭后：回到客栈门口，兵力全满，金钱减半。</summary>
        public override void RespawnAfterDefeat()
        {
            host.Party.Gold /= 2;
            host.Party.FullRestore();
            host.Warp(Town, 16, 10);
            Say(L(null, "胜败乃兵家常事。刘备一行退回涿县休整……"),
                L(null, "（金钱减半，兵力已恢复）"));
        }

        // =====================================================================
        // 涿县
        // =====================================================================
        MapData BuildTown()
        {
            var m = new MapData
            {
                Id = "zhuoxian",
                Name = "涿县",
                Art = "zhuoxian_ground",
                Grade = "#fff6ec", Weather = "petal",
                EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTT",
                    "TTTHHHHTTTTTTTPTTPTT",
                    "T..HHHH......fffff.T",
                    "T..HHHH.....PffSSf.T",
                    "T....=.......fffffPT",
                    "T....=......P..==..T",
                    "T....============..T",
                    "T.HHHH..==....HHHHHT",
                    "T.HHHH..==....HHHHHT",
                    "T.HHHH..==....HHHHHT",
                    "T.......==.........T",
                    "TTTTTTT.==.TTTTTTTTT",
                    "TTTTTTTT==TTTTTTTTTT",
                    "TTTTTTTTggTTTTTTTTTT",   // 城门两格都能出城
                }
            };
            // 底图只有地面；房子、树、桃园、灯笼都在物件清单里（Resources/Map/Decor/zhuoxian_ground.txt）
            m.DecorFile = "zhuoxian_ground";
            // 活气：刘备家烟囱冒炊烟，院子里有鸡，草地上有麻雀和蝴蝶
            m.Emitters.Add(new MapEmitter("smoke", 4.3f, 0.95f));
            // 路人：城里地方小，只放两个——扛锄头的农夫、挎篮的村妇（引擎里还有 shangren、巡街兵 shouwei、老人 laoren、小孩 xiaohai 可换）
            m.Npcs.Add(Walker("farmer", "农夫", "cunmin_nan", "chutou", 1.0f, new[] { 10, 7, 12, 7, 12, 9, 10, 9 }, new[] { 9, 13 },   // 家在城外，天黑出城。客栈门口只有第 10 行一条窄道，(13,10) 是唯一拐进去的口子，别在那儿逛
                "听说东边村子被黄巾烧了，造孽啊。", "今年收成不好，地里的苗都旱蔫了。", "我那块地就在城外，如今都不敢去了。"));
            m.Npcs.Add(Walker("villager_f", "村妇", "cunmin_nv", null, 1.0f, new[] { 7, 2, 11, 2, 11, 4, 7, 4 }, new[] { 3, 10 },   // 住酒馆后头
                "桃园的花开得正好，城东北那片。", "我家那口子也想去投军，我拦不住啊。", "米价又涨了，这日子……"));
            m.Critters.Add(new CritterSpawn("chicken", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 3));
            m.Critters.Add(new CritterSpawn("butterfly", 2));
            m.Critters.Add(new CritterSpawn("butterfly2", 2));

            var flags = host.Flags;

            m.Npcs.Add(new Npc
            {
                Id = "notice", Name = "榜文", Glyph = "榜", Sprite = "Art/notice_board", Color = "#f5e6b0", X = 10, Y = 10,
                Talk = () =>
                {
                    Say(L(null, "榜文：黄巾作乱，侵掠州郡。幽州太守刘焉招募义兵，共讨贼寇。"));
                    if (!flags.Has(F_Notice))
                    {
                        flags.Set(F_Notice);
                        Say(L("刘备", "（长叹）……可恨我势单力薄，空有报国之心。"),
                            L(null, "身后忽然有人厉声说道——"),
                            L("？？？", "大丈夫不与国家出力，在这里叹什么气！"),
                            L(null, "回头一看，那人已经大步走向了酒馆。"));
                    }
                }
            });

            m.Npcs.Add(new Npc
            {
                // 站在酒馆右边的空地上；别放第 10 行，酒馆门口到大路只有第 10 行这一条道，站上去就把人堵在门口
                Id = "guanyu", Name = "关羽", Glyph = "关", Color = GameDatabase.GuanYu.Color, X = 7, Y = 9,
                Visible = () => flags.Has(F_MetZhangFei) && !flags.Has(F_Oath),
                Talk = () =>
                {
                    if (!flags.Has(F_MetGuanYu))
                    {
                        flags.Set(F_MetGuanYu);
                        Say(L("关羽", "某姓关名羽，字云长，河东解良人。因本地豪强仗势欺人，被我杀了，逃难江湖五六年。"),
                            L("关羽", "听闻此处招军破贼，特来应募。"),
                            L("刘备", "玄德正有此志，愿与二位共图大事。"),
                            L("刘备", "翼德庄后有一座桃园，花开正盛。明日便在园中祭告天地，你我三人结为兄弟！"),
                            L(null, "（前往城东北的桃园）"));
                        return;
                    }
                    Say(L("关羽", "兄长，桃园就在城东北。"));
                }
            });

            m.Npcs.Add(new Npc
            {
                Id = "shangren", Name = "张世平", Glyph = "商", Skin = "huolang", Weapon = "biandan", Color = "#e0e0e0", X = 11, Y = 5,
                Visible = () => flags.Has(F_Oath),
                Talk = () =>
                {
                    if (flags.Has(F_Gift))
                    {
                        host.Say(new List<DialogueLine> { L("张世平", "小号也备了些刀枪药酒，诸位尽管挑。") }, () => host.OpenShop(ShopGoods));
                        return;
                    }
                    flags.Set(F_Gift);
                    host.Party.Gold += 300;
                    foreach (var g in host.Party.Members) { var w = ItemDb.Signature(g.Def.Id); if (w != null) g.Weapon = w.Id; }
                    host.Party.Items.Add("wuzi");
                    Say(L("张世平", "在下中山商人张世平。听说三位要讨贼保民，这点心意请务必收下。"),
                        L(null, "获得金钱 300！"),
                        L(null, "以赠送的镔铁打造兵器：刘备「双股剑」、关羽「青龙偃月刀」、张飞「丈八蛇矛」。"),
                        L(null, "全员武力 +5！"),
                        L("张世平", "另有兵书一卷，行军打仗或许用得上。"),
                        L(null, "获得兵书《吴子》！（菜单 → 物品 里研读）"),
                        L("张世平", "小号随车带了些刀枪药酒，诸位缺什么，再来找我便是。"));
                }
            });

            m.Npcs.Add(new Npc
            {
                // 站在城门里头的路中间、正对城门，贴着门楼；路有两格宽，(9,11) 还能过。结义前往城门走由他拦下（见下面 OnStep）
                Id = "shouwei", Name = "守卫", Glyph = "卫", Color = "#c0c0c0", X = 8, Y = 11, Weapon = "qiang",
                Visible = () => !flags.Has(F_Oath),
                Talk = GuardStop
            });

            // ---- 支线：黄巾换赏。客栈左边的空地上站着县衙差役（出城那条路一眼看得到，四周没有树挡着），收黄巾，十条换赏钱和一副皮甲 ----
            var qHuangJin = new Quest
            {
                Id = "huangjin", Name = "黄巾换赏", Giver = "县衙差役",
                Steps = new[] { "打黄巾贼，收集 " + HuangJinNeed + " 条「黄巾」", "回涿县客栈左边，把黄巾交给县衙差役" },
                Reward = "金钱 150、皮甲",
                Stage = () => flags.Has(Q_HuangJinDone) ? -1 : !flags.Has(Q_HuangJin) ? 0 : host.Party.Count("huangjinbu") >= HuangJinNeed ? 2 : 1,
                Progress = () => "黄巾 " + System.Math.Min(host.Party.Count("huangjinbu"), HuangJinNeed) + "/" + HuangJinNeed,
            };
            Quests.Add(qHuangJin);
            m.Npcs.Add(new Npc
            {
                Id = "yayi", Name = "县衙差役", Glyph = "差", Skin = "shouwei", X = 12, Y = 9,
                Visible = () => flags.Has(F_Oath),   // 结义之后才在城里贴赏格
                Mark = () => qHuangJin.Stage() == 0 ? "！" : qHuangJin.Stage() == 2 ? "？" : null,
                Talk = () =>
                {
                    int stage = qHuangJin.Stage(), have = host.Party.Count("huangjinbu");
                    if (stage == 0)
                    {
                        host.Say(new List<DialogueLine>
                        {
                            L("县衙差役", "县令有赏格：黄巾作乱，凡斩贼者，扯下贼人头上的黄巾为证。"),
                            L("县衙差役", "每凑够 " + HuangJinNeed + " 条，来这儿换赏钱，另有一副库里的皮甲。"),
                        }, () => host.Choose("接下这桩差事？", new[] { "接下", "再说吧" }, i =>
                        {
                            if (i != 0) { Say(L("县衙差役", "想好了再来。")); return; }
                            flags.Set(Q_HuangJin);
                            Say(L("刘备", "此事交给我等。"), L(null, "接受支线「黄巾换赏」。（菜单 → 人物 → 任务 可查看）"));
                        }));
                    }
                    else if (stage == 1)
                        Say(L("县衙差役", "黄巾凑了 " + have + " 条，还差 " + (HuangJinNeed - have) + " 条。城外草地上黄巾贼多的是。"));
                    else if (stage == 2)
                    {
                        var party = host.Party;   // 读档会换一个新 Party，每次现取
                        for (int i = 0; i < HuangJinNeed; i++) party.Items.Remove("huangjinbu");
                        party.Gold += 150;
                        bool fit = party.CanHold("pijia");
                        if (fit) party.Items.Add("pijia");
                        flags.Set(Q_HuangJinDone);
                        Say(L("县衙差役", "好！" + HuangJinNeed + " 条，一条不少。诸位真是为民除害。"),
                            L(null, "完成支线「黄巾换赏」！获得金钱 150" + (fit ? "、皮甲" : "（行囊满了，皮甲没拿上）") + "。"));
                    }
                    else Say(L("县衙差役", "多亏诸位，城外太平多了。县里百姓都念着你们的好。"));
                }
            });

            m.OnStep = (x, y) =>
            {
                // 城门：出城
                if (m.At(x, y) == 'g')
                {
                    if (!flags.Has(F_Oath)) { GuardStop(); return true; }
                    host.Warp(Field, 8, 1);
                    return true;
                }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                // 桃园石桌：走到桌前往上撞，三结义
                if ((x == 15 || x == 16) && y == 3 && flags.Has(F_MetGuanYu) && !flags.Has(F_Oath)) { Oath(x, y + 1); return true; }
                // 刘备家门（门口 (5,4) 往上撞）
                if (x == 5 && y == 3) { host.Warp(Home, 6, 7); return true; }
                // 酒馆门（门口 (3,10) 往上撞）
                if (x == 3 && y == 9) { host.Warp(Tavern, 6, 7); return true; }
                // 客栈门（门口 (15,10)/(16,10) 往上撞）
                if ((x == 15 || x == 16) && y == 9) { host.Warp(Inn, 4, 5); return true; }
                return false;
            };

            return m;
        }

        // =====================================================================
        // 刘备家（室内，下边敞口，往下走出画面就出门）
        // =====================================================================
        MapData BuildHome()
        {
            var m = new MapData
            {
                Id = "liubei_home",
                Name = "刘备家",
                Art = "liubei_home",
                Grade = "#fff0dc", Light = "#e8d6c0",
                ArtMargin = 0,
                Rows = new[]
                {
                    "HHHHHHHHHHHHH",
                    "HHHHHHHHHHHHH",
                    "HHH.HHHHH.HHH",
                    "HHH........HH",
                    "HHH........HH",
                    "HH.........HH",
                    "H...........H",
                    ".............",
                }
            };
            var flags = host.Flags;

            m.Npcs.Add(new Npc
            {
                Id = "muqin", Name = "母亲", Glyph = "母", Color = "#f2c6a0", X = 5, Y = 3,
                Talk = () =>
                {
                    if (!flags.Has(F_Oath))
                        Say(L("母亲", "玄德，你常说要匡扶汉室。如今天下大乱，正是男儿立身之时。"),
                            L("母亲", "城门口贴了榜文，去看看吧。"));
                    else if (!flags.Has(F_Liutao))
                    {
                        flags.Set(F_Liutao);
                        host.Party.Items.Add("liutao");
                        Say(L("母亲", "有云长、翼德两位兄弟相助，为娘就放心了。"),
                            L("母亲", "这卷《六韬》是你祖上中山靖王传下来的，带在身边吧。"),
                            L(null, "获得兵书《六韬》！（菜单 → 物品 里研读）"));
                    }
                    else
                        Say(L("母亲", "有云长、翼德两位兄弟相助，为娘就放心了。"),
                            L("母亲", "出城后若是兵马疲惫，记得回客栈歇息。"));
                }
            });

            m.Npcs.Add(Chest(F_ChestHome, 10, 4, 80));   // 灶台和水缸之间

            m.OnTouch = (x, y) =>
            {
                if (y >= m.Height) { host.Warp(Town, 5, 4); return true; }
                return false;
            };

            return m;
        }

        // =====================================================================
        // 酒馆（室内，下边敞口；前景层是下边那几根柱子）
        // =====================================================================
        MapData BuildTavern()
        {
            var m = new MapData
            {
                Id = "tavern",
                Name = "酒馆",
                Art = "tavern",
                Grade = "#ffe8c8", Light = "#e6cfae",
                ArtMargin = 0,
                Rows = new[]
                {
                    "HHHHHHHHHHHHH",
                    "HHHHHHHHHHHHH",
                    "HHHHHHHHHHHHH",
                    "HHH.......HHH",
                    "HHH.......HHH",
                    "HHH........HH",
                    "HHH..........",
                    "HHHH.....H...",
                }
            };
            var flags = host.Flags;

            m.Npcs.Add(new Npc
            {
                Id = "zhangfei", Name = "张飞", Glyph = "张", Color = GameDatabase.ZhangFei.Color, X = 3, Y = 4,
                Visible = () => !flags.Has(F_Oath),
                Talk = () =>
                {
                    if (!flags.Has(F_Notice))
                    {
                        Say(L("大汉", "看什么看？俺在等酒！"));
                        return;
                    }
                    if (!flags.Has(F_MetZhangFei))
                    {
                        flags.Set(F_MetZhangFei);
                        Say(L("张飞", "俺姓张名飞，字翼德，世居涿郡，卖酒屠猪，专好结交天下豪杰。"),
                            L("刘备", "在下刘备，乃汉室宗亲。欲破贼安民，只恨力不能及。"),
                            L("张飞", "俺颇有些家资，正好招募乡勇，与你同举大事，如何？"),
                            L("刘备", "如此甚好！"),
                            L("张飞", "来，先喝一杯！……咦，门外推车的那条红脸大汉，好生威风。"));
                        return;
                    }
                    Say(L("张飞", "那红脸的汉子看着不凡，哥哥快去跟他说说话！"));
                }
            });

            m.OnTouch = (x, y) =>
            {
                if (y >= m.Height) { host.Warp(Town, 3, 10); return true; }
                return false;
            };

            return m;
        }

        /// <summary>调试用：跳过城内剧情（结义、受赠都算完成），直接站到城外。</summary>
        public override void SkipToField()
        {
            var flags = host.Flags;
            if (!flags.Has(F_Oath))
            {
                flags.Set(F_Oath);
                host.Party.Add(new General(GameDatabase.GuanYu));
                host.Party.Add(new General(GameDatabase.ZhangFei));
                host.Party.Gold += 100;
            }
            if (!flags.Has(F_Gift))
            {
                flags.Set(F_Gift);
                host.Party.Gold += 300;
                foreach (var g in host.Party.Members) { var w = ItemDb.Signature(g.Def.Id); if (w != null) g.Weapon = w.Id; }
                host.Party.Items.Add("wuzi");
            }
            flags.Set(F_Notice);
            flags.Set(F_MetZhangFei);
            flags.Set(F_MetGuanYu);
            host.Party.FullRestore();
            host.Warp(Field, 8, 1);
        }

        void Oath(int x, int y)
        {
            var flags = host.Flags;
            flags.Set(F_Oath);
            host.Party.Add(new General(GameDatabase.GuanYu));
            host.Party.Add(new General(GameDatabase.ZhangFei));
            host.PlaceFollowers("back", x - 1, y, x + 1, y);   // 三人并排站在石桌前（刘备在中间），一起对着桌子拜
            host.Party.Gold += 100;
            host.Party.FullRestore();
            host.SetHour(8f);
            host.Say(new List<DialogueLine>
            {
                L(null, "次日，桃园之中，备下乌牛白马，三人焚香再拜。"),
                L("刘备", "念刘备、关羽、张飞，虽然异姓，既结为兄弟，则同心协力，救困扶危；"),
                L("关羽", "上报国家，下安黎庶。"),
                L("张飞", "不求同年同月同日生，但愿同年同月同日死！"),
                L(null, "三人拜玄德为兄，关羽次之，张飞为弟。"),
                L(null, "张飞散尽家财，招募乡勇三百余人。"),
                L(null, "关羽、张飞加入队伍！获得金钱 100。"),
                L("张飞", "大哥，听说有个叫张世平的商人在城里，想见见我们。"),
                L("关羽", "城门已开。黄巾贼的营寨就在城外东南，兄长，出发吧。"),
            });
        }

        // =====================================================================
        // 涿郡郊外
        // =====================================================================
        MapData BuildField()
        {
            var m = new MapData
            {
                Id = "zhuojun_field",
                Name = "涿郡郊外",
                Art = "field_big",
                Grade = "#f0f5ff", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09,
                EnemyLevel = 2,   // 第一章等级段 1～4：郊外遇 1～3 级的黄巾
                ActorScale = 0.8f,
                Ground = ',',
                Rows = new[]
                {
                    "MMMMMMMMtMMMMMMMMMMMMMMMMMMMMMMMMMMMM",
                    "MT,,,,T,=,,T,,,,,MMMMMMM,,,,,,,,,==,M",
                    "M,MM,,,,==,,,MwwM,,MM,,,,,T,T,,,===,M",
                    "M,,,,,,,,,=,,M~~~w,MM,,,,,,,,,,==,,,M",
                    "M,,MM,,,,,=,T,MMwMMMMM,,,,,,,,=,,,T,M",
                    "M,MMMM,,,,====,,,,MMMM,,,,,,,,=,,T,,M",
                    "M,MMMM,,,,,,,=,,,,,,,,,,,,,,==,,,,,,M",
                    ",,,,,,,MwM,T,=,,,HHHMMT,,,==,,,,,,,TM",
                    "MT,,,,w~~w,HH====HHHM,,,==,,,,,MMM,,M",
                    "M,T,,,,MM,,HH,,==,,,,===,,,,,,,T,,,,M",
                    ",,,,,,,,,H,,,,,======,,,T,,,,,,,,,,,M",
                    "MMM,,TT,T,,MM,H===H,,,,,,,,,,,,,,,,,M",
                    "MMMM,,,,,T,MMM,,=,,MM,,,,MM,,,,,,T,,M",
                    "MMM,,MM,M,,,MM,,=,MMM,,,MMM,,T,,,,,,M",
                    "M,,,,,,,,,,,,,,,=,,M,,,,,,M,T,,,,,,,M",
                    "~w,,,,,,M,,,,,,=,,,,,,T,,,,,,,MM,,M,M",
                    "~~~wM,,,,,,,,,==,,,,,,,,,,,,M,,,w~~~w",
                    "Mww~~wM,,,,,,M=M,,,,,,,,,ww~~~~~~~ww~",
                    ",,,M~~~~ww~~~~=~~wM,,,,,w~~wwwwww,,,M",
                    ",,,,,ww~~~Mwww=M~~~~~ww~~~M,,,,,,,,,M",
                    "M,,T,,,,,,,,,,,=,ww~~~~~wM=====CCCCCM",
                    "M,,,,,,T,,T,,,,,==,,======,,,,=CCCCCM",
                    "MMM,,,,,,,,,,,,,,,==,T,,,,,,,,=CCCCCM",
                    "MMMMMMMMMMMMMMMMMMM=MMMMMMMMMMMMMMMMM",
                }
            };
            // 2×2 四屏的大郊外（底图由四块拼成，见 ArtSrc/郊外扩大提示词.md）；树、村子、桥、营寨在物件清单里（Resources/Map/Decor/field_big.txt）
            m.DecorFile = "field_big";
            // 活气：水面起波纹（w = 水塘边的浅水，能趟）、营地篝火冒烟、水里有鱼和鸭子、岸边有蜻蜓、草地上有兔子麻雀
            m.WaterMask = "Art/field_big_water";
            m.Emitters.Add(new MapEmitter("fire", 33.6f, 23.05f));
            m.Critters.Add(new CritterSpawn("fish", 7));
            m.Critters.Add(new CritterSpawn("duck", 2));
            m.Critters.Add(new CritterSpawn("dragonfly", 3));
            m.Critters.Add(new CritterSpawn("rabbit", 4));
            m.Critters.Add(new CritterSpawn("sparrow", 5));
            m.Critters.Add(new CritterSpawn("butterfly", 3));
            m.Critters.Add(new CritterSpawn("butterfly2", 2));
            // 宝箱：右上秋林尽头、东边草地
            m.Npcs.Add(Chest(F_ChestFieldNE, 34, 1, 0, "pijia"));
            m.Npcs.Add(Chest(F_ChestFieldE, 35, 10, 120));
            // 路口的小村子：村妇在井边转悠，老人在农舍门口
            m.Npcs.Add(Walker("field_villager", "村妇", "cunmin_nv", null, 1.0f, new[] { 15, 12, 17, 12, 17, 13, 15, 13 }, new[] { 18, 9 },   // 东边大农舍
                "黄巾贼来过一回，抢了粮就走了。", "井水甜着呢，将军要不要喝一口？", "营寨就在河对岸东南边，过了木桥一直走。"));
            m.Npcs.Add(Walker("field_elder", "老人", "laoren", "guaizhang", 1.2f, new[] { 13, 10, 13, 9 }, new[] { 11, 10 },   // 西边小农舍
                "老朽守着这几间草房，哪儿也不去了。", "河上那座木桥是我年轻时搭的。", "东北那片秋林里，好像有人藏过东西……"));

            var flags = host.Flags;

            m.Npcs.Add(new Npc
            {
                Id = "nanmin", Name = "逃难村民", Glyph = "民", Color = "#e0e0e0", X = 19, Y = 11,
                Talk = () =>
                {
                    if (flags.Has(F_Boss)) Say(L("逃难村民", "多谢三位将军，我们终于可以回家了！"));
                    else Say(L("逃难村民", "黄巾贼的营寨就在东南边，听说头领叫程远志，手下有五万人马……"),
                             L("逃难村民", "将军们若是兵马不足，不如先在草地上剿些散兵，练练兵再去。"));
                }
            });

            // 南边官道（底边 (19,23)，底图上那格正好在土路中间）通往青州：第一章打完之前官兵拦着
            m.Npcs.Add(new Npc
            {
                Id = "qingzhou_guard", Name = "官兵", Glyph = "兵", Skin = "shouwei", Weapon = "qiang", X = 19, Y = 23, Facing = "back",
                Visible = () => !flags.Has(F_Boss),
                Talk = () => Say(L("官兵", "往南是去青州的官道。黄巾作乱，太守有令，闲人不得通行！"),
                                 L("官兵", "你们若真有本事，先把东南边那伙黄巾贼收拾了再说。"))
            });

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't')
                {
                    host.Warp(Town, 8, 12);
                    return true;
                }
                // 官道口：第一章打完了就去青州（第二章）
                if (x == 19 && y == 23)
                {
                    if (flags.Has(F_Boss)) Cross(2, "qingzhou_field", 15, 18);
                    else Say(L("关羽", "兄长，往南便是去青州的官道。"));
                    return true;
                }
                return false;
            };

            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) != 'C') return false;
                if (flags.Has(F_Boss)) { Say(L(null, "营寨已被攻破，只剩一片焦土。")); return true; }
                BossFight();
                return true;
            };

            return m;
        }

        void BossFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("程远志", "哪里来的织席小儿，也敢来送死？"),
                L("邓茂", "大哥，让我先去拿下他们！"),
                L("张飞", "呔！燕人张翼德在此，谁敢与我一战！"),
                L("刘备", "反国逆贼，何不早降！"),
            }, () =>
            {
                host.StartBattle(GameDatabase.Chapter1Boss(), true, () =>
                {
                    host.Flags.Set(F_Boss);
                    host.Say(new List<DialogueLine>
                    {
                        L(null, "张飞挺矛直刺邓茂，关羽舞刀斩程远志于马下。"),
                        L(null, "黄巾军群龙无首，纷纷倒戈卸甲而逃。"),
                        L("刘备", "初战告捷，全赖两位贤弟！"),
                        L("关羽", "兄长，听说青州被黄巾围困，太守龚景求援。"),
                        L("张飞", "那还等什么，杀过去！"),
                        L(null, "（青州在南边，沿郊外南面的官道走过去）"),
                    });   // 不弹结算图，玩家自己走到官道口 (19,23) 进第二章（见 BuildField 的 OnStep）
                });
            });
        }
    }
}
