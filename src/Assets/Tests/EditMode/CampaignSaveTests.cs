using System;
using System.IO;
using NUnit.Framework;

public sealed class CampaignSaveTests
{
    private string directory;
    private string path;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "PixelRebellion_CampaignTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "progress.json");
    }

    [TearDown]
    public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

    [Test]
    public void VersionOneClearUnlocksOnlyItsMode_AndPreservesRecordsSettingsAndBackup()
    {
        const string legacy = "{\"gameId\":\"PixelRebellion\",\"schemaVersion\":1,\"difficulty\":1," +
            "\"showControlHints\":false,\"fullscreen\":false,\"levels\":[{\"levelId\":\"facility_01\"," +
            "\"easyClears\":0,\"hardClears\":2,\"bestHardSeconds\":193}]}";
        File.WriteAllText(path, legacy);
        var store = new UXProgressStore(path);
        Assert.AreEqual(UXPlayerSaveData.CurrentVersion, store.Data.schemaVersion);
        Assert.AreEqual(UXCampaign.CrimsonLevelId, store.Data.FindCampaign(UXDifficulty.Hard).levelId);
        Assert.IsNull(store.Data.FindCampaign(UXDifficulty.Easy));
        Assert.AreEqual(193f, store.Data.FindLevel(UXPlayerProgress.FacilityLevelId).bestHardSeconds);
        Assert.IsFalse(store.Data.showControlHints); Assert.IsFalse(store.Data.fullscreen);
        Assert.IsTrue(store.Save()); Assert.AreEqual(legacy, File.ReadAllText(path + ".bak"));
    }

    [Test]
    public void CheckpointClockAndDetectionsRoundTrip_WithoutChangingOtherMode()
    {
        var store = new UXProgressStore(path);
        store.Data.campaigns.Add(new UXCampaignSlot { mode = UXDifficulty.Hard,
            levelId = UXCampaign.CrimsonLevelId, checkpoint = 2, seconds = 321.5f, detections = 9 });
        store.Data.campaigns.Add(new UXCampaignSlot { mode = UXDifficulty.Easy });
        Assert.IsTrue(store.Save());
        var loaded = new UXProgressStore(path);
        var hard = loaded.Data.FindCampaign(UXDifficulty.Hard);
        Assert.AreEqual(2, hard.checkpoint); Assert.AreEqual(321.5f, hard.seconds); Assert.AreEqual(9, hard.detections);
        Assert.AreEqual(UXPlayerProgress.FacilityLevelId, loaded.Data.FindCampaign(UXDifficulty.Easy).levelId);
    }

    [Test]
    public void InvalidCheckpointOrDuplicateModeIsRejected()
    {
        Assert.IsFalse(new UXCampaignSlot { checkpoint = 1 }.Validate());
        Assert.IsFalse(new UXCampaignSlot { levelId = UXCampaign.CrimsonLevelId, checkpoint = 2, completed = true }.Validate());
        Assert.IsFalse(new UXCampaignSlot { seconds = float.NaN }.Validate());
        var save = new UXPlayerSaveData();
        save.campaigns.Add(new UXCampaignSlot()); save.campaigns.Add(new UXCampaignSlot());
        Assert.IsFalse(save.Validate());
    }
}
