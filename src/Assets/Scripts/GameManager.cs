using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool GameEnded { get; private set; }
    public bool PlayerWon { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void WinGame()
    {
        if (GameEnded)
            return;

        GameEnded = true;
        PlayerWon = true;

        Debug.Log("MISSION COMPLETE - Player escaped successfully!");
    }

    public void LoseGame()
    {
        if (GameEnded)
            return;

        GameEnded = true;
        PlayerWon = false;

        Debug.Log("MISSION FAILED - Player was detected!");
    }
}