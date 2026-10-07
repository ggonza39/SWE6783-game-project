using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ExtractionSetup
{
    static ExtractionSetup()
    {
        EditorApplication.delayCall += SetupExtractionSystem;
    }

    private static void SetupExtractionSystem()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Create Game Manager
        GameObject gameManager = GameObject.Find("GameManager");

        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();
        }

        // Create Extraction Zone
        GameObject extractionZone = GameObject.Find("ExtractionZone");

        if (extractionZone == null)
        {
            extractionZone = new GameObject("ExtractionZone");
            extractionZone.transform.position =
                new Vector3(6f, -3f, 0f);

            extractionZone.transform.localScale =
                new Vector3(2f, 2f, 1f);

            SpriteRenderer renderer =
                extractionZone.AddComponent<SpriteRenderer>();

            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.blue;

            texture.SetPixels(pixels);
            texture.Apply();

            renderer.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 32, 32),
                new Vector2(0.5f, 0.5f),
                32
            );

            BoxCollider2D collider =
                extractionZone.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;

            extractionZone.AddComponent<ExtractionZone>();
        }

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        EditorSceneManager.SaveOpenScenes();

        Debug.Log(
            "Extraction and game manager setup completed successfully."
        );
    }
}