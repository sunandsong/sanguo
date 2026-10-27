using UnityEngine;

namespace LiuBeiRPG.Game
{
    /// <summary>挂在主相机上的后处理（内置管线 OnRenderImage）：泛光、整体调色、暗角。OnGUI 画的界面不受影响。</summary>
    [RequireComponent(typeof(Camera))]
    public class PostFx : MonoBehaviour
    {
        public Color Grade = Color.white;    // 每张地图的色调（暖 / 冷 / 昏黄）
        public float Saturation = 1.08f;
        public float Contrast = 1.04f;
        public float Vignette = 0.3f;
        public float Bloom = 0.3f;
        public float Threshold = 0.78f;

        Material mat;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null) mat = new Material(Resources.Load<Shader>("Shaders/PostFx")) { hideFlags = HideFlags.HideAndDontSave };
            // 泛光在 1/4 分辨率上做，手机也吃得消
            var a = RenderTexture.GetTemporary(src.width / 4, src.height / 4, 0, src.format);
            var b = RenderTexture.GetTemporary(src.width / 4, src.height / 4, 0, src.format);
            mat.SetFloat("_Threshold", Threshold);
            Graphics.Blit(src, a, mat, 0);
            mat.SetVector("_Dir", new Vector4(1, 0));
            Graphics.Blit(a, b, mat, 1);
            mat.SetVector("_Dir", new Vector4(0, 1));
            Graphics.Blit(b, a, mat, 1);

            mat.SetTexture("_BloomTex", a);
            mat.SetFloat("_Bloom", Bloom);
            mat.SetColor("_Grade", Grade);
            mat.SetFloat("_Saturation", Saturation);
            mat.SetFloat("_Contrast", Contrast);
            mat.SetFloat("_Vignette", Vignette);
            Graphics.Blit(src, dst, mat, 2);
            RenderTexture.ReleaseTemporary(a);
            RenderTexture.ReleaseTemporary(b);
        }
    }
}
