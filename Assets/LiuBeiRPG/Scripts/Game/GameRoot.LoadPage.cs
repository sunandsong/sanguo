using System;
using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 读档页（首页「读档」和游戏菜单「读档」都跳这里）：书案背景 Art/load_bg（1080×1920，牌匾 y 185~330）上压三张记录卡 Art/load_card
    /// （1502×432，左边缩略图画框 x 52~623、y 58~375）。每张卡：存档所在地图的底图缩略图 + 「第几章 · 地名」+ 等级、队伍头像 + 存档时间。
    /// 图和提示词见 ArtSrc/读档页提示词.md。
    /// </summary>
    public partial class GameRoot
    {
        bool loadOpen;
        int loadSel;
        bool loadFromTitle;   // 首页进来的：选了档要先起一局；游戏菜单进来的：取消回菜单
        readonly Dictionary<int, Chapter> loadChapters = new Dictionary<int, Chapter>();   // 只用来按地图名找底图，不 Begin、不预载

        static readonly string[] CnNum = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };

        void OpenLoadPage(bool fromTitle)
        {
            loadOpen = true;
            loadFromTitle = fromTitle;
            loadSel = 0;
            for (int i = 0; i < SaveSlots; i++) if (PlayerPrefs.HasKey(SaveKey(i))) { loadSel = i; break; }
        }

        void CloseLoadPage()
        {
            loadOpen = false;
            if (!loadFromTitle) OpenMenu();
        }

        void PickLoad(int slot)
        {
            if (PlayerPrefs.GetString(SaveKey(slot), "") == "") return;   // 空档点不了
            loadOpen = false;
            if (loadFromTitle)
            {
                NewGame();   // 先起一局：建好章节和队伍，跳过开场
                dialogueQueue.Clear();
                currentDialogue = null;
                comicPages = null;
            }
            LoadSlot(slot);
        }

        void LoadPageKey(InputKey key)
        {
            if (key == InputKey.Up) loadSel = (loadSel + SaveSlots - 1) % SaveSlots;
            else if (key == InputKey.Down) loadSel = (loadSel + 1) % SaveSlots;
            else if (key == InputKey.Confirm) PickLoad(loadSel);
            else if (key == InputKey.Cancel) CloseLoadPage();
        }

        void DrawLoadPage()
        {
            // 背景等比铺满（竖屏长的手机裁两边）
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color32(58, 30, 22, 255));
            float s = Mathf.Max(Gfx.VW / 1080f, Gfx.VH / 1920f);
            float ox = (Gfx.VW - 1080 * s) * 0.5f, oy = (Gfx.VH - 1920 * s) * 0.5f;
            var bg = Gfx.Tex("Art/load_bg");
            Gfx.Image(new Rect(ox, oy, 1080 * s, 1920 * s), bg);

            // 牌匾（底图 x 60~1020、y 185~330）：竖屏底图上的那块露得出来就直接用；横屏被裁掉了，就从底图里切这一块画到顶上
            float safeTop = Mathf.Max(Gfx.SafeArea.y, 8);
            Rect plaque = new Rect(ox + 60 * s, oy + 185 * s, 960 * s, 145 * s);
            if (plaque.y < safeTop + 40)
            {
                float pw = Mathf.Min(Gfx.VW * 0.55f, 640), ph = pw * 145 / 960f;
                plaque = new Rect((Gfx.VW - pw) * 0.5f, safeTop + 4, pw, ph);
                if (bg != null) GUI.DrawTextureWithTexCoords(plaque, bg, new Rect(60 / 1080f, 1 - 330 / 1920f, 960 / 1080f, 145 / 1920f));
            }
            Gfx.FxText(plaque, "读  档", Mathf.RoundToInt(plaque.height * 0.43f), new Color32(255, 222, 140, 255));

            // 三张卡：宽按屏幕宽、高按牌匾下面剩的高度，取放得下的那个
            float top = plaque.yMax + 16, gap = Mathf.Clamp((Gfx.VH - top) * 0.03f, 8, 36);
            float ch = Mathf.Min(Mathf.Min(960 * s, Gfx.VW - 32) * 432 / 1502f, (Gfx.VH - top - 12 - gap * 2) / 3f), cw = ch * 1502 / 432f;
            float cx = (Gfx.VW - cw) * 0.5f, cy = top + Mathf.Max(0, (Gfx.VH - top - 12 - ch * 3 - gap * 2) * 0.25f);
            for (int i = 0; i < SaveSlots; i++)
            {
                var r = new Rect(cx, cy + i * (ch + gap), cw, ch);
                DrawLoadCard(r, i);
                if (Gfx.Clicked(r)) { loadSel = i; PickLoad(i); return; }
            }
            if (BackButton()) CloseLoadPage();
        }

        void DrawLoadCard(Rect r, int slot)
        {
            var json = PlayerPrefs.GetString(SaveKey(slot), "");
            var card = Gfx.Tex("Art/load_card");
            Func<float, float, float, float, Rect> C = (x, y, w, h) => new Rect(r.x + x / 1502f * r.width, r.y + y / 432f * r.height, w / 1502f * r.width, h / 432f * r.height);   // 卡图像素 → 屏幕
            var ink = new Color32(74, 44, 22, 255);
            if (json == "")
            {
                GUI.color = new Color(0.75f, 0.72f, 0.68f, 0.55f);
                Gfx.Image(r, card);
                GUI.color = Color.white;
                Gfx.Text(r, "第" + SlotNames[slot] + "档 · 空", Mathf.RoundToInt(r.height * 0.13f), new Color32(120, 100, 85, 255), TextAnchor.MiddleCenter, false, false);
                return;
            }
            var sd = JsonUtility.FromJson<SaveData>(json);
            bool sel = slot == loadSel;
            if (sel) Gfx.RoundRect(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), new Color32(255, 205, 104, 200));   // 键盘选中：金色外光
            Gfx.Image(r, card);

            // 缩略图：等比裁满画框
            var win = C(52, 58, 571, 317);
            var art = LoadArt(sd);
            if (art != null)
            {
                float wa = win.width / win.height, ta = art.width / (float)art.height;
                var uv = ta > wa ? new Rect((1 - wa / ta) * 0.5f, 0, wa / ta, 1) : new Rect(0, (1 - ta / wa) * 0.5f, 1, ta / wa);
                GUI.DrawTextureWithTexCoords(win, art, uv);
            }
            else Gfx.Rect(win, new Color32(92, 64, 44, 255));

            // 右边文字：第几档 / 「第几章 · 地名」/ 等级 + 头像 / 时间
            float tx = r.x + 668 / 1502f * r.width, tw = r.xMax - tx - 0.04f * r.width, h = r.height;
            Gfx.Text(new Rect(tx, r.y + h * 0.10f, tw, h * 0.16f), "第" + SlotNames[slot] + "档", Mathf.RoundToInt(h * 0.10f), new Color32(140, 96, 60, 255), TextAnchor.MiddleLeft, false, false);
            Gfx.Text(new Rect(tx, r.y + h * 0.25f, tw, h * 0.22f), "第" + CnNum[Mathf.Clamp(sd.Chapter, 1, 10)] + "章 · " + sd.Map, Mathf.RoundToInt(h * 0.15f), ink, TextAnchor.MiddleLeft, false, false);
            var lv = new Rect(tx, r.y + h * 0.50f, tw, h * 0.20f);
            Gfx.Text(lv, "Lv " + sd.Level, Mathf.RoundToInt(h * 0.12f), ink, TextAnchor.MiddleLeft, false, false);
            float hs = h * 0.19f, hx = tx + h * 0.42f;
            for (int i = 0; i < sd.Members.Count && i < 5; i++)
            {
                var def = GameDatabase.Find(sd.Members[i].Id);
                var head = def == null ? null : Remote.Tex("Puppets/" + (def.Skin ?? def.Id) + "/front/head");
                if (head == null) continue;
                float hw = hs * head.width / head.height;
                Gfx.Image(new Rect(hx, lv.center.y - hs * 0.5f, hw, hs), head);
                hx += hs * 0.95f;
            }
            long t;
            if (long.TryParse(PlayerPrefs.GetString(Cloud.TimeKey(SaveKey(slot)), ""), out t))
                Gfx.Text(new Rect(tx, r.y + h * 0.72f, tw, h * 0.16f), SaveTime(t), Mathf.RoundToInt(h * 0.095f), new Color32(140, 96, 60, 255), TextAnchor.MiddleLeft, false, false);
        }

        /// <summary>存档所在地图的手绘底图；没有底图的地图用这一章第一张有底图的。</summary>
        Texture2D LoadArt(SaveData sd)
        {
            Chapter c;
            if (!loadChapters.TryGetValue(sd.Chapter, out c))
            {
                c = PeekChapter(Math.Max(1, sd.Chapter));
                loadChapters[sd.Chapter] = c;
            }
            if (c == null) return null;
            var m = Array.Find(c.Maps, x => x.Name == sd.Map && x.Art != null) ?? Array.Find(c.Maps, x => x.Art != null);
            return m == null ? null : Gfx.Tex("Art/" + m.Art);
        }

        /// <summary>只为看地图数据建一章（不 Begin、不预载）。首页还没有剧情标记和队伍，章节建图时会碰到 host.Flags / host.Party：临时给一份空的，建完还原。</summary>
        Chapter PeekChapter(int n)
        {
            var oldFlags = flags; var oldParty = party;
            if (flags == null) flags = new StoryFlags();
            if (party == null) party = new Party();
            try { return BuildChapter(n); }
            catch (Exception e) { Debug.LogWarning("建第 " + n + " 章看地图数据失败：" + e); return null; }
            finally { flags = oldFlags; party = oldParty; }
        }

        /// <summary>首页一出来就在后台下：开场漫画、界面、最近那个存档所在的章（新玩家是第一章），再是法术特效和图标。只下载不解码，见 Remote.Prefetch。</summary>
        void TitlePrefetch()
        {
            if (titlePrefetched) return;
            titlePrefetched = true;
            foreach (var p in new[] { "Map/Art/intro", "Map/Art/ui", "Map/Art/dialogue_box_frame", "Map/Art/place_name_plate", "Map/Art/load", "Map/Art/battle_disc" }) Remote.Prefetch(p);
            int n = 1; long newest = -1;
            for (int i = 0; i < SaveSlots; i++)
            {
                var json = PlayerPrefs.GetString(SaveKey(i), "");
                if (json == "") continue;
                long t; long.TryParse(PlayerPrefs.GetString(Cloud.TimeKey(SaveKey(i)), "0"), out t);
                if (t >= newest) { newest = t; n = Math.Max(1, JsonUtility.FromJson<SaveData>(json).Chapter); }
            }
            var c = PeekChapter(n);
            if (c != null) PrefetchChapter(c);
            foreach (var p in new[] { "Map/Art/fx", "Map/Art/item", "Map/Art/tactic" }) Remote.Prefetch(p);
        }
        bool titlePrefetched;

        static string SaveTime(long ms)
        {
            var d = DateTimeOffset.FromUnixTimeMilliseconds(ms);
#if UNITY_WEBGL && !UNITY_EDITOR
            var t = d.UtcDateTime.AddHours(8);   // ponytail: 微信 WebGL 里拿不到本地时区，按北京时间；出海时改成从 wx.getSystemInfo 取时区
#else
            var t = d.LocalDateTime;
#endif
            return t.Month + "月" + t.Day + "日 " + t.Hour.ToString("00") + ":" + t.Minute.ToString("00");
        }
    }
}
