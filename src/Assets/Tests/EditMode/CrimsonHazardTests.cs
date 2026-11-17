using NUnit.Framework;

public sealed class CrimsonHazardTests
{
    [Test]
    public void MineWarnsBeforeItsSingleDetonation_WithTheRevisedHardReactionMargin()
    {
        var easy = new UXMineState(false); var hard = new UXMineState(true);
        Assert.AreEqual(1.10f, easy.Fuse); Assert.AreEqual(1.10f, hard.Fuse);
        Assert.IsFalse(hard.Tick(.02f, true)); Assert.IsTrue(hard.Armed);
        Assert.IsFalse(hard.Tick(hard.Fuse - .01f, false));
        Assert.IsTrue(hard.Tick(.02f, false)); Assert.IsFalse(hard.Tick(10f, true));
    }

    [Test]
    public void SecuredSectorCancelsAnArmedMine_AndNeutralizesLaserCycles()
    {
        var mine = new UXMineState(true); mine.Tick(.02f, true); mine.Neutralize();
        Assert.IsFalse(mine.Tick(10f, true)); Assert.IsFalse(mine.Armed);
        var laser = new UXLaserState(true, 0f); laser.Tick(2.2f);
        Assert.AreEqual(UXLaserPhase.Active, laser.Phase);
        laser.Neutralize(); laser.Tick(10f); Assert.AreEqual(UXLaserPhase.Safe, laser.Phase);
    }

    [Test]
    public void LaserHasSafeWarningAndActivePhases_AndCatchesAFrameCrossing()
    {
        var laser = new UXLaserState(false, 0f);
        Assert.AreEqual(UXLaserPhase.Safe, laser.Phase);
        laser.Tick(laser.SafeSeconds + .01f); Assert.AreEqual(UXLaserPhase.Warning, laser.Phase);
        laser.Tick(laser.WarningSeconds); Assert.AreEqual(UXLaserPhase.Active, laser.Phase);
        Assert.IsTrue(UXLaserState.Crosses(0f, -2f, 0f, 2f, 0f, 0f, 3.5f, .9f));
        Assert.IsFalse(UXLaserState.Crosses(3f, -2f, 3f, 2f, 0f, 0f, 3.5f, .9f));
    }

    [Test]
    public void HardMineLeavesFifteenHealth_ButLaserKillsThroughRecentHitProtection()
    {
        var state = new UXStealthState();
        Assert.IsTrue(state.TryHazardDamage(85)); Assert.AreEqual(15, state.Health);
        Assert.IsTrue(state.TryLethalHazard()); Assert.AreEqual(0, state.Health);
        Assert.IsFalse(state.TryLethalHazard());
    }

    [Test]
    public void TrapsCanHurtOutsideAlert_ButShareGuardHitProtection()
    {
        var state = new UXStealthState();
        Assert.IsTrue(state.TryHazardDamage(20)); Assert.AreEqual(80, state.Health);
        state.InvestigateNoise(); Assert.IsFalse(state.TryDamage(34));
        state.Tick(.86f, false, false, false); Assert.IsTrue(state.TryDamage(34));
    }
}
