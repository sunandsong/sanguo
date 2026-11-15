using UnityEngine;
using WeChatWASM;

// 只在微信工程（Build/_wxproj）里有：给云存档接上 wx.login，换 openid 当身份；剪贴板接 wx.setClipboardData。由 Tools/build_wx.sh 拷过去。
static class WxGlue
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        LiuBeiRPG.Game.Cloud.GetCode = done => WX.Login(new LoginOption
        {
            success = r => done(r.code),
            fail = e => done(null),   // 拿不到就当游客
        });
        // 关于我们「点此复制邮箱」：WebGL 里 systemCopyBuffer 不通，用微信的剪贴板（成功时微信自己弹提示）
        LiuBeiRPG.Game.GameRoot.CopyText = text => WX.SetClipboardData(new SetClipboardDataOption { data = text });
    }
}
