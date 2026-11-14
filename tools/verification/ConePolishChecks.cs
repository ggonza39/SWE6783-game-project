using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

// Isolated API/geometry checks. Real Unity physics, GPU rendering and frame rate
// still require the engine playtest; the ray provider below models solid boxes.
public static class ConePolishChecks
{
    private static void Check(bool pass, string message) { if (!pass) throw new Exception(message); }
    private static void Call(object target, string method) => target.GetType().GetMethod(method,
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field,
        BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field,
        BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Reset()
    {
        Object.All.Clear();
        foreach (Type type in new[] { typeof(GameManager), typeof(UXStealthDirector), typeof(EvidenceManager) })
            type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        Time.timeScale = 1f; Time.deltaTime = 1f / 60f; Physics2D.SightQuery = Query;
    }
    private static BoxCollider2D Box(string name, float x, float y, float width, float height)
    {
        var go = new GameObject(name); go.transform.position = new Vector3(x,y);
        var box = go.AddComponent<BoxCollider2D>(); box.size = new Vector2(width,height); return box;
    }
    private static RaycastHit2D[] Query(Vector2 origin, Vector2 direction, float range)
    {
        var hits = new List<RaycastHit2D>();
        foreach (BoxCollider2D box in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (!box.enabled || !box.gameObject.activeSelf) continue;
            float near = 0, far = range; Bounds bounds = box.bounds;
            bool intersects = true;
            for (int axis=0;axis<2;axis++)
            {
                float p = axis==0?origin.x:origin.y, d = axis==0?direction.x:direction.y;
                float low = axis==0?bounds.min.x:bounds.min.y, high = axis==0?bounds.max.x:bounds.max.y;
                if (Math.Abs(d)<1e-8f) { if (p<low || p>high) intersects=false; continue; }
                float a=(low-p)/d,b=(high-p)/d;
                near=Math.Max(near,Math.Min(a,b));far=Math.Min(far,Math.Max(a,b));
                if (far<near) intersects=false;
            }
            if (intersects) hits.Add(new RaycastHit2D {collider=box,distance=near});
        }
        hits.Sort((a,b)=>a.distance.CompareTo(b.distance)); return hits.ToArray();
    }

    public static string CameraLifecycle()
    {
        Reset();
        var player = new GameObject("Player"); player.tag="Player";
        player.AddComponent<PlayerController>(); player.transform.position=new Vector3(100,100);
        var manager = new GameObject("Manager").AddComponent<GameManager>(); Call(manager,"Awake");
        var director = new GameObject("Director").AddComponent<UXStealthDirector>(); Call(director,"Awake");
        director.BeginMission(UXDifficulty.Hard);
        var camera = new GameObject("SecurityCamera").AddComponent<SecurityCamera>();
        camera.transform.rotation=Quaternion.Euler(0,0,225);
        Set(camera,"rotationSpeed",24f);Set(camera,"sweepAngle",35f);
        Time.time=9876f; Call(camera,"Start");
        var renderer=camera.GetComponent<DetectionConeRenderer>();Call(renderer,"Awake");
        Call(camera,"Update");float first=camera.transform.eulerAngles.z;
        Check(Math.Abs(Mathf.DeltaAngle(225,first))<0.5f,"Camera jumped when deployment started.");
        Time.time=999999f;Call(camera,"Update");float beforePause=camera.transform.eulerAngles.z;
        Check(Math.Abs(Mathf.DeltaAngle(first,beforePause))<0.5f,"Camera sweep depends on global time.");
        Set(camera,"<IsPlayerDetected>k__BackingField",true);
        Time.timeScale=0;Call(camera,"Update");
        Check(camera.IsPlayerDetected && camera.transform.eulerAngles.z==beforePause,"Pause changed camera pose/feedback.");
        Time.timeScale=1;camera.SetDisabled(true);Call(camera,"Update");Call(renderer,"LateUpdate");
        Check(!camera.ConeActive && !camera.IsPlayerDetected &&
            !Field<MeshRenderer>(renderer,"meshRenderer").enabled && camera.transform.eulerAngles.z==beforePause,
            "Hacked camera retained a cone, detection, or sweep.");
        camera.SetDisabled(false);Call(camera,"Update");Call(renderer,"LateUpdate");
        Check(camera.ConeActive && Field<MeshRenderer>(renderer,"meshRenderer").enabled &&
            Math.Abs(Mathf.DeltaAngle(beforePause,camera.transform.eulerAngles.z))<0.5f,"Camera resumed with a jump.");
        manager.LoseGame();Call(renderer,"LateUpdate");
        Check(!Field<MeshRenderer>(renderer,"meshRenderer").enabled,"Ended mission retained a camera cone.");
        Physics2D.SightQuery=null;
        return "deployment/resume continuity; pause freezes pose and feedback; hack removes cone/detection; mission-end cleanup";
    }

    private sealed class Detector : IConeDetector
    {
        public float angle;
        public Vector2 ConeOrigin=>Vector2.zero;
        public Vector2 ConeForward=>Quaternion.Euler(0,0,angle)*Vector3.right;
        public float ConeRange=>6.21f;
        public float ConeFovAngle=>66f;
        public Color ConeColor=>Color.yellow;
        public bool ConeActive=>true;
    }
    private static Vector2[] Points(DetectionConeRenderer cone)
    {
        var renderer=Field<MeshRenderer>(cone,"meshRenderer");var mesh=Field<Mesh>(cone,"mesh");
        var result=new Vector2[mesh.vertices.Length-1];
        for(int i=0;i<result.Length;i++) result[i]=renderer.transform.TransformPoint(mesh.vertices[i+1]);
        return result;
    }
    private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    private static float PolygonDistance(Vector2[] points,Vector2 direction)
    {
        float found=float.PositiveInfinity;
        for(int i=0;i<points.Length-1;i++)
        {
            Vector2 a=points[i], edge=points[i+1]-a;float denominator=Cross(direction,edge);
            if(Math.Abs(denominator)<1e-8f)continue;
            float distance=Cross(a,edge)/denominator,along=Cross(a,direction)/denominator;
            if(distance>=0 && along>=-0.00001f && along<=1.00001f) found=Math.Min(found,distance);
        }
        Check(!float.IsInfinity(found),"Cone polygon contains a missing wedge.");return found;
    }
    public static string MeshAndOcclusion(string directory)
    {
        Reset();
        var renderer=new GameObject("Cone").AddComponent<DetectionConeRenderer>();Call(renderer,"Awake");
        var detector=new Detector();renderer.Detector=detector;
        var wall=Box("Crate",2.6f,0.35f,0.7f,0.7f);
        var guard=Box("CrossingGuard",1f,0f,0.6f,0.7f);guard.gameObject.AddComponent<GuardController>();
        var player=Box("MovingPlayer",0.7f,0f,0.5f,0.7f);player.gameObject.AddComponent<PlayerController>();
        for(int i=0;i<10;i++)Box("Trigger",0.15f+i*0.12f,0f,0.05f,0.8f).isTrigger=true;
        float distance=DetectionUtility.GetSightDistance(Vector2.zero,Vector2.right,6.21f,
            renderer.transform,new RaycastHit2D[2]);
        Check(Math.Abs(distance-2.25f)<0.001f,"Actors/triggers masked the solid crate or full-buffer fallback failed.");
        Call(renderer,"LateUpdate");var fixedPoints=Points(renderer);
        for(int i=0;i<25;i++)
        {
            guard.transform.position=new Vector3(.8f+i*.16f,-.9f+i*.08f);
            player.transform.position=new Vector3(.3f+i*.08f,-.6f+i*.04f);
            Call(renderer,"LateUpdate");var moved=Points(renderer);
            for(int p=0;p<moved.Length;p++)Check((moved[p]-fixedPoints[p]).sqrMagnitude<1e-9f,
                "A moving actor tore a hole in the vision mesh.");
        }
        double oldError=0,newError=0;int frames=0;Vector2[] exampleOld=null,exampleNew=null;
        var hits=new RaycastHit2D[32];
        for(float rotation=-20f;rotation<=20f;rotation+=1f)
        {
            detector.angle=rotation;Call(renderer,"LateUpdate");var refined=Points(renderer);
            var coarse=new Vector2[49];
            for(int i=0;i<49;i++)
            {
                Vector2 direction=Quaternion.Euler(0,0,rotation-33f+66f*i/48f)*Vector3.right;
                coarse[i]=direction*DetectionUtility.GetSightDistance(Vector2.zero,direction,6.21f,renderer.transform,hits);
            }
            for(int i=0;i<720;i++)
            {
                Vector2 direction=Quaternion.Euler(0,0,rotation-33f+66f*(i+.5f)/720f)*Vector3.right;
                float actual=DetectionUtility.GetSightDistance(Vector2.zero,direction,6.21f,renderer.transform,hits);
                oldError+=Math.Abs(PolygonDistance(coarse,direction)-actual);
                newError+=Math.Abs(PolygonDistance(refined,direction)-actual);
            }
            if(rotation==0){exampleOld=coarse;exampleNew=refined;}frames++;
        }
        Check(newError<oldError*.25,"Extra silhouette samples did not reduce cone-edge error sufficiently.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory,"cone_geometry.json"),System.Text.Json.JsonSerializer.Serialize(
            new {before=Array.ConvertAll(exampleOld,p=>new[]{p.x,p.y}),
                after=Array.ConvertAll(exampleNew,p=>new[]{p.x,p.y}),errorRatio=newError/oldError},
            new System.Text.Json.JsonSerializerOptions {IncludeFields=true}));
        Physics2D.SightQuery=null;
        return "moving actors ignored; walls/crates and full ray buffers respected; "+frames+
            " sweep poses; silhouette error reduced "+(100*(1-newError/oldError)).ToString("0.0")+"%; fixed mesh capacity";
    }

