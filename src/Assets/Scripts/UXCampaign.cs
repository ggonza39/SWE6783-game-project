using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class UXCampaignSlot
{
    public UXDifficulty mode;
    public string levelId = UXPlayerProgress.FacilityLevelId;
    public int checkpoint;
    public bool completed;
    public bool retryPending;
    public int mineSeed;
    public float seconds;
    public int detections;
    public float sprintSeconds = UXSprintReserve.CapacitySeconds;

    public bool Validate() => Enum.IsDefined(typeof(UXDifficulty), mode) &&
        (levelId == UXPlayerProgress.FacilityLevelId || levelId == UXCampaign.CrimsonLevelId) &&
        checkpoint >= 0 && checkpoint <= (levelId == UXCampaign.CrimsonLevelId ? 3 : 0) &&
        (!completed || (levelId == UXCampaign.CrimsonLevelId && checkpoint == 3)) &&
        !float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds >= 0f && detections>=0 &&
        !float.IsNaN(sprintSeconds) && !float.IsInfinity(sprintSeconds) && sprintSeconds>=0f &&
        sprintSeconds<=UXSprintReserve.CapacitySeconds && mineSeed>=0;
}

// Campaign state is independent of scene objects. Only secured sectors are saved;
// a failed attempt can update its elapsed clock without securing unfinished data.
public static class UXCampaign
{
    public const string CrimsonLevelId = "crimson_02";
    public const string FacilityScene = "Assets/Scenes/UXFacilityExpanded.unity";
    public const string CrimsonScene = "Assets/Scenes/UXCrimsonDepths.unity";
    public static bool IsCrimson => SceneManager.GetActiveScene().name == "UXCrimsonDepths";
    public static bool IsCampaignScene => IsCrimson || SceneManager.GetActiveScene().name == "UXFacilityExpanded";
    public static string SceneLevelId => IsCrimson ? CrimsonLevelId : UXPlayerProgress.FacilityLevelId;
    public static string ScenePath(string levelId) => levelId == CrimsonLevelId ? CrimsonScene : FacilityScene;
    public static UXCampaignSlot SelectedSlot => UXPlayerProgress.Data.FindCampaign(UXPlayerProgress.Data.difficulty);
    public static float RunSeconds { get; private set; }
    public static bool NeedsAdvance { get; private set; }
    public static bool Running { get; private set; }
    public static void CancelAdvance(){NeedsAdvance=false;}
    private static UXDifficulty runningMode;
    private static string runningLevel;
    private static int baseDetections;
    public static UXSprintReserve Sprint { get; private set; }
    public static float SprintRemaining => Sprint!=null ? Sprint.Remaining : UXSprintReserve.CapacitySeconds;
    private static float sprintSavedAt, sprintRetryAt;
    private static bool sprintDirty;
    public static int TotalDetections => (int)Math.Min(int.MaxValue,(long)baseDetections+
        (GameplayMetrics.Instance!=null?GameplayMetrics.Instance.TotalDetections:0));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntime()
    {
        RunSeconds = 0f; NeedsAdvance = false; Running = false; runningLevel = null;baseDetections=0;Sprint=null;sprintDirty=false;sprintRetryAt=0f;
    }

    private static UXCampaignSlot GetOrCreate(UXDifficulty mode)
    {
        UXCampaignSlot slot = UXPlayerProgress.Data.FindCampaign(mode);
        if (slot == null) { slot = new UXCampaignSlot { mode = mode }; UXPlayerProgress.Data.campaigns.Add(slot); }
        return slot;
    }

    public static int Begin(string levelId, UXDifficulty mode)
    {
        UXCampaignSlot slot = GetOrCreate(mode);
        if (slot.levelId != levelId || slot.completed)
        { slot.levelId = levelId; slot.checkpoint = 0; slot.seconds = 0f; slot.detections=0;slot.sprintSeconds=UXSprintReserve.CapacitySeconds;slot.completed = false;slot.retryPending=false;slot.mineSeed=0; }
        RefreshRetry(slot);
        if(slot.mineSeed==0)slot.mineSeed=NewMineSeed(0);
        runningMode = mode; runningLevel = levelId;
        RunSeconds = slot.seconds;baseDetections=slot.detections;Running = true; NeedsAdvance = false;
        Sprint=new UXSprintReserve(slot.sprintSeconds);sprintSavedAt=Sprint.Remaining;sprintDirty=false;sprintRetryAt=0f;
        UXPlayerProgress.Store.Save();
        return slot.checkpoint;
    }

