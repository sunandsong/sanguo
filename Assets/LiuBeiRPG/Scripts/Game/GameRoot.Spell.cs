using System;
using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.Core;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 法术演出：施法动作（引擎 cast_xxx）→ 动作里的 release 事件放出火箭 / 落石 / 药葫芦 / 符纸 → 打中时播序列帧并结算。
    /// 和普通攻击的 strike 一样，结算等到命中那一刻才做。
    /// 序列帧在 Resources/Map/Art/fx/（ArtSrc/fx 切出来的，提示词见 ArtSrc/法术特效提示词.md）。
    /// </summary>
    public partial class GameRoot
    {
        class Spell
        {
            public General Actor;
            public string Id;
            public readonly List<General> Targets = new List<General>();
            public readonly Dictionary<General, List<BattleEvent>> Hits = new Dictionary<General, List<BattleEvent>>();   // 每个目标命中时要结算的战报
            public float Start, End;
            public bool Released;
        }

        /// <summary>一段画在战场上的特效：序列帧或一张实物图，可以从 From 移到 To。</summary>
        class Flip
        {
            public string Sheet;        // 序列帧名（fx/<Sheet>_0..7）
            public Texture2D Prop;      // 或者一张实物图
            public Vector2 From, To;
            public float Start, Dur, Width, PivotY = 0.5f, Spin, FrameDur = 0.05f;
            public bool Loop, Aim, Fall, Fade, Pulse;
            public Color Tint = Color.white;
        }

        Spell spell;
        readonly List<KeyValuePair<float, Action>> spellTimers = new List<KeyValuePair<float, Action>>();
        readonly List<Flip> flips = new List<Flip>();
        float drumBeat = -10f;

        static readonly Dictionary<string, string> CastAnims = new Dictionary<string, string>
        {
            { "huoji", "cast_fire" }, { "luoshi", "cast_summon" }, { "liaobing", "cast_heal" }, { "guwu", "cast_rally" }, { "yaoshu", "cast_hex" },
        };
        // 施法聚光染色
        static readonly Dictionary<string, Color> CastTint = new Dictionary<string, Color>
        {
            { "huoji", new Color(1f, 0.6f, 0.3f) }, { "luoshi", new Color(1f, 0.85f, 0.55f) }, { "liaobing", new Color(0.6f, 1f, 0.6f) },
            { "guwu", new Color(1f, 0.9f, 0.5f) }, { "yaoshu", new Color(0.8f, 0.5f, 1f) },
        };
        // 序列帧的落脚点在格子里的高度（0 顶 1 底），照出图时的构图量的
        static readonly Dictionary<string, float> FxPivot = new Dictionary<string, float>
        {
            { "cast_glow", 0.47f }, { "fire_arrow", 0.44f }, { "fire_burst", 0.88f }, { "rock_dust", 0.84f },
            { "heal_light", 0.86f }, { "rally_wave", 0.77f }, { "hex_storm", 0.84f },
        };

        void ResetSpellFx()
        {
            spell = null;
            spellTimers.Clear();
            flips.Clear();
        }

        /// <summary>“X 施展「火计」！”这条：把后面属于它的战报（各目标的伤害 / 回复 / 识破 / 败走）一起收走，开始演。</summary>
        bool TryStartSpell(BattleEvent head)
        {
            if (head.Actor == null || head.Fx == null || head.Target != null || !slots.ContainsKey(head.Actor)) return false;
            var s = new Spell { Actor = head.Actor, Id = head.Fx, Start = Time.time, End = float.MaxValue };
            while (playQueue.Count > 0 && playQueue.Peek().Actor == null && playQueue.Peek().Target != null)
            {
                var e = playQueue.Dequeue();
                if (!s.Hits.ContainsKey(e.Target)) { s.Hits[e.Target] = new List<BattleEvent>(); s.Targets.Add(e.Target); }
                s.Hits[e.Target].Add(e);
            }
            ApplyEvent(head);   // 战报出字
            spell = s;
            nextEventTime = float.MaxValue;

            PuppetActor a;
            if (battlePuppets.TryGetValue(s.Actor, out a) && a != null)
            {
                string anim;
                a.OnEvent = OnSpellEvent;
                a.Play(CastAnims.TryGetValue(s.Id, out anim) ? anim : "cast", true);
            }
            Color tint;
            if (!CastTint.TryGetValue(s.Id, out tint)) tint = Color.white;
            var hand = HandOf(s.Actor);
            flips.Add(new Flip { Sheet = "cast_glow", From = hand, To = hand, Start = Time.time, Dur = 0.7f, Width = HeightOf(s.Actor) * 0.9f, PivotY = FxPivot["cast_glow"], Tint = tint });
            if (s.Id == "guwu")   // 鼓舞：我方身后浮出一面战鼓，敲到放完
            {
                var drum = PartyFoot() + new Vector2(110f, -UnitH * 0.9f);
                flips.Add(new Flip { Prop = Gfx.Tex("Art/fx/prop_drum"), From = drum, To = drum, Start = Time.time, Dur = 1.8f, Width = UnitH * 1.2f, Fade = true, Pulse = true });
            }
            return true;
        }

        void OnSpellEvent(ClipEvent ev)
        {
            if (spell == null) return;
            if (ev.name == "beat") Beat();
            else if (ev.name == "release" || ev.name == "cast") Release();
        }

        void UpdateSpell()
        {
            if (spell == null) return;
            if (!spell.Released && Time.time - spell.Start > 1.6f) Release();   // 动作里没有 release 事件时兜底
            for (int i = 0; i < spellTimers.Count; i++)
            {
                if (Time.time < spellTimers[i].Key) continue;
                var act = spellTimers[i].Value;
                spellTimers.RemoveAt(i--);
                act();
                if (spell == null) return;
            }
            if (spell.Released && spellTimers.Count == 0 && Time.time >= spell.End) FinishSpell();
        }

        void At(float time, Action act)
        {
            spellTimers.Add(new KeyValuePair<float, Action>(time, act));
            spell.End = Mathf.Max(spell.End, time + 0.6f);   // 只在 Release 里调，那时 End 已经设过
        }

        /// <summary>鼓舞每敲一下：鼓一跳，每个队友脚下扫过一圈金色波纹。</summary>
        void Beat()
        {
            drumBeat = Time.time;
            foreach (var g in battle.Players)
                if (slots.ContainsKey(g) && g.Alive)
                    flips.Add(new Flip { Sheet = "rally_wave", From = slots[g], To = slots[g], Start = Time.time, Dur = 0.5f, Width = HeightOf(g) * 1.8f, PivotY = FxPivot["rally_wave"] });
        }

        void Release()
        {
            if (spell == null || spell.Released) return;
            spell.Released = true;
            float now = Time.time;
            var hand = HandOf(spell.Actor);
            spell.End = now + 0.4f;
            for (int i = 0; i < spell.Targets.Count; i++)
            {
                var tg = spell.Targets[i];
                if (!slots.ContainsKey(tg)) { Hit(tg); continue; }
                var foot = slots[tg];
                float h = HeightOf(tg);
                var chest = foot + new Vector2(0f, -h * 0.5f);
                switch (spell.Id)
                {
                    case "huoji":   // 火箭飞过去，炸开一团火
                        flips.Add(new Flip { Sheet = "fire_arrow", From = hand, To = chest, Start = now, Dur = 0.35f, Width = h * 1.1f, PivotY = FxPivot["fire_arrow"], Loop = true, Aim = true });
                        At(now + 0.35f, () => { AddFx("fire_burst", foot, 0.6f, h * 1.6f); Hit(tg); });
                        break;
                    case "luoshi":  // 石头从天上翻滚着砸下来，一个接一个
                    {
                        float d = i * 0.12f;
                        var from = foot + new Vector2(Rnd(-20f, 20f), -h - 260f);
                        flips.Add(new Flip { Prop = Gfx.Tex(i % 2 == 0 ? "Art/fx/prop_rock_big" : "Art/fx/prop_rock_small"), From = from, To = foot + new Vector2(0f, -h * 0.35f),
                            Start = now + d, Dur = 0.38f, Width = h * 0.7f, Spin = 540f, Fall = true });
                        At(now + d + 0.38f, () => { AddFx("rock_dust", foot, 0.7f, h * 1.9f); shakeUntil = Time.time + 0.25f; shakeAmp = 6f; Hit(tg); });
                        break;
                    }
                    case "liaobing":   // 头顶浮出药葫芦，倒下来一道治疗光
                        flips.Add(new Flip { Prop = Gfx.Tex("Art/fx/prop_gourd"), From = foot + new Vector2(0f, -h - 30f), To = foot + new Vector2(0f, -h - 40f), Start = now, Dur = 0.8f, Width = h * 0.55f, Fade = true });
                        At(now + 0.35f, () => { AddFx("heal_light", foot, 0.8f, h * 1.3f); Hit(tg); });
                        break;
                    case "guwu":
                        At(now + i * 0.06f, () => { AddFx("heal_light", foot, 0.8f, h * 1.3f); Hit(tg); });
                        break;
                    case "yaoshu":  // 符纸飞过去，紫雾里劈下闪电
                        flips.Add(new Flip { Prop = Gfx.Tex("Art/fx/prop_talisman"), From = hand, To = chest, Start = now, Dur = 0.4f, Width = h * 0.45f, Spin = 360f });
                        At(now + 0.4f, () => { AddFx("hex_storm", foot, 0.8f, h * 1.8f); Hit(tg); });
                        break;
                    default:
                        Hit(tg);
                        break;
                }
            }
        }

        void AddFx(string sheet, Vector2 foot, float dur, float width)
        {
            flips.Add(new Flip { Sheet = sheet, From = foot, To = foot, Start = Time.time, Dur = dur, Width = width, PivotY = FxPivot[sheet] });
        }

        void Hit(General tg)
        {
            List<BattleEvent> evs;
            if (spell == null || !spell.Hits.TryGetValue(tg, out evs)) return;
            spell.Hits.Remove(tg);
            foreach (var e in evs) ApplyEvent(e);
        }

        /// <summary>演完（或者点战场快进）：没结算的全结算掉，施法的人回待机。</summary>
        void FinishSpell()
        {
            foreach (var tg in spell.Targets) Hit(tg);
            spellTimers.Clear();
            PuppetActor a;
            if (battlePuppets.TryGetValue(spell.Actor, out a) && a != null) { a.OnEvent = null; a.Play("idle"); }
            spell = null;
            nextEventTime = Time.time + 0.35f;
        }

        Vector2 HandOf(General g)
        {
            float h = HeightOf(g), dir = battle.Players.Contains(g) ? -1f : 1f;   // 我方朝左、敌人朝右
            return slots[g] + new Vector2(dir * h * 0.3f, -h * 0.6f);
        }

        Vector2 PartyFoot()
        {
            Vector2 sum = Vector2.zero;
            int n = 0;
            foreach (var g in battle.Players) if (slots.ContainsKey(g)) { sum += slots[g]; n++; }
            return n > 0 ? sum / n : new Vector2(Gfx.VW * 0.6f, Gfx.VH * 0.5f);
        }

        void DrawFlips()
        {
            flips.RemoveAll(f => Time.time > f.Start + f.Dur);
            if (Event.current.type != EventType.Repaint) return;
            var oldC = GUI.color;
            var oldM = GUI.matrix;
            foreach (var f in flips)
            {
                float t = Time.time - f.Start;
                if (t < 0f) continue;
                float k = t / f.Dur;
                var tex = f.Prop != null ? f.Prop
                    : Gfx.Tex("Art/fx/" + f.Sheet + "_" + (f.Loop ? (int)(t / f.FrameDur) % 8 : Mathf.Min(7, (int)(k * 8f))));
                if (tex == null) continue;
                var p = Vector2.Lerp(f.From, f.To, f.Fall ? k * k : k);
                float w = f.Width;
                if (f.Pulse) w *= 1f + 0.15f * Mathf.Max(0f, 1f - (Time.time - drumBeat) / 0.15f);
                float h = w * tex.height / tex.width;
                var r = new Rect(p.x - w * 0.5f, p.y - h * f.PivotY, w, h);

                // 火箭朝着目标：往左飞就水平翻过来再转，火焰不会倒挂
                float ang = f.Spin * t, sx = 1f;
                if (f.Aim)
                {
                    var d = f.To - f.From;
                    sx = d.x < 0f ? -1f : 1f;
                    ang = Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg * sx;
                }
                GUI.matrix = oldM * Matrix4x4.TRS(p, Quaternion.Euler(0, 0, ang), new Vector3(sx, 1f, 1f)) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
                var c = f.Tint;
                if (f.Fade) c.a *= Mathf.Clamp01(Mathf.Min(t / 0.15f, (f.Dur - t) / 0.2f));
                GUI.color = c;
                Gfx.Image(r, tex);
            }
            GUI.matrix = oldM;
            GUI.color = oldC;
        }
    }
}
