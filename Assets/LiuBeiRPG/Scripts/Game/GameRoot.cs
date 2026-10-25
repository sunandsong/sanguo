using System;
using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    public enum Mode { Title, Map, Battle, GameOver, ChapterClear }
    public enum InputKey { Up, Down, Left, Right, Confirm, Cancel }

    /// <summary>
    /// 游戏入口：负责状态切换、输入、对话框、选项框，并实现 IGameHost 供剧情脚本调用。
    /// 不需要手动放进场景——按 Play 时会自动创建（见 Boot）。
    /// 地图部分在 GameRoot.Map.cs，战斗部分在 GameRoot.Battle.cs。
    /// </summary>
    public partial class GameRoot : MonoBehaviour, IGameHost
    {
        [Tooltip("可选：指定一个中文字体。不指定时使用系统默认字体（Win/Mac 一般能显示中文）。")]
        public Font chineseFont;

        static GameRoot instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("LiuBeiRPG");
            go.AddComponent<GameRoot>();
        }

        Mode mode = Mode.Title;
        System.Random rng = new System.Random();

        Party party;
        StoryFlags flags;
        Chapter1 chapter;

        public Party Party { get { return party; } }
        public StoryFlags Flags { get { return flags; } }

        // ---------------- 生命周期 ----------------

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            // 中文显示成方块时：把任意中文 TTF/OTF 字体命名为 ChineseFont 放进 Assets/LiuBeiRPG/Resources/
            if (chineseFont == null) chineseFont = Resources.Load<Font>("ChineseFont");
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
        }

        void NewGame()
        {
            party = new Party();
            flags = new StoryFlags();
            dialogueQueue.Clear();
            currentDialogue = null;
            choice = null;
            chapter = new Chapter1(this);
            mode = Mode.Map;
            chapter.Begin();
        }

        void Update()
        {
            if (mode == Mode.Map) UpdateMap();
            if (mode == Mode.Battle) UpdateBattle();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) held.Clear();
        }

        // ---------------- 输入（用 IMGUI 事件，新旧 Input System 都能用） ----------------

        readonly HashSet<InputKey> held = new HashSet<InputKey>();

        static bool MapKey(KeyCode k, out InputKey key)
        {
            switch (k)
            {
                case KeyCode.UpArrow: case KeyCode.W: key = InputKey.Up; return true;
                case KeyCode.DownArrow: case KeyCode.S: key = InputKey.Down; return true;
                case KeyCode.LeftArrow: case KeyCode.A: key = InputKey.Left; return true;
                case KeyCode.RightArrow: case KeyCode.D: key = InputKey.Right; return true;
                case KeyCode.Z: case KeyCode.Space: case KeyCode.Return: case KeyCode.KeypadEnter: case KeyCode.J:
                    key = InputKey.Confirm; return true;
                case KeyCode.X: case KeyCode.Escape: case KeyCode.Backspace: case KeyCode.K:
                    key = InputKey.Cancel; return true;
            }
            key = InputKey.Confirm;
            return false;
        }

        void ProcessInputEvent(Event e)
        {
            InputKey key;
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None && MapKey(e.keyCode, out key))
            {
                bool repeat = held.Contains(key);
                held.Add(key);
                e.Use();
                if (!repeat) OnKey(key);
            }
            else if (e.type == EventType.KeyUp && MapKey(e.keyCode, out key))
            {
                held.Remove(key);
                e.Use();
            }
        }

        void OnKey(InputKey key)
        {
            // 对话 / 选项优先
            if (choice != null) { ChoiceKey(key); return; }
            if (currentDialogue != null)
            {
                if (key == InputKey.Confirm || key == InputKey.Cancel) AdvanceDialogue();
                return;
            }

            switch (mode)
            {
                case Mode.Title:
                    if (key == InputKey.Confirm) NewGame();
                    break;
                case Mode.Map:
                    MapKeyDown(key);
                    break;
                case Mode.Battle:
                    BattleKey(key);
                    break;
                case Mode.GameOver:
                    if (key == InputKey.Confirm) { mode = Mode.Map; chapter.RespawnAfterDefeat(); }
                    break;
                case Mode.ChapterClear:
                    if (key == InputKey.Confirm) mode = Mode.Title;
                    break;
            }
        }

        // ---------------- 对话框 ----------------

        class DialogueBatch
        {
            public List<DialogueLine> Lines;
            public Action OnDone;
            public int Index;
        }

        readonly Queue<DialogueBatch> dialogueQueue = new Queue<DialogueBatch>();
        DialogueBatch currentDialogue;

        public void Say(List<DialogueLine> lines, Action onDone = null)
        {
            if (lines == null || lines.Count == 0) { if (onDone != null) onDone(); return; }
            var b = new DialogueBatch { Lines = lines, OnDone = onDone };
            if (currentDialogue == null) currentDialogue = b;
            else dialogueQueue.Enqueue(b);
        }

        void AdvanceDialogue()
        {
            var b = currentDialogue;
            b.Index++;
            if (b.Index < b.Lines.Count) return;
            currentDialogue = dialogueQueue.Count > 0 ? dialogueQueue.Dequeue() : null;
            if (b.OnDone != null) b.OnDone();
        }

        bool UiBusy { get { return currentDialogue != null || choice != null; } }

        /// <summary>对话框贴屏幕底部，横竖屏通用。</summary>
        static Rect DialogRect { get { return new Rect(12, Gfx.VH - 148, Gfx.VW - 24, 136); } }

        void DrawDialogue()
        {
            if (currentDialogue == null) return;
            var line = currentDialogue.Lines[currentDialogue.Index];
            var r = DialogRect;
            Gfx.Window(r);
            float y = r.y + 14;
            if (!string.IsNullOrEmpty(line.Speaker))
            {
                Gfx.Text(new Rect(r.x + 20, y, 300, 26), line.Speaker, 20, new Color(1f, 0.85f, 0.35f));
                y += 30;
            }
            Gfx.Text(new Rect(r.x + 20, y, r.width - 40, r.yMax - y - 10), line.Text, 20, Color.white, TextAnchor.UpperLeft, true);
            if (Mathf.Repeat(Time.time, 0.8f) < 0.5f)
                Gfx.Text(new Rect(r.xMax - 34, r.yMax - 30, 20, 20), "▼", 16, Color.white);

            if (Gfx.Clicked(r)) AdvanceDialogue();
        }

        // ---------------- 选项框 ----------------

        class ChoiceState
        {
            public string Question;
            public string[] Options;
            public int Selected;
            public Action<int> OnChoose;
        }
        ChoiceState choice;

        public void Choose(string question, string[] options, Action<int> onChoose)
        {
            choice = new ChoiceState { Question = question, Options = options, OnChoose = onChoose };
        }

        void ChoiceKey(InputKey key)
        {
            if (key == InputKey.Up) choice.Selected = (choice.Selected + choice.Options.Length - 1) % choice.Options.Length;
            else if (key == InputKey.Down) choice.Selected = (choice.Selected + 1) % choice.Options.Length;
            else if (key == InputKey.Confirm) PickChoice(choice.Selected);
            else if (key == InputKey.Cancel) PickChoice(choice.Options.Length - 1);
        }

        void PickChoice(int i)
        {
            var c = choice;
            choice = null;
            if (c.OnChoose != null) c.OnChoose(i);
        }

        void DrawChoice()
        {
            if (choice == null) return;
            var r = DialogRect;
            Gfx.Window(r);
            Gfx.Text(new Rect(r.x + 20, r.y + 16, r.width - 40, 60), choice.Question, 20, Color.white, TextAnchor.UpperLeft, true);

            var box = new Rect(r.xMax - 200, r.y - 30 - choice.Options.Length * 34, 200, 20 + choice.Options.Length * 34);
            Gfx.Window(box);
            for (int i = 0; i < choice.Options.Length; i++)
            {
                var ir = new Rect(box.x + 12, box.y + 10 + i * 34, box.width - 24, 32);
                bool sel = i == choice.Selected;
                if (sel) Gfx.Rect(ir, new Color(1, 1, 1, 0.12f));
                Gfx.Text(new Rect(ir.x + 6, ir.y + 4, ir.width, ir.height), (sel ? "▶ " : "　 ") + choice.Options[i], 20, sel ? Color.yellow : Color.white);
                if (Gfx.Clicked(ir)) { PickChoice(i); return; }
            }
        }

        // ---------------- IGameHost 其他实现 ----------------

        public void ChapterClear(string title, string nextHint)
        {
            clearTitle = title;
            clearHint = nextHint;
            mode = Mode.ChapterClear;
        }

        string clearTitle, clearHint;

        // ---------------- 绘制 ----------------

        void OnGUI()
        {
            if (chineseFont != null && Gfx.CustomFont != chineseFont)
            {
                Gfx.CustomFont = chineseFont;
                Gfx.ClearStyleCache();
            }

            ProcessInputEvent(Event.current);

            GUI.matrix = Matrix4x4.identity;
            Gfx.Rect(new Rect(0, 0, Screen.width, Screen.height), Color.black);
            Gfx.BeginScaled();

            switch (mode)
            {
                case Mode.Title: DrawTitle(); break;
                case Mode.Map: DrawMap(); break;
                case Mode.Battle: DrawBattle(); break;
                case Mode.GameOver: DrawGameOver(); break;
                case Mode.ChapterClear: DrawChapterClear(); break;
            }

            if (choice != null) DrawChoice();
            else DrawDialogue();
        }

        void DrawTitle()
        {
            float oy = (Gfx.VH - 540f) * 0.5f;   // 内容按 540 高排版，竖屏时垂直居中
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.06f, 0.04f, 0.08f));
            // 简单的“桃花”点缀
            var prng = new System.Random(7);
            for (int i = 0; i < 60; i++)
            {
                float x = (float)prng.NextDouble() * Gfx.VW;
                float y = (float)((prng.NextDouble() * Gfx.VH + Time.time * (12 + i % 7 * 4)) % Gfx.VH);
                float s = 3 + i % 4;
                Gfx.Rect(new Rect(x, y, s, s), new Color(0.95f, 0.55f, 0.7f, 0.5f));
            }
            Gfx.Text(new Rect(0, oy + 140, Gfx.VW, 70), "汉末英雄传", 56, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 215, Gfx.VW, 40), "—— 刘备篇 · 第一章 桃园结义 ——", 24, Color.white, TextAnchor.MiddleCenter);
            if (Mathf.Repeat(Time.time, 1.2f) < 0.8f)
                Gfx.Text(new Rect(0, oy + 360, Gfx.VW, 30), "按 Z / 空格 / 回车 开始", 20, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 500, Gfx.VW, 24), "Unity Demo · 方向键移动 · Z 确认 · X 取消", 14, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter);
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) NewGame();
        }

        void DrawGameOver()
        {
            float oy = (Gfx.VH - 540f) * 0.5f;   // 内容按 540 高排版，竖屏时垂直居中
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.12f, 0.02f, 0.02f));
            Gfx.Text(new Rect(0, oy + 190, Gfx.VW, 60), "全 军 覆 没", 48, new Color(0.9f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 270, Gfx.VW, 30), "按确认键退回涿县休整（金钱减半）", 20, Color.white, TextAnchor.MiddleCenter);
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) { mode = Mode.Map; chapter.RespawnAfterDefeat(); }
        }

        void DrawChapterClear()
        {
            float oy = (Gfx.VH - 540f) * 0.5f;   // 内容按 540 高排版，竖屏时垂直居中
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.04f, 0.05f, 0.1f));
            Gfx.Text(new Rect(0, oy + 150, Gfx.VW, 60), clearTitle, 42, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 230, Gfx.VW, 30),
                "全军等级 " + party.Level + "　　金钱 " + party.Gold, 20, Color.white, TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 280, Gfx.VW, 30), clearHint + "（敬请期待）", 22, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 400, Gfx.VW, 30), "按确认键回到标题", 18, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) mode = Mode.Title;
        }
    }
}