    public static string Presentation(string directory)
    {
        Reset(); Physics2D.SightQuery=null;
        var menu=new UXMissionMenuScreen();var briefing=new UXMissionBriefingScreen(3,true);
        foreach(int width in new[]{960,1280,1920,2560})
        {
            Screen.width=width;Screen.height=width*9/16;GUI.Calls.Clear();menu.Draw();
            Check(GUI.Calls.Exists(c=>c.Text=="EASY MODE") && GUI.Calls.Exists(c=>c.Text=="HARD MODE"),"Missing mode selector labels.");
            foreach(var call in GUI.Calls)
            {
                string text=call.Text??"";
                Check(!text.Contains("Slower patrols") && !text.Contains("Three hits") && !text.Contains("Longer data"),"Menu reveals mode tuning.");
                Check(call.X>=-1&&call.Y>=-1&&call.X+call.W<=Screen.width+1&&call.Y+call.H<=Screen.height+1,"Menu layout overflow.");
            }
            File.WriteAllText(Path.Combine(directory,"menu_"+width+".json"),System.Text.Json.JsonSerializer.Serialize(GUI.Calls,
                new System.Text.Json.JsonSerializerOptions {IncludeFields=true}));
            UXPlayerProgress.SelectDifficulty(UXDifficulty.Easy);GUI.Calls.Clear();briefing.Draw();
            string easy=string.Join("\n",GUI.Calls.ConvertAll(c=>c.Text??""));
            UXPlayerProgress.SelectDifficulty(UXDifficulty.Hard);GUI.Calls.Clear();briefing.Draw();
            Check(easy==string.Join("\n",GUI.Calls.ConvertAll(c=>c.Text??"")),"Pre-deploy briefing reveals difficulty differences.");
        }
        return "mode names only; briefing has shared mission guidance; four screen sizes";
    }
}
