using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    // Tracks how many hiding-spot triggers currently overlap the player so
    // leaving one spot doesn't break hiding while still standing in another.
    private int overlappingHidingSpots;

    public bool IsHidden => overlappingHidingSpots > 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            overlappingHidingSpots++;
            Debug.Log("Player is hidden.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            overlappingHidingSpots = Mathf.Max(0, overlappingHidingSpots - 1);

            if (overlappingHidingSpots == 0)
                Debug.Log("Player left hiding spot.");
        }
    }
}