using UnityEngine;

// Preview-only guard patrol. Does not edit the original
// GuardController or GuardDetection source files.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public sealed class UXPreviewGuardFacingPatrol : MonoBehaviour
{
    [SerializeField] private float leftX = -2.8f;
    [SerializeField] private float rightX = 2.8f;
    [SerializeField] private float speed = 1.25f;
    [SerializeField] private float turnDuration = 0.65f;

    private GuardController originalController;
    private int direction = 1;
    private bool turning;
    private float turnTime;
    private float y;
    private float z;

    private void Awake()
    {
        // Only this preview instance uses the new movement.
        originalController = GetComponent<GuardController>();

        if (originalController != null)
            originalController.enabled = false;

        y = transform.position.y;
        z = transform.position.z;
    }

    private void Start()
    {
        transform.rotation = Quaternion.identity;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (turning)
        {
            turnTime += Time.deltaTime;

            float t = Mathf.Clamp01(
                turnTime / Mathf.Max(0.1f, turnDuration));

            t = t * t * (3f - 2f * t);

            float from = direction > 0 ? 0f : 180f;
            float to = direction > 0 ? 180f : 360f;

            transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.LerpAngle(from, to, t));

            if (turnTime >= turnDuration)
            {
                direction *= -1;
                turning = false;
            }
            return;
        }

        float target = direction > 0 ? rightX : leftX;

        float nextX = Mathf.MoveTowards(
            transform.position.x,
            target,
            speed * Time.deltaTime);

        transform.position = new Vector3(nextX, y, z);

        transform.rotation = Quaternion.Euler(
            0f, 0f, direction > 0 ? 0f : 180f);

        if (Mathf.Abs(nextX - target) < 0.001f)
        {
            turning = true;
            turnTime = 0f;
        }
    }
}