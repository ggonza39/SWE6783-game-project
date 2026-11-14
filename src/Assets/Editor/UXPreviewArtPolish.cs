#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;

public static class UXPreviewArtPolish
{
    const string ScenePath = "Assets/Scenes/UXFacilityPreview.unity";
    const string ArtFolder = "Assets/UXFacilityArt";
    const string GroupName = "UX_Neon_Detail_Pass";

    [MenuItem("Pixel Rebellion/UX Preview/Add Neon Detail Pass")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            scene.path != ScenePath)
        {
            EditorUtility.DisplayDialog("Open the preview",
                "Stop Play Mode and open UXFacilityPreview.unity first.",
                "OK");
            return;
        }

        if (GameObject.Find("UX_Facility_Visuals") == null)
        {
            EditorUtility.DisplayDialog("Build facility first",
                "The facility scene has not been built yet.", "OK");
            return;
        }

        if (GameObject.Find(GroupName) != null)
        {
            EditorUtility.DisplayDialog("Already installed",
                "The detail pass already exists. Nothing duplicated.",
                "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(ArtFolder))
            AssetDatabase.CreateFolder("Assets", "UXFacilityArt");

        Sprite blue = Make("ux_blue_glow",64,32,
            (x,y) => Glow(x,y,36,207,255));

        Sprite gold = Make("ux_amber_glow",64,32,
            (x,y) => Glow(x,y,255,183,68));

        Sprite green = Make("ux_green_glow",64,32,
            (x,y) => Glow(x,y,53,255,133));

        Sprite red = Make("ux_red_glow",64,32,
            (x,y) => Glow(x,y,255,65,75));

        Sprite hazard = Make("ux_hazard_floor",32,32,(x,y) =>
        {
            if (x<2 || x>29 || y<10 || y>21) return Clear();
            bool yellow = ((x+y)/6)%2==0;
            if (y==10 || y==21) return C(196,148,47,210);
            return yellow ? C(205,148,42,200) : C(18,28,37,220);
        });

        Sprite vent = Make("ux_floor_vent",32,32,(x,y) =>
        {
            if (x<3 || x>28 || y<5 || y>26) return Clear();
            if (x<6 || x>25 || y<8 || y>23)
                return C(78,104,131);
            return y%5<=1 ? C(102,133,157) : C(13,30,45);
        });

        Sprite route = Make("ux_route_chevrons",32,32,(x,y) =>
        {
            if (x<5 || x>27 || y<8 || y>24) return Clear();
            int u=(x-5)%12;
            int v=Mathf.Abs(y-16);
            bool line=Mathf.Abs(v-(9-u))<=2 && u<=9;
            return line ? C(31,230,225,222) : Clear();
        });

        Sprite lamp = Make("ux_amber_lamp",32,32,(x,y) =>
        {
            if (x<3 || x>28 || y<9 || y>22) return Clear();
            if (x<6 || x>25 || y<12 || y>19)
                return C(26,33,49);
            if (y>=14 && y<=18) return C(255,217,113);
            return C(169,107,44);
        });

        Sprite panel = Make("ux_technical_panel",32,32,(x,y) =>
        {
            if (x<2 || x>29 || y<2 || y>29) return Clear();
            if (x<5 || x>26 || y<5 || y>26)
                return C(83,109,134);
            if (x>=10 && x<=23 && (y==11 || y==22))
                return C(41,175,207);
            if (x==8 || x==24 || y==8 || y==24)
                return C(41,62,89);
            return C(21,38,58);
        });

        Sprite door = Make("ux_extraction_arch",64,32,(x,y) =>
        {
            int dx=Mathf.Abs(x-31);
            if (y<5 || y>58 || dx>29) return Clear();
            if (dx>24 || y>53 || y<10)
                return C(19,42,50);
            if (dx>19 || y>49 || y<14)
                return C(41,234,144);
            if (dx>17 || y>47 || y<16)
                return C(8,79,57);
            if (x>=29 && x<=34 && y>=22 && y<=39)
                return C(154,255,207);
            if (x>=35 && x<=45 &&
                Mathf.Abs(y-30)<=(45-x)/2)
                return C(154,255,207);
            return C(6,33,36);
        });

        GameObject parent = new GameObject(GroupName);

        for (int x=-8; x<=8; x+=2)
        {
            Put(parent,"Blue Perimeter",-5,x,4.52f,blue,0.95f);
            Put(parent,"Blue Perimeter",-5,x,-4.52f,blue,0.95f);
        }

        Vector2[] lights = {
            new Vector2(-7,4),new Vector2(-3,2),
            new Vector2(0,2),new Vector2(4,4),
            new Vector2(7,3),new Vector2(-7,-4),
            new Vector2(1,-4),new Vector2(6,-3)
        };

        foreach (Vector2 p in lights)
        {
            Put(parent,"Amber Light Spill",-3,p.x,p.y,gold,1.8f);
            Put(parent,"Wall Fixture",7,p.x,p.y,lamp,0.67f);
        }

        Put(parent,"Server Glow",-4,-6,3,blue,2.4f);
        Put(parent,"Server Glow",-4,5,-4,blue,2.4f);
        Put(parent,"Evidence Light",6,-4,-2,gold,1.1f);
        Put(parent,"Evidence Light",6,0,0,gold,1.1f);
        Put(parent,"Terminal Glow",6,-2,-2,blue,1.2f);
        Put(parent,"Extraction Glow",6,7,-2,green,2.2f);
        Put(parent,"Extraction Door",8,7,-2,door,0.85f);
        Put(parent,"Security Warning",3,5,3,red,0.95f);

        Vector2[] thresholds = {
            new Vector2(-7,1.2f),
            new Vector2(-5,1.2f),
            new Vector2(0,-1.18f),
            new Vector2(0,1.18f),
            new Vector2(6,1.2f),
            new Vector2(5,-2.82f),
            new Vector2(-6,-2.84f),
            new Vector2(7,-1.15f)
        };

        foreach (Vector2 p in thresholds)
            Put(parent,"Hazard Threshold",-7,
                p.x,p.y,hazard,0.9f);

        Vector2[] arrows = {
            new Vector2(-5,-2),
            new Vector2(-3,-2),
            new Vector2(1,-2),
            new Vector2(4,-2),
            new Vector2(6,-2)
        };

        foreach (Vector2 p in arrows)
            Put(parent,"Cyan Floor Route",-8,
                p.x,p.y-0.38f,route,0.70f);

        Vector2[] vents = {
            new Vector2(-8,0),
            new Vector2(-3,0),
            new Vector2(3,3),
            new Vector2(8,2),
            new Vector2(-3,-4),
            new Vector2(3,-4)
        };

        foreach (Vector2 p in vents)
            Put(parent,"Floor Vent",-9,p.x,p.y,vent,0.68f);

        Vector2[] panels = {
            new Vector2(-8,3),
            new Vector2(7,4),
            new Vector2(-8,-4),
            new Vector2(8,-4)
        };

        foreach (Vector2 p in panels)
            Put(parent,"Wall Detail",2,p.x,p.y,panel,0.72f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Detail pass added",
            "Facility details saved. Inspect the Scene view before Play.",
            "OK");
    }

    static void Put(GameObject parent,string name,int order,
        float x,float y,Sprite sprite,float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform,false);
        go.transform.position = new Vector3(x,y,0);
        go.transform.localScale = Vector3.one*scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
    }

