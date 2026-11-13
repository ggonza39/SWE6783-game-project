using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TerminalInteractionTests
{
    private GameObject gameManagerObject;
    private GameObject evidenceManagerObject;
    private GameObject terminalObject;
    private GameObject cameraObject;
    private MethodInfo tryActivate;

    [SetUp]
    public void SetUp()
    {
        gameManagerObject = new GameObject("TestGameManager");
        TestUtils.InvokeAwake(gameManagerObject.AddComponent<GameManager>());

        evidenceManagerObject = new GameObject("TestEvidenceManager");
        TestUtils.InvokeAwake(evidenceManagerObject.AddComponent<EvidenceManager>());

        terminalObject = new GameObject("TestTerminal");
        terminalObject.AddComponent<TerminalInteraction>();

        cameraObject = new GameObject("TestSecurityCamera");
        cameraObject.AddComponent<SecurityCamera>();

        tryActivate = typeof(TerminalInteraction).GetMethod(
            "TryActivate", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gameManagerObject);
        Object.DestroyImmediate(evidenceManagerObject);
        Object.DestroyImmediate(terminalObject);
        Object.DestroyImmediate(cameraObject);
    }

    private void Activate()
    {
        TerminalInteraction terminal = terminalObject.GetComponent<TerminalInteraction>();
        tryActivate.Invoke(terminal, null);
    }

    [Test]
    public void TwoEvidenceMission_StaysLockedUntilBothPickups()
    {
        TerminalInteraction terminal = terminalObject.GetComponent<TerminalInteraction>();
        typeof(TerminalInteraction).GetField(
            "requiredEvidence", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(terminal, 2);

        EvidenceManager.Instance.CollectEvidence(1);
        Activate();
        Assert.IsFalse(GameManager.Instance.TerminalActivated);
        Assert.IsFalse(cameraObject.GetComponent<SecurityCamera>().IsDisabled);

        EvidenceManager.Instance.CollectEvidence(1);
        Activate();
        Assert.IsTrue(GameManager.Instance.TerminalActivated);
        Assert.IsTrue(cameraObject.GetComponent<SecurityCamera>().IsDisabled);
        Assert.IsTrue(GameManager.Instance.CanExtract(2));
    }

    [Test]
    public void TryActivate_WithoutEvidence_DoesNotActivate()
    {
        Activate();

        Assert.IsFalse(GameManager.Instance.TerminalActivated);
        Assert.IsFalse(cameraObject.GetComponent<SecurityCamera>().IsDisabled);
    }

    [Test]
    public void TryActivate_WithoutEvidence_SetsWarningMessage()
    {
        TerminalInteraction terminal = terminalObject.GetComponent<TerminalInteraction>();

        Activate();

        Assert.AreEqual(TerminalMessageTone.Warning, terminal.LastMessageTone);
        StringAssert.Contains("LOCKED", terminal.LastMessage);
    }

    [Test]
    public void TryActivate_WithEvidence_ActivatesAndDisablesCameras()
    {
        EvidenceManager.Instance.CollectEvidence(1);

        Activate();

        Assert.IsTrue(GameManager.Instance.TerminalActivated);
        Assert.IsTrue(cameraObject.GetComponent<SecurityCamera>().IsDisabled);
    }

    [Test]
    public void TryActivate_WithEvidence_SetsSuccessMessage()
    {
        TerminalInteraction terminal = terminalObject.GetComponent<TerminalInteraction>();
        EvidenceManager.Instance.CollectEvidence(1);

        Activate();

        Assert.AreEqual(TerminalMessageTone.Success, terminal.LastMessageTone);
        StringAssert.Contains("GRANTED", terminal.LastMessage);
    }

    [Test]
    public void TryActivate_CalledTwice_StaysActivatedWithoutError()
    {
        EvidenceManager.Instance.CollectEvidence(1);

        Activate();
        Activate();

        Assert.IsTrue(GameManager.Instance.TerminalActivated);
        Assert.IsTrue(cameraObject.GetComponent<SecurityCamera>().IsDisabled);
    }

    [Test]
    public void TryActivate_CalledTwice_SecondAttemptReportsAlreadyActive()
    {
        TerminalInteraction terminal = terminalObject.GetComponent<TerminalInteraction>();
        EvidenceManager.Instance.CollectEvidence(1);

        Activate();
        Activate();

        Assert.AreEqual(TerminalMessageTone.Info, terminal.LastMessageTone);
        StringAssert.Contains("ALREADY ACTIVE", terminal.LastMessage);
    }

    [Test]
    public void TerminalInteraction_IsNeverDisabledByItself()
    {
        // Regression guard for the UXHUDController integration bug: the HUD
        // used to disable this component entirely to avoid showing its own
        // competing GUI. TerminalInteraction must stay enabled since it is
        // the sole authority for the actual hacking/camera-disable logic.
        TerminalInteraction terminal = terminalObject.GetComponent<TerminalInteraction>();

        Assert.IsTrue(terminal.enabled);

        EvidenceManager.Instance.CollectEvidence(1);
        Activate();

        Assert.IsTrue(terminal.enabled);
    }
}
