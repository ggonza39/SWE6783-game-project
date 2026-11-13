using System;
using System.Reflection;
using UnityEngine;

public static class ExpandedRuntimeChecks
{
    private static void Call(object item, string method, params object[] arguments)
    { item.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(item, arguments); }
    private static void Set(object item, string field, object value)
    { item.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(item, value); }
    private static GameObject At(string name, float x, float y)
    { var go = new GameObject(name); go.transform.position = new Vector3(x, y); return go; }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Tick(UXStealthDirector director, float seconds)
    { Time.deltaTime = seconds; Time.time += seconds; Call(director, "Update"); }

    public static int Interactions()
    {
        UnityEngine.Object.All.Clear();
        var player = At("Player", -12, -6); player.tag = "Player";
        var body = player.AddComponent<BoxCollider2D>();
        var controller = player.AddComponent<PlayerController>(); Call(controller, "Awake");
        player.AddComponent<PlayerHiding>();
        var manager = At("GameManager", 0, 0).AddComponent<GameManager>(); Call(manager, "Awake");
        var dataManager = At("EvidenceManager", 0, 0).AddComponent<EvidenceManager>(); Call(dataManager, "Awake");
        var terminal = At("Terminal", 12, 6).AddComponent<TerminalInteraction>();
        Set(terminal, "requiredEvidence", 3); terminal.gameObject.AddComponent<UXExpandedInteraction>();
        var gate = At("AccessGate", -3, -3.5f).AddComponent<BoxCollider2D>(); gate.size = new Vector2(.98f, 1.96f);
        At("AccessKeycard", -11, 6);
        var camera = At("SecurityCamera", 10, 6).AddComponent<SecurityCamera>();
        var caches = new EvidenceCollectible[3];
        for (int i = 0; i < 3; i++)
        {
            caches[i] = At("Evidence" + i, i * 3, -5).AddComponent<EvidenceCollectible>();
            caches[i].gameObject.AddComponent<UXExpandedInteraction>();
        }
        var director = dataManager.gameObject.AddComponent<UXStealthDirector>(); Call(director, "Awake");
        int count = 0; Time.timeScale = 1f;
        Tick(director, .1f); Check(!director.KeyAcquired && gate.enabled, "Gate opened before key"); count++;
        player.transform.position = new Vector3(-11, 6); Tick(director, .1f);
        Check(director.KeyAcquired && !gate.enabled && dataManager.EvidenceCollected == 0,
            "Key must open gate without counting as evidence"); count++;
        player.transform.position = terminal.transform.position;
        Call(terminal, "OnTriggerEnter2D", body);
        Input.Keys.Add(KeyCode.E); Tick(director, 5f);
        Check(!manager.TerminalActivated && !camera.IsDisabled, "Terminal bypassed evidence gate"); count++;
        Call(terminal, "OnTriggerExit2D", body);
        for (int i = 0; i < caches.Length; i++)
        {
            player.transform.position = caches[i].transform.position;
            Tick(director, 1f); Input.Keys.Clear(); Tick(director, .1f);
            Check(dataManager.EvidenceCollected == i, "Released download should cancel");
            Input.Keys.Add(KeyCode.E);
            for (int step = 0; step < 26; step++) Tick(director, .1f);
            Check(dataManager.EvidenceCollected == i + 1, "Download did not collect exactly one cache");
            count++;
        }
        Check(!manager.CanExtract(3), "Extraction opened before terminal"); count++;
        player.transform.position = terminal.transform.position; Call(terminal, "OnTriggerEnter2D", body);
        Time.timeScale = 0; Tick(director, 10f);
        Check(!manager.TerminalActivated, "Paused terminal download progressed"); count++;
        Time.timeScale = 1;
        for (int step = 0; step < 41; step++) Tick(director, .1f);
        Check(manager.TerminalActivated && camera.IsDisabled && manager.CanExtract(3),
            "Terminal did not retain authoritative camera/extraction activation"); count++;
        director.State.Tick(2f, true, false, false);
        director.Hit(); director.Hit(); director.Hit();
        Check(director.State.Health == 75 && !manager.GameEnded, "Simultaneous guards ended game too early"); count++;
        Input.Keys.Clear(); Call(director, "OnDestroy");
        return count;
    }

    public static void NavigationReturns(float[] boxes)
    {
        UnityEngine.Object.All.Clear();
        for (int i = 0; i < boxes.Length; i += 4)
        {
            var wall = At("Wall", boxes[i], boxes[i + 1]).AddComponent<BoxCollider2D>();
            wall.size = new Vector2(boxes[i + 2], boxes[i + 3]);
        }
        var player = At("Player", 12, 3).AddComponent<PlayerController>(); Call(player, "Awake");
        player.gameObject.AddComponent<PlayerHiding>();
        var guard = At("Guard", .17f, -5);
        guard.AddComponent<GuardController>(); var detection = guard.AddComponent<GuardDetection>();
        var patrol = guard.AddComponent<UXPreviewGuardFacingPatrol>();
        Set(patrol, "routePoints", new [] {new Vector2(0,-5),new Vector2(5,-5),new Vector2(5,-3),new Vector2(0,-3)});
        Set(patrol, "routeEdges", new [] {new Vector2Int(0,1),new Vector2Int(1,2),new Vector2Int(2,3),new Vector2Int(3,0)});
        Call(patrol, "Awake");
        var director = At("Director", 0, 0).AddComponent<UXStealthDirector>(); Call(director, "Awake");
        typeof(UXStealthDirector).GetProperty("LastKnownPosition").SetValue(director, new Vector2(12,3));
        var response = guard.GetComponent<UXGuardResponse>(); Call(response, "Start");
        director.State.Tick(2f, true, false, false);
        Time.timeScale = 1f; Time.deltaTime = .05f;
        for (int i = 0; i < 3000 && Vector2.Distance(guard.transform.position, new Vector2(12,3)) > 1.2f; i++)
        {
            Vector2 previous = guard.transform.position;
            Time.time += Time.deltaTime; Call(response, "Update");
            Check(director.ClearNavigationSegment(previous, guard.transform.position), "Pursuit cut through a wall");
        }
        Check(Vector2.Distance(guard.transform.position, new Vector2(12,3)) <= 1.2f,
            "Pursuit never reached a safe stand-off from the occupied last known position");
        director.State.Tick(20f, false, false, true);
        for (int i = 0; i < 3000 && !patrol.enabled; i++)
        {
            Vector2 previous = guard.transform.position;
            Time.time += Time.deltaTime; Call(response, "Update");
            Check(director.ClearNavigationSegment(previous, guard.transform.position), "Patrol return cut through a wall");
        }
        Check(patrol.enabled && Vector2.Distance(guard.transform.position, new Vector2(.17f,-5)) < .12f,
            "Guard did not return to the original patrol segment");
        Call(director, "OnDestroy");
    }
}
