#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UXExpandedDemoMenu
{
    private const string ScenePath = "Assets/Scenes/UXFacilityExpanded.unity";

    [MenuItem("Pixel Rebellion/Demo/Open Expanded Level 1")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Pixel Rebellion/Demo/Use Expanded Level 1 For Build")]
    public static void PrepareBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        string backup = Path.Combine(root, ".git", "pixel-rebellion-expanded-build-backups",
            DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(backup);
        File.Copy(Path.Combine(Application.dataPath, "..", "ProjectSettings", "EditorBuildSettings.asset"),
            Path.Combine(backup, "EditorBuildSettings.asset"));
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.path != ScenePath) scenes.Add(scene);
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Expanded Level 1 is first in the build. Previous scene entries preserved. Backup: " + backup);
    }
}
#endif
