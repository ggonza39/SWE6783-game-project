using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Applies Assets/sprites/player/player_topdown.png to the existing Player.
public class PlayerSpriteSetup : AssetPostprocessor
{
    public const string SpritePath = "Assets/sprites/player/player_topdown.png";

    private const float TargetHeight = 1.5f;
    private const float PixelsPerUnit = 64f;

    private void OnPreprocessTexture()
    {
        if (assetPath != SpritePath)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
    }

    private static void OnPostprocessAllAssets(
        string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (string path in imported)
        {
            if (path == SpritePath)
            {
                EditorApplication.delayCall += () => ApplyToPlayer(false);
                return;
            }
        }
    }

    [MenuItem("Pixel Rebellion/Apply Player Sprite")]
    private static void ApplyFromMenu()
    {
        ApplyToPlayer(true);
    }

    // Returns true if the PNG sprite was applied to the existing Player.
    public static bool ApplyToPlayer(bool verbose)
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (verbose)
                Debug.LogWarning("Exit Play Mode before applying the player sprite.");
            return false;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

        if (sprite == null)
        {
            if (verbose)
                Debug.LogWarning("Player sprite not found. Place a PNG at " + SpritePath);
            return false;
        }

        GameObject player = GameObject.Find("Player");

        if (player == null)
        {
            if (verbose)
                Debug.LogWarning("No Player in the open scene. Open SampleScene first.");
            return false;
        }

        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();

        if (renderer == null)
            renderer = player.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 10;

        // Scale so the sprite is TargetHeight world units tall, and fit the collider to it.
        Vector2 size = sprite.bounds.size;
        float scale = TargetHeight / Mathf.Max(size.y, 0.0001f);
        player.transform.localScale = new Vector3(scale, scale, 1f);

        BoxCollider2D collider = player.GetComponent<BoxCollider2D>();

        if (collider != null)
        {
            collider.size = size;
            collider.offset = sprite.bounds.center;
        }

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("Player sprite applied from " + SpritePath);
        return true;
    }
}
