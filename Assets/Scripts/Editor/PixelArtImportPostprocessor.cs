using UnityEditor;
using UnityEngine;

namespace Rootbound.Editor
{
    /// <summary>
    /// Configura automaticamente l'importazione nitida (Point, senza compressione, PPU 16)
    /// e i pivot corretti per tutti gli sprite pixel art sotto Assets/Art/.
    /// </summary>
    public sealed class PixelArtImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/"))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = assetPath.StartsWith("Assets/Art/Backgrounds/")
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = 16f;

            bool bottomCenter =
                assetPath.StartsWith("Assets/Art/Player/") ||
                assetPath.StartsWith("Assets/Art/Enemies/") ||
                assetPath.StartsWith("Assets/Art/Boss/") ||
                (assetPath.StartsWith("Assets/Art/Props/") && !assetPath.Contains("root_anchor"));

            if (bottomCenter)
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
            }

            if (assetPath.EndsWith("ui_panel.png"))
            {
                settings.spriteBorder = new Vector4(4f, 4f, 4f, 4f);
            }

            importer.SetTextureSettings(settings);
        }
    }
}
