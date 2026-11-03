using System;
using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    /// <summary>
    /// 一章 = 几张地图 + NPC + 剧情标记 + Boss。每章一个子类（Chapter1、Chapter2…），GameRoot 只认这个基类。
    /// 这里放各章共用的小工具：对话、宝箱、路人、客栈。
    /// </summary>
    public abstract class Chapter
    {
        protected readonly IGameHost host;

        /// <summary>这一章的支线任务，人物页“任务”页签列的就是这些。</summary>
        public readonly List<Quest> Quests = new List<Quest>();

        protected Chapter(IGameHost host) { this.host = host; }

        public abstract int Number { get; }
        /// <summary>这一章所有地图，存档按地图名找回来。</summary>
        public abstract MapData[] Maps { get; }
        /// <summary>飞鸟扇能飞回的城：地图 + 落地格。去过的（flag visited_&lt;地图 Id&gt;）才能选。</summary>
        public abstract List<TownSpot> Towns { get; }
        public abstract void Begin();
        /// <summary>全灭后回哪儿。</summary>
        public abstract void RespawnAfterDefeat();
        /// <summary>调试：跳过开头剧情直接到野外。</summary>
        public virtual void SkipToField() { }

        protected static DialogueLine L(string speaker, string text) { return new DialogueLine(speaker, text); }
        protected void Say(params DialogueLine[] lines) { host.Say(new List<DialogueLine>(lines)); }

        /// <summary>宝箱（通用）：开一次给钱 / 物品，开的时候演一段（GameRoot.Chest），开过换成打开的图。图见 ArtSrc/宝箱提示词.md，缺图时箱子看不见但照样能开。</summary>
        protected Npc Chest(string flag, int x, int y, int gold, string item = null, int count = 1)
        {
            var flags = host.Flags;
            var n = new Npc { Id = flag, Name = "宝箱", Glyph = "箱", X = x, Y = y, Size = 1f, Sprite = flags.Has(flag) ? "Art/chest_open" : "Art/chest" };
            n.Talk = () =>
            {
                if (flags.Has(flag)) { Say(L(null, "箱子是空的。")); return; }
                var it = ItemDb.Get(item);
                if (it != null && !host.Party.CanHold(item)) { Say(L(null, "行囊满了，拿不下。")); return; }
                flags.Set(flag);
                host.Party.Gold += gold;
                for (int i = 0; it != null && i < count; i++) host.Party.Items.Add(item);
                string got = it == null ? "金钱 " + gold : it.Name + (count > 1 ? " ×" + count : "") + (gold > 0 ? "、金钱 " + gold : "");
                host.OpenChest(n, it == null ? null : item, count, gold, "打开宝箱，获得" + got + "！");
            };
            return n;
        }

        protected static readonly Random chatRng = new Random();

        /// <summary>路人：沿 route（x0,y0,x1,y1…）来回闲逛，搭话时随机说一句；天黑走回 home（家门口 x,y）进屋，天亮再出来。
        /// skin 是引擎里的骨骼角色，weapon 是手里拿的东西。</summary>
        protected Npc Walker(string id, string name, string skin, string weapon, float step, int[] route, int[] home, params string[] lines)
        {
            var n = new Npc { Id = id, Name = name, Glyph = name.Substring(0, 1), Skin = skin, Weapon = weapon, StepSeconds = step, Route = route, Home = home, X = route[0], Y = route[1] };
            n.Visible = () => !n.Indoors;
            n.Talk = () => Say(L(name, lines[chatRng.Next(lines.Length)]));
            return n;
        }

        /// <summary>剧情里的站桩 NPC。</summary>
        protected static Npc Person(string id, string name, string skin, string weapon, int x, int y, Action talk, Func<bool> visible = null)
        {
            var n = new Npc { Id = id, Name = name, Glyph = name.Substring(0, 1), Skin = skin, Weapon = weapon, X = x, Y = y, Talk = talk };
            if (visible != null) n.Visible = visible;
            return n;
        }

        /// <summary>客栈一楼 + 二楼（每章一套，图是同一张）。cost 住一晚多少钱，leave 从一楼下边走出去干什么，chest2F 二楼放的宝箱（可空）。</summary>
        protected MapData[] BuildInn(string id, int cost, Action leave, Npc chest2F)
        {
            var inn = new MapData
            {
                Id = id, Name = "客栈", Art = "inn", Grade = "#fff0dc", Light = "#e8d6c0", ArtMargin = 0,
                Rows = new[] { "HHHHHHHHH", "HHHHHHHHH", "HHHH..HHH", "........H", ".........", "........." }
            };
            var up = new MapData
            {
                Id = id + "_2f", Name = "客栈二楼", Art = "inn_2f", Grade = "#fff0dc", Light = "#e8d6c0", ArtMargin = 0,
                Rows = new[] { "HHHHHHHHH", "HHHHHHHHH", "HH.H.HHHH", "HH.......", ".H......H", "HHHHHHHHH" }   // (1,3) 是矮桌，走不上去
            };
            inn.Npcs.Add(new Npc
            {
                Id = "xiaoer", Name = "店小二", Glyph = "店", Color = "#e0e0e0", X = 4, Y = 2,
                Talk = () => host.Choose("客官，住店一晚 " + cost + " 金，兵马都能休整好。住吗？", new[] { "住店", "不了" }, i =>
                {
                    if (i != 0) { Say(L("店小二", "客官慢走！")); return; }
                    if (host.Party.Gold < cost) { Say(L("店小二", "客官，您这钱……不太够啊。")); return; }
                    host.Party.Gold -= cost;
                    host.Party.FullRestore();
                    host.SetHour(7f);
                    Say(L(null, "一夜休整。全军兵力、法力已恢复！"));
                })
            });
            inn.OnTouch = (x, y) =>
            {
                if (y >= inn.Height) { leave(); return true; }
                if (x == 8 && y == 3) { host.Warp(up, 7, 3); return true; }   // 楼梯口 (8,4) 往上走
                return false;
            };
            if (chest2F != null) up.Npcs.Add(chest2F);
            up.OnTouch = (x, y) =>
            {
                if (x == 7 && y == 2) { host.Warp(inn, 8, 4); return true; }   // 楼梯口 (7,3) 往上撞就下楼
                return false;
            };
            return new[] { inn, up };
        }
    }
}
