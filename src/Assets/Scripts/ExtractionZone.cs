using UnityEngine;

public class ExtractionZone : MonoBehaviour
{
    [SerializeField] private int requiredEvidence = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager not found.");
            return;
        }

        if (GameManager.Instance.GameEnded)
            return;

        if (!other.CompareTag("Player"))
            return;

        // Routed through GameManager (rather than EvidenceManager directly)
        // so extraction requirements always stay in sync with the rest of
        // the mission's objective state.
        if (GameManager.Instance.HasRequiredEvidence(requiredEvidence))
        {
            Debug.Log("Extraction successful!");
            GameManager.Instance.WinGame();
        }
        else
        {
            Debug.Log(
                "Extraction locked. Collect the required evidence first."
            );
        }
    }
}
