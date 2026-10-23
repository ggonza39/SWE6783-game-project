using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class GuardSetup
{
    static GuardSetup()
    {
        EditorApplication.delayCall += SetupGuard;
    }

    private static void SetupGuard()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject guard = GameObject.Find("Guard");

        if (guard == null)
        {
            guard = new GameObject("Guard");
            guard.transform.position = new Vector3(-3f, 2f, 0f);

            SpriteRenderer renderer = guard.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            guard.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            guard.AddComponent<BoxCollider2D>();
            guard.AddComponent<GuardController>();

            Debug.Log("Guard setup completed successfully.");
        }
    }
}