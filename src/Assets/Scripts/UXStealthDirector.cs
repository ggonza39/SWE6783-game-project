using System.Collections.Generic;
using UnityEngine;

// Shared by the two authored campaign scenes. Other team scenes retain their own rules.
[DefaultExecutionOrder(80)]
public sealed class UXStealthDirector : MonoBehaviour
{
    public static UXStealthDirector Instance { get; private set; }
    public static bool Owns(GameObject item) => Instance != null &&
        item != null && item.scene == Instance.gameObject.scene;
    public UXStealthState State { get; private set; } = new UXStealthState();
    public UXDifficultyProfile Difficulty { get; private set; } = UXDifficultyProfile.For(UXDifficulty.Easy);
    public bool MissionStarted { get; private set; }
    public UXSectorMission Sectors { get; private set; }
    public bool IsCrimson => Sectors != null;
    public UXCrimsonHazards Hazards { get; private set; }
    public UXMissionEffects Effects { get; private set; }
    public string LastDamageCause { get; private set; }
    public string DeathHeadline => LastDamageCause == "PROXIMITY MINE" ? "KILLED BY MINE" :
        LastDamageCause == "LASER BARRIER" ? "FRIED BY LASER" :
        LastDamageCause == "GUARD FIRE" ? "GUNNED DOWN BY GUARDS" : "MISSION FAILED";
    public string LevelId => IsCrimson ? UXCampaign.CrimsonLevelId : UXPlayerProgress.FacilityLevelId;
    public float ExitDoorSeconds => IsCrimson && Difficulty.Mode == UXDifficulty.Easy ? 4f : Difficulty.ExitDoorDuration;
    public UXAccessLock[] AccessLocks { get; private set; } = new UXAccessLock[0];
    public BoxCollider2D SealedAlternativeEntrance { get; private set; }
    public int ReleasedLocks
    {
        get { int count = 0; foreach (UXAccessLock access in AccessLocks) if (access.IsUnlocked) count++; return count; }
    }
    public bool ExtractionLocksReleased => ReleasedLocks == AccessLocks.Length;
    public bool KeyAcquired { get; private set; }
    public Vector2 LastKnownPosition { get; private set; }
    public Vector2 KeyPosition { get; private set; }
    public Vector2 GatePosition { get; private set; }
    public string Prompt { get; private set; } = "FIND THE GOLD ACCESS KEYCARD";
    public float InteractionProgress { get; private set; }
    // Read-only presentation state; the director remains the interaction authority.
    public bool InteractionAvailable => interacting != null;
    public bool HackingSecurity => interacting is TerminalInteraction;
    public string InteractionAction => interacting is UXAccessLock ? "OVERRIDING EXTRACTION DOOR" :
        (HackingSecurity ? "HACKING SECURITY" : "DOWNLOADING EVIDENCE");
    public string InteractionInstruction => interacting is UXAccessLock ? "HOLD E TO OVERRIDE EXTRACTION DOOR (" + ExitDoorSeconds.ToString("0") + "s)" :
        (HackingSecurity ? "HOLD E TO HACK SECURITY (" + (4f * Difficulty.InteractionDuration).ToString("0.##") + "s)" :
        "HOLD E TO DOWNLOAD EVIDENCE (" + (2.5f * Difficulty.InteractionDuration).ToString("0.##") + "s)");
    public Transform Player => player;
    public bool PlayerHidden => hiding != null && hiding.IsHidden;
    public string Objective => IsCrimson ? Sectors.Objective : (!KeyAcquired ? "FIND THE ACCESS KEYCARD" :
        (GameManager.Instance != null && GameManager.Instance.TerminalActivated ?
        (ExtractionLocksReleased ? (Difficulty.Mode == UXDifficulty.Hard ?
            "GO THROUGH THE OPEN EXIT DOOR" : "REACH EXTRACTION") : "OVERRIDE THE EXTRACTION DOOR - HOLD E 8s") :
        (EvidenceManager.Instance != null && EvidenceManager.Instance.HasEvidence(3) ?
        "HACK SECURITY - HOLD E" : "DOWNLOAD 3 DATA CACHES - HOLD E")));
    public string PhaseLabel => State.Phase == UXAlertPhase.Engaged ? "RED ALERT - BREAK SIGHT" :
        State.Phase == UXAlertPhase.Searching ? "YELLOW - SEARCHING" :
        State.Phase == UXAlertPhase.Suspicious ? "SUSPICION" : "SECURITY: PATROLLING";

