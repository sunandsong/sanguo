using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第一章「桃园结义」：涿县城内剧情 → 出城练级 → 黄巾营寨 Boss。
    /// 每一章都按这个模板写：地图字符串 + NPC + 剧情标记 + Boss。
    /// </summary>
    public class Chapter1
    {
        public const int InnCost = 20;

        readonly IGameHost host;
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
        const string F_Boss = "c1_boss";
        const string F_Liutao = "c1_liutao";   // 母亲给了《六韬》

        public Chapter1(IGameHost host)
        {
            this.host = host;
            Town = BuildTown();
            Field = BuildField();
            Home = BuildHome();
            Tavern = BuildTavern();
            Inn = BuildInn();
            Inn2F = BuildInn2F();
        }

        public int StartX { get { return 5; } }
        public int StartY { get { return 5; } }

        static DialogueLine L(string speaker, string text) { return new DialogueLine(speaker, text); }
        void Say(params DialogueLine[] lines) { host.Say(new List<DialogueLine>(lines)); }

        public void Begin()
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
                L(null, "【操作】方向键移动，Z/空格/回车 确认，X/Esc 取消。"),
            });
        }

        /// <summary>全灭后：回到客栈门口，兵力全满，金钱减半。</summary>
        public void RespawnAfterDefeat()
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
                Art = "zhuoxian",
                Grade = "#fff6ec", Weather = "petal",
                EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTT",
                    "TTTHHHH.....PPPPPPPT",
                    "T.HHHHHH....PPfSfPPT",
                    "T.HHH=HH.....PfffPPT",
                    "T....=.......PPfPPPT",
                    "T....=.........=...T",
                    "T....==========....T",
                    "THHHHH..=.....HHHHHT",
                    "THHHHH..=.....HHHHHT",
                    "THH.....=.....HHHHHT",
                    "T.......=..........T",
                    "TTTTTTT.=.TTTTTTTTTT",
                    "TTTTTTTT=TTTTTTTTTTT",
                    "TTTTTTTTgTTTTTTTTTTT",
                }
            };
            m.Props.Add(new MapProp("house_liubei", 3, 1, 4, 3));
            m.Props.Add(new MapProp("tavern", 2, 7, 3, 3));
            m.Props.Add(new MapProp("inn", 14, 7, 3, 3));

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
                Id = "guanyu", Name = "关羽", Glyph = "关", Color = GameDatabase.GuanYu.Color, X = 5, Y = 10,
                Visible = () => flags.Has(F_MetZhangFei) && !flags.Has(F_Oath),
                Talk = () =>
                {
                    if (!flags.Has(F_MetGuanYu))
                    {
                        flags.Set(F_MetGuanYu);
                        Say(L("关羽", "某姓关名羽，字云长，河东解良人。因本地豪强仗势欺人，被我杀了，逃难江湖五六年。"),
                            L("关羽", "听闻此处招军破贼，特来应募。"),
                            L("刘备", "玄德正有此志，愿与二位共图大事。"),
                            L("张飞", "俺庄后有一座桃园，花开正盛。明日就在园中祭告天地，结为兄弟！"),
                            L(null, "（前往城东北的桃园）"));
                        return;
                    }
                    Say(L("关羽", "兄长，桃园就在城东北。"));
                }
            });

            m.Npcs.Add(new Npc
            {
                Id = "shangren", Name = "张世平", Glyph = "商", Color = "#e0e0e0", X = 11, Y = 5,
                Visible = () => flags.Has(F_Oath),
                Talk = () =>
                {
                    if (flags.Has(F_Gift))
                    {
                        Say(L("张世平", "祝诸位旗开得胜！"));
                        return;
                    }
                    flags.Set(F_Gift);
                    host.Party.Gold += 300;
                    foreach (var g in host.Party.Members) g.WeaponBonus += 5;
                    host.Party.Items.Add("wuzi");
                    Say(L("张世平", "在下中山商人张世平。听说三位要讨贼保民，这点心意请务必收下。"),
                        L(null, "获得金钱 300！"),
                        L(null, "以赠送的镔铁打造兵器：刘备「双股剑」、关羽「青龙偃月刀」、张飞「丈八蛇矛」。"),
                        L(null, "全员武力 +5！"),
                        L("张世平", "另有兵书一卷，行军打仗或许用得上。"),
                        L(null, "获得兵书《吴子》！（菜单 → 物品 里研读）"));
                }
            });

            m.Npcs.Add(new Npc
            {
                Id = "shouwei", Name = "守卫", Glyph = "卫", Color = "#c0c0c0", X = 8, Y = 13, Weapon = "qiang",
                Visible = () => !flags.Has(F_Oath),
                Talk = () => Say(L("守卫", "城外黄巾贼出没，你一个人出去就是送死。"),
                                 L("守卫", "要出城，先招些人马再说吧。"))
            });

            m.OnStep = (x, y) =>
            {
                // 城门：出城
                if (m.At(x, y) == 'g')
                {
                    host.Warp(Field, 8, 1);
                    return true;
                }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                // 桃园石桌：走到桌前往上撞，三结义
                if (x == 15 && y == 2 && flags.Has(F_MetGuanYu) && !flags.Has(F_Oath)) { Oath(); return true; }
                // 刘备家门（门口 (5,3) 往上撞）
                if (x == 5 && y == 2) { host.Warp(Home, 6, 7); return true; }
                // 酒馆门（门口 (3,9) 往上撞）
                if (x == 3 && y == 8) { host.Warp(Tavern, 6, 7); return true; }
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

            m.OnTouch = (x, y) =>
            {
                if (y >= m.Height) { host.Warp(Town, 5, 3); return true; }
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
                            L("张飞", "走，先进酒馆喝一杯！……咦，那边推车进来的大汉，好生威风。"));
                        return;
                    }
                    Say(L("张飞", "那红脸的汉子看着不凡，哥哥快去跟他说说话！"));
                }
            });

            m.OnTouch = (x, y) =>
            {
                if (y >= m.Height) { host.Warp(Town, 3, 9); return true; }
                return false;
            };

            return m;
        }

        // =====================================================================
        // 客栈一楼门厅（下边敞口；东北角楼梯上二楼）
        // =====================================================================
        MapData BuildInn()
        {
            var m = new MapData
            {
                Id = "inn",
                Name = "客栈",
                Art = "inn",
                Grade = "#fff0dc", Light = "#e8d6c0",
                ArtMargin = 0,
                Rows = new[]
                {
                    "HHHHHHHHH",
                    "HHHHHHHHH",
                    "HHHH..HHH",
                    "........H",
                    ".........",
                    ".........",
                }
            };

            m.Npcs.Add(new Npc
            {
                Id = "xiaoer", Name = "店小二", Glyph = "店", Color = "#e0e0e0", X = 4, Y = 2,
                Talk = () =>
                {
                    host.Choose("客官，住店一晚 " + InnCost + " 金，兵马都能休整好。住吗？", new[] { "住店", "不了" }, i =>
                    {
                        if (i != 0) { Say(L("店小二", "客官慢走！")); return; }
                        if (host.Party.Gold < InnCost) { Say(L("店小二", "客官，您这钱……不太够啊。")); return; }
                        host.Party.Gold -= InnCost;
                        host.Party.FullRestore();
                        Say(L(null, "一夜休整。全军兵力、法力已恢复！"));
                    });
                }
            });

            m.OnTouch = (x, y) =>
            {
                if (y >= m.Height) { host.Warp(Town, 16, 10); return true; }
                if (x == 8 && y == 3) { host.Warp(Inn2F, 7, 3); return true; }   // 楼梯口 (8,4) 往上走
                return false;
            };

            return m;
        }

        // =====================================================================
        // 客栈二楼客房（东北角楼梯下楼；前景层是下边的栏杆）
        // =====================================================================
        MapData BuildInn2F()
        {
            var m = new MapData
            {
                Id = "inn_2f",
                Name = "客栈二楼",
                Art = "inn_2f",
                Grade = "#fff0dc", Light = "#e8d6c0",
                ArtMargin = 0,
                Rows = new[]
                {
                    "HHHHHHHHH",
                    "HHHHHHHHH",
                    "HH.H.HHHH",
                    "H........",
                    ".H......H",
                    "HHHHHHHHH",
                }
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 7 && y == 2) { host.Warp(Inn, 8, 4); return true; }   // 楼梯口 (7,3) 往上撞就下楼
                return false;
            };

            return m;
        }

        /// <summary>调试用：跳过城内剧情（结义、受赠都算完成），直接站到城外。</summary>
        public void SkipToField()
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
                foreach (var g in host.Party.Members) g.WeaponBonus += 5;
                host.Party.Items.Add("wuzi");
            }
            flags.Set(F_Notice);
            flags.Set(F_MetZhangFei);
            flags.Set(F_MetGuanYu);
            host.Party.FullRestore();
            host.Warp(Field, 8, 1);
        }

        void Oath()
        {
            var flags = host.Flags;
            flags.Set(F_Oath);
            host.Party.Add(new General(GameDatabase.GuanYu));
            host.Party.Add(new General(GameDatabase.ZhangFei));
            host.Party.Gold += 100;
            host.Party.FullRestore();
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
                Art = "field",
                Grade = "#f0f5ff", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09,
                ActorScale = 0.8f,
                Ground = ',',
                Rows = new[]
                {
                    "MMMMMMMMtMMMMMMMMMMM",
                    "MTTTT,,,=,TTTT,,,,TM",
                    "MTTMTTT,=,,,T,~~~~,M",
                    "MT,,T,TT===,TT~~~~,M",
                    "M,,TMT,,,,=,,,M~~,,M",
                    "M,TMMMT,,,====,,,,,M",
                    "M,,MMM,,,,,,,=,,,TTM",
                    "MTTTT,,,~~,,,=,,,MTM",
                    "MTTTT,,~~~,,,==,,,,M",
                    "MT,,,,,MMM,,,,CC=CCM",
                    "MT,TTT,,,,,,,,CCCCCM",
                    "MTMTTTTTT,,M,CCCCCCM",
                    "MMMMMMMMMMMMMMMMMMMM",
                    "MMMMMMMMMMMMMMMMMMMM",
                }
            };
            m.Props.Add(new MapProp("yellow_turban_camp", 17, 11, 2, 2));

            var flags = host.Flags;

            m.Npcs.Add(new Npc
            {
                Id = "nanmin", Name = "逃难村民", Glyph = "民", Color = "#e0e0e0", X = 5, Y = 1,
                Talk = () =>
                {
                    if (flags.Has(F_Boss)) Say(L("逃难村民", "多谢三位将军，我们终于可以回家了！"));
                    else Say(L("逃难村民", "黄巾贼的营寨就在东南边，听说头领叫程远志，手下有五万人马……"),
                             L("逃难村民", "将军们若是兵马不足，不如先在草地上剿些散兵，练练兵再去。"));
                }
            });

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't')
                {
                    host.Warp(Town, 8, 12);
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
                    }, () => host.ChapterClear("第一章 桃园结义 · 完", "下一章：青州解围"));
                });
            });
        }
    }
}
