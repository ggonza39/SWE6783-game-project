using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;

// Runs real event paths against an inert audio API, not a Unity speaker/browser test.
public static class GameAudioChecks
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    private static string map,folder;
    private static int serial;
    private static UXStealthDirector director;
    private static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private).Invoke(target,args);
    private static object Campaign(string method,params object[] args)=>typeof(CampaignChecks).GetMethod(method,Static).Invoke(null,args);
    private static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    private static int Count(string clip)=>AudioSource.Played.Count(p=>p=="UXAudio/"+clip);
    private static void World(UXDifficulty mode,int checkpoint=0)
    {
        Campaign("Profile",Path.Combine(folder,"audio-"+(serial++)+".json"));
        typeof(CampaignChecks).GetField("geometry",Static).SetValue(null,JsonDocument.Parse(map).RootElement.Clone());
        Campaign("Build",mode,checkpoint,true);director=UXStealthDirector.Instance;
        Physics2D.SightQuery=ConePolishChecks.Query;
        AudioSource.Played.Clear();Input.MouseButtons.Clear();
        Time.timeScale=1f;director.Player.position=new Vector2(-18,12);
        for(int i=0;i<24;i++)director.Hazards.MineState(i).Neutralize();
        for(int i=0;i<6;i++)director.Hazards.LaserState(i).Neutralize();
    }
    private static void Step(float dt=.12f)
    {
        Time.time+=dt;Time.unscaledTime+=dt;Time.deltaTime=dt;
        director.State.Tick(dt,false,false,director.PlayerHidden);
        Call(UXGameAudio.Instance,"Update");
    }
    private static AudioSource[] Sources()=>UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
    public static string Run(string geometry,string output)
    {
        map=geometry;folder=output;Directory.CreateDirectory(folder);
        SaveMigration();ActualEvents();MixPauseAndScene();Menu();
        return "v1-v5 audio-default migration / checkpoint preservation; v6 mute and volume round trip; real collection/hack/door/damage/death/ending events; no restore pickup cues; hidden-mine silence and one arm/blast; distance/rate/voice limits; master mute / independent volume mix; pause/replay and listener/scene cleanup; settings fit four window sizes";
    }
    private static void SaveMigration()
    {
        for(int version=1;version<=5;version++)
        {
            var data=new UXPlayerSaveData{schemaVersion=version,musicVolume=0,effectsVolume=0,audioMuted=true};
            data.RecordCompletion("facility_01",UXDifficulty.Hard,81f);
            if(version>=2)data.campaigns.Add(new UXCampaignSlot{mode=UXDifficulty.Hard,levelId="crimson_02",checkpoint=2,sprintSeconds=7f});
            Check(data.Validate() && data.musicVolume==.55f && data.effectsVolume==.75f && !data.audioMuted,"An old profile starts silent.");
            Check(data.FindLevel("facility_01").hardClears==1 && data.FindCampaign(UXDifficulty.Hard).checkpoint==(version==1?0:2),"Audio migration lost a clear/checkpoint.");
        }
        string path=Path.Combine(folder,"volumes.json");Campaign("Profile",path);
        UXPlayerProgress.SetAudio(.25f,.80f,true);
        var reloaded=new UXProgressStore(path);
        Check(reloaded.Data.musicVolume==.25f && reloaded.Data.effectsVolume==.80f && reloaded.Data.audioMuted,"Volume/mute disk round trip failed.");
        Check(UXCampaign.RestartGame() && UXPlayerProgress.Data.audioMuted && UXPlayerProgress.Data.musicVolume==.25f,"New Game discarded sound preferences.");
        var corrupt=new UXPlayerSaveData{musicVolume=float.NaN,effectsVolume=float.PositiveInfinity};
        Check(corrupt.Validate() && corrupt.musicVolume==.55f && corrupt.effectsVolume==.75f,"Non-finite audio settings reached the mixer.");
    }
    private static void ActualEvents()
    {
        foreach(UXDifficulty mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})
        {
            World(mode,1);
            Check(Count("data")==0 && Count("checkpoint")==0,"Restoring secured progress replayed success audio.");
            var cache=UnityEngine.Object.FindObjectsByType<EvidenceCollectible>(FindObjectsSortMode.None).First(c=>c.gameObject.activeSelf);
            cache.Collect();cache.Collect();Check(Count("data")==1,"A real pickup was silent or repeated.");
            Step();director.State.Tick(2f,true,false,false);director.Hit();Check(Count("hit")==1,"Accepted bullet damage lacks a body hit.");
            director.Hit();Check(Count("hit")==1,"Rejected duplicate damage played a hit.");
            World(mode);
            var sight=UnityEngine.Object.FindObjectsByType<GuardDetection>(FindObjectsSortMode.None)[0];
            sight.transform.position=(Vector2)director.Player.position+new Vector2(0,-1);
            typeof(GuardDetection).GetField("<IsPlayerDetected>k__BackingField",Private).SetValue(sight,true);
            var response=sight.GetComponent<UXGuardResponse>();Call(response,"Start");
            director.AlertFor(sight.transform).Tick(2f,true,false,false);director.State.Tick(2f,true,false,false);
            Time.deltaTime=director.Difficulty.AimTime+.01f;Call(response,"Update");Call(response,"Update");
            Check(Count("shot")+Count("shot_alt")==1,"An actual guard shot is silent or repeats before the cooldown.");
            World(mode);AudioSource.Played.Clear();
            director.Player.position=director.Hazards.MinePosition(0);
            // Reset one live mine while leaving every other hazard inert.
            var mine=director.Hazards.MineState(0);
            Type state=mine.GetType();
            var replacement=new UXMineState(mode==UXDifficulty.Hard);
            var mines=(Array)typeof(UXCrimsonHazards).GetField("mines",Private).GetValue(director.Hazards);
            mines.GetValue(0).GetType().GetField("state").SetValue(mines.GetValue(0),replacement);
            typeof(UXCrimsonHazards).GetField("previousPlayer",Private).SetValue(director.Hazards,(Vector2)director.Player.position);
            Check(Count("mine_armed")==0,"A concealed inactive mine gave away its position.");
            Time.deltaTime=.01f;Call(director.Hazards,"Update");
            Check(Count("mine_armed")==1 && Count("mine_blast")==0,"Mine trigger sound is missing or premature.");
            Time.time+=1.2f;Time.unscaledTime+=1.2f;Time.deltaTime=1.2f;Call(director.Hazards,"Update");
            Check(Count("mine_blast")==1 && director.State.Alive,"Mine explosion is silent or game damage was changed.");
            Call(director.Hazards,"Update");Check(Count("mine_blast")==1,"A spent mine repeated its explosion.");
            World(mode);director.HazardHit(100,"LASER BARRIER",true);
            Check(Count("laser_burn")==1 && Count("failed")==1 && Count("mine_blast")==0 && Count("guard_death")==0,"Laser death used the wrong cause or repeated audio.");
            GameManager.Instance.LoseGame();Check(Count("failed")==1,"Mission outcome audio repeated.");
            World(mode);
            for(int hit=0;hit<5 && director.State.Alive;hit++){Time.time+=3f;Time.unscaledTime+=3f;director.State.Tick(3f,true,false,false);director.Hit();}
            Check(Count("guard_death")==1 && Count("mine_blast")==0 && Count("laser_burn")==0,"Gunfire death incorrectly exploded/fried the player.");
            World(mode);director.Hit();Time.time+=3f;Time.unscaledTime+=3f;director.HazardHit(100,"PROXIMITY MINE");
            Check(Count("mine_death")==1 && Count("guard_death")==0,"Mine death lacks the fragment impact layer.");
            World(mode);
            var data=JsonDocument.Parse(map).RootElement.GetProperty("data");
            for(int i=0;i<3;i++)Campaign("Hold",new Vector2(data[i][0].GetSingle(),data[i][1].GetSingle()),6f);
            var terminal=JsonDocument.Parse(map).RootElement.GetProperty("terminals")[0];
            Campaign("Hold",new Vector2(terminal[0].GetSingle(),terminal[1].GetSingle()),10f);
            Check(Count("data")==3 && Count("security")==1 && Count("checkpoint")==1,"Actual downloads/hack/checkpoint lack success audio.");
            director.Sectors.ExitDoor.Unlock();director.Sectors.ExitDoor.Unlock();
            Check(Count("door")==1,"A door is silent or repeats its opening sound.");
            World(mode);GameManager.Instance.WinGame();GameManager.Instance.WinGame();
            Check(Count("victory")==1 && Count("level_clear")==0,"Final campaign victory is silent/repeated/wrong.");
        }
    }
    private static void MixPauseAndScene()
    {
        World(UXDifficulty.Hard);var audio=UXGameAudio.Instance;
        var rootCount=Sources().Length;UXGameAudio.Attach(director);
        Check(rootCount==Sources().Length && rootCount==13,"Audio initialization creates duplicate source pools.");
        UXPlayerProgress.SetAudio(1f,1f,false);
        foreach(UXAudioCue cue in Enum.GetValues(typeof(UXAudioCue)))UXGameAudio.Play(cue);
        for(int i=0;i<100;i++)UXGameAudio.At(UXAudioCue.Shot,director.Player.position);
        Step(.001f);
        Check(audio.ActiveVoiceCount<=10 && Count("shot")+Count("shot_alt")<=1,"Crowded shooting exceeds rate/voice limits.");
        Check(Sources().Sum(s=>s.volume)<=.9201f,"Combined source gains can exceed the declared peak budget.");
        Step(3f);AudioSource.Played.Clear();
        UXGameAudio.At(UXAudioCue.Shot,(Vector2)director.Player.position+new Vector2(100,0));
        Check(Count("shot")+Count("shot_alt")==0,"Distant off-map gunfire is full-volume.");
        UXGameAudio.At(UXAudioCue.Shot,director.Player.position);
        Time.timeScale=0f;Step();Check(!Sources().Any(s=>s.clip!=null && s.clip.name.Contains("shot") && s.isPlaying),"Shots continue through pause.");
        UXGameAudio.UI();Check(Count("ui")>0,"Pause UI cannot be heard.");
        Time.timeScale=1f;Step(1f);AudioSource.Played.Clear();Step();Check(Count("shot")==0,"Resume replayed old shots.");
        UXPlayerProgress.SetAudio(.5f,0f,false);Step();Check(Sources().Where(s=>!s.gameObject.name.Contains("Music")).All(s=>s.volume==0f) && Sources().Any(s=>s.gameObject.name.Contains("Music")&&s.volume>0f),"Effects/music controls are coupled.");
        UXPlayerProgress.SetAudio(1f,1f,true);Step();Check(Sources().All(s=>s.volume==0f),"Master mute leaves an audible source.");
        UXGameAudio.Play(UXAudioCue.Shot);UXPlayerProgress.SetAudio(.55f,.75f,false);Step();Check(!Sources().Any(s=>s.clip!=null && s.clip.name.Contains("shot") && s.isPlaying),"Unmute resurrected a muted shot.");
        // A later scene camera listener takes precedence over our fallback.
        var listener=new GameObject("SceneListener").AddComponent<AudioListener>();Step(1.1f);
        Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.isActiveAndEnabled)==1 && listener.enabled,"Duplicate audio listeners remain active.");
        World(UXDifficulty.Easy);Check(Sources().Length==13 && UXGameAudio.Instance!=audio,"Scene/replay retained old sound sources.");
    }
    private static void Menu()
    {
        foreach(var size in new[]{(1280,800),(1920,1080),(1024,768),(800,600)})
        {
            Screen.width=size.Item1;Screen.height=size.Item2;
            var menu=new UXMissionMenuScreen();typeof(UXMissionMenuScreen).GetField("settingsVisible",Private).SetValue(menu,true);
            GUI.Calls.Clear();menu.Draw();
            foreach(var c in GUI.Calls)Check(c.X>=-1 && c.Y>=-1 && c.X+c.W<=Screen.width+1 && c.Y+c.H<=Screen.height+1,"Settings overflow the game window.");
            Check(GUI.Calls.Any(c=>c.Text=="MUSIC") && GUI.Calls.Any(c=>c.Text=="SOUND EFFECTS"),"Separate volume controls are missing.");
        }
        var preview=new UXMissionMenuScreen();
        typeof(UXMissionMenuScreen).GetField("settingsVisible",Private).SetValue(preview,true);
        Screen.width=1920;Screen.height=1080;GUI.Calls.Clear();preview.Draw();
        File.WriteAllText(Path.Combine(folder,"settings-layout.json"),JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
        Screen.width=1280;Screen.height=800;
    }
}
