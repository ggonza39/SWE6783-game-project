using UnityEngine;
using UnityEngine.SceneManagement;

public class UXHUDController : MonoBehaviour
{
    private static UXHUDController instance;
    private static bool startGameplayAfterReload = false;
    private static bool showBriefingAfterReload;
    private float advanceAt=-1f;
    private int lastSecuredSectors;
    private string campaignLoadError;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSceneRequests(){startGameplayAfterReload=false;showBriefingAfterReload=false;}

    private Transform player;
    private PlayerHiding playerHiding;
    private PlayerController playerController;
    private UXMissionBriefingScreen missionBriefing;
    private UXMissionMenuScreen missionMenu;

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
    private float interactionCompletedUntil = -1f;
    private string interactionCompletedLabel = "";
    private int lastReleasedLocks;

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
        interactionCompletedUntil = -1f;
        interactionCompletedLabel = "";
        lastReleasedLocks = 0;
        lastSecuredSectors=0;advanceAt=-1f;
        campaignLoadError=null;
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

        UXMissionDefinition definition = FindFirstObjectByType<UXMissionDefinition>();
        if (definition != null) totalEvidence = definition.requiredEvidence;
        else if (evidenceItems != null && evidenceItems.Length > 0)
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
        missionMenu = ExpandedFacility ? new UXMissionMenuScreen() : null;
        if(missionMenu!=null)
        {
            missionMenu.ContinueRequested=()=>
            {
                bool fresh=UXCampaign.SelectedSlot==null;
                LoadCampaignScene(UXCampaign.PrepareContinue(),!fresh,fresh);
            };
            missionMenu.RestartConfirmed=()=>LoadCampaignScene(UXCampaign.FacilityScene,false,true);
            if(showBriefingAfterReload)missionMenu.BriefingVisibleForNewGame();
        }
        showBriefingAfterReload=false;
        if (ExpandedFacility) UXPlayerProgress.ApplyDisplaySettings();

        //Creates a fresh record when player reloads the mission
        bool autoStart = startGameplayAfterReload;

        briefingVisible = !autoStart;
        startGameplayAfterReload = false;

        if (autoStart)
        {
            ConfigureMissionDifficulty();
            GameplayMetrics.Instance?.StartSession();
            UXCampaign.BindTelemetrySession();
            lastEvidenceCount=EvidenceManager.Instance!=null?EvidenceManager.Instance.EvidenceCollected:0;
            lastSecuredSectors=UXStealthDirector.Instance!=null && UXStealthDirector.Instance.Sectors!=null?
                UXStealthDirector.Instance.Sectors.SecuredSectors:0;
            ShowToast(UXCampaign.IsCrimson?"LEVEL 2 / CRIMSON DEPTHS":"CAMPAIGN RESUMED",cyanTex,4f);
        }


