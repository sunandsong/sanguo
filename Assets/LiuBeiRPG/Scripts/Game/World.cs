using System.Collections.Generic;
using UnityEngine;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 世界层：每帧（LateUpdate 里）用 Graphics.DrawMesh 把贴片交给相机画，后处理才能作用在地图和人物上。
    /// 坐标：1 单位 = 1 格，x 向右，y 向上（格子 (x,y) 在世界里是 x..x+1, -y-1..-y）。
    /// 先提交的在下面：每画一片 z 往相机方向挪一点，相机按正交距离从后往前排。
    /// </summary>
    public static class World
    {
        static Mesh quad;
        static Material mat;
        static MaterialPropertyBlock mpb;
        static float z;
        static readonly List<PuppetDraw> draws = new List<PuppetDraw>();
        static readonly Dictionary<string, Texture2D> puppetTex = new Dictionary<string, Texture2D>();

        public static void Begin() { z = 50f; }

        /// <summary>按格子坐标（y 向下，和 Rect 一样）贴一张图。uv 只取贴图的一部分（0..1，y 向上），默认整张。
        /// fx 让它动（见 World.shader：x 幅度，y 频率，z 相位，w 模式），水面模式要带 mask。</summary>
        public static void Quad(Texture tex, Rect r, Color c, Rect? uv = null, Vector4 fx = default(Vector4), Texture mask = null, float push = 0)
        {
            var u = uv ?? new Rect(0, 0, 1, 1);
            Draw(tex, Matrix4x4.TRS(new Vector3(r.x, -r.yMax, 0), Quaternion.identity, new Vector3(r.width, r.height, 1)), c, 0,
                new Vector4(u.width, u.height, u.x, u.y), fx, mask, default(Vector4), push);
        }

        public const float Plant = 1, Hang = 2, Flag = 3, Water = 4;   // fx.w 的几种模式

        static Material addMat, mulMat;

        /// <summary>叠加光：灯笼、篝火的光晕，夜里压暗之后再画，才亮得起来。</summary>
        public static void Glow(Texture tex, Rect r, Color c) { DrawBlend(tex, r, c, ref addMat, 0, UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One); }

        /// <summary>正片叠底：整块乘一个颜色（天色、夜色、雨天）。</summary>
        public static void Tint(Rect r, Color c) { DrawBlend(Texture2D.whiteTexture, r, c, ref mulMat, 1, UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.Zero); }

        static void DrawBlend(Texture tex, Rect r, Color c, ref Material m, float mode, UnityEngine.Rendering.BlendMode src, UnityEngine.Rendering.BlendMode dst)
        {
            if (quad == null) Init();
            if (m == null)
            {
                m = new Material(Resources.Load<Shader>("Shaders/WorldBlend")) { hideFlags = HideFlags.HideAndDontSave };
                m.SetFloat("_Mode", mode);
                m.SetFloat("_SrcBlend", (float)src);
                m.SetFloat("_DstBlend", (float)dst);
            }
            var mtx = Matrix4x4.TRS(new Vector3(r.x, -r.yMax, z), Quaternion.identity, new Vector3(r.width, r.height, 1));
            z -= 0.001f;
            blendMpb.Clear();
            blendMpb.SetTexture("_MainTex", tex);
            blendMpb.SetColor("_Color", c);
            Graphics.DrawMesh(quad, mtx, m, 0, null, 0, blendMpb);
        }

        static readonly MaterialPropertyBlock blendMpb = new MaterialPropertyBlock();

        /// <summary>
        /// 投影：把贴图压扁、斜着铺在地上（太阳方向），染成半透明黑。r 是物件本身在格子坐标里的框（y 向下），
        /// shx = 影子往右斜多少（按高度算，负数往左），sy = 压扁到多高。
        /// </summary>
        public static void CastShadow(Texture tex, Rect r, float shx, float sy, float alpha, Rect? uv = null)
        {
            var u = uv ?? new Rect(0, 0, 1, 1);
            var m = Matrix4x4.identity;
            m.m00 = r.width; m.m01 = r.height * shx; m.m03 = r.x;
            m.m11 = r.height * sy; m.m13 = -r.yMax;
            Draw(tex, m, new Color(0, 0, 0, alpha), 0, new Vector4(u.width, u.height, u.x, u.y));
        }

        /// <summary>同一张贴图上取第 frame 帧（横排 frames 帧），flip 左右翻。小动物、篝火用。</summary>
        public static void Frame(Texture tex, Rect r, int frame, int frames, bool flip, Color c)
        {
            float w = 1f / frames;
            Quad(tex, r, c, flip ? new Rect((frame + 1) * w, 0, -w, 1) : new Rect(frame * w, 0, w, 1));
        }

        /// <summary>以中心点（世界坐标）贴一张旋转的小图：花瓣、落叶。</summary>
        public static void Sprite(Texture tex, Vector2 center, Vector2 size, float angle, Color c, Rect? uv = null)
        {
            var m = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), new Vector3(size.x, size.y, 1))
                    * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));
            var u = uv ?? new Rect(0, 0, 1, 1);
            Draw(tex, m, c, 0, new Vector4(u.width, u.height, u.x, u.y));
        }

        /// <summary>骨骼角色：foot = 脚底中点（世界坐标），s = 原图 1 像素多少格。</summary>
        /// <summary>waterY 不是 NaN 时人站在浅水里：世界 y 低于它的部分染水色、淡出。</summary>
        public static void Puppet(PuppetActor a, Vector2 foot, float s, Color tint, float waterY = float.NaN)
        {
            var clip = float.IsNaN(waterY) ? Vector4.zero : new Vector4(waterY, 1, 0, 0);
            PuppetPose.Pose(a.Rig, a.Skin, a.Clip, a.Facing, a.Time, draws, a.Weapon);
            foreach (var d in draws)
            {
                var tex = PuppetTex(d.tex);
                float w = tex.width, h = tex.height;
                // 单位方块 (qx,qy) → 部件像素 (qx·w, (1-qy)·h) → 角色空间 (a·u + c·v + tx, b·u + d·v + ty) → 世界
                var m = Matrix4x4.identity;
                m.m00 = s * d.a * w; m.m01 = -s * d.c * h; m.m03 = foot.x + s * (d.c * h + d.tx);
                m.m10 = s * d.b * w; m.m11 = -s * d.d * h; m.m13 = foot.y + s * (d.d * h + d.ty);
                // ponytail: 压暗直接乘在 Gamma 空间，和 PuppetActor.DrawGUI 一致；改 Linear 色彩空间时要换成 k^2.2
                Draw(tex, m, new Color(tint.r * d.k, tint.g * d.k, tint.b * d.k, tint.a), a.Flash, new Vector4(1, 1, 0, 0), default(Vector4), null, clip);
            }
        }

        static void Draw(Texture tex, Matrix4x4 m, Color c, float flash, Vector4 uvST, Vector4 fx = default(Vector4), Texture mask = null, Vector4 clip = default(Vector4), float push = 0)
        {
            if (quad == null) Init();
            m.m23 = z;
            z -= 0.001f;
            mpb.SetTexture("_MainTex", tex);
            mpb.SetColor("_Color", c);
            mpb.SetFloat("_Flash", flash);
            mpb.SetVector("_MainTex_ST", uvST);
            mpb.SetVector("_Fx", fx);
            mpb.SetVector("_Clip", clip);
            mpb.SetFloat("_Push", push);
            mpb.SetTexture("_Mask", mask != null ? mask : Texture2D.blackTexture);
            Graphics.DrawMesh(quad, m, mat, 0, null, 0, mpb);   // 属性块在调用时就拷贝了，可以复用
        }

        static void Init()
        {
            quad = new Mesh
            {
                vertices = new[] { new Vector3(0, 0), new Vector3(1, 0), new Vector3(1, 1), new Vector3(0, 1) },
                uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 },
                hideFlags = HideFlags.HideAndDontSave,
            };
            quad.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);   // 变换矩阵会剪切缩放，别让相机按原始包围盒剔掉
            mat = new Material(Resources.Load<Shader>("Shaders/World")) { hideFlags = HideFlags.HideAndDontSave };
            mpb = new MaterialPropertyBlock();
        }

        static Texture2D PuppetTex(string path)
        {
            Texture2D t;
            if (!puppetTex.TryGetValue(path, out t))
            {
                puppetTex[path] = t = Resources.Load<Texture2D>("Puppets/" + path);
                if (t == null) throw new System.Exception("缺少贴图 Resources/Puppets/" + path + ".png");
            }
            return t;
        }
    }
}
