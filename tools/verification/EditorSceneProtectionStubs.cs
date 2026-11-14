using System;
using UnityEngine;
namespace UnityEditor {
 public class InitializeOnLoadAttribute:Attribute {}
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
  UnityEngine.SceneManagement.SceneManager.Active.path="Assets/Scenes/UXFacilityExpanded.unity";
  for(int i=0;i<types.Length;i++)types[i].GetMethod(methods[i],System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
  if(UnityEngine.Object.All.Count!=0||UnityEditor.SceneManagement.EditorSceneManager.Saves!=0)throw new Exception("An expanded scene startup callback created objects or saved the scene.");
  UnityEngine.SceneManagement.SceneManager.Active.path="Assets/Scenes/SampleScene.unity";
  for(int i=0;i<types.Length;i++)types[i].GetMethod(methods[i],System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Invoke(null,null);
  if(GameObject.Find("Guard")==null||GameObject.Find("SecurityCamera")==null||GameObject.Find("Evidence")==null||GameObject.Find("HidingSpot")==null||UnityEditor.SceneManagement.EditorSceneManager.Saves==0)throw new Exception("Legacy scene setup changed outside the expanded demo.");
  return types.Length;
 }
}
