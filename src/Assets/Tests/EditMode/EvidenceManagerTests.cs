using NUnit.Framework;
using UnityEngine;

public class EvidenceManagerTests
{
    private GameObject evidenceManagerObject;

    [SetUp]
    public void SetUp()
    {
        evidenceManagerObject = new GameObject("TestEvidenceManager");
        TestUtils.InvokeAwake(evidenceManagerObject.AddComponent<EvidenceManager>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(evidenceManagerObject);
    }

    [Test]
    public void CollectEvidence_IncrementsCount()
    {
        EvidenceManager.Instance.CollectEvidence(1);
        EvidenceManager.Instance.CollectEvidence(2);

        Assert.AreEqual(3, EvidenceManager.Instance.EvidenceCollected);
    }

    [Test]
    public void HasEvidence_FalseBelowThreshold()
    {
        EvidenceManager.Instance.CollectEvidence(1);

        Assert.IsFalse(EvidenceManager.Instance.HasEvidence(2));
    }

    [Test]
    public void HasEvidence_TrueAtOrAboveThreshold()
    {
        EvidenceManager.Instance.CollectEvidence(2);

        Assert.IsTrue(EvidenceManager.Instance.HasEvidence(2));
        Assert.IsTrue(EvidenceManager.Instance.HasEvidence(1));
    }
}
