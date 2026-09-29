using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class EvidenceSetup
{
    static EvidenceSetup()
    {
        EditorApplication.delayCall += SetupEvidenceSystem;
    }

    private static void SetupEvidenceSystem()
    {
        // Evidence Manager
        GameObject manager = GameObject.Find("EvidenceManager");

        if (manager == null)
        {
            manager = new GameObject("EvidenceManager");
            manager.AddComponent<EvidenceManager>();
        }

        // Evidence collectible
        GameObject evidence = GameObject.Find("Evidence");

        if (evidence == null)
        {
            evidence = CreateVisibleObject(
                "Evidence",
                new Vector3(-2f, -2f, 0f),
                new Vector3(0.7f, 0.7f, 1f),
                Color.yellow
            );

            BoxCollider2D collider =
                evidence.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;

            evidence.AddComponent<EvidenceCollectible>();
        }

        // Interactive terminal
        GameObject terminal = GameObject.Find("Terminal");

        if (terminal == null)
        {
            terminal = CreateVisibleObject(
                "Terminal",
                new Vector3(3f, -2f, 0f),
                new Vector3(1.2f, 1.5f, 1f),
                Color.green
            );

            BoxCollider2D collider =
                terminal.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;

            terminal.AddComponent<TerminalInteraction>();
        }

        // Make sure Player has the Player tag
        GameObject player = GameObject.Find("Player");

        if (player != null)
            player.tag = "Player";

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        EditorSceneManager.SaveOpenScenes();

        Debug.Log(
            "Evidence and terminal system setup completed successfully."
        );
    }

    private static GameObject CreateVisibleObject(
        string objectName,
        Vector3 position,
        Vector3 scale,
        Color color)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.position = position;
        obj.transform.localScale = scale;

        SpriteRenderer renderer =
            obj.AddComponent<SpriteRenderer>();

        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        texture.SetPixels(pixels);
        texture.Apply();

        renderer.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, 32, 32),
            new Vector2(0.5f, 0.5f),
            32
        );

        return obj;
    }
}