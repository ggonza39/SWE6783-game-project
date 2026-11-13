using System.Collections.Generic;
using UnityEngine;

// This component is serialized ONLY in UXFacilityExpanded. Team scenes retain their own rules.
[DefaultExecutionOrder(80)]
public sealed class UXStealthDirector : MonoBehaviour
{
    public static UXStealthDirector Instance { get; private set; }
    public static bool Owns(GameObject item) => Instance != null &&
        item != null && item.scene == Instance.gameObject.scene;
    public UXStealthState State { get; private set; } = new UXStealthState();
    public bool KeyAcquired { get; private set; }
    public Vector2 LastKnownPosition { get; private set; }
    public Vector2 KeyPosition { get; private set; }
    public Vector2 GatePosition { get; private set; }
    public string Prompt { get; private set; } = "FIND THE GOLD ACCESS KEYCARD";
    public float InteractionProgress { get; private set; }
    public Transform Player => player;
    public bool PlayerHidden => hiding != null && hiding.IsHidden;
    public string Objective => !KeyAcquired ? "FIND THE ACCESS KEYCARD" :
        (GameManager.Instance != null && GameManager.Instance.TerminalActivated ? "REACH EXTRACTION" :
        (EvidenceManager.Instance != null && EvidenceManager.Instance.HasEvidence(3) ?
        "HACK SECURITY - HOLD E" : "DOWNLOAD 3 DATA CACHES - HOLD E"));
    public string PhaseLabel => State.Phase == UXAlertPhase.Engaged ? "RED ALERT - BREAK SIGHT" :
        State.Phase == UXAlertPhase.Searching ? "YELLOW - SEARCHING" :
        State.Phase == UXAlertPhase.Suspicious ? "SUSPICION" : "SECURITY: PATROLLING";

    private const int GridWidth = 55, GridHeight = 31;
    private const float GridLeft = -13.5f, GridBottom = -7.5f, GridStep = 0.5f;
    private UXGridPathfinder navigation;
    private readonly List<Bounds> navigationWalls = new List<Bounds>();
    private Transform player;
    private PlayerHiding hiding;
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

    private void Awake()
    {
        Instance = this;
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        player = controller != null ? controller.transform : null;
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
        foreach (GuardDetection guard in guards)
            if (guard.GetComponent<UXGuardResponse>() == null) guard.gameObject.AddComponent<UXGuardResponse>();
        Physics2D.SyncTransforms();
        RebuildNavigation();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || player == null || GameManager.Instance == null ||
            GameManager.Instance.GameEnded) return;
        bool guardSees = false, cameraSees = false;
        foreach (GuardDetection guard in guards) guardSees |= guard != null && guard.IsPlayerDetected;
        foreach (SecurityCamera camera in cameras) cameraSees |= camera != null && camera.IsPlayerDetected;
        State.Tick(Time.deltaTime, guardSees, cameraSees, PlayerHidden);
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
        if (!KeyAcquired && key != null &&
            Vector2.Distance(player.position, key.transform.position) < 0.85f &&
            DetectionUtility.HasLineOfSight(player.position, key.transform.position, player, sightHits, key.transform))
        {
            KeyAcquired = true;
            key.SetActive(false);
            if (gate != null) gate.enabled = false;
            if (gateArt != null) gateArt.enabled = false;
            RebuildNavigation();
            Notice("ACCESS KEY SECURED - CYAN GATE IS OPEN", 5f);
        }

