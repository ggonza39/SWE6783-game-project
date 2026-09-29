using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class GuardDetectionSetup
{
    static GuardDetectionSetup()
    {
        EditorApplication.delayCall += SetupDetection;
    }

    private static void SetupDetection()
    {
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