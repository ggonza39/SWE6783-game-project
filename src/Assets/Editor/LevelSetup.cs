using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class LevelSetup
{
    [MenuItem("Pixel Rebellion/Setup Level Bounds")]
    public static void SetupLevelBounds()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        // Remove old bounds if the setup is run again
        GameObject existingBounds = GameObject.Find("LevelBounds");

        if (existingBounds != null)
        {
            Object.DestroyImmediate(existingBounds);
        }

        // Parent object for all walls
        GameObject bounds = new GameObject("LevelBounds");

        CreateWall(
            "TopWall",
            new Vector2(0f, 5f),
            new Vector2(18f, 1f),
            bounds.transform
        );

        CreateWall(
            "BottomWall",
            new Vector2(0f, -5f),
            new Vector2(18f, 1f),
            bounds.transform
        );

        CreateWall(
            "LeftWall",
            new Vector2(-9f, 0f),
            new Vector2(1f, 10f),
            bounds.transform
        );

        CreateWall(
            "RightWall",
            new Vector2(9f, 0f),
            new Vector2(1f, 10f),
            bounds.transform
        );

        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene()
        );

        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = bounds;

        Debug.Log("Level collision bounds created successfully.");
    }

    private static void CreateWall(
        string wallName,
        Vector2 position,
        Vector2 size,
        Transform parent)
    {
        GameObject wall = new GameObject(wallName);

        wall.transform.SetParent(parent);
        wall.transform.position = position;

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }
}