    static Color32 C(byte r,byte g,byte b,byte a=255)
    {
        return new Color32(r,g,b,a);
    }

    static Color32 Clear()
    {
        return new Color32(0,0,0,0);
    }

    static Color32 Glow(int x,int y,byte r,byte g,byte b)
    {
        float dx=(x-31.5f)/32f;
        float dy=(y-31.5f)/32f;
        float t=Mathf.Clamp01(1f-Mathf.Sqrt(dx*dx+dy*dy));
        return C(r,g,b,(byte)Mathf.RoundToInt(90*t*t));
    }

    static Sprite Make(string name,int size,int ppu,
        Func<int,int,Color32> painter)
    {
        string path=ArtFolder+"/"+name+".png";
        string disk=Path.Combine(
            Application.dataPath,"UXFacilityArt",name+".png");

        if (!File.Exists(disk))
        {
            Texture2D tex=new Texture2D(
                size,size,TextureFormat.RGBA32,false);

            Color32[] pixels=new Color32[size*size];

            for(int y=0;y<size;y++)
                for(int x=0;x<size;x++)
                    pixels[y*size+x]=painter(x,y);

            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(disk,tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        AssetDatabase.ImportAsset(
            path,ImportAssetOptions.ForceSynchronousImport);

        TextureImporter imp=
            (TextureImporter)AssetImporter.GetAtPath(path);

        imp.textureType=TextureImporterType.Sprite;
        imp.spriteImportMode=SpriteImportMode.Single;
        imp.spritePixelsPerUnit=ppu;
        imp.filterMode=FilterMode.Point;
        imp.alphaIsTransparency=true;
        imp.mipmapEnabled=false;
        imp.textureCompression=
            TextureImporterCompression.Uncompressed;

        imp.SaveAndReimport();

        Sprite result=AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if(result==null)
            throw new Exception("Could not import "+path);

        return result;
    }
}
#endif