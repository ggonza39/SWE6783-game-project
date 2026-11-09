
using UnityEngine;

public class SecurityCamera : MonoBehaviour, IConeDetector
{
    [SerializeField] private float rotationSpeed = 40f;
    [SerializeField] private float sweepAngle = 60f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float detectionAngle = 45f;

    /// <summary>True while a hacked terminal has disabled this camera.</summary>
    public bool IsDisabled { get; private set; }

    /// <summary>True on any frame the player is currently visible to this camera.</summary>
    public bool IsPlayerDetected { get; private set; }

    private float startingAngle;
    private Transform player;
    private PlayerHiding playerHiding;
    private readonly RaycastHit2D[] losHitBuffer = new RaycastHit2D[8];

    // IConeDetector - drives the runtime-visible cone (DetectionConeRenderer)
    // with the exact same values used for detection.
    public Vector2 ConeOrigin => transform.position;
    public Vector2 ConeForward => transform.right;
    public float ConeRange => detectionRange;
    public float ConeFovAngle => detectionAngle;
    public Color ConeColor => IsDisabled
        ? new Color(0.5f, 0.5f, 0.5f, 0.2f)
        : (IsPlayerDetected ? new Color(1f, 0f, 0f, 0.35f) : new Color(1f, 0.92f, 0.016f, 0.25f));
    public bool ConeActive => GameManager.Instance == null || !GameManager.Instance.GameEnded;

    /// <summary>Called by TerminalInteraction when a terminal hacks this camera offline.</summary>
    public void SetDisabled(bool disabled)
    {
        IsDisabled = disabled;
        IsPlayerDetected = false;
    }

    private void Start()
    {
        startingAngle = transform.eulerAngles.z;

        DetectionConeRenderer coneRenderer = GetComponent<DetectionConeRenderer>();
        if (coneRenderer == null)
            coneRenderer = gameObject.AddComponent<DetectionConeRenderer>();
        coneRenderer.Detector = this;

        // Find the player by tag, falling back to name.
        GameObject playerObject = null;

        try
        {
            playerObject = GameObject.FindWithTag("Player");
        }
        catch (UnityException)
        {
            Debug.LogWarning(
                "SecurityCamera: Player tag not found. Using name lookup."
            );
        }

        if (playerObject == null)
        {
            playerObject = GameObject.Find("Player");
        }

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHiding = playerObject.GetComponent<PlayerHiding>();
        }
        else
        {
            Debug.LogWarning(
                "SecurityCamera: Player GameObject not found in scene."
            );
        }
    }

    private void Update()
    {
        // Stop camera activity when the game ends,
        // the GameManager is unavailable, or the camera has been hacked.
        if (GameManager.Instance == null ||
            GameManager.Instance.GameEnded ||
            IsDisabled)
        {
            return;
        }

        Sweep();
        DetectPlayer();
    }

    private void Sweep()
    {
        float angle =
            Mathf.Sin(Time.time * rotationSpeed * Mathf.Deg2Rad)
            * sweepAngle;

        transform.rotation =
            Quaternion.Euler(0f, 0f, startingAngle + angle);
    }

    private void DetectPlayer()
    {
        IsPlayerDetected = false;

        if (player == null)
        {
            return;
        }

        // Hidden players cannot be detected.
        if (playerHiding != null && playerHiding.IsHidden)
        {
            return;
        }

        bool withinCone = DetectionUtility.IsWithinFieldOfView(
            transform.position,
            transform.right,
            player.position,
            detectionRange,
            detectionAngle,
            out _,
            out _);

        if (!withinCone)
        {
            return;
        }

        // The player is inside the sweep cone; make sure a wall isn't
        // blocking the camera's line of sight before counting it as a spot.
        if (!DetectionUtility.HasLineOfSight(transform.position, player.position, transform, losHitBuffer))
        {
            return;
        }

        IsPlayerDetected = true;
        Debug.Log("Security camera detected the player!");

        GameManager.Instance.LoseGame();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = IsDisabled
            ? Color.gray
            : (IsPlayerDetected ? Color.red : Color.yellow);

        // Draw detection range.
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        // Draw detection cone boundaries.
        Vector3 forward = transform.right;

        Vector3 leftBoundary =
            Quaternion.Euler(0f, 0f, detectionAngle / 2f)
            * forward;

        Vector3 rightBoundary =
            Quaternion.Euler(0f, 0f, -detectionAngle / 2f)
            * forward;

        Gizmos.DrawRay(
            transform.position,
            leftBoundary * detectionRange
        );

        Gizmos.DrawRay(
            transform.position,
            rightBoundary * detectionRange
        );
    }
}
