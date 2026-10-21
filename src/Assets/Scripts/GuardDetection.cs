using UnityEngine;

public class GuardDetection : MonoBehaviour
{
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float detectionAngle = 60f;

        private Transform player;
    private PlayerHiding playerHiding;

    private void Start()
    {
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
        // Detection only proceeds when GameManager exists and the game has not ended
        if (GameManager.Instance == null || GameManager.Instance.GameEnded)
            return;

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
            Debug.Log("Guard detected the player!");
            GameManager.Instance.LoseGame();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize detection cone
        Vector3 forward = transform.right;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, detectionAngle / 2f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -detectionAngle / 2f) * forward;

        Gizmos.DrawRay(transform.position, leftBoundary * detectionRange);
        Gizmos.DrawRay(transform.position, rightBoundary * detectionRange);
    }
}