using UnityEngine;

public class GuardController : MonoBehaviour
{
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolDistance = 5f;

    [Tooltip("Optional patrol route. When assigned, the guard walks between " +
        "these waypoints in order (looping) instead of the default left/right " +
        "pacing. Leave empty to keep the original back-and-forth behavior.")]
    [SerializeField] private Transform[] waypoints;

    /// <summary>
    /// Current normalized movement direction. GuardDetection reads this so the
    /// vision cone always faces the direction the guard is actually walking,
    /// instead of a fixed world-space direction.
    /// </summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.right;

    private Vector2 startPosition;
    private int direction = 1;
    private int waypointIndex;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Don't keep patrolling once the mission has ended.
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            return;

        if (waypoints != null && waypoints.Length > 0)
            PatrolWaypoints();
        else
            PatrolBackAndForth();
    }

    private void PatrolBackAndForth()
    {
        Vector2 moveStep = Vector2.right * direction * patrolSpeed * Time.deltaTime;
        transform.Translate(moveStep);

        if (transform.position.x >= startPosition.x + patrolDistance)
            direction = -1;
        else if (transform.position.x <= startPosition.x - patrolDistance)
            direction = 1;

        FacingDirection = new Vector2(direction, 0f);
    }

    private void PatrolWaypoints()
    {
        Transform target = waypoints[waypointIndex];

        if (target == null)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float distanceThisFrame = patrolSpeed * Time.deltaTime;

        if (toTarget.sqrMagnitude <= distanceThisFrame * distanceThisFrame)
        {
            transform.position = target.position;
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            return;
        }

        Vector2 moveDirection = toTarget.normalized;
        transform.Translate(moveDirection * distanceThisFrame);
        FacingDirection = moveDirection;
    }
}