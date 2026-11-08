#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;

public static class UXFacilityPreviewBuilder
{
    const string Preview = "Assets/Scenes/UXFacilityPreview.unity";
    const string Art = "Assets/UXFacilityArt";
    const string Root = "UX_Facility_Visuals";

    [MenuItem("Pixel Rebellion/Build UX Facility Preview")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode first.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != Preview)
        {
            EditorUtility.DisplayDialog("Open preview scene",
                "Open Assets/Scenes/UXFacilityPreview.unity first.", "OK");
            return;
        }

        if (GameObject.Find(Root) != null)
        {
            EditorUtility.DisplayDialog("Already built",
                "Facility visuals already exist. Nothing overwritten.", "OK");
            return;
        }

        string[] names = {
            "Player", "Guard", "SecurityCamera", "Evidence",
            "Terminal", "HidingSpot", "ExtractionZone"
        };

        foreach (string name in names)
            if (GameObject.Find(name) == null)
            {
                Debug.LogError("Missing object: " + name);
                return;
            }

        if (!AssetDatabase.IsValidFolder(Art))
            AssetDatabase.CreateFolder("Assets", "UXFacilityArt");

        Sprite floor = Make("floor", 32, (x,y) =>
            x==0 || y==0 ? C(26,43,62) :
            ((x+y*7)%41==0 ? C(64,82,102) : C(42,59,79)));

        Sprite floorAlt = Make("floor_alt", 32, (x,y) =>
            x==0 || y==0 ? C(25,40,59) :
            ((x+y*9)%47==0 ? C(68,85,104) : C(35,52,73)));

        Sprite wall = Make("wall", 32, (x,y) =>
            y>26 ? C(119,149,178) :
            y<4 ? C(15,27,42) :
            x<3 || x>28 ? C(40,61,87) :
            (y==25 || y==5 ? C(85,110,141) : C(57,78,105)));

        Sprite server = Make("server", 32, (x,y) =>
            x<3 || x>28 || y<3 || y>28 ? C(11,21,36) :
            x<6 || x>25 ? C(63,84,104) :
            y%9==0 ? C(80,111,133) :
            y%9>=2 && y%9<=4 && x>9 && x<22
                ? C(19,167,223) : C(21,37,54));

        Sprite crate = Make("crate", 32, (x,y) =>
            x<3 || x>28 || y<3 || y>28 ? C(19,29,42) :
            x<7 || x>24 || y<7 || y>24 ? C(158,116,72) :
            Math.Abs(x-y)<2 || Math.Abs(x+y-31)<2
                ? C(206,154,91) : C(103,72,51));

        Sprite terminal = Make("terminal", 32, (x,y) =>
            x<5 || x>26 || y<5 || y>26 ? Clear() :
            x<8 || x>23 || y>22 ? C(55,79,100) :
            y>=9 && y<20 ? C(18,167,194) : C(15,34,50));

        Sprite evidence = Make("evidence", 32, (x,y) =>
            x<5 || x>26 || y<7 || y>25 ? Clear() :
            x<8 || x>23 || y<10 || y>22 ? C(255,215,86) :
            C(170,119,28));

        Sprite exit = Make("exit", 32, (x,y) =>
            x<3 || x>28 || y<3 || y>28 ? Clear() :
            x<6 || x>25 || y<6 || y>25 ? C(53,227,139) :
            y>=14 && y<=18 && x>=8 && x<=23
                ? C(210,255,227) : C(14,83,62));

        Sprite lamp = Make("lamp", 32, (x,y) =>
            x>=5 && x<=26 && y>=13 && y<=19
                ? (y>=15 && y<=17 ? C(255,216,117) : C(137,94,42))
                : Clear());

        Sprite operative = Make("operative", 4, (x,y) =>
            x>=11 && x<=21 && y>=8 && y<=14 ? C(20,28,40) :
            x>=9 && x<=23 && y>=14 && y<=23 ? C(30,95,111) :
            x>=11 && x<=21 && y>=23 && y<=29 ? C(23,40,65) :
            Clear());

        Sprite guard = Make("security_guard", 32, (x,y) =>
            x>=10 && x<=22 && y>=8 && y<=15 ? C(25,33,46) :
            x>=8 && x<=24 && y>=15 && y<=24 ? C(64,80,100) :
            x>=10 && x<=22 && y>=24 && y<=30 ? C(245,144,69) :
            Clear());

        GameObject old = GameObject.Find("EasyLevel_VisualEnvironment");
        if (old != null)
            old.SetActive(false);

        GameObject root = new GameObject(Root);

        for (int y=-4; y<=4; y++)
            for (int x=-8; x<=8; x++)
                Tile(root.transform, "Floor", x, y,
                    (x+y)%4==0 ? floorAlt : floor, -20, false);

        for (int x=-9; x<=9; x++)
        {
            Tile(root.transform, "NorthWall", x, 5, wall, 1, true);
            Tile(root.transform, "SouthWall", x, -5, wall, 1, true);
        }

        for (int y=-4; y<=4; y++)
        {
            Tile(root.transform, "WestWall", -9, y, wall, 1, true);
            Tile(root.transform, "EastWall", 9, y, wall, 1, true);
        }

        // Interior room partitions with openings.
        for (int x=-8; x<=-5; x++)
            Tile(root.transform, "ServerRoomWall", x, 1, wall, 1, true);

        for (int y=2; y<=4; y++)
            Tile(root.transform, "ServerRoomSide", -4, y, wall, 1, true);

