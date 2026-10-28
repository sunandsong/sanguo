using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 胜利结算面板：大字“胜利”砸下来，经验 / 金钱数字往上滚，经验条涨；升级了再弹金色“升级”横幅，
    /// 下面列兵力上限、新学会的法术（带图标）。点一下（或确认）才出场回地图。
    /// </summary>
    public partial class GameRoot
    {
        float resultAt;
        int resultFromLevel;
        readonly List<string> resultLines = new List<string>();

        void ShowResult()
        {
            resultFromLevel = party.Level;
            resultLines.Clear();
            foreach (var e in battle.GrantRewards())
                if (!e.Text.StartsWith("胜利") && !e.Text.StartsWith("全军升至")) resultLines.Add(e.Text);   // 升到几级在横幅里写
            resultAt = Time.time;
            bstate = BState.Result;
        }

        void CloseResult()
        {
            if (Time.time - resultAt < 0.6f) return;   // 刚弹出来的一下不吃，免得连点直接跳过
            EndBattle();
        }

        void DrawResult()
        {
            float t = Time.time - resultAt;
            bool up = party.Level > resultFromLevel;
            float w = Mathf.Min(bw - 40, 560), h = up ? 330 + resultLines.Count * 40 : 250;   // 底下留 40 给“点击继续”
            var r = new Rect(bx + (bw - w) * 0.5f, by + (bh - h) * 0.5f, w, h);
            float pop = EaseOut(resultAt, 0.25f);
            r.y += (1f - pop) * 30f;

            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, 0.45f * pop));
            Gfx.RoundRect(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color(Accent.r, Accent.g, Accent.b, 0.35f));
            Gfx.Modern(r, 0.96f);

            // 大字“胜 利”：先放大再落定
            float s = 1f + Mathf.Max(0f, 1f - t / 0.2f) * 0.6f;
            int size = Mathf.RoundToInt(52 * s);
            var title = new Rect(r.x, r.y + 14, r.width, 70);
            var dark = new Color(0.25f, 0.12f, 0.02f);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                var o = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 3f;
                Gfx.Text(new Rect(title.x + o.x, title.y + o.y, title.width, title.height), "胜　利", size, dark, TextAnchor.MiddleCenter, false, false);
            }
            Gfx.Text(title, "胜　利", size, Accent, TextAnchor.MiddleCenter);

            // 经验、金钱：数字从 0 滚上去
            float roll = Mathf.Clamp01((t - 0.25f) / 0.6f);
            float y = r.y + 98;
            RewardRow(new Rect(r.x + 40, y, w - 80, 40), "经验", "+" + Mathf.RoundToInt(battle.RewardExp * roll));
            RewardRow(new Rect(r.x + 40, y + 44, w - 80, 40), "金钱", "+" + Mathf.RoundToInt(battle.RewardGold * roll));
            // 经验条
            var bar = new Rect(r.x + 40, y + 96, w - 80, 12);
            Gfx.Bar(bar, (float)party.Exp / party.ExpToNext * roll, new Color(0.45f, 0.75f, 1f));
            Gfx.Text(new Rect(bar.x, bar.y - 2, bar.width, 16), "Lv " + party.Level + "　" + party.Exp + " / " + party.ExpToNext, 13, new Color(1, 1, 1, 0.75f), TextAnchor.MiddleRight);

            if (up && t > 0.9f)
            {
                // 金色“升级”横幅，呼吸发光
                float a = Mathf.Clamp01((t - 0.9f) / 0.2f);
                var banner = new Rect(r.x + 20, y + 124, w - 40, 54);
                Gfx.RoundRect(banner, new Color(Accent.r, Accent.g, Accent.b, (0.75f + 0.15f * Mathf.Sin(Time.time * 5f)) * a));
                Gfx.Text(banner, "升级！　Lv " + resultFromLevel + " → Lv " + party.Level, 28, new Color(0.15f, 0.08f, 0.02f, a), TextAnchor.MiddleCenter, false, false);
                float ly = banner.yMax + 10;
                foreach (var line in resultLines)
                {
                    var lr = new Rect(r.x + 40, ly, w - 80, 36);
                    TacticDef learned = null;
                    foreach (var tc in TacticDb.All) if (line.Contains("「" + tc.Name + "」")) learned = tc;
                    var icon = learned != null ? Gfx.Tex("Art/tactic_" + learned.Id) : null;
                    if (icon != null) { Gfx.Image(new Rect(lr.x, lr.y, 36, 36), icon); lr.x += 44; lr.width -= 44; }
                    var old = GUI.color;
                    GUI.color = new Color(1, 1, 1, a);
                    Gfx.Text(lr, line, 22, learned != null ? Accent : Color.white, TextAnchor.MiddleLeft);
                    GUI.color = old;
                    ly += 40;
                }
            }

            if (t > 0.9f && Mathf.Repeat(Time.time, 1.2f) < 0.85f)
                Gfx.Text(new Rect(r.x, r.yMax - 32, r.width, 24), "点击继续", 16, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter);
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) CloseResult();
        }

        static void RewardRow(Rect r, string label, string value)
        {
            Gfx.RoundRect(r, new Color(1, 1, 1, 0.06f));
            Gfx.Text(new Rect(r.x + 16, r.y, 100, r.height), label, 22, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleLeft);
            Gfx.Text(new Rect(r.x, r.y, r.width - 16, r.height), value, 30, Color.white, TextAnchor.MiddleRight);
        }
    }
}
