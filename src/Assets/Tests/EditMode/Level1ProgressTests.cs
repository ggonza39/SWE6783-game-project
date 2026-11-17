using System;
using System.IO;
using NUnit.Framework;

public sealed class Level1ProgressTests
{
    private string directory;
    private string path;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "PixelRebellion_ProgressTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "progress.json");
    }

    [TearDown]
    public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [Test]
    public void ReloadKeepsSettingsAndSeparateModeRecords_AndRetainsTheFastestRun()
    {
        var save = new UXProgressStore(path);
        save.Data.difficulty = UXDifficulty.Hard;
        save.Data.fullscreen = false; save.Data.showControlHints = false;
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 180f);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 120f);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 150f);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Hard, 240f);
        Assert.IsTrue(save.Save());
        var loaded = new UXProgressStore(path);
        Assert.AreEqual(UXDifficulty.Hard, loaded.Data.difficulty);
        Assert.IsFalse(loaded.Data.fullscreen); Assert.IsFalse(loaded.Data.showControlHints);
        UXLevelRecord record = loaded.Data.FindLevel("facility_01");
        Assert.AreEqual(3, record.easyClears); Assert.AreEqual(1, record.hardClears);
        Assert.AreEqual(120f, record.bestEasySeconds); Assert.AreEqual(240f, record.bestHardSeconds);
    }

    [Test]
    public void CorruptPrimaryRecoversTheBackup_AndTheNextSaveRepairsThePrimary()
    {
        var save = new UXProgressStore(path);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 120f);
        Assert.IsTrue(save.Save());
        save.Data.difficulty = UXDifficulty.Hard;
        Assert.IsTrue(save.Save());
        File.WriteAllText(path, "{broken json");
        var recovered = new UXProgressStore(path);
        Assert.IsTrue(recovered.Data.FindLevel("facility_01").Completed(UXDifficulty.Easy));
        recovered.Data.RecordCompletion("facility_01", UXDifficulty.Hard, 190f);
        Assert.IsTrue(recovered.Save());
        var reloaded = new UXProgressStore(path);
        Assert.IsTrue(reloaded.Data.FindLevel("facility_01").Completed(UXDifficulty.Hard));
        Assert.IsFalse(reloaded.HasSaveError);
    }

    [Test]
    public void NewerSaveVersionIsNeverReplacedByThisVersion()
    {
        string future = "{\"gameId\":\"PixelRebellion\",\"schemaVersion\":"+(UXPlayerSaveData.CurrentVersion+1)+",\"futureProgress\":123}";
        File.WriteAllText(path, future);
        var save = new UXProgressStore(path);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 100f);
        Assert.IsFalse(save.Save()); Assert.IsTrue(save.HasSaveError);
        Assert.AreEqual(future, File.ReadAllText(path));
    }

    [Test]
    public void UnreadableSaveWithoutBackupIsPreserved()
    {
        const string broken = "{broken json";
        File.WriteAllText(path, broken);
        var save = new UXProgressStore(path);
        Assert.IsFalse(save.Save());
        Assert.AreEqual(broken, File.ReadAllText(path));
    }

    [Test]
    public void WriteFailureKeepsTheCurrentRunDataAvailable()
    {
        Directory.CreateDirectory(path);
        var save = new UXProgressStore(path);
        save.Data.RecordCompletion("facility_01", UXDifficulty.Easy, 110f);
        Assert.IsFalse(save.Save()); Assert.IsTrue(save.HasSaveError);
        Assert.IsTrue(save.Data.FindLevel("facility_01").Completed(UXDifficulty.Easy));
        Assert.IsTrue(Directory.Exists(path));
    }

    [Test]
    public void IdenticalCameraExposureAlertsHardBeforeEasy_WhileHitsRemainProtected()
    {
        var easy = new UXStealthState(UXDifficultyProfile.For(UXDifficulty.Easy).AlertGrowth);
        var hard = new UXStealthState(UXDifficultyProfile.For(UXDifficulty.Hard).AlertGrowth);
        easy.Tick(0.8f, false, true, false); hard.Tick(0.8f, false, true, false);
        Assert.AreEqual(UXAlertPhase.Suspicious, easy.Phase);
        Assert.AreEqual(UXAlertPhase.Engaged, hard.Phase);
        Assert.IsTrue(hard.TryDamage(25)); Assert.IsFalse(hard.TryDamage(25));
        Assert.AreEqual(75, hard.Health);
    }

    [Test]
    public void NewHardPresetTakesThreeSeparatedHitsToFail_AndKeepsSharedProtection()
    {
        UXDifficultyProfile hard = UXDifficultyProfile.For(UXDifficulty.Hard);
        var state = new UXStealthState(hard.AlertGrowth,hard.SearchPatience);
        state.Tick(1f,true,false,false);
        Assert.IsTrue(state.TryDamage(hard.HitDamage));
        Assert.AreEqual(66,state.Health);
        Assert.IsFalse(state.TryDamage(hard.HitDamage));
        state.Tick(hard.ShotCooldown,true,false,false);
        Assert.IsTrue(state.TryDamage(hard.HitDamage));
        Assert.AreEqual(32,state.Health);
        state.Tick(hard.ShotCooldown,true,false,false);
        Assert.IsTrue(state.TryDamage(hard.HitDamage));
        Assert.IsFalse(state.Alive);
    }

    [Test]
    public void HardSearchLastsLonger_AndStillClearsWhenHidden()
    {
        UXDifficultyProfile profile = UXDifficultyProfile.For(UXDifficulty.Hard);
        var hard = new UXStealthState(profile.AlertGrowth,profile.SearchPatience);
        var easy = new UXStealthState(UXDifficultyProfile.For(UXDifficulty.Easy).AlertGrowth);
        hard.Tick(2f,true,false,false); easy.Tick(2f,true,false,false);
        for(int i=0;i<60;i++) {hard.Tick(.1f,false,false,true);easy.Tick(.1f,false,false,true);}
        Assert.AreEqual(UXAlertPhase.Clear,easy.Phase);
        Assert.AreEqual(UXAlertPhase.Searching,hard.Phase);
        for(int i=0;i<60;i++) hard.Tick(.1f,false,false,true);
        Assert.AreEqual(UXAlertPhase.Clear,hard.Phase);
    }
}
