using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Automatically applies and sizes the player sprite.
// No manual cropping, resizing, or dragging required.
public class PlayerSpriteSetup : AssetPostprocessor
{
    public const string SpritePath =
        "Assets/sprites/player/player_topdown.png";

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
        importer.textureCompression =
            TextureImporterCompression.Uncompressed;

        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        // Allows us to inspect the pixels automatically.
        importer.isReadable = true;
    }

    private static void OnPostprocessAllAssets(
        string[] imported,
        string[] deleted,
        string[] moved,
        string[] movedFrom)
    {
        foreach (string path in imported)
        {
            if (path == SpritePath)
            {
                EditorApplication.delayCall += () =>
                    ApplyToPlayer(false);

                return;
            }
        }
    }

    [MenuItem("Pixel Rebellion/Apply Player Sprite")]
    private static void ApplyFromMenu()
    {
        ApplyToPlayer(true);
    }

    public static bool ApplyToPlayer(bool verbose)
    {
        if (EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (verbose)
                Debug.LogWarning(
                    "Exit Play Mode before applying player sprite.");

            return false;
        }

        Sprite sprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);

        if (sprite == null || texture == null)
        {
            Debug.LogWarning(
                "Player sprite not found at " + SpritePath);

            return false;
        }

        GameObject player = GameObject.Find("Player");

        if (player == null)
        {
            Debug.LogWarning(
                "Player GameObject not found.");

            return false;
        }

        SpriteRenderer renderer =
            player.GetComponent<SpriteRenderer>();

        if (renderer == null)
            renderer = player.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 10;

        // --------------------------------------------------
        // AUTOMATICALLY FIND VISIBLE CHARACTER PIXELS
        // --------------------------------------------------

        Color32[] pixels = texture.GetPixels32();

        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;

        bool foundCharacter = false;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color32 pixel =
                    pixels[y * texture.width + x];

                // Ignore transparent pixels.
                if (pixel.a > 10)
                {
                    foundCharacter = true;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;

                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (!foundCharacter)
        {
            Debug.LogWarning(
                "No visible pixels found in player sprite.");

            return false;
        }

        // --------------------------------------------------
        // AUTOMATIC PLAYER SIZE
        // --------------------------------------------------

        int visibleWidthPixels =
            maxX - minX + 1;

        int visibleHeightPixels =
            maxY - minY + 1;

        float visibleWidthWorld =
            visibleWidthPixels / PixelsPerUnit;

        float visibleHeightWorld =
            visibleHeightPixels / PixelsPerUnit;

        float scale =
            TargetHeight /
            Mathf.Max(visibleHeightWorld, 0.0001f);

        player.transform.localScale =
            new Vector3(scale, scale, 1f);

        // --------------------------------------------------
        // AUTOMATIC COLLIDER SIZE
        // --------------------------------------------------

        BoxCollider2D collider =
            player.GetComponent<BoxCollider2D>();

        if (collider != null)
        {
            collider.size =
                new Vector2(
                    visibleWidthWorld,
                    visibleHeightWorld);

            float centerX =
                (
                    (minX + maxX + 1) / 2f -
                    texture.width / 2f
                ) / PixelsPerUnit;

            float centerY =
                (
                    (minY + maxY + 1) / 2f -
                    texture.height / 2f
                ) / PixelsPerUnit;

            collider.offset =
                new Vector2(centerX, centerY);
        }

        // --------------------------------------------------
        // SAVE EVERYTHING AUTOMATICALLY
        // --------------------------------------------------

        EditorUtility.SetDirty(player);

        EditorSceneManager.MarkSceneDirty(
            player.scene);

        EditorSceneManager.SaveOpenScenes();

        Debug.Log(
            "Player sprite automatically configured. " +
            "Visible pixels: " +
            visibleWidthPixels + "x" +
            visibleHeightPixels +
            " | Scale: " + scale);

        return true;
    }
}