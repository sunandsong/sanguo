using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LiuBeiRPG.Game
{
    /// <summary>
    /// 云存档（后端 Server/functions/liubei）。本地 PlayerPrefs 照存，这里只是多抄一份到云上：
    /// 存档时 Push；启动时 Pull，云上哪一档比本地新就覆盖本地。网络不通就当没有云，游戏照玩。
    /// 身份：微信里用 wx.login 的 code 换 openid（GetCode 由微信包里的 WxGlue 填），其他平台是游客，token 存本机。
    /// </summary>
    public static class Cloud
    {
        const string Api = "https://renshengqingdan-d9fc03opf3bac6ba-1478597699.ap-shanghai.app.tcloudbase.com/liubei/";
        const string TokenKey = "cloudToken";

        /// <summary>拿 wx.login 的 code（拿不到回调 null）；没设就是游客。</summary>
        public static Action<Action<string>> GetCode;

        [Serializable] class LoginRes { public bool ok; public string token; }
        [Serializable] class Slot { public string data; public long t; }
        [Serializable] class LoadRes { public bool ok; public Slot[] slots; }
        [Serializable] class SaveReq { public int slot; public string data; public long t; }

        public static long Now() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        public static string TimeKey(string saveKey) { return saveKey + "T"; }   // 本地每档的存档时间，和云上比新旧

        /// <summary>启动时调一次：云上更新的档写进 PlayerPrefs（key = saveKey(i)）。</summary>
        public static void Pull(Func<int, string> saveKey, Action done = null)
        {
            Call("load", "{}", json =>
            {
                var r = json == null ? null : JsonUtility.FromJson<LoadRes>(json);
                if (r != null && r.slots != null)
                    for (int i = 0; i < r.slots.Length; i++)
                    {
                        var s = r.slots[i];
                        if (s == null || string.IsNullOrEmpty(s.data)) continue;   // JsonUtility 把 null 读成空对象
                        var k = saveKey(i);
                        long local; long.TryParse(PlayerPrefs.GetString(TimeKey(k), "0"), out local);
                        if (s.t <= local) continue;
                        PlayerPrefs.SetString(k, s.data);
                        PlayerPrefs.SetString(TimeKey(k), s.t.ToString());
                    }
                PlayerPrefs.Save();
                if (done != null) done();
            });
        }

        public static void Push(int slot, string data, long t)
        {
            Call("save", JsonUtility.ToJson(new SaveReq { slot = slot, data = data, t = t }), null);
        }

        /// <summary>带 token 调接口；没 token 先登录；token 失效（401）重登一次。失败回调 null。</summary>
        static void Call(string path, string body, Action<string> done, bool retried = false)
        {
            var token = PlayerPrefs.GetString(TokenKey, "");
            if (token == "") { Login(ok => { if (ok) Call(path, body, done, true); else if (done != null) done(null); }); return; }
            Post(path, body, token, (code, text) =>
            {
                if (code == 401 && !retried) { PlayerPrefs.DeleteKey(TokenKey); Call(path, body, done, true); return; }
                if (done != null) done(code == 200 ? text : null);
            });
        }

        static void Login(Action<bool> done)
        {
            Action<string> withCode = code => Post("login", code == null ? "{}" : "{\"code\":\"" + code + "\"}", null, (status, text) =>
            {
                var r = status == 200 ? JsonUtility.FromJson<LoginRes>(text) : null;
                if (r != null && !string.IsNullOrEmpty(r.token)) { PlayerPrefs.SetString(TokenKey, r.token); PlayerPrefs.Save(); }
                done(r != null && !string.IsNullOrEmpty(r.token));
            });
            if (GetCode != null) GetCode(withCode); else withCode(null);
        }

        static void Post(string path, string body, string token, Action<long, string> done)
        {
            var req = new UnityWebRequest(Api + path, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 10,
            };
            req.SetRequestHeader("Content-Type", "application/json");
            if (token != null) req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SendWebRequest().completed += _ =>
            {
                long code = req.result == UnityWebRequest.Result.ConnectionError ? 0 : req.responseCode;
                string text = req.downloadHandler.text;
                req.Dispose();
                done(code, text);
            };
        }
    }
}
