using UnityEngine;

// The Hard extraction door and the bulkhead that closes the alternate entrance.
public sealed class UXAccessLock : MonoBehaviour
{
    public string Label { get; private set; }
    public bool IsUnlocked { get; private set; }
    public BoxCollider2D Door { get; private set; }
    private SpriteRenderer leaf, screen;
    private SpriteRenderer[] stripes;
    private Sprite unitSprite;

    public static UXAccessLock Create(Transform owner, string label, Vector2 doorPosition,
        Vector2 doorSize, Vector2 consolePosition)
    {
        GameObject root = new GameObject("Hard_Access_Lock_" + label);
        root.transform.SetParent(owner, false);
        root.transform.position = consolePosition;
        UXAccessLock access = root.AddComponent<UXAccessLock>();
        access.Label = label;
        access.unitSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 1f);
        access.Art("Console_Case", consolePosition, new Vector2(0.65f, 0.68f), new Color(0.08f, 0.14f, 0.18f), 8);
        access.screen = access.Art("Console_Screen", consolePosition + new Vector2(0f, 0.08f),
            new Vector2(0.42f, 0.28f), new Color(1f, 0.35f, 0.16f), 9);
        access.Art("Console_Keypad", consolePosition + new Vector2(0f, -0.18f),
            new Vector2(0.34f, 0.08f), new Color(0.65f, 0.85f, 0.9f), 9);
        access.leaf = access.Art("Door_" + label, doorPosition, doorSize, new Color(0.13f, 0.19f, 0.23f), 7);
        access.stripes = new SpriteRenderer[3];
        bool vertical = doorSize.y > doorSize.x;
        for (int i = 0; i < 3; i++)
            access.stripes[i] = access.Art("Door_Lock_Light_" + i,
                doorPosition + (vertical ? new Vector2(0f, (i - 1) * 0.48f) : new Vector2((i - 1) * 0.48f, 0f)),
                vertical ? new Vector2(0.56f, 0.12f) : new Vector2(0.12f, 0.56f),
                new Color(1f, 0.35f, 0.16f), 8);
        access.Door = access.leaf.gameObject.AddComponent<BoxCollider2D>();
        access.Door.size = Vector2.one;
        return access;
    }

    private SpriteRenderer Art(string name, Vector2 position, Vector2 size, Color color, int order)
    {
        GameObject art = new GameObject(name);
        art.transform.SetParent(transform, false);
        art.transform.position = position;
        art.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sprite = art.AddComponent<SpriteRenderer>();
        sprite.sprite = unitSprite; sprite.color = color; sprite.sortingOrder = order;
        return sprite;
    }

    public void Unlock()
    {
        if (IsUnlocked) return;
        IsUnlocked = true; Door.enabled = false; leaf.enabled = false;
        foreach (SpriteRenderer stripe in stripes) stripe.enabled = false;
        screen.color = new Color(0.25f, 1f, 0.5f);
    }

    public BoxCollider2D SealAlternativeEntrance(Vector2 position, Vector2 size)
    {
        SpriteRenderer wall = Art("Hard_Sealed_North_Entrance", position, size,
            new Color(0.17f, 0.24f, 0.28f), 8);
        BoxCollider2D collider = wall.gameObject.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        Art("Bulkhead_Hazard_Top", position + new Vector2(0f, size.y * 0.35f),
            new Vector2(size.x * 0.88f, 0.10f), new Color(1f, 0.65f, 0.10f), 9);
        Art("Bulkhead_Hazard_Bottom", position - new Vector2(0f, size.y * 0.35f),
            new Vector2(size.x * 0.88f, 0.10f), new Color(1f, 0.65f, 0.10f), 9);
        return collider;
    }

    private void OnDestroy() { if (unitSprite != null) Destroy(unitSprite); }
}
