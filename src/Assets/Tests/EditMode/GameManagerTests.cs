using NUnit.Framework;
using UnityEngine;

public class GameManagerTests
{
    private GameObject gameManagerObject;
    private GameObject evidenceManagerObject;

    [SetUp]
    public void SetUp()
    {
        gameManagerObject = new GameObject("TestGameManager");
        TestUtils.InvokeAwake(gameManagerObject.AddComponent<GameManager>());

        evidenceManagerObject = new GameObject("TestEvidenceManager");
        TestUtils.InvokeAwake(evidenceManagerObject.AddComponent<EvidenceManager>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameManagerObject);
        Object.DestroyImmediate(evidenceManagerObject);
    }

    [Test]
    public void WinGame_SetsGameEndedAndPlayerWon()
    {
        GameManager.Instance.WinGame();

        Assert.IsTrue(GameManager.Instance.GameEnded);
        Assert.IsTrue(GameManager.Instance.PlayerWon);
    }

    [Test]
    public void LoseGame_SetsGameEndedAndNotPlayerWon()
    {
        GameManager.Instance.LoseGame();

        Assert.IsTrue(GameManager.Instance.GameEnded);
        Assert.IsFalse(GameManager.Instance.PlayerWon);
    }

    [Test]
    public void LoseGame_AfterWinGame_DoesNotOverwriteOutcome()
    {
        // Mission state must be locked in once the game ends - a guard
        // detecting the player one frame after extraction should not flip
        // a win into a loss.
        GameManager.Instance.WinGame();
        GameManager.Instance.LoseGame();

        Assert.IsTrue(GameManager.Instance.PlayerWon);
    }

    [Test]
    public void HasRequiredEvidence_ReflectsEvidenceManagerState()
    {
        Assert.IsFalse(GameManager.Instance.HasRequiredEvidence(1));

        EvidenceManager.Instance.CollectEvidence(1);

        Assert.IsTrue(GameManager.Instance.HasRequiredEvidence(1));
    }

    [Test]
    public void EvidenceCollected_PassesThroughFromEvidenceManager()
    {
        EvidenceManager.Instance.CollectEvidence(2);

        Assert.AreEqual(2, GameManager.Instance.EvidenceCollected);
    }

    [Test]
    public void ActivateTerminal_SetsTerminalActivated()
    {
        Assert.IsFalse(GameManager.Instance.TerminalActivated);

        GameManager.Instance.ActivateTerminal();

        Assert.IsTrue(GameManager.Instance.TerminalActivated);
    }

    [Test]
    public void ActivateTerminal_IsIdempotent()
    {
        GameManager.Instance.ActivateTerminal();
        GameManager.Instance.ActivateTerminal();

        Assert.IsTrue(GameManager.Instance.TerminalActivated);
    }

    [Test]
    public void CanExtract_FalseWithNeitherEvidenceNorTerminal()
    {
        Assert.IsFalse(GameManager.Instance.CanExtract(1));
    }

    [Test]
    public void CanExtract_FalseWithEvidenceOnly()
    {
        EvidenceManager.Instance.CollectEvidence(1);

        Assert.IsFalse(GameManager.Instance.CanExtract(1));
    }

    [Test]
    public void CanExtract_FalseWithTerminalOnly()
    {
        GameManager.Instance.ActivateTerminal();

        Assert.IsFalse(GameManager.Instance.CanExtract(1));
    }

    [Test]
    public void CanExtract_TrueWithBothEvidenceAndTerminal()
    {
        EvidenceManager.Instance.CollectEvidence(1);
        GameManager.Instance.ActivateTerminal();

        Assert.IsTrue(GameManager.Instance.CanExtract(1));
    }
}
