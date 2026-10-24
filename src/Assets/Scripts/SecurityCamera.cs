
using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 40f;
    [SerializeField] private float sweepAngle = 60f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float detectionAngle = 45f;

    private float startingAngle;
    private Transform player;
    private PlayerHiding playerHiding;

    private void Start()
    {
        startingAngle = transform.eulerAngles.z;

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
        // Stop camera activity when the game ends
        // or the GameManager is unavailable.
        if (GameManager.Instance == null ||
            GameManager.Instance.GameEnded)
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
        if (player == null)
        {
            return;
        }

        // Hidden players cannot be detected.
        if (playerHiding != null && playerHiding.IsHidden)
        {
            return;
        }

        Vector2 directionToPlayer =
            (player.position - transform.position).normalized;

        float distance =
            Vector2.Distance(transform.position, player.position);

        float angle =
            Vector2.Angle(transform.right, directionToPlayer);

        if (distance <= detectionRange &&
            angle <= detectionAngle / 2f)
        {
            Debug.Log("Security camera detected the player!");

            //Records camera detection before the game ends
            GameplayMetrics.Instance?.RecordCameraDetection();

            GameManager.Instance.LoseGame();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

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
