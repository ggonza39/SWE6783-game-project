using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Runs actual movement/cover code with the isolated host, not a physics simulation.
public static class SolidHidingChecks
{
    private static void Call(object item, string method, params object[] args) =>
        item.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(item, args);
    private static void Set(object item, string field, object value) =>
        item.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(item, value);
    private static void Check(bool okay, string message) { if (!okay) throw new Exception(message); }
    private static GameObject At(string name, Vector2 at)
    { var go = new GameObject(name); go.transform.position = at; return go; }
    private static readonly Vector2[] HideAt = {new Vector2(-11,-5),new Vector2(-7,0),new Vector2(-12,4),
        new Vector2(-1,-6),new Vector2(6,-4),new Vector2(1,2),new Vector2(5,6),new Vector2(10,1),new Vector2(10,-6)};

    public static string Run(float[] boxes, float[] routeCoordinates)
    {
        UnityEngine.Object.All.Clear();
        for (int i = 0; i < boxes.Length; i += 4)
        {
            var box = At("Wall", new Vector2(boxes[i], boxes[i+1])).AddComponent<BoxCollider2D>();
            box.size = new Vector2(boxes[i+2], boxes[i+3]);
        }
        foreach (Vector2 at in HideAt)
        {
            var crate = At("HidingSpot", at); crate.tag = "HidingSpot";
            crate.AddComponent<BoxCollider2D>().isTrigger = true;
            At("HidingGlow", at + new Vector2(0,-.2f)).AddComponent<SpriteRenderer>();
        }
        foreach (var at in new [] {new Vector2(-5.5f,-3),new Vector2(-9.5f,2),new Vector2(-3,-3.5f),
            new Vector2(2.5f,0),new Vector2(10.5f,0),new Vector2(3.5f,4),new Vector2(7,4.5f),new Vector2(8,-4.5f),new Vector2(11.5f,-2)})
            At("OpenPassageThreshold",at);
        At("ExtractionZone",new Vector2(11,-6));
        At("Terminal",new Vector2(12,6));
        At("Evidence_1",new Vector2(3,6)); At("Evidence_2",new Vector2(4,-6)); At("Evidence_3",new Vector2(12,3));
        At("SecurityCamera_1",new Vector2(-8,6)); At("SecurityCamera_2",new Vector2(5,7)); At("SecurityCamera_3",new Vector2(12,2));
        var player = At("Player", new Vector2(-12,-6)); player.tag = "Player";
        var playerBody = player.AddComponent<BoxCollider2D>();
        var controller = player.AddComponent<PlayerController>(); Call(controller, "Awake");
        var hiding = player.AddComponent<PlayerHiding>();
        var patrols = new List<UXPreviewGuardFacingPatrol>();
        for (int r = 0; r < routeCoordinates.Length; r += 8)
        {
            var points = new Vector2[4];
            for (int p = 0; p < 4; p++) points[p] = new Vector2(routeCoordinates[r+p*2],routeCoordinates[r+p*2+1]);
            var guard = At("Guard", points[0]); guard.AddComponent<GuardController>();
            guard.AddComponent<BoxCollider2D>().size = new Vector2(.2f,.2f);
            guard.AddComponent<GuardDetection>();
            var patrol = guard.AddComponent<UXPreviewGuardFacingPatrol>();
            Set(patrol, "routePoints", points);
            Set(patrol, "routeEdges", new [] {new Vector2Int(0,1),new Vector2Int(1,2),new Vector2Int(2,3),new Vector2Int(3,0)});
            Call(patrol, "Awake"); patrols.Add(patrol);
        }
        var director = At("Director", Vector2.zero).AddComponent<UXStealthDirector>(); Call(director, "Awake");
        var covers = UnityEngine.Object.FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None);
        Check(covers.Length == 9, "Expected all nine enterable hiding bays");
        var description = new List<string>();
        foreach (var cover in covers)
        {
            var solid = cover.GetComponent<BoxCollider2D>();
            Check(!solid.isTrigger && cover.Pocket != null && cover.Pocket.isTrigger, "Cover and pocket were not separated");
            Check(cover.gameObject.tag != "HidingSpot" && cover.Pocket.CompareTag("HidingSpot"), "Solid body still grants hiding");
            Check(Vector2.Distance(solid.transform.position, cover.Pocket.transform.position) < .001f, "Hide interior must be INSIDE the bay");
            Check(Physics2D.GetIgnoreCollision(solid,playerBody), "Player cannot enter the guard-only exclusion volume");
            player.transform.position = cover.Entrance;
            Check(!hiding.IsHidden, "Standing outside the entrance should not claim HIDDEN");
            Call(hiding,"OnTriggerEnter2D",cover.Pocket);
            Check(!hiding.IsHidden, "Grazing the trigger must not claim HIDDEN before entering");
            Vector2 entry = cover.Entrance;
            for (int step = 0; step <= 50; step++)
            {
                player.transform.position = entry + ((Vector2)cover.transform.position-entry)*(step/50f);
                foreach (BoxCollider2D wall in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
                {
                    if (wall.isTrigger || wall.GetComponentInParent<PlayerController>() != null ||
                        wall.GetComponentInParent<GuardController>() != null || Physics2D.GetIgnoreCollision(wall,playerBody)) continue;
                    Bounds a = playerBody.bounds, b = wall.bounds;
                    Check(a.max.x <= b.min.x || a.min.x >= b.max.x || a.max.y <= b.min.y || a.min.y >= b.max.y,
                        "Player's swept entry path is blocked by a wall or bay frame");
                }
            }
            Check(hiding.IsHidden && hiding.ActiveBay == cover, "Walking INSIDE must activate hiding immediately");
            player.transform.position = cover.Pocket.transform.position;
            foreach (BoxCollider2D wall in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                if (wall.isTrigger || wall.GetComponentInParent<PlayerController>() != null ||
                    wall.GetComponentInParent<GuardController>() != null || Physics2D.GetIgnoreCollision(wall,playerBody)) continue;
                Bounds a = playerBody.bounds, b = wall.bounds;
                Check(a.max.x <= b.min.x || a.min.x >= b.max.x || a.max.y <= b.min.y || a.min.y >= b.max.y,
                    "A hiding pocket places the player in solid geometry");
            }
            Check(!director.ClearNavigationSegment((Vector2)solid.transform.position+new Vector2(-1.5f,0),
                (Vector2)solid.transform.position+new Vector2(1.5f,0)), "Crate missing from navigation sweep");
            description.Add($"({solid.transform.position.x},{solid.transform.position.y}), entry ({cover.Entrance.x},{cover.Entrance.y})");
        }
        UXSolidHidingCover.ConfigureScene(director.gameObject,playerBody);
        Check(UnityEngine.Object.FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None).Length == 9 &&
            GameObject.FindGameObjectsWithTag("HidingSpot").Length == 9, "Repeated setup duplicated cover/pockets");

