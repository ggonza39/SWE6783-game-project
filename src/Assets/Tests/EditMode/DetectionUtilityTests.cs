using NUnit.Framework;
using UnityEngine;

public class DetectionUtilityTests
{
    [Test]
    public void GetSightDistance_ClipsToWallSurface()
    {
        var detector = new GameObject("Detector");
        var wall = new GameObject("Wall");
        try
        {
            wall.transform.position = new Vector2(2.5f, 0f);
            wall.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();

            float distance = DetectionUtility.GetSightDistance(
                Vector2.zero, Vector2.right, 6f, detector.transform, new RaycastHit2D[8]);

            Assert.AreEqual(2f, distance, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(detector);
            Object.DestroyImmediate(wall);
        }
    }

    [Test]
    public void HasLineOfSight_FullTriggerBufferStillFindsWall()
    {
        var detector = new GameObject("Detector");
        var wall = new GameObject("Wall");
        var triggers = new GameObject[10];
        bool previousQuerySetting = Physics2D.queriesHitTriggers;
        try
        {
            Physics2D.queriesHitTriggers = true;
            for (int i = 0; i < triggers.Length; i++)
            {
                triggers[i] = new GameObject("Trigger");
                triggers[i].transform.position = new Vector2(0.3f + i * 0.12f, 0f);
                BoxCollider2D collider = triggers[i].AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.05f, 1f);
                collider.isTrigger = true;
            }
            wall.transform.position = new Vector2(3f, 0f);
            wall.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();

            bool clear = DetectionUtility.HasLineOfSight(
                Vector2.zero, new Vector2(5f, 0f), detector.transform, new RaycastHit2D[8]);

            Assert.IsFalse(clear);
        }
        finally
        {
            Physics2D.queriesHitTriggers = previousQuerySetting;
            foreach (GameObject trigger in triggers)
                Object.DestroyImmediate(trigger);
            Object.DestroyImmediate(detector);
            Object.DestroyImmediate(wall);
        }
    }

    [Test]
    public void IsWithinFieldOfView_TargetDirectlyAhead_ReturnsTrue()
    {
        bool result = DetectionUtility.IsWithinFieldOfView(
            origin: Vector2.zero,
            forward: Vector2.right,
            targetPosition: new Vector2(3f, 0f),
            range: 5f,
            fovAngle: 60f,
            distance: out float distance,
            angle: out float angle);

        Assert.IsTrue(result);
        Assert.AreEqual(3f, distance, 0.0001f);
        Assert.AreEqual(0f, angle, 0.0001f);
    }

    [Test]
    public void IsWithinFieldOfView_TargetBehindDetector_ReturnsFalse()
    {
        // Guard facing +X, player directly behind it (-X) should NOT be seen,
        // regardless of being within range. This guards against the facing
        // direction mismatch bug (cone not matching patrol direction).
        bool result = DetectionUtility.IsWithinFieldOfView(
            origin: Vector2.zero,
            forward: Vector2.right,
            targetPosition: new Vector2(-3f, 0f),
            range: 5f,
            fovAngle: 60f,
            distance: out _,
            angle: out float angle);

        Assert.IsFalse(result);
        Assert.AreEqual(180f, angle, 0.0001f);
    }

    [Test]
    public void IsWithinFieldOfView_FacingLeft_DetectsTargetOnLeftSide()
    {
        // When a guard is walking left, its facing direction should be
        // Vector2.left, and a player to the left should be detected.
        bool result = DetectionUtility.IsWithinFieldOfView(
            origin: Vector2.zero,
            forward: Vector2.left,
            targetPosition: new Vector2(-3f, 0f),
            range: 5f,
            fovAngle: 60f,
            distance: out _,
            angle: out _);

        Assert.IsTrue(result);
    }

    [Test]
    public void IsWithinFieldOfView_TargetOutsideRange_ReturnsFalse()
    {
        bool result = DetectionUtility.IsWithinFieldOfView(
            origin: Vector2.zero,
            forward: Vector2.right,
            targetPosition: new Vector2(10f, 0f),
            range: 5f,
            fovAngle: 60f,
            distance: out float distance,
            angle: out _);

        Assert.IsFalse(result);
        Assert.AreEqual(10f, distance, 0.0001f);
    }

