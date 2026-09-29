using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    public bool IsHidden { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            IsHidden = true;
            Debug.Log("Player is hidden.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("HidingSpot"))
        {
            IsHidden = false;
            Debug.Log("Player left hiding spot.");
        }
    }
}