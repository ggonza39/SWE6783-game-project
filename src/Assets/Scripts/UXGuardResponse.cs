using System.Collections.Generic;
using UnityEngine;

// Pursuit uses the same solid geometry as the visible facility; no teleporting through rooms.
[DefaultExecutionOrder(100)]
public sealed class UXGuardResponse : MonoBehaviour
{
    private UXStealthDirector director;
    private UXPreviewGuardFacingPatrol patrol;
    private GuardDetection sight;
    private bool responding;
    private Vector2 patrolAnchor;
    private Quaternion patrolRotation;
    private List<Vector2> path = new List<Vector2>();
    private int pathStep;
    private float repathAt, aimTime, fireAt, flashUntil, searchAt;
    private Vector2 searchTarget;
    private SpriteRenderer beam;
    private Sprite beamSprite;
    private readonly RaycastHit2D[] sightHits = new RaycastHit2D[16];

    private void Start()
    {
        director = UXStealthDirector.Instance;
        patrol = GetComponent<UXPreviewGuardFacingPatrol>();
        sight = GetComponent<GuardDetection>();
        GameObject line = new GameObject("Guard_Aim_And_Shot");
        line.transform.SetParent(transform, false);
        beam = line.AddComponent<SpriteRenderer>();
        beamSprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
        beam.sprite = beamSprite; beam.sortingOrder = 23; beam.enabled = false;
    }

    private void Update()
    {
        if (beam != null) beam.enabled = false;
        if (director == null || Time.timeScale <= 0f || GameManager.Instance == null ||
            GameManager.Instance.GameEnded) return;
        if (director.State.Responding)
        {
            if (!responding)
            {
                responding = true;
                patrolAnchor = transform.position; patrolRotation = transform.rotation;
                if (patrol != null) patrol.enabled = false;
                path.Clear(); repathAt = 0f; searchAt = 0f;
            }
            bool canAim = sight != null && sight.IsPlayerDetected && !director.PlayerHidden &&
                Vector2.Distance(transform.position, director.Player.position) <= 3.6f;
            if (canAim)
            {
                Vector2 delta = director.Player.position - transform.position;
                Turn(delta, 210f);
                aimTime += Time.deltaTime;
                DrawBeam(delta);
                if (aimTime >= 0.8f && Time.time >= fireAt &&
                    DetectionUtility.HasLineOfSight(transform.position, director.Player.position,
                        transform, sightHits, director.Player))
                {
                    director.Hit(); fireAt = Time.time + 1.35f; flashUntil = Time.time + 0.08f;
                }
            }
            else
            {
                aimTime = 0f;
                Vector2 target = director.LastKnownPosition;
                if (director.State.Phase == UXAlertPhase.Searching &&
                    Vector2.Distance(transform.position, target) < 1.2f)
                {
                    if (Time.time >= searchAt)
                    {
                        searchAt = Time.time + 2f;
                        searchTarget = target + Random.insideUnitCircle * 1.7f;
                    }
                    target = searchTarget;
                }
                Navigate(target, director.State.Phase == UXAlertPhase.Engaged ? 1.9f : 1.25f);
            }
        }
        else if (responding)
        {
            aimTime = 0f;
            Navigate(patrolAnchor, 1.25f);
            if (Vector2.Distance(transform.position, patrolAnchor) < 0.12f)
            {
                // Finish on the original patrol segment before resuming its stored target.
                transform.position = new Vector3(patrolAnchor.x, patrolAnchor.y, transform.position.z);
                transform.rotation = patrolRotation;
                responding = false;
                if (patrol != null) patrol.enabled = true;
            }
        }
    }

    private void Navigate(Vector2 target, float speed)
    {
        if (Time.time >= repathAt)
        {
            path = director.Path(transform.position, target); pathStep = 0;
            repathAt = Time.time + 0.65f;
        }
        while (pathStep < path.Count && Vector2.Distance(transform.position, path[pathStep]) < 0.045f) pathStep++;
        if (pathStep >= path.Count)
        {
            if (!director.State.Responding && path.Count > 0 &&
                Vector2.Distance(transform.position, target) < 0.45f &&
                director.ClearNavigationSegment(transform.position, target))
            {
                Vector2 finish = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
                transform.position = new Vector3(finish.x, finish.y, transform.position.z);
            }
            return;
        }
        Vector2 heading = path[pathStep] - (Vector2)transform.position;
        Turn(heading, 190f);
        // Face first: predictable turns provide a useful stealth window.
        if (Vector2.Angle(transform.right, heading) > 30f) return;
        Vector2 next = Vector2.MoveTowards(transform.position, path[pathStep], speed * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    private void Turn(Vector2 direction, float degreesPerSecond)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.MoveTowardsAngle(transform.eulerAngles.z, angle, degreesPerSecond * Time.deltaTime));
    }

    private void DrawBeam(Vector2 delta)
    {
        if (beam == null) return;
        beam.enabled = true;
        beam.color = Time.time < flashUntil ? Color.white : new Color(1f, 0.35f, 0.12f, 0.65f);
        beam.transform.position = transform.position + new Vector3(0f, 0f, -0.04f);
        beam.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        beam.transform.localScale = new Vector3(delta.magnitude, Time.time < flashUntil ? 0.065f : 0.022f, 1f);
    }
    private void OnDestroy() { if (beamSprite != null) Destroy(beamSprite); }
}