    public static void BindTelemetrySession()
    {
        if(Running)Log("LevelEntered",runningMode,runningLevel,GetOrCreate(runningMode).checkpoint,RunSeconds,TotalDetections);
    }

    public static void Tick(float seconds)
    {
        if (Running && seconds > 0f && !float.IsNaN(seconds) && !float.IsInfinity(seconds)) RunSeconds += seconds;
    }

    public static bool SecureSector(int checkpoint)
    {
        UXCampaignSlot slot = GetOrCreate(runningMode);
        if (!Running || runningLevel != CrimsonLevelId || slot.levelId != CrimsonLevelId ||
            checkpoint < slot.checkpoint || checkpoint > slot.checkpoint + 1 || checkpoint > 3) return false;
        slot.checkpoint = checkpoint; slot.seconds = RunSeconds;slot.detections=TotalDetections;slot.sprintSeconds=SprintRemaining;
        Log("SectorSecured",runningMode,runningLevel,checkpoint,RunSeconds,TotalDetections);
        return UXPlayerProgress.Store.Save();
    }

    public static void SaveClock()
    {
        if (!Running) return;
        UXCampaignSlot slot = GetOrCreate(runningMode);
        if (slot.levelId != runningLevel) return;
        slot.seconds = RunSeconds;slot.detections=TotalDetections;slot.sprintSeconds=SprintRemaining;
        bool saved=UXPlayerProgress.Store.Save();
        sprintSavedAt=SprintRemaining;sprintRetryAt=saved?0f:Time.unscaledTime+1f;
        if(saved)sprintDirty=false;
    }

    // No per-frame disk writes: save at one-second spend boundaries, release,
    // exhaustion, checkpoint, pause/quit and the existing result/menu transitions.
    public static float SpendSprint(float seconds)
    {
        if(!Running || Sprint==null)return 0f;
        float spent=Sprint.Spend(seconds);if(spent<=0f)return 0f;
        GetOrCreate(runningMode).sprintSeconds=Sprint.Remaining;sprintDirty=true;
        if(Time.unscaledTime>=sprintRetryAt && (Sprint.Empty || sprintSavedAt-Sprint.Remaining>=1f))SaveClock();
        return spent;
    }
    public static void FlushSprint() { if(sprintDirty && Time.unscaledTime>=sprintRetryAt)SaveClock(); }

    public static void RecordDeath()
    {
        SaveClock();
        if(Running)
        {
            GetOrCreate(runningMode).retryPending=true;
            UXPlayerProgress.Store.Save();
            Log("CheckpointRetry",runningMode,runningLevel,GetOrCreate(runningMode).checkpoint,RunSeconds,TotalDetections);
        }
    }

    public static void Complete(string levelId, UXDifficulty mode, float seconds)
    {
        UXPlayerProgress.Data.RecordCompletion(levelId, mode, seconds);
        UXCampaignSlot slot = GetOrCreate(mode);
        Log("LevelCleared",mode,levelId,slot.checkpoint,seconds,TotalDetections);
        if (levelId == UXPlayerProgress.FacilityLevelId)
        {
            slot.levelId = CrimsonLevelId; slot.checkpoint = 0; slot.seconds = 0f;slot.detections=0;
            slot.sprintSeconds=UXSprintReserve.CapacitySeconds;slot.completed = false;slot.retryPending=false;slot.mineSeed=0; NeedsAdvance = true;
        }
        else
        {
            slot.levelId = CrimsonLevelId; slot.checkpoint = 3; slot.seconds = seconds;
            slot.completed = true;slot.detections=TotalDetections;slot.sprintSeconds=SprintRemaining;NeedsAdvance = false;
        }
        Running = false; UXPlayerProgress.Store.Save();
    }

