using System.Collections.Generic;
using UnityEngine;

// Presentation only: the HUD still starts the mission and owns pause/session state.
public sealed class UXMissionBriefingScreen
{
    private static readonly Color Ink = new Color(0.015f, 0.027f, 0.050f);
    private static readonly Color Panel = new Color(0.025f, 0.065f, 0.095f);
    private static readonly Color Cyan = new Color(0.18f, 0.88f, 0.96f);
    private static readonly Color Muted = new Color(0.45f, 0.66f, 0.73f);
    private static readonly Color Gold = new Color(1f, 0.77f, 0.28f);
    private static readonly Color Green = new Color(0.36f, 0.96f, 0.61f);

    private readonly List<Rect> solidBounds = new List<Rect>();
    private readonly List<Vector2> dataPositions = new List<Vector2>();
    private readonly List<Vector2> guardPositions = new List<Vector2>();
    private readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
    private readonly int evidenceCount;
    private readonly Texture2D sneakSheet;
    private readonly Vector2 playerPosition;
    private readonly Vector2 terminalPosition;
    private readonly Vector2 exitPosition;
    private readonly Rect worldBounds;
    private float scale;
    private Vector2 offset;
    private GUIStyle deployStyle;
    private int deployFontSize;
    private readonly bool expanded;
    private readonly Vector2 keyPosition;
    private readonly Vector2 gatePosition;
    private bool tacticalMap;

    public UXMissionBriefingScreen(int totalEvidence, bool expandedFacility = false)
    {
        expanded = expandedFacility;
        evidenceCount = Mathf.Max(1, totalEvidence);
        sneakSheet = Resources.Load<Texture2D>("UXArtDeluxe/player_sneak_sheet");
        GameObject player = GameObject.Find("Player");
        GameObject terminal = GameObject.Find("Terminal");
        GameObject exit = GameObject.Find("ExtractionZone");
        playerPosition = player != null ? (Vector2)player.transform.position : Vector2.zero;
        terminalPosition = terminal != null ? (Vector2)terminal.transform.position : Vector2.zero;
        exitPosition = exit != null ? (Vector2)exit.transform.position : Vector2.zero;
        GameObject key = GameObject.Find("AccessKeycard"), gate = GameObject.Find("AccessGate");
        keyPosition = key != null ? (Vector2)key.transform.position : Vector2.zero;
        gatePosition = gate != null ? (Vector2)gate.transform.position : Vector2.zero;

        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        foreach (BoxCollider2D collider in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (!collider.enabled || collider.isTrigger ||
                collider.GetComponentInParent<PlayerController>() != null ||
                collider.GetComponentInParent<GuardController>() != null)
                continue;

            if (expanded && collider.gameObject.name == "AccessGate") continue;
            Bounds box = collider.bounds;
            solidBounds.Add(new Rect(box.min.x, box.min.y, box.size.x, box.size.y));
            if (!hasBounds) { bounds = box; hasBounds = true; }
            else bounds.Encapsulate(box);
        }

        worldBounds = hasBounds
            ? new Rect(bounds.min.x - 0.5f, bounds.min.y - 0.5f,
                bounds.size.x + 1f, bounds.size.y + 1f)
            : new Rect(-10f, -6f, 20f, 12f);

        foreach (EvidenceCollectible item in Object.FindObjectsByType<EvidenceCollectible>(FindObjectsSortMode.None))
            dataPositions.Add(item.transform.position);
        foreach (GuardDetection guard in Object.FindObjectsByType<GuardDetection>(FindObjectsSortMode.None))
            guardPositions.Add(guard.transform.position);
    }

