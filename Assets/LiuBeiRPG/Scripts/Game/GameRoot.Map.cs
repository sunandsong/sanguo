using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>大地图：移动、跟随队列、调查、随机遇敌、绘制。</summary>
    public partial class GameRoot
    {
        const float MoveInterval = 0.44f;

        MapData map;
        int px, py;
        int faceX, faceY = 1;
        readonly List<Vector2Int> trail = new List<Vector2Int>();   // 跟随武将的位置（吞食天地式一字长蛇）
        int stepsSinceBattle;
        bool showGrid = true;   // ponytail: 调试用，对齐完可改回 false
        float nextMoveTime;
        int prevX, prevY;              // 上一格，画的时候从这里滑到 px/py
        float lastStepTime = -10f;
        readonly List<Vector2Int> prevTrail = new List<Vector2Int>();   // 跟随武将上一步的位置，同样滑过来
        readonly Dictionary<string, PuppetActor> puppets = new Dictionary<string, PuppetActor>();

        /// <summary>按武将 Id 取骨骼动画角色；包里没有这个角色返回 null，照旧画字块。</summary>
        PuppetActor Puppet(string id)
        {
            PuppetActor a;
            if (puppets.TryGetValue(id, out a)) return a;
            try
            {
                a = PuppetActor.Load(id);
                a.Speed = 0.8f;   // walk 一循环两步 0.7 秒，一步 0.35 秒 ÷ 每格 0.44 秒
            }
            catch (System.Exception) { a = null; }
            return puppets[id] = a;
        }

        // 张世平赠铁打造的兵器：刘备双股剑、关羽青龙偃月刀、张飞丈八蛇矛
        static readonly Dictionary<string, string> Weapons = new Dictionary<string, string>
        {
            { "liubei", "jian" }, { "guanyu", "dao" }, { "zhangfei", "qiang" },
        };

        static string FacingOf(int dx, int dy) { return dx > 0 ? "side" : dx < 0 ? "left" : dy < 0 ? "back" : "front"; }

        public void Warp(MapData target, int x, int y)
        {
            map = target;
            px = x; py = y;
            prevX = x; prevY = y;
            trail.Clear();
            prevTrail.Clear();
            stepsSinceBattle = 0;
            mode = Mode.Map;
        }

        // ---------------- 输入 ----------------

        void MapKeyDown(InputKey key)
        {
            int dx, dy;
            if (DirOf(key, out dx, out dy))
            {
                TryMove(dx, dy);
                nextMoveTime = Time.time + MoveInterval;   // 平滑移动：滑完这一格紧接着走下一格，不再停顿
                return;
            }
            if (key == InputKey.Confirm) Investigate();
        }

        void UpdateMap()
        {
            if (UiBusy || map == null || Time.time < nextMoveTime) return;
            // 按住方向键连续移动
            foreach (var k in new[] { InputKey.Up, InputKey.Down, InputKey.Left, InputKey.Right })
            {
                if (!held.Contains(k)) continue;
                int dx, dy;
                DirOf(k, out dx, out dy);
                TryMove(dx, dy);
                nextMoveTime = Time.time + MoveInterval;
                return;
            }
        }

        static bool DirOf(InputKey key, out int dx, out int dy)
        {
            dx = 0; dy = 0;
            switch (key)
            {
                case InputKey.Up: dy = -1; return true;
                case InputKey.Down: dy = 1; return true;
                case InputKey.Left: dx = -1; return true;
                case InputKey.Right: dx = 1; return true;
            }
            return false;
        }

        void TryMove(int dx, int dy)
        {
            if (mode != Mode.Map || UiBusy) return;
            faceX = dx; faceY = dy;
            var leaderPuppet = Puppet(party.Members[0].Def.Id);
            if (leaderPuppet != null) leaderPuppet.Facing = FacingOf(dx, dy);
            int nx = px + dx, ny = py + dy;

            if (!map.IsWalkable(nx, ny))
            {
                // 撞到营寨、城门之类的特殊地块
                if (map.NpcAt(nx, ny) == null && map.OnTouch != null) map.OnTouch(nx, ny);
                return;
            }

            var oldTrail = new List<Vector2Int>(trail);
            trail.Insert(0, new Vector2Int(px, py));
            while (trail.Count > Mathf.Max(0, party.Members.Count - 1)) trail.RemoveAt(trail.Count - 1);
            prevTrail.Clear();
            for (int i = 0; i < trail.Count; i++)
            {
                var from = i < oldTrail.Count ? oldTrail[i] : trail[i];   // 刚入队的就地出现
                prevTrail.Add(from);
                var fp = i + 1 < party.Members.Count ? Puppet(party.Members[i + 1].Def.Id) : null;
                if (fp != null && from != trail[i]) fp.Facing = FacingOf(trail[i].x - from.x, trail[i].y - from.y);
            }
            prevX = px; prevY = py;
            px = nx; py = ny;
            lastStepTime = Time.time;
            stepsSinceBattle++;

            var currentMap = map;
            if (map.OnStep != null && map.OnStep(px, py)) return;
            if (map != currentMap || mode != Mode.Map || UiBusy) return;

            // 随机遇敌
            var tile = Tiles.Get(map.At(px, py));
            if (tile.Encounter && stepsSinceBattle >= 4 && rng.NextDouble() < map.EncounterRate)
            {
                stepsSinceBattle = 0;
                StartBattle(GameDatabase.RandomEncounter(rng, party.Level), false, null);
            }
        }

        void Investigate()
        {
            int tx = px + faceX, ty = py + faceY;
            var npc = map.NpcAt(tx, ty);
            if (npc != null)
            {
                var np = npc.Id == null ? null : Puppet(npc.Id);
                if (np != null) np.Facing = FacingOf(-faceX, -faceY);   // 转过来面朝刘备
                if (npc.Talk != null) npc.Talk();
                return;
            }
            if (map.OnTouch != null) map.OnTouch(tx, ty);
        }

        // ---------------- 绘制 ----------------

        void DrawMap()
        {
            if (map == null) return;
            LayoutMap();

            // 整张原画：宽 = 格子宽 + 1 格（布局图左右各留半格边）
            var art = Gfx.Tex(map.Art == null ? null : "Art/" + map.Art);
            var artRect = new Rect(origin.x - tile * map.ArtMargin, origin.y, (map.Width + map.ArtMargin * 2) * tile, map.Height * tile);
            if (art != null) Gfx.Image(artRect, art);

            // 地块：有贴图画贴图（透明的先垫地面），没有贴图退回色块
            var groundTex = Gfx.Tex(Tiles.Get(map.Ground).Sprite);
            int frame = (int)(Time.time * 3f);
            for (int y = 0; art == null && y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var info = Tiles.Get(map.At(x, y));
                    var r = TileRect(x, y);
                    if (groundTex != null)
                    {
                        Gfx.Image(r, groundTex);
                        var tex = Gfx.Tex(info.Frames > 1 ? info.Sprite + "_" + (frame % info.Frames) : info.Sprite);
                        if (tex != null) Gfx.Image(r, tex);
                        continue;
                    }
                    var c = Gfx.Hex(info.Color);
                    if (((x + y) & 1) == 1) c *= 0.95f;
                    c.a = 1f;
                    Gfx.Rect(r, c);
                    if (!info.Walkable)
                        Gfx.Rect(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), c * 0.88f + new Color(0, 0, 0, 1));
                    if (!string.IsNullOrEmpty(info.Glyph))
                        Gfx.Text(r, info.Glyph, 16, new Color(1, 1, 1, info.Walkable ? 0.75f : 0.35f), TextAnchor.MiddleCenter, false, false);
                }
            }

            // 房屋、营寨等整张贴图
            foreach (var p in map.Props)
            {
                var tex = art == null ? Gfx.Tex(p.Sprite) : null;
                if (tex != null) Gfx.Image(new Rect(origin.x + p.X * tile, origin.y + p.Y * tile, p.W * tile, p.H * tile), tex);
            }

            // NPC
            foreach (var n in map.Npcs)
            {
                if (!n.Visible()) continue;
                var tex = Gfx.Tex(n.Sprite);
                if (tex != null)
                {
                    // 手绘物件（告示牌）画 1.3 格大，底边踩在格子底边
                    const float s = 1.3f;
                    Gfx.Image(new Rect(origin.x + (n.X + 0.5f - s / 2) * tile, origin.y + (n.Y + 0.95f - s) * tile, s * tile, s * tile), tex);
                    continue;
                }
                var np = n.Id == null ? null : Puppet(n.Id);
                if (np != null)
                {
                    if (!UiBusy) np.Facing = "front";   // 对话时转向刘备，聊完转回正面
                    np.Equip(n.Weapon);
                    DrawPuppet(np, new Vector2(n.X, n.Y));
                    continue;
                }
                Gfx.Token(Inset(TileRect(n.X, n.Y), 3), n.Glyph, Gfx.Hex(n.Color));
            }

            // 队伍：后面的武将先画
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                int memberIndex = i + 1;
                if (memberIndex >= party.Members.Count) continue;
                var m = party.Members[memberIndex];
                var mp = Puppet(m.Def.Id);
                if (mp != null) { DrawPuppet(mp, Slide(prevTrail[i], trail[i]), !m.Alive); continue; }
                Gfx.Token(Inset(TileRect(trail[i].x, trail[i].y), 5), m.Def.Glyph, Gfx.Hex(m.Def.Color), !m.Alive);
            }
            var leader = party.Members[0];
            var lPuppet = Puppet(leader.Def.Id);
            if (lPuppet != null) DrawPuppet(lPuppet, LeaderPos());
            else Gfx.Token(Inset(TileRect(px, py), 2), leader.Def.Glyph, Gfx.Hex(leader.Def.Color));

            // 前景遮挡层（树冠、屋檐）盖在人物上面
            var fg = art == null ? null : Gfx.Tex("Art/" + map.Art + "_fg");
            if (fg != null) Gfx.Image(artRect, fg);

            // 格子线（G 键开关）：极淡的纯黑，只把底下压暗一点，草地上偏深绿、土路上偏深黄，自动随背景
            if (showGrid)
            {
                var line = new Color(0, 0, 0, 0.07f);
                for (int x = 0; x <= map.Width; x++) Gfx.Rect(new Rect(origin.x + x * tile, origin.y, 1, map.Height * tile), line);
                for (int y = 0; y <= map.Height; y++) Gfx.Rect(new Rect(origin.x, origin.y + y * tile, map.Width * tile, 1), line);
            }

            DrawHud();
        }

        float tile;       // 每格多大（虚拟像素）
        Vector2 origin;   // 格子 (0,0) 的左上角在屏幕上的位置

        /// <summary>地图铺满屏幕（竖屏格子更大），镜头跟着主角，到地图边缘停住。</summary>
        void LayoutMap()
        {
            // 格子大小统一按 20×14 的大地图铺满屏幕算，室内等小地图人物不会跟着放大
            // ponytail: 写死 21×14，以后有更大的地图也照样是这个格子大小
            tile = Mathf.Max(Gfx.VW / 21f, Gfx.VH / 14f);
            var lp = LeaderPos();
            float mg = map.ArtMargin, artW = (map.Width + mg * 2) * tile, artH = map.Height * tile;
            // 比屏幕大就跟着主角滚动（夹在边上），比屏幕小就居中，四周留黑
            float ox = artW <= Gfx.VW ? (Gfx.VW - artW) * 0.5f + tile * mg
                : Mathf.Clamp(Gfx.VW * 0.5f - (lp.x + 0.5f) * tile, Gfx.VW - (map.Width + mg) * tile, tile * mg);
            float oy = artH <= Gfx.VH ? (Gfx.VH - artH) * 0.5f
                : Mathf.Clamp(Gfx.VH * 0.5f - (lp.y + 0.5f) * tile, Gfx.VH - artH, 0f);
            origin = new Vector2(ox, oy);
        }

        /// <summary>主角画在哪（格子坐标，小数）：走一格的 MoveInterval 内从上一格滑过来。</summary>
        Vector2 LeaderPos() { return Slide(new Vector2Int(prevX, prevY), new Vector2Int(px, py)); }

        Vector2 Slide(Vector2Int from, Vector2Int to)
        {
            float t = Mathf.Clamp01((Time.time - lastStepTime) / MoveInterval);
            return Vector2.Lerp(from, to, t);
        }

        /// <summary>骨骼动画角色，脚底踩在格子底边（原图角色高约 950 像素，画出来约 1.1 格高）。</summary>
        void DrawPuppet(PuppetActor a, Vector2 cell, bool dead = false)
        {
            var foot = origin + new Vector2((cell.x + 0.5f) * tile, (cell.y + 1f - 0.08f) * tile);
            float scale = tile * 1.1f / 950f;
            Gfx.Image(new Rect(foot.x - tile * 0.28f, foot.y - tile * 0.07f, tile * 0.56f, tile * 0.14f), ShadowTex());
            // 深色描边：剪影朝 8 个方向各偏一点先画，贴近地图像素画的粗描边
            a.Tint = new Color(0.14f, 0.09f, 0.06f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                a.DrawGUI(foot + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.2f, scale);
            }
            a.Tint = dead ? new Color(0.5f, 0.5f, 0.5f) : new Color(1f, 0.95f, 0.87f);   // 略压暖，贴近地图的光照；阵亡压灰
            a.DrawGUI(foot, scale);
        }

        static Texture2D shadowTex;

        /// <summary>脚下的软阴影：中间深、边缘渐隐的椭圆（画的时候压扁）。</summary>
        static Texture2D ShadowTex()
        {
            if (shadowTex != null) return shadowTex;
            const int n = 32;
            shadowTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    shadowTex.SetPixel(x, y, new Color(0, 0, 0, 0.4f * Mathf.Clamp01(1 - d * d)));
                }
            shadowTex.Apply();
            return shadowTex;
        }

        void TickLeader()
        {
            if (map == null) return;
            bool walking = Time.time - lastStepTime < MoveInterval * 1.5f;
            for (int i = 0; i < party.Members.Count; i++)
            {
                var a = Puppet(party.Members[i].Def.Id);
                // 队伍不满时跟随者原地不动（trail 还没轮到他）就待机
                bool moving = walking && (i == 0 ? new Vector2Int(prevX, prevY) != new Vector2Int(px, py)
                                                 : i - 1 < trail.Count && prevTrail[i - 1] != trail[i - 1]);
                if (a == null) continue;
                a.Play(moving ? "walk" : "idle");
                string w;
                a.Equip(party.Members[i].WeaponBonus > 0 && Weapons.TryGetValue(party.Members[i].Def.Id, out w) ? w : null);   // 受赠之前空手
            }
            foreach (var a in puppets.Values) if (a != null) a.Tick(Time.deltaTime);   // 菜单、对话时也照样 Tick，待机呼吸不停
        }

        Rect TileRect(int x, int y) { return new Rect(origin.x + x * tile, origin.y + y * tile, tile, tile); }
        static Rect Inset(Rect r, float d) { return new Rect(r.x + d, r.y + d, r.width - d * 2, r.height - d * 2); }

        /// <summary>左上角只显示地名。</summary>
        void DrawHud()
        {
            var r = new Rect(8, 8, 120, 40);
            Gfx.Window(r, 0.8f);
            Gfx.Text(Inset(r, 8), map.Name, 18, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter);
        }
    }
}
