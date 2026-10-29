using UnityEngine;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 路人：带 Route 的 NPC 沿着路线一格一格地走，到了路点歇一会儿再走下一段。
    /// 前面有人挡着就等；等太久（被主角堵住）就跳到下一个路点绕开。对话、菜单打开时原地不动。
    /// </summary>
    public partial class GameRoot
    {
        void UpdateWalkers()
        {
            foreach (var n in map.Npcs)
            {
                if (n.Route == null || n.Route.Length < 2 || !n.Visible()) continue;
                if (Time.time < n.StepTime + n.StepSeconds || Time.time < n.WaitUntil) continue;
                if (Mathf.Abs(n.X - px) + Mathf.Abs(n.Y - py) <= 1) continue;   // 主角走到跟前就站住，好搭话
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
