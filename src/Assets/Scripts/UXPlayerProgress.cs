using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public sealed class UXLevelRecord
{
    public string levelId;
    public int easyClears, hardClears;
    public float bestEasySeconds, bestHardSeconds;

    public bool Completed(UXDifficulty mode) => mode == UXDifficulty.Hard ? hardClears > 0 : easyClears > 0;
    public float BestTime(UXDifficulty mode) => mode == UXDifficulty.Hard ? bestHardSeconds : bestEasySeconds;
}

[Serializable]
public sealed class UXPlayerSaveData
{
    public const int CurrentVersion = 6;
    public int schemaVersion = CurrentVersion;
    public string gameId = "PixelRebellion";
    public UXDifficulty difficulty = UXDifficulty.Easy;
    public bool showControlHints = true;
    public bool fullscreen = true;
    public float musicVolume = .55f;
    public float effectsVolume = .75f;
    public bool audioMuted;
    public List<UXLevelRecord> levels = new List<UXLevelRecord>();
    public List<UXCampaignSlot> campaigns = new List<UXCampaignSlot>();

    public UXLevelRecord FindLevel(string levelId) => levels.Find(level => level.levelId == levelId);
    public UXCampaignSlot FindCampaign(UXDifficulty mode) => campaigns.Find(slot => slot.mode == mode);

    public bool Validate()
    {
        if (gameId != "PixelRebellion" || (schemaVersion < 1 || schemaVersion > CurrentVersion)) return false;
        bool initializeAudio = schemaVersion < 6;
        if(initializeAudio){musicVolume=.55f;effectsVolume=.75f;audioMuted=false;}
        musicVolume=ValidTime(musicVolume)?Math.Min(1f,musicVolume):.55f;
        effectsVolume=ValidTime(effectsVolume)?Math.Min(1f,effectsVolume):.75f;
        bool migrate = schemaVersion == 1;
        bool initializeSprint = schemaVersion < 3;
        bool rebalanceSprint = schemaVersion == 3;
        bool expandCrimsonSprint = schemaVersion < 5;
        if (!Enum.IsDefined(typeof(UXDifficulty), difficulty)) difficulty = UXDifficulty.Easy;
        if (levels == null) levels = new List<UXLevelRecord>();
        var ids = new HashSet<string>();
        foreach (UXLevelRecord level in levels)
        {
            if (level == null || string.IsNullOrEmpty(level.levelId) ||
                level.levelId.Length > 64 || !ids.Add(level.levelId)) return false;
            level.easyClears = Math.Max(0, level.easyClears);
            level.hardClears = Math.Max(0, level.hardClears);
            if (!ValidTime(level.bestEasySeconds)) level.bestEasySeconds = 0f;
            if (!ValidTime(level.bestHardSeconds)) level.bestHardSeconds = 0f;
        }
        if (campaigns == null) campaigns = new List<UXCampaignSlot>();
        if (migrate)
        {
            campaigns.Clear();
            UXLevelRecord facility = FindLevel(UXPlayerProgress.FacilityLevelId);
            foreach (UXDifficulty mode in new[] { UXDifficulty.Easy, UXDifficulty.Hard })
                if (facility != null && facility.Completed(mode)) campaigns.Add(new UXCampaignSlot {
                    mode = mode, levelId = UXCampaign.CrimsonLevelId });
            schemaVersion = CurrentVersion;
        }
        var modes = new HashSet<UXDifficulty>();
        foreach (UXCampaignSlot slot in campaigns)
        {
            if(slot==null)return false;
            if(initializeSprint)slot.sprintSeconds=UXCampaign.CapacityForLevel(slot.levelId);
            if(rebalanceSprint)
            {
                if(!ValidTime(slot.sprintSeconds) || slot.sprintSeconds>24f)return false;
                slot.sprintSeconds=slot.sprintSeconds*(UXSprintReserve.CapacitySeconds/24f);
            }
            if(expandCrimsonSprint && !initializeSprint && slot.levelId==UXCampaign.CrimsonLevelId)
            {
                if(!ValidTime(slot.sprintSeconds) || slot.sprintSeconds>UXSprintReserve.CapacitySeconds)return false;
                // Grant only the extra budget once; retain the seconds already spent.
                slot.sprintSeconds+=UXSprintReserve.CrimsonCapacitySeconds-UXSprintReserve.CapacitySeconds;
            }
            if(!slot.Validate() || !modes.Add(slot.mode))return false;
        }
        schemaVersion=CurrentVersion;
        return true;
    }

    private static bool ValidTime(float seconds) => !float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds >= 0f;

    public void RecordCompletion(string levelId, UXDifficulty mode, float seconds)
    {
        UXLevelRecord level = FindLevel(levelId);
        if (level == null) { level = new UXLevelRecord { levelId = levelId }; levels.Add(level); }
        bool timed = ValidTime(seconds) && seconds > 0f;
        if (mode == UXDifficulty.Hard)
        {
            level.hardClears = Math.Min(int.MaxValue - 1, level.hardClears) + 1;
            if (timed && (level.bestHardSeconds <= 0f || seconds < level.bestHardSeconds)) level.bestHardSeconds = seconds;
        }
        else
        {
            level.easyClears = Math.Min(int.MaxValue - 1, level.easyClears) + 1;
            if (timed && (level.bestEasySeconds <= 0f || seconds < level.bestEasySeconds)) level.bestEasySeconds = seconds;
        }
    }
}

