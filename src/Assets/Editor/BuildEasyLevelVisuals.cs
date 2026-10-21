#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Visual upgrade for the existing Pixel Rebellion easy-level scene.
/// Uses the original gameplay objects and scripts; does not alter game rules.</summary>
public static class BuildEasyLevelVisuals
{
    private const string Folder = "Assets/GeneratedEasyLevel";
    private const string DecorRoot = "EasyLevel_VisualEnvironment";

    [MenuItem("Pixel Rebellion/Build Visual Easy Level")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Stop Play Mode before building the level.", "OK");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "SampleScene")
        {
            EditorUtility.DisplayDialog("Open SampleScene", "Open SampleScene before building the easy level.", "OK");
            return;
        }
        string[] required = { "Player", "Guard", "SecurityCamera", "Evidence", "Terminal", "ExtractionZone", "HidingSpot", "GameManager", "EvidenceManager" };
        foreach (string name in required)
        {
            if (GameObject.Find(name) == null)
            {
                EditorUtility.DisplayDialog("Missing object", "Scene is missing " + name + ". No changes were made.", "OK");
                return;
            }
        }
        if (!EditorUtility.DisplayDialog("Build Easy Level", "Create a top-down stealth facility with illustrated gameplay objects. Existing scripts and player artwork are preserved. Existing generated visual environment will be replaced. Save the scene only after checking it.", "Build", "Cancel")) return;

        EnsureFolder();
        Sprite floor = MakeSprite("facility_floor", 32, 32, (x,y) =>
        {
            bool seam = x == 0 || y == 0;
            bool speck = (x * 19 + y * 37) % 47 == 0;
            return seam ? new Color32(27, 44, 64, 255) : speck ? new Color32(55, 76, 94, 255) : new Color32(43, 63, 82, 255);
        });
        Sprite wall = MakeSprite("wall_panel", 32, 32, (x,y) =>
        {
            if (y > 26) return new Color32(104, 135, 152, 255);
            if (y < 4) return new Color32(21, 35, 52, 255);
            if (x == 0 || x == 31) return new Color32(35, 50, 65, 255);
            return new Color32(65, 86, 104, 255);
        });
        Sprite guard = MakeSprite("guard_agent", 32, 32, (x,y) =>
        {
            float dx = x-15.5f, dy = y-15.5f;
            if (dx*dx + dy*dy < 46 && y > 17) return new Color32(210, 83, 84, 255);
            if (x >= 8 && x <= 23 && y >= 5 && y <= 17) return new Color32(113, 35, 50, 255);
            if (x >= 11 && x <= 20 && y >= 8 && y <= 12) return new Color32(39, 48, 62, 255);
            if (x >= 11 && x <= 20 && y >= 20 && y <= 22) return new Color32(25, 33, 45, 255);
            return new Color32(0,0,0,0);
        });
        Sprite camera = MakeSprite("surveillance_camera", 32, 32, (x,y) =>
        {
            if (x>=4 && x<=24 && y>=12 && y<=21) return new Color32(171, 192, 205, 255);
            if (x>=19 && x<=28 && y>=14 && y<=19) return new Color32(40, 53, 65, 255);
            if (x>=25 && x<=29 && y>=15 && y<=18) return new Color32(82, 225, 234, 255);
            if (x>=11 && x<=15 && y>=5 && y<=12) return new Color32(111, 136, 151, 255);
            return new Color32(0,0,0,0);
        });
        Sprite evidence = MakeSprite("evidence_chip", 32, 32, (x,y) =>
        {
            if (x>=6 && x<=25 && y>=5 && y<=26)
            {
                if (x==6 || x==25 || y==5 || y==26) return new Color32(255, 224, 94, 255);
                if (x>=11 && x<=20 && y>=11 && y<=20) return new Color32(250, 206, 63, 255);
                return new Color32(67, 88, 106, 255);
            }
            return new Color32(0,0,0,0);
        });
        Sprite terminal = MakeSprite("computer_terminal", 32, 32, (x,y) =>
        {
            if (x>=4 && x<=27 && y>=9 && y<=26)
            {
                if (x>=7 && x<=24 && y>=13 && y<=23) return new Color32(39, 206, 186, 255);
                return new Color32(22, 31, 44, 255);
            }
            if (x>=11 && x<=20 && y>=4 && y<=9) return new Color32(149, 173, 185, 255);
            return new Color32(0,0,0,0);
        });
        Sprite hiding = MakeSprite("storage_crate", 32, 32, (x,y) =>
        {
            if (x>=3 && x<=28 && y>=3 && y<=28)
            {
                if (x<7 || x>24 || y<7 || y>24) return new Color32(143, 100, 63, 255);
                if (Mathf.Abs(x-y)<2 || Mathf.Abs(x+y-31)<2) return new Color32(170, 125, 78, 255);
                return new Color32(103, 72, 50, 255);
            }
            return new Color32(0,0,0,0);
        });
        Sprite extraction = MakeSprite("exit_marker", 32, 32, (x,y) =>
        {
            if (x>=3 && x<=28 && y>=3 && y<=28)
            {
                if (x<6 || x>25 || y<6 || y>25) return new Color32(73, 236, 142, 255);
                if (y>=14 && y<=18 && x>=9 && x<=21) return new Color32(210, 255, 225, 255);
                if (x>=18 && x<=23 && Mathf.Abs(y-16)<=x-18) return new Color32(210, 255, 225, 255);
                return new Color32(20, 92, 65, 255);
            }
            return new Color32(0,0,0,0);
        });

        GameObject previous = GameObject.Find(DecorRoot);
        if (previous != null) Undo.DestroyObjectImmediate(previous);
        GameObject root = new GameObject(DecorRoot);
        Undo.RegisterCreatedObjectUndo(root, "Build easy level visuals");
        for (int x=-8; x<=8; x++)
            for (int y=-5; y<=5; y++)
                Tile(root.transform, "Floor", floor, x, y, -20);
        for (int x=-8; x<=8; x++)
        {
            Tile(root.transform, "NorthWall", wall, x, 5, -10);
            Tile(root.transform, "SouthWall", wall, x, -5, -10);
        }
        for (int y=-4; y<=4; y++)
        {
            Tile(root.transform, "WestWall", wall, -8, y, -10);
            Tile(root.transform, "EastWall", wall, 8, y, -10);
        }
        // Existing gameplay objects remain intact. These coordinates establish
        // a clear left-to-right tutorial route with a separate surveillance lane.
        Place("Player", -6, -2, null, 0);
        Place("Evidence", -4, -2, evidence, 3);
        Place("Terminal", -2, -2, terminal, 3);
        Place("HidingSpot", 1, -2, hiding, 3);
        Place("ExtractionZone", 6, -2, extraction, 3);
        Place("Guard", 0, 2, guard, 3);
        Place("SecurityCamera", 5, 2, camera, 3);
        // Keep guard movement in the upper patrol lane, away from spawn.
        GuardController patrol = GameObject.Find("Guard").GetComponent<GuardController>();
        if (patrol != null)
        {
            Undo.RecordObject(patrol, "Set easy patrol distance");
            SerializedObject so = new SerializedObject(patrol);
            SerializedProperty prop = so.FindProperty("patrolDistance");
            if (prop != null) { prop.floatValue = 2f; so.ApplyModifiedProperties(); }
        }
        EnsureTag("Player"); EnsureTag("HidingSpot");
        GameObject.Find("Player").tag = "Player";
        GameObject.Find("HidingSpot").tag = "HidingSpot";
        foreach (string name in new [] { "Evidence", "Terminal", "HidingSpot", "ExtractionZone" })
        {
            GameObject obj = GameObject.Find(name);
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col == null) col = Undo.AddComponent<BoxCollider2D>(obj);
            Undo.RecordObject(col, "Set gameplay trigger");
            col.isTrigger = true;
        }
        Camera main = Camera.main;
        if (main != null)
        {
            Undo.RecordObject(main, "Frame easy level");
            main.orthographic = true;
            main.orthographicSize = 6.2f;
            main.transform.position = new Vector3(0,0,-10);
            main.backgroundColor = new Color(0.07f, 0.10f, 0.15f);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Easy Level Visuals Built", "Illustrated facility, guard, camera, evidence, terminal, crate and exit are now in SampleScene. Existing gameplay scripts were preserved. Press Play to test. Save the scene only after verifying it.", "OK");
        Debug.Log("Pixel Rebellion easy-level visuals built. Verify guard/camera orientation, triggers, full evidence-to-extraction flow, and game over in Play Mode.");
    }

