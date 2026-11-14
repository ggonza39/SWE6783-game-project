using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// The level's North/South/East/West wall tiles are visual-only (SpriteRenderer
// with no Collider2D), so the player and all raycasts pass straight through
// the level boundary. This adds a matching BoxCollider2D to every wall tile
// without touching sprites, layout, or any other visual environment element.
[InitializeOnLoad]
public static class WallColliderSetup
{
    static WallColliderSetup()
    {
        if (Application.isBatchMode)
            return;

        EditorApplication.delayCall += SetupWallColliders;
    }

    [MenuItem("Pixel Rebellion/Setup Wall Colliders")]
    public static void SetupWallColliders()
    {
        // The authored demo already contains its complete gameplay setup.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path ==
            "Assets/Scenes/UXFacilityExpanded.unity")
            return;

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string[] wallNames = { "NorthWall", "SouthWall", "EastWall", "WestWall" };
        int added = 0;

        foreach (string wallName in wallNames)
        {
            foreach (GameObject wall in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (wall.name != wallName)
                    continue;

                if (wall.GetComponent<Collider2D>() != null)
                    continue;

                BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
                collider.isTrigger = false;
                EditorUtility.SetDirty(wall);
                added++;
            }
        }

        if (added > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"Wall collider setup completed successfully. Added {added} colliders.");
        }
    }

    // Batch-mode entry point (-executeMethod WallColliderSetup.ApplyToSampleScene):
    // explicitly opens SampleScene since batch mode otherwise starts on an
    // empty, untitled scene where GameObject.Find would find nothing.
    public static void ApplyToSampleScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SetupWallColliders();
    }
}
