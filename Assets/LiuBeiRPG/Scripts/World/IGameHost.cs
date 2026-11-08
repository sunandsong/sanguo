using System;
using System.Collections.Generic;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.World
{
    public class DialogueLine
    {
        public string Speaker;   // 为空表示旁白
        public string Text;
        public DialogueLine(string speaker, string text) { Speaker = speaker; Text = text; }
    }

    /// <summary>一座城的落地点（飞鸟扇用）。</summary>
    public class TownSpot { public MapData Map; public int X, Y; }

    /// <summary>剧情进度标记，例如 "oath"（已结义）。存档时只要存这个集合即可。</summary>
    public class StoryFlags
    {
        readonly HashSet<string> set = new HashSet<string>();
        public static bool DevAll;   // 测试用：所有剧情都算走完（出口全通、boss 不在）
        public bool Has(string f) { return DevAll || set.Contains(f); }
        public void Set(string f) { set.Add(f); }
        public IEnumerable<string> All { get { return set; } }
    }

    /// <summary>
    /// 剧情脚本通过这个接口调用游戏本体（显示对话、开战、换地图……）。
    /// 这样剧情代码不依赖 Unity，也方便以后换成别的表现层。
    /// </summary>
    public interface IGameHost
    {
        Party Party { get; }
        StoryFlags Flags { get; }

        void Say(List<DialogueLine> lines, Action onDone = null);
        void Choose(string question, string[] options, Action<int> onChoose);
        void StartBattle(List<General> enemies, bool isBoss, Action onVictory = null, int turnLimit = 0);   // turnLimit > 0：撑过这么多回合算胜（虎牢关战吕布）
        void Warp(MapData map, int x, int y);
        void ChapterClear(string title, string nextHint);
        bool StartChapter(int number);
        void GoChapter(int number, string mapId, int x, int y);   // 回到/跳到第 n 章的某张图，不放开场剧情（跨章节走地图用）   // 开始第 n 章（队伍、物品、flags 都带着）；没有这一章返回 false 回标题
        void OpenShop(string[] goods);   // 商店：goods 是卖的物品 Id
        void PlaceFollowers(string facing, params int[] cells);   // 剧情站位：队友（第 2、3… 人）站到 x0,y0, x1,y1…，朝 facing（front/back/left/side）
        void SetHour(float hour);        // 跳到某个时辰（住店睡一晚 → 清晨）
        void OpenChest(Npc chest, string item, int count, int gold, string message);   // 开箱演出（抖、开盖换图、东西飞进袋子），演完在下方弹 message，自己消失
    }
}
