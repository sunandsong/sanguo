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
            StoryFlags.DevAll = DevWalkOn;
            StartChapter(1);
            if (Application.isEditor && DevStartAtField)
            {
                dialogueQueue.Clear();
                currentDialogue = null;
                comicPages = null;
                chapter.SkipToField();   // 第一章：结义、受赠都算完成，站到城外
                while (party.Level < DevLevel) party.GainExp(party.ExpToNext - party.Exp);
                party.FullRestore();
                if (DevChapter > 1) { flags.Set(Chapter1.F_Boss); StartChapter(DevChapter); dialogueQueue.Clear(); currentDialogue = null; chapter.SkipToField(); }
                if (DevMap != null) GoChapter(DevChapter, DevMap, DevX, DevY);
            }
            if (Application.isEditor && DevTenBattle)   // 看 10 人上阵的站位：队伍凑满 10 人，直接开一场 5 个敌人（带首领）的仗
            {
                dialogueQueue.Clear();
                currentDialogue = null;
                comicPages = null;
                while (party.Level < 30) party.GainExp(party.ExpToNext - party.Exp);
                var extra = new[] { GameDatabase.GuanYu, GameDatabase.ZhangFei, GameDatabase.JianYong, GameDatabase.ZhaoYun, GameDatabase.SunQian, GameDatabase.MiZhu,
                                    GameDatabase.ChenDao, GameDatabase.ZhouCang, GameDatabase.GuanPing, GameDatabase.ZhuGeLiang, GameDatabase.XuShu };
                foreach (var d in extra) if (party.Members.Count < Party.MaxMembers && !party.Has(d)) party.Add(new General(d, party.Level));
                party.FullRestore();
                StartBattle(GameDatabase.Chapter5XuChu(), false);
            }
            if (Application.isEditor && DevEnding)   // 直接看「未完待续」
            {
                StartChapter(7);
                dialogueQueue.Clear();
                currentDialogue = null;
                ChapterClear("第七章 三顾茅庐 · 完", "第八章：长坂·赤壁");
            }
        }

        // ponytail: 调试开关，编辑器里点「新游戏」直接跳到最后的「未完待续」页；看完改回 false
        const bool DevEnding = false;

        // ponytail: 调试开关，编辑器里新游戏直接从城外开始（城内剧情算走完）、直接到 DevLevel 级、直接进第 DevChapter 章；测完改回 false
        const bool DevStartAtField = false;
        const int DevLevel = 10;
        const int DevChapter = 5;
        const string DevMap = null;   // 新游戏直接站到这张图（null = 按章节默认）
        const int DevX = 15, DevY = 17;
        // ponytail: 测试逛图开关：不遇敌、所有剧情 flag 算已完成（出口全通、boss 和宝箱都没了）；测完改回 false
        const bool DevWalk = false;
        // ponytail: 调试开关，编辑器里新游戏直接凑满 10 人开一仗，看战斗站位；看完改回 false
        const bool DevTenBattle = false;
        static bool DevWalkOn { get { return Application.isEditor && DevWalk; } }

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
            comicPages = null;
            chapter = MakeChapter(n);
            if (chapter == null) { mode = Mode.Title; titleShownAt = -1; return false; }
            mode = Mode.Map;
            flags.Set("c" + n + "_begun");   // 以后走回这一章就不重放开场（Chapter.Cross）
            chapter.Begin();
            return true;
        }

        /// <summary>跨章节走地图：换到第 n 章的某张图，不放开场剧情。</summary>
        public void GoChapter(int n, string mapId, int x, int y)
        {
            chapter = MakeChapter(n);
            mode = Mode.Map;
            Warp(Array.Find(chapter.Maps, m => m.Id == mapId) ?? chapter.Maps[0], x, y);
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
            // Delete / Backspace：队伍页放生选中的人，人物页物品栏扔掉选中的东西
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) && choice == null && currentDialogue == null)
            {
                if (menuPage == PageTeam) { TeamRelease(teamSel); e.Use(); return; }
                if (menuPage == PagePeople && bagTab == 0) { DiscardItem(); e.Use(); return; }
            }
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
            if (comicPages != null) { ComicKey(key); return; }   // 开场漫画盖在最上面
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
                    if ((key == InputKey.Up || key == InputKey.Down) && HasSave) { titleSel = 1 - titleSel; Sfx.Play("ui_tap"); }
                    else if (key == InputKey.Confirm) TitlePick(HasSave ? titleSel : 0);
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
            if (lines == null || lines.Count == 0 || DevWalkOn) { if (onDone != null) onDone(); return; }   // 逛图模式不放对话
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

        bool UiBusy { get { return currentDialogue != null || choice != null || menuPage != 0 || ChestPlaying || comicPages != null; } }

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
            // 框宽跟着最长的一项走（读档的「第一档：Lv 23　联军大营」280 放不下），至少 280，不超出屏幕
            float bw = 280;
            foreach (var o in choice.Options) bw = Mathf.Max(bw, Gfx.Style(26).CalcSize(new GUIContent(o)).x + 14 + 28 + 8);
            bw = Mathf.Min(bw, Gfx.VW - 24);
            var box = new Rect(Mathf.Max(12, r.xMax - bw), r.y - 16 - 24 - choice.Options.Length * rowH, bw, 24 + choice.Options.Length * rowH);
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
            clearShownAt = Time.time;
            mode = Mode.ChapterClear;
            Sfx.Play("chapter_clear");
        }

        string clearTitle, clearHint;
        float clearShownAt;

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
            if (comicPages != null) DrawComic();
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
            if (titleShownAt < 0) titleShownAt = Time.time;   // 每次回到首页重新播一遍入场
            float t = Time.time - titleShownAt;
            bool hasSave = HasSave;
            if (!hasSave) titleSel = 0;

            // 背景：等比铺满（竖屏裁两边），慢慢推近一点点
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.1f, 0.08f, 0.2f));
            var bg = Gfx.Tex("Art/title_bg");
            if (bg != null)
            {
                float s = Mathf.Max(Gfx.VW / bg.width, Gfx.VH / bg.height) * (1.02f + 0.02f * Mathf.Sin(Time.time * 0.15f));
                float w = bg.width * s, h = bg.height * s;
                Gfx.Image(new Rect((Gfx.VW - w) * 0.5f, (Gfx.VH - h) * 0.5f, w, h), bg);
            }
            // 底部压暗，按钮和提示字更清楚
            for (int i = 0; i < 24; i++)
                Gfx.Rect(new Rect(0, Gfx.VH * (0.55f + i * 0.45f / 24), Gfx.VW, Gfx.VH * 0.45f / 24 + 1), new Color(0.05f, 0.03f, 0.12f, 0.025f * i));

            TitlePetals();

            // 标题字：从上方落下回弹，之后轻轻浮动
            var logo = Gfx.Tex("Art/title_logo");
            if (logo != null)
            {
                float lw = Mathf.Min(620, Gfx.VW - 40), lh = lw * logo.height / logo.width;
                float a = Mathf.Clamp01(t / 0.6f), drop = (1 - EaseOutBack(a)) * -60;
                float bob = Mathf.Sin(Time.time * 1.6f) * 4;
                GUI.color = new Color(1, 1, 1, a);
                Gfx.Image(new Rect((Gfx.VW - lw) * 0.5f, oy + 30 + drop + bob, lw, lh), logo);
                GUI.color = Color.white;
            }

            // 菜单：新游戏 / 读档，竖排，晚一点淡入
            string[] items = { "新游戏", "读档" };
            for (int i = 0; i < 2; i++)
            {
                float a = Mathf.Clamp01((t - 0.5f - i * 0.12f) / 0.4f);
                if (a <= 0) continue;
                bool enabled = i == 0 || hasSave, sel = i == titleSel;
                // Q 版扁平胶囊：深色粗描边 + 实心投影 + 底下一道深色“唇边”，和标题字一个画风
                float bw = 230, bh = 56, slide = (1 - a) * 30;
                float lift = sel ? 3 + Mathf.Abs(Mathf.Sin(Time.time * 4)) * 3 : 0;   // 选中的一蹦一蹦
                var r = new Rect((Gfx.VW - bw) * 0.5f, oy + 335 + i * 78 + slide - lift, bw, bh);
                var ink = new Color(0.13f, 0.1f, 0.28f, a);
                Color top, lip, tc;
                if (!enabled) { top = new Color(0.84f, 0.82f, 0.88f, a); lip = new Color(0.68f, 0.66f, 0.75f, a); tc = new Color(0.52f, 0.5f, 0.6f, a); }
                else if (sel) { top = new Color(1f, 0.83f, 0.25f, a); lip = new Color(1f, 0.58f, 0.12f, a); tc = ink; }
                else { top = new Color(1f, 0.97f, 0.9f, a); lip = new Color(0.9f, 0.82f, 0.72f, a); tc = ink; }
                var o = new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8);
                Gfx.Pill(new Rect(o.x, o.y + 5 + lift, o.width, o.height), new Color(0.1f, 0.07f, 0.2f, 0.45f * a));   // 投影，蹦起来时离得远
                Gfx.Pill(o, ink);
                Gfx.Pill(r, lip);
                Gfx.Pill(new Rect(r.x + 3, r.y + 2, r.width - 6, r.height - 9), top);
                Gfx.Pill(new Rect(r.x + 20, r.y + 8, 24, 8), new Color(1, 1, 1, 0.85f * a));   // 左上角的小高光
                Gfx.Pill(new Rect(r.x + 48, r.y + 8, 8, 8), new Color(1, 1, 1, 0.85f * a));
                Gfx.Text(new Rect(r.x, r.y - 3, r.width, r.height), items[i], 28, tc, TextAnchor.MiddleCenter, false, false);
                if (a >= 1 && enabled && Gfx.Clicked(o)) { titleSel = i; TitlePick(i); }
            }
            float ha = Mathf.Clamp01((t - 1.2f) / 0.5f) * 0.75f;
            Gfx.Text(new Rect(0, Gfx.VH - 36, Gfx.VW, 24), "泉都发科技有限公司出品", 15, new Color(1, 1, 1, ha), TextAnchor.MiddleCenter);
        }

        int titleSel;
        float titleShownAt = -1;   // < 0 = 下一帧画首页时重新计时

        void TitlePick(int i)
        {
            if (i == 1 && HasSave) ContinueGame(); else NewGame();
        }

        static float EaseOutBack(float x) { const float c = 1.7f; x -= 1; return 1 + (c + 1) * x * x * x + c * x * x; }

        /// <summary>首页飘的桃花瓣：和地图天气同一张软边椭圆贴图，染粉色，边落边转。</summary>
        void TitlePetals()
        {
            var tex = PetalTex();
            var prng = new System.Random(7);
            float tt = Time.time;
            var old = GUI.matrix;
            for (int i = 0; i < 40; i++)
            {
                float bx = (float)prng.NextDouble(), phase = (float)prng.NextDouble();
                float speed = 30 + (float)prng.NextDouble() * 40, size = 8 + (float)prng.NextDouble() * 8;
                float y = Mathf.Repeat(phase * (Gfx.VH + 40) + tt * speed, Gfx.VH + 40) - 20;
                float x = Mathf.Repeat(bx * Gfx.VW + Mathf.Sin(tt * (0.8f + phase) + i) * 30 + tt * 20, Gfx.VW + 40) - 20;
                var c = new Vector3(x, y);
                GUI.matrix = old * Matrix4x4.TRS(c, Quaternion.Euler(0, 0, (tt * (60 + i % 5 * 25) + i * 47) % 360f), Vector3.one) * Matrix4x4.TRS(-c, Quaternion.identity, Vector3.one);
                GUI.color = new Color(1f, 0.75f, 0.85f, 0.85f);
                Gfx.Image(new Rect(x - size * 0.5f, y - size * 0.33f, size, size * 0.66f), tex);
            }
            GUI.matrix = old;
            GUI.color = Color.white;
        }

        static bool HasSave { get { return PlayerPrefs.HasKey("save1") || PlayerPrefs.HasKey("save2") || PlayerPrefs.HasKey("save3"); } }

        /// <summary>标题读档：先起一局新游戏（建好章节和队伍），跳过开场白，再弹读档。</summary>
        void ContinueGame()
        {
            NewGame();
            dialogueQueue.Clear();
            currentDialogue = null;
            comicPages = null;   // 开场漫画也跳过
            LoadGame();
        }

        void DrawGameOver()
        {
            float oy = (Gfx.VH - 540f) * 0.5f;   // 内容按 540 高排版，竖屏时垂直居中
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.12f, 0.02f, 0.02f));
            Gfx.Text(new Rect(0, oy + 190, Gfx.VW, 60), "全 军 覆 没", 48, new Color(0.9f, 0.3f, 0.3f), TextAnchor.MiddleCenter);
            Gfx.Text(new Rect(0, oy + 270, Gfx.VW, 30), "按确认键撤回休整（金钱减半）", 20, Color.white, TextAnchor.MiddleCenter);
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) { mode = Mode.Map; chapter.RespawnAfterDefeat(); }
        }

        void DrawChapterClear()
        {
            if (MakeChapter(chapter.Number + 1) == null) { DrawToBeContinued(); return; }
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

        /// <summary>最后一章打完：雪夜卧龙岗背景（Art/end_bg），右边竖排章名，盖「未完待续」印章（Art/seal_weiwan）。
        /// clearTitle 形如「第七章 三顾茅庐 · 完」，clearHint 是下一章名。</summary>
        void DrawToBeContinued()
        {
            float t = Time.time - clearShownAt;
            float oy = (Gfx.VH - 540f) * 0.5f;
            var ink = new Color(0.13f, 0.1f, 0.28f);

            // 背景：从黑场淡入，慢慢推近
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), Color.black);
            var bg = Gfx.Tex("Art/end_bg");
            if (bg != null)
            {
                float s = Mathf.Max(Gfx.VW / bg.width, Gfx.VH / bg.height) * (1.0f + 0.04f * Mathf.Clamp01(t / 20f));
                float w = bg.width * s, h = bg.height * s;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(t / 1.2f));
                Gfx.Image(new Rect((Gfx.VW - w) * 0.5f, (Gfx.VH - h) * 0.5f, w, h), bg);
                GUI.color = Color.white;
            }
            // 雪：和花瓣同一张软边贴图，白色、慢、几乎不转
            var flake = PetalTex();
            var prng = new System.Random(3);
            for (int i = 0; i < 50; i++)
            {
                float bx = (float)prng.NextDouble(), ph = (float)prng.NextDouble(), sz = 4 + (float)prng.NextDouble() * 6;
                float y = Mathf.Repeat(ph * (Gfx.VH + 20) + Time.time * (18 + sz * 3), Gfx.VH + 20) - 10;
                float x = Mathf.Repeat(bx * Gfx.VW + Mathf.Sin(Time.time * 0.7f + i) * 18, Gfx.VW);
                GUI.color = new Color(1, 1, 1, 0.8f * Mathf.Clamp01(t / 1.2f));
                Gfx.Image(new Rect(x, y, sz, sz), flake);
            }
            GUI.color = Color.white;

            // 章名竖排，从右往左两列：「第七章」小字，「三顾茅庐」大字，一个字一个字落下
            string label = clearTitle, name = "";
            int sp = clearTitle.IndexOf(' ');
            if (sp > 0) { label = clearTitle.Substring(0, sp); name = clearTitle.Substring(sp + 1).Replace(" · 完", "").Trim(); }
            float colX = Gfx.VW - 70, top = oy + 50;
            float t0 = 1.0f;
            t0 = VerticalText(label, colX, top + 8, 26, new Color(1f, 0.88f, 0.5f), ink, t, t0);
            float bigX = colX - 78;
            t0 = VerticalText(name, bigX, top, 56, new Color(1f, 0.97f, 0.9f), ink, t, t0 + 0.1f);

            // 印章：章名写完后从大往小砸下来，带一点歪
            var seal = Gfx.Tex("Art/seal_weiwan");
            float st = t - (t0 + 0.2f);
            if (seal != null && st > 0)
            {
                float k = Mathf.Clamp01(st / 0.25f), scale = Mathf.Lerp(2.4f, 1f, k * k) + (k >= 1 ? 0.08f * Mathf.Exp(-(st - 0.25f) * 10) * Mathf.Sin((st - 0.25f) * 40) : 0);
                float size = 120 * scale;
                var c = new Vector2(bigX - 90, top + name.Length * 56 * 1.12f - 30);
                var old = GUI.matrix;
                GUI.matrix = old * Matrix4x4.TRS(c, Quaternion.Euler(0, 0, -8), Vector3.one) * Matrix4x4.TRS(-c, Quaternion.identity, Vector3.one);
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(k * 2));
                Gfx.Image(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), seal);
                GUI.color = Color.white;
                GUI.matrix = old;
            }

            // 下回预告：右下一枚深色胶囊
            float pa = Mathf.Clamp01((st - 0.6f) / 0.5f);
            if (pa > 0)
            {
                string next = "下回　" + clearHint.Replace("（未完待续）", "").Replace("：", "　");
                var pr = new Rect(Gfx.VW - 320 - 28, oy + 380 + (1 - pa) * 20, 320, 40);   // 右下，避开画面中下方骑马的三人
                Gfx.Pill(new Rect(pr.x - 3, pr.y - 3, pr.width + 6, pr.height + 6), new Color(1f, 0.83f, 0.25f, 0.9f * pa));
                Gfx.Pill(pr, new Color(ink.r, ink.g, ink.b, 0.9f * pa));
                Gfx.Text(pr, next, 20, new Color(1f, 0.92f, 0.65f, pa), TextAnchor.MiddleCenter, false, false);
                Gfx.Text(new Rect(16, Gfx.VH - 30, 300, 22), "全军等级 " + party.Level + "　金钱 " + party.Gold, 14, new Color(1, 1, 1, 0.55f * pa));
                float ha = pa * (0.5f + 0.3f * Mathf.Sin(Time.time * 2.5f));
                Gfx.Text(new Rect(0, Gfx.VH - 30, Gfx.VW - 16, 22), "按确认键回到标题", 14, new Color(1, 1, 1, ha), TextAnchor.MiddleRight);
            }
            if (t > 1f && Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) StartChapter(chapter.Number + 1);
        }

        /// <summary>竖排一列字，从 t0 起每 0.12 秒落下一个；深色粗描边（Q 版）。返回最后一个字落完的时刻。</summary>
        static float VerticalText(string text, float cx, float top, int size, Color fill, Color ink, float t, float t0)
        {
            for (int i = 0; i < text.Length; i++)
            {
                float a = Mathf.Clamp01((t - t0 - i * 0.12f) / 0.25f);
                if (a <= 0) break;
                var r = new Rect(cx - size, top + i * size * 1.12f - (1 - a) * 16, size * 2, size * 1.12f);
                string ch = text.Substring(i, 1);
                var ic = new Color(ink.r, ink.g, ink.b, a);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0) Gfx.Text(new Rect(r.x + dx * 3, r.y + dy * 3, r.width, r.height), ch, size, ic, TextAnchor.MiddleCenter, false, false);
                Gfx.Text(r, ch, size, new Color(fill.r, fill.g, fill.b, a), TextAnchor.MiddleCenter, false, false);
            }
            return t0 + text.Length * 0.12f + 0.25f;
        }
    }
}
