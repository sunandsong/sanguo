using System.Collections.Generic;
using UnityEngine;
using LiuBeiRPG.World;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 地图的“活气”：一阵一阵的风（树、草、旗子跟着摆）、水面波纹、小动物、涟漪、炊烟和篝火。
    /// 坐标都用格子坐标（y 向下，脚底点），画的时候交给 World。
    /// 小动物的图：Resources/Map/Art/life/{种类}_{动作}.png，每个动作一张横排序列帧（见 ArtSrc/地图生命感提示词.md）。
    /// 缺图的种类不生成，不画字顶替。
    /// </summary>
    public partial class GameRoot
    {
        float wind;   // 0 = 无风，1 = 一阵大风

        void UpdateWind()
        {
            float n = Mathf.PerlinNoise(Time.time * 0.13f, 0.7f);
            wind = Mathf.Max(Mathf.SmoothStep(0, 1, Mathf.Clamp01((n - 0.3f) * 2.2f)), rain * 0.8f);   // 下雨时风大
            Shader.SetGlobalFloat("_Wind", wind);
        }

        // ---------------- 物件 ----------------

        /// <summary>
        /// 读物件清单：一行一件「贴图 X Y 宽 [动法] [键=值…]」，# 后面是注释。
        /// 贴图不带 / 时在 Art/life/ 下；动法 plant / hang / flag；ground = 贴地画（桥）；键：amp speed shadow frames fps h lift。
        /// ArtSrc/life/preview.py 读同一份文件出静态预览。
        /// </summary>
        static void LoadDecor(MapData m)
        {
            if (m.DecorFile == null || m.Decor.Count > 0) return;
            var txt = Resources.Load<TextAsset>("Map/Decor/" + m.DecorFile);
            if (txt == null) return;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var raw in txt.text.Split('\n'))
            {
                var line = raw.Split('#')[0].Split(new[] { ' ', '\t', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (line.Length < 4) continue;
                var d = new MapDecor
                {
                    Sprite = line[0].Contains("/") ? line[0] : "Art/life/" + line[0],
                    X = float.Parse(line[1], inv), Y = float.Parse(line[2], inv), W = float.Parse(line[3], inv),
                };
                float shadow = -1;
                for (int i = 4; i < line.Length; i++)
                {
                    var kv = line[i].Split('=');
                    if (kv[0] == "ground") { d.Ground = true; continue; }
                    if (kv.Length == 1)
                    {
                        d.Motion = kv[0] == "plant" ? LiuBeiRPG.World.Motion.Plant : kv[0] == "hang" ? LiuBeiRPG.World.Motion.Hang
                                 : kv[0] == "flag" ? LiuBeiRPG.World.Motion.Flag : LiuBeiRPG.World.Motion.None;
                        continue;
                    }
                    float v = float.Parse(kv[1], inv);
                    switch (kv[0])
                    {
                        case "amp": d.Amp = v; break;
                        case "speed": d.Speed = v; break;
                        case "shadow": shadow = v; break;
                        case "frames": d.Frames = (int)v; break;
                        case "fps": d.Fps = v; break;
                        case "h": d.H = v; break;
                        case "lift": d.Lift = v; break;
                    }
                }
                d.Shadow = shadow >= 0 ? shadow : d.Motion == LiuBeiRPG.World.Motion.Plant ? 0.7f : 0;
                m.Decor.Add(d);
            }
        }

        void DrawDecor(MapDecor d)
        {
            var tex = Gfx.Tex(d.Sprite);
            if (tex == null) return;   // 缺图就空着
            float h = d.H > 0 ? d.H : d.W * tex.height / (tex.width / (float)d.Frames);
            var r = new Rect(d.X - d.W * 0.5f, d.Y - h - d.Lift, d.W, h);
            if (d.Shadow > 0)
            {
                float sw = d.W * d.Shadow;
                World.Quad(ShadowTex(), new Rect(d.X - sw * 0.5f, d.Y - sw * 0.12f, sw, sw * 0.24f), Color.white);
            }
            if (d.Frames > 1) { World.Frame(tex, r, (int)(Time.time * d.Fps) % d.Frames, d.Frames, false, Color.white); return; }
            // 相位按位置错开，一排树不会齐刷刷一起摆
            var fx = d.Motion == LiuBeiRPG.World.Motion.None ? Vector4.zero : new Vector4(d.Amp, d.Speed, d.X * 1.7f + d.Y * 0.9f, (int)d.Motion);
            World.Quad(tex, r, Color.white, null, fx, null, d.Push);
        }

        /// <summary>草丛、芦苇、灌木：队伍里有人走到跟前就往两边倒，走开再慢慢弹回来。</summary>
        void UpdatePush(float dt)
        {
            foreach (var d in map.Decor)
            {
                if (d.Motion != LiuBeiRPG.World.Motion.Plant || d.W >= 1.3f) continue;
                float r = Mathf.Max(0.6f, d.W * 0.6f), target = 0;
                foreach (var f in PartyFeet())
                {
                    var dv = new Vector2(d.X - f.x, (d.Y - f.y) * 1.5f);
                    float dist = dv.magnitude;
                    if (dist < r) { float k = (1 - dist / r) * (dv.x >= 0 ? 1 : -1); if (Mathf.Abs(k) > Mathf.Abs(target)) target = k; }
                }
                d.Push = Mathf.Lerp(d.Push, target, 1 - Mathf.Exp(-dt * (target != 0 ? 12f : 4f)));
            }
        }

        /// <summary>队伍每个人的脚底（格子坐标）。</summary>
        System.Collections.Generic.IEnumerable<Vector2> PartyFeet()
        {
            yield return LeaderFoot();
            for (int i = 0; i < trail.Count && i + 1 < party.Members.Count; i++) yield return Slide(prevTrail[i], trail[i]) + new Vector2(0.5f, 0.92f);
        }

        // ---------------- 扬尘、落叶 ----------------

        struct Puff { public Vector2 P, V; public float T0, Life, Size, Spin; public Color C; public bool Leaf; }
        readonly List<Puff> puffs = new List<Puff>();
        float seenStep;

        /// <summary>每走一步：土路上扬一小团尘，桃园里踢起几片花瓣。</summary>
        void StepDust()
        {
            if (lastStepTime == seenStep) return;
            seenStep = lastStepTime;
            var cells = new List<Vector2Int> { new Vector2Int(px, py) };
            for (int i = 0; i < trail.Count && i + 1 < party.Members.Count; i++) cells.Add(trail[i]);
            foreach (var c in cells)
            {
                char ch = map.At(c.x, c.y);
                var foot = new Vector2(c.x + 0.5f, c.y + 0.9f);
                if (ch == '=' || ch == 'g' || ch == 't')
                    for (int k = 0; k < 2; k++)
                        puffs.Add(new Puff { P = foot + new Vector2(Rand(-0.15f, 0.15f), 0), V = new Vector2(Rand(-0.25f, 0.25f), -Rand(0.15f, 0.3f)), T0 = Time.time, Life = 0.7f, Size = Rand(0.22f, 0.32f), C = new Color(0.82f, 0.7f, 0.5f, 0.45f) });
                else if (ch == 'f')
                    for (int k = 0; k < 2; k++)
                        Leaf(foot + new Vector2(Rand(-0.2f, 0.2f), -0.05f), new Vector2(Rand(-0.6f, 0.6f), -Rand(0.6f, 1f)), new Color(1f, 0.78f, 0.85f), 0.9f);
            }
        }

        void Leaf(Vector2 p, Vector2 v, Color c, float life)
        {
            puffs.Add(new Puff { P = p, V = v, T0 = Time.time, Life = life, Size = Rand(0.1f, 0.15f), Spin = Rand(-400, 400), C = c, Leaf = true });
        }

        void DrawPuffs()
        {
            float now = Time.time, dt = Time.deltaTime;
            puffs.RemoveAll(p => now - p.T0 > p.Life);
            for (int i = 0; i < puffs.Count; i++)
            {
                var p = puffs[i];
                float age = (now - p.T0) / p.Life;
                if (p.Leaf) { p.V.y += 1.6f * dt; p.V.x *= 1 - dt; p.P += (p.V + new Vector2(Mathf.Sin(now * 5 + i) * 0.4f, 0)) * dt; }
                else p.P += p.V * dt;
                puffs[i] = p;
                var c = p.C;
                c.a *= p.Leaf ? Mathf.Clamp01((1 - age) * 3) : (1 - age);
                float s = p.Leaf ? p.Size : p.Size * (0.6f + age);
                World.Sprite(PetalTex(), new Vector2(p.P.x, -p.P.y), p.Leaf ? new Vector2(s, s * 0.6f) : new Vector2(s, s * 0.7f), p.Spin * (now - p.T0), c);
            }
        }

        /// <summary>走近大树：偶尔扑棱飞出几只麻雀，掉几片叶子。每棵树隔一阵才会再惊起。</summary>
        void TreeBirds()
        {
            var lead = LeaderFoot();
            CritterDef sparrow;
            CritterDefs.TryGetValue("sparrow", out sparrow);
            bool hasBirds = CritterTex("sparrow", "move") != null;
            foreach (var d in map.Decor)
            {
                if (d.Motion != LiuBeiRPG.World.Motion.Plant || d.W < 1.4f || Time.time < d.NextBirds) continue;
                if ((new Vector2(d.X, d.Y) - lead).magnitude > 1.8f) continue;
                d.NextBirds = Time.time + 30f;
                if (Random.value > 0.6f) continue;
                var tex = Gfx.Tex(d.Sprite);
                float h = tex == null ? 2 : d.W * tex.height / (float)tex.width;
                float side = d.X >= lead.x ? 1 : -1;
                if (hasBirds)
                    for (int i = 0, n = Random.Range(2, 4); i < n; i++)
                        critters.Add(new Critter
                        {
                            Kind = "sparrow", Def = sparrow, Temp = true, State = 2, Timer = 3f, Anim = Rand(0, 0.3f),
                            P = new Vector2(d.X + Rand(-0.4f, 0.4f), d.Y + Rand(-0.1f, 0.2f)), Alt = h * Rand(0.45f, 0.7f),
                            Vel = new Vector2(side * Rand(2.4f, 3.6f), -Rand(1f, 2f)), Flip = side < 0,
                        });
                var leafColor = d.Sprite.Contains("peach") ? new Color(1f, 0.78f, 0.85f) : d.Sprite.Contains("autumn") ? new Color(0.95f, 0.65f, 0.2f) : new Color(0.5f, 0.72f, 0.3f);
                for (int i = 0; i < 6; i++)
                    Leaf(new Vector2(d.X + Rand(-d.W * 0.35f, d.W * 0.35f), d.Y - h * Rand(0.4f, 0.75f)), new Vector2(Rand(-0.4f, 0.4f), Rand(-0.2f, 0.2f)), leafColor, Rand(1.6f, 2.4f));
            }
        }

        // ---------------- 小动物 ----------------

        enum Habitat { Ground, Water, Air }

        class CritterDef
        {
            public Habitat Home;
            public float Size, Speed;            // 画多宽（格）、走多快（格/秒）
            public int Idle = 2, Move = 4, Act;  // 各动作帧数；Act = 特殊动作（鱼跳、鸭子扎猛子）
            public float Flee;                   // 人走到这么近就跑，0 = 不怕人
            public bool Flies;                   // 受惊飞走（麻雀），用 move 那排扇翅帧
            public bool Under;                   // 在水下（鱼），俯视图按游的方向转
            public bool NearWater;               // 只在水边飞（蜻蜓）
        }

        static readonly Dictionary<string, CritterDef> CritterDefs = new Dictionary<string, CritterDef>
        {
            { "rabbit",    new CritterDef { Home = Habitat.Ground, Size = 0.42f, Speed = 1.6f, Flee = 2.2f } },
            { "chicken",   new CritterDef { Home = Habitat.Ground, Size = 0.40f, Speed = 0.6f, Flee = 1.4f } },
            { "sparrow",   new CritterDef { Home = Habitat.Ground, Size = 0.26f, Speed = 0.5f, Flee = 2.4f, Flies = true } },
            { "duck",      new CritterDef { Home = Habitat.Water,  Size = 0.42f, Speed = 0.3f, Move = 0, Act = 2, Flee = 1.2f } },
            { "fish",      new CritterDef { Home = Habitat.Water,  Size = 0.36f, Speed = 0.45f, Idle = 0, Act = 3, Under = true } },
            { "butterfly", new CritterDef { Home = Habitat.Air,    Size = 0.20f, Speed = 0.7f, Move = 0 } },
            { "butterfly2", new CritterDef { Home = Habitat.Air,   Size = 0.20f, Speed = 0.7f, Move = 0 } },
            { "dragonfly", new CritterDef { Home = Habitat.Air,    Size = 0.28f, Speed = 2.6f, Move = 0, NearWater = true } },
        };

        class Critter
        {
            public string Kind;
            public CritterDef Def;
            public Vector2 P, Target, Vel;   // 脚底点（格子坐标）
            public float Timer, Anim, Alt;   // Alt：离地多高（飞起来的麻雀、蝴蝶）
            public int State;                // 0 歇着，1 走，2 受惊，3 特殊动作，4 不在（等着重新出现）
            public bool Flip;
            public bool Temp;                // 从树上惊起的鸟：飞走就没了，不重新出现
        }

        readonly List<Critter> critters = new List<Critter>();
        MapData critterMap;
        readonly System.Random lifeRng = new System.Random();

        float Rand(float a, float b) { return a + (float)lifeRng.NextDouble() * (b - a); }

        Texture2D CritterTex(string kind, string act) { return Gfx.Tex("Art/life/" + kind + "_" + act); }

        /// <summary>原图 1 像素画多少格：按 idle（没有就 move）那排一帧宽 = Size 算，几排动作同一个比例，跳起来不会忽大忽小。</summary>
        float CritterScale(Critter c)
        {
            var d = c.Def;
            bool idle = d.Idle > 0;
            var tex = CritterTex(c.Kind, idle ? "idle" : "move");
            return d.Size / (tex.width / (float)(idle ? d.Idle : d.Move));
        }

        /// <summary>换地图时按 map.Critters 重新放；缺图的种类跳过。</summary>
        void SpawnCritters()
        {
            critterMap = map;
            LoadDecor(map);
            critters.Clear();
            ripples.Clear();
            foreach (var s in map.Critters)
            {
                CritterDef def;
                if (!CritterDefs.TryGetValue(s.Kind, out def)) continue;
                if (CritterTex(s.Kind, def.Idle > 0 ? "idle" : "move") == null) continue;
                for (int i = 0; i < s.Count; i++)
                {
                    var c = new Critter { Kind = s.Kind, Def = def, Timer = Rand(0, 4) };
                    if (Place(c, false)) critters.Add(c);
                }
            }
        }

        /// <summary>挑一个合适的格子放下（离主角远一点），找不到返回 false。</summary>
        bool Place(Critter c, bool farFromLeader)
        {
            var lead = LeaderFoot();
            for (int tries = 0; tries < 60; tries++)
            {
                int x = lifeRng.Next(map.Width), y = lifeRng.Next(map.Height);
                if (!Habitable(c.Def, x, y)) continue;
                var p = new Vector2(x + Rand(0.25f, 0.75f), y + Rand(0.5f, 0.9f));
                if (farFromLeader && (p - lead).magnitude < 5) continue;
                c.P = c.Target = p;
                c.State = 0;
                c.Alt = c.Def.Home == Habitat.Air ? 0.6f : 0;
                return true;
            }
            return false;
        }

        bool Habitable(CritterDef def, int x, int y)
        {
            switch (def.Home)
            {
                case Habitat.Water: return map.IsWater(x, y);
                case Habitat.Ground: return map.IsWalkable(x, y) && !map.IsWater(x, y);
                default:
                    if (!def.NearWater) return map.IsWalkable(x, y);
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (map.IsWater(x + dx, y + dy)) return true;
                    return false;
            }
        }

        bool HabitableAt(CritterDef def, Vector2 p) { return Habitable(def, Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y - 0.5f)); }

        Vector2 LeaderFoot() { return LeaderPos() + new Vector2(0.5f, 0.92f); }

        /// <summary>在 c 附近 r 格内挑一个能去的点；挑不到就原地。</summary>
        Vector2 Wander(Critter c, float r)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                var p = c.P + new Vector2(Rand(-r, r), Rand(-r, r));
                if (HabitableAt(c.Def, p) && HabitableAt(c.Def, (p + c.P) * 0.5f)) return p;
            }
            return c.P;
        }

        void UpdateCritters(float dt)
        {
            if (critterMap != map) { SpawnCritters(); puffs.Clear(); }
            var lead = LeaderFoot();
            if (dt > 0) { UpdatePush(dt); StepDust(); TreeBirds(); }
            foreach (var c in critters)
            {
                var d = c.Def;
                c.Anim += dt;
                c.Timer -= dt;
                if (c.State == 4) { if (c.Timer <= 0 && Place(c, true)) c.Timer = Rand(1, 4); continue; }

                float near = (c.P - lead).magnitude;
                if (d.Flee > 0 && near < d.Flee && c.State < 2) Startle(c, lead);

                switch (d.Home)
                {
                    case Habitat.Ground: GroundStep(c, dt); break;
                    case Habitat.Water: WaterStep(c, dt); break;
                    default: AirStep(c, dt); break;
                }
            }
            critters.RemoveAll(c => c.Temp && c.State == 4);
            UpdateRipples();
        }

        void Startle(Critter c, Vector2 lead)
        {
            var away = c.P - lead;
            if (away.sqrMagnitude < 0.01f) away = Vector2.right;
            away.Normalize();
            if (c.Def.Flies)
            {
                c.State = 2; c.Timer = 3f; c.Anim = 0;
                c.Vel = new Vector2(Mathf.Sign(away.x) * 3.2f, -1.6f);
                c.Flip = c.Vel.x < 0;
                return;
            }
            if (c.Def.Home == Habitat.Water && c.Def.Under) { c.Target = c.P + away * 1.2f; if (!HabitableAt(c.Def, c.Target)) c.Target = c.P; c.State = 2; return; }
            // 跑开：往反方向找个能去的点
            for (int tries = 0; tries < 8; tries++)
            {
                var p = c.P + (away + new Vector2(Rand(-0.6f, 0.6f), Rand(-0.6f, 0.6f))) * Rand(2f, 3.5f);
                if (HabitableAt(c.Def, p)) { c.Target = p; c.State = 2; return; }
            }
        }

        void GroundStep(Critter c, float dt)
        {
            var d = c.Def;
            if (c.State == 2 && d.Flies)
            {
                c.P += c.Vel * dt;
                c.Alt += dt * 1.8f;
                if (c.Timer <= 0) { c.State = 4; c.Timer = Rand(15, 30); }
                return;
            }
            if (c.State == 0)
            {
                if (c.Timer <= 0) { c.Target = Wander(c, 3); c.State = 1; c.Anim = 0; }
                return;
            }
            float speed = d.Speed * (c.State == 2 ? 2.5f : 1);
            if (MoveToward(c, speed, dt)) { c.State = 0; c.Timer = Rand(1.5f, 5f); c.Anim = 0; }
        }

        void WaterStep(Critter c, float dt)
        {
            var d = c.Def;
            if (c.State == 3)   // 鱼跳 / 鸭子扎猛子
            {
                float dur = d.Under ? 0.9f : 1.6f;
                if (d.Under) c.P += c.Vel * dt * 0.8f;
                if (c.Anim >= dur)
                {
                    c.State = 1; c.Timer = Rand(8, 20);
                    AddRipple(c.P, d.Under ? 0.9f : 0.6f);
                    if (!HabitableAt(d, c.P)) Place(c, false);   // 跳出了水就悄悄放回水里
                }
                return;
            }
            if (c.Timer <= 0 && c.State != 2)
            {
                c.State = 3; c.Anim = 0;
                AddRipple(c.P, d.Under ? 0.7f : 0.5f);
                return;
            }
            float speed = d.Speed * (c.State == 2 ? 3f : 1);
            if (MoveToward(c, speed, dt)) { c.State = 1; c.Target = Wander(c, 2); }
            if (!d.Under && lifeRng.NextDouble() < dt * 0.3f) AddRipple(c.P + new Vector2(-0.1f, 0), 0.35f);   // 鸭子游过拖出的水纹
        }

        void AirStep(Critter c, float dt)
        {
            var d = c.Def;
            if (d.NearWater)
            {
                // 蜻蜓：悬停一会儿，猛地窜到下一个点
                if (c.State == 0) { if (c.Timer <= 0) { c.Target = Wander(c, 2.5f); c.State = 1; } }
                else if (MoveToward(c, d.Speed, dt)) { c.State = 0; c.Timer = Rand(0.8f, 2.5f); }
                c.Alt = 0.5f + Mathf.Sin(c.Anim * 5f) * 0.03f;
                return;
            }
            // 蝴蝶：慢悠悠地边走边晃，偶尔换方向
            if (MoveToward(c, d.Speed, dt) || c.Timer <= 0) { c.Target = Wander(c, 3); c.Timer = Rand(2, 5); }
            c.P += new Vector2(Mathf.Sin(c.Anim * 2.3f), Mathf.Cos(c.Anim * 1.7f)) * 0.25f * dt;
            c.Alt = 0.55f + Mathf.Sin(c.Anim * 3.1f) * 0.15f;
        }

        /// <summary>朝 Target 走，到了返回 true。</summary>
        static bool MoveToward(Critter c, float speed, float dt)
        {
            var to = c.Target - c.P;
            float dist = to.magnitude;
            if (dist < 0.05f) return true;
            c.Vel = to / dist * speed;
            c.P += c.Vel * Mathf.Min(dt, dist / speed);
            if (Mathf.Abs(to.x) > 0.02f) c.Flip = to.x < 0;
            return false;
        }

        /// <summary>水下的鱼：画在地面之上、人和物件之下，半透明发蓝；俯视图转到游的方向。</summary>
        void DrawUnderwater()
        {
            foreach (var c in critters)
            {
                if (!c.Def.Under || c.State == 3 || c.State == 4) continue;
                var tex = CritterTex(c.Kind, "move");
                if (tex == null) continue;
                int n = c.Def.Move;
                float s = CritterScale(c), w = tex.width / (float)n * s, h = tex.height * s;
                float speed = c.Vel.magnitude;
                int f = (int)(c.Anim * (4 + speed * 10)) % n;
                float ang = speed > 0.01f ? Mathf.Atan2(-c.Vel.y, c.Vel.x) * Mathf.Rad2Deg : 0;
                World.Sprite(tex, new Vector2(c.P.x, -(c.P.y - 0.3f)), new Vector2(w, h), ang, new Color(0.75f, 0.9f, 1f, 0.6f), new Rect(f / (float)n, 0, 1f / n, 1));
            }
        }

        /// <summary>地上的、水面上的、跳出水的：和人物一起按脚底排前后。</summary>
        void AddCritterDraws(List<KeyValuePair<float, System.Action>> list)
        {
            foreach (var c in critters)
            {
                if (c.State == 4 || c.Def.Home == Habitat.Air) continue;
                if (c.Def.Under && c.State != 3) continue;
                if (c.Def.Flies && c.State == 2) continue;   // 飞起来的画在最上面
                var cc = c;
                list.Add(new KeyValuePair<float, System.Action>(c.P.y + list.Count * 1e-5f, () => DrawCritter(cc)));
            }
        }

        /// <summary>飞着的：蝴蝶、蜻蜓、惊飞的麻雀。画在所有东西上面，地上留个淡影子。</summary>
        void DrawFlyers()
        {
            foreach (var c in critters)
                if (c.State != 4 && (c.Def.Home == Habitat.Air || (c.Def.Flies && c.State == 2))) DrawCritter(c);
        }

        void DrawCritter(Critter c)
        {
            var d = c.Def;
            // 选哪排帧：特殊动作 > 飞虫一直扇翅 > 走 / 游 > 歇着
            string act; int n; float fps;
            bool moving = c.State == 1 || c.State == 2;
            if (c.State == 3) { act = d.Under ? "jump" : "dive"; n = d.Act; fps = n / (d.Under ? 0.9f : 1.6f); }
            else if (d.Home == Habitat.Air) { act = "idle"; n = d.Idle; fps = 12; }
            else if ((moving || d.Idle == 0) && d.Move > 0) { act = "move"; n = d.Move; fps = d.Flies && c.State == 2 ? 14 : 6 + d.Speed * 3; }
            else { act = "idle"; n = d.Idle; fps = 3; }
            var tex = CritterTex(c.Kind, act);
            if (tex == null) return;
            int f = c.State == 3 ? Mathf.Min(n - 1, (int)(c.Anim * fps)) : (int)(c.Anim * fps) % n;   // 特殊动作只播一遍
            float s = CritterScale(c), w = tex.width / (float)n * s, h = tex.height * s;
            float lift = c.Alt;
            if (c.State == 3 && d.Under) lift = Mathf.Sin(Mathf.Clamp01(c.Anim / 0.9f) * Mathf.PI) * 0.6f;   // 鱼跳的弧线
            bool inWater = d.Home == Habitat.Water;
            if (!inWater)
            {
                float sw = w * (lift > 0 ? 0.6f : 0.8f);
                World.Quad(ShadowTex(), new Rect(c.P.x - sw * 0.5f, c.P.y - sw * 0.12f, sw, sw * 0.24f), new Color(1, 1, 1, lift > 0 ? 0.5f : 1f));
            }
            var r = new Rect(c.P.x - w * 0.5f, c.P.y - h - lift, w, h);
            World.Frame(tex, r, f, n, c.Flip, Gfx.Hex(map.Light));
        }

        // ---------------- 涟漪 ----------------

        struct Ripple { public Vector2 P; public float T0, Size; }
        readonly List<Ripple> ripples = new List<Ripple>();
        float nextAmbientRipple;
        readonly Dictionary<PuppetActor, float> wadeRipple = new Dictionary<PuppetActor, float>();

        void AddRipple(Vector2 p, float size) { ripples.Add(new Ripple { P = p, T0 = Time.time, Size = size }); }

        void UpdateRipples()
        {
            ripples.RemoveAll(r => Time.time - r.T0 > 1.4f);
            // 水面时不时冒个小圈（鱼吐泡），水塘才不像一块玻璃
            if (map.WaterMask == null || Time.time < nextAmbientRipple) return;
            nextAmbientRipple = Time.time + Rand(0.6f, 1.8f);
            for (int tries = 0; tries < 10; tries++)
            {
                int x = lifeRng.Next(map.Width), y = lifeRng.Next(map.Height);
                if (map.At(x, y) == '~') { AddRipple(new Vector2(x + Rand(0.2f, 0.8f), y + Rand(0.3f, 0.9f)), Rand(0.25f, 0.45f)); break; }
            }
        }

        /// <summary>一圈一圈往外扩、变淡的扁椭圆。</summary>
        void DrawRipples()
        {
            foreach (var r in ripples)
            {
                float age = Time.time - r.T0;
                float w = r.Size * (0.3f + age * 1.1f);
                World.Quad(RingTex(), new Rect(r.P.x - w * 0.5f, r.P.y - w * 0.2f, w, w * 0.4f), new Color(1, 1, 1, 0.55f * (1 - age / 1.4f)));
            }
        }

        /// <summary>人站在浅水里：返回水面的世界 y（否则 NaN），顺便在脚边冒涟漪。</summary>
        float WadeWaterY(PuppetActor a, Vector2 cell, Vector2 foot, float k, bool moving)
        {
            if (map.At(Mathf.RoundToInt(cell.x), Mathf.RoundToInt(cell.y)) != 'w') return float.NaN;
            float last;
            wadeRipple.TryGetValue(a, out last);
            if (Time.time - last > (moving ? 0.3f : 1.1f))
            {
                wadeRipple[a] = Time.time;
                AddRipple(new Vector2(foot.x, -foot.y), 0.55f * k);
            }
            return foot.y + 0.2f * k;   // 水没到小腿
        }

        // ---------------- 炊烟、篝火 ----------------

        void DrawEmitters()
        {
            float t = Time.time;
            foreach (var e in map.Emitters)
            {
                // 烟：一串越飘越大、越淡的烟团，被风往右吹
                const int n = 9;
                for (int i = 0; i < n; i++)
                {
                    float age = Mathf.Repeat(t * 0.16f + i / (float)n + e.X * 0.37f, 1f);
                    float x = e.X + age * age * (0.6f + wind * 1.6f) + Mathf.Sin(t * 0.8f + i * 1.9f) * 0.12f * age;
                    float y = e.Y - 0.3f - age * 2.6f;
                    float s = 0.3f + age * 1.1f;
                    float a = 0.3f * (1 - age) * Mathf.Clamp01(age * 6f);
                    World.Sprite(PetalTex(), new Vector2(x, -y), new Vector2(s, s * 0.8f), i * 40 + t * 8, new Color(0.88f, 0.87f, 0.84f, a));
                }
            }
        }
    }
}
