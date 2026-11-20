using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Small Unity Editor helper for the Dead Cells-inspired tileset.
/// Copy the whole DeadCells_Unity_Tileset folder somewhere under Assets/.
/// Then use Tools > Dead Cells Tileset > Generate Terrain Tile Assets.
/// </summary>
public static class DeadCellsTilesetSetup
{
    // Change this path only if you place the folder somewhere else.
    private const string TilesetRoot = "Assets/Art/DeadCells_Unity_Tileset";
    private const float PixelsPerUnit = 88f;

    [MenuItem("Tools/Dead Cells Tileset/Generate Terrain Tile Assets")]
    public static void GenerateTerrainTileAssets()
    {
        string sourceFolder = TilesetRoot + "/Terrain_Tiles";
        string outputFolder = TilesetRoot + "/Generated_TileAssets";

        if (!AssetDatabase.IsValidFolder(sourceFolder))
        {
            Debug.LogError("Tileset folder not found: " + sourceFolder +
                           ". Copy DeadCells_Unity_Tileset under Assets/Art, or update TilesetRoot in this script.");
            return;
        }

        EnsureFolder(outputFolder);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolder });
        int created = 0;

        foreach (string guid in guids)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .FirstOrDefault();

            if (sprite == null)
            {
                Debug.LogWarning("Could not load Sprite from: " + texturePath);
                continue;
            }

            string tilePath = outputFolder + "/" + Path.GetFileNameWithoutExtension(texturePath) + ".asset";
            Tile existing = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (existing != null)
            {
                existing.sprite = sprite;
                existing.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(existing);
            }
            else
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = Path.GetFileNameWithoutExtension(texturePath);
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dead Cells tileset: processed " + created + " terrain sprites. Tile assets are in " + outputFolder +
                  ". Set Collider Type to None for decorative/floating pieces that should not block the player.");
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
