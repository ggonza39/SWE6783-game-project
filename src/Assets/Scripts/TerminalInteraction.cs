
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TerminalInteraction : MonoBehaviour
{
    [SerializeField] private int requiredEvidence = 1;

    private bool playerNearby;

    private void Update()
    {
        if (!playerNearby)
            return;

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
            return;

        bool ePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            ePressed = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E))
        {
            ePressed = true;
        }
#endif

        if (ePressed)
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
            Debug.Log("Terminal locked. Collect more evidence first.");
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
