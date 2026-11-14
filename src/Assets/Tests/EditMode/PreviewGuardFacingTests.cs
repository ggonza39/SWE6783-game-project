using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PreviewGuardFacingTests
{
    [TestCase(true, 1f)]
    [TestCase(false, -1f)]
    public void ConeFacing_UsesActivePatrolSource(bool mainPatrolEnabled, float expectedX)
    {
        var guard = new GameObject("Guard");
        try
        {
            GuardController controller = guard.AddComponent<GuardController>();
            controller.enabled = mainPatrolEnabled;
            guard.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            GuardDetection detector = guard.AddComponent<GuardDetection>();
            typeof(GuardDetection).GetField(
                "guardController", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(detector, controller);

            Assert.AreEqual(expectedX, detector.ConeForward.x, 0.001f);
            Assert.AreEqual(0f, detector.ConeForward.y, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(guard);
        }
    }
}
