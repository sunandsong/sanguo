using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 贴图：本地 Resources 有就用（编辑器、Mac 包），没有就从云存储下（微信包打包时把 png 剥掉了，见 Tools/build_wx.sh）。
    /// 下载是异步的：没下完之前返回 null，调用方照"缺图"处理（不画 / 色块），下完下一帧自然就画上了。
    /// 云端的图由 Tools/upload_art.sh 压缩上传，路径和 Resources 下一样，清单在 Resources/RemoteFiles.txt。
    /// Prefetch：进章节时把这一章要用的图排队后台先下（见 GameRoot.PrefetchChapter）。
    /// </summary>
    public static class Remote
    {
        public const string Base = "https://7265-renshengqingdan-d9fc03opf3bac6ba-1478597699.tcb.qcloud.la/liubei/";

        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, float> retryAt = new Dictionary<string, float>();   // 正在下 / 失败后等重试
        const float RetryDelay = 5f;

        // 云上有哪些文件：路径（不带扩展名）→ 扩展名
        static Dictionary<string, string> files;
        static Dictionary<string, string> Files
        {
            get
            {
                if (files != null) return files;
                files = new Dictionary<string, string>();
                var list = Resources.Load<TextAsset>("RemoteFiles");
                if (list != null)
                    foreach (var line in list.text.Split('\n'))
                    {
                        int dot = line.LastIndexOf('.');
                        if (dot > 0) files[line.Substring(0, dot)] = line.Substring(dot).Trim();
                    }
                return files;
            }
        }

        /// <summary>path 是 Resources 下的相对路径，不带扩展名，如 "Map/Art/title_bg"、"Puppets/liubei/front/head"。</summary>
        public static Texture2D Tex(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Texture2D t;
            if (cache.TryGetValue(path, out t)) return t;
            if (retryAt.ContainsKey(path)) { Download(path, false); return null; }   // 在下 / 等解码：别每帧再查 Resources
            byte[] b;
            if (stored.TryGetValue(path, out b))   // 内存里有压缩数据（预载的、或释放过的）：要画了才解码，数据留着下次还能用
            {
                retryAt[path] = float.MaxValue;
                decodeQueue.Enqueue(new KeyValuePair<string, byte[]>(path, b));
                return null;
            }
            t = Resources.Load<Texture2D>(path);
            if (t != null) return cache[path] = t;
            if (Files.Count > 0 && !Files.ContainsKey(path)) return cache[path] = null;   // 清单里没有 = 云上也没有（可选的图：地名牌、地图前景…），别发请求白拿 404
            Download(path, false);
            return null;
        }

        /// <summary>还在下 / 等解码就是 true；有了、或者云上根本没有（缺图）都是 false。顺手触发加载。</summary>
        public static bool Pending(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            Tex(path);
            return !cache.ContainsKey(path);
        }

        /// <summary>need 里每个路径（写法同 Prefetch）在云上对应的全部文件；本地有图（编辑器 / Mac 包）时返回空。</summary>
        public static List<string> Expand(List<string> need)
        {
            var r = new List<string>();
            if (LocalArt) return r;
            var seen = new HashSet<string>();
            foreach (var p in Files.Keys)
                foreach (var n in need)
                    if (n != null && Under(p, n)) { if (seen.Add(p)) r.Add(p); break; }
            return r;
        }

        // ---------------- 背景音乐 ----------------

        static readonly Dictionary<string, AudioClip> audio = new Dictionary<string, AudioClip>();
        static readonly HashSet<string> audioLoading = new HashSet<string>();

        /// <summary>背景音乐：本地 Resources 有就用，没有就从云上下 mp3（微信包剥掉了 bgm，见 build_wx.sh；上传见 upload_art.sh）。没下完返回 null。
        /// path 如 "Audio/bgm_map"。失败就当没这首（不响），不重试。</summary>
        public static AudioClip Audio(string path)
        {
            AudioClip c;
            if (audio.TryGetValue(path, out c)) return c;
            c = Resources.Load<AudioClip>(path);
            if (c != null) return audio[path] = c;
            if (!audioLoading.Add(path)) return null;
            var req = UnityWebRequestMultimedia.GetAudioClip(Base + path + ".mp3", AudioType.MPEG);
            req.SendWebRequest().completed += _ =>
            {
                audio[path] = req.result == UnityWebRequest.Result.Success ? DownloadHandlerAudioClip.GetContent(req) : null;
                audioLoading.Remove(path);
                req.Dispose();
            };
            return null;
        }

        // ---------------- 后台预载 ----------------

        static readonly Queue<string> queue = new Queue<string>();
        static readonly HashSet<string> queued = new HashSet<string>();
        static int background;          // 正在下的预载数
        const int MaxBackground = 8;    // 微信同时最多 10 个请求，留 2 个给眼前要画的图

        // 预载只下载、不解码：解码后一张大图就占好几 MB 显存，一章全解码手机会被杀。
        // 下好的压缩数据先放内存（同时也进了微信的文件缓存，见 WxBuild 的 bundlePathIdentifier），画到时才解码。
        static readonly Dictionary<string, byte[]> stored = new Dictionary<string, byte[]>();
        static long storedBytes;
        const long StoreCap = 48L << 20;   // ponytail: 超过 48MB 就不再留内存，只靠微信文件缓存；真机内存紧再调小

        /// <summary>这一轮预载的进度（首页显示「资源下载中」）：排了多少、下完多少。全部下完后两个都归零。</summary>
        public static int PrefetchTotal, PrefetchDone;

        /// <summary>把 path 本身，以及 path + "/"、path + "_" 开头的云上文件（人偶整个目录、动画帧）排进后台下载。</summary>
        public static void Prefetch(string path)
        {
            if (string.IsNullOrEmpty(path) || LocalArt) return;
            foreach (var p in Files.Keys)
                if (Under(p, path) && !cache.ContainsKey(p) && !stored.ContainsKey(p) && queued.Add(p)) { queue.Enqueue(p); PrefetchTotal++; }
            Pump();
        }

        /// <summary>p 是 path 本身，或 path + "/"、path + "_" 开头（人偶整个目录、动画帧）。</summary>
        static bool Under(string p, string path)
        {
            return p == path || (p.Length > path.Length && p.StartsWith(path, System.StringComparison.Ordinal) && (p[path.Length] == '/' || p[path.Length] == '_'));
        }

        static bool? localArt;   // 编辑器 / Mac 包图都在本地，不用预载
        static bool LocalArt { get { return (localArt ?? (localArt = Resources.Load<Texture2D>("Map/Art/title_bg") != null)).Value; } }

        static void Pump()
        {
            while (background < MaxBackground && queue.Count > 0)
            {
                var p = queue.Dequeue();
                queued.Remove(p);
                if (cache.ContainsKey(p) || stored.ContainsKey(p) || retryAt.ContainsKey(p)) { PrefetchDone++; continue; }
                Download(p, true);
            }
            if (queue.Count == 0 && background == 0) PrefetchTotal = PrefetchDone = 0;
        }

        // ---------------- 下载 ----------------

        static void Download(string path, bool bg)
        {
            float at;
            if (retryAt.TryGetValue(path, out at) && Time.realtimeSinceStartup < at) return;
            retryAt[path] = float.MaxValue;
            if (bg) background++;
            string ext;
            var req = UnityWebRequest.Get(Base + path + (Files.TryGetValue(path, out ext) ? ext : ".png"));
            req.timeout = 30;   // 挂住不回的请求也要算完成，不然首页「下载中」会卡住、按钮一直灰着
            req.SendWebRequest().completed += _ =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var data = req.downloadHandler.data;
                    if (!stored.ContainsKey(path) && storedBytes + data.Length <= StoreCap) { stored[path] = data; storedBytes += data.Length; }   // 压缩数据留内存，释放后重解不走网络
                    if (!bg) decodeQueue.Enqueue(new KeyValuePair<string, byte[]>(path, data));   // 眼前要画的：留到 Tick 里分帧解码
                    else retryAt.Remove(path);   // 预载的：只下载，画到时才解码
                }
                else if (req.responseCode == 404) cache[path] = null;   // 云上也没有：当缺图，别再问
                else retryAt[path] = Time.realtimeSinceStartup + RetryDelay;
                req.Dispose();
                if (bg) { background--; PrefetchDone++; Pump(); }
            };
        }

        // ---------------- 分帧解码 ----------------

        static readonly Queue<KeyValuePair<string, byte[]>> decodeQueue = new Queue<KeyValuePair<string, byte[]>>();
        /// <summary>场景加载页开着：画面是静止的，解码放开到每帧 30ms，赶紧解完；玩的时候只给 8ms，不卡。</summary>
        public static bool Loading;
        static float DecodeBudget { get { return Loading ? 0.03f : 0.008f; } }

        /// <summary>每帧调一次（GameRoot.Update）：解码下好的图，超过预算留到下一帧，但每帧至少解一张。</summary>
        public static void Tick()
        {
            float start = Time.realtimeSinceStartup;
            while (decodeQueue.Count > 0)
            {
                var kv = decodeQueue.Dequeue();
                cache[kv.Key] = Decode(kv.Key, kv.Value);
                decoded.Add(kv.Key);
                retryAt.Remove(kv.Key);
                if (Time.realtimeSinceStartup - start > DecodeBudget) break;
            }
        }

        // ---------------- 释放 ----------------

        static readonly HashSet<string> decoded = new HashSet<string>();   // 从网上下来解码的图（Resources 里的不归这里管）

        // 全局一直要用的：界面、首页、读档页、道具 / 法术图标、法术特效、像素图块（Map/ 下非 Art）、兵器
        static readonly string[] Keep = { "Map/Art/ui", "Map/Art/title", "Map/Art/load", "Map/Art/about", "Map/Art/item_", "Map/Art/tactic_",
                                          "Map/Art/fx/", "Map/Art/dialogue", "Map/Art/place_name", "Map/Art/battle_disc", "Map/Art/seal", "Puppets/weapons/" };

        static bool Kept(string p)
        {
            if (p.StartsWith("Map/", System.StringComparison.Ordinal) && !p.StartsWith("Map/Art/", System.StringComparison.Ordinal)) return true;
            foreach (var k in Keep) if (p.StartsWith(k, System.StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>换章节时调：放掉上一章的地图、物件、人偶这些解码过的贴图（一张大图好几 MB 显存）；新章节还要用的（need，同 Prefetch 的路径写法）留着。
        /// 之后再要会重新解码，数据从微信文件缓存读，不走网络；画面上最多闪一两帧，换章节本来有黑场转场盖着。</summary>
        public static void ReleaseExcept(List<string> need, bool dropStored)
        {
            System.Func<string, bool> keep = p => { if (Kept(p)) return true; foreach (var n in need) if (n != null && Under(p, n)) return true; return false; };
            var drop = new List<string>();
            foreach (var p in decoded) if (!keep(p)) drop.Add(p);
            foreach (var p in drop)
            {
                Texture2D t;
                if (cache.TryGetValue(p, out t) && t != null) Object.Destroy(t);
                cache.Remove(p);
                decoded.Remove(p);
            }
            if (dropStored) foreach (var p in new List<string>(stored.Keys)) if (!keep(p)) { storedBytes -= stored[p].Length; stored.Remove(p); }   // 上一章预载了没用上的也放掉
        }

        /// <summary>导入设置照 PixelArtImport / PuppetImport：Map 下的像素图块点采样无 mipmap，手绘整图和人偶双线性 + mipmap。</summary>
        static Texture2D Decode(string path, byte[] bytes)
        {
            bool pixel = path.StartsWith("Map/") && !path.StartsWith("Map/Art/");
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, !pixel) { name = path };
            t.LoadImage(bytes, true);   // png、jpg 都认；不留 CPU 副本，省内存
            t.filterMode = pixel ? FilterMode.Point : FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }
    }
}