// A separate, versioned save file leaves the team's anonymous ID and telemetry intact.
public sealed class UXProgressStore
{
    private readonly string path;
    private bool preserveExisting;
    public UXPlayerSaveData Data { get; private set; }
    public string Status { get; private set; } = "PROGRESS SAVES ON THIS DEVICE";
    public bool HasSaveError { get; private set; }

    public UXProgressStore(string savePath) { path = Path.GetFullPath(savePath); Load(); }

    private static UXPlayerSaveData Read(string file)
    {
        if (!File.Exists(file)) return null;
        string json = File.ReadAllText(file);
        // Missing fields are not enough to establish a valid save identity/version.
        foreach (string field in new[] { "gameId", "schemaVersion", "difficulty", "showControlHints", "fullscreen", "levels" })
            if (!json.Contains("\"" + field + "\"")) return null;
        UXPlayerSaveData data = JsonUtility.FromJson<UXPlayerSaveData>(json);
        if(data!=null && data.schemaVersion>=6)
            foreach(string field in new[]{"musicVolume","effectsVolume","audioMuted"})
                if(!json.Contains("\""+field+"\""))return null;
        if (data != null && data.schemaVersion >= 2 &&
            !json.Contains("\"campaigns\"")) return null;
        if(data!=null && data.schemaVersion>=3 && data.campaigns!=null && data.campaigns.Count>0 &&
            !json.Contains("\"sprintSeconds\""))return null;
        return data != null && data.Validate() ? data : null;
    }

    private void Load()
    {
        bool exists = File.Exists(path);
        try
        {
            if (exists)
            {
                string json = File.ReadAllText(path);
                UXPlayerSaveData header = JsonUtility.FromJson<UXPlayerSaveData>(json);
                if (header != null && header.gameId == "PixelRebellion" &&
                    header.schemaVersion > UXPlayerSaveData.CurrentVersion)
                {
                    preserveExisting = true;
                    Data = new UXPlayerSaveData();
                    HasSaveError = true; Status = "SAVE FROM A NEWER VERSION - EXISTING RECORDS KEPT";
                    return;
                }
                Data = Read(path);
            }
        }
        catch (Exception) { Data = null; }
        if (Data == null)
        {
            try { Data = Read(path + ".bak"); } catch (Exception) { Data = null; }
            if (Data != null) Status = "PROGRESS RECOVERED FROM BACKUP";
            else
            {
                Data = new UXPlayerSaveData();
                if (exists)
                {
                    // Keep an unreadable file until the user deliberately installs a compatible version.
                    preserveExisting = true; HasSaveError = true;
                    Status = "SAVE COULD NOT BE READ - EXISTING FILE KEPT";
                }
            }
        }
    }

    public bool Save()
    {
        if (preserveExisting) return false;
        string temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(temporary, json, new UTF8Encoding(false));
            if (Read(temporary) == null) throw new InvalidDataException("Save verification failed.");
            if (File.Exists(path))
            {
                // Rotate only a valid previous file; never overwrite a good backup with corruption.
                bool previousValid;
                try { previousValid = Read(path) != null; } catch (Exception) { previousValid = false; }
                if (previousValid) File.Copy(path, path + ".bak", true);
                try { File.Replace(temporary, path, null); }
                catch (PlatformNotSupportedException) { File.Copy(temporary, path, true); File.Delete(temporary); }
            }
            else File.Move(temporary, path);
            HasSaveError = false; Status = "PROGRESS SAVED ON THIS DEVICE";
            return true;
        }
        catch (Exception exception)
        {
            HasSaveError = true; Status = "SAVE UNAVAILABLE - CURRENT RUN CAN STILL BE PLAYED";
            Debug.LogWarning("Player progress save failed: " + exception.Message);
            return false;
        }
    }

    public bool TryReplace(UXPlayerSaveData replacement)
    {
        if (replacement == null || !replacement.Validate() || preserveExisting) return false;
        UXPlayerSaveData previous = Data;
        Data = replacement;
        if (Save()) return true;
        Data = previous;
        return false;
    }
}

public static class UXPlayerProgress
{
    public const string FacilityLevelId = "facility_01";
    private static UXProgressStore store;
    public static UXProgressStore Store => store ?? (store = new UXProgressStore(
        Path.Combine(Application.persistentDataPath, "pixel_rebellion_progress_v1.json")));
    public static UXPlayerSaveData Data => Store.Data;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeCache() { store = null; }

    public static void SelectDifficulty(UXDifficulty mode)
    {
        UXGameAudio.UI();
        Data.difficulty = mode == UXDifficulty.Hard ? UXDifficulty.Hard : UXDifficulty.Easy;
        Store.Save();
    }

    public static void SetAudio(float music,float effects,bool muted)
    {
        Data.musicVolume=Mathf.Clamp01(music);Data.effectsVolume=Mathf.Clamp01(effects);Data.audioMuted=muted;
        Store.Save();
    }

    public static void SetControlHints(bool visible) { Data.showControlHints = visible; Store.Save(); }
    public static void SetFullscreen(bool fullscreen)
    {
        Data.fullscreen = fullscreen;
        if (Application.isPlaying && !Application.isEditor) Screen.fullScreen = fullscreen;
        Store.Save();
    }

    public static void ApplyDisplaySettings()
    {
        if (Application.isPlaying && !Application.isEditor) Screen.fullScreen = Data.fullscreen;
    }

    public static void CompleteFacility(UXDifficulty mode, float seconds)
    {
        Data.RecordCompletion(FacilityLevelId, mode, seconds);
        Store.Save();
    }

    public static string FormatTime(float seconds)
    {
        int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
    }
}
