using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class Level1FeatureChecks
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Private).SetValue(target, value);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static GameObject At(string name, float x, float y)
    { var go = new GameObject(name); go.transform.position = new Vector3(x, y); return go; }
    private static void Tick(UXStealthDirector director, float seconds)
    { Time.deltaTime = seconds; Time.time += seconds; Call(director, "Update"); }
    private static void ResetEntities()
    {
        foreach (UnityEngine.Object item in UnityEngine.Object.All.ToArray())
            if (item is GameObject) UnityEngine.Object.DestroyImmediate(item);
        UnityEngine.Object.All.Clear(); Input.Keys.Clear();
    }

    public static string Persistence(string directory)
    {
        string path = Path.Combine(directory, "profile.json");
        var save = new UXProgressStore(path);
        save.Data.difficulty = UXDifficulty.Hard; save.Data.fullscreen = false; save.Data.showControlHints = false;
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 180f);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 120f);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 150f);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Hard, 240f);
        Check(save.Save(), "Could not save a valid profile.");
        save = new UXProgressStore(path);
        UXLevelRecord record = save.Data.FindLevel("facility_01");
        Check(save.Data.difficulty == UXDifficulty.Hard && !save.Data.fullscreen && !save.Data.showControlHints &&
            record.easyClears == 3 && record.hardClears == 1 && record.bestEasySeconds == 120f &&
            record.bestHardSeconds == 240f, "Settings or per-mode records failed their disk round trip.");
        Check(save.Save(), "Backup rotation failed.");
        File.WriteAllText(path, "{broken json");
        save = new UXProgressStore(path);
        Check(save.Data.FindLevel("facility_01").easyClears == 3, "Backup did not recover completion.");
        Check(save.Save(), "Recovered profile could not repair the primary.");
        string future = "{\"gameId\":\"PixelRebellion\",\"schemaVersion\":2}";
        File.WriteAllText(path, future); save = new UXProgressStore(path);
        Check(!save.Save() && File.ReadAllText(path) == future, "A newer save version was overwritten.");
        File.Delete(path + ".bak"); File.WriteAllText(path, "{broken"); save = new UXProgressStore(path);
        Check(!save.Save() && File.ReadAllText(path) == "{broken", "Unrecoverable data was overwritten.");
        string blocked = Path.Combine(directory, "directory_instead_of_file"); Directory.CreateDirectory(blocked);
        save = new UXProgressStore(blocked); save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 1f);
        Check(!save.Save() && save.HasSaveError && save.Data.FindLevel("facility_01").easyClears == 1,
            "A failed write destroyed in-memory progress or claimed success.");
        return "disk round trip; separate Easy/Hard records; best-time retention; backup recovery/repair; " +
            "newer/unreadable save protection; write-failure behavior";
    }

    public static string HardMission(float[] boxes)
    {
        ResetEntities(); Time.timeScale = 1f;
        for (int i = 0; i < boxes.Length; i += 4)
        {
            var wall = At("Wall", boxes[i], boxes[i + 1]).AddComponent<BoxCollider2D>();
            wall.size = new Vector2(boxes[i + 2], boxes[i + 3]);
        }
        var player = At("Player", -12f, -6f); player.tag = "Player";
        var body = player.AddComponent<BoxCollider2D>(); body.size = new Vector2(0.45f, 0.65f);
        var controller = player.AddComponent<PlayerController>(); Call(controller, "Awake");
        player.AddComponent<PlayerHiding>();
        var manager = At("GameManager", 0, 0).AddComponent<GameManager>(); Call(manager, "Awake");
        var data = At("EvidenceManager", 0, 0).AddComponent<EvidenceManager>(); Call(data, "Awake");
        var terminal = At("Terminal", 12, 6).AddComponent<TerminalInteraction>(); Set(terminal, "requiredEvidence", 3);
        terminal.gameObject.AddComponent<UXExpandedInteraction>();
        At("AccessKeycard", -11, 6);
        var gate = At("AccessGate", -3, -3.5f).AddComponent<BoxCollider2D>(); gate.size = new Vector2(.98f, 1.96f);
        Vector2[] positions = { new Vector2(3, 6), new Vector2(4, -6), new Vector2(12, 3) };
        var caches = new EvidenceCollectible[3];
        for (int i = 0; i < 3; i++)
        {
            caches[i] = At("Evidence" + i, positions[i].x, positions[i].y).AddComponent<EvidenceCollectible>();
            caches[i].gameObject.AddComponent<UXExpandedInteraction>();
        }
        foreach (Vector2 point in new[] { new Vector2(-11,-5), new Vector2(-7,0), new Vector2(-12,4),
            new Vector2(-1,-6), new Vector2(6,-4), new Vector2(1,2), new Vector2(5,6),
            new Vector2(10,1), new Vector2(10,-6) })
        {
            var bay = At("HidingSpot",point.x,point.y); bay.tag = "HidingSpot";
            bay.AddComponent<BoxCollider2D>().isTrigger = true;
        }
        var patrolGuard = At("Guard",0,-5); patrolGuard.AddComponent<GuardController>();
        var patrolSight = patrolGuard.AddComponent<GuardDetection>();
        Set(patrolSight,"detectionRange",3.4f); Set(patrolSight,"detectionAngle",55f);
        var patrol = patrolGuard.AddComponent<UXPreviewGuardFacingPatrol>();
        Set(patrol,"speed",1.2f); Set(patrol,"turnDuration",.75f);
        Set(patrol,"minimumPause",1f); Set(patrol,"maximumPause",1.5f);
        Set(patrol,"routePoints",new[]{new Vector2(0,-5),new Vector2(5,-5),new Vector2(5,-3),new Vector2(0,-3)});
        Set(patrol,"routeEdges",new[]{new Vector2Int(0,1),new Vector2Int(1,2),new Vector2Int(2,3),new Vector2Int(3,0)});
        Call(patrol,"Awake");
        var director = At("Director", 0, 0).AddComponent<UXStealthDirector>(); Call(director, "Awake");
        director.BeginMission(UXDifficulty.Hard);
        Check(director.AccessLocks.Length == 1 && !director.ExtractionLocksReleased &&
            director.SealedAlternativeEntrance != null && director.SealedAlternativeEntrance.enabled,
            "Hard must have one exit door and a sealed alternate entrance.");
        player.transform.position = new Vector3(-50, -50);
        Check(director.Path(new Vector2(-12,-6), new Vector2(-11,6)).Count > 0, "Key became unreachable.");
        player.transform.position = new Vector3(-11, 6); Tick(director, .1f);
        Check(director.KeyAcquired && !gate.enabled, "Original key/gate progression broke.");
        player.transform.position = new Vector3(-50,-50);
        foreach (UXSolidHidingCover bay in UnityEngine.Object.FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None))
            Check(director.ClearNavigationSegment(bay.Entrance,bay.Entrance), "A new lock blocked a hiding-bay entrance.");
        foreach (Vector2 target in positions.Concat(new[] {new Vector2(12,6), new Vector2(7,-4.5f)}))
        {
            var route = director.Path(new Vector2(-11,6), target);
            Check(route.Count > 0 && Vector2.Distance(route[route.Count-1], target) <= 0.6f,
                "An objective/lock control is behind a closed lock: " + target.x + "," + target.y);
        }
        Check(director.Path(new Vector2(7,-4.5f), new Vector2(11,-6)).Count == 0,
            "The closed extraction doors can be bypassed geometrically.");
        // Test the scene's actual patrol tuning. Ignoring raycast occlusion here
        // overestimates visibility, so a clear window still gives a useful bound.
        player.transform.position = director.AccessLocks[0].transform.position;
        Call(patrolSight,"Start");
        float clearWindow = 0f, longestWindow = 0f; int seenFrames = 0;
        for(int step=0;step<2400;step++)
        {
            Time.deltaTime=.05f;Time.time+=.05f;
            Call(patrol,"Update");Call(patrolSight,"Update");
            if(patrolSight.IsPlayerDetected) {seenFrames++;clearWindow=0f;}
            else {clearWindow+=.05f;longestWindow=Math.Max(longestWindow,clearWindow);}
        }
        Check(seenFrames > 0 && longestWindow >= director.Difficulty.ExitDoorDuration,
            "The exit override has no full safe patrol window.");
        typeof(GuardDetection).GetProperty("IsPlayerDetected").SetValue(patrolSight,false);
        player.transform.position = director.AccessLocks[0].transform.position;
        Input.Keys.Add(KeyCode.E);
        for (int step = 0; step < 90; step++) Tick(director,.1f);
        Check(!director.ExtractionLocksReleased && director.InteractionProgress == 0f,
            "The exit override was available before the original objectives were complete.");
        Input.Keys.Clear();
        Input.Keys.Add(KeyCode.E);
        for (int i = 0; i < 3; i++)
        {
            player.transform.position = caches[i].transform.position;
            for (int step = 0; step < 48; step++) Tick(director, .1f);
            Check(data.EvidenceCollected == i, "Hard evidence completed before its 5-second hold.");
            Input.Keys.Clear(); Tick(director, .1f); Input.Keys.Add(KeyCode.E);
            for (int step = 0; step < 51; step++) Tick(director, .1f);
            Check(data.EvidenceCollected == i+1, "Hard download did not finish after its full hold.");
        }
        player.transform.position = terminal.transform.position; Call(terminal, "OnTriggerEnter2D", body);
        for (int step = 0; step < 78; step++) Tick(director, .1f);
        Check(!manager.TerminalActivated, "Hard terminal finished too early.");
        Time.timeScale = 0; Tick(director, 10f); Check(!manager.TerminalActivated, "Pause advanced interaction.");
        Time.timeScale = 1; for (int step = 0; step < 3; step++) Tick(director, .1f);
        Check(manager.TerminalActivated && !manager.CanExtract(3), "Terminal bypassed the required exit door.");
        Call(terminal, "OnTriggerExit2D", body);
        foreach (UXAccessLock access in director.AccessLocks)
        {
            player.transform.position = access.transform.position;
            for (int step = 0; step < 78; step++) Tick(director, .1f);
            Check(!access.IsUnlocked, "The eight-second exit door opened early.");
            Input.Keys.Clear(); Tick(director,.1f); Input.Keys.Add(KeyCode.E);
            for (int step = 0; step < 81; step++) Tick(director, .1f);
            Check(access.IsUnlocked && !access.Door.enabled, "Lock could not be opened by its reachable control.");
        }
        Check(manager.CanExtract(3) && director.ExtractionLocksReleased, "Hard extraction did not open after all objectives.");
        player.transform.position = new Vector3(-50,-50);
        var exitRoute = director.Path(new Vector2(7,-4.5f),new Vector2(11,-6));
        Check(exitRoute.Count > 0 && exitRoute.Any(point => point.x >= 7.5f && point.x <= 8.5f &&
            point.y >= -5.5f && point.y <= -3.5f), "The route does not go through the one open exit door.");
        Check(!director.ClearNavigationSegment(new Vector2(11.5f,-1),new Vector2(11.5f,-3)),
            "The sealed northern entrance reopened or became a bypass.");
        director.State.Tick(.8f,false,true,false);
        Check(director.State.Phase == UXAlertPhase.Engaged, "Hard detection did not react sooner.");
        director.Hit(); director.Hit(); Check(director.State.Health == 66, "Hard damage/protection did not apply.");
        director.BeginMission(UXDifficulty.Easy);
        Check(director.Difficulty.Mode == UXDifficulty.Hard && director.State.Health == 66,
            "Difficulty changed or healed the player during the mission.");
        var metrics = At("Metrics",0,0).AddComponent<GameplayMetrics>(); GameplayMetrics.Instance = metrics;
        metrics.ElapsedSeconds = 300f;
        manager.WinGame(); manager.WinGame();
        Check(UXPlayerProgress.Data.FindLevel("facility_01").hardClears == 1,
            "Victory did not save exactly one completion.");
        Input.Keys.Clear(); Call(director, "OnDestroy");
        return "one reachable exit control; alternate route sealed; original objectives required; 5s/8s/8s holds; " +
            "cancellation/pause; exit route crosses the open door; 34 damage with shared protection; saved victory; " +
            "clear exit-override patrol window " + longestWindow.ToString("F1") + "s";
    }

    public static string Menu(string directory)
    {
        UnityEngine.Object.All.Clear(); Input.Keys.Clear();
        var menu = new UXMissionMenuScreen();
        foreach (var size in new[] {new[]{960,540},new[]{1280,720},new[]{1920,1080},new[]{2560,1440}})
        {
            Screen.width = size[0]; Screen.height = size[1];
            foreach (bool settings in new[] {false,true})
            {
                Set(menu,"settingsVisible",settings); GUI.Calls.Clear(); menu.Draw();
                foreach (var call in GUI.Calls)
                    Check(call.X >= -1 && call.Y >= -1 && call.X+call.W <= Screen.width+1 &&
                        call.Y+call.H <= Screen.height+1, "Menu overflow: " + call.Text);
                File.WriteAllText(Path.Combine(directory,(settings?"settings_":"menu_")+Screen.width+".json"),
                    System.Text.Json.JsonSerializer.Serialize(GUI.Calls,
                        new System.Text.Json.JsonSerializerOptions {IncludeFields=true}));
            }
        }
        return "home/settings layout bounds at four resolutions";
    }

    private static int ExposureHealth(UXDifficulty mode)
    {
        ResetEntities(); Time.timeScale = 1f;
        var player = At("Player", 1, 0); player.tag = "Player";
        player.AddComponent<BoxCollider2D>(); player.AddComponent<PlayerHiding>();
        var controller = player.AddComponent<PlayerController>(); Call(controller,"Awake");
        var manager = At("GameManager",0,0).AddComponent<GameManager>(); Call(manager,"Awake");
        var guard = At("Guard",0,0); guard.AddComponent<GuardController>();
        var sight = guard.AddComponent<GuardDetection>();
        var director = At("Director",0,0).AddComponent<UXStealthDirector>(); Call(director,"Awake");
        director.BeginMission(mode); Call(sight,"Start");
        // A 30-degree offset is outside Easy's actual cone and inside Hard's.
        player.transform.position = new Vector3(1.732f,1f);
        Call(sight,"Update");
        Check(sight.IsPlayerDetected == (mode == UXDifficulty.Hard), "Detection does not match the selected cone.");
        player.transform.position = new Vector3(1f,0f); Call(sight,"Update");
        director.State.Tick(2f,true,false,false);
        var response = guard.GetComponent<UXGuardResponse>(); Call(response,"Start");
        for(int i=0;i<22;i++) { Tick(director,.1f); Call(response,"Update"); }
        int health = director.State.Health; Call(director,"OnDestroy"); return health;
    }

    public static string GuardPressure()
    {
        int easy = ExposureHealth(UXDifficulty.Easy), hard = ExposureHealth(UXDifficulty.Hard);
        Check(hard < easy && hard > 0, "Hard did not increase guard pressure while retaining a recovery window.");
        return "same 2.2-second exposure: Easy health " + easy + ", Hard health " + hard +
            "; actual detector matches its mode-specific vision cone";
    }
}
