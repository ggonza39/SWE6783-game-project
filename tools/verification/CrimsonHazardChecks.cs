using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using Object=UnityEngine.Object;

// Integration with real campaign source and serialized-map collision boxes.
// This does not replace a Unity physics/rendering/balance playtest.
public static class CrimsonHazardChecks
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    private static UXStealthDirector director;
    private static UXCrimsonHazards hazards;
    private static string json,folder;
    private static int fixture;
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    private static void Call(object item,string method,params object[] args)=>item.GetType().GetMethod(method,Private).Invoke(item,args);
    private static void Field(object item,string field,object value)=>item.GetType().GetField(field,Private).SetValue(item,value);
    private static object Campaign(string method,params object[] args)=>typeof(CampaignChecks).GetMethod(method,Static).Invoke(null,args);
    private static void World(UXDifficulty mode,int checkpoint=0)
    {
        Campaign("Profile",Path.Combine(folder,"hazard-profile-"+(fixture++)+".json"));
        typeof(CampaignChecks).GetField("geometry",Static).SetValue(null,JsonDocument.Parse(json).RootElement.Clone());
        Campaign("Build",mode,checkpoint,true);director=UXStealthDirector.Instance;hazards=director.Hazards;
        Physics2D.SightQuery=ConePolishChecks.Query;for(int mine=0;mine<24;mine++)Call(hazards,"MoveMine",mine,UXCrimsonHazards.MinePositions[mine]);
    }
    private static void At(Vector2 p)
    {director.Player.position=p;Field(hazards,"previousPlayer",p);}
    private static void Step(float dt)
    {
        Time.deltaTime=dt;Time.time+=dt;Time.unscaledTime+=dt;
        director.State.Tick(dt,false,false,director.PlayerHidden);Call(hazards,"Update");
    }
    private static void OnlyMine(int mine)
    {
        for(int i=0;i<UXCrimsonHazards.MinePositions.Length;i++)if(i!=mine)hazards.MineState(i).Neutralize();
        for(int i=0;i<6;i++)hazards.LaserState(i).Neutralize();
    }
    private static void Fresh(UXDifficulty mode)
    {
        typeof(UXStealthDirector).GetField("<State>k__BackingField",Private).SetValue(director,new UXStealthState());
        typeof(GameManager).GetField("<GameEnded>k__BackingField",Private).SetValue(GameManager.Instance,false);
        typeof(GameManager).GetField("<PlayerWon>k__BackingField",Private).SetValue(GameManager.Instance,false);
        UXPlayerProgress.Data.difficulty=mode;UXCampaign.RestartLevel(UXCampaign.CrimsonLevelId,mode);UXCampaign.Begin(UXCampaign.CrimsonLevelId,mode);
        hazards.Begin(mode,0);Physics2D.SightQuery=ConePolishChecks.Query;for(int mine=0;mine<24;mine++)Call(hazards,"MoveMine",mine,UXCrimsonHazards.MinePositions[mine]);
    }
    public static string Run(string geometry,string output)
    {
        json=geometry;folder=output;Directory.CreateDirectory(output);
        Clocks();PlacementAndEscape();Routes();Consequences();SecurityAndReplay();Presentation();
        return "24 concealed mines / 6 laser strips; 2.40 blast radius; Easy/Hard 1.10s; Hard blast leaves 15 health at full health; lasers lethal in both modes; all 192 Hard approaches have an escape; safe objective/checkpoint access; damage/LOS/shared protection; swept laser hits; local shutdown; pause/result guards; Replay retains checkpoint";
    }
    private static void Clocks()
    {
        foreach(bool hard in new[]{false,true})
        {
            var mine=new UXMineState(hard);Check(!mine.Tick(.016f,true)&&mine.Armed,"Mine skipped its warning.");
            float fuse=mine.Fuse;Check(!mine.Tick(fuse-.01f,false),"Mine fired before its fuse.");
            Check(mine.Tick(.02f,false)&&!mine.Tick(9f,true),"Mine did not fire exactly once.");
            var cancel=new UXMineState(hard);cancel.Tick(.01f,true);cancel.Neutralize();Check(!cancel.Tick(9f,true),"A secured mine still detonated.");
            var laser=new UXLaserState(hard,0);Check(laser.Phase==UXLaserPhase.Safe,"Laser starts with no safe gap.");
            laser.Tick(laser.SafeSeconds+.01f);Check(laser.Phase==UXLaserPhase.Warning,"Laser skipped its telegraph.");
            laser.Tick(laser.WarningSeconds);Check(laser.Phase==UXLaserPhase.Active,"Laser failed to energize.");
            laser.Tick(laser.ActiveSeconds);Check(laser.Phase==UXLaserPhase.Safe,"Laser did not cycle off.");
        }
        Check(UXLaserState.Crosses(0,-2,0,2,0,0,3.5f,.9f)&&!UXLaserState.Crosses(3,-2,3,2,0,0,3.5f,.9f),"Swept beam crossing is wrong.");
    }
    private static void PlacementAndEscape()
    {
        World(UXDifficulty.Hard);int escapes=0;
        var features=JsonDocument.Parse(json).RootElement;
        foreach(Vector2 at in UXCrimsonHazards.MinePositions)
        {
            foreach(string group in new[]{"data","terminals"})foreach(var f in features.GetProperty(group).EnumerateArray())
                Check(Vector2.Distance(at,new Vector2(f[0].GetSingle(),f[1].GetSingle()))>2.2f,"Mine threatens a required hold point: "+at.x+","+at.y);
            foreach(Vector2 refuge in director.Sectors.Checkpoints.Take(3))
                Check(Vector2.Distance(at,refuge)>4f,"Mine threatens a checkpoint refuge.");
        }
        for(int index=0;index<UXCrimsonHazards.MinePositions.Length;index++)for(int direction=0;direction<8;direction++)
        {
            float angle=direction*45f*Mathf.Deg2Rad;Vector2 approach=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            bool survived=false;
            // Test forward first; reversing or turning aside is a valid response when
            // an enlarged warning ring reaches a nearby wall. Never move through solids.
            foreach(int turn in new[]{0,4,2,6,1,3,5,7})
            {
                Fresh(UXDifficulty.Hard);OnlyMine(index);
                float escapeAngle=(direction+turn)*45f*Mathf.Deg2Rad;
                Vector2 escape=new Vector2(Mathf.Cos(escapeAngle),Mathf.Sin(escapeAngle));
                Vector2 start=UXCrimsonHazards.MinePositions[index]-approach*.70f;
                if(!(bool)Campaign("StandingRoom",start))start=UXCrimsonHazards.MinePositions[index]-approach*.60f;
                Check((bool)Campaign("StandingRoom",start),"Approach fixture is inside a solid at mine "+index+" direction "+direction);
                At(start);Step(1f/60f);
                Check(hazards.MineState(index).Armed,"Mine did not arm on this approach.");
                for(int frame=0;frame<90 && !GameManager.Instance.GameEnded;frame++)
                {
                    float paid=frame<9?0f:UXCampaign.SpendSprint(1f/60f);
                    Vector2 delta=frame<9?approach*(2.8f/60f):escape*(paid>0f?4.9f/60f:2.8f/60f);
                    MoveWithSolids(delta);Step(1f/60f);
                    if(hazards.MineState(index).Spent)break;
                }
                if(director.State.Health==100 && hazards.MineState(index).Spent){survived=true;break;}
            }
            Check(survived,"Hard approach has no escape at mine "+index+" direction "+direction);
            escapes++;
        }
        Check(escapes==UXCrimsonHazards.MinePositions.Length*8,"Not all approach directions were checked.");
        // The 25%-smaller ring deliberately gives both modes a fair walking
        // retreat in this open lane; sprint remains the faster escape option.
        foreach(var mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})
        {
            Fresh(mode);OnlyMine(0);At(UXCrimsonHazards.MinePositions[0]-new Vector2(.70f,0));Step(.016f);
            for(int frame=0;frame<100 && !hazards.MineState(0).Spent;frame++)
            {
                MoveWithSolids(new Vector2((frame<9?1f:-1f)*2.8f/60f,0));Step(1f/60f);
            }
            Check(hazards.MineState(0).Spent && director.State.Health==100,"Prompt retreat did not clear the reduced blast ring in "+mode);
        }
    }
    private static void MoveWithSolids(Vector2 delta)
    {
        Vector2 from=director.Player.position,to=from+delta;
        if((bool)Campaign("StandingRoom",to)){director.Player.position=to;return;}
        float lo=0f,hi=1f;
        for(int i=0;i<UXCrimsonHazards.MinePositions.Length;i++)
        {
            float middle=(lo+hi)*.5f;
            if((bool)Campaign("StandingRoom",from+delta*middle))lo=middle;else hi=middle;
        }
        director.Player.position=from+delta*lo;
    }

    private static void Routes()
    {
        var geometry=JsonDocument.Parse(json).RootElement;
        for(int sector=0;sector<3;sector++)
        {
            World(UXDifficulty.Hard,sector);
            const int width=107,height=63;var blocked=new bool[width*height];
            for(int cell=0;cell<blocked.Length;cell++)
            {
                Vector2 at=new Vector2(-26.5f+(cell%width)*.5f,-15.5f+(cell/width)*.5f);
                blocked[cell]=!(bool)Campaign("StandingRoom",at);
                for(int m=sector*UXCrimsonHazards.MinesPerSector;m<UXCrimsonHazards.MinePositions.Length && !blocked[cell];m++)
                    if(Vector2.Distance(at,UXCrimsonHazards.MinePositions[m])<.90f)blocked[cell]=true;
            }
            var grid=new UXGridPathfinder(width,height,blocked);
            Vector2 start=director.Player.position;
            int from=grid.NearestOpen(Mathf.RoundToInt((start.x+26.5f)*2),Mathf.RoundToInt((start.y+15.5f)*2));
            var goals=geometry.GetProperty("data").EnumerateArray().Skip(sector*3).Take(3).Concat(new[]{geometry.GetProperty("terminals")[sector]});
            foreach(var point in goals)
            {
                int to=grid.NearestOpen(Mathf.RoundToInt((point[0].GetSingle()+26.5f)*2),Mathf.RoundToInt((point[1].GetSingle()+15.5f)*2));
                Check(grid.Find(from,to).Count>0,"A required sector objective forces a mine trigger: "+sector);
            }
            foreach(int laser in new[]{sector*2,sector*2+1})
                Check(hazards.LaserState(laser).SafeSeconds> .9f/2.8f+.25f,"Laser has no walkable safe crossing window.");
        }
    }

    private static void Consequences()
    {
        World(UXDifficulty.Easy);OnlyMine(0);At(UXCrimsonHazards.MinePositions[0]);Step(.016f);
        Time.timeScale=0;float before=hazards.MineState(0).Elapsed;Step(3f);
        Check(hazards.MineState(0).Elapsed==before && director.State.Health==100,"Pause advanced a mine.");
        Time.timeScale=1;for(int i=0;i<100;i++)Step(.02f);
        Check(director.State.Health==40 && !GameManager.Instance.GameEnded,"Easy blast damage is wrong.");
        Check(director.GuardResponds(Object.FindObjectsByType<UXPreviewGuardFacingPatrol>(FindObjectsSortMode.None)[0].transform) &&
            !director.GuardResponds(Object.FindObjectsByType<UXPreviewGuardFacingPatrol>(FindObjectsSortMode.None)[4].transform),"Explosion summoned guards from other sectors.");
        World(UXDifficulty.Hard);OnlyMine(0);Vector2 mine=UXCrimsonHazards.MinePositions[0];At(mine);Step(.016f);
        var wall=new GameObject("Blast_Test_Cover");wall.transform.position=mine+new Vector2(.6f,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.15f,2f);
        At(mine+new Vector2(1.2f,0));for(int i=0;i<65;i++)Step(.02f);
        Check(director.State.Health==100,"Blast ignored solid cover.");
        var shared=new UXStealthState();Check(shared.TryHazardDamage(20)&&!shared.TryHazardDamage(20),"Traps ignore shared hit protection.");
        shared.InvestigateNoise();Check(!shared.TryDamage(34),"Guard fire stacked through trap protection.");
        World(UXDifficulty.Hard);foreach(var p in Enumerable.Range(0,UXCrimsonHazards.MinePositions.Length))hazards.MineState(p).Neutralize();
        for(int i=1;i<6;i++)hazards.LaserState(i).Neutralize();
        At(UXCrimsonHazards.LaserPositions[0]+new Vector2(0,-2));Step(2.2f);
        Check(hazards.LaserState(0).Phase==UXLaserPhase.Active,"Laser fixture is not active.");
        director.Player.position=UXCrimsonHazards.LaserPositions[0]+new Vector2(0,2);Step(.016f);
        Check(director.State.Health==0 && GameManager.Instance.GameEnded,"Sprinting across an active beam between frames avoided death.");
    }
    private static void SecurityAndReplay()
    {
        World(UXDifficulty.Hard);At(UXCrimsonHazards.MinePositions[0]);Step(.016f);hazards.SecureSector(0,new Vector2(-12,-9));
        Check(Enumerable.Range(0,UXCrimsonHazards.MinesPerSector).All(i=>hazards.MineState(i).Neutralized)&&Enumerable.Range(0,2).All(i=>hazards.LaserState(i).Neutralized),"Local console did not neutralize its traps.");
        Check(!hazards.MineState(UXCrimsonHazards.MinesPerSector).Neutralized&&!hazards.LaserState(2).Neutralized,"Local console disabled other sectors.");
        for(int i=0;i<100;i++)Step(.02f);Check(director.State.Health==100,"Neutralized armed mine exploded after the hack.");
        World(UXDifficulty.Hard,2);
        Check(Enumerable.Range(0,2*UXCrimsonHazards.MinesPerSector).All(i=>hazards.MineState(i).Neutralized)&&!hazards.MineState(2*UXCrimsonHazards.MinesPerSector).Neutralized,"Restored checkpoint did not restore trap state.");
        Check(director.PlayerHidden,"Replay did not spawn hidden.");
        var hud=new GameObject("HUD").AddComponent<UXHUDController>();Call(hud,"Awake");Call(hud,"Start");Field(hud,"briefingVisible",false);
        At(new Vector2(18,12));director.HazardHit(100,"PROXIMITY MINE");
        float frozen=hazards.LaserState(4).Clock;Step(3f);Check(hazards.LaserState(4).Clock==frozen,"Result screen advanced active hazards.");
        GUI.Calls.Clear();GUI.ButtonClick=null;Call(hud,"OnGUI");
        Check(GUI.Calls.Any(c=>c.Kind=="label"&&c.Text=="REPLAY")&&!GUI.Calls.Any(c=>c.Text=="CONTINUE"),"Death still says Continue.");
        GUI.ButtonClick=t=>t=="REPLAY";Call(hud,"OnGUI");GUI.ButtonClick=null;
        Check(UXCampaign.SelectedSlot.checkpoint==2 && UnityEngine.SceneManagement.SceneManager.LastRequestedScene==UXCampaign.CrimsonScene,"Replay reset the checkpoint or loaded the wrong scene.");
    }
    private static void Presentation()
    {
        World(UXDifficulty.Hard);At(UXCrimsonHazards.MinePositions[0]);Step(.016f);
        var hud=new GameObject("HUD").AddComponent<UXHUDController>();Call(hud,"Awake");Call(hud,"Start");Field(hud,"briefingVisible",false);
        foreach(int w in new[]{960,1280,1920,2560})
        {
            Screen.width=w;Screen.height=w*9/16;GUI.Calls.Clear();Call(hud,"DrawHazardFeedback");
            foreach(var c in GUI.Calls)Check(c.X>=0 && c.Y>=0 && c.X+c.W<=Screen.width+1 && c.Y+c.H<=Screen.height+1,"Hazard warning overflows.");
            File.WriteAllText(Path.Combine(folder,"hazard_warning_"+w+".json"),JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
        }
        // Dump actual primitive transforms for a separate offline art review.
        var art=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.transform.IsChildOf(hazards.transform)).Select(s=>new{
            name=s.gameObject.name,x=s.transform.position.x,y=s.transform.position.y,w=s.transform.lossyScale.x,h=s.transform.lossyScale.y,
            angle=s.transform.eulerAngles.z,r=s.color.r,g=s.color.g,b=s.color.b,a=s.color.a,order=s.sortingOrder,enabled=s.enabled}).ToArray();
        File.WriteAllText(Path.Combine(folder,"hazard_art.json"),JsonSerializer.Serialize(art));
    }
}