    public bool Draw()
    {
        // Fit the entire screen together so buttons and text share one layout.
        scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
        offset = new Vector2((Screen.width - 1280f * scale) / 2f,
            (Screen.height - 800f * scale) / 2f);
        Color previousColor = GUI.color;
        GUI.color = Ink;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        DrawBackground();
        Label(new Rect(48f, 38f, 760f, 25f),
            "REBEL NETWORK  /  ENCRYPTED MISSION UPLINK", 13, Muted, true);
        Fill(new Rect(1044f, 42f, 7f, 7f), Green);
        Label(new Rect(1060f, 32f, 172f, 26f), "UPLINK SECURE", 12, Green, true,
            TextAnchor.MiddleRight);
        Fill(new Rect(48f, 79f, 1184f, 1f), new Color(0.12f, 0.35f, 0.43f));

        Label(new Rect(48f, 110f, 640f, 28f),
            expanded ? "OPERATION 01  //  FACILITY BREACH" : "OPERATION 01  //  NEON FACILITY", 15, Cyan, true);
        Label(new Rect(44f, 147f, 670f, 94f), "PIXEL", 78, Color.white, true);
        Label(new Rect(44f, 228f, 680f, 82f), "REBELLION", 64, Color.white, true);
        Fill(new Rect(50f, 321f, 54f, 4f), Cyan);
        Fill(new Rect(110f, 321f, 18f, 4f), new Color(0.94f, 0.34f, 0.62f));
        Label(new Rect(142f, 306f, 450f, 35f),
            "SHADOW PROTOCOL", 21, Cyan, true);
        Label(new Rect(48f, 365f, 520f, 60f),
            expanded ? "Steal the key. Infiltrate the research wing.\nSecure the data. Escape the lockdown." :
            "Infiltrate. Secure the evidence.\nHack security. Reach extraction.", 21, Color.white);
        Label(new Rect(48f, 433f, 500f, 24f),
            "STAY UNSEEN. GET OUT CLEAN.", 13, Muted, true);
        DrawAgent();
        DrawFacility(new Rect(734f, 110f, 498f, 347f));

        DrawObjective(new Rect(48f, 486f, 384f, 122f), "01", expanded ? "BREACH THE GATE" : "SECURE THE DATA",
            expanded ? "Find the gold access keycard.\nIt opens the marked security gate." :
            "Collect " + evidenceCount + " evidence " + (evidenceCount == 1 ? "cache." : "caches.") +
            "\nWalk over each to pick it up.", Gold);
        DrawObjective(new Rect(448f, 486f, 384f, 122f), "02", "CUT THE CAMERAS",
            expanded ? "Hold E at 3 data caches (2.5s each).\nThen hold E at the terminal (4s)." :
            "With all data, approach the terminal.\nPress E to hack security.", Cyan);
        DrawObjective(new Rect(848f, 486f, 384f, 122f), "03", "REACH EXTRACTION",
            expanded ? "Green exit: service route or east flank.\nBreak sight, hide, let search clear." :
            "Head for the green exit.\nAll objectives must be complete.", Green);

        DrawControls();
        bool deploy = DrawDeploy(new Rect(852f, 632f, 380f, 84f));
        Fill(new Rect(48f, 746f, 4f, 26f), Gold);
        Label(new Rect(62f, 741f, 1170f, 37f),
            expanded ? "RED: PURSUIT.  YELLOW: SEARCH.  Hide to reset security. Health does not regenerate. TAB: facility map." :
            "DETECTION ENDS THE MISSION.  Move crouched by default. Marked hiding spots conceal you.",
            13, Gold, true);
        return deploy;
    }

    public void DrawMapOverlay()
    {
        scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
        offset = new Vector2((Screen.width - 1280f * scale) / 2f, (Screen.height - 800f * scale) / 2f);
        tacticalMap = true;
        DrawFacility(new Rect(210f, 140f, 860f, 520f));
        tacticalMap = false;
    }

