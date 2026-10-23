using UnityEngine;

public class GuardController : MonoBehaviour
{
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolDistance = 5f;

    private Vector2 startPosition;
    private int direction = 1;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.Translate(Vector2.right * direction * patrolSpeed * Time.deltaTime);

        if (transform.position.x >= startPosition.x + patrolDistance)
            direction = -1;
        else if (transform.position.x <= startPosition.x - patrolDistance)
            direction = 1;
    }
}