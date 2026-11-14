using System.Collections.Generic;
using UnityEngine;

// Drives only the guard instance in the dedicated facility preview.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class UXPreviewGuardFacingPatrol : MonoBehaviour
{
    [SerializeField] private float leftX = -2.8f;
    [SerializeField] private float rightX = 2.8f;
    [SerializeField] private float speed = 1.25f;
    [SerializeField] private float turnDuration = 0.65f;
    [SerializeField] private float minimumPause = 0.8f;
    [SerializeField] private float maximumPause = 1.2f;
    [SerializeField] private Vector2[] routePoints = new Vector2[0];
    [SerializeField] private Vector2Int[] routeEdges = new Vector2Int[0];

    private List<int>[] neighbors;
    private int[] visits;
    private int targetPoint;
    private int previousPoint = -1;
    private float worldZ;
    private float pauseRemaining;
    private bool turning;
    private float turnTime;
    private float turnFrom;
    private float turnTo;
    private List<Vector2> navigationPath = new List<Vector2>();
    private int navigationStep;
    private float repathAt;

    private void Awake()
    {
        GuardController originalController = GetComponent<GuardController>();
        if (originalController != null)
            originalController.enabled = false;

        worldZ = transform.position.z;

        // Older saved previews retain their original two-point route.
        if (routePoints.Length < 2)
        {
            routePoints = new[]
            {
                new Vector2(leftX, transform.position.y),
                new Vector2(rightX, transform.position.y)
            };
            routeEdges = new[] { new Vector2Int(0, 1) };
        }

        neighbors = new List<int>[routePoints.Length];
        visits = new int[routePoints.Length];
        for (int i = 0; i < neighbors.Length; i++)
            neighbors[i] = new List<int>();

        foreach (Vector2Int edge in routeEdges)
        {
            if (edge.x < 0 || edge.y < 0 ||
                edge.x >= routePoints.Length || edge.y >= routePoints.Length ||
                edge.x == edge.y)
            {
                Debug.LogError("Preview patrol contains an invalid route edge.", this);
                enabled = false;
                return;
            }

            neighbors[edge.x].Add(edge.y);
            neighbors[edge.y].Add(edge.x);
        }

        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < routePoints.Length; i++)
        {
            float distance = ((Vector2)transform.position - routePoints[i]).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetPoint = i;
            }
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f ||
            (GameManager.Instance != null && GameManager.Instance.GameEnded))
            return;

        UXStealthDirector director = UXStealthDirector.Owns(gameObject) ? UXStealthDirector.Instance : null;
        if (director != null && director.State.Responding) return;

        if (pauseRemaining > 0f)
        {
            pauseRemaining -= Time.deltaTime;
            return;
        }

        if (turning)
        {
            turnTime += Time.deltaTime;
            float duration = Mathf.Max(0.1f, turnDuration);
            float progress = Mathf.Clamp01(turnTime / duration);
            progress = progress * progress * (3f - 2f * progress);
            transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.LerpAngle(turnFrom, turnTo, progress));
            turning = turnTime < duration;
            return;
        }

        if (director != null)
        {
            FollowNavigableRoute(director);
            return;
        }

        Vector3 destination = new Vector3(
            routePoints[targetPoint].x, routePoints[targetPoint].y, worldZ);
        transform.position = Vector3.MoveTowards(
            transform.position, destination, Mathf.Max(0f, speed) * Time.deltaTime);

        if ((transform.position - destination).sqrMagnitude < 0.0001f)
            ChooseNextPoint();
    }

    private void FollowNavigableRoute(UXStealthDirector director)
    {
        Vector2 destination = routePoints[targetPoint];
        if (Time.time >= repathAt)
        {
            navigationPath = director.Path(transform.position, destination);
            navigationStep = 0;
            repathAt = Time.time + 0.65f;
        }
        while (navigationStep < navigationPath.Count &&
            Vector2.Distance(transform.position, navigationPath[navigationStep]) < 0.045f) navigationStep++;
        if (navigationStep >= navigationPath.Count)
        {
            if (Vector2.Distance(transform.position, destination) < 0.045f ||
                (navigationPath.Count > 0 && !director.CanGuardTravel(destination, destination)))
            {
                // A player occupying a waypoint is an obstacle, not someone to push aside.
                ChooseNextPoint();
                return;
            }
            if (navigationPath.Count == 0 || !director.CanGuardTravel(transform.position, destination)) return;
        }
        Vector2 nextPoint = navigationStep < navigationPath.Count ? navigationPath[navigationStep] : destination;
        Vector2 heading = nextPoint - (Vector2)transform.position;
        float angle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.MoveTowardsAngle(transform.eulerAngles.z, angle, 190f * Time.deltaTime));
        if (Vector2.Angle(transform.right, heading) > 30f) return;
        Vector2 next = Vector2.MoveTowards(transform.position, nextPoint, Mathf.Max(0f, speed) * Time.deltaTime);
        if (!director.TryMoveGuard(transform, next)) repathAt = 0f;
    }

    public void ResumeAtCurrentPosition()
    {
        navigationPath.Clear(); navigationStep = 0; repathAt = 0f;
        turning = false; pauseRemaining = 0f;
    }

    public float DistanceToRoute(Vector2 point)
    {
        float closest = Vector2.Distance(point, transform.position);
        foreach (Vector2Int edge in routeEdges)
        {
            if (edge.x < 0 || edge.y < 0 || edge.x >= routePoints.Length || edge.y >= routePoints.Length) continue;
            Vector2 from = routePoints[edge.x], segment = routePoints[edge.y] - from;
            float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(
                ((point.x - from.x) * segment.x + (point.y - from.y) * segment.y) / segment.sqrMagnitude) : 0f;
            closest = Mathf.Min(closest, Vector2.Distance(point, from + segment * t));
        }
        return closest;
    }

    private void ChooseNextPoint()
    {
        int reached = targetPoint;
        visits[reached]++;
        int next = -1;
        int lowestVisits = int.MaxValue;
        int tiedChoices = 0;

        // Connected routes vary while gradually covering the whole facility.
        foreach (int candidate in neighbors[reached])
        {
            if (neighbors[reached].Count > 1 && candidate == previousPoint)
                continue;

            if (visits[candidate] < lowestVisits)
            {
                lowestVisits = visits[candidate];
                tiedChoices = 1;
                next = candidate;
            }
            else if (visits[candidate] == lowestVisits)
            {
                tiedChoices++;
                if (Random.Range(0, tiedChoices) == 0)
                    next = candidate;
            }
        }

        if (next < 0)
        {
            Debug.LogError("Preview patrol reached a point without a connected route.", this);
            enabled = false;
            return;
        }

        previousPoint = reached;
        targetPoint = next;
        navigationPath.Clear(); navigationStep = 0; repathAt = 0f;
        Vector2 heading = routePoints[next] - routePoints[reached];
        turnFrom = transform.eulerAngles.z;
        turnTo = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;
        float angle = Mathf.Abs(Mathf.DeltaAngle(turnFrom, turnTo));
        turning = angle > 1f;
        turnTime = 0f;

        if (angle > 20f || neighbors[reached].Count > 2)
            pauseRemaining = Random.Range(minimumPause, Mathf.Max(minimumPause, maximumPause));
    }
}
