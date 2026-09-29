using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }

    public int EvidenceCollected { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void CollectEvidence(int amount)
    {
        EvidenceCollected += amount;

        Debug.Log(
            "Evidence collected: " + EvidenceCollected
        );
    }

    public bool HasEvidence(int requiredAmount)
    {
        return EvidenceCollected >= requiredAmount;
    }
}