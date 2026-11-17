using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.75f;
    [SerializeField, Range(0.1f, 1f)] private float sneakMultiplier = 0.45f;

    private const string PreviewScenePath = "Assets/Scenes/UXFacilityPreview.unity";
    private bool defaultSneakStance;
    private bool usesSprintReserve;
    private bool sprintedLastStep;

    public bool IsSneaking => !IsSprinting && (defaultSneakStance || IsMovingCarefully);
    public bool IsMovingCarefully { get; private set; }
    public bool IsSprinting { get; private set; }
    public float CurrentMoveSpeed => IsMovingCarefully
        ? moveSpeed * sneakMultiplier
        : (IsSprinting ? moveSpeed * sprintMultiplier : moveSpeed);

    private Rigidbody2D rb;
    private Vector2 movement,blastDirection;
    private float blastRemaining;
    public void ApplyMineKnockback(Vector2 origin)
    {
        Vector2 away=(Vector2)transform.position-origin;
        blastDirection=away.sqrMagnitude>.0001f?away.normalized:new Vector2(0,-1);
        blastRemaining=.30f;IsSprinting=false;
        if(usesSprintReserve)UXCampaign.FlushSprint();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        if (GetComponent<BoxCollider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>();

        defaultSneakStance = gameObject.scene.path == PreviewScenePath ||
            gameObject.scene.path == "Assets/Scenes/UXFacilityExpanded.unity" ||
            gameObject.scene.path == UXCampaign.CrimsonScene;
        usesSprintReserve=gameObject.scene.path==UXCampaign.FacilityScene || gameObject.scene.path==UXCampaign.CrimsonScene;
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
            if(usesSprintReserve)UXCampaign.FlushSprint();
            sprintedLastStep=false;
            return;
        }

        // C/Control adds slower precision steps; the preview is crouched by default.
        IsMovingCarefully = Input.GetKey(KeyCode.C) ||
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        movement = new Vector2(horizontal, vertical).normalized;
        IsSprinting = movement.sqrMagnitude>0f && !IsMovingCarefully &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
            (!usesSprintReserve || (UXCampaign.Running && UXCampaign.SprintRemaining>0f));
    }

    private void FixedUpdate()
    {
        if(Time.timeScale<=0f || (GameManager.Instance!=null && GameManager.Instance.GameEnded))
        {
            rb.linearVelocity=Vector2.zero;IsSprinting=false;blastRemaining=0f;
            if(usesSprintReserve)UXCampaign.FlushSprint();
            sprintedLastStep=false;return;
        }
        if(blastRemaining>0f)
        {
            float dt=Mathf.Min(Time.fixedDeltaTime,blastRemaining);
            Vector2 step=blastDirection*(6f*(blastRemaining/.30f)*dt);
            var director=UXStealthDirector.Instance;
            rb.linearVelocity=Vector2.zero;
            if(director!=null)rb.MovePosition(director.ClampBlastTravel(rb.position,step));
            blastRemaining=Mathf.Max(0f,blastRemaining-dt);
            IsSprinting=false;sprintedLastStep=false;return;
        }
        float speed=CurrentMoveSpeed;
        if(usesSprintReserve && IsSprinting && movement.sqrMagnitude>0f)
        {
            float dt=Time.fixedDeltaTime;
            float paid=UXCampaign.SpendSprint(dt);
            // Pay exactly for the final partial step; its remainder uses crouch speed.
            float portion=dt>0f?paid/dt:0f;
            speed=moveSpeed*(1f+(sprintMultiplier-1f)*portion);
            IsSprinting=paid>0f && UXCampaign.SprintRemaining>0f;
        }
        if(usesSprintReserve && sprintedLastStep && !IsSprinting)UXCampaign.FlushSprint();
        sprintedLastStep=IsSprinting;
        rb.linearVelocity=movement*speed;
    }

    private void OnDisable(){if(usesSprintReserve)UXCampaign.FlushSprint();}

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
