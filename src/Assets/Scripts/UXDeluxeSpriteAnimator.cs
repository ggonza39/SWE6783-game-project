using UnityEngine;

// UX art only. Keeps existing player movement, guard patrol and detection unchanged.
// Place on the Player and Guard in UXFacilityPreview through the Editor installer.
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class UXDeluxeSpriteAnimator : MonoBehaviour
{
    private const string ArtRoot = "UXArtDeluxe/";
    private const int FrameCount = 4;
    private readonly string[] directions = { "down", "up", "left", "right" };
    private Sprite[,] frames;
    private SpriteRenderer art;
    private Transform artTransform;
    private Transform legacyArt;
    private UXCharacterVisuals legacyController;
    private Vector3 previousPosition;
    private bool isGuard;
    private int facing;
    private int previousFrame = -1;
    private int previousDirection = -1;

    private void Start()
    {
        isGuard = GetComponent<GuardController>() != null;
        string prefix = isGuard ? "guard_" : "player_";
        frames = new Sprite[4, FrameCount];

        for (int direction = 0; direction < 4; direction++)
        {
            for (int frame = 0; frame < FrameCount; frame++)
            {
                string path = ArtRoot + prefix + directions[direction] + "_" + frame;
                frames[direction, frame] = Resources.Load<Sprite>(path);
                if (frames[direction, frame] == null)
                {
                    Debug.LogWarning("UX Deluxe art missing: " + path + ". Keeping earlier visuals.");
                    enabled = false;
                    return;
                }
            }
        }

        // The existing procedural animator automatically attaches to Player/Guard.
        // Suppress ONLY its presentation; do not destroy or modify gameplay scripts.
        legacyController = GetComponent<UXCharacterVisuals>();
        if (legacyController != null)
        {
            legacyController.enabled = false;
            Transform child = transform.Find(isGuard ? "UX_GuardAnimatedArt" : "UX_PlayerAnimatedArt");
            if (child != null)
            {
                legacyArt = child;
                legacyArt.gameObject.SetActive(false);
            }
        }

        artTransform = transform.Find("UX_Deluxe_PreviewArt");
        if (artTransform == null)
        {
            GameObject child = new GameObject("UX_Deluxe_PreviewArt");
            child.transform.SetParent(transform, false);
            artTransform = child.transform;
        }

        art = artTransform.GetComponent<SpriteRenderer>();
        if (art == null) art = artTransform.gameObject.AddComponent<SpriteRenderer>();
        art.sprite = frames[0, 0];
        art.color = Color.white;
        art.sortingOrder = isGuard ? 19 : 20;
        art.enabled = true;
        previousPosition = transform.position;
        facing = isGuard && transform.right.x < 0 ? 2 : 0;
        ApplyWorldSize();
    }

    private void LateUpdate()
    {
        if (art == null || Time.timeScale <= 0f) return;

        Vector3 position = transform.position;
        Vector2 delta = position - previousPosition;
        previousPosition = position;
        bool moving = delta.sqrMagnitude > 0.000001f;

        if (isGuard)
        {
            Vector2 sight = transform.right;
            if (Mathf.Abs(sight.x) >= Mathf.Abs(sight.y))
                facing = sight.x < 0f ? 2 : 3;
            else
                facing = sight.y > 0f ? 1 : 0;
        }
        else if (moving)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                facing = delta.x < 0f ? 2 : 3;
            else
                facing = delta.y > 0f ? 1 : 0;
        }

        int frameNumber = moving
            ? Mathf.FloorToInt(Time.time * (isGuard ? 6f : 9f)) % FrameCount
            : 0;

        if (frameNumber != previousFrame || facing != previousDirection)
        {
            art.sprite = frames[facing, frameNumber];
            previousFrame = frameNumber;
            previousDirection = facing;
        }

        ApplyWorldSize();
    }

    private void ApplyWorldSize()
    {
        if (artTransform == null) return;
        // Existing player has a very small Transform scale due to its original
        // sprite setup script. Counteract that here, without changing colliders.
        Vector3 parentScale = transform.lossyScale;
        float height = isGuard ? 1.25f : 1.35f;
        // Actual character occupies approximately 67% of the 96px sprite cell.
        float imageScale = height / 0.67f;

        artTransform.localPosition = new Vector3(0f, 0f, -0.02f);
        artTransform.localScale = new Vector3(
            imageScale / Mathf.Max(0.01f, Mathf.Abs(parentScale.x)),
            imageScale / Mathf.Max(0.01f, Mathf.Abs(parentScale.y)),
            1f);
        // Guard facing rotates its gameplay transform; its visual stays upright.
        artTransform.rotation = Quaternion.identity;
    }
}
