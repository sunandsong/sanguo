using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WeChatWASM;

public static class WxBuild
{
    // 命令行导出微信小游戏到 Build/wxgame（webgl + minigame 两个目录）。由 Tools/build_wx.sh 拷进 Build/_wxproj 再跑
    public static void Build()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Main.unity", true) };
        var c = WXConvertCore.config;
        c.ProjectConf.projectName = "刘备英雄传";
        c.ProjectConf.Appid = "wxd2614ebe0d3da219";
        c.ProjectConf.DST = "/Users/songzhang/Desktop/LiuBeiRPG-Demo/Build/wxgame";
        c.ProjectConf.relativeDST = "../wxgame";
        c.ProjectConf.CDN = Environment.GetEnvironmentVariable("WX_CDN") ?? "";
        c.ProjectConf.bundlePathIdentifier = "StreamingAssets;liubei/;";   // 云上的图（Remote.cs）也走微信的文件缓存，下过一次就不再下
        var f = c.ProjectConf.GetType().GetField("Orientation");
        f.SetValue(c.ProjectConf, Enum.Parse(f.FieldType, Enum.GetNames(f.FieldType).First(n => n.StartsWith("Portrait"))));
        // 界面用 IMGUI，内置 GUISkin 是 MonoBehaviour 资源，要 MonoScript（类 ID 115）；剥引擎代码会把它剥掉，
        // 运行时刷「Could not produce class with ID 115」「Copied style is null」，所以关掉
        PlayerSettings.stripEngineCode = false;
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        var r = WXConvertCore.DoExport();
        Debug.Log("WXEXPORT " + r);
        EditorApplication.Exit(r == WXConvertCore.WXExportError.SUCCEED ? 0 : 1);
    }
}
