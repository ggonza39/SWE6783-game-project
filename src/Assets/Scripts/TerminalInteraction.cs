
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum TerminalMessageTone
{
    Info,
    Success,
    Warning
}

public class TerminalInteraction : MonoBehaviour
{
    [SerializeField] private int requiredEvidence = 1;

    [Tooltip("Cameras this terminal disables once hacked. Leave empty to " +
        "auto-disable every SecurityCamera currently in the scene.")]
    [SerializeField] private SecurityCamera[] camerasToDisable;

    private bool playerNearby;
    private bool activated;
    public bool PlayerNearby => playerNearby;
    public bool IsActivated => activated;

    // Terminal hacking logic lives here; on-screen presentation (toasts,
    // prompts, the HUD) is owned by UXHUDController, which reads these
    // instead of this component drawing its own competing GUI.
    public string LastMessage { get; private set; } = "";
    public TerminalMessageTone LastMessageTone { get; private set; } = TerminalMessageTone.Info;
    public float LastMessageTime { get; private set; } = -1f;

    private void Update()
    {
        if (GetComponent<UXExpandedInteraction>() != null) return;
        if (Time.timeScale <= 0f || !playerNearby) return;

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
        //Records every E-key terminal attempt
        GameplayMetrics.Instance?.RecordTerminalInteraction();

        if (activated)
        {
            Notify("TERMINAL ALREADY ACTIVE", TerminalMessageTone.Info);
            return;
        }

        bool hasEvidence = GameManager.Instance != null
            ? GameManager.Instance.HasRequiredEvidence(requiredEvidence)
            : EvidenceManager.Instance != null && EvidenceManager.Instance.HasEvidence(requiredEvidence);

        if (!hasEvidence)
        {
            Notify("TERMINAL LOCKED - Collect evidence first.", TerminalMessageTone.Warning);
            return;
        }

        activated = true;
        GameManager.Instance?.ActivateTerminal();
        DisableLinkedCameras();
        Notify("TERMINAL ACCESS GRANTED - Security camera disabled.", TerminalMessageTone.Success);
    }

    public void ActivateFromDownload()
    {
        if (Time.timeScale > 0f && playerNearby &&
            (GameManager.Instance == null || !GameManager.Instance.GameEnded)) TryActivate();
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

    // Updates the public, HUD-readable message state. Only called for
    // meaningful one-shot outcomes (already-active / locked / granted),
    // not the ambient "press E" proximity hint, which the HUD already
    // shows continuously via its own bottom-bar prompt.
    private void Notify(string text, TerminalMessageTone tone)
    {
        LastMessage = text;
        LastMessageTone = tone;
        LastMessageTime = Time.time;
        Debug.Log(text);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerNearby = false;
    }
}
