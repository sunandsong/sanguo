using UnityEditor;
using UnityEngine;

/// <summary>Resources/Map 下的贴图：像素图块点采样、不压缩、不生成 mipmap，否则会糊。</summary>
public class PixelArtImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Map/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.alphaIsTransparency = true;
        // Map/Art 是手绘整图，要缩放显示：双线性 + mipmap；其余像素图块保持点采样
        bool art = assetPath.Contains("/Resources/Map/Art/");
        ti.filterMode = art ? FilterMode.Bilinear : FilterMode.Point;
        ti.mipmapEnabled = art;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.wrapMode = TextureWrapMode.Clamp;
    }
}
