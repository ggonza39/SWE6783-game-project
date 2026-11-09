
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TerminalInteraction : MonoBehaviour
{
    [SerializeField] private int requiredEvidence = 1;

    [Tooltip("Cameras this terminal disables once hacked. Leave empty to " +
        "auto-disable every SecurityCamera currently in the scene.")]
    [SerializeField] private SecurityCamera[] camerasToDisable;

    private bool playerNearby;
    private bool activated;
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
            TryActivate();
    }

    private void TryActivate()
    {
        if (activated)
        {
            ShowMessage("TERMINAL ALREADY ACTIVE");
            return;
        }

        bool hasEvidence = GameManager.Instance != null
            ? GameManager.Instance.HasRequiredEvidence(requiredEvidence)
            : EvidenceManager.Instance != null && EvidenceManager.Instance.HasEvidence(requiredEvidence);

        if (!hasEvidence)
        {
            ShowMessage("TERMINAL LOCKED - Collect evidence first.");
            return;
        }

        activated = true;
        GameManager.Instance?.ActivateTerminal();
        DisableLinkedCameras();
        ShowMessage("TERMINAL ACCESS GRANTED - Security camera disabled.");
    }

    private void DisableLinkedCameras()
    {
        SecurityCamera[] targets = (camerasToDisable != null && camerasToDisable.Length > 0)
            ? camerasToDisable
            : FindObjectsByType<SecurityCamera>(FindObjectsSortMode.None);

        foreach (SecurityCamera camera in targets)
        {
            if (camera != null)
                camera.SetDisabled(true);
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
