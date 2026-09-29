using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class HidingSetup
{
    static HidingSetup()
    {
        EditorApplication.delayCall += SetupHiding;
    }

    private static void SetupHiding()
    {
        GameObject player = GameObject.Find("Player");

        if (player != null && player.GetComponent<PlayerHiding>() == null)
        {
            player.AddComponent<PlayerHiding>();
            EditorUtility.SetDirty(player);
        }

        GameObject hidingSpot = GameObject.Find("HidingSpot");

        if (hidingSpot == null)
        {
            hidingSpot = new GameObject("HidingSpot");
            hidingSpot.transform.position = new Vector3(0f, 3f, 0f);

            SpriteRenderer renderer = hidingSpot.AddComponent<SpriteRenderer>();

            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.gray;

            texture.SetPixels(pixels);
            texture.Apply();

            renderer.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, 32, 32),
                new Vector2(0.5f, 0.5f),
                32
            );

            hidingSpot.transform.localScale =
                new Vector3(2f, 2f, 1f);

            BoxCollider2D collider =
                hidingSpot.AddComponent<BoxCollider2D>();

            collider.isTrigger = true;

            // Automatically create and assign HidingSpot tag
            SerializedObject tagManager =
                new SerializedObject(
                    AssetDatabase.LoadAllAssetsAtPath(
                        "ProjectSettings/TagManager.asset"
                    )[0]
                );

            SerializedProperty tags =
                tagManager.FindProperty("tags");

            bool tagExists = false;

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == "HidingSpot")
                {
                    tagExists = true;
                    break;
                }
            }

            if (!tagExists)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue =
                    "HidingSpot";

                tagManager.ApplyModifiedProperties();
            }

            hidingSpot.tag = "HidingSpot";
        }

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        EditorSceneManager.SaveOpenScenes();

        Debug.Log("Hiding mechanic setup completed successfully.");
    }
}