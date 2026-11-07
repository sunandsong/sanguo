using System;
using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 第六章「古城会」（演义第二十五～二十八回）：关羽千里走单骑、兄弟会于汝南 → 卧牛山收周仓、关定庄收关平 → 刘辟龚都试刀 → 古城外斩蔡阳，三兄弟古城相会。
    /// 等级段 32～38。队伍不拆：开场旁白交代关羽已寻到刘备，张飞在古城等着。
    /// </summary>
    public class Chapter6 : Chapter
    {
        public const int InnCost = 350;

        public readonly MapData Field, Town, Inn, Inn2F;

        const string F_ZhouCang = "c6_zhoucang", F_GuanPing = "c6_guanping", F_LiuPi = "c6_liupi";
        public const string F_Boss = "c6_boss";
        const string F_ChestF1 = "c6_chest_f1", F_ChestF2 = "c6_chest_f2", F_ChestF3 = "c6_chest_f3", F_ChestF4 = "c6_chest_f4";
        const string F_ChestTown1 = "c6_chest_town1", F_ChestTown2 = "c6_chest_town2", F_ChestInn = "c6_chest_inn2f";

        // 古城是张飞占的小城，货不多（设计文档 8.3 没排古城，按许都减掉最贵的）
        static readonly string[] ShopGoods = { "caoyao", "jinchuangyao", "zhixuesan", "qingjiu", "huanhundan", "langyan", "yanmudan", "nuqiwan", "feiniaoshan", "qingfengjian", "langyabang", "fuchen", "suozijia", "yulinjia", "junma" };
        static readonly GeneralDef[] RunanPool = { GameDatabase.HuangJinTouMu, GameDatabase.CaoJunBuBing, GameDatabase.CaoJunBuBing, GameDatabase.CaoJunGongBing, GameDatabase.QingZhouBing, GameDatabase.HuBaoQi };

        public Chapter6(IGameHost host) : base(host)
        {
            Field = BuildField();
            Town = BuildTown();
            var inn = BuildInn("gucheng_inn", InnCost, () => host.Warp(Town, 5, 7), Chest(F_ChestInn, 3, 3, 0, "zhixuesan"));
            Inn = inn[0]; Inn2F = inn[1];
        }

        public override int Number { get { return 6; } }
        public override MapData[] Maps { get { return new[] { Field, Town, Inn, Inn2F }; } }
        public override List<TownSpot> Towns { get { return new List<TownSpot> { new TownSpot { Map = Town, X = 12, Y = 14 } }; } }

        public override void Begin()
        {
            host.Party.FullRestore();
            host.Warp(Field, 15, 18);
            host.Say(new List<DialogueLine>
            {
                L(null, "关羽在曹营，斩颜良、诛文丑，报了曹操之恩。得知兄长在袁绍处，便封金挂印，护着二位嫂嫂辞曹而去。"),
                L(null, "一路过五关、斩六将，终于在汝南寻到刘备。"),
                L("关羽", "兄长！"),
                L("刘备", "云长！备日夜思念，今日总算……"),
                L("孙乾", "主公，听说芒砀山一带有个叫古城的地方，被一员猛将占了，赶走了县官，莫不是三将军？"),
                L("关羽", "汝南往北便是古城。只是我斩了蔡阳的外甥秦琪，蔡阳扬言要追我报仇，怕是快到了。"),
            });
        }

        public override void RespawnAfterDefeat()
        {
            host.Party.Gold /= 2;
            host.Party.FullRestore();
            if (host.Flags.Has(F_LiuPi)) { host.Warp(Town, 5, 7); Say(L(null, "胜败乃兵家常事。刘备一行退回古城休整……"), L(null, "（金钱减半，兵力已恢复）")); }
            else { host.Warp(Field, 15, 18); Say(L(null, "胜败乃兵家常事。刘备一行退回汝南官道口重整……"), L(null, "（金钱减半，兵力已恢复）")); }
        }

        public override void SkipToField()
        {
            host.Party.FullRestore();
            host.Warp(Field, 15, 18);
        }

        // =====================================================================
        // 汝南郊外：南边进来；西北卧牛山（周仓）、东边关定庄（关平）、中间刘辟龚都的营；北头是古城城门，蔡阳最后堵在门前
        // =====================================================================
        MapData BuildField()
        {
            var m = new MapData
            {
                Id = "runan_field", Name = "汝南郊外", Art = "runan_field",
                Grade = "#f2f4ea", Light = "#f4f0ea", Weather = "leaf",
                EncounterRate = 0.09, EnemyLevel = 34, ActorScale = 0.8f, Ground = ',', EnemyPool = RunanPool,
                Rows = new[]
                {
                    "MMMMMMMMMMMMMMMtMMMMMMMMMMMMMM",
                    "MMMMMM,,,,,,,,,=,,,,,,,,,,,,,M",
                    "MMMMM,,,,,,,,,,=,,,,,T,,,,,,,M",
                    "MMMM,,,,,,,T,,,=,,,,,,,,,,,,,M",
                    "MMM,,,,,,,,,,,,=,,,,,,,,T,,,,M",
                    "MM,,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,MM,,,,,,,,,,=,,,,,,HHHH,,,M",
                    "M,,MM,,,,,,,,,,=,,,,,,HHHH,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,HHHH,,,M",
                    "M,,,,,,,,,,,,,,=======.=,,,,,M",
                    "M,,,,,,,,CCCC,,=,,,,,,,,,,,,,M",
                    "M,,,,T,,,CCCC,,=,,,,,,,T,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,MM,,M",
                    "M,,~~~~,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,~~~~~~,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,~~~~,,,,T,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,T,,,,,,,,,M",
                    "M,T,,,,,,,,,,,,=,,,,,,,,,,,,,M",
                    "M,,,,,,,,,,,,,,=,,,,,,,,,,T,,M",
                    "MMMMMMMMMMMMMMM=MMMMMMMMMMMMMM",
                }
            };
            m.DecorFile = "runan_field";
            m.Emitters.Add(new MapEmitter("fire", 10.7f, 11.45f));
            m.Emitters.Add(new MapEmitter("smoke", 24.0f, 6.2f));
            m.Critters.Add(new CritterSpawn("rabbit", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            m.Critters.Add(new CritterSpawn("chicken", 2));
            m.Critters.Add(new CritterSpawn("duck", 2));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestF1, 6, 1, 0, "zhixuesan", 2));
            m.Npcs.Add(Chest(F_ChestF2, 28, 1, 600));
            m.Npcs.Add(Chest(F_ChestF3, 2, 18, 0, "qingjiu", 3));
            m.Npcs.Add(Chest(F_ChestF4, 27, 15, 0, "yulinjia"));

            m.Npcs.Add(Person("zhoucang", "周仓", "zhoucang", "dao", 7, 3, () =>
            {
                if (flags.Has(F_ZhouCang)) return;
                flags.Set(F_ZhouCang);
                host.Say(new List<DialogueLine>
                {
                    L("周仓", "（扑翻便拜）那红脸长髯的，可是关将军？小人周仓，原是黄巾张宝部下，久仰将军大名！"),
                    L("周仓", "小人在卧牛山落草，今日得见将军，情愿替将军扛刀，鞍前马后！"),
                    L("关羽", "你肯弃暗投明，便跟着我吧。"),
                    L(null, "周仓加入队伍！"),
                }, () => Join(GameDatabase.ZhouCang));
            }, () => !flags.Has(F_ZhouCang)));
            m.Npcs.Add(Person("guanping", "关平", "guanping", "dao", 24, 9, () =>
            {
                if (flags.Has(F_GuanPing)) return;
                flags.Set(F_GuanPing);
                host.Say(new List<DialogueLine>
                {
                    L("关定", "老汉关定，这是次子关平，自幼习武。将军与老汉同宗，若不嫌弃，让他随将军去吧。"),
                    L("刘备", "云长尚无子嗣，不如收平儿为义子，如何？"),
                    L("关羽", "（大喜）平儿，拜见伯父。"),
                    L("关平", "关平拜见伯父！拜见父亲！"),
                    L(null, "关平加入队伍！"),
                }, () => Join(GameDatabase.GuanPing));
            }, () => !flags.Has(F_GuanPing)));

            Action test = LiuPiFight;
            Func<bool> camped = () => !flags.Has(F_LiuPi);
            m.Npcs.Add(Person("liupi_npc", "刘辟", "liupi", "langya", 11, 12, test, camped));
            m.Npcs.Add(Person("gongdu_npc", "龚都", "gongdu", "cha", 12, 12, test, camped));

            m.Npcs.Add(Person("caiyang_npc", "蔡阳", "caiyang", "dao", 15, 1, CaiYangFight, () => flags.Has(F_LiuPi) && !flags.Has(F_Boss)));
            m.Npcs.Add(Walker("rn_farmer", "农夫", "cunmin_nan", "chutou", 1.0f, new[] { 17, 14, 22, 14, 22, 15, 17, 15 }, null,
                "卧牛山上有伙强人，头领叫周仓，力气大得很。", "东边那座庄子是关老汉家的，他二儿子武艺了得。", "汝南的黄巾余党刘辟、龚都，扎营在西边。"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 't')
                {
                    if (flags.Has(F_Boss)) host.Warp(Town, 12, 14);
                    else if (flags.Has(F_LiuPi)) Say(L(null, "城门紧闭，城头一员黑脸大将挺矛大叫：「关羽背义之人，还有脸来见我！」"), L("关羽", "三弟误会了……先打退门外的蔡阳，他自然明白。"));
                    else Say(L("孙乾", "主公，汝南的刘辟、龚都扎营在西边，不先去打个招呼，怕他们从背后捅刀子。"));
                    return true;
                }
                if (x == 15 && y == 19) { Say(L("关羽", "兄长，往北才是古城。")); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (m.At(x, y) == 'H') { Say(L(null, "关定庄，土墙茅屋，院里晒着谷子。")); return true; }
                if (m.At(x, y) == 'C') { Say(L(null, flags.Has(F_LiuPi) ? "刘辟、龚都的营，如今插着刘字旗。" : "黄巾余党的营盘，旗上还写着「黄天」。")); return true; }
                return false;
            };
            return m;
        }

        void LiuPiFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("刘辟", "皇叔要借汝南安身？弟兄们跟着我刀口舔血，可不认什么皇叔不皇叔。"),
                L("龚都", "要我们听令也成，先让我们看看皇叔麾下的本事！"),
            }, () => host.StartBattle(GameDatabase.Chapter6LiuPi(), true, () =>
            {
                host.Flags.Set(F_LiuPi);
                host.Say(new List<DialogueLine>
                {
                    L("刘辟", "服了，服了！汝南的弟兄，从今往后听皇叔调遣。"),
                    L("龚都", "皇叔，北边古城那员猛将，环眼虎须，自称燕人。"),
                    L("刘备", "那必是翼德！"),
                    L("关羽", "不说笑了。探马来报，蔡阳的追兵已到古城门外！"),
                });
            }));
        }

        void CaiYangFight()
        {
            host.Say(new List<DialogueLine>
            {
                L("蔡阳", "关羽！你杀我外甥秦琪，今日奉丞相之命，取你首级！"),
                L("张飞", "（城头）二哥，你若真心，三通鼓内斩了此人，俺便信你！"),
                L("关羽", "三弟看好了！"),
                L(null, "张飞亲自擂鼓。"),
            }, () => host.StartBattle(GameDatabase.Chapter6CaiYang(), true, () =>
            {
                host.Flags.Set(F_Boss);
                host.Say(new List<DialogueLine>
                {
                    L(null, "一通鼓未尽，关羽刀起处，蔡阳头已落地。"),
                    L(null, "张飞大哭，下城拜伏于地。"),
                    L("张飞", "二哥！是俺错怪你了！"),
                    L("关羽", "三弟，起来。"),
                    L(null, "刘备、关羽、张飞，古城相会。赵云、孙乾、糜竺、简雍、周仓、关平都在，兄弟重聚，大宴三日。"),
                    L("刘备", "备飘零半生，今日方知，得兄弟者得天下。"),
                    L("赵云", "主公，曹操势大，袁绍将败，汝南不可久留。荆州刘表是主公同宗，可往投之。"),
                }, () => host.ChapterClear("第六章 古城会 · 完", "第七章：三顾茅庐"));
            }));
        }

        // =====================================================================
        // 古城：张飞占的小县城，衙门、客栈、商铺各一座，南门回汝南
        // =====================================================================
        MapData BuildTown()
        {
            var m = new MapData
            {
                Id = "gucheng", Name = "古城", Art = "gucheng",
                Grade = "#fff3e4", Weather = "leaf", EncounterRate = 0,
                Rows = new[]
                {
                    "TTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T.........HHHHH.........T",
                    "T..HHHHH..HHHHH...HHHH..T",
                    "T..HHHHH..HHHHH...HHHH..T",
                    "T..HHHHH..HHHHH...HHHH..T",
                    "T..HHHHH....=.....HHHH..T",
                    "T.....=.....=.......=...T",
                    "T.....=============.=...T",
                    "T...........=...........T",
                    "T..HHHHH....=....HHHHH..T",
                    "T..HHHHH....=....HHHHH..T",
                    "T..HHHHH....=....HHHHH..T",
                    "T...........=...........T",
                    "T...........=...........T",
                    "T...........=...........T",
                    "TTTTTTTTTTTTgTTTTTTTTTTTT",
                }
            };
            m.DecorFile = "gucheng";
            m.Emitters.Add(new MapEmitter("smoke", 5.3f, 8.95f));
            m.Critters.Add(new CritterSpawn("chicken", 3));
            m.Critters.Add(new CritterSpawn("sparrow", 4));
            var flags = host.Flags;

            m.Npcs.Add(Chest(F_ChestTown1, 23, 1, 400));
            m.Npcs.Add(Chest(F_ChestTown2, 1, 14, 0, "huanhundan"));
            m.Npcs.Add(Person("gc_merchant", "掌柜", "shangren", null, 20, 6, () =>
                host.Say(new List<DialogueLine> { L("掌柜", "张将军占了城，倒没为难咱们做买卖的。诸位要些什么？") }, () => host.OpenShop(ShopGoods))));
            m.Npcs.Add(Person("gc_guard", "古城兵", "shouwei", "qiang", 11, 14, () =>
                Say(L("古城兵", "张将军说了，皇叔一家团聚，全城的酒都管够！"))));
            m.Npcs.Add(Walker("gc_patrol", "巡街兵", "shouwei", "qiang", 0.9f, new[] { 12, 13, 12, 8 }, new[] { 12, 14 },
                "张将军一来就把县官赶跑了。", "那天关将军一通鼓就斩了蔡阳！", "皇叔说要去荆州投刘表。"));
            m.Npcs.Add(Walker("gc_woman", "村妇", "cunmin_nv", null, 1.0f, new[] { 2, 12, 9, 12 }, new[] { 5, 12 },
                "张将军嗓门大，人倒不坏。", "二位夫人平安到了，阿弥……不对，谢天谢地。", "关将军的那个扛刀的，长得可真黑。"));
            m.Npcs.Add(Walker("gc_kid", "小孩", "xiaohai", null, 0.7f, new[] { 15, 13, 22, 13 }, new[] { 19, 13 },
                "黑脸叔叔擂鼓，咚咚咚！", "关平哥哥教我耍刀！", "我也要拜个干爹！"));

            m.OnStep = (x, y) =>
            {
                if (m.At(x, y) == 'g') { host.Warp(Field, 15, 1); return true; }
                return false;
            };
            m.OnTouch = (x, y) =>
            {
                if (x == 5 && y == 5) { host.Warp(Inn, 4, 5); return true; }
                if (x == 12 && y == 4) { Say(L(null, "县衙。张飞把县官的公案劈成了两半，至今没修。")); return true; }
                return false;
            };
            return m;
        }
    }
}