        for (int x=-2; x<=2; x++)
            if (x!=0)
            {
                Tile(root.transform, "OfficeNorth", x, 1, wall, 1, true);
                Tile(root.transform, "OfficeSouth", x, -1, wall, 1, true);
            }

        Tile(root.transform, "OfficeWest", -2, 0, wall, 1, true);
        Tile(root.transform, "OfficeEast", 2, 0, wall, 1, true);

        for (int x=4; x<=7; x++)
            if (x!=6)
                Tile(root.transform, "ControlRoomWall", x, 1, wall, 1, true);

        for (int x=3; x<=7; x++)
            if (x!=5)
                Tile(root.transform, "ServiceRoomWall", x, -3, wall, 1, true);

        for (int x=-8; x<=-5; x++)
            if (x!=-6)
                Tile(root.transform, "StorageWall", x, -3, wall, 1, true);

        for (int x=-7; x<=-5; x++)
            Tile(root.transform, "ServerRack", x, 3, server, 4, true);

        for (int x=4; x<=6; x++)
            Tile(root.transform, "ServerRack", x, -4, server, 4, true);

        Tile(root.transform, "CoverCrate", -7, -3, crate, 5, true);
        Tile(root.transform, "CoverCrate", 3, -1, crate, 5, true);
        Tile(root.transform, "CoverCrate", 7, 0, crate, 5, true);

        Vector2[] lights = {
            new Vector2(-7,4), new Vector2(-3,2),
            new Vector2(0,2), new Vector2(4,4),
            new Vector2(7,3), new Vector2(-7,-4),
            new Vector2(1,-4), new Vector2(6,-3)
        };

        foreach (Vector2 p in lights)
            Tile(root.transform, "AmberWallLight",
                p.x, p.y, lamp, 8, false);

        // Preserve gameplay components while repositioning objects
        // in this separate preview scene.
        Place("Player", -6, -2, operative, 10);
        Place("Guard", 0, 3, guard, 10);
        Place("SecurityCamera", 5, 3, null, 10);
        Place("Evidence", -4, -2, evidence, 9);
        Place("Terminal", -2, -2, terminal, 9);
        Place("HidingSpot", 1, -2, crate, 9);
        Place("ExtractionZone", 7, -2, exit, 9);

        GameObject player = GameObject.Find("Player");
        BoxCollider2D body = player.GetComponent<BoxCollider2D>();
        if (body != null)
        {
            Vector3 scale = player.transform.lossyScale;
            body.size = new Vector2(
                0.5f / Mathf.Max(0.01f, Mathf.Abs(scale.x)),
                0.7f / Mathf.Max(0.01f, Mathf.Abs(scale.y))
            );
            body.offset = Vector2.zero;
        }

        GameObject evidence2 = UnityEngine.Object.Instantiate(
            GameObject.Find("Evidence"),
            new Vector3(0,0,0), Quaternion.identity);
        evidence2.name = "Evidence_Secondary";

        GameObject hide2 = UnityEngine.Object.Instantiate(
            GameObject.Find("HidingSpot"),
            new Vector3(-6,0,0), Quaternion.identity);
        hide2.name = "HidingSpot_Secondary";
        hide2.tag = "HidingSpot";

        Collider2D hideCollider = hide2.GetComponent<Collider2D>();
        if (hideCollider != null)
            hideCollider.isTrigger = true;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.transform.position = new Vector3(0,0,-10);
            cam.backgroundColor = new Color(0.035f,0.065f,0.10f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Include preview in Editor Build Settings so Restart works.
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        if (!scenes.Exists(s => s.path == Preview))
        {
            scenes.Add(new EditorBuildSettingsScene(Preview,true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("UX Facility preview built successfully.");

        EditorUtility.DisplayDialog("Preview built",
            "Saved UXFacilityPreview.unity. Inspect Scene view before Play.",
            "OK");
    }

    static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r,g,b,255);
    }

    static Color32 Clear()
    {
        return new Color32(0,0,0,0);
    }

    static Sprite Make(string name, int ppu,
        Func<int,int,Color32> painter)
    {
        string path = Art + "/" + name + ".png";
        string diskPath = Path.Combine(
            Application.dataPath, "UXFacilityArt", name + ".png");

        if (!File.Exists(diskPath))
        {
            Texture2D t = new Texture2D(
                32,32,TextureFormat.RGBA32,false);

            Color32[] pixels = new Color32[32*32];

            for (int y=0; y<32; y++)
                for (int x=0; x<32; x++)
                    pixels[y*32+x] = painter(x,y);

            t.SetPixels32(pixels);
            t.Apply();

            File.WriteAllBytes(diskPath,t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }

        AssetDatabase.ImportAsset(
            path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter imp =
            (TextureImporter)AssetImporter.GetAtPath(path);

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        Sprite result = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (result == null)
            throw new Exception("Could not generate " + path);

        return result;
    }

    static GameObject Tile(Transform parent, string name,
        float x, float y, Sprite sprite, int order, bool solid)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent,false);
        go.transform.position = new Vector3(x,y,0);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;

        if (solid)
        {
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.98f,0.98f);
        }

        return go;
    }

    static void Place(string name, float x, float y,
        Sprite sprite, int order)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
            throw new Exception("Missing " + name);

        go.transform.position = new Vector3(x,y,0);

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = go.AddComponent<SpriteRenderer>();

        if (sprite != null)
        {
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = order;
        }
    }
}
#endif