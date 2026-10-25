using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildMac
{
    // 命令行打包：创建空场景（GameRoot.Boot 会自动启动游戏）并输出 macOS 程序
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        EditorSceneManager.SaveScene(scene, "Assets/Main.unity");
        PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        var r = BuildPipeline.BuildPlayer(new[] { "Assets/Main.unity" }, "Build/LiuBeiRPG.app", BuildTarget.StandaloneOSX, BuildOptions.None);
        EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
