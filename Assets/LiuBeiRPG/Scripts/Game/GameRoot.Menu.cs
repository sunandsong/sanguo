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
        int menuPage;      // 0 = 没开，PageMain = 菜单首页，PagePeople = 人物（装备 + 物品），PageShop = 商店
        int pageMember;    // 人物页正在看第几个人
        int mainSel;       // 首页选中的按钮
        const int PagePeople = 1, PageMain = 3, PageShop = 4;
        const int ItemSlots = Party.BagSlots;

        static readonly string[] MenuItems = { "人物 · 物品 · 法术", "存档", "读档" };

        void OpenMenu() { menuPage = PageMain; }

        void MainPick(int i)
        {
            mainSel = i;
            switch (i)
            {
                case 0: menuPage = PagePeople; pageMember = 0; itemSel = 0; tacticSel = 0; break;
                case 1: SaveGame(); break;                  // 提示框叠在菜单上，看完还在菜单
                case 2: menuPage = 0; LoadGame(); break;
            }
        }

        void MenuPageKey(InputKey key)
        {
            if (menuPage == PageShop) { ShopKey(key); return; }
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
            else if (d != 0 && bagTab == 0) itemSel = Mathf.Clamp(itemSel + d, 0, ItemSlots - 1);
            else if (d != 0) tacticSel = Mathf.Clamp(tacticSel + d, 0, Mathf.Max(0, party.AvailableTactics().Count - 1));
            else if (key == InputKey.Confirm && bagTab == 0 && itemSel < party.Slots().Count) UseItem();
            else if (key == InputKey.Confirm && bagTab == 1 && tacticSel < party.AvailableTactics().Count) UseTactic(party.AvailableTactics()[tacticSel]);
            else if (key == InputKey.Cancel) CloseMenuPage();
        }

        /// <summary>人物页返回首页，首页、商店返回地图。</summary>
        void CloseMenuPage() { menuPage = menuPage == PagePeople ? PageMain : 0; }

        void DrawMenuPage()
        {
            if (menuPage == 0) return;
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), new Color(0, 0, 0, 0.45f));
            if (menuPage == PageMain) DrawMainPage();
            else if (menuPage == PageShop) DrawShop();
            else DrawPeoplePage();
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
            float y = foot.y + 40;
            foreach (var m in party.Members)
            {
                Gfx.Text(new Rect(r.x + 24, y, 60, 26), m.Name, 18, m.Alive ? Color.white : Color.gray);
                Gfx.Bar(new Rect(r.x + 84, y + 8, lw - 160, 10), m.MaxSoldiers > 0 ? (float)m.Soldiers / m.MaxSoldiers : 0, new Color(0.4f, 0.8f, 0.45f));
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
            // 右边两个页签：物品 / 策略
            string[] tabs = { "物品", "法术" };
            for (int i = 0; i < 2; i++)
            {
                var tr = new Rect(bag.x + 16 + i * 104, bag.y, 96, 40);
                bool sel = i == bagTab;
                Gfx.RoundRect(tr, sel ? Accent : new Color(1, 1, 1, 0.08f));
                Gfx.Text(tr, tabs[i], 20, sel ? new Color(0.12f, 0.09f, 0.04f) : Color.white, TextAnchor.MiddleCenter, false, !sel);
                if (Gfx.Clicked(tr)) bagTab = i;
            }
            var content = new Rect(bag.x, bag.y + 52, bag.width, bag.height - 52);
            if (bagTab == 0) DrawBag(content); else DrawTactics(content, party.Members[pageMember]);
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

            string[] labels = { "武力", "智力", "防御力", "速度" };
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
            string[] slots = { "武器", "头盔", "防具", "鞋子", "坐骑", "饰品" }, stats = { "武力", "防御力", "防御力", "速度", "速度", "智力" };
            const float under = 52;   // 格子下面两行字：名字、加成
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
                Gfx.Text(new Rect(sr.x - 10, sr.yMax + 6, sr.width + 20, 22), item != null ? item.Name : "空", 15, item != null ? Color.white : new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter);
                if (item == null) continue;
                Gfx.Text(new Rect(sr.x - 10, sr.yMax + 28, sr.width + 20, 20), stats[i] + " +" + item.Power, 14, Accent, TextAnchor.MiddleCenter);
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
            string msg = party.UseItem(party.Slots()[itemSel], party.Members[pageMember]);
            if (msg != null) Note(msg, null);   // 对话框叠在人物页上，看完还停在这页
        }

        /// <summary>行囊按钮上的字：兵书研读、药酒使用、装备给左边这个人换上。</summary>
        string UseLabel(ItemDef item)
        {
            if (item.Kind == ItemKind.Book) return "研读";
            if (item.Equip) return "装备";
            return item.Kind == ItemKind.Heal ? "给" + party.Members[pageMember].Name : "使用";
        }

        /// <summary>行囊：6 列格子，同种叠一格，下面是选中物品的说明和使用按钮。</summary>
        void DrawBag(Rect area)
        {
            const int cols = 6;
            int rows = (ItemSlots + cols - 1) / cols;
            float x0 = area.x + 16, gw = area.width - 32;
            // 格子按宽、高都放得下来算：上面 32 标题，下面留 84 给说明和“研读”
            float cell = Mathf.Min(72, Mathf.Min((gw - (cols - 1) * 8) / cols, (area.height - 32 - 84) / rows - 8));
            float gx = (gw - cols * cell) / (cols - 1);   // 格子变小时横向间距撑开，左右都贴齐，右边不留空
            var slotsList = party.Slots();
            Gfx.Text(new Rect(x0, area.y, gw, 26), "物品　" + slotsList.Count + "/" + ItemSlots, 18, new Color(1, 1, 1, 0.6f));
            float gy = area.y + 32;
            for (int i = 0; i < ItemSlots; i++)
            {
                var cr = new Rect(x0 + (i % cols) * (cell + gx), gy + (i / cols) * (cell + 8), cell, cell);
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
            Gfx.RoundRect(use, Accent);
            Gfx.Text(use, UseLabel(item), 20, new Color(0.12f, 0.09f, 0.04f), TextAnchor.MiddleCenter, false, false);
            if (Gfx.Clicked(use)) UseItem();
        }

        // ---------------- 策略：地图上只能用恢复类 ----------------

        int bagTab;      // 人物页右边：0 = 物品，1 = 策略
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

        void UseTactic(TacticDef t)
        {
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
            var lines = new List<DialogueLine> { new DialogueLine(null, caster.Name + "使用了" + t.Name + "。") };
            foreach (var g in targets)
                if (g.Alive) lines.Add(new DialogueLine(null, g.Name + " 兵力恢复 " + g.Heal(Formula.TacticHeal(caster, t, rng))));
            Say(lines);   // 对话框叠在人物页上，看完还停在这页
        }

        void Note(string text, Action then) { Say(new List<DialogueLine> { new DialogueLine(null, text) }, then); }

        // ---------------- 存档：一个档位，JSON 放 PlayerPrefs ----------------
        // 剧情进度全在 flags 里，地图上的人和东西都由 flags 推出来，所以只存队伍（含物品、兵书学的策略）+ flags + 位置

        const string SaveKey = "save1";

        [Serializable] class SavedMember { public string Id; public string[] Gear; public int Soldiers, MaxSoldiers, WeaponBonus; }   // WeaponBonus：旧档用
        [Serializable] class SaveData
        {
            public int Level, Exp, Gold, TP;
            public string Strategist;
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
                s.Members.Add(new SavedMember { Id = m.Def.Id, Soldiers = m.Soldiers, MaxSoldiers = m.MaxSoldiers, Gear = m.Gear });
            s.Flags.AddRange(flags.All);
            s.Items.AddRange(party.Items);
            s.Learned.AddRange(party.Learned);
            s.Strategist = party.Strategist.Def.Id;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
            Note("已存档。", null);
        }

        static string Nul(string s) { return string.IsNullOrEmpty(s) ? null : s; }   // JsonUtility 把 null 存成 ""

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
                if (def == null) continue;
                var g = new General(def) { Soldiers = sm.Soldiers, MaxSoldiers = sm.MaxSoldiers };
                for (int i = 0; sm.Gear != null && i < sm.Gear.Length && i < General.GearSlots; i++) g.Gear[i] = Nul(sm.Gear[i]);
                if (g.Weapon == null && sm.WeaponBonus > 0) g.Weapon = ItemDb.Signature(def.Id).Id;   // 旧档：受赠过就是专属兵器
                party.Members.Add(g);
            }
            party.Strategist = party.Members.Find(m => m.Def.Id == s.Strategist);
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