        Object target = null;
        float duration = 2.5f;
        Prompt = !KeyAcquired ? "GOLD KEYCARD OPENS THE RED SECURITY GATE" :
            "CROUCH: WASD   SLOW: C/CTRL   SPRINT: SHIFT   DOWNLOAD: HOLD E   MAP: TAB";
        float closest = 1.15f;
        foreach (EvidenceCollectible item in evidence)
        {
            if (item == null) continue;
            float distance = Vector2.Distance(player.position, item.transform.position);
            if (distance >= closest || !DetectionUtility.HasLineOfSight(player.position,
                    item.transform.position, player, sightHits, item.transform)) continue;
            closest = distance; target = item;
            Prompt = "HOLD E - DOWNLOAD EVIDENCE (2.5s)";
        }
        if (terminal != null && terminal.PlayerNearby && !terminal.IsActivated)
        {
            if (EvidenceManager.Instance != null && EvidenceManager.Instance.HasEvidence(3))
            {
                target = terminal; duration = 4f;
                Prompt = "HOLD E - HACK SECURITY (4s)";
            }
            else Prompt = "TERMINAL LOCKED - DOWNLOAD ALL 3 CACHES FIRST";
        }
        if (target != interacting) { interacting = target; InteractionProgress = 0f; }
        bool holding = target != null && Input.GetKey(KeyCode.E);
        if (!holding) InteractionProgress = 0f;
        else
        {
            InteractionProgress += Time.deltaTime / duration;
            // Staying still while downloading is intentional; moving away cancels it.
            if (InteractionProgress >= 1f)
            {
                if (target is EvidenceCollectible cache)
                {
                    cache.Collect();
                    Notice("DATA SECURED - FIND THE NEXT OBJECTIVE", 3f);
                }
                else if (target is TerminalInteraction security)
                {
                    security.ActivateFromDownload();
                    if (security.IsActivated) Notice("CAMERAS OFFLINE - REACH EXTRACTION", 5f);
                }
                interacting = null; InteractionProgress = 0f;
            }
        }
        if (target == null && noticeUntil > Time.time) Prompt = notice;
        if (PlayerHidden && target == null && noticeUntil <= Time.time)
            Prompt = State.Responding ? "HIDDEN - WAIT FOR YELLOW SEARCH TO CLEAR" : "HIDDEN - WATCH THE PATROL BEFORE MOVING";
    }

    public void Hit()
    {
        if (PlayerHidden || GameManager.Instance == null || GameManager.Instance.GameEnded ||
            !State.TryDamage(25)) return;
        Notice("HIT! BREAK SIGHT AND REACH MARKED COVER", 2f);
        if (!State.Alive) GameManager.Instance.LoseGame();
    }

    private void Notice(string text, float seconds) { notice = text; noticeUntil = Time.time + seconds; }

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
        bool[] blocked = new bool[GridWidth * GridHeight];
        for (int i = 0; i < blocked.Length; i++)
        {
            Vector2 at = CellPosition(i);
            foreach (Bounds wall in navigationWalls)
                if (at.x >= wall.min.x && at.x <= wall.max.x && at.y >= wall.min.y && at.y <= wall.max.y)
                { blocked[i] = true; break; }
        }
        navigation = new UXGridPathfinder(GridWidth, GridHeight, blocked);
    }

    private static Vector2 CellPosition(int cell) => new Vector2(
        GridLeft + cell % GridWidth * GridStep, GridBottom + cell / GridWidth * GridStep);

    public List<Vector2> Path(Vector2 from, Vector2 to)
    {
        var result = new List<Vector2>();
        if (navigation == null) return result;
        int start = navigation.NearestOpen(Mathf.RoundToInt((from.x - GridLeft) / GridStep),
            Mathf.RoundToInt((from.y - GridBottom) / GridStep));
        int goal = navigation.NearestOpen(Mathf.RoundToInt((to.x - GridLeft) / GridStep),
            Mathf.RoundToInt((to.y - GridBottom) / GridStep));
        foreach (int cell in navigation.Find(start, goal)) result.Add(CellPosition(cell));
        // Avoid walking backward to the nearest grid center on every repath.
        // Only skip it when the whole swept body can reach the next center.
        if (result.Count > 1 && ClearNavigationSegment(from, result[1])) result.RemoveAt(0);
        return result;
    }

    public bool ClearNavigationSegment(Vector2 from, Vector2 to)
    {
        foreach (Bounds wall in navigationWalls)
        {
            float enter = 0f, leave = 1f;
            Vector2 delta = to - from;
            bool intersects = true;
            for (int axis = 0; axis < 2; axis++)
            {
                float start = axis == 0 ? from.x : from.y;
                float direction = axis == 0 ? delta.x : delta.y;
                float low = axis == 0 ? wall.min.x : wall.min.y;
                float high = axis == 0 ? wall.max.x : wall.max.y;
                if (Mathf.Abs(direction) < 0.00001f)
                {
                    if (start < low || start > high) { intersects = false; break; }
                }
                else
                {
                    float first = (low - start) / direction, last = (high - start) / direction;
                    enter = Mathf.Max(enter, Mathf.Min(first, last));
                    leave = Mathf.Min(leave, Mathf.Max(first, last));
                    if (enter > leave) { intersects = false; break; }
                }
            }
            if (intersects) return false;
        }
        return true;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
