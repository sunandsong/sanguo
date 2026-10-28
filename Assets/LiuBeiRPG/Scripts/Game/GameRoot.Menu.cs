using System;
using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 大地图菜单（X / Esc / 手机“菜单”键）：人物、策略、物品、存档、读档。
    /// 列表借用选项框 Choose；人物详情、物品格子是单独画的页。
    /// </summary>
    public partial class GameRoot
    {
        int menuPage;      // 0 = 没开，PagePeople = 人物详情，PageItems = 物品格子
        int pageMember;    // 人物详情正在看第几个人
        const int PagePeople = 1, PageItems = 2;
        const int ItemSlots = 24;

        static readonly string[] MenuItems = { "人物", "策略", "物品", "存档", "读档", "返回" };

        // 兵器的中文名（Weapons 表里的引擎名 → 显示名）
        static readonly Dictionary<string, string> WeaponNames = new Dictionary<string, string>
        {
            { "jian", "双股剑" }, { "dao", "青龙偃月刀" }, { "qiang", "丈八蛇矛" },
        };

        void OpenMenu()
        {
            Choose("等级 " + party.Level + "　金 " + party.Gold + "　策略点 " + party.TP + "/" + party.MaxTP, MenuItems, i =>
            {
                switch (i)
                {
                    case 0: menuPage = PagePeople; pageMember = 0; break;
                    case 1: TacticMenu(); break;
                    case 2: menuPage = PageItems; itemSel = 0; break;
                    case 3: SaveGame(); break;
                    case 4: LoadGame(); break;
                }
            });
        }

        void MenuPageKey(InputKey key)
        {
            if (menuPage == PageItems) { ItemsKey(key); return; }
            int n = party.Members.Count;
            if (menuPage == PagePeople && key == InputKey.Left) pageMember = (pageMember + n - 1) % n;
            else if (menuPage == PagePeople && key == InputKey.Right) pageMember = (pageMember + 1) % n;
            else if (key == InputKey.Confirm || key == InputKey.Cancel) CloseMenuPage();
        }

        void CloseMenuPage() { menuPage = 0; OpenMenu(); }

        void DrawMenuPage()
        {
            if (menuPage == 0) return;
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, 0.45f));
            if (menuPage == PagePeople) DrawPeoplePage(); else DrawItemsPage();
        }

        /// <summary>面板 + 右上角“返回”。点返回关掉这一页。</summary>
        Rect PagePanel(float w, float h)
        {
            var r = new Rect((Gfx.VW - w) * 0.5f, (Gfx.VH - h) * 0.5f, w, h);
            Gfx.Modern(r, 0.95f);
            var back = new Rect(r.xMax - 96, r.y + 14, 80, 34);
            Gfx.RoundRect(back, new Color(1, 1, 1, 0.1f));
            Gfx.Text(back, "返回", 17, Color.white, TextAnchor.MiddleCenter);
            if (Gfx.Clicked(back)) CloseMenuPage();
            return r;
        }

        void DrawPeoplePage()
        {
            bool portrait = Gfx.Portrait;
            float w = Mathf.Min(Gfx.VW - 24, 780), h = Mathf.Min(Gfx.VH - 24, portrait ? 760 : 480);
            var r = PagePanel(w, h);
            if (menuPage == 0) return;   // 刚点了返回
            pageMember = Mathf.Clamp(pageMember, 0, party.Members.Count - 1);

            // 顶上一排人名页签
            for (int i = 0; i < party.Members.Count; i++)
            {
                var tr = new Rect(r.x + 16 + i * 96, r.y + 14, 88, 34);
                bool sel = i == pageMember;
                if (sel) Gfx.RoundRect(tr, Accent); else Gfx.RoundRect(tr, new Color(1, 1, 1, 0.08f));
                Gfx.Text(tr, party.Members[i].Name, 18, sel ? new Color(0.12f, 0.09f, 0.04f) : Color.white, TextAnchor.MiddleCenter, false, !sel);
                if (Gfx.Clicked(tr)) pageMember = i;
            }

            var m = party.Members[pageMember];
            float top = r.y + 62;
            // 人：横屏在左边一栏，竖屏在上面
            var stage = portrait ? new Rect(r.x, top, w, 300) : new Rect(r.x, top, 290, r.yMax - top);
            var foot = new Vector2(stage.center.x, stage.yMax - 56);
            var disc = Gfx.Tex("Art/battle_disc");
            if (disc != null)
            {
                var old = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.8f);
                Gfx.Image(new Rect(foot.x - 120, foot.y - 34, 240, 240f * disc.height / disc.width), disc);
                GUI.color = old;
            }
            var bust = Portrait(m.Name);
            if (bust != null) { bust.Tint = m.Alive ? Color.white : new Color(0.5f, 0.5f, 0.5f); bust.DrawGUI(foot, (stage.height - 110) / 950f); bust.Tint = Color.white; }   // 和对话头像是同一个，画完复原
            Gfx.Text(new Rect(stage.x, foot.y + 18, stage.width, 30), m.Name + "　Lv " + party.Level, 22, Accent, TextAnchor.MiddleCenter);

            // 数值和装备
            float x = portrait ? r.x + 20 : stage.xMax + 10, y = portrait ? stage.yMax + 4 : top + 6, rw = r.xMax - 20 - x;
            Gfx.Text(new Rect(x, y, rw, 24), "兵力", 16, new Color(1, 1, 1, 0.6f));
            Gfx.Text(new Rect(x, y, rw, 24), m.Soldiers + " / " + m.MaxSoldiers, 18, Color.white, TextAnchor.UpperRight);
            Gfx.Bar(new Rect(x, y + 28, rw, 14), m.MaxSoldiers > 0 ? (float)m.Soldiers / m.MaxSoldiers : 0, new Color(0.4f, 0.8f, 0.45f));
            y += 58;

            string[] labels = { "武力", "智力", "统率", "速度" };
            int[] values = { m.Wu, m.Zhi, m.Tong, m.Su };
            float cw = (rw - 30) / 4;
            for (int i = 0; i < 4; i++)
            {
                var cr = new Rect(x + i * (cw + 10), y, cw, 70);
                Gfx.RoundRect(cr, new Color(1, 1, 1, 0.06f));
                Gfx.Text(new Rect(cr.x, cr.y + 8, cr.width, 20), labels[i], 15, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter);
                Gfx.Text(new Rect(cr.x, cr.y + 30, cr.width, 32), values[i].ToString(), 28, Color.white, TextAnchor.MiddleCenter);
            }
            y += 90;

            Gfx.Text(new Rect(x, y, rw, 24), "装备", 16, new Color(1, 1, 1, 0.6f));
            y += 30;
            string weapon;
            if (m.WeaponBonus <= 0 || !Weapons.TryGetValue(m.Def.Id, out weapon)) weapon = null;   // 受赠之前空手，和地图上一致
            string[] slots = { "武器", "防具", "坐骑" };
            float sw = Mathf.Min(110, (rw - 20) / 3);
            for (int i = 0; i < 3; i++)
            {
                var sr = new Rect(x + i * (sw + 10), y, sw, sw);
                Gfx.RoundRect(sr, new Color(1, 1, 1, 0.06f));
                Gfx.Frame(sr, new Color(1, 1, 1, 0.12f), 1);
                bool has = i == 0 && weapon != null;
                if (has) WeaponIcon(sr, weapon);
                else Gfx.Text(sr, slots[i], 16, new Color(1, 1, 1, 0.25f), TextAnchor.MiddleCenter);
                string name;
                string under = has ? (WeaponNames.TryGetValue(weapon, out name) ? name : weapon) : "空";
                Gfx.Text(new Rect(sr.x - 10, sr.yMax + 6, sr.width + 20, 22), under, 15, has ? Color.white : new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter);
                if (has) Gfx.Text(new Rect(sr.x - 10, sr.yMax + 28, sr.width + 20, 20), "武力 +" + m.WeaponBonus, 14, Accent, TextAnchor.MiddleCenter);
            }
        }

        /// <summary>兵器原图是竖长条，斜 45° 放进格子里。</summary>
        static void WeaponIcon(Rect slot, string weapon)
        {
            var tex = Resources.Load<Texture2D>("Puppets/weapons/" + weapon + "/" + weapon);
            if (tex == null) return;
            float len = slot.width * 1.2f, wid = len * tex.width / tex.height;
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(45, slot.center);
            Gfx.Image(new Rect(slot.center.x - wid * 0.5f, slot.center.y - len * 0.5f, wid, len), tex);
            GUI.matrix = old;
        }

        int itemSel;   // 物品页选中的格子
        int ItemCols { get { return Gfx.Portrait ? 4 : 6; } }

        void ItemsKey(InputKey key)
        {
            int d = key == InputKey.Left ? -1 : key == InputKey.Right ? 1 : key == InputKey.Up ? -ItemCols : key == InputKey.Down ? ItemCols : 0;
            if (d != 0) itemSel = Mathf.Clamp(itemSel + d, 0, ItemSlots - 1);
            else if (key == InputKey.Confirm && itemSel < party.Items.Count) UseItem();
            else if (key == InputKey.Cancel) CloseMenuPage();
        }

        void UseItem()
        {
            Note(party.UseItem(itemSel), null);   // 对话框叠在物品页上，看完还停在这页
        }

        void DrawItemsPage()
        {
            int cols = ItemCols, rows = (ItemSlots + cols - 1) / cols;
            float cell = Mathf.Min(84, (Mathf.Min(Gfx.VW - 24, 640) - 40 - (cols - 1) * 10) / cols);
            float w = cols * cell + (cols - 1) * 10 + 40, gridH = rows * (cell + 10);
            var r = PagePanel(w, 64 + gridH + 96);
            if (menuPage == 0) return;
            Gfx.Text(new Rect(r.x + 20, r.y + 18, 200, 28), "物品　" + party.Items.Count + "/" + ItemSlots, 20, Accent);
            // ponytail: 还没有物品图标，格子里先写书名；出了图标再换成图
            for (int i = 0; i < ItemSlots; i++)
            {
                var cr = new Rect(r.x + 20 + (i % cols) * (cell + 10), r.y + 64 + (i / cols) * (cell + 10), cell, cell);
                bool has = i < party.Items.Count;
                Gfx.RoundRect(cr, has ? new Color(0.55f, 0.42f, 0.25f, 0.5f) : new Color(1, 1, 1, 0.06f));
                Gfx.Frame(cr, i == itemSel ? Accent : new Color(1, 1, 1, 0.1f), i == itemSel ? 2 : 1);
                if (has) Gfx.Text(cr, ItemDb.Get(party.Items[i]).Name, 20, Color.white, TextAnchor.MiddleCenter);
                if (Gfx.Clicked(cr)) itemSel = i;
            }
            // 底下：选中物品的说明 + 使用
            float y = r.y + 64 + gridH + 6;
            if (itemSel >= party.Items.Count) return;
            var item = ItemDb.Get(party.Items[itemSel]);
            var use = new Rect(r.xMax - 116, y + 22, 96, 40);
            Gfx.Text(new Rect(r.x + 20, y, use.x - r.x - 30, 24), "《" + item.Name + "》", 18, Accent);
            Gfx.Text(new Rect(r.x + 20, y + 28, use.x - r.x - 30, 56), item.Desc, 15, new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft, true);
            Gfx.RoundRect(use, Accent);
            Gfx.Text(use, "研读", 18, new Color(0.12f, 0.09f, 0.04f), TextAnchor.MiddleCenter, false, false);
            if (Gfx.Clicked(use)) UseItem();
        }

        // ---------------- 策略：地图上只能用恢复类 ----------------

        void TacticMenu()
        {
            var list = party.AvailableTactics();
            var names = new string[list.Count + 1];
            for (int i = 0; i < list.Count; i++) names[i] = list[i].Name + "　" + list[i].Cost + "点";
            names[list.Count] = "返回";
            Choose("策略点 " + party.TP + "/" + party.MaxTP + "（地图上只能用恢复类）", names, i =>
            {
                if (i >= list.Count) { OpenMenu(); return; }
                var t = list[i];
                if (t.Kind != TacticKind.Heal) { Note(t.Name + "只能在战斗中使用。", TacticMenu); return; }
                if (party.TP < t.Cost) { Note("策略点不足。", TacticMenu); return; }
                if (t.TargetsAll) { CastHeal(t, party.Members); return; }
                var targets = party.Members.FindAll(m => m.Alive);
                var opts = new string[targets.Count + 1];
                for (int j = 0; j < targets.Count; j++) opts[j] = targets[j].Name + "　" + targets[j].Soldiers + "/" + targets[j].MaxSoldiers;
                opts[targets.Count] = "返回";
                Choose("对谁使用" + t.Name + "？", opts, j =>
                {
                    if (j >= targets.Count) TacticMenu();
                    else CastHeal(t, new List<General> { targets[j] });
                });
            });
        }

        /// <summary>智力最高的活人施放，和战斗里同一个公式。</summary>
        void CastHeal(TacticDef t, List<General> targets)
        {
            General caster = null;
            foreach (var m in party.Members) if (m.Alive && (caster == null || m.Zhi > caster.Zhi)) caster = m;
            party.TP -= t.Cost;
            var lines = new List<DialogueLine> { new DialogueLine(null, caster.Name + "使用了" + t.Name + "。") };
            foreach (var g in targets)
                if (g.Alive) lines.Add(new DialogueLine(null, g.Name + " 兵力恢复 " + g.Heal(Formula.TacticHeal(caster, t, rng))));
            Say(lines, TacticMenu);
        }

        void Note(string text, Action then) { Say(new List<DialogueLine> { new DialogueLine(null, text) }, then); }

        // ---------------- 存档：一个档位，JSON 放 PlayerPrefs ----------------
        // 剧情进度全在 flags 里，地图上的人和东西都由 flags 推出来，所以只存队伍（含物品、兵书学的策略）+ flags + 位置

        const string SaveKey = "save1";

        [Serializable] class SavedMember { public string Id; public int Soldiers, MaxSoldiers, WeaponBonus; }
        [Serializable] class SaveData
        {
            public int Level, Exp, Gold, TP;
            public List<SavedMember> Members = new List<SavedMember>();
            public List<string> Flags = new List<string>();
            public List<string> Items = new List<string>(), Learned = new List<string>();
            public string Map;
            public int X, Y;
        }

        MapData[] ChapterMaps { get { return new[] { chapter.Town, chapter.Field, chapter.Home, chapter.Tavern, chapter.Inn, chapter.Inn2F }; } }

        void SaveGame()
        {
            var s = new SaveData { Level = party.Level, Exp = party.Exp, Gold = party.Gold, TP = party.TP, Map = map.Name, X = px, Y = py };
            foreach (var m in party.Members)
                s.Members.Add(new SavedMember { Id = m.Def.Id, Soldiers = m.Soldiers, MaxSoldiers = m.MaxSoldiers, WeaponBonus = m.WeaponBonus });
            s.Flags.AddRange(flags.All);
            s.Items.AddRange(party.Items);
            s.Learned.AddRange(party.Learned);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
            Note("已存档。", null);
        }

        void LoadGame()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (json == "") { Note("还没有存档。", OpenMenu); return; }
            var s = JsonUtility.FromJson<SaveData>(json);
            party = new Party { Level = s.Level, Exp = s.Exp, Gold = s.Gold };
            party.RecalcMaxTP();
            party.TP = s.TP;
            party.Items.AddRange(s.Items);
            party.Learned.UnionWith(s.Learned);
            var defs = new[] { GameDatabase.LiuBei, GameDatabase.GuanYu, GameDatabase.ZhangFei };
            foreach (var sm in s.Members)
            {
                var def = Array.Find(defs, d => d.Id == sm.Id);
                if (def != null) party.Members.Add(new General(def) { Soldiers = sm.Soldiers, MaxSoldiers = sm.MaxSoldiers, WeaponBonus = sm.WeaponBonus });
            }
            flags = new StoryFlags();
            foreach (var f in s.Flags) flags.Set(f);
            dialogueQueue.Clear();
            currentDialogue = null;
            choice = null;
            chapter = new Chapter1(this);
            var target = Array.Find(ChapterMaps, m => m.Name == s.Map) ?? chapter.Town;
            Warp(target, s.X, s.Y);
            Note("读档完成。", null);
        }
    }
}
