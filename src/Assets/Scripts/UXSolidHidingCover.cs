using System.Collections.Generic;
using UnityEngine;

// Expanded-scene maintenance bays: an open entrance for the player and solid
// guard exclusion. The metal back/sides still block the player normally.
[DisallowMultipleComponent]
[DefaultExecutionOrder(300)]
public sealed class UXSolidHidingCover : MonoBehaviour
{
    public BoxCollider2D Pocket { get; private set; }
    public Vector2 Entrance { get; private set; }
    private BoxCollider2D playerBody;
    private SpriteRenderer floor, lamp;
    private Sprite pixel;

    public bool ContainsPlayer(Vector2 at)
    {
        Vector2 delta = at - (Vector2)transform.position;
        return enabled && gameObject.activeSelf && Mathf.Abs(delta.x) <= 0.28f && Mathf.Abs(delta.y) <= 0.3f;
    }

    public static void ConfigureScene(GameObject owner, BoxCollider2D playerBody = null)
    {
        var covers = new List<UXSolidHidingCover>();
        foreach (GameObject spot in GameObject.FindGameObjectsWithTag("HidingSpot"))
        {
            if (spot.scene != owner.scene || spot.GetComponentInParent<UXSolidHidingCover>() != null) continue;
            BoxCollider2D box = spot.GetComponent<BoxCollider2D>();
            if (box == null) continue;
            spot.tag = "Untagged";
            box.isTrigger = false;
            box.offset = Vector2.zero;
            box.size = new Vector2(0.98f / Mathf.Abs(spot.transform.lossyScale.x),
                0.98f / Mathf.Abs(spot.transform.lossyScale.y));
            covers.Add(spot.AddComponent<UXSolidHidingCover>());
        }
        Physics2D.SyncTransforms();
        BoxCollider2D[] geometry = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
        UXPreviewGuardFacingPatrol[] patrols = FindObjectsByType<UXPreviewGuardFacingPatrol>(FindObjectsSortMode.None);
        Transform[] features = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        SpriteRenderer[] art = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (UXSolidHidingCover cover in covers)
        {
            Vector2 offset = cover.ChooseEntrance(geometry, patrols, features);
            if (offset.sqrMagnitude < 0.01f)
            {
                Debug.LogError("Hiding bay needs a clear entrance.", cover);
                continue;
            }
            cover.Entrance = (Vector2)cover.transform.position + offset;
            GameObject pocket = new GameObject("HidingBayInterior");
            pocket.transform.SetParent(cover.transform, false);
            pocket.tag = "HidingSpot";
            cover.Pocket = pocket.AddComponent<BoxCollider2D>();
            cover.Pocket.isTrigger = true;
            cover.Pocket.size = new Vector2(0.7f / Mathf.Abs(pocket.transform.lossyScale.x),
                0.6f / Mathf.Abs(pocket.transform.lossyScale.y));
            cover.BuildBay(offset.y > 0f);
            foreach (SpriteRenderer glow in art)
            {
                if (glow.gameObject.name != "HidingGlow" || glow.gameObject.scene != owner.scene ||
                    Vector2.Distance(glow.transform.position, cover.transform.position) > 0.75f) continue;
                glow.transform.position = new Vector3(cover.transform.position.x,
                    cover.transform.position.y, glow.transform.position.z);
                break;
            }
        }
        // Repeated setup also restores the player exception without duplicating art or colliders.
        foreach (UXSolidHidingCover cover in FindObjectsByType<UXSolidHidingCover>(FindObjectsSortMode.None))
        {
            if (cover.gameObject.scene != owner.scene) continue;
            cover.playerBody = playerBody;
            if (playerBody != null) Physics2D.IgnoreCollision(cover.GetComponent<BoxCollider2D>(), playerBody, true);
        }
    }

