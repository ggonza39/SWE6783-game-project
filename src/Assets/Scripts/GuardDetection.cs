using UnityEngine;

public class GuardDetection : MonoBehaviour
{
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float detectionAngle = 60f;

    private Transform player;

    private void Start()
    {
        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }

    private void Update()
    {
        if (player == null)
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
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}