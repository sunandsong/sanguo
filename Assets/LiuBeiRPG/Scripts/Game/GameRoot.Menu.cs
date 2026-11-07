using System;
using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 大地图菜单（X / Esc / 手机“菜单”键）：人物（装备 + 物品 + 策略）、存档、读档。
    /// </summary>
    public partial class GameRoot
    {
        int menuPage;      // 0 = 没开，PageMain = 菜单首页，PagePeople = 人物（装备 + 物品），PageShop = 商店，PageTeam = 队伍
        int pageMember;    // 人物页正在看第几个人
        int mainSel;       // 首页选中的按钮
        const int PagePeople = 1, PageMain = 3, PageShop = 4, PageTeam = 5;
        const int ItemSlots = Party.BagSlots;

        static readonly string[] MenuItems = { "人物 · 物品 · 法术", "队伍", "存档", "读档" };

        void OpenMenu() { menuPage = PageMain; }

        void MainPick(int i)
        {
            mainSel = i;
            switch (i)
            {
                case 0: menuPage = PagePeople; pageMember = 0; itemSel = 0; tacticSel = 0; break;
                case 1:   // 队伍调整只在城里（吞食天地的营地）：没有遇敌的地图都算城里
                    if (map.EncounterRate > 0) { Note("要回到城里才能调整队伍。", null); Sfx.Play("ui_error"); }
                    else { menuPage = PageTeam; teamSel = 0; }
                    break;
                case 2: menuPage = 0; SaveGame(); break;   // 选档的选项框弹在地图上，选完回到菜单
                case 3: menuPage = 0; LoadGame(); break;
            }
        }

        void MenuPageKey(InputKey key)
        {
            if (menuPage == PageShop) { ShopKey(key); return; }
            if (menuPage == PageTeam) { TeamKey(key); return; }
            if (menuPage == PageMain)
            {
                if (key == InputKey.Up) mainSel = (mainSel + MenuItems.Length - 1) % MenuItems.Length;
                else if (key == InputKey.Down) mainSel = (mainSel + 1) % MenuItems.Length;
                else if (key == InputKey.Confirm) MainPick(mainSel);
                else if (key == InputKey.Cancel) menuPage = 0;
                return;
            }
            // 人物页：左右换人，上下在当前页签里挑，确认使用；Tab 键切 物品 / 策略（见 GameRoot.cs）
            int n = party.Members.Count, d = key == InputKey.Up ? -1 : key == InputKey.Down ? 1 : 0;
            if (key == InputKey.Left) pageMember = (pageMember + n - 1) % n;
            else if (key == InputKey.Right) pageMember = (pageMember + 1) % n;
            else if (d != 0 && bagTab == 0) itemSel = Mathf.Clamp(itemSel + d, 0, Mathf.Max(0, party.Slots().Count - 1));
            else if (d != 0 && bagTab == 1) tacticSel = Mathf.Clamp(tacticSel + d, 0, Mathf.Max(0, party.AvailableTactics().Count - 1));
            else if (key == InputKey.Confirm && bagTab == 0 && itemSel < party.Slots().Count) UseItem();
            else if (key == InputKey.Confirm && bagTab == 1 && tacticSel < party.AvailableTactics().Count) UseTactic(party.AvailableTactics()[tacticSel]);
            else if (key == InputKey.Cancel) CloseMenuPage();
        }

        /// <summary>人物页、队伍页返回首页，首页、商店返回地图。</summary>
        void CloseMenuPage() { menuPage = menuPage == PagePeople || menuPage == PageTeam ? PageMain : 0; }

        void DrawMenuPage()
        {
            if (menuPage == 0) return;
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, 0.45f));
            if (menuPage == PageMain) DrawMainPage();
            else if (menuPage == PageShop) DrawShop();
            else if (menuPage == PageTeam) DrawTeamPage();
            else DrawPeoplePage();
        }

        // ---------------- 队伍（吞食天地的营地 + 队形）：换顺序、调去后方、调回 ----------------

        int teamSel;   // 0～Members.Count-1 是队里的，往后是后方的

        void TeamKey(InputKey key)
        {
            int n = party.Members.Count + party.Stored.Count, m = party.Members.Count;
            if (key == InputKey.Up) teamSel = (teamSel + n - 1) % n;
            else if (key == InputKey.Down) teamSel = (teamSel + 1) % n;
            else if (key == InputKey.Left && teamSel > 0 && teamSel < m) { party.Swap(teamSel, -1); teamSel--; }
            else if (key == InputKey.Right && teamSel < m - 1) { party.Swap(teamSel, 1); teamSel++; }
            else if (key == InputKey.Confirm) TeamToggle(teamSel);
            else if (key == InputKey.Cancel) CloseMenuPage();
        }

        /// <summary>队里的调去后方，后方的调回队里。</summary>
        void TeamToggle(int i)
        {
            int m = party.Members.Count;
            string msg = i < m ? party.ToStored(party.Members[i]) : party.ToMembers(party.Stored[i - m]);
            if (msg != null) { Note(msg, null); Sfx.Play("ui_error"); return; }
            Sfx.Play("equip");
            teamSel = Mathf.Clamp(teamSel, 0, party.Members.Count + party.Stored.Count - 1);
        }

        void DrawTeamPage()
        {
            float w = Mathf.Min(Gfx.VW - 24, 720), h = Mathf.Min(Gfx.VH - 24, 600);
            var r = PagePanel(w, h);
            if (menuPage != PageTeam) return;
            var fighters = party.Fighters();
            Gfx.Text(new Rect(r.x + 24, r.y + 18, w - 160, 30), "队伍　上场 " + fighters.Count + " 人 · 队里 " + party.Members.Count + "/" + Party.MaxMembers + " · 后方 " + party.Stored.Count, 20, Accent);
            Gfx.Text(new Rect(r.x + 24, r.y + 50, w - 48, 24), "活着的前 5 人上场，后面的后备；超过 5 人时军师留在后备供法术。前面有人败走，下一仗后备顶上。", 14, new Color(1, 1, 1, 0.55f));
            float x0 = r.x + 24, rw = w - 48, y = r.y + 84;
            const float rowH = 46;
            int m = party.Members.Count;
            for (int i = 0; i < m + party.Stored.Count; i++)
            {
                bool stored = i >= m;
                if (i == m)
                {
                    Gfx.Text(new Rect(x0, y + 6, rw, 24), "后方待命", 16, new Color(1, 1, 1, 0.5f));
                    y += 34;
                    if (party.Stored.Count == 0) { Gfx.Text(new Rect(x0, y, rw, 24), "（没有人。收编的武将队伍满了会放在这里）", 14, new Color(1, 1, 1, 0.35f)); break; }
                }
                var g = stored ? party.Stored[i - m] : party.Members[i];
                var row = new Rect(x0, y, rw, rowH - 6);
                if (row.yMax > r.yMax - 12) break;
                bool sel = i == teamSel;
                Gfx.RoundRect(row, new Color(1, 1, 1, sel ? 0.14f : 0.06f));
                if (sel) Gfx.Frame(row, Accent, 2);
                if (Gfx.Clicked(row)) teamSel = i;
                // 身份牌：军师金色，上场白色，后备灰色
                string role = stored ? "后方" : g == party.Strategist ? "军师" : fighters.Contains(g) ? "上场" : "后备";
                var tag = new Rect(row.x + 10, row.y + 8, 52, 24);
                Gfx.RoundRect(tag, role == "军师" ? new Color(Accent.r, Accent.g, Accent.b, 0.25f) : new Color(1, 1, 1, 0.08f));
                Gfx.Text(tag, role, 14, role == "军师" ? Accent : role == "上场" ? Color.white : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter, false, false);
                Gfx.Text(new Rect(row.x + 72, row.y + 6, 110, 28), g.Name, 19, g.Alive ? Color.white : Color.gray);
                Gfx.Bar(new Rect(row.x + 184, row.y + 16, Mathf.Max(60, rw - 184 - 250), 8), g.MaxSoldiers > 0 ? (float)g.Soldiers / g.MaxSoldiers : 0, new Color(0.4f, 0.8f, 0.45f));
                // 右边的按钮：队里的 ▲ ▼ 后方，后方的 调回
                float bxr = row.xMax - 8;
                if (!stored)
                {
                    var toBack = new Rect(bxr - 64, row.y + 5, 60, 30);
                    var down = new Rect(toBack.x - 44, row.y + 5, 38, 30);
                    var up = new Rect(down.x - 44, row.y + 5, 38, 30);
                    Gfx.RoundRect(up, new Color(1, 1, 1, i > 0 ? 0.12f : 0.04f));
                    Gfx.RoundRect(down, new Color(1, 1, 1, i < m - 1 ? 0.12f : 0.04f));
                    Gfx.RoundRect(toBack, new Color(1, 1, 1, 0.12f));
                    Gfx.Text(up, "▲", 15, new Color(1, 1, 1, i > 0 ? 1f : 0.3f), TextAnchor.MiddleCenter, false, false);
                    Gfx.Text(down, "▼", 15, new Color(1, 1, 1, i < m - 1 ? 1f : 0.3f), TextAnchor.MiddleCenter, false, false);
                    Gfx.Text(toBack, "后方", 15, Color.white, TextAnchor.MiddleCenter, false, false);
                    if (i > 0 && Gfx.Clicked(up)) { party.Swap(i, -1); teamSel = i - 1; return; }
                    if (i < m - 1 && Gfx.Clicked(down)) { party.Swap(i, 1); teamSel = i + 1; return; }
                    if (Gfx.Clicked(toBack)) { TeamToggle(i); return; }
                }
                else
                {
                    var back = new Rect(bxr - 64, row.y + 5, 60, 30);
                    Gfx.RoundRect(back, Accent);
                    Gfx.Text(back, "调回", 15, new Color(0.12f, 0.09f, 0.04f), TextAnchor.MiddleCenter, false, false);
                    if (Gfx.Clicked(back)) { TeamToggle(i); return; }
                }
                y += rowH;
            }
        }

        /// <summary>面板 + 右上角“返回”。点返回关掉这一页。</summary>
        Rect PagePanel(float w, float h)
        {
            var r = new Rect((Gfx.VW - w) * 0.5f, (Gfx.VH - h) * 0.5f, w, h);
            Gfx.Modern(r, 0.95f);
            var back = new Rect(r.xMax - 112, r.y + 12, 96, 44);
            Gfx.RoundRect(back, new Color(1, 1, 1, 0.1f));
            Gfx.Text(back, "返回", 20, Color.white, TextAnchor.MiddleCenter);
            if (Gfx.Clicked(back)) CloseMenuPage();
            return r;
        }

        /// <summary>菜单首页：左边主公立绘 + 全队兵力，右边一列大按钮（手指好点，64 高）。</summary>
        void DrawMainPage()
        {
            float w = Mathf.Min(Gfx.VW - 24, 660), h = Mathf.Min(Gfx.VH - 24, 470);
            var r = PagePanel(w, h);
            if (menuPage != PageMain) return;   // 刚点了返回
            float lw = w * 0.5f;

            // 左：主公站在圆盘上
            var foot = new Vector2(r.x + lw * 0.5f, r.y + 230);
            var disc = Gfx.Tex("Art/battle_disc");
            if (disc != null)
            {
                var old = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.8f);
                Gfx.Image(new Rect(foot.x - 100, foot.y - 28, 200, 200f * disc.height / disc.width), disc);
                GUI.color = old;
            }
            var leader = Portrait(party.Members[0].Name);
            if (leader != null) leader.DrawGUI(foot, 180 / 950f);

            // 左下：每人一条兵力
            float y = foot.y + 40, nameW = NameColumn(18, 60);
            foreach (var m in party.Members)
            {
                Gfx.Text(new Rect(r.x + 24, y, nameW, 26), m.Name, 18, m.Alive ? Color.white : Color.gray);
                Gfx.Bar(new Rect(r.x + 24 + nameW, y + 8, lw - 100 - nameW, 10), m.MaxSoldiers > 0 ? (float)m.Soldiers / m.MaxSoldiers : 0, new Color(0.4f, 0.8f, 0.45f));
                Gfx.Text(new Rect(r.x + lw - 72, y, 60, 26), m.Soldiers.ToString(), 15, new Color(1, 1, 1, 0.75f), TextAnchor.UpperRight);
                y += 32;
            }
            Gfx.Text(new Rect(r.x + 24, r.yMax - 40, lw - 30, 26), "Lv " + party.Level + "　金 " + party.Gold + "　法力 " + party.TP + "/" + party.MaxTP, 17, Accent);
            Gfx.Rect(new Rect(r.x + lw, r.y + 24, 1, h - 48), new Color(1, 1, 1, 0.1f));

            // 右：大按钮
            float bx = r.x + lw + 24, bw = w - lw - 48, bh = 64, gap = 10;
            float by0 = r.y + 64 + (h - 64 - 20 - MenuItems.Length * (bh + gap)) * 0.5f;
            for (int i = 0; i < MenuItems.Length; i++)
            {
                var br = new Rect(bx, by0 + i * (bh + gap), bw, bh);
                bool sel = i == mainSel;
                Gfx.RoundRect(br, sel ? Accent : new Color(1, 1, 1, 0.07f));
                Gfx.Text(br, MenuItems[i], 24, sel ? new Color(0.12f, 0.09f, 0.04f) : Color.white, TextAnchor.MiddleCenter, false, !sel);
                if (Gfx.Clicked(br)) { MainPick(i); return; }
            }
        }

        /// <summary>人物页：横屏左边人物（立绘 + 数值 + 装备）右边行囊；竖屏上下排。</summary>
        void DrawPeoplePage()
        {
            bool portrait = Gfx.Portrait;
            float w = portrait ? Gfx.VW - 24 : Mathf.Min(Gfx.VW - 24, 940);
            float h = portrait ? Mathf.Min(Gfx.VH - 24, 860) : Mathf.Min(Gfx.VH - 24, 500);
            var r = PagePanel(w, h);
            if (menuPage != PagePeople) return;   // 刚点了返回
            pageMember = Mathf.Clamp(pageMember, 0, party.Members.Count - 1);

            // 顶上一排人名页签
            for (int i = 0; i < party.Members.Count; i++)
            {
                var tr = new Rect(r.x + 16 + i * 96, r.y + 12, 88, 44);
                bool sel = i == pageMember;
                if (sel) Gfx.RoundRect(tr, Accent); else Gfx.RoundRect(tr, new Color(1, 1, 1, 0.08f));
                Gfx.Text(tr, party.Members[i].Name, 20, sel ? new Color(0.12f, 0.09f, 0.04f) : Color.white, TextAnchor.MiddleCenter, false, !sel);
                if (Gfx.Clicked(tr)) pageMember = i;
            }

            float top = r.y + 68;
            Rect who, bag;
            if (portrait) { who = new Rect(r.x, top, w, 420); bag = new Rect(r.x, top + 430, w, r.yMax - top - 430); }   // 装备两排六格，人物这块要 420 高
            else { who = new Rect(r.x, top, 540, r.yMax - top); bag = new Rect(r.x + 550, top, w - 550, r.yMax - top); }
            Gfx.Rect(portrait ? new Rect(r.x + 20, bag.y - 6, w - 40, 1) : new Rect(bag.x - 4, top, 1, bag.height - 16), new Color(1, 1, 1, 0.1f));
            DrawCharacter(party.Members[pageMember], who);
            // 右边三个页签：物品 / 法术 / 任务
            string[] tabs = { "物品", "法术", "任务" };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tr = new Rect(bag.x + 16 + i * 104, bag.y, 96, 40);
                bool sel = i == bagTab;
                Gfx.RoundRect(tr, sel ? Accent : new Color(1, 1, 1, 0.08f));
                Gfx.Text(tr, tabs[i], 20, sel ? new Color(0.12f, 0.09f, 0.04f) : Color.white, TextAnchor.MiddleCenter, false, !sel);
                if (Gfx.Clicked(tr)) bagTab = i;
            }
            var content = new Rect(bag.x, bag.y + 52, bag.width, bag.height - 52);
            if (bagTab == 0) DrawBag(content);
            else if (bagTab == 1) DrawTactics(content, party.Members[pageMember]);
            else DrawQuests(content);
        }

        void DrawCharacter(General m, Rect area)
        {
            var stage = new Rect(area.x, area.y, 210, area.height);
            var foot = new Vector2(stage.center.x, stage.yMax - 56);
            var disc = Gfx.Tex("Art/battle_disc");
            if (disc != null)
            {
                var old = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.8f);
                Gfx.Image(new Rect(foot.x - 100, foot.y - 28, 200, 200f * disc.height / disc.width), disc);
                GUI.color = old;
            }
            var bust = Portrait(m.Name);
            if (bust != null) { bust.Tint = m.Alive ? Color.white : new Color(0.5f, 0.5f, 0.5f); bust.DrawGUI(foot, Mathf.Min(stage.height - 110, 300) / 950f); bust.Tint = Color.white; }   // 和对话头像是同一个，画完复原
            Gfx.Text(new Rect(stage.x, foot.y + 18, stage.width, 30), m.Name + "　Lv " + party.Level, 22, Accent, TextAnchor.MiddleCenter);

            // 数值和装备
            float x = stage.xMax + 10, y = area.y + 4, rw = area.xMax - 10 - x;
            Gfx.Text(new Rect(x, y, rw, 24), "兵力", 16, new Color(1, 1, 1, 0.6f));
            Gfx.Text(new Rect(x, y, rw, 24), m.Soldiers + " / " + m.MaxSoldiers, 18, Color.white, TextAnchor.UpperRight);
            Gfx.Bar(new Rect(x, y + 28, rw, 14), m.MaxSoldiers > 0 ? (float)m.Soldiers / m.MaxSoldiers : 0, new Color(0.4f, 0.8f, 0.45f));
            y += 58;

            string[] labels = { "攻击", "谋略", "防御", "速度" };
            int[] values = { m.Atk, m.Mou, m.Defense, m.Su };   // 含装备、乘过等级成长的实际值
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
            // 军师：只有军师能放法术（吞食天地）。是军师的挂金牌，不是的给个任命按钮
            var sr0 = new Rect(x + rw - 120, y - 8, 120, 34);
            if (m == party.Strategist)
            {
                Gfx.RoundRect(sr0, new Color(Accent.r, Accent.g, Accent.b, 0.2f));
                Gfx.Text(sr0, "军师", 17, Accent, TextAnchor.MiddleCenter);
            }
            else
            {
                Gfx.RoundRect(sr0, new Color(1, 1, 1, 0.1f));
                Gfx.Text(sr0, "任命为军师", 16, Color.white, TextAnchor.MiddleCenter);
                if (Gfx.Clicked(sr0)) party.Strategist = m;
            }
            y += 30;
            // 六格装备，两排三格（顺序见 ItemKind）；点有东西的格子卸回行囊（行囊里点装备按“装备”换上）
            // 格子下面不写名字、加成：图标看得出是什么，加成已经算进上面的数值里
            string[] slots = { "武器", "头盔", "防具", "鞋子", "坐骑", "饰品" };
            const float under = 12;   // 两排格子之间的空
            float sw = Mathf.Min(110, Mathf.Min((rw - 20) / 3, (area.yMax - y - 2 * under) / 2));
            float sgx = (rw - 3 * sw) / 2;   // 格子变小时横向撑开，左右贴齐
            for (int i = 0; i < General.GearSlots; i++)
            {
                var sr = new Rect(x + (i % 3) * (sw + sgx), y + (i / 3) * (sw + under), sw, sw);
                Gfx.RoundRect(sr, new Color(1, 1, 1, 0.06f));
                Gfx.Frame(sr, new Color(1, 1, 1, 0.12f), 1);
                var item = ItemDb.Get(m.Gear[i]);
                if (item != null) ItemIcon(sr, item);
                else Gfx.Text(sr, slots[i], 16, new Color(1, 1, 1, 0.25f), TextAnchor.MiddleCenter);
                if (item == null) continue;
                if (Gfx.Clicked(sr))
                {
                    if (!party.CanHold(item.Id)) Note("行囊满了，卸不下来。", null);
                    else party.Unequip(m, i);   // 格子一空就看得出卸了，不弹字
                }
            }
        }

        /// <summary>物品图标：兵器用引擎贴图斜放，其余 Art/item_&lt;Id&gt;（见 ArtSrc/物品图标提示词.md；缺图就空着，不写字顶替）。</summary>
        static void ItemIcon(Rect cell, ItemDef item)
        {
            if (item.Sprite != null) { WeaponIcon(new Rect(cell.x + 6, cell.y + 6, cell.width - 12, cell.height - 12), item.Sprite); return; }
            var icon = Gfx.Tex("Art/item_" + item.Id);
            if (icon != null) Gfx.Image(new Rect(cell.x + 4, cell.y + 4, cell.width - 8, cell.height - 8), icon);
        }

        /// <summary>同种叠放的件数，写在格子右下角。</summary>
        void StackCount(Rect cell, string id)
        {
            int n = party.Count(id);
            if (n > 1) Gfx.Text(new Rect(cell.x, cell.yMax - 22, cell.width - 5, 20), n.ToString(), 15, Color.white, TextAnchor.LowerRight);
        }

        /// <summary>兵器原图是竖长条，斜 45° 放进格子里。</summary>
        static void WeaponIcon(Rect slot, string weapon)
        {
            var tex = Resources.Load<Texture2D>("Puppets/weapons/" + weapon + "/" + weapon);
            if (tex == null) return;
            float len = slot.width * 1.2f, wid = len * tex.width / tex.height;
            var old = GUI.matrix;
            // 不用 GUIUtility.RotateAroundPivot：它把中心点当屏幕像素，GUI.matrix 有缩放时会转到格子外面去
            var c = (Vector3)slot.center;
            GUI.matrix = old * Matrix4x4.TRS(c, Quaternion.Euler(0, 0, 45), Vector3.one) * Matrix4x4.TRS(-c, Quaternion.identity, Vector3.one);
            Gfx.Image(new Rect(slot.center.x - wid * 0.5f, slot.center.y - len * 0.5f, wid, len), tex);
            GUI.matrix = old;
        }

        int itemSel;   // 行囊里选中的格子

        void UseItem()
        {
            string id = party.Slots()[itemSel];
            var item = ItemDb.Get(id);
            if (item.Kind == ItemKind.Tool && !item.InBattle) { UseTool(item); return; }
            int had = party.Count(id);
            string msg = party.UseItem(id, party.Members[pageMember]);
            if (msg != null) Note(msg, null);
            if (item.Equip) Sfx.Play("equip");
            else if (party.Count(id) >= had) Sfx.Play("ui_error");   // 没用掉：满了 / 救不回来
            else Sfx.Play(item.Kind == ItemKind.Book ? "learn" : item.Kind == ItemKind.Wine ? "drink" : "heal");
        }

        /// <summary>地图上用的道具：烟幕弹一段路不遇敌；飞鸟扇飞回去过的城。</summary>
        void UseTool(ItemDef item)
        {
            if (item.Effect == "smoke")
            {
                party.Items.Remove(item.Id);
                smokeSteps = 60;
                Sfx.Play("cast");
                Note("撒下烟幕，接下来 60 步不会遇敌。", null);
                return;
            }
            var towns = chapter.Towns.FindAll(t => flags.Has("visited_" + t.Map.Id));
            if (towns.Count == 0) { Note("还没有去过的城池。", null); Sfx.Play("ui_error"); return; }
            var names = new List<string>(towns.ConvertAll(t => t.Map.Name)) { "取消" };
            menuPage = 0;
            Choose("飞往哪座城？", names.ToArray(), i =>
            {
                if (i >= towns.Count) { OpenMenu(); return; }
                party.Items.Remove(item.Id);
                Warp(towns[i].Map, towns[i].X, towns[i].Y);
            });
        }

        /// <summary>行囊按钮上的字：兵书研读、药酒使用、装备给左边这个人换上。</summary>
        string UseLabel(ItemDef item)
        {
            if (item.Kind == ItemKind.Book) return "研读";
            if (item.Equip) return "装备";
            return item.Kind == ItemKind.Heal || item.Kind == ItemKind.Revive ? "给" + party.Members[pageMember].Name : "使用";
        }

        /// <summary>行囊：6 列格子，同种叠一格，下面是选中物品的说明和使用按钮。</summary>
        void DrawBag(Rect area)
        {
            float x0 = area.x + 16, gw = area.width - 32;
            var slotsList = party.Slots();
            // 格子固定 80，每排能放几个放几个，一页放得下几排就几排（上面 32 标题，下面留 84 给说明和“研读”），24 格放不下就翻页。
            // 当前页跟着选中的格子走，方向键选到下一页的物品就自动翻过去
            const float cell = 80;
            int cols = Mathf.Max(1, Mathf.FloorToInt((gw + 8) / (cell + 8)));
            int rows = Mathf.Max(1, Mathf.FloorToInt((area.height - 32 - 84 + 8) / (cell + 8)));
            int perPage = cols * rows, pages = (ItemSlots + perPage - 1) / perPage;
            int page = Mathf.Clamp(itemSel / perPage, 0, pages - 1);
            float gx = cols > 1 ? (gw - cols * cell) / (cols - 1) : 0;   // 横向间距撑开，左右都贴齐，右边不留空
            Gfx.Text(new Rect(x0, area.y, gw, 26), "物品　" + slotsList.Count + "/" + ItemSlots, 18, new Color(1, 1, 1, 0.6f));
            if (pages > 1)
            {
                // 右上角翻页：‹ 1/2 ›
                var prev = new Rect(x0 + gw - 120, area.y - 4, 34, 30);
                var next = new Rect(x0 + gw - 34, area.y - 4, 34, 30);
                Gfx.RoundRect(prev, new Color(1, 1, 1, page > 0 ? 0.12f : 0.04f));
                Gfx.RoundRect(next, new Color(1, 1, 1, page < pages - 1 ? 0.12f : 0.04f));
                Gfx.Text(prev, "‹", 22, new Color(1, 1, 1, page > 0 ? 1f : 0.3f), TextAnchor.MiddleCenter);
                Gfx.Text(next, "›", 22, new Color(1, 1, 1, page < pages - 1 ? 1f : 0.3f), TextAnchor.MiddleCenter);
                Gfx.Text(new Rect(prev.xMax, area.y - 4, next.x - prev.xMax, 30), (page + 1) + "/" + pages, 16, new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
                if (page > 0 && Gfx.Clicked(prev)) itemSel = (page - 1) * perPage;
                if (page < pages - 1 && Gfx.Clicked(next)) itemSel = (page + 1) * perPage;
            }
            float gy = area.y + 32;
            for (int k = 0; k < perPage; k++)
            {
                int i = page * perPage + k;
                if (i >= ItemSlots) break;
                var cr = new Rect(x0 + (k % cols) * (cell + gx), gy + (k / cols) * (cell + 8), cell, cell);
                bool has = i < slotsList.Count;
                Gfx.RoundRect(cr, has ? new Color(0.55f, 0.42f, 0.25f, 0.5f) : new Color(1, 1, 1, 0.06f));
                Gfx.Frame(cr, i == itemSel ? Accent : new Color(1, 1, 1, 0.1f), i == itemSel ? 2 : 1);
                if (has) { ItemIcon(cr, ItemDb.Get(slotsList[i])); StackCount(cr, slotsList[i]); }
                if (Gfx.Clicked(cr)) itemSel = i;
            }
            // 底下：选中物品的说明 + 使用
            float y = gy + rows * (cell + 8) + 4;
            if (itemSel >= slotsList.Count) return;
            var item = ItemDb.Get(slotsList[itemSel]);
            var use = new Rect(area.x + area.width - 16 - 96, y + 16, 96, 48);
            Gfx.Text(new Rect(x0, y, use.x - x0 - 10, 24), item.Kind == ItemKind.Book ? "《" + item.Name + "》" : item.Name, 18, Accent);
            Gfx.Text(new Rect(x0, y + 26, use.x - x0 - 10, 60), item.Desc, 15, new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft, true);
            if (item.Kind == ItemKind.Material) return;   // 材料只能卖，没有使用按钮
            Gfx.RoundRect(use, Accent);
            Gfx.Text(use, UseLabel(item), 20, new Color(0.12f, 0.09f, 0.04f), TextAnchor.MiddleCenter, false, false);
            if (Gfx.Clicked(use)) UseItem();
        }

        // ---------------- 策略：地图上只能用恢复类 ----------------

        int bagTab;      // 人物页右边：0 = 物品，1 = 策略，2 = 任务

        /// <summary>
        /// 任务页：接了的支线一张卡一个——名字、谁所托、现在该做什么、进度、奖励；做完的压暗写“已完成”，排在后面。
        /// 一个没接时提示去找头顶“！”的人。
        /// </summary>
        void DrawQuests(Rect area)
        {
            float x0 = area.x + 16, w = area.width - 32, y = area.y;
            var list = chapter.Quests.FindAll(q => q.Taken);
            list.Sort((a, b) => a.Done.CompareTo(b.Done));
            if (list.Count == 0)
            {
                Gfx.Text(new Rect(x0, y + 8, w, 60), "还没有接任务。\n城里、野外头顶有「！」的人，去问问。", 17, new Color(1, 1, 1, 0.6f), TextAnchor.UpperLeft, true);
                return;
            }
            foreach (var q in list)
            {
                int stage = q.Stage();
                var card = new Rect(x0, y, w, 108);
                if (card.yMax > area.yMax) break;
                Gfx.RoundRect(card, new Color(1, 1, 1, q.Done ? 0.04f : 0.08f));
                Gfx.RoundRect(new Rect(card.x, card.y, 4, card.height), q.Done ? new Color(1, 1, 1, 0.25f) : Accent);
                var dim = q.Done ? 0.45f : 1f;
                Gfx.Text(new Rect(card.x + 16, card.y + 8, w - 120, 28), q.Name, 20, new Color(Accent.r, Accent.g, Accent.b, dim));
                Gfx.Text(new Rect(card.x + 16, card.y + 8, w - 32, 28), q.Done ? "已完成" : q.Giver + "所托", 15, new Color(1, 1, 1, 0.5f * dim), TextAnchor.MiddleRight);
                string step = q.Done ? "已交差。" : q.Steps[Mathf.Clamp(stage - 1, 0, q.Steps.Length - 1)];
                Gfx.Text(new Rect(card.x + 16, card.y + 40, w - 32, 26), (q.Done ? "" : "当前：") + step, 16, new Color(1, 1, 1, 0.9f * dim));
                if (!q.Done && q.Progress != null)
                    Gfx.Text(new Rect(card.x + 16, card.y + 40, w - 32, 26), q.Progress(), 16, stage >= q.Steps.Length ? new Color(0.5f, 1f, 0.55f) : new Color(0.7f, 0.88f, 1f), TextAnchor.MiddleRight);
                Gfx.Text(new Rect(card.x + 16, card.y + 72, w - 32, 24), "奖励：" + q.Reward, 15, new Color(1, 1, 1, 0.55f * dim));
                y += card.height + 10;
            }
        }
        int tacticSel;

        /// <summary>法术：和物品一样一格一个图标，不写字；只列学会的。选中的在下面写名字和说明。
        /// 恢复类在地图上能用，单体的给左边正在看的这个人。</summary>
        void DrawTactics(Rect area, General who)
        {
            const int cols = 4;
            float x0 = area.x + 16, gw = area.width - 32;
            float cell = Mathf.Min(80, (gw - (cols - 1) * 10) / cols);
            float gx = (gw - cols * cell) / (cols - 1);   // 和物品格子一样左右贴齐
            Gfx.Text(new Rect(x0, area.y, gw, 24), "法力 " + party.TP + "/" + party.MaxTP + "　军师 " + party.Strategist.Name, 17, new Color(1, 1, 1, 0.6f));
            var learned = party.AvailableTactics();   // 只列学会的，没学会的不出现
            if (learned.Count == 0) return;
            tacticSel = Mathf.Clamp(tacticSel, 0, learned.Count - 1);
            float gy = area.y + 32;
            for (int i = 0; i < learned.Count; i++)
            {
                var t = learned[i];
                var cr = new Rect(x0 + (i % cols) * (cell + gx), gy + (i / cols) * (cell + 10), cell, cell);
                Gfx.RoundRect(cr, new Color(1, 1, 1, 0.06f));
                Gfx.Frame(cr, i == tacticSel ? Accent : new Color(1, 1, 1, 0.1f), i == tacticSel ? 2 : 1);
                var icon = Gfx.Tex("Art/tactic_" + t.Id);   // 图标见 ArtSrc/法术图标提示词.md；缺图就空着，不写字顶替
                if (icon != null) Gfx.Image(new Rect(cr.x + 4, cr.y + 4, cr.width - 8, cr.height - 8), icon);
                if (Gfx.Clicked(cr)) tacticSel = i;
            }

            // 底下：选中策略的名字、消耗、说明；能在地图上用的出“使用”按钮
            var cur = learned[tacticSel];
            bool usable = cur.Kind == TacticKind.Heal;
            float y = gy + ((learned.Count + cols - 1) / cols) * (cell + 10) + 4;
            float tw = usable ? gw - 190 : gw;
            Gfx.Text(new Rect(x0, y, tw, 26), cur.Name + "　" + cur.Cost + " 点", 19, Accent);
            string line2 = usable ? cur.Desc : cur.Desc + "（战斗中使用）";
            Gfx.Text(new Rect(x0, y + 28, tw, 44), line2, 15, new Color(1, 1, 1, 0.75f), TextAnchor.UpperLeft, true);
            if (!usable) return;
            var use = new Rect(area.xMax - 16 - 180, y + 10, 180, 48);
            Gfx.RoundRect(use, Accent);
            Gfx.Text(use, cur.TargetsAll ? "使用（全队）" : "使用（" + who.Name + "）", 19, new Color(0.12f, 0.09f, 0.04f), TextAnchor.MiddleCenter, false, false);
            if (Gfx.Clicked(use)) UseTactic(cur);
        }

        /// <summary>名字那一列的宽度：按队里最长的名字算，四个字的「黄巾头目」不会压到后面的兵力。</summary>
        float NameColumn(int fontSize, float min)
        {
            int n = 0;
            foreach (var m in party.Members) n = Mathf.Max(n, m.Name.Length);
            return Mathf.Max(min, n * fontSize + 10);
        }

        void UseTactic(TacticDef t)
        {
            if (t.Kind != TacticKind.Heal || party.TP < t.Cost || !party.Strategist.Alive || !t.TargetsAll && !party.Members[pageMember].Alive) Sfx.Play("ui_error");
            if (t.Kind != TacticKind.Heal) { Note(t.Name + "只能在战斗中使用。", null); return; }
            if (party.TP < t.Cost) { Note("法力不足。", null); return; }
            if (!party.Strategist.Alive) { Note("军师" + party.Strategist.Name + "已经败走，放不了法术。", null); return; }
            var who = party.Members[pageMember];
            if (!t.TargetsAll && !who.Alive) { Note(who.Name + "已经败走，疗兵救不回来。", null); return; }
            CastHeal(t, t.TargetsAll ? party.Members : new List<General> { who });
        }

        /// <summary>军师施放，和战斗里同一个公式。</summary>
        void CastHeal(TacticDef t, List<General> targets)
        {
            var caster = party.Strategist;
            party.TP -= t.Cost;
            Sfx.Play("heal_magic");
            var lines = new List<DialogueLine> { new DialogueLine(null, caster.Name + "使用了" + t.Name + "。") };
            foreach (var g in targets)
                if (g.Alive) lines.Add(new DialogueLine(null, g.Name + " 兵力恢复 " + g.Heal(Formula.TacticHeal(caster, t, rng))));
            Say(lines);   // 对话框叠在人物页上，看完还停在这页
        }

        /// <summary>菜单里的小提示：屏幕下方淡入一条字，停一会儿自己淡出，不用点；新的直接顶掉旧的。then 立刻接着走。</summary>
        void Note(string text, Action then)
        {
            noteText = text;
            noteAt = Time.time;
            if (then != null) then();
        }

        string noteText;
        float noteAt;
        const float NoteHold = 1.8f, NoteFade = 0.3f;

        void DrawNote()
        {
            if (noteText == null) return;
            float t = Time.time - noteAt;
            if (t > NoteHold + NoteFade) { noteText = null; return; }
            float a = Mathf.Clamp01(t / 0.15f) * (1 - Mathf.Clamp01((t - NoteHold) / NoteFade));
            float w = Mathf.Min(Gfx.VW - 32, 40 + noteText.Length * 20), h = 52;
            var r = new Rect((Gfx.VW - w) / 2, Gfx.SafeArea.yMax - h - 40 + (1 - Mathf.Clamp01(t / 0.15f)) * 12, w, h);   // 淡入时从下面升一点
            Gfx.RoundRect(r, new Color(0.08f, 0.06f, 0.04f, 0.85f * a));
            Gfx.Text(r, noteText, 19, new Color(1, 1, 1, a), TextAnchor.MiddleCenter, false, false);
        }

        // ---------------- 存档：三个档位（吞食天地），JSON 放 PlayerPrefs ----------------
        // 剧情进度全在 flags 里，地图上的人和东西都由 flags 推出来，所以只存队伍（含物品、学会的法术）+ flags + 位置

        const int SaveSlots = 3;
        static string SaveKey(int slot) { return "save" + (slot + 1); }   // 第一档就是以前的 save1，老档照读
        static readonly string[] SlotNames = { "一", "二", "三" };

        [Serializable] class SavedMember { public string Id; public string[] Gear; public int Soldiers, WeaponBonus; }   // WeaponBonus：旧档用
        [Serializable] class SaveData
        {
            public int Level, Exp, Gold, TP, TPBonus, Chapter;
            public string Strategist;
            public List<SavedMember> Members = new List<SavedMember>(), Stored = new List<SavedMember>();
            public List<string> Flags = new List<string>();
            public List<string> Items = new List<string>(), Learned = new List<string>();
            public string Map;
            public int X, Y;
        }


        /// <summary>选档框的选项：「第一档：Lv 3　涿县」，空的写「空」，最后一项取消。</summary>
        string[] SlotLabels()
        {
            var labels = new string[SaveSlots + 1];
            for (int i = 0; i < SaveSlots; i++)
            {
                var json = PlayerPrefs.GetString(SaveKey(i), "");
                var sd = json == "" ? null : JsonUtility.FromJson<SaveData>(json);
                labels[i] = "第" + SlotNames[i] + "档：" + (sd == null ? "空" : "Lv " + sd.Level + "　" + sd.Map);
            }
            labels[SaveSlots] = "取消";
            return labels;
        }

        void SaveGame()
        {
            Choose("存到哪一档？", SlotLabels(), slot =>
            {
                OpenMenu();
                if (slot >= SaveSlots) return;
                var s = new SaveData { Level = party.Level, Exp = party.Exp, Gold = party.Gold, TP = party.TP, TPBonus = party.TPBonus, Chapter = chapter.Number, Map = map.Name, X = px, Y = py };
                foreach (var m in party.Members) s.Members.Add(new SavedMember { Id = m.Def.Id, Soldiers = m.Soldiers, Gear = m.Gear });
                foreach (var m in party.Stored) s.Stored.Add(new SavedMember { Id = m.Def.Id, Soldiers = m.Soldiers, Gear = m.Gear });
                s.Flags.AddRange(flags.All);
                s.Items.AddRange(party.Items);
                s.Learned.AddRange(party.Learned);
                s.Strategist = party.Strategist.Def.Id;
                PlayerPrefs.SetString(SaveKey(slot), JsonUtility.ToJson(s));
                PlayerPrefs.Save();
                Note("已存到第" + SlotNames[slot] + "档。", null);
                Sfx.Play("save");
            });
        }

        static string Nul(string s) { return string.IsNullOrEmpty(s) ? null : s; }   // JsonUtility 把 null 存成 ""

        General LoadMember(SavedMember sm, int level)
        {
            var def = GameDatabase.Find(sm.Id);
            if (def == null) return null;
            var g = new General(def, level);   // 兵力上限按等级算，不读存档（旧档是按老公式存的）
            g.Soldiers = Math.Min(sm.Soldiers, g.MaxSoldiers);
            for (int i = 0; sm.Gear != null && i < sm.Gear.Length && i < General.GearSlots; i++) g.Gear[i] = Nul(sm.Gear[i]);
            if (g.Weapon == null && sm.WeaponBonus > 0) g.Weapon = ItemDb.Signature(def.Id).Id;   // 旧档：受赠过就是专属兵器
            return g;
        }

        void LoadGame()
        {
            Choose("读哪一档？", SlotLabels(), slot =>
            {
                if (slot >= SaveSlots) { OpenMenu(); return; }
                var json = PlayerPrefs.GetString(SaveKey(slot), "");
                if (json == "") { Note("第" + SlotNames[slot] + "档是空的。", OpenMenu); return; }
                var s = JsonUtility.FromJson<SaveData>(json);
                party = new Party { Level = s.Level, Exp = s.Exp, Gold = s.Gold, TPBonus = s.TPBonus };
                party.Items.AddRange(s.Items);
                party.Learned.UnionWith(s.Learned);
                foreach (var sm in s.Members) { var g = LoadMember(sm, s.Level); if (g != null) party.Members.Add(g); }
                foreach (var sm in s.Stored) { var g = LoadMember(sm, s.Level); if (g != null) party.Stored.Add(g); }
                party.Strategist = party.Members.Find(m => m.Def.Id == s.Strategist);
                party.LearnByLevel();   // 旧档：升级学的法术以前不记在 Learned 里，这里补上
                party.RecalcMaxTP();
                party.TP = Math.Min(s.TP, party.MaxTP);
                flags = new StoryFlags();
                foreach (var f in s.Flags) flags.Set(f);
                dialogueQueue.Clear();
                currentDialogue = null;
                smokeSteps = 0;
                chapter = MakeChapter(Math.Max(1, s.Chapter)) ?? new Chapter1(this);   // 老档没记章数，就是第一章
                var target = Array.Find(chapter.Maps, m => m.Name == s.Map) ?? chapter.Maps[0];
                Warp(target, s.X, s.Y);
                Note("读档完成。", null);
            });
        }
    }
}
