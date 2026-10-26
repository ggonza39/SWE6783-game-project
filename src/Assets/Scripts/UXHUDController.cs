using UnityEngine;
using UnityEngine.SceneManagement;

public class UXHUDController : MonoBehaviour
{
    private static UXHUDController instance;
    private static bool startGameplayAfterReload = false;

    private Transform player;
    private PlayerHiding playerHiding;

    private GameObject terminal;
    private TerminalInteraction terminalInteraction;
    private GameObject extractionZone;
    private Collider2D extractionCollider;
    private EvidenceCollectible[] evidenceItems;

    private Texture2D briefingTexture;

    private int totalEvidence = 1;
    private int lastEvidenceCount = 0;
    private bool briefingVisible = true;
    private bool helpVisible = false;
    private bool restartConfirmationVisible = false;
    private bool mainMenuConfirmationVisible = false;
    private bool restartOpenedFromPause = false;
    private bool terminalAccessed = false;

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
        terminal = null;
        terminalInteraction = null;
        extractionZone = null;
        extractionCollider = null;
        evidenceItems = null;

        helpVisible = false;
        restartConfirmationVisible = false;
        mainMenuConfirmationVisible = false;
        restartOpenedFromPause = false;
        terminalAccessed = false;
        toastMessage = "";
        toastUntil = 0f;
        toastBorder = cyanTex;

        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHiding =
                playerObject.GetComponent<PlayerHiding>();
        }

        terminal = GameObject.Find("Terminal");

        if (terminal != null)
        {
            terminalInteraction =
                terminal.GetComponent<TerminalInteraction>();

            // Our UX layer owns terminal prompts and feedback.
            // Disable Elvin's temporary OnGUI popup so messages do not
            // cover the center/top of the gameplay screen.
            if (terminalInteraction != null)
                terminalInteraction.enabled = false;
        }

        extractionZone = GameObject.Find("ExtractionZone");

        if (extractionZone != null)
        {
            extractionCollider =
                extractionZone.GetComponent<Collider2D>();

            // The terminal must be accessed before extraction is enabled.
            if (extractionCollider != null)
                extractionCollider.enabled = false;
        }

        evidenceItems =
            FindObjectsByType<EvidenceCollectible>(
                FindObjectsSortMode.None
            );

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
        UpdateTerminalInteraction();
    }

    private void UpdateEvidenceFeedback()
    {
        if (EvidenceManager.Instance == null)
            return;

        int evidence =
            EvidenceManager.Instance.EvidenceCollected;

        if (evidence > lastEvidenceCount)
        {
            ShowToast(
                "EVIDENCE ACQUIRED  " +
                evidence + " / " + totalEvidence,
                cyanTex,
                1.6f
            );
        }

        lastEvidenceCount = evidence;
    }

    private void UpdateTerminalInteraction()
    {
        if (player == null || terminal == null)
            return;

        if (Vector2.Distance(
                player.position,
                terminal.transform.position
            ) > 2f)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        //Records every E-key terminal attempts
        GameplayMetrics.Instance?.RecordTerminalInteraction();

        int evidence =
            EvidenceManager.Instance != null
            ? EvidenceManager.Instance.EvidenceCollected
            : 0;

        if (evidence < totalEvidence)
        {
            ShowToast(
                "TERMINAL LOCKED - COLLECT EVIDENCE",
                redTex,
                1.7f
            );
            return;
        }

        if (terminalAccessed)
        {
            ShowToast(
                "TERMINAL ALREADY ACCESSED",
                greenTex,
                1.2f
            );
            return;
        }

        terminalAccessed = true;

        if (extractionCollider != null)
            extractionCollider.enabled = true;

        ShowToast(
            "TERMINAL ACCESS GRANTED",
            greenTex,
            1.8f
        );
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
        int evidence =
            EvidenceManager.Instance != null
            ? EvidenceManager.Instance.EvidenceCollected
            : 0;

        bool hidden =
            playerHiding != null &&
            playerHiding.IsHidden;

        string objective;

        if (evidence < totalEvidence)
        {
            objective = "COLLECT THE EVIDENCE";
        }
        else if (!terminalAccessed)
        {
            objective = "ACCESS SECURITY TERMINAL [E]";
        }
        else
        {
            objective = "REACH EXTRACTION";
        }

        float topBarHeight = 58f;

        DrawPanel(
            new Rect(0, 0, Screen.width, topBarHeight),
            darkTex,
            cyanTex
        );

        GUI.Label(
            new Rect(14, 7, 205, 44),
            "PIXEL REBELLION",
            TitleStyle(17)
        );

        GUI.Label(
            new Rect(220, 7, 165, 44),
            "EVIDENCE  " + evidence + " / " + totalEvidence,
            CenterStyle(14, Color.white)
        );

        GUI.Label(
            new Rect(385, 7, 180, 44),
            hidden
                ? "STATUS  HIDDEN"
                : "STATUS  VISIBLE",
            CenterStyle(
                14,
                hidden
                    ? new Color(0.35f, 1f, 0.48f)
                    : new Color(1f, 0.85f, 0.25f)
            )
        );

        GUI.Label(
            new Rect(
                565,
                7,
                Mathf.Max(220f, Screen.width - 579f),
                44
            ),
            "OBJECTIVE:  " + objective,
            CenterStyle(
                14,
                new Color(0.45f, 1f, 0.55f)
            )
        );

        DrawContextPrompt(evidence);
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
                    "EVIDENCE FOUND  -  MOVE CLOSER TO COLLECT";
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
                else if (terminalAccessed)
                {
                    message = "TERMINAL ACCESSED";
                    border = greenTex;
                }
                else
                {
                    message =
                        "PRESS E TO ACCESS SECURITY TERMINAL";
                    border = greenTex;
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
                else if (!terminalAccessed)
                {
                    message =
                        "EXTRACTION LOCKED  -  ACCESS TERMINAL";
                    border = redTex;
                }
                else
                {
                    message = "EXTRACTION ZONE";
                    border = greenTex;
                }
            }
        }

        if (string.IsNullOrEmpty(message))
        {
            message =
                "WASD / ARROWS  MOVE    SHIFT  SPRINT    E  INTERACT    R  RESTART    ESC  PAUSE / HELP";
            border = cyanTex;
        }

        float bottomBarHeight = 46f;

        Rect bottomBar =
            new Rect(
                0,
                Screen.height - bottomBarHeight,
                Screen.width,
                bottomBarHeight
            );

        DrawPanel(
            bottomBar,
            darkTex,
            border
        );

        GUI.Label(
            new Rect(
                12,
                bottomBar.y + 6,
                Screen.width - 24,
                34
            ),
            message,
            CenterStyle(13, Color.white)
        );
    }

    private void DrawToast()
    {
        if (string.IsNullOrEmpty(toastMessage) ||
            Time.unscaledTime >= toastUntil)
        {
            return;
        }

        float remaining = toastUntil - Time.unscaledTime;
        float alpha = Mathf.Clamp01(remaining / 0.35f);

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);

        Rect toast =
            new Rect(
                Screen.width - 355f,
                70f,
                335f,
                46f
            );

        DrawPanel(
            toast,
            darkTex,
            toastBorder != null ? toastBorder : cyanTex
        );

        GUI.Label(
            new Rect(
                toast.x + 12f,
                toast.y + 6f,
                toast.width - 24f,
                34f
            ),
            toastMessage,
            CenterStyle(13, Color.white)
        );

        GUI.color = previousColor;
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

    private void ConfigureGameplayViewport()
    {
        Camera camera = Camera.main;

        if (camera == null)
            return;

        float screenHeight =
            Mathf.Max(1f, Screen.height);

        float topSpace =
            58f / screenHeight;

        float bottomSpace =
            46f / screenHeight;

        camera.rect =
            new Rect(
                0f,
                bottomSpace,
                1f,
                Mathf.Max(
                    0.1f,
                    1f - topSpace - bottomSpace
                )
            );
    }

    private void DrawHelp()
    {
        GUI.DrawTexture(
            new Rect(
                0,
                0,
                Screen.width,
                Screen.height
            ),
            blackTex
        );

        Rect panel =
            new Rect(
                Screen.width / 2f - 350,
                Screen.height / 2f - 235,
                700,
                470
            );

        DrawPanel(panel, darkTex, cyanTex);

        GUI.Label(
            new Rect(
                panel.x + 40,
                panel.y + 22,
                panel.width - 80,
                44
            ),
            "PAUSE / HELP",
            TitleStyle(27)
        );

        GUI.Label(
            new Rect(
                panel.x + 70,
                panel.y + 82,
                panel.width - 140,
                220
            ),
            "WASD / ARROW KEYS   Move\n\n" +
            "SHIFT   Sprint\n\n" +
            "E   Interact with terminal\n\n" +
            "R   Request mission restart\n\n" +
            "Enter hiding areas to conceal yourself\n\n" +
            "Collect evidence, access the terminal, then extract",
            TextStyle(17, Color.white)
        );

        float buttonWidth = 180f;
        float gap = 18f;
        float totalWidth = buttonWidth * 3f + gap * 2f;
        float startX = panel.x + (panel.width - totalWidth) / 2f;
        float buttonY = panel.y + 342f;

        Rect resumeButton =
            new Rect(startX, buttonY, buttonWidth, 52f);

        Rect restartButton =
            new Rect(
                startX + buttonWidth + gap,
                buttonY,
                buttonWidth,
                52f
            );

        Rect menuButton =
            new Rect(
                startX + (buttonWidth + gap) * 2f,
                buttonY,
                buttonWidth,
                52f
            );

        if (GUI.Button(
            resumeButton,
            "RESUME",
            ActionButtonStyle(16)
        ))
        {
            ResumeGame();
        }

        if (GUI.Button(
            restartButton,
            "RESTART",
            ActionButtonStyle(16)
        ))
        {
            RequestRestart();
        }

        if (GUI.Button(
            menuButton,
            "MAIN MENU",
            ActionButtonStyle(16)
        ))
        {
            RequestMainMenu();
        }

        GUI.Label(
            new Rect(
                panel.x + 60,
                panel.y + 407,
                panel.width - 120,
                30
            ),
            "ESC  RESUME",
            CenterStyle(
                13,
                new Color(0.55f, 0.85f, 1f)
            )
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
                : "Security detected you. Try a different route.",
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
