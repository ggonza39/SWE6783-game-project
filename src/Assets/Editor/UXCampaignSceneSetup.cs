#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UXCampaignSceneSetup
{
    [InitializeOnLoadMethod]
    private static void Schedule() { EditorApplication.delayCall += RegisterScenes; }

    [MenuItem("Pixel Rebellion/Campaign/Register Campaign For Builds")]
    public static void RegisterScenes()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode ||
            !File.Exists(Path.Combine(Application.dataPath,"..",UXCampaign.FacilityScene)) ||
            !File.Exists(Path.Combine(Application.dataPath,"..",UXCampaign.CrimsonScene)))return;
        var scenes=new List<EditorBuildSettingsScene> {
            new EditorBuildSettingsScene(UXCampaign.FacilityScene,true),
            new EditorBuildSettingsScene(UXCampaign.CrimsonScene,true) };
        foreach(EditorBuildSettingsScene old in EditorBuildSettings.scenes)
            if(old.path!=UXCampaign.FacilityScene && old.path!=UXCampaign.CrimsonScene)scenes.Add(old);
        bool same=scenes.Count==EditorBuildSettings.scenes.Length;
        for(int i=0;same && i<scenes.Count;i++)same=scenes[i].path==EditorBuildSettings.scenes[i].path &&
            scenes[i].enabled==EditorBuildSettings.scenes[i].enabled;
        if(same)return;
        try
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..",".."));
            string backup=Path.Combine(Directory.Exists(Path.Combine(root,".git"))?Path.Combine(root,".git"):
                Application.persistentDataPath,"pixel-rebellion-campaign-build-backups",DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
            Directory.CreateDirectory(backup);
            string settings=Path.Combine(Application.dataPath,"..","ProjectSettings","EditorBuildSettings.asset");
            if(File.Exists(settings))File.Copy(settings,Path.Combine(backup,"EditorBuildSettings.asset"));
        }
        catch(Exception exception){Debug.LogWarning("Campaign build-settings backup: "+exception.Message);}
        EditorBuildSettings.scenes=scenes.ToArray();
        Debug.Log("Campaign scenes registered. Existing additional scene entries were preserved.");
    }

    [MenuItem("Pixel Rebellion/Campaign/Open Crimson Depths")]
    public static void OpenCrimson()
    {
        if(!EditorApplication.isPlayingOrWillChangePlaymode && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(UXCampaign.CrimsonScene);
    }
}
#endif
