using UnityEditor;
using UnityEngine;

namespace SailingOut.Editor
{
    public static class PixelArtImportTools
    {
        [MenuItem("Sailing Out/Apply Pixel Settings to Selected Textures")]
        public static void ApplyToSelectedTextures()
        {
            int updated = 0;
            foreach (Object selected in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16f;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                updated++;
            }

            Debug.Log($"Sailing Out: applied 16 PPU pixel settings to {updated} texture(s).");
        }

        [MenuItem("Sailing Out/Apply Pixel Settings to Selected Textures", true)]
        private static bool ValidateApplyToSelectedTextures()
        {
            foreach (Object selected in Selection.objects)
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(selected)) is TextureImporter)
                    return true;
            }

            return false;
        }
    }
}
