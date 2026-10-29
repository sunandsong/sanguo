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

    /// <summary>剧情进度标记，例如 "oath"（已结义）。存档时只要存这个集合即可。</summary>
    public class StoryFlags
    {
        readonly HashSet<string> set = new HashSet<string>();
        public bool Has(string f) { return set.Contains(f); }
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
        void StartBattle(List<General> enemies, bool isBoss, Action onVictory = null);
        void Warp(MapData map, int x, int y);
        void ChapterClear(string title, string nextHint);
        void OpenShop(string[] goods);   // 商店：goods 是卖的物品 Id
        void SetHour(float hour);        // 跳到某个时辰（住店睡一晚 → 清晨）
    }
}
