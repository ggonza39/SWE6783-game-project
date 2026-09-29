using UnityEngine;

public class TerminalInteraction : MonoBehaviour
{
    [SerializeField] private int requiredEvidence = 1;
    private bool playerNearby;

    private void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            InteractWithTerminal();
        }
    }

    private void InteractWithTerminal()
    {
        EvidenceManager manager = EvidenceManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning("Evidence Manager not found.");
            return;
        }

        if (manager.HasEvidence(requiredEvidence))
        {
            Debug.Log("Terminal accessed successfully.");
        }
        else
        {
            Debug.Log(
                "Terminal locked. Collect more evidence first."
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            Debug.Log("Press E to access terminal.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
        }
    }
}