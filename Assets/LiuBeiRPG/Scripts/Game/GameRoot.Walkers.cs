using UnityEngine;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 路人：带 Route 的 NPC 沿着路线一格一格地走，到了路点歇一会儿再走下一段。
    /// 前面有人挡着就等；等太久（被主角堵住）就跳到下一个路点绕开。对话、菜单打开时原地不动。
    /// 天黑（BedTime）有家的路人沿最短路一路不停地走回家门口进屋，天亮从家门口出来接着逛；
    /// 刚进地图时直接按钟点摆好（夜里进城看不到人往家赶，都已经在屋里了）。
    /// </summary>
    public partial class GameRoot
    {
        /// <summary>该回家的钟点：黄昏天暗下来（18.5 时）到天亮（6 时）。</summary>
        bool BedTime { get { return hour >= 18.5f || hour < 6f; } }

        bool walkersPlaced;   // 进地图后按钟点摆过路人没有（OnEnterMap 清掉）

        void UpdateWalkers()
        {
            bool bed = BedTime;
            if (!walkersPlaced)
            {
                walkersPlaced = true;
                foreach (var n in map.Npcs) if (n.Home != null) n.Indoors = bed;
            }
            foreach (var n in map.Npcs)
            {
                if (n.Route == null || n.Route.Length < 2) continue;
                if (n.Indoors)
                {
                    // 天亮出门：家门口没人站着就出来
                    if (bed || (n.Home[0] == px && n.Home[1] == py) || map.NpcAt(n.Home[0], n.Home[1]) != null) continue;
                    n.Indoors = false;
                    n.X = n.PrevX = n.Home[0]; n.Y = n.PrevY = n.Home[1];
                    n.StepTime = Time.time;
                    n.WaitUntil = Time.time + Random.Range(0.5f, 3f);
                    continue;
                }
                if (Time.time < n.StepTime + n.StepSeconds || (Time.time < n.WaitUntil && !(bed && n.Home != null))) continue;
                if (Mathf.Abs(n.X - px) + Mathf.Abs(n.Y - py) <= 1) continue;   // 主角走到跟前就站住，好搭话
                if (bed && n.Home != null) { WalkHome(n); continue; }
                int count = n.Route.Length / 2;
                int tx = n.Route[n.RouteIndex * 2], ty = n.Route[n.RouteIndex * 2 + 1];
                if (n.X == tx && n.Y == ty)
                {
                    n.RouteIndex = (n.RouteIndex + 1) % count;
                    n.WaitUntil = Time.time + Random.Range(5f, 12f);   // 到了路点站一会儿，偶尔才走一段
                    continue;
                }
                int dx = System.Math.Sign(tx - n.X), dy = System.Math.Sign(ty - n.Y);
                // 先走横向，横向走不通再走纵向
                if (!TryWalk(n, dx, 0) && !TryWalk(n, 0, dy))
                {
                    if (n.BlockedSince < 0) n.BlockedSince = Time.time;
                    else if (Time.time - n.BlockedSince > 3f) { n.RouteIndex = (n.RouteIndex + 1) % count; n.BlockedSince = -1; }
                    n.WaitUntil = Time.time + 0.4f;
                }
            }
        }

        /// <summary>天黑往家走：沿最短路走一格，到门口就进屋；被堵住就原地等。</summary>
        void WalkHome(Npc n)
        {
            int hx = n.Home[0], hy = n.Home[1];
            if (n.X == hx && n.Y == hy) { n.Indoors = true; n.RouteIndex = 0; return; }
            int dx, dy;
            if (!StepToward(n.X, n.Y, hx, hy, out dx, out dy) || !TryWalk(n, dx, dy)) n.WaitUntil = Time.time + 0.4f;
            n.WaitUntil = Mathf.Min(n.WaitUntil, Time.time + 0.4f);   // 赶着回家，不停下来张望
        }

        /// <summary>从 (x,y) 往 (tx,ty) 的最短路上的下一步（只看地块能不能走，不管路上站着的人）。走不到返回 false。</summary>
        bool StepToward(int x, int y, int tx, int ty, out int dx, out int dy)
        {
            dx = dy = 0;
            int w = map.Width, h = map.Height;
            var dist = new int[w * h];
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            var q = new System.Collections.Generic.Queue<int>();
            dist[ty * w + tx] = 0;
            q.Enqueue(ty * w + tx);
            int[] ox = { 1, -1, 0, 0 }, oy = { 0, 0, 1, -1 };
            while (q.Count > 0)
            {
                int c = q.Dequeue(), cx = c % w, cy = c / w;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + ox[k], ny = cy + oy[k];
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h || dist[ny * w + nx] >= 0) continue;
                    if (!Tiles.Get(map.At(nx, ny)).Walkable) continue;
                    dist[ny * w + nx] = dist[c] + 1;
                    q.Enqueue(ny * w + nx);
                }
            }
            int best = int.MaxValue;
            for (int k = 0; k < 4; k++)
            {
                int nx = x + ox[k], ny = y + oy[k];
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int d = dist[ny * w + nx];
                if (d >= 0 && d < best) { best = d; dx = ox[k]; dy = oy[k]; }
            }
            return best != int.MaxValue;
        }

        bool TryWalk(Npc n, int dx, int dy)
        {
            if (dx == 0 && dy == 0) return false;
            int x = n.X + dx, y = n.Y + dy;
            if (!map.IsWalkable(x, y) || (x == px && y == py)) return false;
            for (int i = 0; i < trail.Count && i + 1 < party.Members.Count; i++) if (trail[i].x == x && trail[i].y == y) return false;
            n.PrevX = n.X; n.PrevY = n.Y;
            n.X = x; n.Y = y;
            n.StepTime = Time.time;
            n.BlockedSince = -1;
            n.Facing = FacingOf(dx, dy);
            if (Random.value < 0.3f) n.WaitUntil = Time.time + n.StepSeconds + Random.Range(1.5f, 4f);   // 走着走着停下来张望
            return true;
        }

        /// <summary>画在哪：路人走一格的时间里从上一格滑过来，其余 NPC 就在自己格子上。</summary>
        Vector2 WalkerCell(Npc n)
        {
            if (n.Route == null) return new Vector2(n.X, n.Y);
            float t = Mathf.Clamp01((Time.time - n.StepTime) / n.StepSeconds);
            return Vector2.Lerp(new Vector2(n.PrevX, n.PrevY), new Vector2(n.X, n.Y), t);
        }
    }
}
