using UnityEngine;
using UnityEngine.SceneManagement;

public class UXHUDController : MonoBehaviour
{
    private static UXHUDController instance;
    private static bool startGameplayAfterReload = false;

    private Transform player;
    private PlayerHiding playerHiding;
    private PlayerController playerController;
    private UXMissionBriefingScreen missionBriefing;

    private GameObject terminal;
    private TerminalInteraction terminalInteraction;
    private GameObject extractionZone;
    private EvidenceCollectible[] evidenceItems;
    private GameObject[] hidingSpots;

    private Texture2D briefingTexture;

    private int totalEvidence = 1;
    private int lastEvidenceCount = 0;
    private bool briefingVisible = true;
    private bool helpVisible = false;
    private bool restartConfirmationVisible = false;
    private bool mainMenuConfirmationVisible = false;
    private bool restartOpenedFromPause = false;
    private bool lastTerminalActivatedForToast;
    private float lastSeenTerminalMessageTime = -1f;

    private bool TerminalAccessed =>
        GameManager.Instance != null && GameManager.Instance.TerminalActivated;

    private string toastMessage = "";
    private float toastUntil = 0f;
    private Texture2D toastBorder;

    private Texture2D blackTex;
    private Texture2D darkTex;
    private Texture2D cyanTex;
    private Texture2D greenTex;
    private Texture2D yellowTex;
    private Texture2D redTex;
    private Texture2D hoverTex;
    private Texture2D pressTex;

    // Exact START MISSION button bounds inside the 2000x1125 briefing artwork.
    private const float BriefingSourceWidth = 2000f;
    private const float BriefingSourceHeight = 1125f;
    private static readonly Rect StartButtonSourceRect =
        new Rect(1481f, 815f, 399f, 104f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateHUD()
    {
        UXHUDController existing = FindFirstObjectByType<UXHUDController>();

        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject hud = new GameObject("Victor_UX_HUD");
        instance = hud.AddComponent<UXHUDController>();
        DontDestroyOnLoad(hud);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // DontDestroyOnLoad only has meaning (and is only legal to call)
        // in Play Mode/builds; guarding it keeps this component testable
        // from EditMode tests without changing any runtime behavior.
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        briefingTexture =
            Resources.Load<Texture2D>("UX/BriefingWireframe");

        CreateTextures();
        InitializeSceneState();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (blackTex == null)
            CreateTextures();

        if (briefingTexture == null)
        {
            briefingTexture =
                Resources.Load<Texture2D>("UX/BriefingWireframe");
        }

        InitializeSceneState();
    }

    private void InitializeSceneState()
    {
        player = null;
        playerHiding = null;
        playerController = null;
        terminal = null;
        terminalInteraction = null;
        extractionZone = null;
        evidenceItems = null;
        hidingSpots = null;

        helpVisible = false;
        restartConfirmationVisible = false;
        mainMenuConfirmationVisible = false;
        restartOpenedFromPause = false;
        lastTerminalActivatedForToast = false;
        lastSeenTerminalMessageTime = -1f;
        toastMessage = "";
        toastUntil = 0f;
        toastBorder = cyanTex;

        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHiding =
                playerObject.GetComponent<PlayerHiding>();
            playerController = playerObject.GetComponent<PlayerController>();
        }

        terminal = GameObject.Find("Terminal");

        if (terminal != null)
        {
            // The UX layer owns on-screen prompts and toasts, but
            // TerminalInteraction stays enabled - it is the sole authority
            // for actually hacking the terminal and disabling cameras. We
            // only read its public state here to drive our own messaging.
            terminalInteraction =
                terminal.GetComponent<TerminalInteraction>();
        }

        extractionZone = GameObject.Find("ExtractionZone");

        evidenceItems =
            FindObjectsByType<EvidenceCollectible>(
                FindObjectsSortMode.None
            );

        hidingSpots = GameObject.FindGameObjectsWithTag("HidingSpot");

        if (evidenceItems != null &&
            evidenceItems.Length > 0)
        {
            totalEvidence = evidenceItems.Length;
        }
        else
        {
            totalEvidence = 1;
        }

        lastEvidenceCount =
            EvidenceManager.Instance != null
            ? EvidenceManager.Instance.EvidenceCollected
            : 0;

        missionBriefing = SceneManager.GetActiveScene().name == "UXFacilityPreview" || ExpandedFacility
            ? new UXMissionBriefingScreen(totalEvidence, ExpandedFacility) : null;

        //Creates a fresh record when player reloads the mission
        bool autoStart = startGameplayAfterReload;

        briefingVisible = !autoStart;
        startGameplayAfterReload = false;

        if (autoStart)
            GameplayMetrics.Instance?.StartSession();


        ConfigureGameplayViewport();
        Time.timeScale = briefingVisible ? 0f : 1f;
    }

    private void Update()
    {
        if (Screen.width != hudViewportWidth || Screen.height != hudViewportHeight)
            ConfigureGameplayViewport();
        if (briefingVisible)
        {
            // Keep keyboard access while the visible artwork button is clickable.
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                StartMission();
            }

            return;
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
        {
            Time.timeScale = 0f;

            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                RetryMission();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToMainMenu();
            }

            return;
        }

