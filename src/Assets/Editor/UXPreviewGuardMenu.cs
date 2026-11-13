#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UXPreviewGuardMenu
{
    private const string Preview =
        "Assets/Scenes/UXFacilityPreview.unity";

    [MenuItem("Pixel Rebellion/UX Preview/Enable Two-Way Guard Patrol")]
    public static void EnablePatrol()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.path != Preview)
        {
            EditorUtility.DisplayDialog(
                "Wrong scene",
                "Open UXFacilityPreview.unity first.",
                "OK");
            return;
        }

        GameObject guard = GameObject.Find("Guard");

        if (guard == null ||
            guard.GetComponent<GuardController>() == null ||
            guard.GetComponent<GuardDetection>() == null)
        {
            EditorUtility.DisplayDialog(
                "Guard not ready",
                "Could not find the original GuardController and GuardDetection.",
                "OK");
            return;
        }

        if (guard.GetComponent<UXPreviewGuardFacingPatrol>() != null)
        {
            EditorUtility.DisplayDialog(
                "Already installed",
                "Two-way patrol is already attached.",
                "OK");
            return;
        }

        Undo.AddComponent<UXPreviewGuardFacingPatrol>(guard);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            "UX two-way patrol installed in preview only. " +
            "Original gameplay source files unchanged.");

        EditorUtility.DisplayDialog(
            "Guard patrol installed",
            "The guard will now walk in both directions, " +
            "pause to turn, and point its detection cone " +
            "where it is facing. Press Play to test.",
            "OK");
    }
}
#endif