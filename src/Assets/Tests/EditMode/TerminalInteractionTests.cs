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
    public void TryActivate_WithoutEvidence_DoesNotActivate()
    {
        Activate();

        Assert.IsFalse(GameManager.Instance.TerminalActivated);
        Assert.IsFalse(cameraObject.GetComponent<SecurityCamera>().IsDisabled);
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
    public void TryActivate_CalledTwice_StaysActivatedWithoutError()
    {
        EvidenceManager.Instance.CollectEvidence(1);

        Activate();
        Activate();

        Assert.IsTrue(GameManager.Instance.TerminalActivated);
        Assert.IsTrue(cameraObject.GetComponent<SecurityCamera>().IsDisabled);
    }
}
