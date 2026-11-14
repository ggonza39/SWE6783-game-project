using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.75f;
    [SerializeField, Range(0.1f, 1f)] private float sneakMultiplier = 0.45f;

    private const string PreviewScenePath = "Assets/Scenes/UXFacilityPreview.unity";
    private bool defaultSneakStance;

    public bool IsSneaking => !IsSprinting && (defaultSneakStance || IsMovingCarefully);
    public bool IsMovingCarefully { get; private set; }
    public bool IsSprinting { get; private set; }
    public float CurrentMoveSpeed => IsMovingCarefully
        ? moveSpeed * sneakMultiplier
        : (IsSprinting ? moveSpeed * sprintMultiplier : moveSpeed);

    private Rigidbody2D rb;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        if (GetComponent<BoxCollider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>();

        defaultSneakStance = gameObject.scene.path == PreviewScenePath ||
            gameObject.scene.path == "Assets/Scenes/UXFacilityExpanded.unity";
        if (defaultSneakStance)
            ConfigurePreviewCollider();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f ||
            (GameManager.Instance != null && GameManager.Instance.GameEnded))
        {
            movement = Vector2.zero;
            IsMovingCarefully = false;
            IsSprinting = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // C/Control adds slower precision steps; the preview is crouched by default.
        IsMovingCarefully = Input.GetKey(KeyCode.C) ||
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        IsSprinting = !IsMovingCarefully &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        movement = new Vector2(horizontal, vertical).normalized;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.GameEnded)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = movement * CurrentMoveSpeed;
    }

    private void ConfigurePreviewCollider()
    {
        // Legacy sprite reimports can resize the body to the old 1.5-unit art.
        // The facility's one-tile passages use the original 0.5 x 0.7 body.
        BoxCollider2D body = GetComponent<BoxCollider2D>();
        Vector3 scale = transform.lossyScale;
        body.size = new Vector2(
            0.5f / Mathf.Max(0.01f, Mathf.Abs(scale.x)),
            0.7f / Mathf.Max(0.01f, Mathf.Abs(scale.y)));
        body.offset = Vector2.zero;
    }
}
