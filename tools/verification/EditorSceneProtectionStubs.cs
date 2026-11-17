using System;
using UnityEngine;
namespace UnityEditor {
 public class InitializeOnLoadAttribute:Attribute {}
 public class InitializeOnLoadMethodAttribute:Attribute {}
 public class EditorBuildSettingsScene {public string path;public bool enabled;public EditorBuildSettingsScene(string p,bool e){path=p;enabled=e;}}
 public static class EditorBuildSettings {public static EditorBuildSettingsScene[] scenes=new EditorBuildSettingsScene[0];}
 public class MenuItemAttribute:Attribute {public MenuItemAttribute(string name){}}
 public static class EditorApplication {public static bool isPlaying,isPlayingOrWillChangePlaymode;public static Action delayCall;}
 public static class EditorUtility {public static void SetDirty(UnityEngine.Object o){}}
 public static class AssetDatabase {
  public static T GetBuiltinExtraResource<T>(string path) where T:UnityEngine.Object,new()=>new T();
  public static UnityEngine.Object[] LoadAllAssetsAtPath(string path)=>new UnityEngine.Object[]{new UnityEngine.Object()};
 }
 public class SerializedObject {public SerializedObject(UnityEngine.Object o){}public SerializedProperty FindProperty(string p)=>new SerializedProperty();public void ApplyModifiedProperties(){}}
 public class SerializedProperty {public int arraySize;public string stringValue;public SerializedProperty GetArrayElementAtIndex(int i)=>this;public void InsertArrayElementAtIndex(int i){arraySize++;}}
}
namespace UnityEditor.SceneManagement {
 public static class EditorSceneManager {
  public static int Saves;
  public static bool SaveCurrentModifiedScenesIfUserWantsTo()=>true;
  public static UnityEngine.SceneManagement.Scene GetActiveScene()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  public static void MarkSceneDirty(UnityEngine.SceneManagement.Scene scene){}
  public static bool SaveOpenScenes(){Saves++;return true;}
  public static void OpenScene(string p){UnityEngine.SceneManagement.SceneManager.Active.path=p;}
 }
}
public static class EditorSceneProtectionChecks {
 public static int Run(){
  var types=new[]{typeof(EvidenceSetup),typeof(ExtractionSetup),typeof(GuardSetup),typeof(GuardDetectionSetup),typeof(SecurityCameraSetup),typeof(HidingSetup),typeof(WallColliderSetup)};
  var methods=new[]{"SetupEvidenceSystem","SetupExtractionSystem","SetupGuard","SetupDetection","SetupCamera","SetupHiding","SetupWallColliders"};
  UnityEngine.Object.All.Clear(); UnityEditor.SceneManagement.EditorSceneManager.Saves=0;
  foreach(string path in new[]{UXCampaign.FacilityScene,UXCampaign.CrimsonScene,"Assets/Scenes/UXFacilityPreview.unity"}){
   UnityEngine.SceneManagement.SceneManager.Active.path=path;
   for(int i=0;i<types.Length;i++)types[i].GetMethod(methods[i],System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
   if(UnityEngine.Object.All.Count!=0||UnityEditor.SceneManagement.EditorSceneManager.Saves!=0)throw new Exception("An authored scene startup callback created objects or saved the scene: "+path);
  }
  UnityEngine.SceneManagement.SceneManager.Active.path="Assets/Scenes/SampleScene.unity";
  for(int i=0;i<types.Length;i++)types[i].GetMethod(methods[i],System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
  if(GameObject.Find("Guard")==null||GameObject.Find("SecurityCamera")==null||GameObject.Find("Evidence")==null||GameObject.Find("HidingSpot")==null||UnityEditor.SceneManagement.EditorSceneManager.Saves==0)throw new Exception("Legacy scene setup changed outside the expanded demo.");
  return types.Length;
 }
}

public static class CampaignEditorChecks {
 public static void Run(string root){
  string assets=System.IO.Path.Combine(root,"src","Assets");Application.dataPath=assets;
  System.IO.Directory.CreateDirectory(System.IO.Path.Combine(assets,"Scenes"));
  foreach(string scene in new[]{"UXFacilityExpanded.unity","UXCrimsonDepths.unity"})System.IO.File.WriteAllText(System.IO.Path.Combine(assets,"Scenes",scene),"fixture");
  string settings=System.IO.Path.Combine(root,"src","ProjectSettings");System.IO.Directory.CreateDirectory(settings);
  System.IO.File.WriteAllText(System.IO.Path.Combine(settings,"EditorBuildSettings.asset"),"original build settings");
  Application.persistentDataPath=System.IO.Path.Combine(root,"profile");
  UnityEditor.EditorBuildSettings.scenes=new[]{new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/Other.unity",false),new UnityEditor.EditorBuildSettingsScene(UXCampaign.CrimsonScene,false),new UnityEditor.EditorBuildSettingsScene(UXCampaign.FacilityScene,false)};
  UXCampaignSceneSetup.RegisterScenes();var scenes=UnityEditor.EditorBuildSettings.scenes;
  if(scenes.Length!=3||scenes[0].path!=UXCampaign.FacilityScene||!scenes[0].enabled||scenes[1].path!=UXCampaign.CrimsonScene||!scenes[1].enabled||scenes[2].enabled||scenes[2].path!="Assets/Scenes/Other.unity")throw new Exception("Campaign build order or existing scene changed.");
  if(System.IO.Directory.GetFiles(Application.persistentDataPath,"EditorBuildSettings.asset",System.IO.SearchOption.AllDirectories).Length!=1)throw new Exception("Build settings backup missing.");
  UXCampaignSceneSetup.RegisterScenes();if(!object.ReferenceEquals(scenes,UnityEditor.EditorBuildSettings.scenes))throw new Exception("No-op registration rewrote settings.");
  UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode=true;UnityEditor.EditorBuildSettings.scenes=new UnityEditor.EditorBuildSettingsScene[0];UXCampaignSceneSetup.RegisterScenes();
  if(UnityEditor.EditorBuildSettings.scenes.Length!=0)throw new Exception("Registration changed settings while starting Play mode.");
  UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode=false;
 }
}