        // Draw the real HUD marker: hidden feedback must be next to the player as well as the top bar.
        var hud = At("HUD",Vector2.zero).AddComponent<UXHUDController>();
        Call(hud,"CreateTextures"); Call(hud,"InitializeSceneState");
        Camera.main = At("Camera",Vector2.zero).AddComponent<Camera>();
        UnityEngine.GUI.Calls.Clear(); Call(hud,"DrawObjectiveMarkers");
        Check(UnityEngine.GUI.Calls.Exists(x=>x.Text=="HIDDEN"), "Green HIDDEN marker missing above player");
        player.transform.position = covers[0].Entrance;
        UnityEngine.GUI.Calls.Clear(); Call(hud,"DrawObjectiveMarkers");
        Check(UnityEngine.GUI.Calls.Exists(x=>x.Text=="HIDE BAY") && !UnityEngine.GUI.Calls.Exists(x=>x.Text=="HIDDEN"),
            "Outside cue should say HIDE BAY, not HIDDEN");

        // Repeat the reported situation INSIDE every bay for several full patrol circuits.
        Time.timeScale = 1f; Time.deltaTime = .05f;
        foreach (var cover in covers)
        {
            player.transform.position = cover.Pocket.transform.position;
            Call(hiding, "OnTriggerEnter2D", cover.Pocket);
            var visitsBefore = new int[patrols.Count];
            for (int i = 0; i < patrols.Count; i++) visitsBefore[i] = VisitCount(patrols[i]);
            for (int step = 0; step < 2400; step++)
            {
                Time.time += Time.deltaTime;
                foreach (var patrol in patrols)
                {
                    Vector2 before = patrol.transform.position;
                    Call(patrol, "Update");
                    Check(director.ClearNavigationSegment(before, patrol.transform.position), "Normal patrol crossed a solid crate/wall");
                    Check(director.CanGuardTravel(before, patrol.transform.position) ||
                        Vector2.Distance(before, patrol.transform.position) < .00001f, "Normal patrol stepped into the hidden player");
                }
            }
            Check(hiding.IsHidden && Vector2.Distance(player.transform.position, cover.Pocket.transform.position) < .00001f,
                "Guard movement displaced the hidden player");
            for (int i = 0; i < patrols.Count; i++)
                Check(VisitCount(patrols[i]) > visitsBefore[i], "A guard got stuck while player occupied a hiding pocket");
            Call(hiding, "OnTriggerExit2D", cover.Pocket);
        }
        foreach (var patrol in patrols)
            Check((int)patrol.GetType().GetField("previousPoint",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(patrol) >= 0,
                "A patrol never resumed its route");

        // All response phases must obey the same solids and player stand-off rule.
        foreach (var patrol in patrols)
        {
            var response = patrol.GetComponent<UXGuardResponse>(); Call(response, "Start");
            Vector2 anchor = patrol.transform.position;
            player.transform.position = new Vector2(-6,-1);
            typeof(UXStealthDirector).GetProperty("LastKnownPosition").SetValue(director, new Vector2(-7,0));
            director.State.Tick(2f, true, false, false);
            for (int step = 0; step < 500; step++) StepResponse(response,director);
            director.State.Tick(2f, false, false, true);
            Check(director.State.Phase == UXAlertPhase.Searching, "Expected yellow search phase");
            for (int step = 0; step < 120; step++) StepResponse(response,director);
            director.State.Tick(20f, false, false, true);
            player.transform.position = anchor; // Old patrol anchor now occupied: return must not snap here.
            for (int step = 0; step < 3000 && !patrol.enabled; step++) StepResponse(response,director);
            Check(patrol.enabled, "Occupied patrol anchor prevented recovery");
            Check(!Overlaps(response.transform.position, playerBody.bounds), "Returning guard snapped into player");
        }
        Call(director,"OnDestroy");
        return "9 player-enterable / guard-blocked bays; swept player entry; inside/outside hidden state; HIDE BAY/HIDDEN HUD cues; 18 simulated patrol minutes; chase/search/occupied-anchor return. " +
            string.Join("; ",description);
    }
    private static int VisitCount(UXPreviewGuardFacingPatrol patrol)
    {
        int total = 0;
        foreach (int count in (int[])typeof(UXPreviewGuardFacingPatrol).GetField("visits",
            BindingFlags.Instance|BindingFlags.NonPublic).GetValue(patrol)) total += count;
        return total;
    }
    private static bool Overlaps(Vector2 at, Bounds b) => at.x>b.min.x && at.x<b.max.x && at.y>b.min.y && at.y<b.max.y;
    private static void StepResponse(UXGuardResponse response, UXStealthDirector director)
    {
        Vector2 before = response.transform.position;
        Time.time += Time.deltaTime; Call(response,"Update");
        Check(director.ClearNavigationSegment(before,response.transform.position), "Response crossed solid cover/wall");
        Check(director.CanGuardTravel(before,response.transform.position) ||
            Vector2.Distance(before,response.transform.position)<.00001f, "Response stepped into player");
    }
}
