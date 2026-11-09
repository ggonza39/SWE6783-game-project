using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool GameEnded { get; private set; }
    public bool PlayerWon { get; private set; }

    /// <summary>True once a terminal has been successfully hacked.</summary>
    public bool TerminalActivated { get; private set; }

    /// <summary>
    /// Current evidence count, mirrored from EvidenceManager so objective
    /// progress (HUD, extraction gating, terminal checks) has a single,
    /// authoritative place to read from.
    /// </summary>
    public int EvidenceCollected =>
        EvidenceManager.Instance != null ? EvidenceManager.Instance.EvidenceCollected : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Single source of truth for "has the player collected enough evidence".
    /// Terminals and the extraction zone should both call this rather than
    /// reaching into EvidenceManager directly, so objective gating always
    /// stays in sync.
    /// </summary>
    public bool HasRequiredEvidence(int requiredAmount)
    {
        return EvidenceManager.Instance != null &&
               EvidenceManager.Instance.HasEvidence(requiredAmount);
    }

    /// <summary>
    /// Single source of truth for "is extraction currently allowed". The
    /// team's UX design requires the security terminal to be hacked in
    /// addition to collecting the required evidence before extraction opens.
    /// </summary>
    public bool CanExtract(int requiredEvidence)
    {
        return HasRequiredEvidence(requiredEvidence) && TerminalActivated;
    }

    /// <summary>Marks the mission's terminal objective as complete.</summary>
    public void ActivateTerminal()
    {
        if (TerminalActivated)
            return;

        TerminalActivated = true;
        Debug.Log("Objective update: Terminal activated.");
    }

    public void WinGame()
    {
        if (GameEnded)
            return;

        GameEnded = true;
        PlayerWon = true;

        //Records if the player won the game
        GameplayMetrics.Instance?.EndSession("Won");

        Debug.Log("MISSION COMPLETE - Player escaped successfully!");
    }

    public void LoseGame()
    {
        if (GameEnded)
            return;

        GameEnded = true;
        PlayerWon = false;

        //Records if the player lost the game
        GameplayMetrics.Instance?.EndSession("Lost");

        Debug.Log("MISSION FAILED - Player was detected!");
    }
}