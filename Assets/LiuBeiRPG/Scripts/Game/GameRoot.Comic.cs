using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 开场漫画：横屏整页看，竖屏一格一格推镜头看。每页一句旁白打字机打出来。
    /// 确定 / 点屏幕：字没打完先打完，打完了下一格（竖屏）或下一页；取消 / 右上「跳过」：直接看完。
    /// </summary>
    public partial class GameRoot
    {
        // 每页的格子（原图像素，整页 1920×1080），按阅读顺序。新页面加在这里
        static readonly Dictionary<string, Rect[]> ComicPanels = new Dictionary<string, Rect[]>
        {
            { "intro_1", new[] { R(0, 0, 1920, 524), R(0, 537, 935, 1080), R(958, 537, 1920, 1080) } },
            { "intro_2", new[] { R(0, 0, 716, 1080), R(737, 0, 1920, 511), R(737, 533, 1920, 1080) } },
            { "intro_3", new[] { R(0, 0, 1920, 350), R(0, 364, 658, 1080), R(683, 364, 1920, 1080) } },
            { "intro_4", new[] { R(0, 0, 619, 1080), R(643, 0, 1920, 1080) } },
        };
        static Rect R(float x0, float y0, float x1, float y1) { return Rect.MinMaxRect(x0, y0, x1, y1); }

        string[] comicPages, comicCaps;
        Action comicDone;
        int comicPage, comicPanel;
        float comicPageAt, comicStepAt, comicCapAt;
        Rect comicFrom, comicCam;   // 竖屏推镜头：从哪格移到哪格（原图像素）
        const float ComicCps = 18f;  // 打字速度：字 / 秒

        public void ShowComic(string[] pages, string[] captions, Action onDone)
        {
            comicPages = pages;
            comicCaps = captions;
            comicDone = onDone;
            ComicGoPage(0);
        }

        bool ComicPortrait { get { return Gfx.VW < Gfx.VH; } }

        void ComicGoPage(int p)
        {
            comicPage = p;
            comicPanel = 0;
            comicPageAt = comicStepAt = comicCapAt = Time.time;
            comicFrom = ComicPanel(p, 0);
        }

        Rect ComicPanel(int page, int panel)
        {
            Rect[] ps;
            if (ComicPanels.TryGetValue(comicPages[page], out ps) && panel < ps.Length) return ps[panel];
            return new Rect(0, 0, 1920, 1080);
        }

        int ComicPanelCount(int page)
        {
            Rect[] ps;
            return ComicPanels.TryGetValue(comicPages[page], out ps) ? ps.Length : 1;
        }

        bool ComicTyped { get { return (Time.time - comicCapAt) * ComicCps >= Caption.Length; } }
        string Caption { get { return comicCaps != null && comicPage < comicCaps.Length ? comicCaps[comicPage] ?? "" : ""; } }

        void ComicNext()
        {
            if (!ComicTyped) { comicCapAt = -999; return; }   // 先把字打完
            if (ComicPortrait && comicPanel < ComicPanelCount(comicPage) - 1)
            {
                comicFrom = comicCam;
                comicPanel++;
                comicStepAt = Time.time;
                return;
            }
            if (comicPage < comicPages.Length - 1) { ComicGoPage(comicPage + 1); return; }
            ComicEnd();
        }

        void ComicEnd()
        {
            var done = comicDone;
            comicPages = null;
            comicDone = null;
            FadeIn();
            if (done != null) done();
        }

        void ComicKey(InputKey key)
        {
            if (key == InputKey.Confirm) ComicNext();
            else if (key == InputKey.Cancel) ComicEnd();
        }

        void DrawComic()
        {
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0.04f, 0.03f, 0.06f));
            var tex = Gfx.Tex("Art/" + comicPages[comicPage]);
            var safe = Gfx.SafeArea;
            bool portrait = ComicPortrait;
            float pageA = Mathf.Clamp01((Time.time - comicPageAt) / 0.5f);

            // 画面区域：横屏整屏（旁白压在底部），竖屏上面留给图、下面留给旁白
            float capH = portrait ? 170 : 96;
            var view = portrait ? new Rect(safe.x + 12, safe.y + 64, safe.width - 24, safe.height - 64 - capH - 12)
                                : new Rect(0, 0, Gfx.VW, Gfx.VH);
            Rect panelOnScreen = view;
            if (tex != null)
            {
                Rect cam;
                if (portrait)
                {
                    var to = ComicPanel(comicPage, Mathf.Min(comicPanel, ComicPanelCount(comicPage) - 1));
                    float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.time - comicStepAt) / 0.55f));
                    cam = new Rect(Mathf.Lerp(comicFrom.x, to.x, k), Mathf.Lerp(comicFrom.y, to.y, k),
                                   Mathf.Lerp(comicFrom.width, to.width, k), Mathf.Lerp(comicFrom.height, to.height, k));
                }
                else
                {
                    float z = 1 + 0.035f * Mathf.Clamp01((Time.time - comicPageAt) / 8f);   // 整页慢慢推近一点
                    cam = new Rect(960 - 960 / z, 540 - 540 / z, 1920 / z, 1080 / z);
                }
                comicCam = cam;
                // 横屏铺满（裁边），竖屏整格放进去（留边）
                float s = portrait ? Mathf.Min(view.width / cam.width, view.height / cam.height)
                                   : Mathf.Max(view.width / cam.width, view.height / cam.height);
                // 全按 1920×1080 的设计坐标算，贴图实际分辨率不影响
                var drawn = new Rect(view.center.x - cam.center.x * s, view.center.y - cam.center.y * s, 1920 * s, 1080 * s);
                GUI.color = new Color(1, 1, 1, pageA);
                Gfx.Image(drawn, tex);
                GUI.color = Color.white;
                panelOnScreen = new Rect(drawn.x + cam.x * s, drawn.y + cam.y * s, cam.width * s, cam.height * s);
                if (portrait)   // 当前格以外压暗，格子描一圈深色边
                {
                    var dim = new Color(0.04f, 0.03f, 0.06f, 0.88f);
                    Gfx.Rect(new Rect(0, 0, Gfx.VW, panelOnScreen.y), dim);
                    Gfx.Rect(new Rect(0, panelOnScreen.yMax, Gfx.VW, Gfx.VH - panelOnScreen.yMax), dim);
                    Gfx.Rect(new Rect(0, panelOnScreen.y, panelOnScreen.x, panelOnScreen.height), dim);
                    Gfx.Rect(new Rect(panelOnScreen.xMax, panelOnScreen.y, Gfx.VW - panelOnScreen.xMax, panelOnScreen.height), dim);
                }
            }

            // 旁白：深色带 + 打字机
            string cap = Caption;
            int shown = Mathf.Clamp(Mathf.FloorToInt((Time.time - comicCapAt) * ComicCps), 0, cap.Length);
            if (cap.Length > 0)
            {
                var band = portrait ? new Rect(0, safe.yMax - capH, Gfx.VW, capH + (Gfx.VH - safe.yMax))
                                    : new Rect(0, Gfx.VH - capH, Gfx.VW, capH);
                Gfx.Rect(band, new Color(0.05f, 0.03f, 0.1f, portrait ? 0.95f : 0.72f));
                Gfx.Rect(new Rect(band.x, band.y, band.width, 3), new Color(1f, 0.83f, 0.25f, 0.9f));
                var tr = portrait ? new Rect(safe.x + 24, band.y + 18, safe.width - 48, capH - 36)
                                  : new Rect(safe.x + 60, band.y + 10, safe.width - 120, capH - 20);
                Gfx.Text(tr, cap.Substring(0, shown), portrait ? 22 : 24, new Color(1f, 0.96f, 0.86f), portrait ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter, true);
            }
            // 进度：几页里的第几页，右下小圆点
            for (int i = 0; i < comicPages.Length; i++)
                Gfx.Pill(new Rect(safe.xMax - 24 - (comicPages.Length - i) * 16, safe.yMax - 16, i == comicPage ? 12 : 8, 8),
                         i == comicPage ? new Color(1f, 0.83f, 0.25f) : new Color(1, 1, 1, 0.4f));

            // 跳过：右上角小胶囊
            var skip = new Rect(safe.xMax - 92, safe.y + 14, 76, 34);
            Gfx.Pill(new Rect(skip.x - 3, skip.y - 3, skip.width + 6, skip.height + 6), new Color(0.13f, 0.1f, 0.28f, 0.9f));
            Gfx.Pill(skip, new Color(1f, 0.97f, 0.9f, 0.92f));
            Gfx.Text(skip, "跳过", 18, new Color(0.13f, 0.1f, 0.28f), TextAnchor.MiddleCenter, false, false);
            if (Gfx.Clicked(skip)) { ComicEnd(); return; }
            if (Gfx.Clicked(new Rect(0, 0, Gfx.VW, Gfx.VH))) ComicNext();
        }
    }
}
