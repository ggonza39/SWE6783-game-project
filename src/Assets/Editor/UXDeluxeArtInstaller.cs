#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UXDeluxeArtInstaller
{
    private const string PreviewPath = "Assets/Scenes/UXFacilityPreview.unity";
    private const string Folder = "Assets/Resources/UXArtDeluxe";
    private const string Marker = "UX_Deluxe_Illustrated_Art_Applied";

    [MenuItem("Pixel Rebellion/UX Preview/Apply Deluxe Pixel Art Pack")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != PreviewPath)
        {
            EditorUtility.DisplayDialog("Open UX facility preview",
                "Exit Play Mode, then open UXFacilityPreview.unity.", "OK");
            return;
        }

        if (GameObject.Find(Marker) != null)
        {
            EditorUtility.DisplayDialog("Already applied",
                "The illustrated art pack is already installed in this preview.", "OK");
            return;
        }

        GameObject root = GameObject.Find("UX_Facility_Visuals");
        GameObject details = GameObject.Find("UX_Neon_Detail_Pass");
        GameObject player = GameObject.Find("Player");
        GameObject guard = GameObject.Find("Guard");

        if (root == null || details == null || player == null || guard == null)
        {
            EditorUtility.DisplayDialog("Preview not ready",
                "Open the previously built and polished UXFacilityPreview scene.", "OK");
            return;
        }

        // Turn all PNGs into point-filtered sprites. Never modify original assets.
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { Folder });
        if (guids.Length < 44)
        {
            EditorUtility.DisplayDialog("Art pack incomplete",
                "Expected 44 PNG assets under Assets/Resources/UXArtDeluxe. Found " + guids.Length + ".", "OK");
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            int ppu = System.IO.Path.GetFileName(path).StartsWith("deluxe_") ? 64 : 96;
            bool change = importer.textureType != TextureImporterType.Sprite ||
                importer.spritePixelsPerUnit != ppu ||
                importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (change)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        Sprite test = Get("player_down_0");
        Sprite sampleWall = Get("deluxe_wall");
        if (test == null || sampleWall == null)
        {
            EditorUtility.DisplayDialog("Art import problem",
                "Character or wall sprites could not be loaded. Check Unity Console.", "OK");
            return;
        }

        int updated = 0;
        int floorCount = 0;
        foreach (Transform child in root.transform)
        {
            string name = child.name;
            string asset = null;
            if (name == "Floor") asset = (++floorCount % 6 == 0) ? "deluxe_floor_alt" : "deluxe_floor";
            else if (name == "AmberWallLight") asset = "deluxe_lamp";
            else if (name.Contains("Wall") || name == "ServerRoomSide") asset = "deluxe_wall";
            else if (name == "ServerRack") asset = "deluxe_server";
            else if (name == "CoverCrate") asset = "deluxe_crate";
            if (asset != null && SetSprite(child.gameObject, asset)) updated++;
        }

        foreach (Transform child in details.transform)
        {
            string asset = null;
            if (child.name == "Floor Vent") asset = "deluxe_vent";
            else if (child.name == "Wall Detail") asset = "deluxe_panel";
            else if (child.name == "Hazard Threshold") asset = "deluxe_hazard";
            else if (child.name == "Wall Fixture") asset = "deluxe_lamp";
            if (asset != null && SetSprite(child.gameObject, asset)) updated++;
        }

        foreach (string name in new string[] { "Evidence", "Evidence_Secondary" })
            if (SetSprite(GameObject.Find(name), "deluxe_evidence")) updated++;
        if (SetSprite(GameObject.Find("Terminal"), "deluxe_terminal")) updated++;
        foreach (string name in new string[] { "HidingSpot", "HidingSpot_Secondary" })
            if (SetSprite(GameObject.Find(name), "deluxe_crate")) updated++;
        if (SetSprite(GameObject.Find("ExtractionZone"), "deluxe_exit")) updated++;

        if (AddCharacterArt(player, "player")) updated++;
        if (AddCharacterArt(guard, "guard")) updated++;

        GameObject marker = new GameObject(Marker);
        Undo.RegisterCreatedObjectUndo(marker, "Apply UX deluxe art");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("UX Deluxe Art: applied " + updated + " sprite updates. Gameplay, walls and colliders unchanged.");
        EditorUtility.DisplayDialog("Deluxe pixel art applied",
            "Updated " + updated + " visual objects. The illustrated characters are visible before Play and animate in Play Mode.\n\nExisting gameplay scripts and colliders were not modified.",
            "OK");
    }

    private static Sprite Get(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
    }

    private static bool SetSprite(GameObject go, string asset)
    {
        if (go == null) return false;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        Sprite sprite = Get(asset);
        if (renderer == null || sprite == null) return false;
        Undo.RecordObject(renderer, "Apply UX illustrated sprite");
        renderer.sprite = sprite;
        renderer.color = Color.white;
        EditorUtility.SetDirty(renderer);
        return true;
    }

    private static bool AddCharacterArt(GameObject go, string kind)
    {
        Sprite sprite = Get(kind + "_down_0");
        if (sprite == null) return false;

        SpriteRenderer original = go.GetComponent<SpriteRenderer>();
        if (original != null)
        {
            Undo.RecordObject(original, "Hide placeholder character art");
            original.enabled = false;
            EditorUtility.SetDirty(original);
        }

        Transform child = go.transform.Find("UX_Deluxe_PreviewArt");
        if (child == null)
        {
            GameObject art = new GameObject("UX_Deluxe_PreviewArt");
            Undo.RegisterCreatedObjectUndo(art, "Create illustrated character art");
            art.transform.SetParent(go.transform, false);
            child = art.transform;
        }

        SpriteRenderer display = child.GetComponent<SpriteRenderer>();
        if (display == null) display = Undo.AddComponent<SpriteRenderer>(child.gameObject);
        Undo.RecordObject(display, "Assign illustrated character art");
        display.sprite = sprite;
        display.sortingOrder = kind == "guard" ? 19 : 20;
        display.color = Color.white;
        display.enabled = true;

        // Preserve parent Transform, Rigidbody2D and BoxCollider2D completely.
        Vector3 scale = go.transform.lossyScale;
        float height = kind == "guard" ? 1.25f : 1.35f;
        float imageScale = height / 0.67f;
        child.localPosition = new Vector3(0, 0, -0.02f);
        child.localScale = new Vector3(
            imageScale / Mathf.Max(0.01f, Mathf.Abs(scale.x)),
            imageScale / Mathf.Max(0.01f, Mathf.Abs(scale.y)), 1);
        child.rotation = Quaternion.identity;

        if (go.GetComponent<UXDeluxeSpriteAnimator>() == null)
            Undo.AddComponent<UXDeluxeSpriteAnimator>(go);
        return true;
    }
}
#endif
