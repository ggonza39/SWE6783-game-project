using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 40f;
    [SerializeField] private float sweepAngle = 60f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float detectionAngle = 45f;

    private float startingAngle;
    private Transform player;

        private void Start()
    {
        startingAngle = transform.eulerAngles.z;

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
            Debug.LogWarning("SecurityCamera: Player GameObject not found in scene.");
        }
    }

        private void Update()
    {
        // Detection and sweep only proceed when GameManager exists and the game has not ended
        if (GameManager.Instance == null || GameManager.Instance.GameEnded)
            return;

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
            return;

        // Respect the player's hiding state
        if (playerHiding != null && playerHiding.IsHidden)
            return;

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
            GameManager.Instance.LoseGame();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize detection cone
        Vector3 forward = transform.right;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, detectionAngle / 2f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -detectionAngle / 2f) * forward;

        Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
    }
}