using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>手机效果预览用的打包：网页版、iPhone 模拟器版。</summary>
public static class BuildPreview
{
    // 菜单打包网页版，输出到 Build/WebGL。预览：在该目录运行 python3 -m http.server 8000，
    // Chrome 打开 http://localhost:8000，开发者工具里切到手机模式（Cmd+Shift+M）。
    [MenuItem("LiuBeiRPG/打包网页版 (WebGL)")]
    public static void WebGL()
    {
        // ponytail: 本地预览不压缩，省得配服务器的压缩响应头；上微信时改由转换插件处理
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        var r = BuildPipeline.BuildPlayer(new[] { "Assets/Main.unity" }, "Build/WebGL", BuildTarget.WebGL, BuildOptions.None);
        if (r.summary.result == BuildResult.Succeeded) EditorUtility.RevealInFinder("Build/WebGL/index.html");
    }

    // 命令行调用（见 Tools/run_ios_sim.sh）：导出 iPhone 模拟器用的 Xcode 工程到 Build/iOS
    public static void IOSSimulator()
    {
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
        PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
        // 横竖屏都允许，模拟器里旋转就能看两种布局
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        var r = BuildPipeline.BuildPlayer(new[] { "Assets/Main.unity" }, "Build/iOS", BuildTarget.iOS, BuildOptions.None);
        EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
