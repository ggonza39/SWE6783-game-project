using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SolidHidingCoverTests
{
    private Scene previousScene, testScene;
    private GameObject owner, bay, player;
    private UXSolidHidingCover cover;
    private PlayerHiding hiding;

    [SetUp]
    public void SetUp()
    {
        previousScene = SceneManager.GetActiveScene();
        testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(testScene);
        owner = new GameObject("SolidCoverTestOwner");
        bay = new GameObject("HidingSpot"); bay.tag = "HidingSpot";
        bay.AddComponent<BoxCollider2D>().isTrigger = true;
        player = new GameObject("SolidCoverTestPlayer");
        BoxCollider2D body = player.AddComponent<BoxCollider2D>(); body.size = new Vector2(.5f,.7f);
        TestUtils.InvokeAwake(player.AddComponent<PlayerController>());
        hiding = player.AddComponent<PlayerHiding>();
        UXSolidHidingCover.ConfigureScene(owner,body);
        cover = bay.GetComponent<UXSolidHidingCover>();
        hiding.ConfigureBays(new [] {cover});
        player.transform.position = bay.transform.position;
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void TearDown()
    {
        SceneManager.SetActiveScene(previousScene);
        EditorSceneManager.CloseScene(testScene, true);
    }

    [Test]
    public void PlayerCanEnterInterior_WhileGuardExclusionRemainsSolid()
    {
        BoxCollider2D solid = bay.GetComponent<BoxCollider2D>();
        Assert.IsFalse(solid.isTrigger);
        Assert.IsTrue(cover.Pocket.isTrigger);
        Assert.IsTrue(Physics2D.GetIgnoreCollision(player.GetComponent<BoxCollider2D>(), solid));
        var guardBody = new GameObject("TestGuard").AddComponent<BoxCollider2D>();
        Assert.IsFalse(Physics2D.GetIgnoreCollision(guardBody, solid));
        Assert.IsTrue(hiding.IsHidden);
        Assert.AreSame(cover,hiding.ActiveBay);
        Assert.IsFalse(DetectionUtility.HasLineOfSight(new Vector2(-2,0),new Vector2(2,0),null,new RaycastHit2D[16]));
    }

    [Test]
    public void EntryHasNoPlayerCollision_AndFrameStillBlocksTheSidesAndBack()
    {
        BoxCollider2D body = player.GetComponent<BoxCollider2D>();
        BoxCollider2D solid = bay.GetComponent<BoxCollider2D>();
        BoxCollider2D[] parts = bay.GetComponentsInChildren<BoxCollider2D>();
        for (int i = 0; i <= 30; i++)
        {
            player.transform.position = Vector2.Lerp(cover.Entrance,bay.transform.position,i/30f);
            Physics2D.SyncTransforms();
            foreach (BoxCollider2D part in parts)
                if (part != solid && !part.isTrigger)
                {
                    Assert.IsFalse(Physics2D.GetIgnoreCollision(body,part));
                    Assert.IsFalse(Physics2D.Distance(body,part).isOverlapped,"The open mouth must fit the whole player.");
                }
        }
    }

    [Test]
    public void HiddenStateRequiresEntering_AndClearsImmediatelyOnExit()
    {
        player.transform.position = cover.Entrance;
        Assert.IsFalse(hiding.IsHidden);
        player.transform.position = bay.transform.position;
        Assert.IsTrue(hiding.IsHidden);
        player.transform.position = cover.Entrance;
        Assert.IsFalse(hiding.IsHidden);
    }

    [Test]
    public void RepeatedSetup_DoesNotDuplicateInteriorOrFrames()
    {
        BoxCollider2D pocket = cover.Pocket;
        UXSolidHidingCover.ConfigureScene(owner,player.GetComponent<BoxCollider2D>());
        Assert.AreSame(pocket,cover.Pocket);
        Assert.AreEqual(5,bay.GetComponentsInChildren<BoxCollider2D>().Length);
    }

    [Test]
    public void GuardMovement_RejectsBayInterior_ButFindsRouteAroundIt()
    {
        UXStealthDirector director = owner.AddComponent<UXStealthDirector>();
        typeof(UXStealthDirector).GetField("playerBody",BindingFlags.Instance|BindingFlags.NonPublic)
            .SetValue(director,player.GetComponent<BoxCollider2D>());
        typeof(UXStealthDirector).GetMethod("RebuildNavigation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(director,null);
        var guard = new GameObject("SolidCoverTestGuard"); guard.transform.position = new Vector2(-2,0);
        Assert.IsFalse(director.TryMoveGuard(guard.transform,Vector2.zero));
        var path = director.Path(guard.transform.position,new Vector2(2,0));
        Assert.Greater(path.Count,0);
        foreach (Vector2 next in path) Assert.IsTrue(director.TryMoveGuard(guard.transform,next));
        Assert.Less(Vector2.Distance(guard.transform.position,new Vector2(2,0)),.1f);
    }
}
