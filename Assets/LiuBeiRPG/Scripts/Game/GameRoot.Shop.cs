using UnityEngine;
using LiuBeiRPG.Core;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 商店（张世平）：上面 买 / 卖 两个页签，中间一格一个货，下面写名字、说明、价钱和大按钮。
    /// 买页列这家店卖的东西，卖页列行囊（剧情给的不能卖）。
    /// </summary>
    public partial class GameRoot
    {
        string[] shopGoods = new string[0];
        int shopTab;   // 0 = 买，1 = 卖
        int shopSel;
        const int ShopCols = 6;

        public void OpenShop(string[] goods)
        {
            shopGoods = goods;
            shopTab = 0;
            shopSel = 0;
            shopFlies.Clear();
            shopOpenedAt = Time.time;
            menuPage = PageShop;
        }

        string[] ShopList() { return shopTab == 0 ? shopGoods : party.Slots().ToArray(); }

        void ShopKey(InputKey key)
        {
            int n = ShopList().Length;
            if (key == InputKey.Left) shopSel--;
            else if (key == InputKey.Right) shopSel++;
            else if (key == InputKey.Up) shopSel -= ShopCols;
            else if (key == InputKey.Down) shopSel += ShopCols;
            else if (key == InputKey.Confirm && shopSel < n) ShopDeal(ShopList()[shopSel]);
            else if (key == InputKey.Cancel) CloseMenuPage();
            shopSel = Mathf.Clamp(shopSel, 0, Mathf.Max(0, n - 1));
        }

        // 买下的东西从格子飞进右上角的袋子：一件一个
        struct ShopFly { public string Id; public Vector2 From; public float T0; }
        readonly System.Collections.Generic.List<ShopFly> shopFlies = new System.Collections.Generic.List<ShopFly>();
        float shopLanded = -9, shopOpenedAt;   // 上次落进袋子的时刻；开店时刻（袋子滑下来）
        Rect shopCellRect;                      // 选中那一格在哪（飞的起点）
        const float ShopFlyTime = 0.5f;

        void ShopDeal(string id)
        {
            if (shopTab == 0)
            {
                int had = party.Count(id);
                string msg = party.Buy(id);
                if (party.Count(id) > had) { shopFlies.Add(new ShopFly { Id = id, From = shopCellRect.center, T0 = Time.time }); Sfx.Play("buy"); }   // 买成了就飞，不弹字
                else { Note(msg, null); Sfx.Play("ui_error"); }
                return;
            }
            int gold = party.Gold;
            Note(party.Sell(id), null);
            Sfx.Play(party.Gold > gold ? "sell" : "ui_error");
        }

        void DrawShopFlies()
        {
            DrawPouch(1 - BackEase(Mathf.Clamp01((Time.time - shopOpenedAt) / 0.3f)), Time.time - shopLanded);
            var to = BagRect().center;
            var old = GUI.color;
            for (int i = shopFlies.Count - 1; i >= 0; i--)
            {
                var f = shopFlies[i];
                float u = (Time.time - f.T0) / ShopFlyTime;
                if (u >= 1) { shopFlies.RemoveAt(i); shopLanded = Time.time; Sfx.Play("item_get"); continue; }
                u *= u;   // 加速冲进去
                var dir = to - f.From;
                var ctrl = (f.From + to) * 0.5f + new Vector2(-dir.y, dir.x).normalized * 80f;   // 往外拱一道弧
                var p = (1 - u) * (1 - u) * f.From + 2 * (1 - u) * u * ctrl + u * u * to;
                float s = Mathf.Lerp(shopCellRect.width * 0.5f, BagRect().width * 0.3f, u);
                GUI.color = new Color(1f, 0.85f, 0.45f, 0.6f);
                Gfx.Image(new Rect(p.x - s * 1.6f, p.y - s * 1.6f, s * 3.2f, s * 3.2f), GlowTex());
                GUI.color = old;
                ItemIcon(new Rect(p.x - s, p.y - s, s * 2, s * 2), ItemDb.Get(f.Id));
            }
        }

        void DrawShop()
        {
            float w = Mathf.Min(Gfx.VW - 24, 720), h = Mathf.Min(Gfx.VH - 24, 500);
            var r = PagePanel(w, h);
            if (menuPage != PageShop) return;   // 刚点了返回

            string[] tabs = { "买", "卖" };
            for (int i = 0; i < 2; i++)
            {
                var tr = new Rect(r.x + 16 + i * 96, r.y + 12, 88, 44);
                bool sel = i == shopTab;
                Gfx.RoundRect(tr, sel ? Accent : new Color(1, 1, 1, 0.08f));
                Gfx.Text(tr, tabs[i], 22, sel ? new Color(0.12f, 0.09f, 0.04f) : Color.white, TextAnchor.MiddleCenter, false, !sel);
                if (Gfx.Clicked(tr)) { shopTab = i; shopSel = 0; }
            }
            Gfx.Text(new Rect(r.x + 216, r.y + 12, r.width - 216 - 124, 44), "金 " + party.Gold, 22, Accent, TextAnchor.MiddleRight);

            var list = ShopList();
            shopSel = Mathf.Clamp(shopSel, 0, Mathf.Max(0, list.Length - 1));
            float x0 = r.x + 20, gw = r.width - 40, gy = r.y + 72;
            int rows = Mathf.Max(1, (list.Length + ShopCols - 1) / ShopCols);
            // 和行囊一样：宽、高都放得下，左右贴齐；下面留 110 给说明和按钮
            float cell = Mathf.Min(84, Mathf.Min((gw - (ShopCols - 1) * 10) / ShopCols, (r.yMax - gy - 110) / rows - 10));
            float gx = (gw - ShopCols * cell) / (ShopCols - 1);
            for (int i = 0; i < list.Length; i++)
            {
                var cr = new Rect(x0 + (i % ShopCols) * (cell + gx), gy + (i / ShopCols) * (cell + 10), cell, cell);
                var item = ItemDb.Get(list[i]);
                bool dim = shopTab == 0 ? party.Gold < item.Price : item.Price <= 0;
                Gfx.RoundRect(cr, new Color(0.55f, 0.42f, 0.25f, dim ? 0.2f : 0.5f));
                Gfx.Frame(cr, i == shopSel ? Accent : new Color(1, 1, 1, 0.1f), i == shopSel ? 2 : 1);
                var old = GUI.color;
                if (dim) GUI.color = new Color(1, 1, 1, 0.4f);
                ItemIcon(cr, item);
                GUI.color = old;
                if (shopTab == 1) StackCount(cr, list[i]);
                if (Gfx.Clicked(cr)) shopSel = i;
                if (i == shopSel) shopCellRect = cr;
            }
            if (list.Length == 0) return;

            // 底下：名字、价钱、说明、持有几件；右边大按钮
            var cur = ItemDb.Get(list[shopSel]);
            float y = r.yMax - 104;
            Gfx.Rect(new Rect(r.x + 20, y - 8, r.width - 40, 1), new Color(1, 1, 1, 0.1f));
            var btn = new Rect(r.xMax - 20 - 140, y + 18, 140, 60);
            float tw = btn.x - x0 - 16;
            int price = shopTab == 0 ? cur.Price : party.SellPrice(cur.Id);
            Gfx.Text(new Rect(x0, y, tw, 28), cur.Name + "　" + (cur.Price > 0 ? price + " 金" : "不能卖"), 21, Accent);
            Gfx.Text(new Rect(x0, y, tw, 28), "持有 " + party.Count(cur.Id), 16, new Color(1, 1, 1, 0.55f), TextAnchor.MiddleRight);
            Gfx.Text(new Rect(x0, y + 32, tw, 60), cur.Desc, 16, new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft, true);
            bool ok = shopTab == 0 ? party.Gold >= cur.Price : cur.Price > 0;
            Gfx.RoundRect(btn, ok ? Accent : new Color(1, 1, 1, 0.1f));
            Gfx.Text(btn, shopTab == 0 ? "买" : "卖", 26, ok ? new Color(0.12f, 0.09f, 0.04f) : new Color(1, 1, 1, 0.4f), TextAnchor.MiddleCenter, false, false);
            if (Gfx.Clicked(btn)) ShopDeal(cur.Id);
            if (shopTab == 0) DrawShopFlies();
        }
    }
}
