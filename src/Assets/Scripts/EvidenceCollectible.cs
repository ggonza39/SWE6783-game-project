using UnityEngine;

public class EvidenceCollectible : MonoBehaviour
{
    [SerializeField] private int evidenceValue = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        EvidenceManager manager = FindFirstObjectByType<EvidenceManager>();

        if (manager != null)
        {
            manager.CollectEvidence(evidenceValue);
            Debug.Log("Evidence collected.");
            Destroy(gameObject);
        }
    }
}