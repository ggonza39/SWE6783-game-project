
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TerminalInteraction : MonoBehaviour
{
    [SerializeField] private int requiredEvidence = 1;
    private bool playerNearby;
    private string message = "";
    private float messageUntil;

    private void Update()
    {
        if (!playerNearby) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded) return;

        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null &&
                  Keyboard.current.eKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pressed |= Input.GetKeyDown(KeyCode.E);
#endif

        if (pressed)
        {
            var manager = EvidenceManager.Instance;

            if (manager == null)
                ShowMessage("Evidence Manager not found.");
            else if (manager.HasEvidence(requiredEvidence))
                ShowMessage("TERMINAL ACCESS GRANTED");
            else
                ShowMessage("TERMINAL LOCKED - Collect evidence first.");
        }
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.time + 4f;
        Debug.Log(text);
    }

    private void OnGUI()
    {
        if (Time.time > messageUntil || string.IsNullOrEmpty(message))
            return;

        GUI.Box(new Rect(Screen.width / 2f - 200, 30, 400, 50),
                message);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            ShowMessage("Press E to access terminal");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerNearby = false;
    }
}
