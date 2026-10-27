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

        /// <summary>按格子坐标（y 向下，和 Rect 一样）贴一张图。</summary>
        public static void Quad(Texture tex, Rect r, Color c)
        {
            Draw(tex, Matrix4x4.TRS(new Vector3(r.x, -r.yMax, 0), Quaternion.identity, new Vector3(r.width, r.height, 1)), c, 0);
        }

        /// <summary>以中心点（世界坐标）贴一张旋转的小图：花瓣、落叶。</summary>
        public static void Sprite(Texture tex, Vector2 center, Vector2 size, float angle, Color c)
        {
            var m = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), new Vector3(size.x, size.y, 1))
                    * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));
            Draw(tex, m, c, 0);
        }

        /// <summary>骨骼角色：foot = 脚底中点（世界坐标），s = 原图 1 像素多少格。</summary>
        public static void Puppet(PuppetActor a, Vector2 foot, float s, Color tint)
        {
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
                Draw(tex, m, new Color(tint.r * d.k, tint.g * d.k, tint.b * d.k, tint.a), a.Flash);
            }
        }

        static void Draw(Texture tex, Matrix4x4 m, Color c, float flash)
        {
            if (quad == null) Init();
            m.m23 = z;
            z -= 0.001f;
            mpb.SetTexture("_MainTex", tex);
            mpb.SetColor("_Color", c);
            mpb.SetFloat("_Flash", flash);
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
