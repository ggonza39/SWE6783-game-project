using UnityEngine;

public class EvidenceCollectible : MonoBehaviour
{
    [SerializeField] private int evidenceValue = 1;
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (GetComponent<UXExpandedInteraction>() != null) return;
        Collect();
    }

    public void Collect()
    {
        if (collected || (GameManager.Instance != null && GameManager.Instance.GameEnded)) return;

        EvidenceManager manager = FindFirstObjectByType<EvidenceManager>();

        if (manager != null)
        {
            collected = true;
            manager.CollectEvidence(evidenceValue);
            Debug.Log("Evidence collected.");
            Destroy(gameObject);
        }
    }
}