        ConfigureGameplayViewport();
        Time.timeScale = briefingVisible ? 0f : 1f;
    }

    private void Update()
    {
        if (Screen.width != hudViewportWidth || Screen.height != hudViewportHeight)
            ConfigureGameplayViewport();
        if (briefingVisible)
        {
            if (missionMenu != null && !missionMenu.BriefingVisible)
            {
                missionMenu.HandleKeyboard();
                return;
            }
            if (missionMenu != null && Input.GetKeyDown(KeyCode.Escape))
            {
                missionMenu.ShowHome();
                return;
            }
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

            if (!GameManager.Instance.PlayerWon && UXStealthDirector.Instance != null &&
                UXStealthDirector.Instance.Effects != null && (UXStealthDirector.Instance.Effects.DeathRevealing ||
                    UXStealthDirector.Instance.Effects.Replay.WaitingForFreeze)) return;

            UXDeathReplay deathReplay=UXStealthDirector.Instance!=null && UXStealthDirector.Instance.Effects!=null?
                UXStealthDirector.Instance.Effects.Replay:null;
            if(!GameManager.Instance.PlayerWon && deathReplay!=null && deathReplay.AutoPlaying)
            {
                if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Escape))deathReplay.SkipAuto();
                return;
            }

            if(ExpandedFacility && GameManager.Instance.PlayerWon && UXCampaign.NeedsAdvance)
            {
                if(advanceAt<0f)advanceAt=Time.unscaledTime+1.25f;
                if(Time.unscaledTime>=advanceAt)LoadCampaignScene(UXCampaign.CrimsonScene,true);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space))
            {
                ContinueMission();
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
        if (ExpandedFacility && UXStealthDirector.Instance != null)
        {
            UXSectorMission sectors=UXStealthDirector.Instance.Sectors;
            if(sectors!=null && sectors.SecuredSectors>lastSecuredSectors)
            {
                ConfirmInteraction(UXPlayerProgress.Store.HasSaveError?"SECTOR SECURED":"SECTOR SECURED - CHECKPOINT SAVED");
                ShowToast(UXPlayerProgress.Store.HasSaveError?"SECTOR SECURED - SAVE UNAVAILABLE":
                    "CHECKPOINT SAVED  /  SECTOR "+sectors.SecuredSectors,greenTex,3f);
                lastSecuredSectors=sectors.SecuredSectors;
            }
            int released = UXStealthDirector.Instance.ReleasedLocks;
            if (released > lastReleasedLocks) ConfirmInteraction("EXTRACTION DOOR OPEN");
            lastReleasedLocks = released;
        }
    }

    private void UpdateEvidenceFeedback()
    {
        if (EvidenceManager.Instance == null)
            return;

        int evidence =
            EvidenceManager.Instance.EvidenceCollected;

        if (evidence > lastEvidenceCount)
        {
            ConfirmInteraction("DOWNLOAD COMPLETE");
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
            ConfirmInteraction("SECURITY HACKED - CAMERAS OFFLINE");
            bool routeLocked = ExpandedFacility && UXStealthDirector.Instance != null &&
                !UXStealthDirector.Instance.ExtractionLocksReleased;
            ShowToast(routeLocked ? "CAMERAS OFFLINE - OVERRIDE THE EXIT DOOR" :
                "EXIT UNLOCKED - REACH EXTRACTION", greenTex, 2.6f);
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
            UXStealthDirector ended = ExpandedFacility ? UXStealthDirector.Instance : null;
            if (!GameManager.Instance.PlayerWon && ended != null && ended.Effects != null &&
                (ended.Effects.DeathRevealing || ended.Effects.Replay.WaitingForFreeze))
            {
                DrawHUD();
                DrawHazardFeedback();
                if(ended.Effects.ImpactPulse>0f)
                {
                    Color previous=GUI.color;
                    GUI.color=ended.LastDamageCause=="LASER BARRIER"?new Color(.55f,.9f,1f,ended.Effects.ImpactPulse*.3f):new Color(1f,.3f,.15f,ended.Effects.ImpactPulse*.3f);
                    GUI.DrawTexture(new Rect(0,GetTopBarHeight(),Screen.width,Screen.height-GetTopBarHeight()-GetBottomBarHeight()),Texture2D.whiteTexture);
                    GUI.color=previous;
                }
                float scale = GetHudScale();
                Rect caption = new Rect(14f * scale, Screen.height - GetBottomBarHeight() + 10f * scale,
                    Screen.width - 28f * scale, 46f * scale);
                DrawPanel(caption, darkTex, redTex);
                GUI.Label(caption, ended.DeathHeadline, CenterStyle(Mathf.RoundToInt(21f * scale), new Color(1f,.55f,.35f)));
                return;
            }
            if(!GameManager.Instance.PlayerWon && ended!=null && ended.Effects!=null && ended.Effects.Replay.AutoPlaying)
            { DrawAutomaticDeathReplay(ended.Effects.Replay,ended.DeathHeadline);return; }
            if(ExpandedFacility && GameManager.Instance.PlayerWon && UXCampaign.NeedsAdvance)
            {
                DrawPanel(new Rect(0,0,Screen.width,Screen.height),darkTex,greenTex);
                GUI.Label(new Rect(20,Screen.height*.35f,Screen.width-40,60),"LEVEL 1 CLEARED",TitleStyle(30));
                GUI.Label(new Rect(20,Screen.height*.50f,Screen.width-40,48),"DESCENDING INTO CRIMSON DEPTHS",CenterStyle(20,Color.white));
                return;
            }
            DrawMissionResult();
            return;
        }

        DrawHUD();

        // Labels are visual guidance only: no gameplay state is changed.
        if (!helpVisible && !restartConfirmationVisible &&
            !mainMenuConfirmationVisible)
        {
            DrawObjectiveMarkers();
            DrawHazardFeedback();
            DrawExtractionSignal();
        }
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
        if (missionMenu != null && !missionMenu.BriefingVisible)
        {
            missionMenu.Draw();
            return;
        }
        if (missionBriefing != null)
        {
            if (missionBriefing.Draw()) StartMission();
            if (missionMenu != null) missionMenu.DrawBriefingNavigation();
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
        float s = GetHudScale(), h = GetTopBarHeight() - (ExpandedFacility ? 56f * s : 0f), p = 12f * s;
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
        GUI.Label(title, UXCampaign.IsCrimson?"CRIMSON DEPTHS":"PIXEL REBELLION",
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
        if (stealth != null){DrawSecurityStatus(stealth, h, s);DrawSprintReserve(h+32f*s,s);}
        DrawContextPrompt(evidence);
    }

    private void DrawSprintReserve(float y,float s)
    {
        float left=UXCampaign.SprintRemaining,ratio=left/UXCampaign.SprintCapacity;
        ratio=Mathf.Clamp01(ratio);
        Color tint=StaminaTint(ratio);
        float p=14f*s,label=158f*s,copy=250f*s;
        GUI.Label(new Rect(p,y,label,20f*s),"SPRINT  "+left.ToString("0.0")+"s",CenterStyle(Mathf.RoundToInt(12f*s),tint));
        Rect track=new Rect(p+label+8f*s,y+6f*s,Screen.width-2f*p-label-copy-20f*s,9f*s);
        Color old=GUI.color;GUI.color=new Color(.12f,.23f,.28f);GUI.DrawTexture(track,Texture2D.whiteTexture);
        GUI.color=tint;GUI.DrawTexture(new Rect(track.x,track.y,track.width*ratio,track.height),Texture2D.whiteTexture);GUI.color=old;
        DrawRectBorder(track,Texture2D.whiteTexture,1);
        GUI.Label(new Rect(Screen.width-p-copy,y,copy,20f*s),left<=0f?"RESERVE EMPTY / CROUCH ONLY":ratio<=.25f?
            "RESERVE CRITICAL / NO RECHARGE":ratio<=.5f?"RESERVE LOW / NO RECHARGE":"RESERVE READY / NO RECHARGE",
            CenterStyle(Mathf.RoundToInt(11f*s),tint));
    }

    private static Color StaminaTint(float fraction)
    {
        float f=Mathf.Clamp01(fraction);
        Color red=new Color(1f,.25f,.18f),yellow=new Color(1f,.80f,.18f),green=new Color(.28f,1f,.48f);
        Color from=f<.5f?red:yellow,to=f<.5f?yellow:green;
        float t=f<.5f?Mathf.Clamp01((f-.2f)/.3f):(f-.5f)*2f;
        return new Color(from.r+(to.r-from.r)*t,from.g+(to.g-from.g)*t,from.b+(to.b-from.b)*t);
    }

    private void DrawExtractionSignal()
    {
        UXStealthDirector d=UXStealthDirector.Instance;
        if(d==null || !d.IsCrimson || d.Difficulty.Mode!=UXDifficulty.Hard || d.Sectors.SecuredSectors<3 ||
            d.InteractionAvailable || (d.Hazards!=null && d.Hazards.Warning!=null))return;
        float s=GetHudScale(),width=Mathf.Min(Screen.width-28f*s,650f*s);
        Rect panel=new Rect((Screen.width-width)*.5f,Screen.height-GetBottomBarHeight()-43f*s,width,30f*s);
        DrawPanel(panel,darkTex,d.ExtractionLocksReleased?greenTex:yellowTex);
        GUI.Label(panel,d.ExtractionLocksReleased?"EXTRACTION ROUTE OPEN / REACH THE LIFT":"LIFT POWER RESTORED / OVERRIDE THE EXIT DOOR",
            CenterStyle(Mathf.RoundToInt(13f*s),d.ExtractionLocksReleased?new Color(.35f,1f,.65f):Color.yellow));
    }

    private void DrawHazardFeedback()
    {
        UXCrimsonHazards hazards=UXStealthDirector.Instance!=null?UXStealthDirector.Instance.Hazards:null;
        if(hazards==null)return;
        float s=GetHudScale();
        if(hazards.Warning!=null)
        {
            float width=Mathf.Min(Screen.width-24f,610f*s);
            Rect panel=new Rect((Screen.width-width)*.5f,GetTopBarHeight()+10f*s,width,36f*s);
            DrawPanel(panel,darkTex,yellowTex);
            GUI.Label(panel,hazards.Warning,CenterStyle(Mathf.RoundToInt(15f*s),Color.yellow));
        }
        if(hazards.ImpactFlash>0f)
        {
            Color previous=GUI.color;GUI.color=new Color(1f,.4f,.2f,hazards.ImpactFlash*.8f);
            DrawRectBorder(new Rect(0,GetTopBarHeight(),Screen.width,Screen.height-GetTopBarHeight()-GetBottomBarHeight()),redTex,Mathf.Max(2,Mathf.RoundToInt(5*s)));
            GUI.color=previous;
        }
    }

    private void DrawSecurityStatus(UXStealthDirector stealth, float y, float s)
    {
        Color alert = stealth.State.Phase == UXAlertPhase.Engaged ? new Color(1f, 0.35f, 0.3f) :
            stealth.State.Phase == UXAlertPhase.Clear ? new Color(0.35f, 0.95f, 0.7f) : new Color(1f, 0.8f, 0.25f);
        float third = Screen.width / 3f;
        GUI.Label(new Rect(12f * s, y, third - 20f * s, 27f * s),
            stealth.Difficulty.Mode.ToString().ToUpperInvariant() + "  |  HEALTH " + stealth.State.Health + " / 100",
            CenterStyle(Mathf.RoundToInt(14f * s), stealth.State.Health <= stealth.Difficulty.HitDamage ? Color.red : Color.white));
        GUI.Label(new Rect(third, y, third, 27f * s), stealth.PhaseLabel,
            CenterStyle(Mathf.RoundToInt(14f * s), alert));
        GUI.Label(new Rect(third * 2f, y, third, 27f * s),
            stealth.IsCrimson?"SECTORS: "+stealth.Sectors.SecuredSectors+" / 3   |   TAB MAP":stealth.KeyAcquired ? (stealth.Difficulty.Mode == UXDifficulty.Hard ?
                "EXIT DOOR: " + (stealth.ExtractionLocksReleased ? "OPEN" : "LOCKED") + "   |   TAB MAP" :
                "ACCESS GATE: OPEN   |   TAB MAP") :
                "ACCESS KEY: REQUIRED   |   TAB MAP",
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
        UXStealthDirector stealth = ExpandedFacility ? UXStealthDirector.Instance : null;
        bool showMeter = stealth != null && (stealth.InteractionAvailable ||
            interactionCompletedUntil > Time.time);
        if (showMeter)
            border = stealth.InteractionProgress <= 0f && interactionCompletedUntil > Time.time
                ? greenTex : cyanTex;
        DrawPanel(bar, darkTex, border);
        if (showMeter)
        {
            DrawInteractionMeter(stealth, bar, s);
            return;
        }

        GUI.Label(new Rect(12f * s, bar.y + 3f * s,
            Screen.width - 24f * s, (ExpandedFacility ? 40f * s : bh) - 6f * s), message,
            CenterStyle(Mathf.RoundToInt((Screen.width < 1000 ? 13f : 15f) * s), Color.white));
        if (ExpandedFacility && UXPlayerProgress.Data.showControlHints)
            GUI.Label(new Rect(12f * s, bar.y + 47f * s,
                Screen.width - 24f * s, 23f * s),
                "HOLD E: INTERACT    |    TAB: MAP    |    ESC: PAUSE",
                CenterStyle(Mathf.RoundToInt(12f * s), new Color(0.75f, 0.83f, 0.90f)));
    }

    private void ConfirmInteraction(string label)
    {
        if (!ExpandedFacility) return;
        // Only confirmed evidence/terminal state changes can show 100% success.
        interactionCompletedLabel = label;
        interactionCompletedUntil = Time.time + 0.9f;
    }

    private void DrawInteractionMeter(UXStealthDirector stealth, Rect bar, float s)
    {
        float progress = Mathf.Clamp01(stealth.InteractionProgress);
        bool active = progress > 0f;
        bool completed = !active && interactionCompletedUntil > Time.time;
        Color accent = completed ? new Color(0.36f, 1f, 0.62f) :
            new Color(0.20f, 0.88f, 1f);
        string label = completed ? interactionCompletedLabel :
            (active ? stealth.InteractionAction : stealth.InteractionInstruction);
        // Keep the number outside the fill so its contrast never changes.
        int percent = completed ? 100 : Mathf.Min(99, Mathf.FloorToInt(progress * 100f));
        float padding = 24f * s;
        float percentWidth = 76f * s;
        GUIStyle title = TextStyle(Mathf.RoundToInt((Screen.width < 800 ? 12f : 14f) * s), Color.white);
        title.alignment = TextAnchor.MiddleLeft;
        title.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(padding, bar.y + 6f * s,
            bar.width - 2f * padding - percentWidth, 25f * s), label, title);
        GUIStyle number = CenterStyle(Mathf.RoundToInt(20f * s), accent);
        number.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(bar.width - padding - percentWidth, bar.y + 4f * s,
            percentWidth, 27f * s), percent + "%", number);

        Rect track = new Rect(padding, bar.y + 36f * s,
            bar.width - 2f * padding, 22f * s);
        DrawProgressRect(track, Color.white);
        float inset = Mathf.Max(2f, 2f * s);
        Rect inner = new Rect(track.x + inset, track.y + inset,
            track.width - inset * 2f, track.height - inset * 2f);
        DrawProgressRect(inner, new Color(0.015f, 0.025f, 0.045f));
        float fillWidth = inner.width * (completed ? 1f : progress);
        if (fillWidth > 0f)
        {
            DrawProgressRect(new Rect(inner.x, inner.y, fillWidth, inner.height), accent);
            if (!completed)
            {
                float edge = Mathf.Min(fillWidth, 2f * s);
                DrawProgressRect(new Rect(inner.x + fillWidth - edge, inner.y,
                    edge, inner.height), Color.white);
            }
        }

        string hint = completed ? "COMPLETE - CONTINUE TO YOUR NEXT OBJECTIVE" :
            (active ? "KEEP HOLDING E  |  RELEASE OR MOVE AWAY TO CANCEL" :
            "HOLD E TO START  |  STAY CLOSE UNTIL THE BAR IS FULL");
        GUI.Label(new Rect(padding, bar.y + 60f * s,
            bar.width - 2f * padding, 19f * s), hint,
            CenterStyle(Mathf.RoundToInt(11f * s), new Color(0.80f, 0.87f, 0.93f)));
    }

    private void DrawProgressRect(Rect bounds, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(bounds, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void DrawObjectiveMarkers()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        int collected = EvidenceManager.Instance != null
            ? EvidenceManager.Instance.EvidenceCollected : 0;

        // Only show the current required objective to avoid label clutter.
        UXStealthDirector director=ExpandedFacility?UXStealthDirector.Instance:null;
        if(director!=null && director.IsCrimson)
        {
            UXSectorMission sectors=director.Sectors;
            for(int i=0;i<9;i++)if(sectors.CacheAvailable(i))
                DrawObjectMarker(camera,sectors.Cache(i).transform.position,"EVIDENCE",cyanTex);
            if(sectors.SecuredSectors<3)
                DrawObjectMarker(camera,sectors.Consoles[sectors.SecuredSectors].transform.position,"HACK",yellowTex);
            else if(!sectors.ExitDoor.IsUnlocked)DrawObjectMarker(camera,sectors.ExitDoor.transform.position,"EXIT DOOR",yellowTex);
            else if(extractionZone!=null)DrawObjectMarker(camera,extractionZone.transform.position,"EXTRACTION",greenTex);
        }
        else if (ExpandedFacility && UXStealthDirector.Instance != null && !UXStealthDirector.Instance.KeyAcquired)
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
                    if (evidence != null && evidence.gameObject.activeSelf)
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
        else if (ExpandedFacility && UXStealthDirector.Instance != null && !UXStealthDirector.Instance.ExtractionLocksReleased)
        {
            foreach (UXAccessLock access in UXStealthDirector.Instance.AccessLocks)
                if (!access.IsUnlocked)
                    DrawObjectMarker(camera, access.transform.position, "EXIT DOOR [HOLD E]", yellowTex);
        }
        else if (TerminalAccessed && extractionZone != null)
        {
            DrawObjectMarker(camera, extractionZone.transform.position,
                "EXTRACTION", greenTex);
        }

        if (ExpandedFacility && UXStealthDirector.Instance != null &&
            !UXStealthDirector.Instance.IsCrimson && UXStealthDirector.Instance.KeyAcquired && player != null &&
            Vector2.Distance(player.position, UXStealthDirector.Instance.GatePosition) < 6f)
            DrawObjectMarker(camera, UXStealthDirector.Instance.GatePosition, "OPEN", greenTex);

        if (player == null || hidingSpots == null) return;
        if (ExpandedFacility && playerHiding != null && playerHiding.IsHidden)
        {
            DrawObjectMarker(camera, player.position + new Vector3(0f, .35f, 0f), "HIDDEN", greenTex);
            return;
        }
        if (playerHiding != null && playerHiding.IsHidden) return;

        foreach (GameObject spot in hidingSpots)
        {
            if (spot != null &&
                Vector2.Distance(player.position, spot.transform.position) <= 1.8f)
            {
                DrawObjectMarker(camera, spot.transform.position,
                    ExpandedFacility ? "HIDE BAY" : "HIDE", cyanTex);
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
            label=="EXIT DOOR"||label=="EXIT DOOR [HOLD E]"?"EXIT DOOR":label=="HACK"?"HACK":
            label == "TERMINAL [E]" ? (terminalInteraction != null && terminalInteraction.PlayerNearby ? "USE E" : "TERM") :
            label == "EXTRACTION" ? "EXIT" :
            (label == "KEY" || label == "LOCKED" || label == "OPEN" || label == "HIDDEN" || label == "HIDE BAY" ? label : "HIDE");
        float s = Mathf.Min(GetHudScale(), 1.25f);
        float width = (compactLabel=="EXIT DOOR"?110f:label == "HIDDEN" || label == "HIDE BAY" ? 94f : 66f) * s;
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
            CenterStyle(Mathf.RoundToInt(12f * s), label == "HIDDEN" ? new Color(.45f, 1f, .65f) : Color.white));
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
            (ExpandedFacility ? 56f * GetHudScale() : 0f);
    }

    private bool ExpandedFacility => UXCampaign.IsCampaignScene;

    private float GetBottomBarHeight()
    {
        // Reserve a stable footer so interaction feedback never covers gameplay.
        return (ExpandedFacility ? 84f : 42f) * GetHudScale();
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
            "2   Hold E to download all data and hack the terminal.\n\n" +
            "3   Follow the mission objectives to extraction.\n\n" +
            "RED: pursuit. Break sight and hide. YELLOW: searching.\n" +
            "Walk into an open blue bay. Green HIDDEN confirms cover.\n\n" +
            "WASD crouch   C / CTRL slow   SHIFT limited sprint\n" +
            "No sprint recharge. TAB map   R restart   ESC resume";
        if(UXCampaign.IsCrimson)instructions=
            "1   Download each sector's data and hack its console.\n\n"+
            "2   Follow the objectives to the extraction lift.\n\n"+
            "Replay restores secured sectors; spent sprint stays spent.\n"+
            "Enter blue bays; green HIDDEN confirms cover.\n\n"+
            "WASD crouch   C / CTRL slow   SHIFT limited sprint\n"+
            "TAB map   R restart level (confirm)   ESC resume";

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
            ExpandedFacility?"RESTART LEVEL?":"RESTART MISSION?",
            TitleStyle(26)
        );

        GUI.Label(
            new Rect(panel.x + 25f, panel.y + 88f, panel.width - 50f, 45f),
            ExpandedFacility?"Current-level progress and checkpoints will be lost. Earlier level clears are kept.":
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
            ExpandedFacility?"Progress since the last checkpoint will be lost.":"Current mission progress will be lost.",
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

        bool won = GameManager.Instance.PlayerWon;
        UXStealthDirector resultDirector=ExpandedFacility?UXStealthDirector.Instance:null;
        if(!won && resultDirector!=null && resultDirector.Effects!=null && resultDirector.Effects.Replay.HasFrames)
        { DrawDeathReplayResult(resultDirector.Effects.Replay,resultDirector);return; }
        bool hardExtraction=won && resultDirector!=null && resultDirector.IsCrimson && resultDirector.Difficulty.Mode==UXDifficulty.Hard;

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
            won ? (hardExtraction ? "EXTRACTION CONFIRMED" : "MISSION COMPLETE") :
                resultDirector!=null ? resultDirector.DeathHeadline : "MISSION FAILED",
            CenterStyle(26,won?new Color(.35f,1f,.65f):new Color(1f,.45f,.32f))
        );

        GUI.Label(
            new Rect(
                result.x + 70,
                result.y + 103,
                result.width - 140,
                45
            ),
            campaignLoadError??(won
                ? (hardExtraction?"Crimson Depths cleared. Evidence uplink complete.":"Evidence secured. Extraction successful.")
                : (UXStealthDirector.Instance!=null && UXStealthDirector.Instance.LastDamageCause=="PROXIMITY MINE" ?
                    "Caught in a mine blast. Watch the warning ring and sprint clear." :
                    UXStealthDirector.Instance!=null && UXStealthDirector.Instance.LastDamageCause=="LASER BARRIER" ?
                    "Caught by a laser barrier. Wait for a safe gap before crossing." :
                    ExpandedFacility ? "Health depleted. Break sight and hide when security is alerted." :
                    "Security detected you. Try a different route.")),
            CenterStyle(15, Color.white)
        );

        if (ExpandedFacility && UXStealthDirector.Instance != null)
            GUI.Label(new Rect(result.x + 25f, result.y + 148f, result.width - 50f, 25f),
                UXStealthDirector.Instance.Difficulty.Mode.ToString().ToUpperInvariant() +
                (GameplayMetrics.Instance != null ? "  |  TIME " +
                    UXPlayerProgress.FormatTime(UXCampaign.RunSeconds) +
                    "  |  DETECTIONS " + UXCampaign.TotalDetections : ""),
                CenterStyle(12, new Color(0.65f, 0.88f, 1f)));

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
            ExpandedFacility?(won?"REPLAY LEVEL":"REPLAY"):"RETRY MISSION",
            ActionButtonStyle(17)
        ))
        {
            ContinueMission();
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
            ExpandedFacility?"ENTER / SPACE  REPLAY        ESC  MAIN MENU":"ENTER / SPACE  RETRY        ESC  MAIN MENU",
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

    private void PaintReplayRect(Rect rect,Color color)
    {
        Color previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;
    }

    private Rect DrawReplayFrame(UXDeathReplay replay,Rect frame)
    {
        GUI.DrawTexture(frame,blackTex);Rect fitted=frame;
        if(replay.CurrentFrame!=null)
        {
            RenderTexture image=replay.CurrentFrame;
            float aspect=replay.PlaybackAspect;
            float width=Mathf.Min(frame.width,frame.height*aspect),height=width/aspect;
            fitted=new Rect(frame.x+(frame.width-width)*.5f,frame.y+(frame.height-height)*.5f,width,height);
            GUI.DrawTextureWithTexCoords(fitted,image,replay.PlaybackUV,true);
        }
        Color previous=GUI.color;GUI.color=new Color(.30f,.65f,.76f);
        DrawRectBorder(fitted,Texture2D.whiteTexture,1);GUI.color=previous;
        return fitted;
    }

    private void DrawSecurityReplay(UXDeathReplay replay,Rect housing,float s,bool watchControl)
    {
        Color steel=new Color(.15f,.22f,.28f),ink=new Color(.025f,.045f,.065f),cyan=new Color(.35f,.88f,1f);
        PaintReplayRect(housing,steel);
        Rect inset=new Rect(housing.x+2f*s,housing.y+2f*s,housing.width-4f*s,housing.height-4f*s);
        PaintReplayRect(inset,ink);
        Rect header=new Rect(housing.x+12f*s,housing.y+7f*s,housing.width-24f*s,34f*s);
        GUIStyle left=CenterStyle(Mathf.Max(10,Mathf.RoundToInt(14f*s)),cyan);left.alignment=TextAnchor.MiddleLeft;
        GUI.Label(new Rect(header.x,header.y,header.width*.65f,header.height),replay.CameraLabel,left);
        GUIStyle right=CenterStyle(Mathf.Max(9,Mathf.RoundToInt(12f*s)),new Color(.72f,.84f,.9f));right.alignment=TextAnchor.MiddleRight;
        GUI.Label(new Rect(header.x+header.width*.65f,header.y,header.width*.35f,header.height),"COLOR / RECORDED",right);
        Rect viewport=new Rect(housing.x+12f*s,housing.y+42f*s,housing.width-24f*s,housing.height-90f*s);
        Rect picture=DrawReplayFrame(replay,viewport);
        // Camera brackets remain outside the picture; no blur/grain/color filter.
        float arm=16f*s,thick=Mathf.Max(1f,2f*s);
        for(int side=0;side<2;side++)
        {
            float x=side==0?picture.x:picture.x+picture.width-arm;
            PaintReplayRect(new Rect(x,picture.y,arm,thick),cyan);
            PaintReplayRect(new Rect(x,picture.y+picture.height-thick,arm,thick),cyan);
        }
        for(int side=0;side<2;side++)
        {
            float x=side==0?picture.x:picture.x+picture.width-thick;
            PaintReplayRect(new Rect(x,picture.y,thick,arm),cyan);
            PaintReplayRect(new Rect(x,picture.y+picture.height-arm,thick,arm),cyan);
        }
        float stampWidth=Mathf.Min(picture.width-16f*s,340f*s);
        Rect stamp=new Rect(picture.x+picture.width-stampWidth-8f*s,picture.y+8f*s,stampWidth,30f*s);
        PaintReplayRect(stamp,new Color(.015f,.025f,.035f,.84f));
        GUI.Label(stamp,replay.CurrentFrameTime.ToString("yyyy-MM-dd HH:mm:ss.fff",System.Globalization.CultureInfo.InvariantCulture),
            CenterStyle(Mathf.Max(9,Mathf.RoundToInt(13f*s)),Color.white));
        Rect footer=new Rect(housing.x+12f*s,housing.y+housing.height-44f*s,housing.width-24f*s,34f*s);
        GUI.Label(new Rect(footer.x,footer.y,footer.width-(watchControl?175f*s:0f),footer.height),
            "ARCHIVE PLAYBACK  "+replay.CursorSeconds.ToString("0.0")+"s / "+replay.ClipSeconds.ToString("0.0")+"s",
            CenterStyle(Mathf.Max(9,Mathf.RoundToInt(12f*s)),new Color(.75f,.86f,.92f)));
        if(watchControl)
        {
            Rect button=new Rect(footer.x+footer.width-164f*s,footer.y,164f*s,footer.height);
            Color previous=GUI.color;GUI.color=cyan;DrawRectBorder(button,Texture2D.whiteTexture,1);GUI.color=previous;
            GUIStyle watch=ActionButtonStyle(Mathf.Max(8,Mathf.RoundToInt(12f*s)));watch.padding=new RectOffset(0,0,0,0);
            if(GUI.Button(new Rect(button.x+1,button.y+1,button.width-2,button.height-2),"WATCH AGAIN",watch))replay.WatchAgain();
        }
        PaintReplayRect(new Rect(housing.x+12f*s,housing.y+housing.height-6f*s,housing.width-24f*s,2f*s),new Color(.10f,.20f,.26f));
        PaintReplayRect(new Rect(housing.x+12f*s,housing.y+housing.height-6f*s,(housing.width-24f*s)*replay.Progress,2f*s),cyan);
    }

    private GUIStyle ReplayPrimaryStyle(int size)
    {
        GUIStyle style=ActionButtonStyle(size);
        style.normal.background=cyanTex;style.focused.background=cyanTex;
        style.normal.textColor=new Color(.02f,.06f,.09f);style.focused.textColor=style.normal.textColor;
        style.hover.textColor=new Color(.02f,.06f,.09f);style.active.textColor=new Color(.02f,.06f,.09f);
        return style;
    }

    private void DrawAutomaticDeathReplay(UXDeathReplay replay,string headline)
    {
        float s=Mathf.Min(1.3f,Mathf.Min(Screen.width/1000f,Screen.height/650f));
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),blackTex);
        GUI.Label(new Rect(16f*s,8f*s,Screen.width-32f*s,38f*s),"DEATH REPLAY / "+headline,
            CenterStyle(Mathf.Max(13,Mathf.RoundToInt(21f*s)),new Color(1f,.60f,.46f)));
        DrawSecurityReplay(replay,new Rect(18f*s,55f*s,Screen.width-36f*s,Screen.height-120f*s),s,false);
        GUI.Label(new Rect(20f*s,Screen.height-52f*s,Screen.width-240f*s,38f*s),"ENTER / SPACE / ESC: SKIP REPLAY",
            CenterStyle(Mathf.Max(10,Mathf.RoundToInt(14f*s)),Color.white));
        if(GUI.Button(new Rect(Screen.width-210f*s,Screen.height-52f*s,190f*s,38f*s),"SKIP REPLAY",ReplayPrimaryStyle(Mathf.Max(11,Mathf.RoundToInt(14f*s)))))replay.SkipAuto();
    }

    private void DrawDeathReplayResult(UXDeathReplay replay,UXStealthDirector director)
    {
        float s=Mathf.Min(1.30f,Mathf.Min(Screen.width/1160f,Screen.height/900f));
        Rect panel=new Rect((Screen.width-1080f*s)*.5f,(Screen.height-840f*s)*.5f,1080f*s,840f*s);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),blackTex);
        PaintReplayRect(panel,new Color(.025f,.037f,.055f));
        PaintReplayRect(new Rect(panel.x,panel.y,panel.width,3f*s),new Color(.95f,.32f,.25f));
        GUI.Label(new Rect(panel.x+20f*s,panel.y+19f*s,panel.width-40f*s,42f*s),director.DeathHeadline,
            CenterStyle(Mathf.Max(12,Mathf.RoundToInt(29f*s)),new Color(1f,.68f,.56f)));
        GUI.Label(new Rect(panel.x+28f*s,panel.y+66f*s,(panel.width-56f*s)*.45f,32f*s),
            "MISSION FAILED / "+director.Difficulty.Mode.ToString().ToUpperInvariant()+" / "+UXPlayerProgress.FormatTime(replay.DeathMissionSeconds),
            CenterStyle(Mathf.Max(8,Mathf.RoundToInt(14f*s)),new Color(.75f,.84f,.92f)));
        GUI.Label(new Rect(panel.x+panel.width*.45f,panel.y+66f*s,panel.width*.55f-28f*s,32f*s),
            "TIME OF DEATH  "+replay.DeathLocalTime.ToString("yyyy-MM-dd HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture)+" LOCAL",
            CenterStyle(Mathf.Max(8,Mathf.RoundToInt(13f*s)),new Color(.75f,.84f,.92f)));
        DrawSecurityReplay(replay,new Rect(panel.x+28f*s,panel.y+108f*s,panel.width-56f*s,510f*s),s,true);
        // A separate opaque action deck visibly sits BELOW the camera housing.
        Rect actions=new Rect(panel.x+28f*s,panel.y+642f*s,panel.width-56f*s,145f*s);
        PaintReplayRect(actions,new Color(.045f,.085f,.12f));
        PaintReplayRect(new Rect(actions.x,actions.y,actions.width,2f*s),new Color(.25f,.72f,.88f));
        GUI.Label(new Rect(actions.x+16f*s,actions.y+8f*s,actions.width-32f*s,32f*s),"CHOOSE YOUR NEXT ACTION",
            CenterStyle(Mathf.Max(10,Mathf.RoundToInt(14f*s)),new Color(.48f,.88f,1f)));
        Rect retry=new Rect(actions.x+24f*s,actions.y+41f*s,(actions.width-72f*s)*.5f,59f*s);
        Rect menu=new Rect(retry.x+retry.width+24f*s,retry.y,retry.width,retry.height);
        if(GUI.Button(retry,"REPLAY",ReplayPrimaryStyle(Mathf.Max(13,Mathf.RoundToInt(24f*s)))))ContinueMission();
        Color previous=GUI.color;GUI.color=new Color(.5f,.70f,.82f);DrawRectBorder(menu,Texture2D.whiteTexture,Mathf.Max(1,Mathf.RoundToInt(s)));GUI.color=previous;
        if(GUI.Button(new Rect(menu.x+2f*s,menu.y+2f*s,menu.width-4f*s,menu.height-4f*s),"MAIN MENU",ActionButtonStyle(Mathf.Max(13,Mathf.RoundToInt(24f*s)))))ReturnToMainMenu();
        GUI.Label(new Rect(retry.x,actions.y+108f*s,retry.width,28f*s),"RESUME SAVED CHECKPOINT",CenterStyle(Mathf.Max(8,Mathf.RoundToInt(13f*s)),new Color(.72f,.86f,.92f)));
        GUI.Label(new Rect(menu.x,actions.y+108f*s,menu.width,28f*s),"RETURN TO MISSION MENU",CenterStyle(Mathf.Max(8,Mathf.RoundToInt(13f*s)),new Color(.72f,.86f,.92f)));
        GUI.Label(new Rect(panel.x+20f*s,panel.y+804f*s,panel.width-40f*s,28f*s),"ENTER / SPACE: REPLAY    |    ESC: MAIN MENU",
            CenterStyle(Mathf.Max(9,Mathf.RoundToInt(12f*s)),new Color(.60f,.72f,.82f)));
    }

    private void StartMission()
    {
        ConfigureMissionDifficulty();
        briefingVisible = false;
        helpVisible = false;
        toastMessage = "";
        Time.timeScale = 1f;
        ShowToast(UXCampaign.IsCrimson?"CRIMSON DEPTHS - SECURE THE DOCKS":ExpandedFacility ? "FIND THE GOLD KEYCARD - TAB OPENS THE MAP" :
            "COLLECT EVIDENCE - AVOID SECURITY", cyanTex, 3f);

        //Records when the player starts the mission
        GameplayMetrics.Instance?.StartSession();
        if(ExpandedFacility)UXCampaign.BindTelemetrySession();
    }

    private void ConfigureMissionDifficulty()
    {
        if (!ExpandedFacility || UXStealthDirector.Instance == null) return;
        UXStealthDirector.Instance.BeginMission(UXPlayerProgress.Data.difficulty);
        GameplayMetrics.SelectedDifficulty = UXStealthDirector.Instance.Difficulty.Mode.ToString();
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
        if(ExpandedFacility)
        {
            GameplayMetrics.Instance?.RecordRestart();GameplayMetrics.Instance?.EndSession("Restarted");
            UXCampaign.RestartLevel(UXCampaign.SceneLevelId,UXPlayerProgress.Data.difficulty);
            LoadCampaignScene(UXCampaign.ScenePath(UXCampaign.SceneLevelId),true);
            return;
        }
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
        if(ExpandedFacility)
        {
            UXCampaign.SaveClock();GameplayMetrics.Instance?.EndSession("Abandoned");
            LoadCampaignScene(UXCampaign.FacilityScene,false);return;
        }
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

    private void ContinueMission()
    {
        if(!ExpandedFacility){RetryMission();return;}
        LoadCampaignScene(UXCampaign.PrepareReplay(),true);
    }

    private void LoadCampaignScene(string scene,bool autoStart,bool newBriefing=false)
    {
        if(!Application.CanStreamedLevelBeLoaded(scene))
        {
            UXCampaign.CancelAdvance();
            campaignLoadError="Campaign unavailable in this build.";
            if(missionMenu!=null)missionMenu.ErrorNotice="CAMPAIGN UNAVAILABLE IN THIS BUILD";
            Debug.LogError("Campaign scene unavailable: "+scene+". Reopen Unity after importing the campaign update.");
            return;
        }
        startGameplayAfterReload=autoStart;showBriefingAfterReload=newBriefing;
        helpVisible=false;restartConfirmationVisible=false;mainMenuConfirmationVisible=false;
        Time.timeScale=1f;
        SceneManager.LoadScene(scene);
    }

    private void OnApplicationQuit(){if(ExpandedFacility)UXCampaign.SaveClock();}
    private void OnApplicationPause(bool paused){if(paused && ExpandedFacility)UXCampaign.SaveClock();}

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