    private static int NewMineSeed(int previous)
    {
        int seed;
        do {seed=Guid.NewGuid().GetHashCode()&int.MaxValue;} while(seed==0 || seed==previous);
        return seed;
    }
    private static void RefreshRetry(UXCampaignSlot slot)
    {
        if(!slot.retryPending)return;
        slot.sprintSeconds=UXSprintReserve.CapacitySeconds;
        slot.mineSeed=NewMineSeed(slot.mineSeed);slot.retryPending=false;
        Sprint=new UXSprintReserve(slot.sprintSeconds);
        sprintSavedAt=Sprint.Remaining;sprintDirty=false;sprintRetryAt=0f;
    }
    public static int MineSeed => GetOrCreate(runningMode).mineSeed;
    public static string PrepareReplay()
    {
        SaveClock();
        GetOrCreate(UXPlayerProgress.Data.difficulty).retryPending=true;
        return PrepareContinue();
    }

    public static string PrepareContinue()
    {
        UXCampaignSlot slot = GetOrCreate(UXPlayerProgress.Data.difficulty);
        if (slot.completed) { slot.checkpoint = 0; slot.seconds = 0f;slot.detections=0;slot.sprintSeconds=UXSprintReserve.CapacitySeconds;slot.completed = false;slot.retryPending=false;slot.mineSeed=0; }
        RefreshRetry(slot);
        if(slot.mineSeed==0)slot.mineSeed=NewMineSeed(0);
        NeedsAdvance = false; Running = false;
        UXPlayerProgress.Store.Save();
        return ScenePath(slot.levelId);
    }

    public static void RestartLevel(string levelId, UXDifficulty mode)
    {
        UXCampaignSlot slot = GetOrCreate(mode);
        Log("LevelRestarted",mode,levelId,slot.checkpoint,RunSeconds,TotalDetections);
        slot.levelId = levelId; slot.checkpoint = 0; slot.seconds = 0f;slot.detections=0;slot.sprintSeconds=UXSprintReserve.CapacitySeconds;slot.completed = false;slot.retryPending=false;slot.mineSeed=0;
        RunSeconds = 0f; baseDetections = 0; Sprint=null;sprintDirty=false;sprintRetryAt=0f;Running = false; NeedsAdvance = false;
        UXPlayerProgress.Store.Save();
    }

    public static bool RestartGame()
    {
        UXPlayerSaveData old = UXPlayerProgress.Data;
        var fresh = new UXPlayerSaveData { difficulty = old.difficulty,
            fullscreen = old.fullscreen, showControlHints = old.showControlHints };
        if (!UXPlayerProgress.Store.TryReplace(fresh)) return false;
        Log("CampaignReset",old.difficulty,"campaign",0,RunSeconds,TotalDetections);
        ResetRuntime(); return true;
    }

    public static void RecordHazard(string action)
    {
        if(Running)Log(action,runningMode,runningLevel,GetOrCreate(runningMode).checkpoint,RunSeconds,TotalDetections);
    }

    private static void Log(string action,UXDifficulty mode,string levelId,int checkpoint,float seconds,int detections)
    {
        try
        {
            string path=Path.Combine(Application.persistentDataPath,"campaign_events.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string header=File.Exists(path)?"":"PlayerID,SessionID,UtcTime,LevelId,Difficulty,Checkpoint,Event,RunSeconds,RunDetections"+Environment.NewLine;
            string identity=GameplayMetrics.Instance!=null?GameplayMetrics.Instance.PlayerID+","+GameplayMetrics.Instance.SessionID:",";
            File.AppendAllText(path,header+identity+","+DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture)+","+levelId+","+mode+","+
                checkpoint+","+action+","+seconds.ToString("F2",CultureInfo.InvariantCulture)+","+detections+Environment.NewLine);
        }
        catch(Exception exception){Debug.LogWarning("Campaign event log unavailable: "+exception.Message);}
    }
}
