using UnityEngine;

public class ExtractionZone : MonoBehaviour
{
    [SerializeField] private int requiredEvidence = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        EvidenceManager evidenceManager = EvidenceManager.Instance;

        if (evidenceManager == null)
        {
            Debug.LogWarning("Evidence Manager not found.");
            return;
        }

        if (evidenceManager.HasEvidence(requiredEvidence))
        {
            Debug.Log("Extraction successful!");

            if (GameManager.Instance != null)
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