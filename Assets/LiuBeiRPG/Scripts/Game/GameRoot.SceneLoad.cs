using System;
using System.Collections.Generic;
using UnityEngine;
using Puppet;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 场景加载页：进地图（含开局、读档）和进战斗时，把这个场景要用的图（微信包里在云上）下载并解码好再放行，
    /// 中间盖一张和微信启动页一样的图（Art/loading_bg）+ 金色进度条。图都已在内存里就直接过，不出这一页。
    /// 编辑器 / Mac 包图在本地，Remote.Expand 返回空，永远不出。
    /// </summary>
    public partial class GameRoot
    {
        List<string> sceneFiles;   // 这次要等的文件；null = 没在加载
        float sceneLoadAt;
        Action sceneReady;
        int sceneLastDone;
        float sceneProgressAt;                // 上次有进展的时间
        const float SceneStallHint = 15f;     // 15 秒一点进展都没有（断网之类）才给「点此跳过」，不自动跳

        bool SceneLoading { get { return sceneFiles != null; } }

        /// <summary>need 是路径前缀（写法同 Remote.Prefetch）；全部就绪后调 ready（重置入场动画的计时）。</summary>
        void BeginSceneLoad(List<string> need, Action ready)
        {
            var files = Remote.Expand(need);
            foreach (var f in files) Remote.Tex(f);   // 顺手触发：预载好的排进解码，没下的马上下
            sceneFiles = files;
            sceneLoadAt = sceneProgressAt = Time.time;
            sceneLastDone = 0;
            sceneShownK = 0;
            sceneReady = ready;
            UpdateSceneLoad();   // 都已就绪就当场放行，不出加载页
        }

        int SceneDone()
        {
            int n = 0;
            foreach (var f in sceneFiles) if (!Remote.Pending(f)) n++;
            return n;
        }

        /// <summary>每帧（Update）：全部就绪才放行（下载失败的会自动重试，云上没有的不算）。</summary>
        void UpdateSceneLoad()
        {
            Remote.Loading = sceneFiles != null;
            if (sceneFiles == null) return;
            int done = SceneDone();
            if (done != sceneLastDone) { sceneLastDone = done; sceneProgressAt = Time.time; }
            if (done < sceneFiles.Count) return;
            FinishSceneLoad();
        }

        void FinishSceneLoad()
        {
            sceneFiles = null;
            Remote.Loading = false;
            var r = sceneReady;
            sceneReady = null;
            if (r != null) r();
        }

        void DrawSceneLoad()
        {
            Gfx.Rect(new Rect(0, 0, Gfx.VW, Gfx.VH), Color.black);
            if (Time.time - sceneLoadAt < 0.25f) return;   // 解码一两帧就好的，只黑一下，别闪加载页
            var bg = Gfx.Tex("Art/loading_bg");
            if (bg != null)
            {
                float s = Mathf.Max(Gfx.VW / bg.width, Gfx.VH / bg.height), w = bg.width * s, h = bg.height * s;
                Gfx.Image(new Rect((Gfx.VW - w) * 0.5f, (Gfx.VH - h) * 0.5f, w, h), bg);
            }
            // 进度条：和微信启动页同色（暖金 #D9A441），压在底图下方暗色草地上
            float k = sceneFiles.Count == 0 ? 1 : SceneDone() / (float)sceneFiles.Count;
            sceneShownK = Mathf.MoveTowards(sceneShownK, k, Time.unscaledDeltaTime * 1.5f);   // 显示的进度追着真进度走，小人跑得顺
            float bw = Mathf.Min(Gfx.VW * 0.6f, 420), bh = 22;
            var bar = new Rect((Gfx.VW - bw) * 0.5f, Gfx.VH * 0.86f, bw, bh);
            Gfx.Pill(bar, new Color(0, 0, 0, 0.55f));
            if (sceneShownK > 0) Gfx.Pill(new Rect(bar.x + 2, bar.y + 2, Mathf.Max(bh - 4, (bw - 4) * sceneShownK), bh - 4), new Color32(217, 164, 65, 255));
            DrawRunner(new Vector2(bar.x + 6 + (bw - 12) * sceneShownK, bar.y - 2));
            Gfx.Text(new Rect(0, bar.yMax + 6, Gfx.VW, 24), "加载中 " + Mathf.RoundToInt(sceneShownK * 100) + "%", 16, Color.white, TextAnchor.MiddleCenter);
            if (Time.time - sceneProgressAt > SceneStallHint)
            {
                var hint = new Rect(0, bar.yMax + 34, Gfx.VW, 30);
                Gfx.Text(hint, "网络不太好，仍在加载…点此跳过", 15, new Color(1, 0.9f, 0.6f), TextAnchor.MiddleCenter);
                if (Gfx.Clicked(hint)) FinishSceneLoad();   // 缺的图进去以后再补
            }
        }

        float sceneShownK;
        PuppetActor runner;
        bool runnerTried;

        /// <summary>加载页上的刘备：侧身小跑，脚踩在进度条头上，跟着进度往右跑。
        /// 他侧面那套部件打进了微信包（build_wx.sh 不剥），加载页一出来就能跑，不用等下载。</summary>
        void DrawRunner(Vector2 foot)
        {
            if (!runnerTried)
            {
                runnerTried = true;
                try { runner = PuppetActor.Load("liubei"); runner.Facing = "side"; runner.Speed = 1.8f; runner.Play("walk"); } catch (Exception) { runner = null; }
            }
            if (runner == null) return;
            if (Event.current.type == EventType.Repaint) runner.Tick(Time.unscaledDeltaTime);
            runner.DrawGUI(foot, 86 / 950f);
        }

        /// <summary>这张地图要用的：底图、前景、物件、路人，加队伍每个人所有朝向的部件。</summary>
        List<string> MapNeed()
        {
            var need = new List<string>();
            if (map.Art != null) { need.Add("Map/Art/" + map.Art); need.Add("Map/Art/" + map.Art + "_fg"); }
            if (map.WaterMask != null) need.Add("Map/" + map.WaterMask);
            if (party != null) foreach (var m in party.Members) need.Add("Puppets/" + (m.Def.Skin ?? m.Def.Id));
            LoadDecor(map);   // 物件清单可能还没读
            foreach (var d in map.Decor) need.Add("Map/" + d.Sprite);
            foreach (var p in map.Props) need.Add("Map/" + p.Sprite);
            foreach (var n in map.Npcs)
            {
                if (n.Sprite != null) need.Add("Map/" + n.Sprite);
                if (n.Id != null) need.Add("Puppets/" + (n.Skin ?? n.Id));
                if (n.Weapon != null) need.Add("Puppets/weapons/" + n.Weapon);
            }
            return need;
        }
    }
}
