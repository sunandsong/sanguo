using System;
using System.Collections.Generic;

namespace LiuBeiRPG.World
{
    /// <summary>一种地块：能不能走、颜色、会不会遇敌。</summary>
    public class TileInfo
    {
        public bool Walkable;
        public string Color;
        public string Glyph;      // 可选的地块文字，例如“山”
        public bool Encounter;    // 走在上面会随机遇敌
        public string Sprite;     // Resources/Map 下的贴图名；没有贴图时退回色块
        public int Frames = 1;    // >1 时按 Sprite_0、Sprite_1… 循环播放

        public TileInfo(bool walkable, string color, string glyph = null, bool encounter = false)
        {
            Walkable = walkable; Color = color; Glyph = glyph; Encounter = encounter;
        }
    }

    public static class Tiles
    {
        // 地图字符 → 地块。改地图只需要改 Chapter1 里的字符串。
        public static readonly Dictionary<char, TileInfo> Legend = new Dictionary<char, TileInfo>
        {
            { '.', new TileInfo(true,  "#5c8f3e") { Sprite = "ground_town_grass" } },  // 城内草地
            { ',', new TileInfo(true,  "#6f9a45", null, true) { Sprite = "ground_wild_grass" } },  // 野外草地（遇敌）
            { '=', new TileInfo(true,  "#b89a63") { Sprite = "ground_road" } },  // 道路
            { 'f', new TileInfo(true,  "#d99bb0") { Sprite = "ground_peach_garden" } },  // 桃园地面
            { 'T', new TileInfo(false, "#2f5a2a", "木") { Sprite = "tree" } },  // 树
            { 'P', new TileInfo(false, "#e07a9a", "桃") { Sprite = "peach_tree" } },  // 桃树
            { 'S', new TileInfo(false, "#9a9a8a", "桌") },               // 桃园石桌
            { 'H', new TileInfo(false, "#8a5a3c") },                    // 房屋（整栋贴图见 MapData.Props）
            { 'M', new TileInfo(false, "#6e5a44", "山") { Sprite = "mountain" } },  // 山
            { '~', new TileInfo(false, "#3a6fb0") { Sprite = "water", Frames = 3 } },  // 水（深，过不去）
            { 'w', new TileInfo(true,  "#5a8fb8") },  // 浅水：能趟，水没到小腿
            { 'g', new TileInfo(true,  "#a07a4a", "门") { Sprite = "city_gate" } },  // 城门（出城）
            { 't', new TileInfo(true,  "#a07a4a", "城") { Sprite = "town_entrance" } },  // 野外 → 回城
            { 'C', new TileInfo(false, "#7a2e2e", "营") },               // 黄巾营寨（整张贴图见 MapData.Props）
        };

        public static TileInfo Get(char c)
        {
            TileInfo t;
            return Legend.TryGetValue(c, out t) ? t : Legend['.'];
        }
    }

    public class Npc
    {
        public string Id;
        public string Name;
        public string Glyph;
        public string Sprite;     // 有贴图就画贴图，否则画字块
        public float Size = 1.3f; // 贴图画几格大
        public string Weapon;     // 骨骼角色右手拿的武器（qiang / dao / jian），null = 空手
        public string Color = "#ffffff";
        public int X, Y;
        public Func<bool> Visible = () => true;
        public Action Talk;

        public string Skin;       // 骨骼角色名，空 = 用 Id（几个路人共用一个角色时填）
        public int[] Route;       // 路人的巡游路线：x0,y0, x1,y1, …，一格一格走过去，到头再从第一个点开始
        public float StepSeconds = 0.6f;   // 路人走一格几秒：老人慢、小孩快
        public int[] Home;        // 路人的家门口 x,y：天黑走回这里进屋，天亮从这里出来；null = 不回家
        public bool Indoors;      // 进屋了（不画、不能搭话）
        // 运行时（GameRoot.Walkers）
        public int PrevX, PrevY, RouteIndex;
        public float StepTime = -10f, WaitUntil, BlockedSince = -1f;
        public string Facing = "front";
    }

    /// <summary>物件怎么动（对应 World.shader 的 _Fx.w）。</summary>
    public enum Motion { None, Plant, Hang, Flag }

    /// <summary>
    /// 地图上单独一张图的物件：树、灌木、芦苇、房子、灯笼、旗子、篝火。和人物一起按脚底排前后，所以能绕到树后面。
    /// </summary>
    public class MapDecor
    {
        public string Sprite;            // Resources/Map 下的路径，例如 Art/life/tree_green_0
        public float X, Y;               // 脚底中点（格子坐标，y 向下）
        public float W, H;               // 画多大（格）；H = 0 按贴图比例
        public Motion Motion;
        public float Amp = 0.03f, Speed = 1.1f;   // 摆多大、多快
        public float Shadow;             // 脚下影子宽（占 W 的比例），0 = 不画
        public int Frames = 1;           // >1：横排序列帧循环（篝火）
        public float Fps = 10;
        public bool Ground;              // 贴着地面画（桥）：在所有人和物件下面，人能走在上面
        public float Lift;               // 画的位置往上抬多少格，排前后仍按 Y：挂在屋檐下的灯笼 Y 跟房子一样，抬上去
        // 运行时：草被人挤开的程度、树上下次还能惊起鸟的时刻
        public float Push, NextBirds;

