using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 镜头和过场：镜头带缓动地跟着主角；切地图时从主角处圆形展开（iris）；进新地图毛笔字写出地名。
    /// 地名字体：Resources/Fonts/BrushFont.ttf（马善政毛笔楷书，SIL OFL，见 BrushFont_OFL.txt）。
    /// </summary>
    public partial class GameRoot
    {
        Vector2 camFocus;          // 镜头对准的格子坐标（小数），追着主角
        bool camSnap = true;       // 下一帧直接对准（刚换地图）

        float irisStart = -10f;
        const float IrisTime = 0.6f;

        float titleStart = -10f;
        string titleText;
        static Font brushFont;

        /// <summary>每帧一次：镜头朝主角缓动，离得太远（瞬移）就直接跳过去。</summary>
        void UpdateCamera()
        {
            var lp = LeaderPos();
            if (camSnap || (lp - camFocus).sqrMagnitude > 9f) { camFocus = lp; camSnap = false; return; }
            camFocus = Vector2.Lerp(camFocus, lp, 1f - Mathf.Exp(-Time.deltaTime * 7f));
        }

        /// <summary>Warp 里调：圆形转场，换了地图就写地名。</summary>
        void OnEnterMap(bool newMap)
        {
            camSnap = true;
            walkersPlaced = false;   // 路人按钟点重新摆：夜里在屋里，白天在外面
            irisStart = irisBegan = Time.time;
            if (newMap) { titleText = map.Name; titleStart = Time.time + 0.25f; Sfx.Play("map_enter"); }
            WarmMap();
        }

        float irisBegan;   // 这次转场从什么时候开始等（底图没解码好时圆洞先不张开，最多等 3 秒）

        /// <summary>进地图：队伍每个人所有朝向的部件、这张图的物件 / 路人，提前排进解码（微信包里的图是下载后才解码的）。</summary>
        void WarmMap()
        {
            if (map.Art != null) Remote.Tex("Map/Art/" + map.Art);   // 底图排第一个解码，转场等的就是它
            if (party != null) foreach (var m in party.Members) Remote.Warm("Puppets/" + (m.Def.Skin ?? m.Def.Id));
            if (map.Art != null) Remote.Warm("Map/Art/" + map.Art + "_fg");
            LoadDecor(map);   // 物件清单可能还没读
            foreach (var d in map.Decor) Remote.Warm("Map/" + d.Sprite);
            foreach (var p in map.Props) Remote.Warm("Map/" + p.Sprite);
            foreach (var n in map.Npcs)
            {
                if (n.Sprite != null) Remote.Warm("Map/" + n.Sprite);
                if (n.Id != null) Remote.Warm("Puppets/" + (n.Skin ?? n.Id));
            }
        }

        /// <summary>圆形转场：黑幕中间一个从主角处张开的圆洞。</summary>
        void DrawIris()
        {
            // 底图还没解码好：圆洞先别张开（全黑），省得看到一张空地图；最多等 3 秒
            if (mode == Mode.Map && Time.time - irisBegan < 3f && map.Art != null && Remote.Pending("Map/Art/" + map.Art)) irisStart = Time.time;
            float t = (Time.time - irisStart) / IrisTime;
            if (t >= 1f || mode != Mode.Map) return;
            if (t < 0) t = 0;
            float e = t * t * (3 - 2 * t);
            var c = origin + (LeaderPos() + new Vector2(0.5f, 0.4f)) * tile;
            float far = new Vector2(Mathf.Max(c.x, Gfx.VW - c.x), Mathf.Max(c.y, Gfx.VH - c.y)).magnitude;
            float r = Mathf.Lerp(0, far * 1.1f, e);
            var hole = new Rect(c.x - r, c.y - r, r * 2, r * 2);
            var black = Color.black;
            Gfx.Rect(new Rect(0, 0, Gfx.VW, hole.y), black);
            Gfx.Rect(new Rect(0, hole.yMax, Gfx.VW, Gfx.VH - hole.yMax), black);
            Gfx.Rect(new Rect(0, hole.y, hole.x, hole.height), black);
            Gfx.Rect(new Rect(hole.xMax, hole.y, Gfx.VW - hole.xMax, hole.height), black);
            if (r > 0) Gfx.Image(hole, HoleTex());
        }

        static Texture2D holeTex;

        /// <summary>黑底中间一个软边透明圆。</summary>
        static Texture2D HoleTex()
        {
            if (holeTex != null) return holeTex;
            const int n = 128;
            holeTex = new Texture2D(n, n) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    holeTex.SetPixel(x, y, new Color(0, 0, 0, Mathf.Clamp01((d - 0.9f) * 10f)));
                }
            holeTex.Apply();
            return holeTex;
        }

        /// <summary>地名：毛笔字从左往右写出来，停一会儿淡出。下面垫一条淡淡的墨色横带。</summary>
        void DrawMapTitle()
        {
            if (titleText == null || mode != Mode.Map) return;
            float t = Time.time - titleStart;
            const float write = 0.9f, hold = 1.6f, fade = 0.8f;
            if (t < 0 || t > write + hold + fade) return;
            if (brushFont == null) brushFont = Resources.Load<Font>("Fonts/BrushFont");
            float a = t < write + hold ? 1f : 1f - (t - write - hold) / fade;
            float reveal = Mathf.Clamp01(t / write);

            float h = 96, y = Gfx.VH * 0.22f;
            var band = new Rect(0, y - 10, Gfx.VW, h + 20);
            // 墨色横带：中间浓两边淡，用三段叠出来
            for (int i = 0; i < 3; i++)
            {
                float k = (i + 1) / 3f;
                float w = Gfx.VW * (0.35f + 0.25f * i) * Mathf.SmoothStep(0, 1, reveal);
                Gfx.Rect(new Rect((Gfx.VW - w) * 0.5f, band.y + i * 6, w, band.height - i * 12), new Color(0.05f, 0.03f, 0.02f, 0.18f * a * (1.2f - k)));
            }

            var st = new GUIStyle(GUI.skin.label) { font = brushFont, fontSize = 72, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
            var size = st.CalcSize(new GUIContent(titleText));
            var r = new Rect((Gfx.VW - size.x) * 0.5f, y, size.x, h);
            // 写字：按宽度从左往右露出来
            GUI.BeginGroup(new Rect(r.x, r.y - 20, size.x * reveal + 4, h + 40));
            var inner = new Rect(0, 20, size.x, h);
            Gfx.SetColor(st, new Color(0, 0, 0, 0.6f * a));
            GUI.Label(new Rect(inner.x + 3, inner.y + 3, inner.width, inner.height), titleText, st);
            Gfx.SetColor(st, new Color(0.98f, 0.94f, 0.86f, a));
            GUI.Label(inner, titleText, st);
            GUI.EndGroup();
        }
    }
}
