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
    private bool resetVisible;
    private string resetError;
    public System.Action ContinueRequested, RestartConfirmed;
    public string ErrorNotice;
    public bool BriefingVisible { get; private set; }

    public void ShowHome() { BriefingVisible = false; settingsVisible = false; resetVisible=false; }
    public void BriefingVisibleForNewGame(){BriefingVisible=true;}

    public void HandleKeyboard()
    {
        if(resetVisible)
        {
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.Return))resetVisible=false;
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) { ShowHome(); return; }
        if (settingsVisible) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.UpArrow)) UXPlayerProgress.SelectDifficulty(UXDifficulty.Easy);
        if (Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.DownArrow)) UXPlayerProgress.SelectDifficulty(UXDifficulty.Hard);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space)) ActivatePrimary();
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
        Label(new Rect(46f, 65f, 1188f, 65f), "PIXEL REBELLION", 46, Color.white, true);
        Label(new Rect(48f, 140f, 1100f, 28f),
            settingsVisible ? "SHADOW PROTOCOL  /  SETTINGS" : "SHADOW PROTOCOL", 16, Muted);

        if(resetVisible) DrawReset();
        else if (settingsVisible) DrawSettings();
        else DrawHome();

        Fill(new Rect(48f, 691f, 1184f, 1f), new Color(0.12f, 0.35f, 0.43f));
        if (ErrorNotice != null || UXPlayerProgress.Store.HasSaveError)
            Label(new Rect(48f, 706f, 1184f, 25f), ErrorNotice ?? UXPlayerProgress.Store.Status, 12, Gold, true);
        Label(new Rect(48f, 746f, 1184f, 23f), resetVisible?"ESC / ENTER: CANCEL":settingsVisible ? "ESC: BACK TO MENU" :
            "ARROWS: DIFFICULTY     ENTER: NEW GAME / CONTINUE", 12, Muted);
    }

    private void DrawHome()
    {
        UXCampaignSlot slot = UXCampaign.SelectedSlot;
        bool completed = slot != null && slot.completed;
        bool levelTwo = slot != null && slot.levelId == UXCampaign.CrimsonLevelId;
        Label(new Rect(48f, 210f, 1184f, 24f), "CAMPAIGN", 14, Cyan, true);
        DrawLevelCard(new Rect(48f, 246f, 572f, 224f), false, slot);
        DrawLevelCard(new Rect(660f, 246f, 572f, 224f), true, slot);

        Label(new Rect(48f, 494f, 572f, 24f), "CHOOSE YOUR DIFFICULTY", 18, Color.white, true);
        DrawDifficultyCard(new Rect(48f, 530f, 260f, 66f), UXDifficulty.Easy, Cyan);
        DrawDifficultyCard(new Rect(328f, 530f, 260f, 66f), UXDifficulty.Hard, Gold);
        if (Button(new Rect(48f, 614f, 260f, 42f), "SETTINGS")) settingsVisible = true;
        if (!Application.isEditor && Button(new Rect(328f, 614f, 260f, 42f), "EXIT GAME"))
            Application.Quit();

        string action = slot == null || completed ? "NEW GAME  /  LEVEL 1  >" :
            "CONTINUE LEVEL " + (levelTwo ? "2" : "1") + "  >";
        Label(new Rect(660f, 488f, 572f, 20f), completed ? "CAMPAIGN COMPLETE" :
            slot == null ? "A NEW GAME ALWAYS STARTS AT LEVEL 1" :
            "CONTINUE RESUMES THE HIGHLIGHTED LEVEL", 12, completed ? Green : Muted, true);
        if (Button(new Rect(660f, 522f, 572f, 62f), action, true)) ActivatePrimary();
        if (completed)
        {
            if (Button(new Rect(660f, 604f, 572f, 50f), "REPLAY LEVEL 2"))
                ContinueRequested?.Invoke();
        }
        else if (slot != null)
        {
            if (Button(new Rect(660f, 604f, 572f, 50f), "NEW GAME  /  START AT LEVEL 1"))
                RequestNewGame();
        }
    }

    private void DrawLevelCard(Rect bounds, bool crimson, UXCampaignSlot slot)
    {
        bool inLevelTwo = slot != null && slot.levelId == UXCampaign.CrimsonLevelId;
        bool complete = slot != null && slot.completed;
        bool locked = crimson && !inLevelTwo;
        bool current = !complete && (crimson ? inLevelTwo : !inLevelTwo);
        bool cleared = crimson ? complete : inLevelTwo;
        Color accent = current ? Cyan : cleared ? Green : Muted;
        Fill(bounds, current ? new Color(0.045f, 0.14f, 0.18f) : Panel);
        Border(bounds, accent, current ? 3f : 1f);
        if (current) Fill(new Rect(bounds.x, bounds.y, 5f, bounds.height), Cyan);
        Label(new Rect(bounds.x + 24f, bounds.y + 17f, bounds.width - 48f, 24f),
            crimson ? "LEVEL 2" : "LEVEL 1", 14, accent, true);
        Label(new Rect(bounds.x + 24f, bounds.y + 52f, bounds.width - 48f, 40f),
            crimson ? "CRIMSON DEPTHS" : "FACILITY BREACH", 27, locked ? Muted : Color.white, true);
        string state = locked ? "LOCKED  /  CLEAR LEVEL 1" : current ?
            (slot == null ? "NEW GAME STARTS HERE" : "CONTINUE HERE") :
            complete && crimson ? "CAMPAIGN COMPLETE" : "CLEARED";
        Label(new Rect(bounds.x + 24f, bounds.y + 104f, bounds.width - 48f, 28f),
            state, 17, accent, true);
        if (current && slot != null)
            Label(new Rect(bounds.x + 24f, bounds.y + 158f, bounds.width - 48f, 24f),
                crimson ? "SAVED CHECKPOINT " + slot.checkpoint + " / 3" : "RESUMES AT LEVEL START",
                15, Muted);
    }

    private void DrawDifficultyCard(Rect bounds, UXDifficulty mode, Color accent)
    {
        bool selected = UXPlayerProgress.Data.difficulty == mode;
        bool hovered = ScreenRect(bounds).Contains(Event.current.mousePosition);
        bool pressed = hovered && Input.GetMouseButton(0);
        Fill(bounds, pressed ? new Color(0.10f, 0.27f, 0.32f) : selected ?
            new Color(0.045f, 0.14f, 0.18f) : hovered ? new Color(0.04f, 0.12f, 0.16f) : Panel);
        Border(bounds, selected || hovered ? accent : new Color(0.12f, 0.30f, 0.37f), selected || pressed ? 2f : 1f);
        if (selected) Fill(new Rect(bounds.x, bounds.y, 5f, bounds.height), accent);
        Label(new Rect(bounds.x + 20f, bounds.y, bounds.width - 40f, bounds.height),
            mode.ToString().ToUpperInvariant() + " MODE", 26, selected ? accent : Muted,
            true, TextAnchor.MiddleCenter);
        if (GUI.Button(ScreenRect(bounds), GUIContent.none, GUIStyle.none)) UXPlayerProgress.SelectDifficulty(mode);
    }

    private static string RecordLabel(UXDifficulty mode,string levelId,string prefix)
    {
        UXLevelRecord level = UXPlayerProgress.Data.FindLevel(levelId);
        if (level == null || !level.Completed(mode)) return prefix + "  /  NOT CLEARED";
        float best = level.BestTime(mode);
        return prefix + "  /  COMPLETE" +
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

    private void ActivatePrimary()
    {
        UXCampaignSlot slot = UXCampaign.SelectedSlot;
        if (slot == null || slot.completed) RequestNewGame();
        else ContinueRequested?.Invoke();
    }

    private void RequestNewGame()
    {
        resetError = null;
        if (UXPlayerProgress.Data.campaigns.Count == 0 && UXPlayerProgress.Data.levels.Count == 0)
            StartNewGame();
        else resetVisible = true;
    }

    private void StartNewGame()
    {
        if (!UXCampaign.RestartGame())
        {
            resetVisible = true;
            resetError = "Progress could not be reset. Your existing records have been kept.";
            return;
        }
        resetVisible = false;
        ErrorNotice = null;
        if (RestartConfirmed != null) RestartConfirmed();
        else BriefingVisible = true;
    }

    private void DrawReset()
    {
        Rect panel=new Rect(208f,212f,864f,425f);Fill(panel,Panel);Border(panel,Gold);
        Label(new Rect(240f,242f,800f,42f),"START A NEW GAME?",28,Color.white,true);
        Label(new Rect(240f,316f,800f,112f),
            "All level progress, checkpoints and saved mission records in Easy and Hard will be erased.\nYou will start again at Level 1.",21,Color.white);
        Label(new Rect(240f,451f,800f,48f),resetError??"Your display and control settings will be kept.",15,
            resetError!=null?Gold:Muted);
        if(Button(new Rect(240f,548f,350f,52f),"CANCEL",true))resetVisible=false;
        if(Button(new Rect(690f,548f,350f,52f),"START NEW GAME")) StartNewGame();
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
        bool pressed = hovered && Input.GetMouseButton(0);
        Fill(bounds, primary ? (pressed ? new Color(0.11f, 0.61f, 0.69f) : hovered ? new Color(0.55f, 1f, 1f) : Cyan) :
            (pressed ? new Color(0.12f, 0.31f, 0.36f) : hovered ? new Color(0.08f, 0.22f, 0.28f) : new Color(0.04f, 0.11f, 0.15f)));
        Border(bounds, primary || hovered ? Cyan : new Color(0.20f, 0.48f, 0.56f), hovered ? 2f : 1f);
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
