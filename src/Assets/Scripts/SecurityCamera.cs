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

        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }

    private void Update()
    {
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
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}