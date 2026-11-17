using System;
using System.IO;
using NUnit.Framework;

public sealed class SprintReserveTests
{
    [Test]
    public void SpendingIsFiniteAndWaitingCannotRecharge()
    {
        var reserve=new UXSprintReserve();
        Assert.AreEqual(UXSprintReserve.CapacitySeconds,reserve.Remaining);
        Assert.AreEqual(3.5f,reserve.Spend(3.5f));
        Assert.AreEqual(0f,reserve.Spend(0));Assert.AreEqual(0f,reserve.Spend(-1));
        Assert.AreEqual(6.5f,reserve.Remaining);
        Assert.AreEqual(6.5f,reserve.Spend(100));Assert.IsTrue(reserve.Empty);
        Assert.AreEqual(0f,reserve.Spend(1));
    }

    [Test]
    public void DepletedResourceRoundTrips_WithoutErasingCheckpointOrRecord()
    {
        string dir=Path.Combine(Path.GetTempPath(),"PixelSprint_"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new UXProgressStore(Path.Combine(dir,"save.json"));
            store.Data.RecordCompletion(UXPlayerProgress.FacilityLevelId,UXDifficulty.Hard,100);
            store.Data.campaigns.Add(new UXCampaignSlot{mode=UXDifficulty.Hard,levelId=UXCampaign.CrimsonLevelId,checkpoint=2,sprintSeconds=0});
            Assert.IsTrue(store.Save());
            var loaded=new UXProgressStore(Path.Combine(dir,"save.json"));
            Assert.AreEqual(0f,loaded.Data.FindCampaign(UXDifficulty.Hard).sprintSeconds);
            Assert.AreEqual(2,loaded.Data.FindCampaign(UXDifficulty.Hard).checkpoint);
            Assert.AreEqual(100f,loaded.Data.FindLevel(UXPlayerProgress.FacilityLevelId).bestHardSeconds);
        }
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }

    [Test]
    public void ExistingSchemaTwoGetsInitialReserveOnce_AndKeepsProgress()
    {
        var save=new UXPlayerSaveData{schemaVersion=2};
        save.campaigns.Add(new UXCampaignSlot{mode=UXDifficulty.Hard,levelId=UXCampaign.CrimsonLevelId,checkpoint=2,seconds=123,sprintSeconds=0});
        Assert.IsTrue(save.Validate());Assert.AreEqual(UXSprintReserve.CapacitySeconds,save.FindCampaign(UXDifficulty.Hard).sprintSeconds);
        save.FindCampaign(UXDifficulty.Hard).sprintSeconds=0;
        Assert.IsTrue(save.Validate());Assert.AreEqual(0f,save.FindCampaign(UXDifficulty.Hard).sprintSeconds);
        Assert.AreEqual(2,save.FindCampaign(UXDifficulty.Hard).checkpoint);
        Assert.AreEqual(123f,save.FindCampaign(UXDifficulty.Hard).seconds);
    }

    [Test]
    public void SchemaThreeKeepsTheSpentFraction_WhenTheBudgetIsReduced()
    {
        var save=new UXPlayerSaveData{schemaVersion=3};
        save.campaigns.Add(new UXCampaignSlot{mode=UXDifficulty.Easy,levelId=UXCampaign.CrimsonLevelId,checkpoint=2,sprintSeconds=12f});
        save.campaigns.Add(new UXCampaignSlot{mode=UXDifficulty.Hard,levelId=UXCampaign.CrimsonLevelId,checkpoint=1,sprintSeconds=0f});
        Assert.IsTrue(save.Validate());
        Assert.AreEqual(5f,save.FindCampaign(UXDifficulty.Easy).sprintSeconds);
        Assert.AreEqual(0f,save.FindCampaign(UXDifficulty.Hard).sprintSeconds);
        Assert.AreEqual(2,save.FindCampaign(UXDifficulty.Easy).checkpoint);
        Assert.IsTrue(save.Validate());Assert.AreEqual(5f,save.FindCampaign(UXDifficulty.Easy).sprintSeconds);
    }

    [Test]
    public void InvalidSavedResourceIsRejected()
    {
        Assert.IsFalse(new UXCampaignSlot{sprintSeconds=-1}.Validate());
        Assert.IsFalse(new UXCampaignSlot{sprintSeconds=25}.Validate());
        Assert.IsFalse(new UXCampaignSlot{sprintSeconds=float.PositiveInfinity}.Validate());
    }
}
