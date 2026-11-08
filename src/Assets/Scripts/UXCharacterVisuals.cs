using UnityEngine;
using UnityEngine.SceneManagement;

// Presentation-only runtime characters. Existing PlayerController,
// GuardController, physics, colliders and gameplay remain unchanged.
[DisallowMultipleComponent]
public sealed class UXCharacterVisuals : MonoBehaviour
{
    private const int Size = 48;
    private const int Frames = 4;
    private const int Directions = 4;

    private bool guard;
    private SpriteRenderer original;
    private SpriteRenderer display;
    private bool originalWasEnabled;
    private Sprite[,] sprites;
    private Texture2D[,] textures;
    private Vector3 lastPosition;
    private int facing;
    private int lastFrame = -1;
    private int lastDirection = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Attach();
    }

    private static void Attach()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.GetComponent<UXCharacterVisuals>() == null)
            player.gameObject.AddComponent<UXCharacterVisuals>();

        foreach (GuardController enemy in
            FindObjectsByType<GuardController>(FindObjectsSortMode.None))
        {
            if (enemy.GetComponent<UXCharacterVisuals>() == null)
                enemy.gameObject.AddComponent<UXCharacterVisuals>();
        }
    }

    private void Awake()
    {
        guard = GetComponent<GuardController>() != null;
        original = GetComponent<SpriteRenderer>();

        if (original == null)
        {
            enabled = false;
            return;
        }

        sprites = new Sprite[Directions, Frames];
        textures = new Texture2D[Directions, Frames];

        for (int dir = 0; dir < Directions; dir++)
            for (int frame = 0; frame < Frames; frame++)
                sprites[dir, frame] = MakeSprite(dir, frame);

        GameObject art = new GameObject(
            guard ? "UX_GuardAnimatedArt" : "UX_PlayerAnimatedArt"
        );

        art.transform.SetParent(transform, false);
        art.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        display = art.AddComponent<SpriteRenderer>();
        display.sprite = sprites[0, 0];
        display.sortingLayerID = original.sortingLayerID;
        display.sortingOrder = Mathf.Max(
            original.sortingOrder + 1, guard ? 5 : 11
        );
        display.color = Color.white;

        originalWasEnabled = original.enabled;
        original.enabled = false;
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (display == null || Time.timeScale <= 0f)
            return;

        Vector3 difference = transform.position - lastPosition;
        lastPosition = transform.position;

        bool moving = difference.sqrMagnitude > 0.00001f;

        if (moving)
        {
            if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
                facing = difference.x < 0f ? 2 : 3;
            else
                facing = difference.y > 0f ? 1 : 0;
        }

        // Guard artwork must match its real detection direction.
        if (guard)
        {
            Vector2 sight = transform.right;

            if (Mathf.Abs(sight.x) >= Mathf.Abs(sight.y))
                facing = sight.x < 0f ? 2 : 3;
            else
                facing = sight.y > 0f ? 1 : 0;
        }
        int frame = moving
            ? Mathf.FloorToInt(Time.time * (guard ? 6f : 9f)) % Frames
            : 0;

        if (frame != lastFrame || facing != lastDirection)
        {
            display.sprite = sprites[facing, frame];
            lastFrame = frame;
            lastDirection = facing;
        }

        // Keep art size independent of the existing Player scale.
        Vector3 scale = transform.lossyScale;
        float height = guard ? 1.25f : 1.35f;
        float desired = height / (Size / 40f);

        if (guard) display.transform.rotation = Quaternion.identity;

        display.transform.localScale = new Vector3(
            desired / Mathf.Max(0.01f, Mathf.Abs(scale.x)),
            desired / Mathf.Max(0.01f, Mathf.Abs(scale.y)),
            1f
        );
    }

    private Sprite MakeSprite(int dir, int frame)
    {
        Texture2D texture = new Texture2D(
            Size, Size, TextureFormat.RGBA32, false
        );

        texture.name = (guard ? "UX_Guard" : "UX_Player")
            + "_" + dir + "_" + frame;

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[Size * Size];
        PaintCharacter(pixels, dir, frame);

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        textures[dir, frame] = texture;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, Size, Size),
            new Vector2(0.5f, 0.45f),
            40f, 0, SpriteMeshType.FullRect
        );

        sprite.name = texture.name;
        return sprite;
    }

    private void PaintCharacter(Color32[] p, int dir, int frame)
    {
        Color32 outline = guard
            ? new Color32(13, 20, 32, 255)
            : new Color32(9, 17, 29, 255);

        Color32 cloth = guard
            ? new Color32(53, 74, 97, 255)
            : new Color32(31, 61, 82, 255);

        Color32 shade = guard
            ? new Color32(31, 45, 67, 255)
            : new Color32(23, 39, 62, 255);

        Color32 light = guard
            ? new Color32(109, 136, 158, 255)
            : new Color32(65, 120, 142, 255);

        Color32 accent = guard
            ? new Color32(238, 160, 61, 255)
            : new Color32(65, 220, 218, 255);

        Color32 skin = new Color32(187, 144, 107, 255);
        Color32 boot = new Color32(15, 22, 30, 255);

        // Transparent surroundings and a soft ground shadow.
        for (int y = 5; y <= 10; y++)
            for (int x = 13; x <= 35; x++)
            {
                float nx = (x - 24f) / 12f;
                float ny = (y - 7.5f) / 3f;

                if (nx * nx + ny * ny <= 1f)
                    Pixel(p, x, y, new Color32(1, 8, 16, 58));
            }

        int stride = frame == 1 ? 3 : (frame == 3 ? -3 : 0);

        // Independently animated legs and boots.
        Box(p, 16, 10 + stride, 8, 13, outline);
        Box(p, 26, 10 - stride, 8, 13, outline);
        Box(p, 18, 13 + stride, 5, 9, shade);
        Box(p, 28, 13 - stride, 5, 9, shade);
        Box(p, 15, 9 + stride, 9, 5, boot);
        Box(p, 26, 9 - stride, 9, 5, boot);
        Box(p, 18, 12 + stride, 5, 2, light);
        Box(p, 28, 12 - stride, 5, 2, light);

        // Animated arms, gloves, and shoulder markings.
        Box(p, 9, 23 - stride / 2, 8, 12, outline);
        Box(p, 33, 23 + stride / 2, 8, 12, outline);
        Box(p, 11, 25 - stride / 2, 6, 9, cloth);
        Box(p, 33, 25 + stride / 2, 6, 9, shade);
        Box(p, 11, 21 - stride / 2, 6, 5, boot);
        Box(p, 33, 21 + stride / 2, 6, 5, boot);
        Box(p, 13, 31 - stride / 2, 3, 3, accent);
        Box(p, 35, 31 + stride / 2, 3, 3, accent);

        // Torso, armor, and utility belt.
        Box(p, 16, 19, 18, 18, outline);
        Box(p, 18, 22, 14, 14, cloth);
        Box(p, 18, 20, 14, 5, shade);
        Box(p, 19, 20, 12, 2, accent);
        Box(p, 20, 25, 10, 8, shade);
        Box(p, 20, 31, 10, 2, light);
        Box(p, 17, 23, 3, 8, light);
        Box(p, 30, 23, 3, 8, light);

        if (dir == 1)
        {
            // Character facing away: backpack and rear helmet.
            Box(p, 19, 25, 12, 11, outline);
            Box(p, 21, 27, 8, 8, shade);
            Box(p, 22, 29, 6, 2, accent);
            Box(p, 16, 33, 18, 11, outline);
            Box(p, 18, 35, 14, 7, cloth);
            Box(p, 20, 41, 10, 2, light);
        }
        else
        {
            // Front and sideways hood / security cap.
            Box(p, 16, 33, 18, 11, outline);
            Box(p, 18, 35, 14, 8, guard ? cloth : shade);
            Box(p, 20, 33, 10, 5, skin);

            if (guard)
            {
                Box(p, 16, 40, 18, 4, shade);
                Box(p, 15, 38, 20, 3, outline);
                Box(p, 22, 40, 6, 2, accent);
            }
            else
            {
                Box(p, 18, 40, 14, 3, cloth);
                Box(p, 21, 36, 8, 3, outline);
            }

            Box(p, 20, 37, 10, 2, accent);
            Box(p, 21, 33, 8, 3, shade);
            Box(p, 23, 34, 4, 1, light);

            if (dir == 2)
            {
                Box(p, 18, 35, 6, 5, shade);
                Box(p, 18, 38, 5, 2, accent);
            }
            else if (dir == 3)
            {
                Box(p, 27, 35, 6, 5, shade);
                Box(p, 28, 38, 5, 2, accent);
            }
        }

        Box(p, 24, 26, 3, 3, accent);

        if (guard)
            Box(p, 33, 26, 4, 3, accent);
    }

    private static void Box(
        Color32[] pixels, int x, int y, int w, int h, Color32 color)
    {
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
                Pixel(pixels, px, py, color);
    }

    private static void Pixel(
        Color32[] pixels, int x, int y, Color32 color)
    {
        if (x >= 0 && x < Size && y >= 0 && y < Size)
            pixels[y * Size + x] = color;
    }

    private void OnDestroy()
    {
        if (original != null)
            original.enabled = originalWasEnabled;

        if (sprites == null)
            return;

        for (int dir = 0; dir < Directions; dir++)
            for (int frame = 0; frame < Frames; frame++)
            {
                if (sprites[dir, frame] != null)
                    Destroy(sprites[dir, frame]);

                if (textures[dir, frame] != null)
                    Destroy(textures[dir, frame]);
            }
    }
}