    private UXMissionDefinition definition;
    private float NavHalfWidth=>definition!=null?definition.halfWidth:14.5f;
    private float NavHalfHeight=>definition!=null?definition.halfHeight:8.5f;
    private int GridWidth => Mathf.RoundToInt((NavHalfWidth-1f)*4f)+1;
    private int GridHeight => Mathf.RoundToInt((NavHalfHeight-1f)*4f)+1;
    private float GridLeft => 1f-NavHalfWidth;
    private float GridBottom => 1f-NavHalfHeight;
    private const float GridStep = 0.5f;
    private UXGridPathfinder navigation;
    private UXGridPathfinder occupiedNavigation;
    private bool[] navigationBlocked;
    private bool[] occupiedBlocked;
    private readonly List<Bounds> navigationWalls = new List<Bounds>();
    private Transform player;
    private BoxCollider2D playerBody;
    private PlayerHiding hiding;
    private UXSolidHidingCover[] hidingBays;
    private GuardDetection[] guards;
    private SecurityCamera[] cameras;
    private EvidenceCollectible[] evidence;
    private TerminalInteraction terminal;
    private GameObject key;
    private BoxCollider2D gate;
    private SpriteRenderer gateArt;
    private Object interacting;
    private bool guardReported, cameraReported;
    private string notice;
    private float noticeUntil;
    private readonly RaycastHit2D[] sightHits = new RaycastHit2D[16];
    private readonly UXStealthState[] sectorAlerts = new UXStealthState[3];
    private readonly Vector2[] sectorLastKnown = new Vector2[3];
    private readonly bool[] sectorGuardSeen = new bool[3], sectorCameraSeen = new bool[3];

    public void BeginMission(UXDifficulty mode)
    {
        if (MissionStarted) return;
        Difficulty = UXDifficultyProfile.For(mode);
        State = new UXStealthState(Difficulty.AlertGrowth, Difficulty.SearchPatience);
        for (int i=0;i<3;i++) sectorAlerts[i]=new UXStealthState(Difficulty.AlertGrowth,Difficulty.SearchPatience);
        MissionStarted = true;
        LastDamageCause = null;
        int checkpoint = UXCampaign.Begin(LevelId,mode);
        if (IsCrimson)
        {
            Sectors.Restore(checkpoint); Hazards.Begin(mode,checkpoint); RefreshNavigation();
        }
        else if (Difficulty.Mode == UXDifficulty.Hard)
        {
            AccessLocks = new[] {
                UXAccessLock.Create(transform, "EXIT", new Vector2(8f, -4.5f),
                    new Vector2(0.72f, 2.02f), new Vector2(7f, -4.5f))
            };
            SealedAlternativeEntrance = AccessLocks[0].SealAlternativeEntrance(
                new Vector2(11.5f, -2f), new Vector2(2.02f, 0.98f));
            Physics2D.SyncTransforms();
            RebuildNavigation();
        }
        Effects?.Begin();
        UXGameAudio.Unlock();UXGameAudio.Play(UXAudioCue.Deploy);
    }