        // Confirmation takes priority over all gameplay and pause inputs.
        if (restartConfirmationVisible)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                RetryMission();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelRestart();
            }

            return;
        }

        // Returning to the mission briefing also discards progress.
        if (mainMenuConfirmationVisible)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ReturnToMainMenu();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelMainMenu();
            }

            return;
        }

        if (helpVisible)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ResumeGame();

            return;
        }

        // R asks for confirmation; it never resets progress on its own.
        if (Input.GetKeyDown(KeyCode.R))
        {
            RequestRestart();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
            return;
        }

        UpdateEvidenceFeedback();
        UpdateTerminalFeedback();
    }

    private void UpdateEvidenceFeedback()
    {
        if (EvidenceManager.Instance == null)
            return;

        int evidence =
            EvidenceManager.Instance.EvidenceCollected;

        if (evidence > lastEvidenceCount)
        {
            if (evidence >= totalEvidence)
            {
                ShowToast(
                    "EVIDENCE COMPLETE - FIND TERMINAL",
                    greenTex,
                    2.4f
                );
            }
            else
            {
                ShowToast(
                    "EVIDENCE ACQUIRED  " +
                    evidence + " / " + totalEvidence,
                    cyanTex,
                    1.6f
                );
            }
        }

        lastEvidenceCount = evidence;
    }

    private void UpdateTerminalFeedback()
    {
        bool activated = TerminalAccessed;

        // The state-transition toast below already covers the success case
        // with the latest wording (which also tells the player extraction is
        // now reachable), so this is the single toast fired for that event -
        // no separate "TERMINAL ACCESS GRANTED" toast follows it.
        if (activated && !lastTerminalActivatedForToast)
        {
            ShowToast("EXIT UNLOCKED - REACH EXTRACTION", greenTex, 2.6f);
        }

        lastTerminalActivatedForToast = activated;

        if (terminalInteraction != null &&
            terminalInteraction.LastMessageTime > lastSeenTerminalMessageTime)
        {
            lastSeenTerminalMessageTime = terminalInteraction.LastMessageTime;

            // The success case is already covered by the state-transition
            // toast above; only relay the other outcomes (locked /
            // already-active) here to avoid a duplicate toast for the same
            // event.
            if (terminalInteraction.LastMessageTone != TerminalMessageTone.Success)
            {
                Texture2D border = terminalInteraction.LastMessageTone == TerminalMessageTone.Warning
                    ? redTex
                    : greenTex;

                ShowToast(terminalInteraction.LastMessage, border, 1.7f);
            }
        }
    }

    private void OnGUI()
    {
        if (briefingVisible)
        {
            DrawBriefing();
            return;
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
        {
            DrawMissionResult();
            return;
        }

        DrawHUD();

        // Labels are visual guidance only: no gameplay state is changed.
        if (!helpVisible && !restartConfirmationVisible &&
            !mainMenuConfirmationVisible)
            DrawObjectiveMarkers();
        if (ExpandedFacility && Input.GetKey(KeyCode.Tab) && !helpVisible &&
            !restartConfirmationVisible && !mainMenuConfirmationVisible && missionBriefing != null)
            missionBriefing.DrawMapOverlay();
        if (restartConfirmationVisible)
        {
            DrawRestartConfirmation();
            return;
        }

        if (mainMenuConfirmationVisible)
        {
            DrawMainMenuConfirmation();
            return;
        }

        if (helpVisible)
        {
            DrawHelp();
            return;
        }

        DrawToast();
    }

    private void DrawBriefing()
    {
        if (missionBriefing != null)
        {
            if (missionBriefing.Draw()) StartMission();
            return;
        }

        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            blackTex
        );

        if (briefingTexture == null)
        {
            Rect fallback =
                new Rect(
                    Screen.width / 2f - 300,
                    Screen.height / 2f - 150,
                    600,
                    300
                );

            DrawPanel(fallback, darkTex, cyanTex);

            GUI.Label(
                new Rect(
                    fallback.x + 40,
                    fallback.y + 45,
                    fallback.width - 80,
                    70
                ),
                "MISSION BRIEFING",
                TitleStyle(26)
            );

            Rect fallbackButton =
                new Rect(
                    fallback.x + 150,
                    fallback.y + 170,
                    300,
                    60
                );

            if (GUI.Button(
                fallbackButton,
                "START MISSION",
                ActionButtonStyle(18)
            ))
            {
                StartMission();
            }

            return;
        }

        Rect briefingRect =
            GetScaleToFitRect(
                briefingTexture.width,
                briefingTexture.height
            );

        GUI.DrawTexture(
            briefingRect,
            briefingTexture,
            ScaleMode.StretchToFill,
            true
        );

        Rect startRect =
            MapSourceRectToScreen(
                StartButtonSourceRect,
                briefingRect
            );

        Vector2 mouse = Event.current.mousePosition;
        bool hovered = startRect.Contains(mouse);
        bool pressed = hovered && Input.GetMouseButton(0);

        if (hovered)
        {
            float pulse =
                0.5f +
                0.5f * Mathf.Sin(Time.unscaledTime * 5f);

            int thickness =
                Mathf.RoundToInt(3f + pulse * 2f);

            DrawRectBorder(
                ExpandRect(startRect, 4f + pulse * 2f),
                cyanTex,
                thickness
            );

            GUI.DrawTexture(
                startRect,
                pressed ? pressTex : hoverTex
            );
        }

        // Invisible hit target placed exactly over the yellow START MISSION
        // button that already exists in the briefing artwork.
        if (GUI.Button(
            startRect,
            GUIContent.none,
            InvisibleButtonStyle()
        ))
        {
            StartMission();
        }
    }

    private Rect GetScaleToFitRect(
        float sourceWidth,
        float sourceHeight)
    {
        float scale =
            Mathf.Min(
                Screen.width / sourceWidth,
                Screen.height / sourceHeight
            );

        float width = sourceWidth * scale;
        float height = sourceHeight * scale;

        return new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - height) / 2f,
            width,
            height
        );
    }

    private Rect MapSourceRectToScreen(
        Rect sourceRect,
        Rect destinationRect)
    {
        float scaleX =
            destinationRect.width / BriefingSourceWidth;

        float scaleY =
            destinationRect.height / BriefingSourceHeight;

        return new Rect(
            destinationRect.x + sourceRect.x * scaleX,
            destinationRect.y + sourceRect.y * scaleY,
            sourceRect.width * scaleX,
            sourceRect.height * scaleY
        );
    }

    private Rect ExpandRect(Rect rect, float amount)
    {
        return new Rect(
            rect.x - amount,
            rect.y - amount,
            rect.width + amount * 2f,
            rect.height + amount * 2f
        );
    }

    private void DrawHUD()
    {
        int evidence = EvidenceManager.Instance != null
            ? EvidenceManager.Instance.EvidenceCollected : 0;
        bool hidden = playerHiding != null && playerHiding.IsHidden;
        bool sneaking = playerController != null && playerController.IsSneaking;
        bool sprinting = playerController != null && playerController.IsSprinting;
        string objective = evidence < totalEvidence
            ? "COLLECT THE EVIDENCE"
            : (!TerminalAccessed ? "ACCESS SECURITY TERMINAL [E]" : "REACH EXTRACTION");
        UXStealthDirector stealth = ExpandedFacility ? UXStealthDirector.Instance : null;
        if (stealth != null) objective = stealth.Objective;
        float s = GetHudScale(), h = GetTopBarHeight() - (ExpandedFacility ? 32f * s : 0f), p = 12f * s;
        float usable = Screen.width - 2f * p;
        Rect title, count, state, goal;

        if (Screen.width < 800)
        {
            float row = h / 3f;
            title = new Rect(p, 0f, usable, row);
            count = new Rect(p, row, usable / 2f, row);
            state = new Rect(p + usable / 2f, row, usable / 2f, row);
            goal = new Rect(p, 2f * row, usable, row);
        }
        else if (Screen.width < 1550)
        {
            float row = h * 0.55f, third = usable / 3f;
            title = new Rect(p, 0f, third, row);
            count = new Rect(p + third, 0f, third, row);
            state = new Rect(p + 2f * third, 0f, third, row);
            goal = new Rect(p, row, usable, h - row);
        }
        else
        {
            title = new Rect(p, 0f, usable * 0.19f, h);
            count = new Rect(p + usable * 0.19f, 0f, usable * 0.16f, h);
            state = new Rect(p + usable * 0.35f, 0f, usable * 0.17f, h);
            goal = new Rect(p + usable * 0.52f, 0f, usable * 0.48f, h);
        }

        DrawPanel(new Rect(0f, 0f, Screen.width, GetTopBarHeight()), darkTex, cyanTex);
        GUI.Label(title, "PIXEL REBELLION",
            TitleStyle(Mathf.RoundToInt(18f * s)));
        GUI.Label(count, "EVIDENCE  " + evidence + " / " + totalEvidence,
            CenterStyle(Mathf.RoundToInt(16f * s), Color.white));
        GUI.Label(state, hidden ? "STATUS  HIDDEN" :
            (sneaking ? "STATUS  SNEAKING" : (sprinting ? "STATUS  SPRINTING" : "STATUS  VISIBLE")),
            CenterStyle(Mathf.RoundToInt(16f * s), hidden
                ? new Color(0.35f, 1f, 0.48f)
                : (sneaking ? new Color(0.35f, 0.82f, 1f)
                    : new Color(1f, 0.85f, 0.25f))));
        GUI.Label(goal, "OBJECTIVE:  " + objective,
            CenterStyle(Mathf.RoundToInt(16f * s),
                new Color(0.45f, 1f, 0.55f)));
        if (stealth != null) DrawSecurityStatus(stealth, h, s);
        DrawContextPrompt(evidence);
    }

    private void DrawSecurityStatus(UXStealthDirector stealth, float y, float s)
    {
        Color alert = stealth.State.Phase == UXAlertPhase.Engaged ? new Color(1f, 0.35f, 0.3f) :
            stealth.State.Phase == UXAlertPhase.Clear ? new Color(0.35f, 0.95f, 0.7f) : new Color(1f, 0.8f, 0.25f);
        float third = Screen.width / 3f;
        GUI.Label(new Rect(12f * s, y, third - 20f * s, 27f * s),
            "HEALTH " + stealth.State.Health + " / 100",
            CenterStyle(Mathf.RoundToInt(14f * s), stealth.State.Health <= 25 ? Color.red : Color.white));
        GUI.Label(new Rect(third, y, third, 27f * s), stealth.PhaseLabel,
            CenterStyle(Mathf.RoundToInt(14f * s), alert));
        GUI.Label(new Rect(third * 2f, y, third, 27f * s),
            stealth.KeyAcquired ? "ACCESS GATE: OPEN   |   TAB MAP" : "ACCESS KEY: REQUIRED   |   TAB MAP",
            CenterStyle(Mathf.RoundToInt(13f * s), stealth.KeyAcquired ? Color.cyan : Color.yellow));
        Color previous = GUI.color;
        GUI.color = alert;
        GUI.DrawTexture(new Rect(third, y + 27f * s,
            third * stealth.State.Alert / 100f, 3f * s), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void DrawContextPrompt(int evidence)
    {
        string message = "";
        Texture2D border = cyanTex;

        if (player != null)
        {
            float closestEvidenceDistance =
                float.MaxValue;

            if (evidenceItems != null)
            {
                foreach (EvidenceCollectible item
                         in evidenceItems)
                {
                    if (item == null)
                        continue;

                    float distance =
                        Vector2.Distance(
                            player.position,
                            item.transform.position
                        );

                    if (distance < closestEvidenceDistance)
                        closestEvidenceDistance = distance;
                }
            }

            if (closestEvidenceDistance <= 2f)
            {
                message =
                    "EVIDENCE AHEAD - WALK OVER IT TO COLLECT";
                border = cyanTex;
            }

            if (terminal != null &&
                Vector2.Distance(
                    player.position,
                    terminal.transform.position
                ) <= 2f)
            {
                if (evidence < totalEvidence)
                {
                    message =
                        "TERMINAL LOCKED  -  COLLECT EVIDENCE";
                    border = redTex;
                }
                else if (TerminalAccessed)
                {
                    message = "TERMINAL ACCESSED";
                    border = greenTex;
                }
                else if (terminalInteraction != null && terminalInteraction.PlayerNearby)
                {
                    message = "PRESS E TO ACCESS SECURITY TERMINAL";
                    border = greenTex;
                }
                else
                {
                    message = "SECURITY TERMINAL - MOVE CLOSER";
                    border = yellowTex;
                }
            }

            if (extractionZone != null &&
                Vector2.Distance(
                    player.position,
                    extractionZone.transform.position
                ) <= 2.2f)
            {
                if (evidence < totalEvidence)
                {
                    message =
                        "EXTRACTION LOCKED  -  COLLECT EVIDENCE";
                    border = redTex;
                }
                else if (!TerminalAccessed)
                {
                    message =
                        "EXTRACTION LOCKED  -  ACCESS TERMINAL";
                    border = redTex;
                }
                else
                {
                    message = "EXTRACTION READY - ENTER TO FINISH";
                    border = greenTex;
                }
            }
        }

        if (ExpandedFacility && UXStealthDirector.Instance != null)
            message = UXStealthDirector.Instance.Prompt;

        if (string.IsNullOrEmpty(message))
        {
            message =
                Screen.width < 1000
                    ? "CROUCH MOVE WASD   SLOW C/CTRL   SPRINT SHIFT   USE E   HELP ESC"
                    : "WASD / ARROWS CROUCH MOVE    C / CTRL EXTRA SLOW    SHIFT SPRINT    E TERMINAL    R RESTART    ESC HELP";
            border = cyanTex;
        }

        float s = GetHudScale();
        float bh = GetBottomBarHeight();
        Rect bar = new Rect(0f, Screen.height - bh, Screen.width, bh);
        DrawPanel(bar, darkTex, border);
        GUI.Label(new Rect(12f * s, bar.y + 3f * s,
            Screen.width - 24f * s, bh - 6f * s), message,
            CenterStyle(Mathf.RoundToInt(15f * s), Color.white));
        if (ExpandedFacility && UXStealthDirector.Instance != null && UXStealthDirector.Instance.InteractionProgress > 0f)
            GUI.DrawTexture(new Rect(0f, bar.y, Screen.width *
                Mathf.Clamp01(UXStealthDirector.Instance.InteractionProgress), 4f * s), cyanTex);
    }

    private void DrawObjectiveMarkers()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        int collected = EvidenceManager.Instance != null
            ? EvidenceManager.Instance.EvidenceCollected : 0;

        // Only show the current required objective to avoid label clutter.
        if (ExpandedFacility && UXStealthDirector.Instance != null && !UXStealthDirector.Instance.KeyAcquired)
        {
            DrawObjectMarker(camera, UXStealthDirector.Instance.KeyPosition, "KEY", yellowTex);
            DrawObjectMarker(camera, UXStealthDirector.Instance.GatePosition, "LOCKED", redTex);
        }
        else if (collected < totalEvidence)
        {
            if (evidenceItems != null)
            {
                foreach (EvidenceCollectible evidence in evidenceItems)
                {
                    if (evidence != null)
                        DrawObjectMarker(camera, evidence.transform.position,
                            "EVIDENCE", cyanTex);
                }
            }
        }
        else if (!TerminalAccessed && terminal != null)
        {
            DrawObjectMarker(camera, terminal.transform.position,
                "TERMINAL [E]", yellowTex);
        }
        else if (TerminalAccessed && extractionZone != null)
        {
            DrawObjectMarker(camera, extractionZone.transform.position,
                "EXTRACTION", greenTex);
        }

        if (ExpandedFacility && UXStealthDirector.Instance != null &&
            UXStealthDirector.Instance.KeyAcquired && player != null &&
            Vector2.Distance(player.position, UXStealthDirector.Instance.GatePosition) < 6f)
            DrawObjectMarker(camera, UXStealthDirector.Instance.GatePosition, "OPEN", greenTex);

        // Nearby hiding zones are optional cues, not a new mechanic.
        if (player == null || hidingSpots == null ||
            (playerHiding != null && playerHiding.IsHidden))
            return;

        foreach (GameObject spot in hidingSpots)
        {
            if (spot != null &&
                Vector2.Distance(player.position, spot.transform.position) <= 1.8f)
            {
                DrawObjectMarker(camera, spot.transform.position,
                    "HIDE", cyanTex);
            }
        }
    }

    private void DrawObjectMarker(Camera camera, Vector3 worldPosition,
        string label, Texture2D border)
    {
        Vector3 screen = camera.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f ||
            !camera.pixelRect.Contains(new Vector2(screen.x, screen.y)))
            return;

        string compactLabel = label == "EVIDENCE" ? "DATA" :
            label == "TERMINAL [E]" ? (terminalInteraction != null && terminalInteraction.PlayerNearby ? "USE E" : "TERM") :
            label == "EXTRACTION" ? "EXIT" :
            (label == "KEY" || label == "LOCKED" || label == "OPEN" ? label : "HIDE");
        float s = Mathf.Min(GetHudScale(), 1.25f);
        float width = 66f * s;
        float height = 22f * s;
        float minY = GetTopBarHeight() + 5f;
        float maxY = Screen.height - GetBottomBarHeight() - height - 5f;
        if (maxY < minY || Screen.width <= width + 16f)
            return;

        float x = Mathf.Clamp(screen.x - width / 2f,
            8f, Screen.width - width - 8f);
        float y = Mathf.Clamp(Screen.height - screen.y - 35f * s,
            minY, maxY);
        Rect marker = new Rect(x, y, width, height);

        DrawPanel(marker, darkTex, border);
        GUI.Label(new Rect(x + 5f * s, y + 2f * s,
            width - 10f * s, height - 4f * s), compactLabel,
            CenterStyle(Mathf.RoundToInt(12f * s), Color.white));
    }
    private void DrawToast()
    {
        if (string.IsNullOrEmpty(toastMessage) ||
            Time.unscaledTime >= toastUntil)
            return;
        float s = GetHudScale();
        float alpha = Mathf.Clamp01(
            (toastUntil - Time.unscaledTime) / 0.35f);
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        float margin = 14f * s;
        float width = Mathf.Min(360f * s,
            Screen.width - 2f * margin);
        Rect toast = new Rect(Screen.width - width - margin,
            GetTopBarHeight() + 8f * s, width, 48f * s);
        DrawPanel(toast, darkTex,
            toastBorder != null ? toastBorder : cyanTex);
        GUI.Label(new Rect(toast.x + 10f * s, toast.y + 4f * s,
            toast.width - 20f * s, toast.height - 8f * s),
            toastMessage,
            CenterStyle(Mathf.RoundToInt(15f * s), Color.white));
        GUI.color = oldColor;
    }

    private void ShowToast(
        string message,
        Texture2D border,
        float duration)
    {
        toastMessage = message;
        toastBorder = border;
        toastUntil = Time.unscaledTime + duration;
    }

    private float GetHudScale()
    {
        return Mathf.Clamp(Mathf.Min(Screen.width / 1280f,
            Screen.height / 720f), 0.85f, 1.60f);
    }

    private float GetTopBarHeight()
    {
        return (Screen.width < 800 ? 102f :
            (Screen.width < 1550 ? 74f : 54f)) * GetHudScale() +
            (ExpandedFacility ? 32f * GetHudScale() : 0f);
    }

    private bool ExpandedFacility => SceneManager.GetActiveScene().name == "UXFacilityExpanded";

    private float GetBottomBarHeight()
    {
        return 42f * GetHudScale();
    }

    private int hudViewportWidth = -1;
    private int hudViewportHeight = -1;

    private void ConfigureGameplayViewport()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        float screenHeight = Mathf.Max(1f, Screen.height);
        float topSpace = GetTopBarHeight() / screenHeight;
        float bottomSpace = GetBottomBarHeight() / screenHeight;
        camera.rect = new Rect(0f, bottomSpace, 1f,
            Mathf.Max(0.1f, 1f - topSpace - bottomSpace));
        hudViewportWidth = Screen.width;
        hudViewportHeight = Screen.height;
    }

    private void DrawHelp()
    {
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            blackTex
        );

        float s = Mathf.Min(
            GetHudScale(),
            Mathf.Min(
                (Screen.width - 20f) / 760f,
                (Screen.height - 20f) / 500f
            )
        );

        float w = 760f * s;
        float h = 500f * s;

        Rect panel = new Rect(
            (Screen.width - w) / 2f,
            (Screen.height - h) / 2f,
            w,
            h
        );

        DrawPanel(panel, darkTex, cyanTex);

        GUI.Label(
            new Rect(panel.x + 30f*s, panel.y + 20f*s,
                w - 60f*s, 45f*s),
            "PAUSE / HELP",
            TitleStyle(Mathf.RoundToInt(27f*s))
        );

        GUI.Label(
            new Rect(panel.x + 35f*s, panel.y + 68f*s,
                w - 70f*s, 32f*s),
            "MISSION GUIDE",
            CenterStyle(Mathf.RoundToInt(16f*s),
                new Color(0.45f, 1f, 0.55f))
        );

        string instructions =
            "1   Walk over evidence to collect it automatically.\n\n" +
            "2   Press E at the terminal after collecting evidence.\n\n" +
            "3   Enter the extraction zone once it unlocks.\n\n" +
            "STEALTH   Move crouched by default. Marked hiding spots conceal you.\n\n" +
            "WASD / ARROWS   Move       C / CTRL   Extra slow\n" +
            "SHIFT   Sprint       R   Restart (confirm)       ESC   Resume";

        if (ExpandedFacility) instructions =
            "1   Gold keycard opens the marked red security gate.\n\n" +
            "2   Hold E at 3 data caches (2.5s), then the terminal (4s).\n\n" +
            "3   Extract through the service route or the east flank.\n\n" +
            "RED: pursuit. Break sight and hide. YELLOW: searching.\n" +
            "Health does not regenerate; marked blue cover conceals you.\n\n" +
            "WASD move crouched   C / CTRL slow   SHIFT sprint\n" +
            "TAB map   R restart (confirm)   ESC resume";

        GUI.Label(
            new Rect(panel.x + 70f*s, panel.y + 115f*s,
                w - 140f*s, 235f*s),
            instructions,
            TextStyle(Mathf.RoundToInt(17f*s), Color.white)
        );

        float buttonWidth = 185f*s;
        float gap = 18f*s;
        float total = 3f*buttonWidth + 2f*gap;
        float x = panel.x + (w - total)/2f;
        float y = panel.y + 365f*s;

        if (GUI.Button(
            new Rect(x, y, buttonWidth, 55f*s),
            "RESUME",
            ActionButtonStyle(Mathf.RoundToInt(17f*s))
        ))
            ResumeGame();

        if (GUI.Button(
            new Rect(x + buttonWidth + gap, y,
                buttonWidth, 55f*s),
            "RESTART",
            ActionButtonStyle(Mathf.RoundToInt(17f*s))
        ))
            RequestRestart();

        if (GUI.Button(
            new Rect(x + 2f*(buttonWidth + gap), y,
                buttonWidth, 55f*s),
            "MAIN MENU",
            ActionButtonStyle(Mathf.RoundToInt(17f*s))
        ))
            RequestMainMenu();

        GUI.Label(
            new Rect(panel.x + 35f*s, panel.y + 440f*s,
                w - 70f*s, 28f*s),
            "ESC  RESUME",
            CenterStyle(Mathf.RoundToInt(14f*s),
                new Color(0.55f, 0.85f, 1f))
        );
    }
    private void DrawRestartConfirmation()
    {
        // Keep the gameplay visible but inactive behind the confirmation.
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            blackTex
        );
        GUI.color = oldColor;

        float width = Mathf.Min(560f, Screen.width - 30f);
        Rect panel = new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - 268f) / 2f,
            width,
            268f
        );

        DrawPanel(panel, darkTex, yellowTex);

        GUI.Label(
            new Rect(panel.x + 15f, panel.y + 28f, panel.width - 30f, 48f),
            "RESTART MISSION?",
            TitleStyle(26)
        );

        GUI.Label(
            new Rect(panel.x + 25f, panel.y + 88f, panel.width - 50f, 45f),
            "Current mission progress will be lost.",
            CenterStyle(16, Color.white)
        );

        float buttonWidth = Mathf.Min(190f, (panel.width - 65f) / 2f);
        float buttonGap = 20f;
        float buttonsX = panel.x + (panel.width - 2f * buttonWidth - buttonGap) / 2f;
        float buttonsY = panel.y + 155f;

        if (GUI.Button(
            new Rect(buttonsX, buttonsY, buttonWidth, 52f),
            "CANCEL",
            ActionButtonStyle(17)
        ))
        {
            CancelRestart();
        }

        if (GUI.Button(
            new Rect(buttonsX + buttonWidth + buttonGap, buttonsY, buttonWidth, 52f),
            "RESTART",
            ActionButtonStyle(17)
        ))
        {
            RetryMission();
        }

        GUI.Label(
            new Rect(panel.x + 20f, panel.y + 221f, panel.width - 40f, 28f),
            "ESC  CANCEL        ENTER  RESTART",
            CenterStyle(13, new Color(0.65f, 0.88f, 1f))
        );
    }

    private void DrawMainMenuConfirmation()
    {
        // Return to the briefing resets the current mission, so ask first.
        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.85f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            blackTex
        );
        GUI.color = oldColor;

        float width = Mathf.Min(560f, Screen.width - 30f);
        Rect panel = new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - 268f) / 2f,
            width,
            268f
        );

        DrawPanel(panel, darkTex, yellowTex);

        GUI.Label(
            new Rect(panel.x + 15f, panel.y + 28f, panel.width - 30f, 48f),
            "RETURN TO MAIN MENU?",
            TitleStyle(26)
        );

        GUI.Label(
            new Rect(panel.x + 25f, panel.y + 88f, panel.width - 50f, 45f),
            "Current mission progress will be lost.",
            CenterStyle(16, Color.white)
        );

        float buttonWidth = Mathf.Min(190f, (panel.width - 65f) / 2f);
        float buttonGap = 20f;
        float buttonsX = panel.x + (panel.width - 2f * buttonWidth - buttonGap) / 2f;
        float buttonsY = panel.y + 155f;

        if (GUI.Button(
            new Rect(buttonsX, buttonsY, buttonWidth, 52f),
            "CANCEL",
            ActionButtonStyle(17)
        ))
        {
            CancelMainMenu();
        }

        if (GUI.Button(
            new Rect(buttonsX + buttonWidth + buttonGap, buttonsY, buttonWidth, 52f),
            "MAIN MENU",
            ActionButtonStyle(17)
        ))
        {
            ReturnToMainMenu();
        }

        GUI.Label(
            new Rect(panel.x + 20f, panel.y + 221f, panel.width - 40f, 28f),
            "ESC  CANCEL        ENTER  MAIN MENU",
            CenterStyle(13, new Color(0.65f, 0.88f, 1f))
        );
    }

    private void DrawMissionResult()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.GameEnded)
        {
            return;
        }

        bool won =
            GameManager.Instance.PlayerWon;

        //Records Player ID
        string playerID = GameplayMetrics.Instance != null
            ? GameplayMetrics.Instance.PlayerID
            : "Unavailable";

        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            blackTex
        );

        Rect result =
            new Rect(
                Screen.width / 2f - 310,
                Screen.height / 2f - 165,
                620,
                330
            );

        DrawPanel(
            result,
            darkTex,
            won ? greenTex : redTex
        );

        GUI.Label(
            new Rect(
                result.x + 25,
                result.y + 34,
                result.width - 50,
                62
            ),
            won
                ? "MISSION COMPLETE"
                : "MISSION FAILED",
            TitleStyle(30)
        );

        GUI.Label(
            new Rect(
                result.x + 70,
                result.y + 103,
                result.width - 140,
                45
            ),
            won
                ? "Evidence secured. Extraction successful."
                : (ExpandedFacility ? "Health depleted. Break sight and hide when security is alerted." :
                    "Security detected you. Try a different route."),
            CenterStyle(15, Color.white)
        );

        Rect retryButton =
            new Rect(
                result.x + 65,
                result.y + 180,
                220,
                56
            );

        Rect menuButton =
            new Rect(
                result.x + result.width - 285,
                result.y + 180,
                220,
                56
            );

        if (GUI.Button(
            retryButton,
            "RETRY MISSION",
            ActionButtonStyle(17)
        ))
        {
            RetryMission();
        }

        if (GUI.Button(
            menuButton,
            "MAIN MENU",
            ActionButtonStyle(17)
        ))
        {
            ReturnToMainMenu();
        }

        GUI.Label(
            new Rect(
                result.x + 45,
                result.y + 255,
                result.width - 90,
                35
            ),
            "ENTER / SPACE  RETRY        ESC  MAIN MENU",
            CenterStyle(
                13,
                new Color(0.65f, 0.88f, 1f)
            )
        );


        GUI.Label(
            new Rect(
                result.x + 45,
                result.y + 290,
                result.width - 90,
                30
            ),
            "PLAYER ID: " + playerID,
            CenterStyle(14, Color.yellow)
        );

    }

    private void StartMission()
    {
        briefingVisible = false;
        helpVisible = false;
        toastMessage = "";
        Time.timeScale = 1f;
        ShowToast(ExpandedFacility ? "FIND THE GOLD KEYCARD - TAB OPENS THE MAP" :
            "COLLECT EVIDENCE - AVOID SECURITY", cyanTex, 3f);

        //Records when the player starts the mission
        GameplayMetrics.Instance?.StartSession();
    }

    private void PauseGame()
    {
        helpVisible = true;
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        helpVisible = false;
        Time.timeScale = 1f;
    }

    private void RequestRestart()
    {
        restartOpenedFromPause = helpVisible;
        restartConfirmationVisible = true;
        Time.timeScale = 0f;
    }

    private void CancelRestart()
    {
        restartConfirmationVisible = false;
        helpVisible = restartOpenedFromPause;
        restartOpenedFromPause = false;
        Time.timeScale = helpVisible ? 0f : 1f;
    }

    private void RequestMainMenu()
    {
        mainMenuConfirmationVisible = true;
        Time.timeScale = 0f;
    }

    private void CancelMainMenu()
    {
        mainMenuConfirmationVisible = false;
        // This dialog is reached from Pause/Help: return to pause safely.
        Time.timeScale = helpVisible ? 0f : 1f;
    }

    private void RetryMission()
    {
        //Records when player restarts the mission
        if (GameplayMetrics.Instance != null)
        {
            GameplayMetrics.Instance.RecordRestart();
            GameplayMetrics.Instance.EndSession("Restarted");
        }

        startGameplayAfterReload = true;
        restartConfirmationVisible = false;
        mainMenuConfirmationVisible = false;
        restartOpenedFromPause = false;
        helpVisible = false;
        briefingVisible = false;
        toastMessage = "";
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void ReturnToMainMenu()
    {
        //Records the player leaving an unfinished mission
        GameplayMetrics.Instance?.EndSession("Abandoned");

        // "Main Menu" is the team's existing Mission Briefing / Start Mission
        // screen, not a separate new menu scene.
        startGameplayAfterReload = false;
        restartConfirmationVisible = false;
        mainMenuConfirmationVisible = false;
        restartOpenedFromPause = false;
        helpVisible = false;
        briefingVisible = false;
        toastMessage = "";
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void DrawPanel(
        Rect rect,
        Texture2D background,
        Texture2D border)
    {
        GUI.DrawTexture(rect, border);

        GUI.DrawTexture(
            new Rect(
                rect.x + 2,
                rect.y + 2,
                rect.width - 4,
                rect.height - 4
            ),
            background
        );
    }

    private void DrawRectBorder(
        Rect rect,
        Texture2D texture,
        int thickness)
    {
        thickness = Mathf.Max(1, thickness);

        GUI.DrawTexture(
            new Rect(rect.x, rect.y, rect.width, thickness),
            texture
        );

        GUI.DrawTexture(
            new Rect(
                rect.x,
                rect.yMax - thickness,
                rect.width,
                thickness
            ),
            texture
        );

        GUI.DrawTexture(
            new Rect(rect.x, rect.y, thickness, rect.height),
            texture
        );

        GUI.DrawTexture(
            new Rect(
                rect.xMax - thickness,
                rect.y,
                thickness,
                rect.height
            ),
            texture
        );
    }

    private GUIStyle InvisibleButtonStyle()
    {
        GUIStyle style = new GUIStyle(GUIStyle.none);
        style.stretchWidth = true;
        style.stretchHeight = true;
        return style;
    }

    private GUIStyle ActionButtonStyle(int size)
    {
        GUIStyle style =
            new GUIStyle(GUI.skin.button);

        style.fontSize = size;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        style.normal.background = darkTex;
        style.hover.background = cyanTex;
        style.active.background = greenTex;
        style.focused.background = darkTex;

        style.normal.textColor = Color.white;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.focused.textColor = Color.white;

        return style;
    }

    private GUIStyle TitleStyle(int size)
    {
        GUIStyle style =
            new GUIStyle(GUI.skin.label);

        style.fontSize = size;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        style.normal.textColor =
            new Color(0.35f, 0.82f, 1f);

        return style;
    }

    private GUIStyle TextStyle(
        int size,
        Color color)
    {
        GUIStyle style =
            new GUIStyle(GUI.skin.label);

        style.fontSize = size;
        style.normal.textColor = color;
        style.wordWrap = true;

        return style;
    }

    private GUIStyle CenterStyle(
        int size,
        Color color)
    {
        GUIStyle style =
            TextStyle(size, color);

        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;

        return style;
    }

    private void CreateTextures()
    {
        blackTex =
            MakeTexture(
                new Color(0f, 0f, 0f, 1f)
            );

        darkTex =
            MakeTexture(
                new Color(
                    0.025f,
                    0.045f,
                    0.075f,
                    0.94f
                )
            );

        cyanTex =
            MakeTexture(
                new Color(
                    0.10f,
                    0.62f,
                    0.90f,
                    1f
                )
            );

        greenTex =
            MakeTexture(
                new Color(
                    0.16f,
                    0.78f,
                    0.33f,
                    1f
                )
            );

        yellowTex =
            MakeTexture(
                new Color(
                    0.95f,
                    0.68f,
                    0.10f,
                    1f
                )
            );

        redTex =
            MakeTexture(
                new Color(
                    0.90f,
                    0.18f,
                    0.18f,
                    1f
                )
            );

        hoverTex =
            MakeTexture(
                new Color(0.25f, 0.85f, 1f, 0.10f)
            );

        pressTex =
            MakeTexture(
                new Color(0.18f, 0.90f, 0.35f, 0.18f)
            );
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