    private static void Place(string name, float x, float y, Sprite sprite, int sortingOrder)
    {
        GameObject obj = GameObject.Find(name);
        Undo.RecordObject(obj.transform, "Position " + name);
        obj.transform.position = new Vector3(x,y,0);
        if (sprite == null) return; // Preserve the actual player artwork.
        SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<SpriteRenderer>(obj);
        Undo.RecordObject(renderer, "Set " + name + " sprite");
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = sortingOrder;
        obj.transform.localScale = Vector3.one;
    }
    private static void Tile(Transform parent, string name, Sprite sprite, float x, float y, int order)
    {
        GameObject tile = new GameObject(name);
        tile.transform.SetParent(parent);
        tile.transform.position = new Vector3(x,y,0);
        SpriteRenderer r = tile.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.sortingOrder = order;
    }
    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "GeneratedEasyLevel");
    }
    private static Sprite MakeSprite(string name, int w, int h, System.Func<int,int,Color32> pixel)
    {
        string path = Folder + "/" + name + ".png";
        Texture2D texture = new Texture2D(w,h,TextureFormat.RGBA32,false);
        texture.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[w*h];
        for (int y=0;y<h;y++) for(int x=0;x<w;x++) pixels[y*w+x] = pixel(x,y);
        texture.SetPixels32(pixels); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void EnsureTag(string tag)
    {
        foreach (string t in UnityEditorInternal.InternalEditorUtility.tags) if (t == tag) return;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0) return;
        SerializedObject manager = new SerializedObject(assets[0]);
        SerializedProperty tags = manager.FindProperty("tags");
        if (tags == null) return;
        int i = tags.arraySize;
        tags.InsertArrayElementAtIndex(i);
        tags.GetArrayElementAtIndex(i).stringValue = tag;
        manager.ApplyModifiedProperties();
    }
}
#endif
