using System.Collections.Generic;
using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 绘图小工具：界面（对话、菜单、战斗面板）用 IMGUI 画贴图、色块和文字。
    /// 大地图本身由相机画（见 World），这里只管叠在上面的界面。
    /// </summary>
    public static class Gfx
    {
        // 虚拟分辨率：横屏 960×540，竖屏 540×960，多出来的一边随屏幕比例伸长。
        // 手机端允许系统自动旋转，所有 UI 坐标都基于当前方向重新计算。
        public static bool Portrait { get { return Screen.height > Screen.width; } }
        static float Scale
        {
            get
            {
                return Portrait ? Mathf.Min(Screen.width / 540f, Screen.height / 960f)
                                : Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            }
        }
        public static float VW { get { return Screen.width / Scale; } }
        public static float VH { get { return Screen.height / Scale; } }

        public static Rect SafeArea
        {
            get
            {
                Rect s = Screen.safeArea;
                return new Rect(s.x / Scale, (Screen.height - s.yMax) / Scale,
                    s.width / Scale, s.height / Scale);
            }
        }

        static readonly Dictionary<string, Color> colorCache = new Dictionary<string, Color>();
        static readonly Dictionary<int, GUIStyle> styleCache = new Dictionary<int, GUIStyle>();
        public static Font CustomFont;

        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            Color c;
            if (colorCache.TryGetValue(hex, out c)) return c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            colorCache[hex] = c;
            return c;
        }

        /// <summary>把虚拟分辨率 VW×VH 等比缩放铺满窗口。</summary>
        public static void BeginScaled()
        {
            float s = Scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        }

        public static void Rect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        /// <summary>读取 Resources/Map 下的贴图；不存在返回 null（调用方退回色块）。</summary>
        public static Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Texture2D t;
            if (texCache.TryGetValue(name, out t)) return t;
            t = Resources.Load<Texture2D>("Map/" + name);   // 采样方式由 PixelArtImport 决定
            texCache[name] = t;
            return t;
        }

        public static void Image(Rect r, Texture2D tex)
        {
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
        }

        public static void Frame(Rect r, Color c, float t = 2f)
        {
            Rect(new Rect(r.x, r.y, r.width, t), c);
            Rect(new Rect(r.x, r.yMax - t, r.width, t), c);
            Rect(new Rect(r.x, r.y, t, r.height), c);
            Rect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        /// <summary>面板：木框羊皮纸（Art/ui_panel 九宫格）。之后画的字自动换墨色，画完记得 Paper = false。
        /// 贴图缺失时退回半透明深色圆角底。</summary>
        public static void Window(Rect r, float alpha = 0.92f)
        {
            if (NineSlice(r, ref panelStyle, "Art/ui_panel", 20)) { Paper = true; return; }
            Modern(r, alpha);
        }

        /// <summary>现代风格面板：半透明深色圆角底 + 极淡的亮边（对话框、选项框用）。</summary>
        public static void Modern(Rect r, float alpha = 0.92f)
        {
            RoundRect(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), new Color(1, 1, 1, 0.12f));
            RoundRect(r, new Color(0.07f, 0.08f, 0.1f, alpha));
        }

        /// <summary>方形卡片（战斗里的敌将）：Art/ui_card 九宫格，同样换墨色字。</summary>
        public static void Card(Rect r)
        {
            if (NineSlice(r, ref cardStyle, "Art/ui_card", 26)) Paper = true;
            else Rect(r, new Color(0, 0, 0, 0.45f));
        }

        static GUIStyle panelStyle, cardStyle;

        static bool NineSlice(Rect r, ref GUIStyle style, string tex, int border)
        {
            if (style == null)
            {
                var t = Tex(tex);
                if (t == null) return false;
                style = new GUIStyle { border = new RectOffset(border, border, border, border) };
                style.normal.background = t;
            }
            if (Event.current.type == EventType.Repaint) style.Draw(r, false, false, false, false);
            return true;
        }

        /// <summary>正在羊皮纸上写字：浅色字换成墨色，金/黄换朱红，其他颜色压深；不画投影。</summary>
        public static bool Paper;

        static Color Ink(Color c)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            Color o;
            if (s < 0.3f) { o = new Color(0.24f, 0.15f, 0.09f); if (v < 0.7f) c.a *= 0.45f; }   // 白/灰 → 墨（灰的淡一些）
            else if (h > 0.07f && h < 0.2f) o = new Color(0.66f, 0.18f, 0.1f);                // 金/黄 → 朱红
            else o = Color.HSVToRGB(h, Mathf.Max(s, 0.6f), 0.5f);
            o.a = c.a;
            return o;
        }

        /// <summary>菜单的一行：选中时底色呼吸、左边一道朱红竖条、字往右让一点。</summary>
        public static void MenuRow(Rect r, string text, int size, bool sel, Color c)
        {
            if (sel)
            {
                float pulse = 0.12f + 0.06f * Mathf.Sin(Time.time * 5f);
                Rect(r, Paper ? new Color(0.6f, 0.3f, 0.1f, pulse) : new Color(1, 1, 1, pulse));
                Rect(new Rect(r.x, r.y + 3, 3, r.height - 6), Paper ? new Color(0.66f, 0.18f, 0.1f) : new Color(0.96f, 0.76f, 0.32f));
            }
            Text(new Rect(r.x + (sel ? 14 : 8), r.y + 4, r.width - 14, r.height), text, size, c);
        }

        static GUIStyle roundStyle;

        /// <summary>圆角矩形（半径 12），九宫格拉伸，任意大小都不变形。</summary>
        public static void RoundRect(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (roundStyle == null)
            {
                const int n = 48, rad = 12;
                var tex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // 到最近圆角圆心的距离，边缘 1 像素做抗锯齿
                        float dx = Mathf.Max(0, Mathf.Max(rad - x - 0.5f, x + 0.5f - (n - rad)));
                        float dy = Mathf.Max(0, Mathf.Max(rad - y - 0.5f, y + 0.5f - (n - rad)));
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f)));
                    }
                tex.Apply();
                roundStyle = new GUIStyle { border = new RectOffset(rad, rad, rad, rad) };
                roundStyle.normal.background = tex;
            }
            var old = GUI.color;
            GUI.color = c;
            roundStyle.Draw(r, false, false, false, false);
            GUI.color = old;
        }

        public static GUIStyle Style(int size, TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = false)
        {
            int key = size * 100 + (int)anchor * 2 + (wrap ? 1 : 0);
            GUIStyle st;
            if (styleCache.TryGetValue(key, out st)) return st;
            st = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                wordWrap = wrap,
                richText = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            if (CustomFont != null) st.font = CustomFont;
            styleCache[key] = st;
            return st;
        }

        public static void ClearStyleCache() { styleCache.Clear(); }

        public static void Text(Rect r, string text, int size, Color c, TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = false, bool shadow = true)
        {
            var st = Style(size, anchor, wrap);
            if (Paper) { c = Ink(c); shadow = false; }
            if (shadow)
            {
                st.normal.textColor = new Color(0, 0, 0, 0.8f * c.a);
                GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, st);
            }
            st.normal.textColor = c;
            GUI.Label(r, text, st);
        }

        /// <summary>兵力条。</summary>
        public static void Bar(Rect r, float ratio, Color fill)
        {
            ratio = Mathf.Clamp01(ratio);
            Rect(r, new Color(0, 0, 0, 0.6f));
            if (ratio < 0.25f) fill = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), fill, ratio * 4f);
            Rect(new Rect(r.x + 1, r.y + 1, (r.width - 2) * ratio, r.height - 2), fill);
        }

        /// <summary>画一个“人物格子”：底色 + 边框 + 单字。</summary>
        public static void Token(Rect r, string glyph, Color c, bool dim = false)
        {
            bool paper = Paper;
            Paper = false;   // 字块自带深色底，字保持原色
            Rect(r, dim ? new Color(0.15f, 0.15f, 0.15f, 0.9f) : new Color(0.08f, 0.08f, 0.1f, 0.95f));
            Frame(r, dim ? Color.gray : c, 2);
            Text(r, glyph, Mathf.RoundToInt(r.height * 0.62f), dim ? Color.gray : c, TextAnchor.MiddleCenter);
            Paper = paper;
        }

        /// <summary>检测在某个区域内的鼠标点击（已考虑缩放矩阵）。</summary>
        public static bool Clicked(Rect r)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }
            if (e.type == EventType.TouchDown && r.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }
            return false;
        }
    }
}
