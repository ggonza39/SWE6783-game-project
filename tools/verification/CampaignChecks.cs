using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Integration checks against API stand-ins and the generated level geometry.
// These do not execute Unity physics, rendering, scene loading or a real build.
public static class CampaignChecks
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static JsonElement geometry;
    private static UXStealthDirector director;
    private static UXSectorMission sectors;
    private static PlayerController player;
    private static TerminalInteraction[] consoles;
    private static SecurityCamera[] cameras;
    private static UXPreviewGuardFacingPatrol[] patrols;
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Private).Invoke(obj,args);
    private static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Private).SetValue(obj,value);
    private static T Field<T>(object obj,string field)=>(T)obj.GetType().GetField(field,Private).GetValue(obj);
    private static Vector2 Point(JsonElement p)=>new Vector2(p[0].GetSingle(),p[1].GetSingle());
    private static GameObject At(string name,Vector2 point){var go=new GameObject(name);go.transform.position=point;return go;}
    private static void Profile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Application.persistentDataPath=Path.GetDirectoryName(path);
        typeof(UXPlayerProgress).GetField("store",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,new UXProgressStore(path));
    }
    private static void ResetWorld(string name="UXCrimsonDepths")
    {
        foreach(var obj in Object.All.ToArray())if(obj is GameObject)Object.DestroyImmediate(obj);
        Object.All.Clear();Input.Keys.Clear();Input.DownKeys.Clear();Input.Axes.Clear();GUI.ButtonClick=null;Camera.main=null;
        SceneManager.Active=new Scene{name=name,path="Assets/Scenes/"+name+".unity"};
        Time.timeScale=1f;Time.time=0f;Time.unscaledTime=0f;Application.ScenesAvailable=true;
        Physics2D.SightQuery=null;
        UnityEngine.Random.Fraction=0;UnityEngine.Random.ChooseUpper=false;
        typeof(UXCampaign).GetMethod("ResetRuntime",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
        typeof(UXHUDController).GetMethod("ResetSceneRequests",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
    }
    private static void Tick(float dt=.1f)
    {Time.deltaTime=dt;Time.time+=dt;Time.unscaledTime+=dt;Call(director,"Update");}

    public static string Persistence(string directory)
    {
        Directory.CreateDirectory(directory);string path=Path.Combine(directory,"legacy.json");
        string legacy="{\"gameId\":\"PixelRebellion\",\"schemaVersion\":1,\"difficulty\":1,\"showControlHints\":false,\"fullscreen\":false,"+
            "\"levels\":[{\"levelId\":\"facility_01\",\"easyClears\":2,\"hardClears\":1,\"bestEasySeconds\":81,\"bestHardSeconds\":197}]}";
        File.WriteAllText(path,legacy);var store=new UXProgressStore(path);
        Check(store.Data.schemaVersion==UXPlayerSaveData.CurrentVersion && store.Data.campaigns.Count==2,"Legacy campaign slots were not migrated.");
        Check(store.Data.FindCampaign(UXDifficulty.Hard).levelId==UXCampaign.CrimsonLevelId &&
            store.Data.FindLevel("facility_01").bestHardSeconds==197f && !store.Data.fullscreen && !store.Data.showControlHints,
            "Migration lost records or settings.");
        Check(store.Save()&&File.ReadAllText(path+".bak")==legacy,"Migration did not retain the original save backup.");
        store.Data.FindCampaign(UXDifficulty.Hard).checkpoint=2;store.Data.FindCampaign(UXDifficulty.Hard).seconds=99f;store.Data.FindCampaign(UXDifficulty.Hard).detections=7;
        Check(store.Save(),"Could not save a checkpoint.");store=new UXProgressStore(path);
        Check(store.Data.FindCampaign(UXDifficulty.Hard).checkpoint==2 && store.Data.FindCampaign(UXDifficulty.Hard).seconds==99f && store.Data.FindCampaign(UXDifficulty.Hard).detections==7 &&
            store.Data.FindCampaign(UXDifficulty.Easy).checkpoint==0,"Checkpoint disk round trip or mode isolation failed.");
        File.WriteAllText(path,"{broken");store=new UXProgressStore(path);
        Check(!store.HasSaveError && store.Data.FindLevel("facility_01").hardClears==1,"Campaign backup recovery failed.");
        string future="{\"gameId\":\"PixelRebellion\",\"schemaVersion\":999}";File.WriteAllText(path,future);
        store=new UXProgressStore(path);Check(!store.TryReplace(new UXPlayerSaveData()) && File.ReadAllText(path)==future,
            "A confirmed restart erased an unsupported save.");
        return "v1 migration retains both mode records/settings and original backup; v2 checkpoints reload; modes stay separate; recovery/newer-save protection";
    }

    public static string CampaignFlow(string directory)
    {
        ResetWorld("UXFacilityExpanded");Profile(Path.Combine(directory,"flow.json"));
        UXPlayerProgress.Data.fullscreen=false;UXPlayerProgress.Data.showControlHints=false;
        UXCampaign.Begin("facility_01",UXDifficulty.Easy);UXCampaign.Tick(17f);
        UXCampaign.Complete("facility_01",UXDifficulty.Easy,17f);
        var saved=new UXProgressStore(Path.Combine(directory,"flow.json"));
        Check(saved.Data.FindCampaign(UXDifficulty.Easy).levelId==UXCampaign.CrimsonLevelId &&
            UXCampaign.NeedsAdvance && saved.Data.FindLevel("facility_01").easyClears==1,
            "Level 1 did not save Level 2 entry before transition.");
        UXCampaign.Begin(UXCampaign.CrimsonLevelId,UXDifficulty.Easy);UXCampaign.Tick(9f);
        Check(UXCampaign.SecureSector(1) && !UXCampaign.SecureSector(3),"A checkpoint could skip a sector.");
        UXCampaign.Begin("facility_01",UXDifficulty.Hard);
        Check(UXPlayerProgress.Data.FindCampaign(UXDifficulty.Easy).checkpoint==1,"Hard replaced the Easy campaign.");
        UXCampaign.RestartLevel(UXCampaign.CrimsonLevelId,UXDifficulty.Easy);
        Check(UXPlayerProgress.Data.FindCampaign(UXDifficulty.Easy).checkpoint==0 &&
            UXPlayerProgress.Data.FindLevel("facility_01").easyClears==1,"Restart Level erased earlier clears.");
        Check(UXCampaign.RestartGame() && UXPlayerProgress.Data.levels.Count==0 && UXPlayerProgress.Data.campaigns.Count==0 &&
            !UXPlayerProgress.Data.fullscreen && !UXPlayerProgress.Data.showControlHints,"Restart Game scope/settings failed.");
        string blocked=Path.Combine(directory,"blocked-save");Directory.CreateDirectory(blocked);Profile(blocked);
        UXPlayerProgress.Data.RecordCompletion("facility_01",UXDifficulty.Easy,10f);
        Check(!UXCampaign.RestartGame() && UXPlayerProgress.Data.FindLevel("facility_01").easyClears==1,
            "A failed reset destroyed the in-memory record.");
        return "clear saved before next level; checkpoint order enforced; separate modes; Restart Level preserves prior clears; full reset is transactional and retains settings";
    }

    private static void Build(UXDifficulty mode,int checkpoint,bool arrange=true)
    {
        ResetWorld();
        if(arrange)
        {
            UXPlayerProgress.Data.difficulty=mode;
            if(UXPlayerProgress.Data.FindLevel("facility_01")==null)UXPlayerProgress.Data.RecordCompletion("facility_01",mode,100f);
            UXPlayerProgress.Data.campaigns.RemoveAll(slot=>slot.mode==mode);
            UXPlayerProgress.Data.campaigns.Add(new UXCampaignSlot{mode=mode,levelId=UXCampaign.CrimsonLevelId,checkpoint=checkpoint});
            UXPlayerProgress.Store.Save();
        }
        var bodyGo=At("Player",Point(geometry.GetProperty("spawn")));bodyGo.tag="Player";
        bodyGo.AddComponent<BoxCollider2D>();bodyGo.AddComponent<PlayerHiding>();player=bodyGo.AddComponent<PlayerController>();Call(player,"Awake");
        var manager=At("GameManager",Vector2.zero).AddComponent<GameManager>();Call(manager,"Awake");
        var root=At("EvidenceManager",Vector2.zero);var evidence=root.AddComponent<EvidenceManager>();Call(evidence,"Awake");
        var definition=root.AddComponent<UXMissionDefinition>();definition.crimson=true;definition.requiredEvidence=9;
        definition.halfWidth=27.5f;definition.halfHeight=16.5f;
        var doors=new BoxCollider2D[2];
        foreach(var wall in geometry.GetProperty("walls").EnumerateArray())
        {
            string name=wall.GetProperty("name").GetString();var go=At(name,new Vector2(wall.GetProperty("x").GetSingle(),wall.GetProperty("y").GetSingle()));
            var box=go.AddComponent<BoxCollider2D>();box.size=new Vector2(wall.GetProperty("w").GetSingle(),wall.GetProperty("h").GetSingle());
            go.AddComponent<SpriteRenderer>();if(name.StartsWith("gate"))doors[int.Parse(name.Substring(4))]=box;
        }
        foreach(string group in new[]{"hiding","checkpoints"})foreach(var p in geometry.GetProperty(group).EnumerateArray())
        {var go=At("HidingSpot",Point(p));go.tag="HidingSpot";go.AddComponent<BoxCollider2D>().isTrigger=true;}
        var caches=geometry.GetProperty("data").EnumerateArray().Select((p,i)=>{
            var go=At("Evidence_"+(i+1),Point(p));go.AddComponent<UXExpandedInteraction>();go.AddComponent<BoxCollider2D>().isTrigger=true;
            return go.AddComponent<EvidenceCollectible>();}).ToArray();
        cameras=new SecurityCamera[12];
        for(int i=0;i<12;i++)
        {var spec=geometry.GetProperty("cameras")[i];var go=At("SecurityCamera_"+(i+1),Point(spec.GetProperty("position")));
            go.transform.rotation=Quaternion.Euler(0,0,spec.GetProperty("angle").GetSingle());go.AddComponent<UXSecuritySector>().sector=i/4;
            cameras[i]=go.AddComponent<SecurityCamera>();Set(cameras[i],"rotationSpeed",24f);Set(cameras[i],"sweepAngle",35f);
            Set(cameras[i],"detectionRange",5.4f);Set(cameras[i],"detectionAngle",44f);}
        consoles=geometry.GetProperty("terminals").EnumerateArray().Select((p,i)=>{
            var go=At("Terminal_"+(char)('A'+i),Point(p));go.AddComponent<UXExpandedInteraction>();go.AddComponent<BoxCollider2D>().isTrigger=true;
            var console=go.AddComponent<TerminalInteraction>();Set(console,"requiredEvidence",(i+1)*3);Set(console,"completesMission",false);
            Set(console,"camerasToDisable",cameras.Skip(i*4).Take(4).ToArray());return console;}).ToArray();
        patrols=geometry.GetProperty("routes").EnumerateArray().Select((points,i)=>{
            var route=points.EnumerateArray().Select(Point).ToArray();var go=At("Guard_"+i,route[0]);go.AddComponent<UXSecuritySector>().sector=i/4;
            go.AddComponent<GuardController>();go.AddComponent<BoxCollider2D>().size=new Vector2(.5f,.7f);
            var detection=go.AddComponent<GuardDetection>();Set(detection,"detectionRange",3.4f);Set(detection,"detectionAngle",55f);
            var patrol=go.AddComponent<UXPreviewGuardFacingPatrol>();Set(patrol,"routePoints",route);
            Set(patrol,"routeEdges",Enumerable.Range(0,route.Length).Select(j=>new Vector2Int(j,(j+1)%route.Length)).ToArray());
            Set(patrol,"speed",1.2f);Set(patrol,"turnDuration",.75f);Set(patrol,"minimumPause",1.1f);Set(patrol,"maximumPause",1.65f);Call(patrol,"Awake");return patrol;}).ToArray();
        sectors=root.AddComponent<UXSectorMission>();Set(sectors,"caches",caches);Set(sectors,"consoles",consoles);Set(sectors,"bulkheads",doors);
        Set(sectors,"checkpointPositions",geometry.GetProperty("checkpoints").EnumerateArray().Select(Point).ToArray());
        director=root.AddComponent<UXStealthDirector>();Call(director,"Awake");director.BeginMission(mode);
        foreach(var camera in cameras)Call(camera,"Start");
        foreach(var patrol in patrols)Call(patrol.GetComponent<GuardDetection>(),"Start");
        Check(Object.FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None).Length==3,"Crimson must have exactly one hiding bay per sector.");
        foreach(var bay in Object.FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None))
            Check(bay.Pocket!=null && (bay.Entrance-(Vector2)bay.transform.position).sqrMagnitude>.1f,"A hiding/checkpoint bay has no usable entrance: "+bay.transform.position.x+","+bay.transform.position.y);
    }

    private static void Hold(Vector2 at,float seconds)
    {
        player.transform.position=at;
        foreach(var console in consoles)Set(console,"playerNearby",Vector2.Distance(at,console.transform.position)<1.2f);
        Input.Keys.Add(KeyCode.E);for(float t=0;t<seconds;t+=.1f)Tick();Input.Keys.Clear();Tick();
    }
    private static Vector2 Entrance(int checkpoint)=>Object.FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None)
        .First(b=>Vector2.Distance(b.transform.position,sectors.Checkpoints[checkpoint])<.01f).Entrance;

    public static string Mission(string json,string directory)
    {
        geometry=JsonDocument.Parse(json).RootElement.Clone();
        foreach(UXDifficulty mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})
        {
            Profile(Path.Combine(directory,"mission-"+mode+".json"));Build(mode,0);
            Check(director.IsCrimson && director.PlayerHidden && director.State.Health==100,"Entry checkpoint is not safe/healthy.");
            Check(director.Path(Entrance(0),consoles[0].transform.position).Count>0 &&
                director.Path(Entrance(0),consoles[1].transform.position).Count==0,"A locked sector is accessible, or its own console is trapped.");
            Hold(consoles[0].transform.position,9f);Check(sectors.SecuredSectors==0,"Console opened without its data.");
            Hold(Point(geometry.GetProperty("data")[3]),6f);Check(EvidenceManager.Instance.EvidenceCollected==0,"Locked-sector data could be downloaded.");
            player.transform.position=Point(geometry.GetProperty("data")[0]);Input.Keys.Add(KeyCode.E);Tick(.4f);
            float partial=director.InteractionProgress;Time.timeScale=0;Tick(1f);Check(director.InteractionProgress==partial,"Pause advanced a download.");
            Time.timeScale=1;Input.Keys.Clear();Tick();Check(director.InteractionProgress==0,"Release did not cancel progress.");
            for(int stage=0;stage<3;stage++)
            {
                for(int i=stage*3;i<stage*3+3;i++)
                {
                    Vector2 at=Point(geometry.GetProperty("data")[i]);
                    Check(director.Path(Entrance(stage),at).Count>0,"A required cache is unreachable before its sector gate.");
                    Hold(at,2.5f*director.Difficulty.InteractionDuration+.15f);
                }
                Hold(consoles[stage].transform.position,4f*director.Difficulty.InteractionDuration+.15f);
                Check(sectors.SecuredSectors==stage+1 && UXCampaign.SelectedSlot.checkpoint==stage+1,"Sector checkpoint did not advance.");
                Check(cameras.Take((stage+1)*4).All(c=>c.IsDisabled) && cameras.Skip((stage+1)*4).All(c=>!c.IsDisabled),
                    "A console disabled another sector's cameras, or its own cameras stayed on.");
                Check(GameManager.Instance.TerminalActivated==(stage==2),"Global extraction enabled before every console.");
                if(stage==0)
                {
                    Hold(Point(geometry.GetProperty("data")[3]),2.5f*director.Difficulty.InteractionDuration+.15f);
                    director.State.Tick(2f,true,false,false);
                    while(director.State.Alive){director.Hit();if(director.State.Alive)Tick(.9f);}
                    Check(GameManager.Instance.GameEnded,"Zero health did not end the attempt.");
                    float savedSeconds=UXCampaign.SelectedSlot.seconds;
                    var disk=new UXProgressStore(Path.Combine(directory,"mission-"+mode+".json"));
                    Check(disk.Data.FindCampaign(mode).checkpoint==1 && disk.Data.FindLevel("facility_01").Completed(mode),"Death lost the last clear/checkpoint.");
                    Profile(Path.Combine(directory,"mission-"+mode+".json"));Build(mode,1,false);
                    Check(EvidenceManager.Instance.EvidenceCollected==3 && sectors.CacheAvailable(3) && director.State.Health==100 &&
                        director.PlayerHidden && UXCampaign.RunSeconds>=savedSeconds,"Restore retained unsaved data, lost the clock, or spawned unsafe.");
                }
            }
            Vector2 exit=Point(geometry.GetProperty("exit"));
            Check(!GameManager.Instance.CanExtract(9) && director.Path(Entrance(3),exit).Count==0,"Final door can be bypassed.");
            Hold(sectors.ExitDoor.transform.position,director.ExitDoorSeconds+.15f);
            Check(GameManager.Instance.CanExtract(9) && director.Path(Entrance(3),exit).Count>0,"Final door did not open its required route.");
            GameManager.Instance.WinGame();GameManager.Instance.WinGame();
            Check(UXPlayerProgress.Data.FindLevel(UXCampaign.CrimsonLevelId).Completed(mode) && UXCampaign.SelectedSlot.completed &&
                (mode==UXDifficulty.Easy?UXPlayerProgress.Data.FindLevel(UXCampaign.CrimsonLevelId).easyClears:
                    UXPlayerProgress.Data.FindLevel(UXCampaign.CrimsonLevelId).hardClears)==1,"Victory was not saved exactly once.");
        }
        return "Easy/Hard complete; 9 caches/3 local hacks; ordered bulkheads; 3 sector refuges; cancel/pause; death and disk reload restore secured objectives/health/clock; final gate required; one victory record";
    }

    public static string Menus(string directory)
    {
        ResetWorld("UXFacilityExpanded");Profile(Path.Combine(directory,"menu.json"));
        UXCampaign.Begin("facility_01",UXDifficulty.Easy);UXCampaign.Complete("facility_01",UXDifficulty.Easy,20f);
        var menu=new UXMissionMenuScreen();bool continued=false,reset=false;
        menu.ContinueRequested=()=>continued=true;menu.RestartConfirmed=()=>reset=true;
        GUI.ButtonClick=t=>t=="CONTINUE  >";menu.Draw();Check(continued,"Continue did not request a saved campaign load.");
        Set(menu,"resetVisible",true);GUI.ButtonClick=t=>t=="CANCEL";menu.Draw();
        Check(UXPlayerProgress.Data.levels.Count==1&&!reset,"Cancel reset campaign progress.");
        Set(menu,"resetVisible",true);Input.DownKeys.Add(KeyCode.Return);menu.HandleKeyboard();Input.DownKeys.Clear();
        Check(!Field<bool>(menu,"resetVisible")&&UXPlayerProgress.Data.levels.Count==1,"Enter did not default to Cancel.");
        foreach(int width in new[]{960,1280,1920,2560})
        {
            Screen.width=width;Screen.height=width*9/16;
            foreach(bool modal in new[]{false,true})
            {
                Set(menu,"resetVisible",modal);GUI.ButtonClick=null;GUI.Calls.Clear();menu.Draw();
                foreach(var c in GUI.Calls)Check(c.X>=-1 && c.Y>=-1 && c.X+c.W<=Screen.width+1 && c.Y+c.H<=Screen.height+1,"Campaign menu overflow: "+c.Text);
                File.WriteAllText(Path.Combine(directory,(modal?"reset_":"menu_")+width+".json"),JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
            }
        }
        Set(menu,"resetVisible",true);GUI.ButtonClick=t=>t=="RESTART GAME";menu.Draw();GUI.ButtonClick=null;
        Check(reset && UXPlayerProgress.Data.levels.Count==0 && UXPlayerProgress.Data.campaigns.Count==0,"Confirmed reset did not clear both progress types.");
        return "Continue uses saved campaign; full reset requires confirmation; Cancel/Enter preserve records; responsive home/reset layouts at four sizes";
    }

    public static string Transition(string directory)
    {
        ResetWorld("UXFacilityExpanded");Profile(Path.Combine(directory,"transition.json"));
        var body=At("Player",new Vector2(-12,-6));body.AddComponent<BoxCollider2D>();body.AddComponent<PlayerHiding>();
        var controller=body.AddComponent<PlayerController>();Call(controller,"Awake");
        var manager=At("GameManager",Vector2.zero).AddComponent<GameManager>();Call(manager,"Awake");
        var root=At("EvidenceManager",Vector2.zero);var evidence=root.AddComponent<EvidenceManager>();Call(evidence,"Awake");
        var d=root.AddComponent<UXStealthDirector>();Call(d,"Awake");d.BeginMission(UXDifficulty.Easy);UXCampaign.Tick(19f);
        var hud=At("HUD",Vector2.zero).AddComponent<UXHUDController>();Call(hud,"Awake");Call(hud,"Start");Set(hud,"briefingVisible",false);
        manager.WinGame();manager.WinGame();
        var disk=new UXProgressStore(Path.Combine(directory,"transition.json"));
        Check(disk.Data.FindLevel("facility_01").easyClears==1 && disk.Data.FindCampaign(UXDifficulty.Easy).levelId==UXCampaign.CrimsonLevelId,
            "Win was not saved once before scene loading.");
        SceneManager.LastRequestedScene=null;Call(hud,"Update");Check(SceneManager.LastRequestedScene==null,"Transition skipped its title beat.");
        Time.unscaledTime+=1.4f;Call(hud,"Update");
        Check(SceneManager.LastRequestedScene==UXCampaign.CrimsonScene &&
            (bool)typeof(UXHUDController).GetField("startGameplayAfterReload",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null),
            "Level 1 did not request automatic Level 2 gameplay.");
        Application.ScenesAvailable=false;Call(hud,"LoadCampaignScene",UXCampaign.CrimsonScene,true,false);
        Check(!UXCampaign.NeedsAdvance && Field<string>(hud,"campaignLoadError")!=null,"Missing scene causes an automatic transition loop.");
        Application.ScenesAvailable=true;
        return "Level 1 clear and Level 2 entry persisted exactly once before load; brief automatic title transition; no second Deploy; missing-scene recovery";
    }

    public static string Presentation(string json,string directory)
    {
        geometry=JsonDocument.Parse(json).RootElement.Clone();Profile(Path.Combine(directory,"presentation.json"));Build(UXDifficulty.Hard,2);
        At("ExtractionZone",Point(geometry.GetProperty("exit")));
        var hud=At("HUD",Vector2.zero).AddComponent<UXHUDController>();Call(hud,"Awake");Call(hud,"Start");
        Set(hud,"briefingVisible",false);Check(Field<int>(hud,"totalEvidence")==9,"Restored mission has the wrong evidence total.");
        var briefing=new UXMissionBriefingScreen(9,true);
        foreach(int width in new[]{960,1280,1920,2560})
        {
            Screen.width=width;Screen.height=width*9/16;
            foreach(string surface in new[]{"briefing","blueprint","hud"})
            {
                GUI.Calls.Clear();
                if(surface=="briefing")briefing.Draw();else if(surface=="blueprint")briefing.DrawMapOverlay();else Call(hud,"DrawHUD");
                foreach(var c in GUI.Calls)Check(c.X>=-1 && c.Y>=-1 && c.X+c.W<=Screen.width+1 && c.Y+c.H<=Screen.height+1,"Crimson presentation overflow: "+c.Text);
                File.WriteAllText(Path.Combine(directory,surface+"_"+width+".json"),JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
            }
        }
        return "restored HUD retains 9-cache total; new briefing, tactical map and HUD fit four sizes";
    }

    private static bool StandingRoom(Vector2 at)
    {
        BoxCollider2D body=player.GetComponent<BoxCollider2D>();
        foreach(var box in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if(!box.enabled||box.isTrigger||box.GetComponentInParent<PlayerController>()!=null||
                box.GetComponentInParent<GuardController>()!=null||Physics2D.GetIgnoreCollision(box,body))continue;
            Bounds bounds=box.bounds;bounds.Expand(new Vector3(.5f,.7f));
            if(at.x>bounds.min.x&&at.x<bounds.max.x&&at.y>bounds.min.y&&at.y<bounds.max.y)return false;
        }
        return true;
    }
    private static bool Sees(IConeDetector detector,Transform owner,Vector2 at)
    {
        return DetectionUtility.IsWithinFieldOfView(detector.ConeOrigin,detector.ConeForward,at,detector.ConeRange,
            detector.ConeFovAngle,out _,out _) && DetectionUtility.HasLineOfSight(detector.ConeOrigin,at,owner,
                probeHits,player.transform);
    }
    private static readonly RaycastHit2D[] probeHits=new RaycastHit2D[32];

    public static string Patrols(string json,string directory)
    {
        geometry=JsonDocument.Parse(json).RootElement.Clone();
        float[] worst=new[]{float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity};
        for(int variation=0;variation<2;variation++)
        {
            Profile(Path.Combine(directory,"patrol-"+variation+".json"));Build(UXDifficulty.Hard,0);
            UnityEngine.Random.Fraction=variation;UnityEngine.Random.ChooseUpper=variation==1;
            var sights=patrols.Select(p=>p.GetComponent<GuardDetection>()).ToArray();
            player.transform.position=Point(geometry.GetProperty("data")[0]);
            Set(sights[0],"<IsPlayerDetected>k__BackingField",true);Tick(1f);
            Check(director.GuardResponds(patrols[0].transform) && !director.GuardResponds(patrols[4].transform) &&
                !director.GuardResponds(patrols[8].transform),"One sector alerted all twelve guards.");
            Check(!director.TryMoveGuard(patrols[0].transform,new Vector2(20,8)),"A guard left its assigned sector.");
            Set(sights[0],"<IsPlayerDetected>k__BackingField",false);player.transform.position=sectors.Checkpoints[0];
            for(int i=0;i<160;i++)Tick(.1f);
            Check(!director.State.Responding,"Search did not clear from a protected checkpoint.");
            var centers=consoles.Select(c=>(Vector2)c.transform.position).Concat(new[]{(Vector2)sectors.ExitDoor.transform.position}).ToArray();
            var offsets=new[]{Vector2.zero,new Vector2(.6f,0),new Vector2(-.6f,0),new Vector2(0,.6f),new Vector2(0,-.6f),
                new Vector2(.55f,.55f),new Vector2(-.55f,.55f),new Vector2(.55f,-.55f),new Vector2(-.55f,-.55f)};
            var candidates=centers.Select(center=>offsets.Select(offset=>center+offset).Where(StandingRoom).ToArray()).ToArray();
            Check(candidates.All(c=>c.Length>0),"A console has no standing room.");
            var streak=candidates.Select(c=>new float[c.Length]).ToArray();var best=new float[4];var traveled=new float[12];
            Physics2D.SightQuery=ConePolishChecks.Query;
            for(int frame=0;frame<1800;frame++)
            {
                Tick(.1f);
                for(int i=0;i<patrols.Length;i++)
                {
                    Vector2 before=patrols[i].transform.position;Call(patrols[i],"Update");Vector2 after=patrols[i].transform.position;
                    traveled[i]+=Vector2.Distance(before,after);
                    Check(director.ClearNavigationSegment(after,after) && UXSectorMission.InSector(i/4,after),"Patrol clipped a wall or crossed sectors.");
                }
                foreach(var camera in cameras)Call(camera,"Update");
                for(int objective=0;objective<4;objective++)for(int p=0;p<candidates[objective].Length;p++)
                {
                    Vector2 at=candidates[objective][p];bool seen=false;
                    for(int g=0;g<sights.Length&&!seen;g++)seen=Sees(sights[g],patrols[g].transform,at);
                    if(objective<3)for(int c=objective*4;c<objective*4+4&&!seen;c++)seen=Sees(cameras[c],cameras[c].transform,at);
                    streak[objective][p]=seen?0:streak[objective][p]+.1f;
                    best[objective]=Math.Max(best[objective],streak[objective][p]);
                }
            }
            Physics2D.SightQuery=null;
            Check(traveled.All(distance=>distance>10f),"A guard became stuck on its patrol: "+string.Join(",",traveled.Select(f=>f.ToString("0.0"))));
            for(int i=0;i<4;i++)
            {worst[i]=Math.Min(worst[i],best[i]);Check(best[i]>=8.3f,"Hard interaction "+i+" has no full clear hold window: "+best[i]);}
        }
        File.WriteAllText(Path.Combine(directory,"hard_hold_windows.json"),JsonSerializer.Serialize(worst));
        return "12 guards move inside their sectors without wall clipping; localized response; 6 simulated patrol minutes; full Hard hold windows (seconds): "+
            string.Join(", ",worst.Select(f=>f.ToString("0.0")));
    }
}
