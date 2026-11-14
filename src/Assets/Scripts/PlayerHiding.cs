using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    // Tracks how many hiding-spot triggers currently overlap the player so
    // leaving one spot doesn't break hiding while still standing in another.
    private int overlappingHidingSpots;
    private UXSolidHidingCover[] bays;
    private bool wasInBay;

    public UXSolidHidingCover ActiveBay
    {
        get
        {
            if (bays != null)
                foreach (UXSolidHidingCover bay in bays)
                    if (bay != null && bay.gameObject.scene == gameObject.scene && bay.ContainsPlayer(transform.position)) return bay;
            return null;
        }
    }

    public bool IsHidden => bays != null ? ActiveBay != null : overlappingHidingSpots > 0;

    public void ConfigureBays(UXSolidHidingCover[] sceneBays)
    {
        bays = sceneBays != null && sceneBays.Length > 0 ? sceneBays : null;
    }

    private void Update()
    {
        if (bays == null) return;
        bool hidden = IsHidden;
        if (hidden && !wasInBay)
        {
            GameplayMetrics.Instance?.RecordHiding();
            Debug.Log("Player is hidden inside a hiding bay.");
        }
        else if (!hidden && wasInBay) Debug.Log("Player left hiding bay.");
        wasInBay = hidden;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (bays != null) return; // Expanded bays use the player's center, not a grazing trigger contact.
        if (other.CompareTag("HidingSpot"))
        {
            overlappingHidingSpots++;

            //Records if player is hidden
            GameplayMetrics.Instance?.RecordHiding();

            Debug.Log("Player is hidden.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (bays != null) return;
        if (other.CompareTag("HidingSpot"))
        {
            overlappingHidingSpots = Mathf.Max(0, overlappingHidingSpots - 1);

            if (overlappingHidingSpots == 0)
                Debug.Log("Player left hiding spot.");
        }
    }
}
