using NUnit.Framework;

public sealed class ExpandedStealthTests
{
    [Test]
    public void FullCameraAlertStartsPursuitInsteadOfEndingTheMission()
    {
        var state = new UXStealthState();
        state.Tick(0.8f, false, true, false);
        Assert.AreEqual(UXAlertPhase.Engaged, state.Phase);
        Assert.AreEqual(100, state.Health);
        Assert.IsTrue(state.Alive);
    }

    [Test]
    public void HiddenPlayerCannotRaiseAlert_AndSearchEventuallyClears()
    {
        var state = new UXStealthState();
        state.Tick(2f, true, true, true);
        Assert.AreEqual(UXAlertPhase.Clear, state.Phase);
        state.Tick(2f, true, false, false);
        state.Tick(0.5f, false, false, true);
        Assert.AreEqual(UXAlertPhase.Engaged, state.Phase);
        for (int i = 0; i < 70; i++) state.Tick(0.1f, false, false, true);
        Assert.AreEqual(UXAlertPhase.Clear, state.Phase);
        Assert.AreEqual(100, state.Health);
    }

    [Test]
    public void SimultaneousGuardShotsShareProtection_AndPauseDoesNotConsumeIt()
    {
        var state = new UXStealthState();
        state.Tick(2f, true, false, false);
        Assert.IsTrue(state.TryDamage(25));
        Assert.IsFalse(state.TryDamage(25));
        state.Tick(0f, false, false, false);
        Assert.IsFalse(state.TryDamage(25));
        Assert.AreEqual(75, state.Health);
        for (int i = 0; i < 3; i++)
        {
            state.Tick(0.9f, true, false, false);
            Assert.IsTrue(state.TryDamage(25));
        }
        Assert.IsFalse(state.Alive);
        Assert.IsFalse(state.TryDamage(25));
        Assert.AreEqual(0, state.Health);
    }

    [Test]
    public void ReacquisitionInterruptsSearch_WithoutRestoringHealth()
    {
        var state = new UXStealthState();
        state.Tick(2f, true, false, false);
        state.TryDamage(25);
        state.Tick(2f, false, false, false);
        Assert.AreEqual(UXAlertPhase.Searching, state.Phase);
        state.Tick(0.1f, true, false, false);
        Assert.AreEqual(UXAlertPhase.Engaged, state.Phase);
        Assert.AreEqual(75, state.Health);
    }

    [Test]
    public void NavigationUsesDoorways_AndCannotCrossAClosedGateOrDiagonalCorner()
    {
        bool[] cells = new bool[25];
        for (int y = 0; y < 5; y++) cells[y * 5 + 2] = true;
        var grid = new UXGridPathfinder(5, 5, cells);
        Assert.IsEmpty(grid.Find(0, 24));
        cells[12] = false;
        var route = grid.Find(0, 24);
        CollectionAssert.Contains(route, 12);
        for (int i = 1; i < route.Count; i++)
        {
            int distance = System.Math.Abs(route[i] % 5 - route[i - 1] % 5) +
                System.Math.Abs(route[i] / 5 - route[i - 1] / 5);
            Assert.AreEqual(1, distance);
            Assert.IsFalse(cells[route[i]]);
        }
    }
}
