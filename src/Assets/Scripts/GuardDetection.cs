using UnityEngine;

public class GuardDetection : MonoBehaviour, IConeDetector
{
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float detectionAngle = 60f;

    /// <summary>True on any frame the player is currently visible to this guard.</summary>
    public bool IsPlayerDetected { get; private set; }

    private Transform player;
    private PlayerHiding playerHiding;
    private GuardController guardController;
    private readonly RaycastHit2D[] losHitBuffer = new RaycastHit2D[8];

    // IConeDetector - drives the runtime-visible cone (DetectionConeRenderer)
    // with the exact same values and forward direction used for detection.
    public Vector2 ConeOrigin => transform.position;
    public Vector2 ConeForward => guardController != null && guardController.enabled
        ? guardController.FacingDirection
        : (Vector2)transform.right;
    public float ConeRange => detectionRange;
    public float ConeFovAngle => detectionAngle;
    public Color ConeColor => IsPlayerDetected ? new Color(1f, 0f, 0f, 0.35f) : new Color(1f, 0.5f, 0f, 0.25f);
    public bool ConeActive => GameManager.Instance == null || !GameManager.Instance.GameEnded;

    private void Start()
    {
        guardController = GetComponent<GuardController>();

        DetectionConeRenderer coneRenderer = GetComponent<DetectionConeRenderer>();
        if (coneRenderer == null)
            coneRenderer = gameObject.AddComponent<DetectionConeRenderer>();
        coneRenderer.Detector = this;

        // Safely attempt to find the player by tag
        GameObject playerObject = null;
        try 
        {
            playerObject = GameObject.FindWithTag("Player");
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Player tag not found. Falling back to name lookup.");
        }

        // Fallback to name if tag lookup fails or player is untagged
        if (playerObject == null)
            playerObject = GameObject.Find("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHiding = playerObject.GetComponent<PlayerHiding>();
        }
        else
        {
            Debug.LogWarning("GuardDetection: Player GameObject not found in scene.");
        }
    }

    private void Update()
    {
        IsPlayerDetected = false;

        // Detection only proceeds when GameManager exists and the game has not ended
        if (Time.timeScale <= 0f || GameManager.Instance == null || GameManager.Instance.GameEnded)
            return;

        if (player == null)
            return;

        // Respect the player's hiding state
        if (playerHiding != null && playerHiding.IsHidden)
            return;

        // Face the direction the guard is actually walking so the detection
        // cone always matches its visible facing, even while patrolling left.
        Vector2 forward = ConeForward;

        bool withinCone = DetectionUtility.IsWithinFieldOfView(
            transform.position,
            forward,
            player.position,
            detectionRange,
            detectionAngle,
            out _,
            out _);

        if (!withinCone)
            return;

        // The player is inside the vision cone; make sure a wall isn't
        // blocking the guard's line of sight before counting it as a spot.
        if (!DetectionUtility.HasLineOfSight(transform.position, player.position, transform, losHitBuffer, player))
            return;

        IsPlayerDetected = true;
        if (UXStealthDirector.Owns(gameObject)) return;
        Debug.Log("Guard detected the player!");

        //Records guard detection before the game ends
        GameplayMetrics.Instance?.RecordGuardDetection();

        GameManager.Instance.LoseGame();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsPlayerDetected ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize detection cone using the same forward direction detection uses.
        Vector3 forward = Application.isPlaying
            ? (Vector3)ConeForward
            : transform.right;

        Vector3 leftBoundary = Quaternion.Euler(0, 0, detectionAngle / 2f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -detectionAngle / 2f) * forward;

        Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
    }
}
