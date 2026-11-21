// Importa los PNG de Huerta Nueva como pixel art: sin filtrado, sin compresión y sin mipmaps.
using UnityEditor;

namespace HuertaNueva.Editor
{
    public class TexturasImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/HuertaNueva/Resources/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = 32;
            t.filterMode = UnityEngine.FilterMode.Point;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.mipmapEnabled = false;
            t.npotScale = TextureImporterNPOTScale.None;
            t.alphaIsTransparency = true;
            t.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        }
    }
}
