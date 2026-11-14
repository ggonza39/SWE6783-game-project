using System.Collections.Generic;
using UnityEngine;

// The expanded scene's front menu; the HUD continues to own deployment and pause.
public sealed class UXMissionMenuScreen
{
    private static readonly Color Ink = new Color(0.015f, 0.027f, 0.050f);
    private static readonly Color Panel = new Color(0.025f, 0.065f, 0.095f);
    private static readonly Color Cyan = new Color(0.18f, 0.88f, 0.96f);
    private static readonly Color Muted = new Color(0.62f, 0.76f, 0.81f);
    private static readonly Color Green = new Color(0.36f, 0.96f, 0.61f);
    private static readonly Color Gold = new Color(1f, 0.77f, 0.28f);
    private readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
    private float scale;
    private Vector2 offset;
    private bool settingsVisible;
    public bool BriefingVisible { get; private set; }

    public void ShowHome() { BriefingVisible = false; settingsVisible = false; }

    public void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { ShowHome(); return; }
        if (settingsVisible) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) UXPlayerProgress.SelectDifficulty(UXDifficulty.Easy);
        if (Input.GetKeyDown(KeyCode.RightArrow)) UXPlayerProgress.SelectDifficulty(UXDifficulty.Hard);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space)) BriefingVisible = true;
    }

    private void Fit()
    {
        scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
        offset = new Vector2((Screen.width - 1280f * scale) / 2f,
            (Screen.height - 800f * scale) / 2f);
    }

    public void Draw()
    {
        Fit();
        Color previous = GUI.color;
        GUI.color = Ink;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
        for (float x = 0f; x < 1280f; x += 40f)
            Fill(new Rect(x, 0f, 1f, 800f), new Color(0.10f, 0.28f, 0.35f, 0.25f));
        for (float y = 0f; y < 800f; y += 40f)
            Fill(new Rect(0f, y, 1280f, 1f), new Color(0.10f, 0.28f, 0.35f, 0.25f));
        Label(new Rect(48f, 38f, 900f, 30f), "REBEL NETWORK  /  OPERATIONS", 14, Cyan, true);
        Label(new Rect(46f, 85f, 1188f, 65f), "PIXEL REBELLION", 46, Color.white, true);
        Label(new Rect(48f, 156f, 1100f, 28f),
            settingsVisible ? "SHADOW PROTOCOL  /  SETTINGS" : "SHADOW PROTOCOL  /  SELECT YOUR APPROACH", 16, Muted);

        if (settingsVisible) DrawSettings();
        else DrawHome();

        Fill(new Rect(48f, 691f, 1184f, 1f), new Color(0.12f, 0.35f, 0.43f));
        Label(new Rect(48f, 711f, 1184f, 25f), UXPlayerProgress.Store.Status, 12,
            UXPlayerProgress.Store.HasSaveError ? Gold : Green, true);
        Label(new Rect(48f, 746f, 1184f, 23f), settingsVisible ? "ESC: BACK TO MENU" :
            "LEFT / RIGHT: DIFFICULTY     ENTER: MISSION BRIEFING", 12, Muted);
    }

    private void DrawHome()
    {
        Rect mission = new Rect(48f, 212f, 488f, 425f);
        Fill(mission, Panel); Border(mission, new Color(0.12f, 0.35f, 0.43f));
        Label(new Rect(72f, 234f, 440f, 28f), "OPERATION 01", 14, Cyan, true);
        Label(new Rect(72f, 278f, 440f, 43f), "FACILITY BREACH", 28, Color.white, true);
        Label(new Rect(72f, 338f, 432f, 75f),
            "Secure the keycard and evidence.\nHack security. Reach extraction.", 19, Muted);
        Label(new Rect(72f, 434f, 440f, 25f), "SAVED MISSION RECORD", 12, Cyan, true);
        Label(new Rect(72f, 470f, 440f, 33f), RecordLabel(UXPlayerProgress.Data.difficulty), 16, Green, true);
        UXLevelRecord record = UXPlayerProgress.Data.FindLevel(UXPlayerProgress.FacilityLevelId);
        bool cleared = record != null && record.Completed(UXPlayerProgress.Data.difficulty);
        if (Button(new Rect(72f, 536f, 440f, 62f), cleared ? "REPLAY LEVEL 1  >" : "START LEVEL 1  >", true))
            BriefingVisible = true;
        Label(new Rect(72f, 607f, 440f, 20f), "MISSION BRIEFING AND CONTROLS NEXT", 11, Muted,
            true, TextAnchor.MiddleCenter);

        Label(new Rect(560f, 222f, 672f, 34f), "CHOOSE YOUR DIFFICULTY", 22, Color.white, true);
        DrawDifficultyCard(new Rect(560f, 278f, 672f, 96f), UXDifficulty.Easy, Cyan);
        DrawDifficultyCard(new Rect(560f, 400f, 672f, 96f), UXDifficulty.Hard, Gold);
        if (Button(new Rect(560f, 546f, 324f, 52f), "SETTINGS")) settingsVisible = true;
        if (!Application.isEditor && Button(new Rect(908f, 546f, 324f, 52f), "EXIT GAME")) Application.Quit();
        Label(new Rect(560f, 611f, 672f, 25f), "COMPLETION AND BEST TIME ARE SAVED FOR EACH MODE", 11, Muted, true);
    }

    private void DrawDifficultyCard(Rect bounds, UXDifficulty mode, Color accent)
    {
        bool selected = UXPlayerProgress.Data.difficulty == mode;
        Fill(bounds, selected ? new Color(0.045f, 0.14f, 0.18f) : Panel);
        Border(bounds, selected ? accent : new Color(0.12f, 0.30f, 0.37f), selected ? 2f : 1f);
        if (selected) Fill(new Rect(bounds.x, bounds.y, 5f, bounds.height), accent);
        Label(new Rect(bounds.x + 20f, bounds.y + 20f, bounds.width - 40f, 56f),
            mode.ToString().ToUpperInvariant() + " MODE", 26, selected ? accent : Muted,
            true, TextAnchor.MiddleCenter);
        if (GUI.Button(ScreenRect(bounds), GUIContent.none, GUIStyle.none)) UXPlayerProgress.SelectDifficulty(mode);
    }

    private static string RecordLabel(UXDifficulty mode)
    {
        UXLevelRecord level = UXPlayerProgress.Data.FindLevel(UXPlayerProgress.FacilityLevelId);
        if (level == null || !level.Completed(mode)) return mode.ToString().ToUpperInvariant() + "  /  NOT CLEARED";
        float best = level.BestTime(mode);
        return mode.ToString().ToUpperInvariant() + "  /  COMPLETE" +
            (best > 0f ? "  /  BEST " + UXPlayerProgress.FormatTime(best) : "");
    }

    private void DrawSettings()
    {
        Rect panel = new Rect(208f, 212f, 864f, 425f);
        Fill(panel, Panel); Border(panel, Cyan);
        Label(new Rect(240f, 236f, 800f, 42f), "PLAY YOUR WAY", 25, Color.white, true);
        if (Button(new Rect(240f, 306f, 800f, 62f),
            "DISPLAY: " + (UXPlayerProgress.Data.fullscreen ? "FULLSCREEN" : "WINDOWED")))
            UXPlayerProgress.SetFullscreen(!UXPlayerProgress.Data.fullscreen);
        Label(new Rect(240f, 376f, 800f, 23f), "Your preferred game-window display mode.", 13, Muted);
        if (Button(new Rect(240f, 418f, 800f, 62f),
            "CONTROL REMINDERS: " + (UXPlayerProgress.Data.showControlHints ? "ON" : "OFF")))
            UXPlayerProgress.SetControlHints(!UXPlayerProgress.Data.showControlHints);
        Label(new Rect(240f, 488f, 800f, 23f), "Interaction prompts and progress feedback stay visible.", 13, Muted);
        if (Button(new Rect(240f, 548f, 350f, 52f), "BACK TO MENU")) settingsVisible = false;
    }

    public void DrawBriefingNavigation()
    {
        Fit();
        Label(new Rect(48f, 461f, 700f, 21f),
            "DIFFICULTY: " + UXPlayerProgress.Data.difficulty.ToString().ToUpperInvariant(), 12, Cyan, true);
        if (Button(new Rect(1004f, 460f, 228f, 22f), "BACK TO MENU  [ESC]")) ShowHome();
    }

    private bool Button(Rect bounds, string text, bool primary = false)
    {
        bool hovered = ScreenRect(bounds).Contains(Event.current.mousePosition);
        Fill(bounds, primary ? (hovered ? new Color(0.55f, 1f, 1f) : Cyan) :
            (hovered ? new Color(0.08f, 0.22f, 0.28f) : new Color(0.04f, 0.11f, 0.15f)));
        Border(bounds, primary ? Cyan : new Color(0.20f, 0.48f, 0.56f));
        GUIStyle style = Style(Mathf.Max(10, Mathf.RoundToInt((bounds.height < 30f ? 10f : 17f) * scale)),
            primary ? Ink : Color.white, true, TextAnchor.MiddleCenter);
        return GUI.Button(ScreenRect(bounds), text, style);
    }

    private Rect ScreenRect(Rect bounds) => new Rect(offset.x + bounds.x * scale,
        offset.y + bounds.y * scale, bounds.width * scale, bounds.height * scale);

    private void Fill(Rect bounds, Color color)
    {
        Color previous = GUI.color; GUI.color = color;
        GUI.DrawTexture(ScreenRect(bounds), Texture2D.whiteTexture); GUI.color = previous;
    }

    private void Border(Rect bounds, Color color, float thickness = 1f)
    {
        Fill(new Rect(bounds.x, bounds.y, bounds.width, thickness), color);
        Fill(new Rect(bounds.x, bounds.yMax - thickness, bounds.width, thickness), color);
        Fill(new Rect(bounds.x, bounds.y, thickness, bounds.height), color);
        Fill(new Rect(bounds.xMax - thickness, bounds.y, thickness, bounds.height), color);
    }

    private GUIStyle Style(int font, Color color, bool bold, TextAnchor alignment)
    {
        int key = font * 32 + (int)alignment * 2 + (bold ? 1 : 0);
        if (!styles.TryGetValue(key, out GUIStyle style))
        {
            style = new GUIStyle(GUIStyle.none) { fontSize = font, wordWrap = true,
                alignment = alignment, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                padding = new RectOffset(0, 0, 0, 0) };
            styles.Add(key, style);
        }
        style.normal.textColor = color; style.hover.textColor = color;
        style.active.textColor = color; style.focused.textColor = color;
        return style;
    }

    private void Label(Rect bounds, string text, int font, Color color, bool bold = false,
        TextAnchor alignment = TextAnchor.UpperLeft)
    {
        GUI.Label(ScreenRect(bounds), text, Style(Mathf.Max(8, Mathf.RoundToInt(font * scale)), color, bold, alignment));
    }
}
