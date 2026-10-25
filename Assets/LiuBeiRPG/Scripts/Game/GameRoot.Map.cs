using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>大地图：移动、跟随队列、调查、随机遇敌、绘制。</summary>
    public partial class GameRoot
    {
        const float MoveInterval = 0.14f;

        MapData map;
        int px, py;
        int faceX, faceY = 1;
        readonly List<Vector2Int> trail = new List<Vector2Int>();   // 跟随武将的位置（吞食天地式一字长蛇）
        int stepsSinceBattle;
        float nextMoveTime;

        public void Warp(MapData target, int x, int y)
        {
            map = target;
            px = x; py = y;
            trail.Clear();
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
                nextMoveTime = Time.time + MoveInterval * 1.8f;   // 第一次按下后稍停顿再连续移动
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
            int nx = px + dx, ny = py + dy;

            if (!map.IsWalkable(nx, ny))
            {
                // 撞到营寨、城门之类的特殊地块
                if (map.NpcAt(nx, ny) == null && map.OnTouch != null) map.OnTouch(nx, ny);
                return;
            }

            trail.Insert(0, new Vector2Int(px, py));
            while (trail.Count > Mathf.Max(0, party.Members.Count - 1)) trail.RemoveAt(trail.Count - 1);
            px = nx; py = ny;
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
            var artRect = new Rect(origin.x - tile * 0.5f, origin.y, (map.Width + 1) * tile, map.Height * tile);
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
                if (tex != null) { Gfx.Image(TileRect(n.X, n.Y), tex); continue; }
                Gfx.Token(Inset(TileRect(n.X, n.Y), 3), n.Glyph, Gfx.Hex(n.Color));
            }

            // 队伍：后面的武将先画
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                int memberIndex = i + 1;
                if (memberIndex >= party.Members.Count) continue;
                var m = party.Members[memberIndex];
                Gfx.Token(Inset(TileRect(trail[i].x, trail[i].y), 5), m.Def.Glyph, Gfx.Hex(m.Def.Color), !m.Alive);
            }
            var leader = party.Members[0];
            var lr = Inset(TileRect(px, py), 2);
            Gfx.Token(lr, leader.Def.Glyph, Gfx.Hex(leader.Def.Color));
            // 朝向标记
            var cx = lr.center.x + faceX * (lr.width * 0.5f - 3);
            var cy = lr.center.y + faceY * (lr.height * 0.5f - 3);
            Gfx.Rect(new Rect(cx - 3, cy - 3, 6, 6), Color.white);

            // 前景遮挡层（树冠、屋檐）盖在人物上面
            var fg = art == null ? null : Gfx.Tex("Art/" + map.Art + "_fg");
            if (fg != null) Gfx.Image(artRect, fg);

            // 地名标签
            foreach (var lb in map.Labels)
            {
                var r = new Rect(origin.x + lb.X * tile - 60, origin.y + lb.Y * tile - 12, 120, 24);
                Gfx.Text(r, lb.Text, 16, Color.white, TextAnchor.MiddleCenter);
            }

            // 面前是 NPC 时提示
            var front = map.NpcAt(px + faceX, py + faceY);
            if (front != null && !UiBusy)
            {
                var fr = TileRect(px + faceX, py + faceY);
                Gfx.Text(new Rect(fr.x - 30, fr.y - 22, fr.width + 60, 20), "Z 对话", 14, Color.yellow, TextAnchor.MiddleCenter);
            }

            DrawHud();
        }

        float tile;       // 每格多大（虚拟像素）
        Vector2 origin;   // 格子 (0,0) 的左上角在屏幕上的位置

        /// <summary>地图铺满屏幕（竖屏格子更大），镜头跟着主角，到地图边缘停住。</summary>
        void LayoutMap()
        {
            float artW = map.Width + 1, artH = map.Height;
            tile = Mathf.Max(Gfx.VW / artW, Gfx.VH / artH);
            float ox = Gfx.VW * 0.5f - (px + 0.5f) * tile;
            float oy = Gfx.VH * 0.5f - (py + 0.5f) * tile;
            origin = new Vector2(
                Mathf.Clamp(ox, Gfx.VW - (map.Width + 0.5f) * tile, tile * 0.5f),
                Mathf.Clamp(oy, Gfx.VH - map.Height * tile, 0f));
        }

        Rect TileRect(int x, int y) { return new Rect(origin.x + x * tile, origin.y + y * tile, tile, tile); }
        static Rect Inset(Rect r, float d) { return new Rect(r.x + d, r.y + d, r.width - d * 2, r.height - d * 2); }

        /// <summary>左上角的小信息栏，横竖屏通用。</summary>
        void DrawHud()
        {
            var r = new Rect(8, 8, 240, 68 + party.Members.Count * 30);
            Gfx.Window(r, 0.8f);
            float x = r.x + 12, y = r.y + 10;
            Gfx.Text(new Rect(x, y, 140, 24), map.Name, 18, new Color(1f, 0.85f, 0.35f));
            Gfx.Text(new Rect(x, y + 2, r.width - 24, 22), "等级 " + party.Level, 15, Color.white, TextAnchor.UpperRight);
            y += 26;
            Gfx.Text(new Rect(x, y, r.width - 24, 20),
                "经验 " + party.Exp + "/" + party.ExpToNext + "　金 " + party.Gold + "　策 " + party.TP + "/" + party.MaxTP,
                13, new Color(1, 1, 1, 0.85f));
            y += 24;

            foreach (var m in party.Members)
            {
                var c = Gfx.Hex(m.Def.Color);
                Gfx.Token(new Rect(x, y, 24, 24), m.Def.Glyph, c, !m.Alive);
                Gfx.Text(new Rect(x + 32, y + 3, 86, 20), m.Name + (m.WeaponBonus > 0 ? " 武" + m.Wu : ""), 14, Color.white);
                Gfx.Bar(new Rect(x + 120, y + 9, 50, 8), (float)m.Soldiers / m.MaxSoldiers, c);
                Gfx.Text(new Rect(x + 176, y + 5, 40, 18), m.Soldiers.ToString(), 13, new Color(1, 1, 1, 0.8f));
                y += 30;
            }
        }
    }
}