        /// <summary>树、灌木、芦苇：根部钉住随风摆，脚下垫影子。</summary>
        public static MapDecor Plant(string sprite, float x, float y, float w, float amp = 0.03f)
        {
            return new MapDecor { Sprite = sprite, X = x, Y = y, W = w, Motion = Motion.Plant, Amp = amp, Shadow = 0.7f };
        }
    }

    /// <summary>这张地图上放几只什么小动物（种类见 GameRoot.Life）。</summary>
    public class CritterSpawn
    {
        public string Kind;
        public int Count;
        public CritterSpawn(string kind, int count) { Kind = kind; Count = count; }
    }

    /// <summary>冒烟的地方（炊烟、篝火），fire 还带一圈一闪一闪的火光。</summary>
    public class MapEmitter
    {
        public string Kind;   // smoke / fire
        public float X, Y;
        public MapEmitter(string kind, float x, float y) { Kind = kind; X = x; Y = y; }
    }

    /// <summary>占多格的整张贴图（房屋、营寨），画在地块之上。</summary>
    public class MapProp
    {
        public string Sprite;
        public int X, Y, W, H;   // 以格子为单位
        public MapProp(string sprite, int x, int y, int w, int h) { Sprite = sprite; X = x; Y = y; W = w; H = h; }
    }

    public class MapData
    {
        public string Id;
        public string Name;
        public string[] Rows;

        /// <summary>整张地图原画 Resources/Map/Art/{Art}(.png / _fg.png)；为空时按地块逐格画。</summary>
        public string Art;
        /// <summary>原画左右各比格子多出多少格：大地图是半格（边上是树），室内原画和格子正好对齐为 0。</summary>
        public float ArtMargin = 0.5f;
        public readonly List<Npc> Npcs = new List<Npc>();
        public readonly List<MapProp> Props = new List<MapProp>();
        public readonly List<MapDecor> Decor = new List<MapDecor>();
        public readonly List<CritterSpawn> Critters = new List<CritterSpawn>();
        public readonly List<MapEmitter> Emitters = new List<MapEmitter>();
        /// <summary>水面遮罩 Resources/Map/{WaterMask}：白 = 水，原画上这部分会起波纹、闪光。</summary>
        public string WaterMask;
        /// <summary>物件清单 Resources/Map/Decor/{DecorFile}.txt（一行一件，格式见 GameRoot.Life.LoadDecor）。</summary>
        public string DecorFile;

        /// <summary>整个画面乘的色调（后处理）：城里偏暖、野外偏冷、室内昏黄。</summary>
        public string Grade = "#ffffff";
        /// <summary>人物身上额外乘的环境光，让人物融进背景：室内压暗一点。</summary>
        public string Light = "#fff2de";
        /// <summary>飘落物：petal（桃花瓣）/ leaf（落叶）/ null（室内，没有云影）。</summary>
        public string Weather;

        /// <summary>人物画多大（1 = 约 1.1 格高）。野外地图景物比例大，人缩小一点。</summary>
        public float ActorScale = 1f;

        /// <summary>垫在透明贴图（树、山、房屋）下面的地面。</summary>
        public char Ground = '.';

        /// <summary>每走一步在遇敌地块上触发战斗的概率。</summary>
        public double EncounterRate;

        /// <summary>走到某格之后触发（返回 true 表示已处理，比如切换地图）。</summary>
        public Func<int, int, bool> OnStep;

        /// <summary>撞到/调查某格（含不可走的格子）时触发。</summary>
        public Func<int, int, bool> OnTouch;

        public int Width { get { return Rows[0].Length; } }
        public int Height { get { return Rows.Length; } }

        public char At(int x, int y)
        {
            if (x < 0 || y < 0 || y >= Height || x >= Width) return 'M';
            return Rows[y][x];
        }

        public Npc NpcAt(int x, int y)
        {
            foreach (var n in Npcs) if (n.X == x && n.Y == y && n.Visible()) return n;
            return null;
        }

        public bool IsWater(int x, int y) { char c = At(x, y); return c == '~' || c == 'w'; }

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || y < 0 || y >= Height || x >= Width) return false;
            if (NpcAt(x, y) != null) return false;
            return Tiles.Get(At(x, y)).Walkable;
        }
    }
}