    private void DrawBackground()
    {
        Color grid = new Color(0.14f, 0.53f, 0.63f, 0.075f);
        for (float x = 0f; x < 1280f; x += 40f)
            Fill(new Rect(x, 0f, 1f, 800f), grid);
        for (float y = 0f; y < 800f; y += 40f)
            Fill(new Rect(0f, y, 1280f, 1f), grid);
        // This motion uses unscaled time so the paused briefing remains animated.
        float pulse = 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 1.8f);
        Fill(new Rect(0f, 0f, 6f, 800f), new Color(Cyan.r, Cyan.g, Cyan.b, pulse));
        Fill(new Rect(1274f, 0f, 6f, 800f), new Color(Cyan.r, Cyan.g, Cyan.b, pulse));
    }

    private void DrawAgent()
    {
        Rect panel = new Rect(584f, 343f, 112f, 114f);
        Fill(panel, Panel);
        Border(panel, new Color(0.12f, 0.37f, 0.44f));
        if (sneakSheet != null)
        {
            int frame = Mathf.FloorToInt(Time.unscaledTime * 4f) % 4;
            int cellWidth = sneakSheet.width / 4;
            int cellHeight = sneakSheet.height / 4;
            Rect uv = new Rect(frame * cellWidth / (float)sneakSheet.width,
                (sneakSheet.height - cellHeight) / (float)sneakSheet.height,
                cellWidth / (float)sneakSheet.width, cellHeight / (float)sneakSheet.height);
            GUI.DrawTextureWithTexCoords(ScreenRect(new Rect(589f, 344f, 102f, 102f)), sneakSheet, uv);
        }
        Label(new Rect(588f, 434f, 104f, 18f), "SILENT ENTRY", 9, Cyan, true,
            TextAnchor.MiddleCenter);
    }

    private void DrawFacility(Rect panel)
    {
        Fill(panel, Panel);
        Border(panel, new Color(0.12f, 0.37f, 0.44f));
        Label(new Rect(panel.x + 20f, panel.y + 13f, panel.width - 40f, 26f),
            tacticalMap ? "FACILITY BLUEPRINT  /  RELEASE TAB TO CLOSE" : "FACILITY SCAN  /  MISSION ROUTE", 13, Cyan, true);

        Rect available = new Rect(panel.x + 18f, panel.y + 52f, panel.width - 36f, panel.height - 99f);
        float unit = Mathf.Min(available.width / worldBounds.width,
            available.height / worldBounds.height);
        Rect map = new Rect(available.center.x - worldBounds.width * unit / 2f,
            available.center.y - worldBounds.height * unit / 2f,
            worldBounds.width * unit, worldBounds.height * unit);
        Fill(map, new Color(0.035f, 0.095f, 0.13f));
        for (float x = Mathf.Ceil(worldBounds.xMin); x < worldBounds.xMax; x++)
        {
            float sx = MapPoint(new Vector2(x, worldBounds.yMin), map).x;
            Fill(new Rect(sx, map.y, 0.7f, map.height), new Color(0.12f, 0.24f, 0.29f));
        }
        for (float y = Mathf.Ceil(worldBounds.yMin); y < worldBounds.yMax; y++)
        {
            float sy = MapPoint(new Vector2(worldBounds.xMin, y), map).y;
            Fill(new Rect(map.x, sy, map.width, 0.7f), new Color(0.12f, 0.24f, 0.29f));
        }

        foreach (Rect solid in solidBounds)
        {
            Vector2 topLeft = MapPoint(new Vector2(solid.xMin, solid.yMax), map);
            Rect wall = new Rect(topLeft.x, topLeft.y, solid.width * unit, solid.height * unit);
            Fill(wall, new Color(0.24f, 0.38f, 0.46f));
            Fill(new Rect(wall.x, wall.y, wall.width, Mathf.Min(2f, wall.height)),
                new Color(0.38f, 0.58f, 0.65f));
        }

        if (tacticalMap)
        {
            foreach (EvidenceCollectible data in Object.FindObjectsByType<EvidenceCollectible>(FindObjectsSortMode.None))
                DrawMapMarker(MapPoint(data.transform.position, map), Gold, 8f);
        }
        else foreach (Vector2 data in dataPositions) DrawMapMarker(MapPoint(data, map), Gold, 8f);
        DrawMapMarker(MapPoint(terminalPosition, map), Cyan, 8f);
        DrawMapMarker(MapPoint(exitPosition, map), Green, 11f);
        GameObject currentPlayer = tacticalMap ? GameObject.Find("Player") : null;
        DrawMapMarker(MapPoint(currentPlayer != null ? (Vector2)currentPlayer.transform.position : playerPosition, map), Color.white, 7f);
        if (expanded)
        {
            bool hasKey = tacticalMap && GameObject.Find("AccessKeycard") == null;
            Vector2 keyPoint = MapPoint(keyPosition, map), gatePoint = MapPoint(gatePosition, map);
            if (!hasKey)
            {
                DrawMapMarker(keyPoint, Gold, 11f);
                Label(new Rect(keyPoint.x - 18f, keyPoint.y - 23f, 44f, 16f), "KEY", 10, Gold, true);
            }
            DrawMapMarker(gatePoint, hasKey ? Green : new Color(1f, 0.25f, 0.25f), 10f);
            Label(new Rect(gatePoint.x + 8f, gatePoint.y - 9f, 54f, 16f),
                hasKey ? "OPEN" : "LOCKED", 10, hasKey ? Green : Gold, true);
        }
        if (!tacticalMap) foreach (Vector2 guard in guardPositions)
        {
            Vector2 position = MapPoint(guard, map);
            float blink = 0.45f + 0.3f * Mathf.Sin(Time.unscaledTime * 3f);
            Border(new Rect(position.x - 8f, position.y - 8f, 16f, 16f),
                new Color(1f, 0.42f, 0.28f, blink));
            DrawMapMarker(position, new Color(1f, 0.42f, 0.28f), 5f);
        }

        float scan = Mathf.Repeat(Time.unscaledTime * 0.16f, 1f);
        Fill(new Rect(map.x + scan * (map.width - 2f), map.y, 2f, map.height),
            new Color(0.2f, 0.95f, 1f, 0.45f));
        Label(new Rect(panel.x + 18f, panel.yMax - 34f, 130f, 22f), "DATA", 11, Gold, true);
        Label(new Rect(panel.x + 148f, panel.yMax - 34f, 140f, 22f), "TERMINAL", 11, Cyan, true);
        Label(new Rect(panel.x + 303f, panel.yMax - 34f, 150f, 22f), "EXTRACTION", 11, Green, true);
    }

    private Vector2 MapPoint(Vector2 point, Rect map)
    {
        return new Vector2(map.x + (point.x - worldBounds.xMin) / worldBounds.width * map.width,
            map.yMax - (point.y - worldBounds.yMin) / worldBounds.height * map.height);
    }

    private void DrawMapMarker(Vector2 center, Color color, float size)
    {
        Fill(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), color);
    }

    private void DrawObjective(Rect panel, string number, string title, string copy, Color accent)
    {
        Fill(panel, Panel);
        Border(panel, new Color(0.12f, 0.28f, 0.35f));
        Fill(new Rect(panel.x, panel.y, panel.width, 3f), accent);
        Label(new Rect(panel.x + 17f, panel.y + 15f, 35f, 30f), number, 22, accent, true);
        Label(new Rect(panel.x + 61f, panel.y + 15f, panel.width - 75f, 30f), title, 18, Color.white, true);
        Label(new Rect(panel.x + 18f, panel.y + 57f, panel.width - 36f, 52f), copy, 16, Muted);
    }

    private void DrawControls()
    {
        Fill(new Rect(48f, 632f, 772f, 84f), Panel);
        Border(new Rect(48f, 632f, 772f, 84f), new Color(0.12f, 0.28f, 0.35f));
        DrawControl(65f, 644f, 142f, "WASD / ARROWS", "MOVE");
        DrawControl(323f, 644f, 94f, "C / CTRL", "EXTRA SLOW");
        DrawControl(579f, 644f, 60f, "SHIFT", "SPRINT");
        DrawControl(65f, 681f, 30f, "E", expanded ? "HOLD / USE" : "TERMINAL");
        DrawControl(323f, 681f, 44f, "ESC", "PAUSE");
        DrawControl(579f, 681f, expanded ? 44f : 30f, expanded ? "TAB" : "R", expanded ? "MAP" : "RESTART");
    }

    private void DrawControl(float x, float y, float keyWidth, string key, string action)
    {
        Fill(new Rect(x, y, keyWidth, 24f), new Color(0.075f, 0.17f, 0.21f));
        Label(new Rect(x, y, keyWidth, 24f), key, 12, Cyan, true, TextAnchor.MiddleCenter);
        Label(new Rect(x + keyWidth + 10f, y, 100f, 24f), action, 12, Color.white, true,
            TextAnchor.MiddleLeft);
    }

    private bool DrawDeploy(Rect rect)
    {
        Rect screen = ScreenRect(rect);
        bool hovered = screen.Contains(Event.current.mousePosition);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
        Border(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f),
            new Color(Cyan.r, Cyan.g, Cyan.b, hovered ? 1f : 0.25f + pulse * 0.3f));
        Fill(rect, hovered ? new Color(0.55f, 1f, 1f) : Cyan);
        int font = Mathf.Max(10, Mathf.RoundToInt(25f * scale));
        if (deployStyle == null || deployFontSize != font)
        {
            deployFontSize = font;
            deployStyle = new GUIStyle(GUIStyle.none)
            {
                fontSize = font, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            deployStyle.normal.textColor = Ink;
            deployStyle.hover.textColor = Ink;
            deployStyle.active.textColor = Ink;
            deployStyle.focused.textColor = Ink;
        }
        GUI.SetNextControlName("UXDeployMission");
        bool clicked = GUI.Button(screen, "DEPLOY  >", deployStyle);
        Label(new Rect(rect.x, rect.yMax + 9f, rect.width, 22f),
            "ENTER / SPACE  TO DEPLOY", 11, Muted, true, TextAnchor.MiddleCenter);
        return clicked;
    }

    private Rect ScreenRect(Rect rect)
    {
        return new Rect(offset.x + rect.x * scale, offset.y + rect.y * scale,
            rect.width * scale, rect.height * scale);
    }

    private void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(ScreenRect(rect), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void Border(Rect rect, Color color)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, 1f), color);
        Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
        Fill(new Rect(rect.x, rect.y, 1f, rect.height), color);
        Fill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
    }

    private void Label(Rect rect, string text, int size, Color color, bool bold = false,
        TextAnchor alignment = TextAnchor.UpperLeft)
    {
        int font = Mathf.Max(8, Mathf.RoundToInt(size * scale));
        int key = font * 32 + (int)alignment * 2 + (bold ? 1 : 0);
        if (!styles.TryGetValue(key, out GUIStyle style))
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = font,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = alignment,
                wordWrap = true,
                padding = new RectOffset(0, 0, 0, 0)
            };
            styles.Add(key, style);
        }
        style.normal.textColor = color;
        GUI.Label(ScreenRect(rect), text, style);
    }
}