    private void BuildBay(bool opensNorth)
    {
        SpriteRenderer oldCrate = GetComponent<SpriteRenderer>();
        if (oldCrate != null) oldCrate.enabled = false;
        pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        floor = Part("Bay_Recess", Vector2.zero, new Vector2(.98f,.98f), new Color(.025f,.08f,.11f), 4);
        Part("Bay_Left_Frame", new Vector2(-.46f,0), new Vector2(.06f,.98f), new Color(.31f,.48f,.57f), 8, true);
        Part("Bay_Right_Frame", new Vector2(.46f,0), new Vector2(.06f,.98f), new Color(.31f,.48f,.57f), 8, true);
        float back = opensNorth ? -.46f : .46f;
        Part("Bay_Back_Frame", new Vector2(0,back), new Vector2(.98f,.06f), new Color(.4f,.57f,.65f), 8, true);
        Part("Bay_Inset", new Vector2(0,back*.55f), new Vector2(.66f,.18f), new Color(.09f,.19f,.24f), 5);
        lamp = Part("Bay_Status_Light", new Vector2(0,back*.85f), new Vector2(.52f,.045f), new Color(.08f,.85f,1f), 9);
        // A visible gap and inward floor steps show exactly where to walk in.
        float sign = opensNorth ? 1f : -1f;
        for (int i = 0; i < 3; i++)
            Part("Bay_Entry_Step", new Vector2(0,sign*(.38f+i*.15f)),
                new Vector2(.34f-i*.075f,.04f), new Color(.08f,.75f,.9f), 6);
    }

    private SpriteRenderer Part(string name, Vector2 offset, Vector2 size, Color color, int order, bool solid = false)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(transform, false);
        part.transform.position = transform.position + (Vector3)offset;
        // Use world dimensions so imported root scale cannot resize the entrance.
        Vector3 scale = transform.lossyScale;
        part.transform.localScale = new Vector3(size.x / Mathf.Abs(scale.x), size.y / Mathf.Abs(scale.y), 1f);
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = pixel; renderer.color = color; renderer.sortingOrder = order;
        if (solid) part.AddComponent<BoxCollider2D>();
        return renderer;
    }

    private void LateUpdate()
    {
        if (floor == null || lamp == null) return;
        bool occupied = playerBody != null && ContainsPlayer(playerBody.bounds.center);
        floor.color = occupied ? new Color(.025f,.2f,.12f) : new Color(.025f,.08f,.11f);
        lamp.color = occupied ? new Color(.25f,1f,.5f) : new Color(.08f,.85f,1f);
    }

    private Vector2 ChooseEntrance(BoxCollider2D[] geometry, UXPreviewGuardFacingPatrol[] patrols, Transform[] features)
    {
        // A vertical opening fits the player's 0.5-wide body without a fiddly narrow side entrance.
        Vector2[] sides = { new Vector2(0f, -1.05f), new Vector2(0f, 1.05f) };
        float best = float.NegativeInfinity;
        Vector2 chosen = Vector2.zero;
        foreach (Vector2 side in sides)
        {
            Vector2 at = (Vector2)transform.position + side;
            bool free = true;
            foreach (BoxCollider2D collider in geometry)
            {
                if (!collider.enabled || collider.isTrigger || collider.gameObject.scene != gameObject.scene ||
                    collider.GetComponentInParent<PlayerController>() != null ||
                    collider.GetComponentInParent<GuardController>() != null) continue;
                Bounds box = collider.bounds; box.Expand(new Vector3(.6f,.8f,0f));
                if (at.x >= box.min.x && at.x <= box.max.x && at.y >= box.min.y && at.y <= box.max.y)
                { free = false; break; }
            }
            if (!free) continue;
            foreach (Transform feature in features)
            {
                if (feature.gameObject.scene != gameObject.scene) continue;
                string name = feature.gameObject.name;
                if ((name == "OpenPassageThreshold" || name == "AccessKeycard" || name == "Terminal" ||
                    name == "ExtractionZone" || name.StartsWith("Evidence_") || name.StartsWith("SecurityCamera_")) &&
                    Vector2.Distance(at, feature.position) < .9f) { free = false; break; }
            }
            if (!free) continue;
            float distance = 20f;
            foreach (UXPreviewGuardFacingPatrol patrol in patrols)
                if (patrol.gameObject.scene == gameObject.scene) distance = Mathf.Min(distance, patrol.DistanceToRoute(at));
            if (distance > best + .01f) { best = distance; chosen = side; }
        }
        return chosen;
    }

    private void OnDestroy()
    {
        if (pixel == null) return;
        if (Application.isPlaying) Destroy(pixel); else DestroyImmediate(pixel);
    }
}
