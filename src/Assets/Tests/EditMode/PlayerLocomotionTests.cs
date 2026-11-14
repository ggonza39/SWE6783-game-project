using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class PlayerLocomotionTests
{
    private GameObject player;
    private PlayerController controller;
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        player = new GameObject("LocomotionTestPlayer");
        controller = player.AddComponent<PlayerController>();
        TestUtils.InvokeAwake(controller);
        typeof(PlayerController).GetField("defaultSneakStance",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(player);
        Time.timeScale = previousTimeScale;
    }

    [TestCase(false, false, 5f)]
    [TestCase(false, true, 8.75f)]
    [TestCase(true, false, 2.25f)]
    [TestCase(true, true, 2.25f)]
    public void MovementSpeed_PreservesWalkAndSprint_AndPrioritizesCarefulSteps(
        bool careful, bool sprint, float expectedSpeed)
    {
        SetMode("IsMovingCarefully", careful);
        SetMode("IsSprinting", sprint);
        Assert.AreEqual(expectedSpeed, controller.CurrentMoveSpeed, 0.001f);
    }

    [Test]
    public void Pausing_ClearsMovementWhileKeepingTheDefaultCrouch()
    {
        SetMode("IsMovingCarefully", true);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.linearVelocity = new Vector2(2f, 1f);
        typeof(PlayerController).GetField("movement",
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, Vector2.right);
        Time.timeScale = 0f;

        typeof(PlayerController).GetMethod("Update",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);

        Assert.IsTrue(controller.IsSneaking);
        Assert.IsFalse(controller.IsMovingCarefully);
        Assert.IsFalse(controller.IsSprinting);
        Assert.AreEqual(Vector2.zero, body.linearVelocity);
        Assert.AreEqual(Vector2.zero, typeof(PlayerController).GetField("movement",
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller));
    }

    [Test]
    public void Sneaking_StillRequiresCoverToBeHidden()
    {
        PlayerHiding hiding = player.AddComponent<PlayerHiding>();
        Assert.IsFalse(hiding.IsHidden,
            "Sneak posture must not bypass line of sight or grant invisibility.");
    }

    [Test]
    public void DefaultMovement_CrouchesWithoutAnExtraHeldKey()
    {
        Assert.IsTrue(controller.IsSneaking);
        Assert.IsFalse(controller.IsMovingCarefully);
        SetMode("IsSprinting", true);
        Assert.IsFalse(controller.IsSneaking);
    }

    [TestCase(0.15311004f, 0.15311004f)]
    [TestCase(2f, 3f)]
    public void PreviewFootprint_FitsPassagesAfterLegacySpriteResize(float scaleX, float scaleY)
    {
        var north = new GameObject("PassageNorth");
        var south = new GameObject("PassageSouth");
        try
        {
            north.transform.position = new Vector3(0f, 1f, 0f);
            south.transform.position = new Vector3(0f, -1f, 0f);
            BoxCollider2D northWall = north.AddComponent<BoxCollider2D>();
            BoxCollider2D southWall = south.AddComponent<BoxCollider2D>();
            northWall.size = southWall.size = new Vector2(1f, 0.98f);
            player.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            BoxCollider2D body = player.GetComponent<BoxCollider2D>();
            body.size = new Vector2(0.9f / scaleX, 1.5f / scaleY);
            Physics2D.SyncTransforms();
            Assert.IsTrue(Physics2D.Distance(body, northWall).isOverlapped);

            typeof(PlayerController).GetMethod("ConfigurePreviewCollider",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
            Physics2D.SyncTransforms();

            Assert.AreEqual(0.5f, body.bounds.size.x, 0.001f);
            Assert.AreEqual(0.7f, body.bounds.size.y, 0.001f);
            Assert.IsFalse(Physics2D.Distance(body, northWall).isOverlapped);
            Assert.IsFalse(Physics2D.Distance(body, southWall).isOverlapped);
        }
        finally
        {
            Object.DestroyImmediate(north);
            Object.DestroyImmediate(south);
        }
    }

    private void SetMode(string property, bool value)
    {
        typeof(PlayerController).GetProperty(property).SetValue(controller, value);
    }
}
