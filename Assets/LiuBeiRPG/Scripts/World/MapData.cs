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
            { 'H', new TileInfo(false, "#8a5a3c") },                    // 房屋（整栋贴图见 MapData.Props）
            { 'M', new TileInfo(false, "#6e5a44", "山") { Sprite = "mountain" } },  // 山
            { '~', new TileInfo(false, "#3a6fb0") { Sprite = "water", Frames = 3 } },  // 水
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
        public string Weapon;     // 骨骼角色右手拿的武器（qiang / dao / jian），null = 空手
        public string Color = "#ffffff";
        public int X, Y;
        public Func<bool> Visible = () => true;
        public Action Talk;
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

        public bool IsWalkable(int x, int y)
        {
            if (x < 0 || y < 0 || y >= Height || x >= Width) return false;
            if (NpcAt(x, y) != null) return false;
            return Tiles.Get(At(x, y)).Walkable;
        }
    }
}
