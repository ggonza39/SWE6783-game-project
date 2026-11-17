using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using Object=UnityEngine.Object;

// Runs real campaign/controller code with the existing API stand-ins and map.
// Unity physics, rendering and the packaged executable still require playtesting.
public static class FinalCampaignChecks
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    private static string map,folder;
    private static int serial;
    private static UXStealthDirector director;
    private static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private).Invoke(target,args);
    private static object Campaign(string method,params object[] args)=>typeof(CampaignChecks).GetMethod(method,Static).Invoke(null,args);
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    private static void World(UXDifficulty mode,int checkpoint=0)
    {
        Campaign("Profile",Path.Combine(folder,"final-"+(serial++)+".json"));
        typeof(CampaignChecks).GetField("geometry",Static).SetValue(null,JsonDocument.Parse(map).RootElement.Clone());
        Campaign("Build",mode,checkpoint,true);director=UXStealthDirector.Instance;
        Physics2D.SightQuery=ConePolishChecks.Query;
    }
    private static void Step(float dt)
    {
        Time.deltaTime=dt;Time.time+=dt;Time.unscaledTime+=dt;
        director.State.Tick(dt,false,false,director.PlayerHidden);Call(director.Hazards,"Update");
    }
    public static string Run(string geometry,string output)
    {
        map=geometry;folder=output;Directory.CreateDirectory(output);
        LayoutsAndRoutes();Impacts();AllLevelRetriesAndEnding();
        return "32 deterministic shuffled layouts / 768 mine placements; objective routes avoid triggers; four Easy/Hard open/wall knockbacks; no sprint charge during impact; fatal fragments and floor splatter; full resource retry in both levels; campaign ends after Level 2";
    }
    private static void LayoutsAndRoutes()
    {
        World(UXDifficulty.Hard);var hazards=director.Hazards;
        const int width=107,height=63;var points=new Vector2[width*height];var solids=new bool[points.Length];
        for(int i=0;i<points.Length;i++)
        {
            points[i]=new Vector2(-26.5f+i%width*.5f,-15.5f+i/width*.5f);
            solids[i]=!(bool)Campaign("StandingRoom",points[i]);
        }
        string previous=null;
        for(int seed=1;seed<=32;seed++)
        {
            Call(hazards,"ShuffleMines",seed);
            var layout=Enumerable.Range(0,24).Select(hazards.MinePosition).ToArray();
            string snapshot=string.Join(";",layout.Select(p=>p.x.ToString("R")+","+p.y.ToString("R")));
            Check(snapshot!=previous,"Consecutive seeds repeated the same mine layout.");previous=snapshot;
            for(int i=0;i<layout.Length;i++)Check((bool)Call(hazards,"SafeMine",layout[i],i,layout),"A generated layout fell back to an unsafe/unvalidated placement.");
            Call(hazards,"ShuffleMines",seed);
            Check(Enumerable.Range(0,24).All(i=>Vector2.Distance(layout[i],hazards.MinePosition(i))<.0001f),"Same-attempt layout is unstable.");
            for(int sector=0;sector<3;sector++)
            {
                var blocked=(bool[])solids.Clone();
                for(int i=0;i<blocked.Length;i++)if(!blocked[i])
                    for(int m=sector*8;m<24;m++)if(Vector2.Distance(points[i],layout[m])<.90f){blocked[i]=true;break;}
                var grid=new UXGridPathfinder(width,height,blocked);
                Vector2 start=director.Sectors.Checkpoints[sector];
                int from=grid.NearestOpen(Mathf.RoundToInt((start.x+26.5f)*2),Mathf.RoundToInt((start.y+15.5f)*2));
                var goals=Enumerable.Range(sector*3,3).Select(i=>(Vector2)director.Sectors.Cache(i).transform.position)
                    .Concat(new[]{(Vector2)director.Sectors.Consoles[sector].transform.position});
                foreach(var goal in goals)
                {
                    int to=grid.NearestOpen(Mathf.RoundToInt((goal.x+26.5f)*2),Mathf.RoundToInt((goal.y+15.5f)*2));
                    Check(grid.Find(from,to).Count>0,"A shuffled mine layout blocks a required objective route.");
                }
            }
        }
        Check(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.gameObject.name.StartsWith("Mine_")).All(s=>!s.enabled),"Placement exposed dormant hidden mines.");
    }
    private static void Impacts()
    {
        foreach(var mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})foreach(bool wall in new[]{false,true})
        {
            World(mode);var hazards=director.Hazards;Vector2 at=hazards.MinePosition(0);
            for(int i=1;i<24;i++)hazards.MineState(i).Neutralize();for(int i=0;i<6;i++)hazards.LaserState(i).Neutralize();
            director.Player.position=at;typeof(UXCrimsonHazards).GetField("previousPlayer",Private).SetValue(hazards,at);
            var artGo=new GameObject("Impact_Player_Art");artGo.transform.SetParent(director.Player,false);var art=artGo.AddComponent<SpriteRenderer>();
            if(wall){var obstacle=new GameObject("Impact_Test_Wall");obstacle.transform.position=at+new Vector2(0,-.72f);obstacle.AddComponent<BoxCollider2D>().size=new Vector2(2f,.1f);director.RefreshNavigation();}
            UXCampaign.SpendSprint(2f);float bank=UXCampaign.SprintRemaining;
            Step(.01f);Step(1.12f);Check(director.State.Health==(mode==UXDifficulty.Hard?15:40),"A full-health first mine hit has incorrect damage.");
            var controller=director.Player.GetComponent<PlayerController>();Time.fixedDeltaTime=.02f;
            for(int i=0;i<17;i++){Call(controller,"Update");Call(controller,"FixedUpdate");}
            float distance=Vector2.Distance(at,director.Player.position);
            Check(wall?distance>.10f&&distance<.4f:distance>.7f&&distance<1.05f,"Mine knockback is absent, excessive, or passes through a wall.");
            Check(UXCampaign.SprintRemaining==bank,"Mine impact charged sprint reserve.");
            director.State.Tick(.86f,false,false,false);director.HazardHit(85,"PROXIMITY MINE");
            Check(GameManager.Instance.GameEnded&&!art.enabled,"Fatal mine kept its intact character renderer.");
            int objects=Object.All.Count;Time.unscaledTime+=.8f;Call(director.Effects,"LateUpdate");
            var bits=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.gameObject.name=="Fatal_Mine_Body_Fragment").ToArray();
            Check(bits.Length==12&&Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(s=>s.gameObject.name=="Fatal_Mine_Splatter")==12,"Fatal mine is missing fragments or splatter.");
            Check(objects==Object.All.Count,"Fatal animation allocates new objects each frame.");
        }
    }
    private static void AllLevelRetriesAndEnding()
    {
        foreach(var mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})foreach(var level in new[]{UXPlayerProgress.FacilityLevelId,UXCampaign.CrimsonLevelId})
        {
            float capacity=UXCampaign.CapacityForLevel(level);
            World(mode);UXCampaign.RestartLevel(level,mode);UXCampaign.Begin(level,mode);UXCampaign.SpendSprint(capacity);UXCampaign.RecordDeath();
            Check(UXCampaign.PrepareReplay()==UXCampaign.ScenePath(level),"Replay selected another level.");UXCampaign.Begin(level,mode);
            Check(UXCampaign.SprintRemaining==capacity,"One level or difficulty restores the wrong sprint budget after Replay.");
        }
        World(UXDifficulty.Easy);UXCampaign.Complete(UXCampaign.CrimsonLevelId,UXDifficulty.Easy,120f);
        Check(UXCampaign.SelectedSlot.completed&&!UXCampaign.NeedsAdvance&&!UXCampaign.Running,"Level 2 still tries to advance to another mission.");
        Check(UXCampaign.PrepareContinue()==UXCampaign.CrimsonScene,"Completed campaign cannot replay its final level.");
    }
}