    private void Awake()
    {
        Instance = this;
        definition=GetComponent<UXMissionDefinition>();
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        player = controller != null ? controller.transform : null;
        playerBody = controller != null ? controller.GetComponent<BoxCollider2D>() : null;
        hiding = controller != null ? controller.GetComponent<PlayerHiding>() : null;
        guards = FindObjectsByType<GuardDetection>(FindObjectsSortMode.None);
        cameras = FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);
        evidence = FindObjectsByType<EvidenceCollectible>(FindObjectsSortMode.None);
        terminal = FindFirstObjectByType<TerminalInteraction>();
        key = GameObject.Find("AccessKeycard");
        GameObject door = GameObject.Find("AccessGate");
        if (key != null) KeyPosition = key.transform.position;
        if (door != null)
        {
            gate = door.GetComponent<BoxCollider2D>();
            gateArt = door.GetComponent<SpriteRenderer>();
            GatePosition = door.transform.position;
        }
        Sectors=GetComponent<UXSectorMission>();
        if(IsCrimson)
        {
            KeyAcquired=true;Sectors.Initialize(this);AccessLocks=new[]{Sectors.ExitDoor};
            for(int i=0;i<3;i++)sectorAlerts[i]=new UXStealthState();
        }
        UXSolidHidingCover.ConfigureScene(gameObject, playerBody);
        hidingBays = FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None);
        if (hiding != null) hiding.ConfigureBays(hidingBays);
        foreach (GuardDetection guard in guards)
        {
            if (guard.GetComponent<UXGuardResponse>() == null) guard.gameObject.AddComponent<UXGuardResponse>();
            BoxCollider2D guardBody = guard.GetComponent<BoxCollider2D>();
            // Guard movement maintains personal space below. The contact solver must not
            // transfer a moving guard's motion into the player's dynamic rigidbody.
            if (guardBody != null && playerBody != null)
                Physics2D.IgnoreCollision(guardBody, playerBody, true);
        }
        Physics2D.SyncTransforms();
        RebuildNavigation();
        if(IsCrimson)Hazards=UXCrimsonHazards.Create(this);
        Effects=UXMissionEffects.Create(this);
        UXGameAudio.Attach(this);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || player == null || GameManager.Instance == null ||
            GameManager.Instance.GameEnded) return;
        bool guardSees = false, cameraSees = false;
        System.Array.Clear(sectorGuardSeen,0,3);System.Array.Clear(sectorCameraSeen,0,3);
        foreach (GuardDetection guard in guards) if(guard != null && guard.IsPlayerDetected)
        { guardSees=true;if(IsCrimson)sectorGuardSeen[UXSectorMission.Membership(guard.transform)]=true; }
        foreach (SecurityCamera camera in cameras) if(camera != null && camera.IsPlayerDetected)
        { cameraSees=true;if(IsCrimson)sectorCameraSeen[UXSectorMission.Membership(camera.transform)]=true; }
        if(IsCrimson)for(int i=0;i<3;i++)
        {
            sectorAlerts[i].Tick(Time.deltaTime,sectorGuardSeen[i],sectorCameraSeen[i],PlayerHidden);
            if(!PlayerHidden && (sectorGuardSeen[i]||sectorCameraSeen[i]))sectorLastKnown[i]=player.position;
        }
        State.Tick(Time.deltaTime, guardSees, cameraSees, PlayerHidden);
        if(MissionStarted)UXCampaign.Tick(Time.deltaTime);
        if (!PlayerHidden && (guardSees || cameraSees)) LastKnownPosition = player.position;
        if (State.Responding)
        {
            if (guardSees && !guardReported) { GameplayMetrics.Instance?.RecordGuardDetection(); guardReported = true; }
            if (cameraSees && !cameraReported) { GameplayMetrics.Instance?.RecordCameraDetection(); cameraReported = true; }
        }
        if (State.Phase == UXAlertPhase.Clear) { guardReported = false; cameraReported = false; }
        UpdateInteraction();
    }

    private void UpdateInteraction()
    {
        if(IsCrimson)
        {
            float hold;string text;
            Object candidate=Sectors.SelectTarget(out hold,out text);Prompt=text;
            AdvanceInteraction(candidate,hold);
            if(candidate==null && noticeUntil>Time.time)Prompt=notice;
            return;
        }
        if (!KeyAcquired && key != null &&
            Vector2.Distance(player.position, key.transform.position) < 0.85f &&
            DetectionUtility.HasLineOfSight(player.position, key.transform.position, player, sightHits, key.transform))
        {
            KeyAcquired = true;
            UXGameAudio.Play(UXAudioCue.Keycard);
            key.SetActive(false);
            if (gate != null) gate.enabled = false;
            if (gateArt != null) gateArt.enabled = false;
            RebuildNavigation();
            Notice("ACCESS KEY SECURED - CYAN GATE IS OPEN", 5f);
        }

        Object target = null;
        float duration = 2.5f * Difficulty.InteractionDuration;
        Prompt = !KeyAcquired ? "GOLD KEYCARD OPENS THE RED SECURITY GATE" :
            (UXPlayerProgress.Data.showControlHints ?
            "CROUCH: WASD   SLOW: C/CTRL   SPRINT: SHIFT   DOWNLOAD: HOLD E   MAP: TAB" :
            "FOLLOW THE MARKED MISSION OBJECTIVES");
        float closest = 1.15f;
        foreach (EvidenceCollectible item in evidence)
        {
            if (item == null) continue;
            float distance = Vector2.Distance(player.position, item.transform.position);
            if (distance >= closest || !DetectionUtility.HasLineOfSight(player.position,
                    item.transform.position, player, sightHits, item.transform)) continue;
            closest = distance; target = item;
            Prompt = "HOLD E - DOWNLOAD EVIDENCE";
        }
        if (terminal != null && terminal.PlayerNearby && !terminal.IsActivated)
        {
            if (EvidenceManager.Instance != null && EvidenceManager.Instance.HasEvidence(3))
            {
                target = terminal; duration = 4f * Difficulty.InteractionDuration;
                Prompt = "HOLD E - HACK SECURITY";
            }
            else Prompt = "TERMINAL LOCKED - DOWNLOAD ALL 3 CACHES FIRST";
        }
        foreach (UXAccessLock access in AccessLocks)
        {
            if (access.IsUnlocked || Vector2.Distance(player.position, access.transform.position) >= 1.15f ||
                !DetectionUtility.HasLineOfSight(player.position, access.transform.position, player, sightHits, access.transform)) continue;
            if (!KeyAcquired) { Prompt = "ACCESS LOCK - FIND THE GOLD KEYCARD FIRST"; continue; }
            if (GameManager.Instance == null || !GameManager.Instance.TerminalActivated ||
                !GameManager.Instance.HasRequiredEvidence(3))
            { Prompt = "EXIT DOOR LOCKED - DOWNLOAD ALL DATA AND HACK SECURITY FIRST"; continue; }
            target = access; duration = Difficulty.ExitDoorDuration;
            Prompt = "HOLD E - OVERRIDE EXTRACTION DOOR (8s)";
            break;
        }
        AdvanceInteraction(target,duration);
        if (target == null && noticeUntil > Time.time) Prompt = notice;
        if (target == null && GameManager.Instance != null && GameManager.Instance.TerminalActivated &&
            !ExtractionLocksReleased && noticeUntil <= Time.time)
            Prompt = "EXIT DOOR LOCKED - HOLD E AT ITS CONTROL FOR 8s (TAB MAP)";
        if (PlayerHidden && target == null && noticeUntil <= Time.time)
            Prompt = State.Responding ? "HIDDEN - WAIT FOR YELLOW SEARCH TO CLEAR" : "HIDDEN - WATCH THE PATROL BEFORE MOVING";
        else if (!PlayerHidden && target == null && noticeUntil <= Time.time && hidingBays != null)
            foreach (UXSolidHidingCover bay in hidingBays)
                if (bay != null && Vector2.Distance(player.position, bay.transform.position) < 1.8f)
                { Prompt = "WALK INTO THE OPEN HIDING BAY - GREEN HIDDEN CONFIRMS COVER"; break; }
    }

    private void AdvanceInteraction(Object target,float duration)
    {
        if (target != interacting) { interacting = target; InteractionProgress = 0f; }
        bool holding = target != null && Input.GetKey(KeyCode.E);
        if (!holding) InteractionProgress = 0f;
        else
        {
            InteractionProgress += Time.deltaTime / duration;
            // Staying still while downloading is intentional; moving away cancels it.
            if (InteractionProgress >= 1f)
            {
                if(IsCrimson)Sectors.Complete(target);
                else if (target is EvidenceCollectible cache)
                {
                    cache.Collect();
                    Notice("DATA SECURED - FIND THE NEXT OBJECTIVE", 3f);
                }
                else if (target is TerminalInteraction security)
                {
                    security.ActivateFromDownload();
                    if (security.IsActivated) Notice(ExtractionLocksReleased ? "CAMERAS OFFLINE - REACH EXTRACTION" :
                        "CAMERAS OFFLINE - OVERRIDE THE MARKED EXTRACTION DOOR", 5f);
                }
                else if (target is UXAccessLock access)
                {
                    access.Unlock(); RebuildNavigation();
                    Notice("EXTRACTION DOOR OPEN - USE THE WEST SERVICE ENTRANCE", 4f);
                }
                interacting = null; InteractionProgress = 0f;
            }
        }
    }

    public void Hit()
    {
        if (PlayerHidden || GameManager.Instance == null || GameManager.Instance.GameEnded ||
            !State.TryDamage(Difficulty.HitDamage)) return;
        LastDamageCause="GUARD FIRE";
        if(State.Alive)UXGameAudio.Play(UXAudioCue.Hit);
        Notice("HIT! BREAK SIGHT AND REACH MARKED COVER", 2f);
        if (!State.Alive) FinishFatalHit();
    }

    public bool HazardHit(int amount,string cause,bool lethal=false)
    {
        if(!IsCrimson || !MissionStarted || PlayerHidden || GameManager.Instance==null || GameManager.Instance.GameEnded ||
            !(lethal ? State.TryLethalHazard() : State.TryHazardDamage(amount)))return false;
        LastDamageCause=cause;Notice(cause+" HIT - GET CLEAR!",2f);
        if(cause=="LASER BARRIER")UXGameAudio.Play(UXAudioCue.LaserBurn);
        else if(State.Alive)UXGameAudio.Play(UXAudioCue.Hit);
        if(!State.Alive)FinishFatalHit();
        return true;
    }

    private void FinishFatalHit()
    {
        Effects?.ShowFatalHit(LastDamageCause);
        GameManager.Instance.LoseGame();
    }

    public void InvestigateNoise(Vector2 at)
    {
        if(!IsCrimson || !MissionStarted)return;
        int sector=UXSectorMission.IndexOf(at);
        if(sectorAlerts[sector].Phase!=UXAlertPhase.Engaged)sectorLastKnown[sector]=at;
        sectorAlerts[sector].InvestigateNoise();
        if(State.Phase!=UXAlertPhase.Engaged)LastKnownPosition=at;
        State.InvestigateNoise();
    }

    public void Notice(string text, float seconds) { notice = text; noticeUntil = Time.time + seconds; }
    public UXStealthState AlertFor(Transform guard) => IsCrimson ? sectorAlerts[UXSectorMission.Membership(guard)] : State;
    public Vector2 LastKnownFor(Transform guard) => IsCrimson ? sectorLastKnown[UXSectorMission.Membership(guard)] : LastKnownPosition;
    public bool GuardResponds(Transform guard) => AlertFor(guard).Responding;
    public void RefreshNavigation() { Physics2D.SyncTransforms(); RebuildNavigation(); }

    private void RebuildNavigation()
    {
        navigationWalls.Clear();
        foreach (BoxCollider2D collider in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (!collider.enabled || collider.isTrigger ||
                collider.GetComponentInParent<PlayerController>() != null ||
                collider.GetComponentInParent<GuardController>() != null) continue;
            Bounds box = collider.bounds;
            box.Expand(new Vector3(0.56f, 0.76f, 0f));
            navigationWalls.Add(box);
        }
        navigationBlocked = new bool[GridWidth * GridHeight];
        for (int i = 0; i < navigationBlocked.Length; i++)
        {
            Vector2 at = CellPosition(i);
            foreach (Bounds wall in navigationWalls)
                if (at.x >= wall.min.x && at.x <= wall.max.x && at.y >= wall.min.y && at.y <= wall.max.y)
                { navigationBlocked[i] = true; break; }
        }
        navigation = new UXGridPathfinder(GridWidth, GridHeight, navigationBlocked);
        occupiedBlocked=new bool[navigationBlocked.Length];
        occupiedNavigation=new UXGridPathfinder(GridWidth,GridHeight,occupiedBlocked);
    }

    private Vector2 CellPosition(int cell) => new Vector2(
        GridLeft + cell % GridWidth * GridStep, GridBottom + cell / GridWidth * GridStep);

    public List<Vector2> Path(Vector2 from, Vector2 to,int sector=-1)
    {
        var result = new List<Vector2>();
        if (navigation == null) return result;
        UXGridPathfinder grid = navigation;
        bool[] blocked = navigationBlocked;
        if ((playerBody != null && playerBody.enabled) || sector>=0)
        {
            blocked = occupiedBlocked;System.Array.Copy(navigationBlocked,blocked,blocked.Length);
            Bounds occupied = playerBody!=null?PlayerSpace():new Bounds();
            for (int i = 0; i < blocked.Length; i++)
                if ((playerBody!=null && playerBody.enabled && Contains(occupied, CellPosition(i))) ||
                    (sector>=0 && !UXSectorMission.InSector(sector,CellPosition(i)))) blocked[i] = true;
            grid = occupiedNavigation;
        }
        int start = grid.NearestOpen(Mathf.RoundToInt((from.x - GridLeft) / GridStep),
            Mathf.RoundToInt((from.y - GridBottom) / GridStep));
        // Off-grid patrol anchors must connect to the first cell without crossing a crate.
        if (start >= 0 && !CanGuardTravel(from, CellPosition(start)))
        {
            start = -1;
            float closest = float.PositiveInfinity;
            for (int i = 0; i < blocked.Length; i++)
            {
                float distance = (CellPosition(i) - from).sqrMagnitude;
                if (!blocked[i] && distance < closest && CanGuardTravel(from, CellPosition(i)))
                { start = i; closest = distance; }
            }
        }
        int goal = grid.NearestOpen(Mathf.RoundToInt((to.x - GridLeft) / GridStep),
            Mathf.RoundToInt((to.y - GridBottom) / GridStep));
        foreach (int cell in grid.Find(start, goal)) result.Add(CellPosition(cell));
        // Avoid walking backward to the nearest grid center on every repath.
        // Only skip it when the whole swept body can reach the next center.
        if (result.Count > 1 && CanGuardTravel(from, result[1])) result.RemoveAt(0);
        return result;
    }

    public bool ClearNavigationSegment(Vector2 from, Vector2 to)
    {
        foreach (Bounds wall in navigationWalls)
            if (Intersects(wall, from, to)) return false;
        return true;
    }

    public Vector2 ClampBlastTravel(Vector2 from,Vector2 delta)
    {
        if(ClearNavigationSegment(from,from+delta))return from+delta;
        float lo=0f,hi=1f;
        for(int i=0;i<12;i++)
        {
            float t=(lo+hi)*.5f;
            if(ClearNavigationSegment(from,from+delta*t))lo=t;else hi=t;
        }
        return from+delta*Mathf.Max(0f,lo-.002f);
    }

    public bool CanGuardTravel(Vector2 from, Vector2 to)
    {
        if (!ClearNavigationSegment(from, to)) return false;
        if (playerBody == null || !playerBody.enabled) return true;
        Bounds occupied = PlayerSpace();
        if (!Intersects(occupied, from, to)) return true;
        // If the player approaches a stationary guard, allow the guard to step away.
        Vector2 away = from - (Vector2)occupied.center, step = to - from;
        return Contains(occupied, from) && step.sqrMagnitude > 0.0000001f &&
            away.x * step.x + away.y * step.y >= 0f;
    }

    public bool TryMoveGuard(Transform guard, Vector2 destination)
    {
        if(IsCrimson && !UXSectorMission.InSector(UXSectorMission.Membership(guard),destination))return false;
        if (!CanGuardTravel(guard.position, destination)) return false;
        guard.position = new Vector3(destination.x, destination.y, guard.position.z);
        return true;
    }

    private Bounds PlayerSpace()
    {
        Bounds box = playerBody.bounds;
        box.Expand(new Vector3(0.62f, 0.82f, 0f));
        return box;
    }

    private static bool Contains(Bounds box, Vector2 at) => at.x >= box.min.x && at.x <= box.max.x &&
        at.y >= box.min.y && at.y <= box.max.y;

    private static bool Intersects(Bounds box, Vector2 from, Vector2 to)
    {
        float enter = 0f, leave = 1f;
        Vector2 delta = to - from;
        for (int axis = 0; axis < 2; axis++)
        {
            float start = axis == 0 ? from.x : from.y;
            float direction = axis == 0 ? delta.x : delta.y;
            float low = axis == 0 ? box.min.x : box.min.y;
            float high = axis == 0 ? box.max.x : box.max.y;
            if (Mathf.Abs(direction) < 0.00001f)
            {
                if (start < low || start > high) return false;
            }
            else
            {
                float first = (low - start) / direction, last = (high - start) / direction;
                enter = Mathf.Max(enter, Mathf.Min(first, last));
                leave = Mathf.Min(leave, Mathf.Max(first, last));
                if (enter > leave) return false;
            }
        }
        return true;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
