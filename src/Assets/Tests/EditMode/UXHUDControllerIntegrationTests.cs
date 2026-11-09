using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Regression coverage for the UXHUDController / TerminalInteraction
/// integration bug: the HUD previously disabled TerminalInteraction
/// entirely (to avoid a competing OnGUI popup) and managed extraction
/// gating itself, which silently broke the terminal's camera-disable
/// effect. These tests verify the HUD now only reads gameplay state
/// instead of bypassing or duplicating it.
/// </summary>
public class UXHUDControllerIntegrationTests
{
    private GameObject gameManagerObject;
    private GameObject evidenceManagerObject;
    private GameObject playerObject;
    private GameObject terminalObject;
    private GameObject extractionZoneObject;
    private GameObject hudObject;

    [SetUp]
    public void SetUp()
    {
        gameManagerObject = new GameObject("TestGameManager");
        TestUtils.InvokeAwake(gameManagerObject.AddComponent<GameManager>());

        evidenceManagerObject = new GameObject("TestEvidenceManager");
        TestUtils.InvokeAwake(evidenceManagerObject.AddComponent<EvidenceManager>());

        playerObject = new GameObject("Player");
        playerObject.tag = "Player";

        // UXHUDController finds these by exact name, matching the scene setup.
        terminalObject = new GameObject("Terminal");
        terminalObject.AddComponent<BoxCollider2D>().isTrigger = true;
        terminalObject.AddComponent<TerminalInteraction>();

        extractionZoneObject = new GameObject("ExtractionZone");
        extractionZoneObject.AddComponent<BoxCollider2D>().isTrigger = true;
        extractionZoneObject.AddComponent<ExtractionZone>();

        hudObject = new GameObject("TestHUD");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameManagerObject);
        Object.DestroyImmediate(evidenceManagerObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(terminalObject);
        Object.DestroyImmediate(extractionZoneObject);
        Object.DestroyImmediate(hudObject);
    }

    private UXHUDController CreateAndInitializeHud()
    {
        UXHUDController hud = hudObject.AddComponent<UXHUDController>();
        TestUtils.InvokeAwake(hud);
        TestUtils.InvokeStart(hud);
        return hud;
    }

    [Test]
    public void InitializingHud_DoesNotDisableTerminalInteraction()
    {
        CreateAndInitializeHud();

        TerminalInteraction terminalInteraction = terminalObject.GetComponent<TerminalInteraction>();
        Assert.IsTrue(terminalInteraction.enabled,
            "UXHUDController must not disable TerminalInteraction - it is the sole authority for terminal hacking.");
    }

    [Test]
    public void InitializingHud_DoesNotDisableExtractionZoneCollider()
    {
        CreateAndInitializeHud();

        Collider2D extractionCollider = extractionZoneObject.GetComponent<Collider2D>();
        Assert.IsTrue(extractionCollider.enabled,
            "ExtractionZone's trigger must stay enabled so it can report locked/unlocked state itself; " +
            "gating is handled by GameManager.CanExtract(), not by toggling the collider.");
    }

    [Test]
    public void TerminalActivation_IsSynchronizedThroughGameManager()
    {
        CreateAndInitializeHud();

        // Simulate a real terminal hack via TerminalInteraction (the sole
        // authority), not via the HUD.
        EvidenceManager.Instance.CollectEvidence(1);
        var tryActivate = typeof(TerminalInteraction).GetMethod(
            "TryActivate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        tryActivate.Invoke(terminalObject.GetComponent<TerminalInteraction>(), null);

        // GameManager is the single source of truth both TerminalInteraction
        // and UXHUDController read from - verify it reflects the hack.
        Assert.IsTrue(GameManager.Instance.TerminalActivated);
        Assert.IsTrue(GameManager.Instance.CanExtract(1));
    }
}
