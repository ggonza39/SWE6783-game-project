using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using Object = UnityEngine.Object;

// Real source/serialized geometry with API stand-ins. Engine acceptance is separate.
public static class MissionEffectsChecks
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags Static = BindingFlags.NonPublic | BindingFlags.Static;
    private static string geometry, output;
    private static int fixture;
    private static UXStealthDirector d;
    private static UXHUDController hud;
    private static SpriteRenderer playerArt;
    private static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    private static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Private).Invoke(o, args);
    private static object Campaign(string method, params object[] args) => typeof(CampaignChecks).GetMethod(method, Static).Invoke(null, args);
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    private static void World(UXDifficulty mode, int checkpoint = 0)
    {
        Campaign("Profile", Path.Combine(output, "effects-profile-" + fixture++ + ".json"));
        typeof(CampaignChecks).GetField("geometry", Static).SetValue(null, JsonDocument.Parse(geometry).RootElement.Clone());
        Campaign("Build", mode, checkpoint, true); d = UXStealthDirector.Instance;
        Physics2D.SightQuery = ConePolishChecks.Query;for(int mine=0;mine<24;mine++)Call(d.Hazards,"MoveMine",mine,UXCrimsonHazards.MinePositions[mine]);
        var art = new GameObject("UX_Deluxe_PreviewArt"); art.transform.SetParent(d.Player, false);
        playerArt = art.AddComponent<SpriteRenderer>(); playerArt.color = Color.white;
        hud = new GameObject("HUD").AddComponent<UXHUDController>(); Call(hud, "Awake"); Call(hud, "Start"); Set(hud, "briefingVisible", false);
        d.Player.position = new Vector2(-18, 12); Input.DownKeys.Clear(); Input.Keys.Clear(); Time.timeScale=1f;
    }
    private static void Step(float dt)
    {
        Time.deltaTime = dt; Time.time += dt; Time.unscaledTime += dt;
        d.State.Tick(dt, false, false, d.PlayerHidden); Call(d.Hazards, "Update");
    }
    private static void IsolateMine()
    {
        for (int i = 1; i < 24; i++) d.Hazards.MineState(i).Neutralize();
        for (int i = 0; i < 6; i++) d.Hazards.LaserState(i).Neutralize();
        Set(d.Hazards, "previousPlayer", (Vector2)d.Player.position);
    }
    public static string Run(string map, string folder)
    {
        geometry = map; output = folder; Directory.CreateDirectory(folder);
        RetryAndScatter(); MinesAndCauses(); LasersAndReveal(); StaminaAndLayout(); AtmosphereAndLift(); RecordedReplay(); ReplayCaptureQuality(); DeathProfiles(); ReducedBlastBoundary();
        return "1280x720 / 20fps bounded replay; viewport HUD crop; fixed death/local timestamp; camera housing and separate action controls; 2.4-unit blast boundary in both modes; mine damage/fuse unchanged; separate gun blood-pool / mine fireball+shrapnel / laser char+scorch profiles; no explosive gun particles; backend-aware upright replay UVs in auto/result views; replay chronology/skip/watch-again/checkpoint/cleanup; stamina/lift/layout regressions";
    }
    private static void RetryAndScatter()
    {
        var easy=UXDifficultyProfile.For(UXDifficulty.Easy);var hard=UXDifficultyProfile.For(UXDifficulty.Hard);
        Check(easy.GuardRange==hard.GuardRange && easy.GuardAngle==hard.GuardAngle &&
            hard.ShotCooldown<easy.ShotCooldown,"Guard difficulty balance is wrong.");
        foreach(var mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})
        {
            Campaign("Profile",Path.Combine(output,"retry-scatter-"+mode+".json"));
            typeof(CampaignChecks).GetField("geometry",Static).SetValue(null,JsonDocument.Parse(geometry).RootElement.Clone());
            Campaign("Build",mode,1,true);var first=UXStealthDirector.Instance;
            int seed=UXCampaign.MineSeed;
            var original=Enumerable.Range(0,24).Select(first.Hazards.MinePosition).ToArray();
            UXCampaign.SpendSprint(3f);UXCampaign.SaveClock();UXCampaign.PrepareContinue();
            Campaign("Build",mode,1,false);first=UXStealthDirector.Instance;
            Check(UXCampaign.SprintRemaining==17f && UXCampaign.MineSeed==seed &&
                Enumerable.Range(0,24).All(i=>Vector2.Distance(original[i],first.Hazards.MinePosition(i))<.001f),
                "Live Continue refunded resources or rerolled mines.");
            UXCampaign.SpendSprint(17f);UXCampaign.RecordDeath();UXCampaign.PrepareContinue();
            Campaign("Build",mode,1,false);first=UXStealthDirector.Instance;
            Check(first.State.Health==100 && UXCampaign.SprintRemaining==20f && UXCampaign.MineSeed!=seed &&
                Enumerable.Range(0,24).Count(i=>Vector2.Distance(original[i],first.Hazards.MinePosition(i))>.01f)>=16,
                "Death Replay failed to refill or shuffle mines.");
            Check(UXCampaign.SelectedSlot.checkpoint==1 && Enumerable.Range(0,8).All(i=>first.Hazards.MineState(i).Neutralized),
                "Replay lost its secured checkpoint.");
            UXCampaign.SpendSprint(20f);UXCampaign.PrepareReplay();
            Campaign("Build",mode,1,false);
            Check(UXCampaign.SprintRemaining==20f,"Explicit Replay did not repair an older exhausted save.");
        }
    }

    private static void MinesAndCauses()
    {
        Check(Math.Abs(new UXMineState(true).Fuse - 1.10f) < .001f && new UXMineState(false).Fuse == 1.10f, "Hard margin/Easy clock is wrong.");
        foreach (UXDifficulty mode in new[] { UXDifficulty.Easy, UXDifficulty.Hard })
        {
            World(mode); IsolateMine(); Step(.01f);
            Check(d.Hazards.MineState(0).Armed, "Physical mine did not arm.");
            Step(1.08f); Check(d.State.Health == 100, "Mine fired early."); Step(.03f);
            Check(d.State.Health == (mode == UXDifficulty.Hard ? 15 : 40) && !GameManager.Instance.GameEnded, "First mine is not survivable from full health.");
            d.State.Tick(.86f, false, false, false); d.HazardHit(85, "PROXIMITY MINE");
            Check(GameManager.Instance.GameEnded && d.DeathHeadline == "KILLED BY MINE", "Mine death cause is wrong.");
            string cause = d.LastDamageCause; Check(!d.HazardHit(100, "LASER BARRIER", true) && d.LastDamageCause == cause, "Later hazard replaced a fatal cause.");
        }
        World(UXDifficulty.Hard); d.State.InvestigateNoise(); d.Hit();
        Check(d.State.Health == 66, "Guard damage changed."); d.State.Tick(.86f, false, false, false); d.Hit();
        d.State.Tick(.86f, false, false, false); d.Hit();
        Check(GameManager.Instance.GameEnded && d.DeathHeadline == "GUNNED DOWN BY GUARDS", "Guard headline is wrong.");
    }
    private static void LasersAndReveal()
    {
        foreach (UXDifficulty mode in new[] { UXDifficulty.Easy, UXDifficulty.Hard })
        {
            World(mode); for (int i = 0; i < 24; i++) d.Hazards.MineState(i).Neutralize();
            string logPath=Path.Combine(Application.persistentDataPath,"campaign_events.csv");
            int priorDeaths=File.Exists(logPath)?File.ReadAllLines(logPath).Count(s=>s.Contains("CheckpointRetry")):0;
            for (int i = 1; i < 6; i++) d.Hazards.LaserState(i).Neutralize();
            Vector2 beam = UXCrimsonHazards.LaserPositions[0]; d.Player.position = beam;
            Set(d.Hazards, "previousPlayer", beam); Step(d.Hazards.LaserState(0).SafeSeconds + .01f);
            Check(d.State.Health == 100 && d.Hazards.LaserState(0).Phase == UXLaserPhase.Warning, "Laser warning itself harmed the player.");
            d.Hazards.LaserState(0).Tick(d.Hazards.LaserState(0).WarningSeconds);
            d.HazardHit(1, "PROXIMITY MINE"); Check(d.State.Health == 99, "Recent-hit fixture failed."); Step(.01f);
            Check(d.State.Health == 0 && GameManager.Instance.GameEnded && d.DeathHeadline == "FRIED BY LASER", "Energized laser was not lethal through protection in " + mode);
            Check(d.Effects.DeathRevealing, "Burn reveal skipped.");
            float clock = d.Hazards.LaserState(0).Clock; Call(hud, "Update"); Check(Time.timeScale == 0f, "Death left gameplay running.");
            GUI.Calls.Clear(); Call(hud, "OnGUI"); Check(GUI.Calls.Any(c => c.Text == "FRIED BY LASER") && !GUI.Calls.Any(c => c.Text == "REPLAY"), "Results covered the burn effect.");
            int objects = Object.All.Count; Time.unscaledTime += .25f; Call(d.Effects, "LateUpdate");
            Check(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(s => s.gameObject.name == "Fatal_Electric_Arc" && s.enabled) == 10, "Electrical arcs are missing.");
            float bright = playerArt.color.r; Time.unscaledTime += .85f; Call(d.Effects, "LateUpdate");
            Check(!d.Effects.DeathRevealing && playerArt.color.r < bright && playerArt.color.r <= .11f && Math.Abs(playerArt.transform.eulerAngles.z) > 60f, "Player did not char/slump before results.");
            Check(Object.All.Count == objects, "Death animation allocates per frame.");
            Step(.2f); Check(d.Hazards.LaserState(0).Clock == clock, "Ended gameplay advanced traps.");
            GUI.Calls.Clear(); Call(hud, "OnGUI"); Check(GUI.Calls.Any(c => c.Text == "REPLAY") && GUI.Calls.Any(c => c.Text == "FRIED BY LASER"), "Final laser result is mislabeled.");
            string log = File.ReadAllText(Path.Combine(Application.persistentDataPath, "campaign_events.csv"));
            int deaths = log.Split('\n').Count(s => s.Contains("CheckpointRetry")); GameManager.Instance.LoseGame();
            Check(deaths == priorDeaths+1 && File.ReadAllText(Path.Combine(Application.persistentDataPath, "campaign_events.csv")) == log, "Death logged twice.");
        }
    }
    private static void StaminaAndLayout()
    {
        var tint = typeof(UXHUDController).GetMethod("StaminaTint", Static);
        Color full = (Color)tint.Invoke(null, new object[] { 1f }), half = (Color)tint.Invoke(null, new object[] { .5f }), empty = (Color)tint.Invoke(null, new object[] { 0f });
        Check(full.g > full.r && half.r == 1f && half.g > .75f && empty.r == 1f && empty.g < .3f, "Stamina colors do not progress green/yellow/red.");
        Color a = (Color)tint.Invoke(null, new object[] { .499f }), b = (Color)tint.Invoke(null, new object[] { .501f });
        Check(Math.Abs(a.r - b.r) < .01f && Math.Abs(a.g - b.g) < .01f, "Stamina tint jumps at halfway.");
        foreach (UXDifficulty mode in new[] { UXDifficulty.Easy, UXDifficulty.Hard })
        {
            World(mode); foreach (int width in new[] { 640, 960, 1280, 1920, 2560 }) foreach (float reserve in new[] { 20f, 10f, 4f, 0f })
            {
                Screen.width = width; Screen.height = width * 9 / 16;
                typeof(UXCampaign).GetProperty("Sprint").SetValue(null, new UXSprintReserve(reserve, UXSprintReserve.CrimsonCapacitySeconds));
                GUI.Calls.Clear(); Call(hud, "DrawHUD"); Bounds();
                Check(GUI.Calls.Any(c => c.Text == "SPRINT  " + reserve.ToString("0.0") + "s"), "Sprint numeric readout missing.");
                File.WriteAllText(Path.Combine(output, "stamina_" + mode + "_" + width + "_" + reserve + ".json"), JsonSerializer.Serialize(GUI.Calls, new JsonSerializerOptions { IncludeFields = true }));
            }
        }
        World(UXDifficulty.Hard); d.HazardHit(100, "LASER BARRIER", true); Time.unscaledTime += 1.2f;
        foreach (int width in new[] { 640, 960, 1280, 1920 })
        {
            Screen.width = width; Screen.height = width * 9 / 16; GUI.Calls.Clear(); Call(hud, "DrawMissionResult"); Bounds();
            File.WriteAllText(Path.Combine(output, "death_" + width + ".json"), JsonSerializer.Serialize(GUI.Calls, new JsonSerializerOptions { IncludeFields = true }));
        }
    }
    private static void AtmosphereAndLift()
    {
        World(UXDifficulty.Easy, 3); Check(!d.Effects.HardAtmosphere, "Hard atmosphere leaked into Easy.");
        World(UXDifficulty.Hard, 3); Check(d.Effects.HardAtmosphere, "Hard industrial detail is missing.");
        int count = Object.All.Count; d.Effects.Begin(); Check(count == Object.All.Count, "Repeated begin duplicated scenery.");
        Check(!Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None).Any(c => c.transform.IsChildOf(d.Effects.transform)), "Decor added collisions.");
        var lamp = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(s => s.gameObject.name == "Lift_Perimeter_Lamp");
        Check(lamp.color.r > lamp.color.g, "Secured lift should be amber before override.");
        d.Sectors.ExitDoor.Unlock(); Call(d.Effects, "LateUpdate"); Check(lamp.color.g > lamp.color.r, "Open lift did not turn green.");
        var vapor = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(s => s.gameObject.name == "Vent_Vapor");
        Vector3 at = vapor.transform.position; Time.timeScale = 0f; Time.unscaledTime += 2f; Call(d.Effects, "LateUpdate");
        Check(Vector2.Distance(at, vapor.transform.position) == 0f, "Pause advanced environment vapor.");
        Time.timeScale = 1f; Time.time += .5f; Call(d.Effects, "LateUpdate"); Check(Object.All.Count == count, "Environment allocates per frame.");
        foreach (int width in new[] { 640, 960, 1280, 1920 })
        {
            Screen.width = width; Screen.height = width * 9 / 16; GUI.Calls.Clear(); Call(hud, "DrawExtractionSignal"); Bounds();
            Check(GUI.Calls.Any(c => c.Text == "EXTRACTION ROUTE OPEN / REACH THE LIFT"), "Hard extraction signal missing.");
        }
        GameManager.Instance.WinGame(); GUI.Calls.Clear(); Call(hud, "DrawMissionResult");
        Check(GUI.Calls.Any(c => c.Text == "EXTRACTION CONFIRMED"), "Hard clear did not get its extraction result.");
        var art = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s => s.transform.IsChildOf(d.Effects.transform)).Select(s => new { name=s.gameObject.name, x=s.transform.position.x, y=s.transform.position.y, w=s.transform.lossyScale.x, h=s.transform.lossyScale.y, angle=s.transform.eulerAngles.z, r=s.color.r, g=s.color.g, b=s.color.b, a=s.color.a, order=s.sortingOrder, enabled=s.enabled }).ToArray();
        File.WriteAllText(Path.Combine(output, "hard_atmosphere_art.json"), JsonSerializer.Serialize(art));
    }
    private static void RecordedReplay()
    {
        World(UXDifficulty.Hard,2); Screen.width=1920;Screen.height=1080;
        UXCampaign.SpendSprint(2.5f);var replay=d.Effects.Replay;
        ScreenCapture.Captures=0;var previous=new RenderTexture(1,1,0,RenderTextureFormat.ARGB32);RenderTexture.active=previous;
        for(int i=0;i<UXDeathReplay.Capacity+8;i++){Time.unscaledTime+=UXDeathReplay.SampleInterval;Call(replay,"CaptureAtEndOfFrame");}
        Check(ScreenCapture.Captures==UXDeathReplay.Capacity+8 && (int)replay.GetType().GetField("count",Private).GetValue(replay)==UXDeathReplay.Capacity,"Replay ring did not stay bounded.");
        Check(RenderTexture.active==previous,"Capture changed the active render target.");
        d.HazardHit(100,"LASER BARRIER",true);
        int fatalFrames=(int)Math.Ceiling(UXMissionEffects.FatalRevealSeconds/UXDeathReplay.SampleInterval)-1;
        for(int i=0;i<fatalFrames;i++){Time.unscaledTime+=UXDeathReplay.SampleInterval;Call(d.Effects,"LateUpdate");Call(replay,"CaptureAtEndOfFrame");}
        Time.unscaledTime+=UXDeathReplay.SampleInterval;Call(replay,"CaptureAtEndOfFrame");
        int oldest=ScreenCapture.Captures-UXDeathReplay.Capacity+1;
        Check(replay.HasFrames && replay.AutoPlaying && ScreenCapture.Captures==UXDeathReplay.Capacity+8+fatalFrames,"Replay did not freeze before result capture.");
        Check(replay.CurrentFrame.CaptureStamp==oldest,"Replay did not start at the oldest retained frame.");
        Time.unscaledTime+=1f;Check(replay.CurrentFrame.CaptureStamp==oldest+(int)(1f/UXDeathReplay.SampleInterval),"Replay chronology/speed is wrong.");
        int captures=ScreenCapture.Captures;Call(replay,"CaptureAtEndOfFrame");Check(ScreenCapture.Captures==captures,"Replay captured itself recursively.");
        GUI.Calls.Clear();Call(hud,"OnGUI");Check(GUI.Calls.Any(c=>c.Text=="DEATH REPLAY / FRIED BY LASER"),"Automatic replay overlay is missing.");
        DateTime deathTime=replay.DeathLocalTime;float deathRun=replay.DeathMissionSeconds;replay.FreezeAfter(99f);
        Check(replay.DeathLocalTime==deathTime && replay.DeathMissionSeconds==deathRun,"Death timestamp was overwritten.");
        CheckReplayOrientation(hud,"OnGUI");
        string scene=UnityEngine.SceneManagement.SceneManager.LastRequestedScene;
        Input.DownKeys.Add(KeyCode.Space);Call(hud,"Update");Input.DownKeys.Clear();
        Check(!replay.AutoPlaying && UnityEngine.SceneManagement.SceneManager.LastRequestedScene==scene,"Skip replay restarted the game.");
        foreach(int width in new[]{640,960,1280,1920,2560})
        {
            Screen.width=width;Screen.height=width*9/16;GUI.Calls.Clear();Call(hud,"OnGUI");Bounds();
            Check(GUI.Calls.Any(c=>c.Text=="WATCH AGAIN")&&GUI.Calls.Any(c=>c.Text=="REPLAY"),"Failed screen lacks separate clip and checkpoint controls.");
            CheckReplayOrientation(hud,"OnGUI");
            var video=GUI.Calls.Single(c=>c.Kind=="texture"&&c.Texture=="Death_Replay_Frame");
            var replayButton=GUI.Calls.Single(c=>c.Text=="REPLAY");var menuButton=GUI.Calls.Single(c=>c.Text=="MAIN MENU");
            Check(replayButton.Y>video.Y+video.H && menuButton.Y>video.Y+video.H,"Main controls overlap recorded footage.");
            Check(GUI.Calls.Any(c=>c.Text=="CHOOSE YOUR NEXT ACTION")&&GUI.Calls.Any(c=>c.Text!=null&&c.Text.StartsWith("TIME OF DEATH"))&&GUI.Calls.Any(c=>c.Text==replay.CameraLabel),"Camera/death/action labels are missing.");
            File.WriteAllText(Path.Combine(output,"replay_result_"+width+".json"),JsonSerializer.Serialize(GUI.Calls,new JsonSerializerOptions{IncludeFields=true}));
        }
        GUI.ButtonClick=t=>t=="WATCH AGAIN";Call(hud,"OnGUI");GUI.ButtonClick=null;
        Check(replay.CurrentFrame.CaptureStamp==oldest && UnityEngine.SceneManagement.SceneManager.LastRequestedScene==scene,"Watch Again changed campaign state.");
        Check(UXCampaign.SelectedSlot.checkpoint==2 && UXCampaign.SprintRemaining==17.5f,"Watching replay changed checkpoint or sprint.");
        GUI.ButtonClick=t=>t=="REPLAY";Call(hud,"OnGUI");GUI.ButtonClick=null;
        Check(UnityEngine.SceneManagement.SceneManager.LastRequestedScene==UXCampaign.CrimsonScene && UXCampaign.SelectedSlot.checkpoint==2,"Checkpoint Replay changed recovery.");
        var textures=Object.FindObjectsByType<RenderTexture>(FindObjectsSortMode.None).Where(t=>t.name=="Death_Replay_Frame").ToArray();
        Call(replay,"OnDestroy");Check(textures.All(t=>!t.IsCreated()),"Replay GPU frames were not released.");

        World(UXDifficulty.Easy);Screen.width=1280;Screen.height=720;replay=d.Effects.Replay;
        Call(replay,"CaptureAtEndOfFrame");captures=ScreenCapture.Captures;Time.timeScale=0;Time.unscaledTime+=1;
        Call(replay,"CaptureAtEndOfFrame");Check(ScreenCapture.Captures==captures,"Pause was recorded into the clip.");
        Time.timeScale=1;Screen.width=640;Screen.height=360;Time.unscaledTime+=.125f;Call(replay,"CaptureAtEndOfFrame");
        Check((int)replay.GetType().GetField("count",Private).GetValue(replay)==1,"Resize mixed old/new aspect frames.");
        GameManager.Instance.WinGame();Call(replay,"CaptureAtEndOfFrame");Check(!replay.HasFrames,"Victory kept a death replay.");
        World(UXDifficulty.Easy);replay=d.Effects.Replay;RenderTexture.FailCreate=true;Call(replay,"CaptureAtEndOfFrame");RenderTexture.FailCreate=false;
        captures=ScreenCapture.Captures;Call(replay,"CaptureAtEndOfFrame");Check(ScreenCapture.Captures==captures,"Failed capture keeps retrying/allocating.");
        d.HazardHit(100,"PROXIMITY MINE");Time.unscaledTime+=2f;GUI.Calls.Clear();Call(hud,"OnGUI");
        Check(GUI.Calls.Any(c=>c.Text=="REPLAY")&&!replay.HasFrames,"Capture failure blocked death recovery.");
        RenderTexture.active=null;
    }
    private static void CheckReplayOrientation(UXHUDController target,string method)
    {
        foreach(bool top in new[]{true,false})
        {
            SystemInfo.graphicsUVStartsAtTop=top;GUI.Calls.Clear();Call(target,method);
            var image=GUI.Calls.Single(c=>c.Kind=="texture" && c.Texture=="Death_Replay_Frame");
            Check(image.U==0f && image.UW==1f && image.V==(top?1f:0f) && image.VH==(top?-1f:1f),"Replay vertical UV correction is missing/doubled.");
            RenderTexture frame=d.Effects.Replay.CurrentFrame;
            Check(Math.Abs(image.W/image.H-d.Effects.Replay.PlaybackAspect)<.001f,"Replay aspect ratio is distorted.");Bounds();
        }
        SystemInfo.graphicsUVStartsAtTop=true;GUI.Calls.Clear();Call(target,method);
    }
    private static void ReplayCaptureQuality()
    {
        Check(UXDeathReplay.Capacity==80 && Math.Abs(UXDeathReplay.SampleInterval-1f/20f)<.00001f,"Replay cadence/history did not improve.");
        foreach(bool top in new[]{true,false})
        {
            World(UXDifficulty.Hard);Screen.width=1920;Screen.height=1080;
            Camera source=new GameObject("Source View").AddComponent<Camera>();source.rect=new Rect(.07f,.13f,.88f,.63f);Camera.main=source;
            SystemInfo.graphicsUVStartsAtTop=top;var replay=d.Effects.Replay;Call(replay,"CaptureAtEndOfFrame");
            var frames=(RenderTexture[])replay.GetType().GetField("frames",Private).GetValue(replay);var image=frames[0];
            Check(image.width>480&&image.width<=UXDeathReplay.FrameWidth&&image.height<=UXDeathReplay.FrameHeight,"Replay retains the old low resolution.");
            Check(Math.Abs((image.width*replay.CapturedViewport.width/(image.height*replay.CapturedViewport.height))-(1920f*.88f/(1080f*.63f)))<.01f,"HUD crop changes camera aspect.");
            Rect uv=replay.PlaybackUV;
            Check(Math.Abs(uv.x-.07f)<.001f && Math.Abs(uv.width-.88f)<.001f && Math.Abs(uv.y-(top?1f-.13f:.13f))<.001f && Math.Abs(uv.height-(top?-.63f:.63f))<.001f,"Camera crop is wrong for backend orientation.");
            source.rect=new Rect(0,.2f,1,.6f);Time.unscaledTime+=UXDeathReplay.SampleInterval;Call(replay,"CaptureAtEndOfFrame");
            Check(!image.IsCreated()&&(int)replay.GetType().GetField("count",Private).GetValue(replay)==1,"Viewport change mixed mismatched frames.");
            Time.unscaledTime+=UXDeathReplay.SampleInterval;Call(replay,"CaptureAtEndOfFrame");
            UXCampaign.Tick(145f);d.HazardHit(100,"LASER BARRIER",true);
            Check(replay.DeathLocalTime.Kind==DateTimeKind.Local&&replay.DeathMissionSeconds==145f,"Camera time of death does not retain actual local/mission time.");
            Time.unscaledTime+=1.1f;Call(replay,"CaptureAtEndOfFrame");
            Check(replay.HasFrames && Math.Abs(replay.PlaybackAspect-1920f/(1080f*.6f))<.01f,"Frozen playback does not preserve its cropped camera aspect.");
        }
        SystemInfo.graphicsUVStartsAtTop=true;
    }
    private static void DeathProfiles()
    {
        foreach(UXDifficulty mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})foreach(string source in new[]{"GUARD FIRE","PROXIMITY MINE","LASER BARRIER"})
        {
            World(mode);playerArt.sortingOrder=20;
            int colliders=Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None).Length;
            Vector2 root=d.Player.position;
            if(source=="GUARD FIRE")
            {
                d.State.InvestigateNoise();
                for(int hit=0;hit<6 && d.State.Alive;hit++){d.Hit();d.State.Tick(.86f,false,false,false);}
            }
            else d.HazardHit(100,source,source=="LASER BARRIER"); // Fatal presentation fixture; real 85/60 mine damage is checked separately.
            Check(GameManager.Instance.GameEnded,"Death profile fixture failed.");
            var art=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.transform.IsChildOf(d.Effects.transform)&&s.gameObject.name.StartsWith("Fatal_")).ToArray();
            if(source=="GUARD FIRE")
            {
                Check(art.Count(s=>s.gameObject.name=="Fatal_Gun_Blood_Pool")==7,"Gun death lacks a pooled blood stain.");
                Check(!art.Any(s=>s.gameObject.name.Contains("Mine")||s.gameObject.name.Contains("Laser")||s.gameObject.name.Contains("Smoke")||s.gameObject.name.Contains("Spark")||s.gameObject.name.Contains("Arc")),"Gunfire still creates explosive/electrical/smoke particles.");
                Check(d.Effects.ImpactPulse<=.25f,"Gun impact still has the explosive flash strength.");
            }
            else if(source=="PROXIMITY MINE")
                Check(art.Count(s=>s.gameObject.name=="Fatal_Mine_Fireball")==9 && art.Count(s=>s.gameObject.name=="Fatal_Mine_Shrapnel")==20 && !art.Any(s=>s.gameObject.name.Contains("Gun_Blood")||s.gameObject.name.Contains("Laser")),"Mine profile is not an explosion only.");
            else
                Check(art.Count(s=>s.gameObject.name=="Fatal_Electric_Arc")==10 && art.Count(s=>s.gameObject.name=="Fatal_Laser_Scorch")==3 && !art.Any(s=>s.gameObject.name.Contains("Gun_Blood")||s.gameObject.name.Contains("Mine")),"Laser profile lacks scorch or leaks another death effect.");
            Time.unscaledTime+=.18f;Call(d.Effects,"LateUpdate");
            float smallPool=source=="GUARD FIRE"?art.Where(s=>s.gameObject.name=="Fatal_Gun_Blood_Pool").Max(s=>s.transform.lossyScale.x):0f;
            DumpDeathProfile(mode+"_"+source.Replace(' ','_')+"_early",art);
            int objects=Object.All.Count;Time.unscaledTime+=.77f;Call(d.Effects,"LateUpdate");
            Check(objects==Object.All.Count && colliders==Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None).Length && Vector2.Distance(root,d.Player.position)==0f,"Death profile creates per-frame objects or changes gameplay geometry/root.");
            if(source=="GUARD FIRE")
            {
                var pool=art.Where(s=>s.gameObject.name=="Fatal_Gun_Blood_Pool").ToArray();
                Check(pool.Max(s=>s.transform.lossyScale.x)>smallPool && pool.All(s=>s.color.r>s.color.g*5 && s.sortingOrder<playerArt.sortingOrder),"Gun blood does not pool on the floor below the body.");
            }
            if(source=="LASER BARRIER")Check(playerArt.color.r<.06f&&playerArt.color.g<.06f&&playerArt.color.b<.06f,"Laser corpse did not turn charcoal.");
            DumpDeathProfile(mode+"_"+source.Replace(' ','_')+"_late",art);
        }
    }
    private static void DumpDeathProfile(string name,SpriteRenderer[] art)
    {
        var snapshot=art.Concat(new[]{playerArt}).Select(s=>new{name=s.gameObject.name,x=s.transform.position.x-d.Player.position.x,y=s.transform.position.y-d.Player.position.y,w=s.transform.lossyScale.x,h=s.transform.lossyScale.y,angle=s.transform.eulerAngles.z,r=s.color.r,g=s.color.g,b=s.color.b,a=s.color.a,order=s.sortingOrder,enabled=s.enabled,body=s==playerArt}).ToArray();
        File.WriteAllText(Path.Combine(output,"profile_"+name+".json"),JsonSerializer.Serialize(snapshot));
    }
    private static void ReducedBlastBoundary()
    {
        Check(Math.Abs(UXCrimsonHazards.BlastRadius-3.2f*.75f)<.0001f,"Blast radius was not reduced by 25 percent.");
        foreach(UXDifficulty mode in new[]{UXDifficulty.Easy,UXDifficulty.Hard})foreach(bool inside in new[]{true,false})
        {
            World(mode);IsolateMine();Step(.01f);Check(d.Hazards.MineState(0).Armed,"Boundary fixture did not arm.");
            d.Player.position=UXCrimsonHazards.MinePositions[0]+new Vector2(UXCrimsonHazards.BlastRadius+(inside?-.01f:.01f),0f);
            Step(1.12f);Check(d.Hazards.MineState(0).Spent,"Boundary fixture did not detonate.");
            int expected=inside?(mode==UXDifficulty.Hard?15:40):100;
            Check(d.State.Health==expected,"Reduced blast boundary damage mismatch in "+mode+" inside="+inside);
        }
    }
    private static void Bounds()
    {
        foreach (var c in GUI.Calls) Check(c.X >= -1 && c.Y >= -1 && c.W >= 0 && c.H >= 0 && c.X+c.W <= Screen.width+1 && c.Y+c.H <= Screen.height+1, "HUD/result overflows: " + c.Text);
    }
}