    [Test]
    public void IsWithinFieldOfView_TargetOutsideConeAngle_ReturnsFalse()
    {
        // 60 degree cone means +/-30 degrees from forward. 45 degrees off-axis
        // should be rejected.
        Vector2 target = new Vector2(Mathf.Cos(45f * Mathf.Deg2Rad), Mathf.Sin(45f * Mathf.Deg2Rad)) * 3f;

        bool result = DetectionUtility.IsWithinFieldOfView(
            origin: Vector2.zero,
            forward: Vector2.right,
            targetPosition: target,
            range: 5f,
            fovAngle: 60f,
            distance: out _,
            angle: out float angle);

        Assert.IsFalse(result);
        Assert.AreEqual(45f, angle, 0.01f);
    }

    [Test]
    public void HasLineOfSight_NoObstacles_ReturnsTrue()
    {
        var detector = new GameObject("Detector");
        try
        {
            bool losClear = DetectionUtility.HasLineOfSight(
                Vector2.zero, new Vector2(5f, 0f), detector.transform, new RaycastHit2D[8]);

            Assert.IsTrue(losClear);
        }
        finally
        {
            Object.DestroyImmediate(detector);
        }
    }

    [Test]
    public void HasLineOfSight_SolidWallBetween_ReturnsFalse()
    {
        var detector = new GameObject("Detector");
        var wall = new GameObject("Wall");

        try
        {
            wall.transform.position = new Vector2(2.5f, 0f);
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 5f);
            collider.isTrigger = false;
            Physics2D.SyncTransforms();

            bool losClear = DetectionUtility.HasLineOfSight(
                Vector2.zero, new Vector2(5f, 0f), detector.transform, new RaycastHit2D[8]);

            Assert.IsFalse(losClear);
        }
        finally
        {
            Object.DestroyImmediate(detector);
            Object.DestroyImmediate(wall);
        }
    }

    [Test]
    public void HasLineOfSight_TriggerColliderBetween_DoesNotBlock()
    {
        var detector = new GameObject("Detector");
        var hidingSpot = new GameObject("HidingSpotCollider");

        try
        {
            hidingSpot.transform.position = new Vector2(2.5f, 0f);
            var collider = hidingSpot.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 5f);
            collider.isTrigger = true;
            Physics2D.SyncTransforms();

            bool losClear = DetectionUtility.HasLineOfSight(
                Vector2.zero, new Vector2(5f, 0f), detector.transform, new RaycastHit2D[8]);

            Assert.IsTrue(losClear);
        }
        finally
        {
            Object.DestroyImmediate(detector);
            Object.DestroyImmediate(hidingSpot);
        }
    }

    [Test]
    public void HasLineOfSight_IgnoresDetectorsOwnCollider()
    {
        var detector = new GameObject("Detector");

        try
        {
            var selfCollider = detector.AddComponent<BoxCollider2D>();
            selfCollider.size = new Vector2(0.2f, 0.2f);
            selfCollider.isTrigger = false;
            Physics2D.SyncTransforms();

            bool losClear = DetectionUtility.HasLineOfSight(
                detector.transform.position, new Vector2(5f, 0f), detector.transform, new RaycastHit2D[8]);

            Assert.IsTrue(losClear);
        }
        finally
        {
            Object.DestroyImmediate(detector);
        }
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void HasLineOfSight_PlayerColliderDoesNotMaskWalls(bool wallBetween, bool expected)
    {
        var detector = new GameObject("Detector");
        var player = new GameObject("Player");
        var wall = new GameObject("Wall");

        try
        {
            player.transform.position = new Vector2(5f, 0f);
            player.AddComponent<BoxCollider2D>();

            wall.transform.position = new Vector2(2.5f, 0f);
            wall.AddComponent<BoxCollider2D>().enabled = wallBetween;
            Physics2D.SyncTransforms();

            bool losClear = DetectionUtility.HasLineOfSight(
                detector.transform.position, player.transform.position, detector.transform,
                new RaycastHit2D[8], player.transform);

            Assert.AreEqual(expected, losClear);
        }
        finally
        {
            Object.DestroyImmediate(detector);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(wall);
        }
    }
}
