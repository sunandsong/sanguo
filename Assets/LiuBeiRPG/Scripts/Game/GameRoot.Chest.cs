using System;
using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using LiuBeiRPG.World;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 开宝箱：箱子先抖几下、缝里漏金光 → 盖子弹开（换开着的图），一闪 + 火星往上喷，顶上滑下来一个袋子 →
    /// 东西和战斗结算一样在屏幕中间亮相、碎开、飞进袋子（Art/icon_bag，缺图就只飞不画袋子）→ 下方弹「获得××」小提示（自己消失），提示消失袋子才收。
    /// 演出期间不能走、不吃按键。
    /// </summary>
    public partial class GameRoot
    {
        class ChestFx
        {
            public Npc N;
            public float T0;
            public List<ResultLoot> Loot;
            public bool Opened, Finished;   // 盖子开了 / 演完了、对话出来了
            public string Message;
        }
        ChestFx chestFx;

        struct Ember { public Vector2 P, V; public float T0, Life, Size; }
        readonly List<Ember> embers = new List<Ember>();

        const float ChestShake = 0.5f;   // 抖多久开盖

        public void OpenChest(Npc chest, string item, int count, int gold, string message)
        {
            var loot = new List<ResultLoot>();
            if (item != null) loot.Add(new ResultLoot { Item = item, Total = count, Bits = 1, Tint = new Color(1f, 0.6f, 0.95f) });   // 物品整个飞进去，不碎；只有金钱碎成好几个
            if (gold > 0) loot.Add(new ResultLoot { Icon = "Art/icon_gold", Total = gold, Bits = Mathf.Clamp(gold, 1, 8), Tint = Accent });
            ScheduleLoot(loot, 0.25f);   // 开盖一闪之后再亮相
            foreach (var l in loot) l.Target = BagRect();
            chestFx = new ChestFx { N = chest, T0 = Time.time, Loot = loot, Message = message };
            Sfx.Play("chest_shake");
        }

        /// <summary>演到什么时候出对话：最后一个小图标落进袋子再停一下。</summary>
        float ChestEnd { get { float e = 0.6f; foreach (var l in chestFx.Loot) e = Mathf.Max(e, l.Land[l.Bits - 1] + 0.3f); return ChestShake + e; } }

        bool ChestPlaying { get { return chestFx != null && !chestFx.Finished; } }

        void UpdateChest()
        {
            if (chestFx == null) return;
            float t = Time.time - chestFx.T0;
            LootSfx(chestFx.Loot, t - Time.deltaTime - ChestShake, t - ChestShake);
            if (!chestFx.Opened && t >= ChestShake)
            {
                chestFx.Opened = true;
                chestFx.N.Sprite = "Art/chest_open";
                Sfx.Play("chest_open");
                for (int i = 0; i < 22; i++)
                {
                    float a = UnityEngine.Random.Range(-2.4f, -0.75f);   // 朝上的一个扇面
                    float sp = UnityEngine.Random.Range(2.5f, 6f);
                    embers.Add(new Ember { P = Vector2.zero, V = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, T0 = Time.time,
                        Life = UnityEngine.Random.Range(0.5f, 1f), Size = UnityEngine.Random.Range(0.12f, 0.26f) });
                }
            }
            if (!chestFx.Finished && t >= ChestEnd)
            {
                chestFx.Finished = true;
                Note(chestFx.Message, null);   // 下方小提示，自己消失；这时已经能走了
            }
            else if (chestFx.Finished && noteText == null) chestFx = null;   // 提示消失袋子才收
        }

        /// <summary>箱子图怎么变形：抖的时候左右晃、往下压；开盖那一下往上一蹿再落回。</summary>
        Rect ChestRect(Npc n, Rect r)
        {
            if (chestFx == null || chestFx.N != n) return r;
            float t = Time.time - chestFx.T0, sx = 1, sy = 1, dx = 0;
            if (t < ChestShake)
            {
                float k = t / ChestShake;
                dx = Mathf.Sin(t * 70f) * 0.045f * k;
                sy = 1 - 0.08f * k; sx = 1 + 0.05f * k;
            }
            else
            {
                float k = Mathf.Clamp01((t - ChestShake) / 0.3f);
                float pop = Mathf.Sin(k * Mathf.PI) * (1 - k * 0.5f);
                sy = 1 + 0.18f * pop; sx = 1 - 0.08f * pop;
            }
            float w = r.width * sx, h = r.height * sy;
            return new Rect(r.center.x - w / 2 + dx, r.yMax - h, w, h);
        }

        /// <summary>光、火星画在箱子上；东西走战斗结算那套（屏幕中间亮相 → 碎开飞进顶上的袋子）。</summary>
        void DrawChestFx()
        {
            if (chestFx == null || map == null) return;
            var n = chestFx.N;
            float t = Time.time - chestFx.T0;
            var mouth = origin + new Vector2((n.X + 0.5f) * tile, (n.Y + 0.95f - n.Size * 0.55f) * tile);   // 箱口
            var oldC = GUI.color;

            if (t < ChestShake)
            {
                // 缝里漏光，越抖越亮
                float k = t / ChestShake;
                GUI.color = new Color(1f, 0.82f, 0.35f, 0.8f * k);
                float s = tile * (0.5f + 0.9f * k);
                Gfx.Image(new Rect(mouth.x - s, mouth.y - s * 0.35f, s * 2, s * 0.7f), GlowTex());
                GUI.color = oldC;
                return;
            }

            float o = t - ChestShake;
            // 开盖一闪
            float flash = 1 - Mathf.Clamp01(o / 0.35f);
            if (flash > 0)
            {
                GUI.color = new Color(1f, 0.95f, 0.75f, flash);
                float s = tile * (1.2f + 1.6f * (1 - flash));
                Gfx.Image(new Rect(mouth.x - s, mouth.y - s, s * 2, s * 2), GlowTex());
            }
            // 火星：往上喷、受重力落下、越来越小
            embers.RemoveAll(e => Time.time - e.T0 > e.Life);
            foreach (var e in embers)
            {
                float a = Time.time - e.T0, k = a / e.Life;
                var p = mouth + (e.V * a + new Vector2(0, 7f) * a * a * 0.5f) * tile;
                float s = e.Size * tile * (1 - k * 0.6f);
                GUI.color = new Color(1f, 0.85f, 0.4f, 1 - k);
                Gfx.Image(new Rect(p.x - s / 2, p.y - s / 2, s, s), StarTex());
            }
            GUI.color = oldC;

            // 顶上的袋子：第一个小图标快飞到时才从上面滑下来，刚停稳东西就落进去；落进去就压扁一下、左右晃几下慢慢停住，提示消失前一直在
            float appear = 9;
            foreach (var l in chestFx.Loot) appear = Mathf.Min(appear, l.Land[0] - 0.25f);
            if (o >= appear)
            {
                float since = 9;
                foreach (var l in chestFx.Loot) { float a = o - l.LastLand(o); if (a >= 0) since = Mathf.Min(since, a); }
                DrawPouch(1 - BackEase(Mathf.Clamp01((o - appear) / 0.25f)), since);
            }

            DrawLootShow(chestFx.Loot, new Vector2(Gfx.VW / 2f, Gfx.VH * 0.45f), Mathf.Min(Gfx.VW - 40, 700), o);
        }

        /// <summary>右上角的袋子（开宝箱、买东西共用）。slide：还差几成滑到位（1 = 在屏幕外）；since：离上次有东西落进去几秒。
        /// 落进去就压扁一下、蹦一下、左右晃几下慢慢停住。</summary>
        void DrawPouch(float slide, float since)
        {
            var bag = BagRect();
            float shake = Mathf.Exp(-since * 4f);
            float sq = Mathf.Sin(since * 30f) * shake;
            float w = bag.width * (1 + 0.25f * sq), h = bag.height * (1 - 0.25f * sq);
            float hop = Mathf.Max(0, Mathf.Sin(since * 20f)) * 8f * shake;   // 被砸得往上一蹦
            var br = new Rect(bag.center.x - w / 2, bag.yMax - h - slide * 160 - hop, w, h);   // 底边不动，往下压
            var bagTex = Gfx.Tex("Art/icon_bag");
            if (bagTex != null) RotatedImage(br, bagTex, Mathf.Sin(since * 24f) * 25f * shake, Color.white);
        }

        static Rect BagRect() { return new Rect(Gfx.VW - 8 - 48, 14, 48, 48); }   // 右上角，和左上角地名一样高（地名框 y 8～68，中线 38）
    }
}
