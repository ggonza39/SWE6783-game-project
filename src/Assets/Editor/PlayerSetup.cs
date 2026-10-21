using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlayerSetup
{
    [MenuItem("Pixel Rebellion/Setup Player")]
    public static void SetupPlayer()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject existingPlayer = GameObject.Find("Player");

        // Never recreate the Player: keep its components and scene references intact.
        if (existingPlayer != null)
        {
            PlayerSpriteSetup.ApplyToPlayer(true);
            return;
        }

        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;
        player.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        player.tag = "Player";

        // Create visible temporary player sprite
        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();

        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.cyan;

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, 32, 32),
            new Vector2(0.5f, 0.5f),
            32
        );

        renderer.sprite = sprite;
        renderer.sortingOrder = 10;

        // Physics
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        player.AddComponent<BoxCollider2D>();

        // Movement controller
        player.AddComponent<PlayerController>();

        PlayerSpriteSetup.ApplyToPlayer(false);

        Selection.activeGameObject = player;

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        EditorSceneManager.SaveOpenScenes();

        Debug.Log("Player setup completed successfully.");
    }
}