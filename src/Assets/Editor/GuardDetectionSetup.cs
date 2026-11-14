using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class GuardDetectionSetup
{
    static GuardDetectionSetup()
    {
        if (Application.isBatchMode)
            return;

        EditorApplication.delayCall += SetupDetection;
    }

    private static void SetupDetection()
    {
        // The authored demo already contains its complete gameplay setup.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path ==
            "Assets/Scenes/UXFacilityExpanded.unity")
            return;

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject guard = GameObject.Find("Guard");

        if (guard == null)
        {
            Debug.LogWarning("Guard not found.");
            return;
        }

        if (guard.GetComponent<GuardDetection>() == null)
        {
            guard.AddComponent<GuardDetection>();
            EditorUtility.SetDirty(guard);
        }

        Debug.Log("Guard detection setup completed successfully.");
    }
}