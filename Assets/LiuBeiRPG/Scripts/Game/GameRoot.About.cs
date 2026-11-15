using System;
using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 首页「关于我们」：版式、底图（Art/about_bg，720×1280）和文案照登天闯关（woyaoshangtian 的 Story.ts ABOUT_PAGE）。
    /// 底图上的位置是量图量的：紫标题条中心 y 108、浅蓝面板 y 231~1039、黄底牌中心 y 1179。底牌点一下复制邮箱。
    /// </summary>
    public partial class GameRoot
    {
        const string AboutEmail = "quandoufa@quandoufa.cn";
        static readonly string AboutText = string.Join("\n", new[]
        {
            "济南泉都发科技有限公司",
            "",
            "我们是来自泉城济南的一支小团队，",
            "专注于制作好玩、耐玩的原创小游戏。",
            "",
            "《刘备英雄传》由我们独立设计与开发，",
            "从涿县结义到三顾茅庐，",
            "每一段旅程都用心打磨。",
            "",
            "游戏中遇到问题，或有任何意见和建议，",
            "欢迎来信：",
            "",
            AboutEmail,
            "",
            "感谢你的游玩！",
        });

        /// <summary>复制到剪贴板；微信包里由 WxGlue 换成 wx.setClipboardData（它自己会弹「内容已复制」）。</summary>
        public static Action<string> CopyText = s => GUIUtility.systemCopyBuffer = s;

        bool aboutOpen;
        float aboutCopiedAt = -10;

        void DrawAbout()
        {
            // 底图露不到的上下两边，补上底图边缘的颜色
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH * 0.5f), new Color32(14, 147, 254, 255));
            Gfx.Rect(new Rect(0, Gfx.VH * 0.5f, Gfx.VW, Gfx.VH * 0.5f), new Color32(157, 102, 252, 255));
            // 等比放进屏幕，顶上让出刘海和微信胶囊
            float top = Mathf.Max(Gfx.SafeArea.y, Gfx.VH * 0.06f);
            float s = Mathf.Min(Gfx.VW / 720f, (Gfx.VH - top) / 1280f);
            float ox = (Gfx.VW - 720 * s) * 0.5f, oy = top + (Gfx.VH - top - 1280 * s) * 0.5f;
            Func<float, float, float, float, Rect> R = (x, y, w, h) => new Rect(ox + x * s, oy + y * s, w * s, h * s);   // 底图像素 → 屏幕
            Gfx.Image(R(0, 0, 720, 1280), Gfx.Tex("Art/about_bg"));

            // 标题：奶油色字 + 紫描边
            var tr = R(150, 68, 420, 80);
            int ts = Mathf.RoundToInt(40 * s);
            var outline = new Color32(60, 24, 110, 220);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4, d = 2.5f * s;
                Gfx.Text(new Rect(tr.x + Mathf.Cos(a) * d, tr.y + Mathf.Sin(a) * d, tr.width, tr.height), "关于我们", ts, outline, TextAnchor.MiddleCenter, false, false);
            }
            Gfx.Text(tr, "关于我们", ts, new Color32(255, 248, 230, 255), TextAnchor.MiddleCenter, false, false);

            Gfx.Text(R(100, 278, 520, 740), AboutText, Mathf.RoundToInt(25 * s), new Color32(38, 42, 92, 255), TextAnchor.MiddleCenter, true, false);

            var foot = R(140, 1124, 440, 110);
            bool copied = Time.time - aboutCopiedAt < 1.5f;
            Gfx.Text(foot, copied ? "已复制" : "点此复制邮箱", Mathf.RoundToInt(24 * s), new Color32(110, 52, 0, 255), TextAnchor.MiddleCenter, false, false);
            if (Gfx.Clicked(foot)) { CopyText(AboutEmail); aboutCopiedAt = Time.time; }

            if (BackButton()) aboutOpen = false;
        }

        /// <summary>左上角后退钮（关于我们、读档页共用），和胶囊同一行；点到返回 true。</summary>
        bool BackButton()
        {
            // 左上角后退钮，和胶囊同一行；画风照底图的黄底牌：深紫粗描边 + 黄面 + 下沿橙色唇边 + 高光，箭头用两段圆头粗线画
            var back = new Rect(Gfx.SafeArea.x + 16, Mathf.Max(Gfx.SafeArea.y, 8) + 4, 44, 44);
            var purple = new Color32(60, 24, 110, 255);
            Gfx.Pill(new Rect(back.x, back.y + 3, back.width, back.height), new Color(0.15f, 0.05f, 0.35f, 0.35f));   // 投影
            Gfx.Pill(back, purple);
            Gfx.Pill(new Rect(back.x + 3, back.y + 3, back.width - 6, back.height - 6), new Color32(255, 150, 20, 255));
            Gfx.Pill(new Rect(back.x + 3, back.y + 3, back.width - 6, back.height - 10), new Color32(255, 210, 60, 255));
            Gfx.Pill(new Rect(back.x + 11, back.y + 8, 9, 5), new Color(1, 1, 1, 0.8f));
            var c = back.center + new Vector2(-2, -1);
            ChevronStroke(c + new Vector2(0, -6), -45, purple);   // GUI 的 y 朝下：-45° 是往右上，上半笔
            ChevronStroke(c + new Vector2(0, 6), 45, purple);     // 下半笔往右下，两笔左端在 c 左边 6 处相接
            return Gfx.Clicked(new Rect(back.x - 10, back.y - 10, back.width + 20, back.height + 20));
        }

        /// <summary>箭头的一笔：以 at 为中心、旋转 deg 度的圆头粗线（不用 RotateAroundPivot，理由见 WeaponIcon）。</summary>
        static void ChevronStroke(Vector2 at, float deg, Color c)
        {
            var old = GUI.matrix;
            var p = (Vector3)at;
            GUI.matrix = old * Matrix4x4.TRS(p, Quaternion.Euler(0, 0, deg), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
            Gfx.Pill(new Rect(at.x - 9, at.y - 3, 18, 6), c);
            GUI.matrix = old;
        }
    }
}
