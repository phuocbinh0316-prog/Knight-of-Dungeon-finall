using UnityEditor;
using UnityEngine;

namespace Knight.Editor
{
    /// <summary>
    /// Tự động cấu hình Texture Importer chuẩn Pixel Art (Point, Uncompressed, 32 PPU, Pivot đúng gót chân)
    /// cho các sprite Quái Tinh Anh Địa Ngục trong Assets/Resources/HellElite/.
    /// </summary>
    public class HellEliteSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            string normPath = assetPath.Replace('\\', '/');
            bool isElite = normPath.Contains("/Resources/HellElite/");
            bool isMini = normPath.Contains("/Resources/HellMini/");
            bool isRoguelite = normPath.Contains("/Resources/Roguelite/");
            if (!isElite && !isMini && !isRoguelite) return;

            var importer = assetImporter as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = isRoguelite ? 16f : 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            if (isElite)
            {
                settings.spritePivot = new Vector2(0.53125f, 0.0116f); // Canvas 96x86 cho Quái Tinh Anh
            }
            else if (isMini)
            {
                settings.spritePivot = new Vector2(0.48f, 0.09375f); // Canvas 64x64 gót chân y=58 cho Quái Mini
            }
            else
            {
                // Roguelite props
                bool isScroll = normPath.Contains("scroll");
                settings.spritePivot = isScroll ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.0f);
            }
            importer.SetTextureSettings(settings);
        }
    }
}
