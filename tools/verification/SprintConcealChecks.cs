using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SprintConcealChecks
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    private static UXStealthDirector director;
    private static PlayerController player;
    private static string directory,geometry;
    private static int profile;
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    private static void Near(float a,float b,string why){Check(Math.Abs(a-b)<.002f,why+" / "+a+" vs "+b);}
    private static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
    private static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
    private static object Campaign(string method,params object[] args)=>typeof(CampaignChecks).GetMethod(method,Static).Invoke(null,args);
    private static void World(UXDifficulty mode,int checkpoint=0)
    {
        Campaign("Profile",Path.Combine(directory,"reserve-"+(profile++)+".json"));
        typeof(CampaignChecks).GetField("geometry",Static).SetValue(null,JsonDocument.Parse(geometry).RootElement.Clone());
        Campaign("Build",mode,checkpoint,true);director=UXStealthDirector.Instance;
        player=director.Player.GetComponent<PlayerController>();Set(player,"moveSpeed",2.8f);
        Physics2D.SightQuery=ConePolishChecks.Query;Time.fixedDeltaTime=.02f;for(int mine=0;mine<24;mine++)Call(director.Hazards,"MoveMine",mine,UXCrimsonHazards.MinePositions[mine]);
    }
    private static void MoveStep(float x,bool shift,bool careful=false)
    {
        Input.Axes["Horizontal"]=x;Input.Axes["Vertical"]=0f;Input.Keys.Clear();
        if(shift)Input.Keys.Add(KeyCode.LeftShift);if(careful)Input.Keys.Add(KeyCode.C);
        Call(player,"Update");Call(player,"FixedUpdate");
    }
    public static string Run(string json,string output)
    {
        geometry=json;directory=output;Directory.CreateDirectory(output);
        Migration();DrainAndPersistence();SaveFailure();HiddenMines();Hud();
        return "schema1/2/3 migration retains records/checkpoints; same 20s Level 2 bank in both modes; physics-time drain and partial final step; idle/pause/holds do not drain; no recharge; checkpoints retain spent stamina; death/replay restores full stamina; new level/restart refill; hidden-before-trigger hardware; larger blast; responsive reserve meter";
    }
    private static void Migration()
    {
        string legacy="{\"gameId\":\"PixelRebellion\",\"schemaVersion\":2,\"difficulty\":1,\"fullscreen\":false,\"showControlHints\":false,"+
            "\"levels\":[{\"levelId\":\"facility_01\",\"hardClears\":1,\"bestHardSeconds\":121}],"+
            "\"campaigns\":[{\"mode\":1,\"levelId\":\"crimson_02\",\"checkpoint\":2,\"seconds\":75,\"detections\":3}]}";
        string path=Path.Combine(directory,"v2.json");File.WriteAllText(path,legacy);var store=new UXProgressStore(path);
        var slot=store.Data.FindCampaign(UXDifficulty.Hard);
        Check(store.Data.schemaVersion==UXPlayerSaveData.CurrentVersion && slot.checkpoint==2 && slot.seconds==75 && slot.detections==3 &&
            store.Data.FindLevel("facility_01").bestHardSeconds==121 && !store.Data.fullscreen && !store.Data.showControlHints,"Migration lost prior progress/settings.");
        Near(slot.sprintSeconds,UXSprintReserve.CrimsonCapacitySeconds,"Migration failed to grant the initial resource");Check(store.Save()&&File.ReadAllText(path+".bak")==legacy,"Original save was not backed up.");
        slot.sprintSeconds=0f;Check(store.Save(),"Could not save exhaustion.");store=new UXProgressStore(path);Near(store.Data.FindCampaign(UXDifficulty.Hard).sprintSeconds,0f,"Reload replenished empty bank.");
        slot=new UXCampaignSlot{sprintSeconds=-1f};Check(!slot.Validate(),"Negative bank accepted.");slot.sprintSeconds=float.NaN;Check(!slot.Validate(),"Nonfinite bank accepted.");
    }
    private static void DrainAndPersistence()
    {
        foreach(var mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})
        {
            World(mode,1);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds,"Mode has a different reserve");
            for(int i=0;i<50;i++)MoveStep(1,true);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds-1f,"One physics second did not cost one stamina second");
            Near(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,4.9f,"Sprinting speed changed");
            for(int i=0;i<500;i++)MoveStep(0,true);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds-1f,"Holding Shift while idle drained reserve");
            for(int i=0;i<50;i++)MoveStep(1,true,true);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds-1f,"Precision crouch charged sprint");
            Time.timeScale=0;for(int i=0;i<50;i++)MoveStep(1,true);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds-1f,"Pause drained reserve");Time.timeScale=1;
            for(int i=0;i<300;i++)MoveStep(1,false);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds-1f,"Waiting/crouching refilled reserve");
            UXCampaign.SpendSprint(2.35f);UXCampaign.FlushSprint();float remaining=UXCampaign.SprintRemaining;
            Check(UXCampaign.SecureSector(2),"Checkpoint failed");Near(UXCampaign.SprintRemaining,remaining,"Hack refilled reserve");
            UXCampaign.RecordDeath();string path=Path.Combine(directory,"reserve-"+(profile-1)+".json");
            Campaign("Profile",path);UXCampaign.PrepareContinue();UXCampaign.Begin(UXCampaign.CrimsonLevelId,mode);
            Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds,"Death Replay failed to refill");remaining=UXSprintReserve.CrimsonCapacitySeconds;
            UXCampaign.SpendSprint(remaining-.01f);MoveStep(1,true);
            Near(UXCampaign.SprintRemaining,0,"Last partial step did not empty bank");
            Near(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,2.8f*1.375f,"Partial step granted unpaid sprint distance");
            MoveStep(1,true);Check(!player.IsSprinting&&player.IsSneaking,"Exhaustion did not restore crouched stance");
            Near(player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,2.8f,"Exhaustion blocked normal crouched movement");
            UXCampaign.RecordDeath();Campaign("Profile",path);UXCampaign.Begin(UXCampaign.CrimsonLevelId,mode);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds,"Empty Replay failed to refill");
            UXCampaign.RestartLevel(UXCampaign.CrimsonLevelId,mode);UXCampaign.Begin(UXCampaign.CrimsonLevelId,mode);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds,"Fresh level restart has no usable reserve");
            UXCampaign.Begin(UXPlayerProgress.FacilityLevelId,mode);UXCampaign.SpendSprint(UXSprintReserve.CapacitySeconds);UXCampaign.Complete(UXPlayerProgress.FacilityLevelId,mode,10);
            UXCampaign.Begin(UXCampaign.CrimsonLevelId,mode);Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds,"New level inherited previous level's exhaustion");
        }
    }
    private static void SaveFailure()
    {
        World(UXDifficulty.Hard);
        string blocked=Path.Combine(directory,"blocked-resource-save");Directory.CreateDirectory(blocked);
        Campaign("Profile",blocked);UXCampaign.Begin(UXCampaign.CrimsonLevelId,UXDifficulty.Hard);
        Time.unscaledTime=10f;UXCampaign.SpendSprint(2f);
        Check(UXPlayerProgress.Store.HasSaveError,"Save failure fixture did not fail.");
        string temp=blocked+".tmp";DateTime sentinel=new DateTime(2001,1,1,0,0,0,DateTimeKind.Utc);File.SetLastWriteTimeUtc(temp,sentinel);
        for(int i=0;i<100;i++)UXCampaign.FlushSprint();
        Check(File.GetLastWriteTimeUtc(temp)==sentinel,"Failed save retried every frame instead of waiting.");
        Near(UXCampaign.SprintRemaining,UXSprintReserve.CrimsonCapacitySeconds-2f,"Save failure refilled in-memory resource.");
    }

    private static bool MineHardware(SpriteRenderer s)=>s.gameObject.name=="Mine_Casing" || s.gameObject.name=="Mine_Caution" ||
        s.gameObject.name=="Mine_Inner" || s.gameObject.name=="Mine_X" || s.gameObject.name=="Mine_Core";
    private static void HiddenMines()
    {
        World(UXDifficulty.Easy);var hazard=director.Hazards;
        var hardware=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(MineHardware).ToArray();
        Check(hardware.Length==UXCrimsonHazards.MinePositions.Length*6 && hardware.All(s=>!s.enabled),"A dormant mine reveals its location.");
        Check(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.gameObject.name=="Mine_Blast_Warning").All(s=>!s.enabled),"An idle mine has a visible ring.");
        Vector2 mine=UXCrimsonHazards.MinePositions[0];director.Player.position=mine;Set(hazard,"previousPlayer",mine);Call(hazard,"Update");
        Check(hazard.MineState(0).Armed && hardware.Count(s=>s.enabled)==6,"Triggered mine failed to reveal only itself.");
        var rings=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.gameObject.name=="Mine_Blast_Warning"&&s.enabled).ToArray();
        Check(rings.Length==20 && rings.All(s=>Math.Abs(Vector2.Distance(s.transform.position,mine)-UXCrimsonHazards.BlastRadius)<.01f),"Warning radius differs from larger damage radius.");
        director.Player.position=mine+new Vector2(2f,0);Set(hazard,"previousPlayer",(Vector2)director.Player.position);
        for(int i=0;i<90;i++){Time.deltaTime=.02f;director.State.Tick(.02f,false,false,false);Call(hazard,"Update");}
        Check(director.State.Health==40,"Larger radius did not hit a previously safe 2-unit position.");
        World(UXDifficulty.Hard);UXCampaign.SpendSprint(UXSprintReserve.CrimsonCapacitySeconds);hazard=director.Hazards;mine=UXCrimsonHazards.MinePositions[0];
        director.Player.position=mine;Set(hazard,"previousPlayer",mine);Time.deltaTime=.02f;Call(hazard,"Update");
        Check(hazard.Warning.Contains("BACK AWAY"),"Empty-bank warning still orders an unavailable sprint.");
        hazard.Begin(UXDifficulty.Hard,2);Check(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(MineHardware).All(s=>!s.enabled),"Replay or neutralization reveals hidden mines.");
    }
    private static void Hud()
    {
        World(UXDifficulty.Hard);var hud=new GameObject("HUD").AddComponent<UXHUDController>();Call(hud,"Awake");Call(hud,"Start");Set(hud,"briefingVisible",false);
        foreach(int width in new[]{640,960,1280,1920,2560})foreach(float left in new[]{UXSprintReserve.CrimsonCapacitySeconds,3f,0f})
        {
            UXCampaign.RestartLevel(UXCampaign.CrimsonLevelId,UXDifficulty.Hard);UXCampaign.Begin(UXCampaign.CrimsonLevelId,UXDifficulty.Hard);UXCampaign.SpendSprint(UXSprintReserve.CrimsonCapacitySeconds-left);
            Screen.width=width;Screen.height=width*9/16;GUI.Calls.Clear();Call(hud,"DrawHUD");
            foreach(var c in GUI.Calls)Check(c.X>=-1 && c.Y>=-1 && c.W>=0 && c.X+c.W<=Screen.width+1 && c.Y+c.H<=Screen.height+1,"Reserve HUD overflow: "+c.Text);
            Check(GUI.Calls.Any(c=>c.Text!=null&&c.Text.StartsWith("SPRINT  ")),"Resource meter label absent");
            File.WriteAllText(Path.Combine(directory,"sprint_"+width+"_"+(int)left+".json"),JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
        }
    }
}
