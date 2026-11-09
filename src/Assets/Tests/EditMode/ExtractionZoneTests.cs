using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ExtractionZoneTests
{
    private GameObject gameManagerObject;
    private GameObject evidenceManagerObject;
    private GameObject extractionZoneObject;
    private GameObject playerObject;
    private MethodInfo onTriggerEnter2D;

    [SetUp]
    public void SetUp()
    {
        gameManagerObject = new GameObject("TestGameManager");
        TestUtils.InvokeAwake(gameManagerObject.AddComponent<GameManager>());

        evidenceManagerObject = new GameObject("TestEvidenceManager");
        TestUtils.InvokeAwake(evidenceManagerObject.AddComponent<EvidenceManager>());

        extractionZoneObject = new GameObject("TestExtractionZone");
        extractionZoneObject.AddComponent<ExtractionZone>();

        playerObject = new GameObject("TestPlayer");
        playerObject.tag = "Player";
        playerObject.AddComponent<BoxCollider2D>();

        onTriggerEnter2D = typeof(ExtractionZone).GetMethod(
            "OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameManagerObject);
        Object.DestroyImmediate(evidenceManagerObject);
        Object.DestroyImmediate(extractionZoneObject);
        Object.DestroyImmediate(playerObject);
    }

    private void EnterExtractionZone()
    {
        ExtractionZone zone = extractionZoneObject.GetComponent<ExtractionZone>();
        onTriggerEnter2D.Invoke(zone, new object[] { playerObject.GetComponent<Collider2D>() });
    }

    [Test]
    public void PlayerEntersWithoutEvidence_DoesNotWinGame()
    {
        EnterExtractionZone();

        Assert.IsFalse(GameManager.Instance.GameEnded);
    }

    [Test]
    public void PlayerEntersWithEvidenceButNoTerminal_DoesNotWinGame()
    {
        // Extraction requires both evidence AND a hacked terminal (team UX
        // requirement from the uxDemoPolish integration) - evidence alone
        // must not be enough.
        EvidenceManager.Instance.CollectEvidence(1);

        EnterExtractionZone();

        Assert.IsFalse(GameManager.Instance.GameEnded);
    }

    [Test]
    public void PlayerEntersWithTerminalButNoEvidence_DoesNotWinGame()
    {
        GameManager.Instance.ActivateTerminal();

        EnterExtractionZone();

        Assert.IsFalse(GameManager.Instance.GameEnded);
    }

    [Test]
    public void PlayerEntersWithEvidenceAndTerminal_WinsGame()
    {
        EvidenceManager.Instance.CollectEvidence(1);
        GameManager.Instance.ActivateTerminal();

        EnterExtractionZone();

        Assert.IsTrue(GameManager.Instance.GameEnded);
        Assert.IsTrue(GameManager.Instance.PlayerWon);
    }

    [Test]
    public void NonPlayerCollider_IsIgnored()
    {
        var decoy = new GameObject("Decoy");
        try
        {
            decoy.AddComponent<BoxCollider2D>();
            ExtractionZone zone = extractionZoneObject.GetComponent<ExtractionZone>();
            onTriggerEnter2D.Invoke(zone, new object[] { decoy.GetComponent<Collider2D>() });

            Assert.IsFalse(GameManager.Instance.GameEnded);
        }
        finally
        {
            Object.DestroyImmediate(decoy);
        }
    }
}
