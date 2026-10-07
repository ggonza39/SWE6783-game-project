using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SecurityCameraSetup
{
    static SecurityCameraSetup()
    {
        EditorApplication.delayCall += SetupCamera;
    }

    private static void SetupCamera()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject cameraObject = GameObject.Find("SecurityCamera");

        if (cameraObject == null)
        {
            cameraObject = new GameObject("SecurityCamera");
            cameraObject.transform.position = new Vector3(4f, 2f, 0f);

            SpriteRenderer renderer =
                cameraObject.AddComponent<SpriteRenderer>();

            renderer.sprite =
                AssetDatabase.GetBuiltinExtraResource<Sprite>(
                    "UI/Skin/Knob.psd"
                );

            cameraObject.transform.localScale =
                new Vector3(1.2f, 1.2f, 1f);

            cameraObject.AddComponent<SecurityCamera>();

            EditorSceneManager.MarkSceneDirty(
                EditorSceneManager.GetActiveScene()
            );

            EditorSceneManager.SaveOpenScenes();

            Debug.Log(
                "Security camera setup completed successfully."
            );
        }
    }
}