using System.Collections.Generic;
using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 绘图小工具：Demo 用 IMGUI 画贴图、色块和文字。
    /// 以后换成 Tilemap + Sprite 时，只需要替换 Game 目录下的表现层。
    /// </summary>
    public static class Gfx
    {
        // 虚拟分辨率：横屏至少 960×540，竖屏至少 540×960，多出来的一边随屏幕比例伸长（不留黑边）。
        // 横竖屏只看窗口形状；上线时横屏、竖屏各打一个包。
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

        /// <summary>复古风格对话框：深色底 + 双线白框。</summary>
        public static void Window(Rect r, float alpha = 0.92f)
        {
            Rect(r, new Color(0.05f, 0.06f, 0.12f, alpha));
            Frame(r, new Color(1, 1, 1, 0.9f), 2);
            Frame(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), new Color(1, 1, 1, 0.35f), 1);
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
            Rect(r, dim ? new Color(0.15f, 0.15f, 0.15f, 0.9f) : new Color(0.08f, 0.08f, 0.1f, 0.95f));
            Frame(r, dim ? Color.gray : c, 2);
            Text(r, glyph, Mathf.RoundToInt(r.height * 0.62f), dim ? Color.gray : c, TextAnchor.MiddleCenter);
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
            return false;
        }
    }
}
