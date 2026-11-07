using System;
using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;
using Puppet;

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
        Chapter chapter;

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
            SetupCamera();
        }

        void NewGame()
        {
            party = new Party();
            flags = new StoryFlags();
            dialogueQueue.Clear();
            currentDialogue = null;
            choice = null;
            chestFx = null;
            smokeSteps = 0;
            StartChapter(1);
            if (Application.isEditor && DevStartAtField)
            {
                dialogueQueue.Clear();
                currentDialogue = null;
                chapter.SkipToField();   // 第一章：结义、受赠都算完成，站到城外
                while (party.Level < DevLevel) party.GainExp(party.ExpToNext - party.Exp);
                party.FullRestore();
                if (DevChapter > 1) { flags.Set(Chapter1.F_Boss); StartChapter(DevChapter); dialogueQueue.Clear(); currentDialogue = null; chapter.SkipToField(); }
            }
        }

        // ponytail: 调试开关，编辑器里新游戏直接从城外开始（城内剧情算走完）、直接到 DevLevel 级、直接进第 DevChapter 章；测完改回 false
        const bool DevStartAtField = true;
        const int DevLevel = 10;
        const int DevChapter = 2;

        Chapter MakeChapter(int n)
        {
            switch (n)
            {
                case 1: return new Chapter1(this);
                case 2: return new Chapter2(this);
                case 3: return new Chapter3(this);
                case 4: return new Chapter4(this);
                case 5: return new Chapter5(this);
                case 6: return new Chapter6(this);
                case 7: return new Chapter7(this);
                default: return null;
            }
        }

        /// <summary>开始第 n 章：队伍、物品、flags 都带着，只换剧情和地图。没有这一章就回标题。</summary>
        public bool StartChapter(int n)
        {
            dialogueQueue.Clear();
            currentDialogue = null;
            choice = null;
            chestFx = null;
            chapter = MakeChapter(n);
            if (chapter == null) { mode = Mode.Title; return false; }
            mode = Mode.Map;
            chapter.Begin();
            return true;
        }

        void Update()
        {
            UpdateTouch();
            UpdateChest();
            if (mode == Mode.Map) UpdateMap();
            if (mode == Mode.Map && map != null) { UpdateCamera(); if (!UiBusy) UpdateWalkers(); }
            TickLeader();
            foreach (var p in portraits.Values) p.Tick(Time.deltaTime);
            if (mode == Mode.Battle) UpdateBattle();
            // 结算时让出来给胜利号角；全军覆没、通关静音
            Sfx.Music(mode == Mode.Title ? "bgm_title" : mode == Mode.Map ? "bgm_map" : mode == Mode.Battle && bstate != BState.Result ? "bgm_battle" : null);
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
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.G) { showGrid = !showGrid; e.Use(); return; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab && menuPage == PagePeople) { bagTab = (bagTab + 1) % 3; e.Use(); return; }   // 人物页切 物品 / 法术 / 任务
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab && mode == Mode.Battle && menuPage == 0) { scouting = !scouting; e.Use(); return; }   // 战斗里开关侦察
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
            if (ChestPlaying) return;   // 开箱演出时不吃按键
            KeySfx(key);
            // 对话 / 选项优先
            if (choice != null) { ChoiceKey(key); return; }
            if (currentDialogue != null)
            {
                if (key == InputKey.Confirm || key == InputKey.Cancel) AdvanceDialogue();
                return;
            }
            if (menuPage != 0) { MenuPageKey(key); return; }

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
                    if (key == InputKey.Confirm) StartChapter(chapter.Number + 1);
                    break;
            }
        }

        /// <summary>界面里按键的声：挪光标 / 确认 / 取消。地图上走路、调查和战报播放时不响。</summary>
        void KeySfx(InputKey key)
        {
            if (currentDialogue != null) return;   // 对话翻页的声在 AdvanceDialogue
            bool ui = choice != null || menuPage != 0 || mode != Mode.Map && !(mode == Mode.Battle && (bstate == BState.Playing || bstate == BState.Result));   // 结算面板的声在 EndBattle
            if (!ui) return;
            if (key == InputKey.Confirm) Sfx.Play("ui_tap");
            else if (key == InputKey.Cancel) Sfx.Play("ui_cancel");
            else Sfx.Play("ui_move", 0.6f);
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
            Sfx.Play("dialog_next", 0.35f);   // 翻页：比按钮清脆、轻
            var b = currentDialogue;
            // 还在打字：先把这句一次显示完
            if (shownLine == b.Lines[b.Index] && Revealed(shownLine) < shownLine.Text.Length) { lineStart = -1000f; return; }
            b.Index++;
            if (b.Index < b.Lines.Count) return;
            currentDialogue = dialogueQueue.Count > 0 ? dialogueQueue.Dequeue() : null;
            if (b.OnDone != null) b.OnDone();
        }

        bool UiBusy { get { return currentDialogue != null || choice != null || menuPage != 0 || ChestPlaying; } }

        /// <summary>对话框贴在安全区域底部居中（最宽 900），横竖屏使用不同高度。</summary>
        static Rect DialogRect
        {
            get
            {
                Rect safe = Gfx.SafeArea;
                bool portrait = Gfx.Portrait;
                float margin = portrait ? 14f : 16f;
                float height = portrait ? 228f : 132f;
                float width = Mathf.Min(safe.width - margin * 2f, 900f);
                return new Rect(safe.x + (safe.width - width) * 0.5f, safe.yMax - height - margin, width, height);
            }
        }

        static readonly Color Accent = new Color(0.96f, 0.76f, 0.32f);   // 名字牌、选中项的强调色
        const float CharsPerSecond = 45f;                                 // 打字机速度

        // 说话人 → 骨骼角色（半身像）。没列的（旁白、？？？、大汉、黄巾将领）不显示头像
        static readonly Dictionary<string, string> PortraitIds = new Dictionary<string, string>
        {
            { "刘备", "liubei" }, { "关羽", "guanyu" }, { "张飞", "zhangfei" }, { "母亲", "muqin" },
            { "守卫", "shouwei" }, { "张世平", "huolang" }, { "店小二", "xiaoer" }, { "逃难村民", "nanmin" },
            { "简雍", "jianyong" }, { "龚景", "gongjing" }, { "张宝", "zhangbao" }, { "张梁", "zhangliang" }, { "张角", "zhangjiao" },
            // 第三～七章（sanguoguge 2026-11-07 做好）
            { "赵云", "zhaoyun" }, { "孙乾", "sunqian" }, { "糜竺", "mizhu" }, { "陈到", "chendao" }, { "周仓", "zhoucang" }, { "关平", "guanping" },
            { "诸葛亮", "zhugeliang" }, { "徐庶", "xushu" }, { "单福", "xushu" }, { "刘封", "liufeng" }, { "寇封", "liufeng" }, { "华雄", "huaxiong" },
            { "吕布", "lvbu" }, { "管亥", "guanhai" }, { "纪灵", "jiling" }, { "车胄", "chezhou" }, { "典韦", "dianwei" }, { "许褚", "xuchu" },
            { "刘辟", "liupi" }, { "龚都", "gongdu" }, { "蔡阳", "caiyang" }, { "夏侯惇", "xiahoudun" }, { "曹仁", "caoren" }, { "曹操", "caocao" },
            { "袁绍", "yuanshao" }, { "公孙瓒", "gongsunzan" }, { "孔融", "kongrong" }, { "太史慈", "taishici" }, { "陶谦", "taoqian" }, { "董承", "dongcheng" },
            { "诸葛均", "zhugejun" }, { "童子", "xiaohai" }, { "掌柜", "shangren" }, { "随军货郎", "huolang" }, { "关定", "laoren" },
        };
        readonly Dictionary<string, PuppetActor> portraits = new Dictionary<string, PuppetActor>();
        DialogueLine shownLine;   // 正在打字的那句
        float lineStart;

        PuppetActor Portrait(string speaker)
        {
            string id;
            if (speaker == null || !PortraitIds.TryGetValue(speaker, out id)) return null;
            PuppetActor a;
            if (!portraits.TryGetValue(id, out a)) portraits[id] = a = PuppetActor.Load(id);   // 单独一份，不和地图上的朝向互相影响
            // 武器跟地图上的同一个人一致（守卫拿枪、三兄弟受赠后拿兵器）
            PuppetActor onMap;
            if (puppets.TryGetValue(id, out onMap) && onMap != null) a.Equip(onMap.Weapon == null ? null : onMap.Weapon.name);
            return a;
        }

        int Revealed(DialogueLine line)
        {
            return Mathf.Min(line.Text.Length, (int)((Time.time - lineStart) * CharsPerSecond));
        }

        float dialogueOpenTime, speakerTime;   // 对话框弹出、换人说话的时刻（做入场动画）
        bool dialogueWasOpen;
        string lastSpeaker;

        static float EaseOut(float since, float dur) { float t = 1f - Mathf.Clamp01((Time.time - since) / dur); return 1f - t * t; }

        void DrawDialogue()
        {
            if (currentDialogue == null) { dialogueWasOpen = false; return; }
            if (!dialogueWasOpen) { dialogueWasOpen = true; dialogueOpenTime = Time.time; lastSpeaker = null; }
            var line = currentDialogue.Lines[currentDialogue.Index];
            if (line != shownLine) { shownLine = line; lineStart = Time.time; }
            if (line.Speaker != lastSpeaker) { lastSpeaker = line.Speaker; speakerTime = Time.time; }
            int shown = Revealed(line);
            var r = DialogRect;
            r.y += (1f - EaseOut(dialogueOpenTime, 0.22f)) * 40f;   // 弹出时从下面升上来
            float slide = (1f - EaseOut(speakerTime, 0.25f)) * -36f;  // 换人说话：头像和名字牌从左边滑进来
            bool portrait = Gfx.Portrait;
            int size = portrait ? 22 : 20;

            Gfx.Modern(r, 0.94f);
            // 半身像：画在框前面，人站在框左边，头肩探出框上沿
            var bust = Portrait(line.Speaker);
            if (bust != null) bust.DrawGUI(new Vector2(r.x + 96 + slide, r.yMax + 20), 0.28f);
            float tx = r.x + (bust != null ? 196 : 32);
            if (!string.IsNullOrEmpty(line.Speaker))
            {
                // 名字牌压在框的上边
                var tag = new Rect(tx - 12 + slide, r.y - 16, 28 + line.Speaker.Length * 19, 32);
                Gfx.RoundRect(tag, Accent);
                Gfx.Text(tag, line.Speaker, 18, new Color(0.12f, 0.09f, 0.04f), TextAnchor.MiddleCenter, false, false);
            }
            Gfx.Text(new Rect(tx, r.y + 30, r.xMax - tx - 48, r.height - 40), line.Text.Substring(0, shown), size,
                new Color(0.95f, 0.95f, 0.95f), TextAnchor.UpperLeft, true);
            if (shown >= line.Text.Length)   // 打完了才出继续箭头，上下轻轻跳
                Gfx.Text(new Rect(r.xMax - 40, r.yMax - 34 + Mathf.Sin(Time.time * 6f) * 3f, 20, 20), "▼", 14, Accent);

            // 刚弹出的一瞬不吃点击：手机上按“确认”键调查时，同一下触摸会再被当成点了对话框
            if (Gfx.Clicked(r, null) && Time.time - dialogueOpenTime > 0.1f) AdvanceDialogue();
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
            Gfx.Modern(r);
            Gfx.Text(new Rect(r.x + 32, r.y + 24, r.width - 64, r.height - 40), choice.Question, Gfx.Portrait ? 24 : 22, Color.white, TextAnchor.UpperLeft, true);

            // 选项框大一点，手机上好点
            const float rowH = 54f;
            var box = new Rect(r.xMax - 280, r.y - 16 - 24 - choice.Options.Length * rowH, 280, 24 + choice.Options.Length * rowH);
            Gfx.Modern(box);
            for (int i = 0; i < choice.Options.Length; i++)
            {
                var ir = new Rect(box.x + 14, box.y + 12 + i * rowH, box.width - 28, rowH - 4);
                bool sel = i == choice.Selected;
                Gfx.MenuRow(ir, choice.Options[i], 26, sel, sel ? Accent : Color.white);
                if (Gfx.Clicked(ir)) { PickChoice(i); return; }
            }
        }

        // ---------------- IGameHost 其他实现 ----------------

        public void ChapterClear(string title, string nextHint)
        {
            clearTitle = title;
            clearHint = nextHint;
            mode = Mode.ChapterClear;
            Sfx.Play("chapter_clear");
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
            Gfx.Paper = false;
            if (mode != Mode.Map && mode != Mode.Battle) Gfx.Rect(new Rect(0, 0, Screen.width, Screen.height), Color.black);   // 大地图由相机画在底下
            Gfx.BeginScaled();

            switch (mode)
            {
                case Mode.Title: DrawTitle(); break;
                case Mode.Map: DrawMap(); break;
                case Mode.Battle: DrawBattle(); break;
                case Mode.GameOver: DrawGameOver(); break;
                case Mode.ChapterClear: DrawChapterClear(); break;
            }

            DrawMapTitle();
            DrawTouchUi();
            DrawMenuPage();
            if (choice != null) DrawChoice();
            else DrawDialogue();
            DrawNote();

            // 转场：切地图、进出战斗时从黑场淡入
            float fade = 1f - (Time.time - fadeStart) / FadeTime;
            if (fade > 0f) Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, fade * fade));
            DrawIris();   // 切地图：圆形展开
        }

        const float FadeTime = 0.45f;
        float fadeStart = -10f;

        void FadeIn() { fadeStart = Time.time; }

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
            bool hasNext = MakeChapter(chapter.Number + 1) != null;
            Gfx.Text(new Rect(0, oy + 280, Gfx.VW, 30), clearHint + (hasNext ? "" : "（敬请期待）"), 22, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 400, Gfx.VW, 30), hasNext ? "按确认键继续" : "按确认键回到标题", 18, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) StartChapter(chapter.Number + 1);
        }
    }
}
