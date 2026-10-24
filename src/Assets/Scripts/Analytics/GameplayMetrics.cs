
using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class GameplayMetrics : MonoBehaviour
{
    public static GameplayMetrics Instance { get; private set; }

    private const string GoogleSheetsURL = "https://script.google.com/macros/s/AKfycbwcjFD0zhpMjyOcoOrh6juVaneGKafEvaKJs2eb0cAmRaPXg59wrYYa3cUGpytrhpdN/exec";

    // Will change when Easy/Hard selection is implemented.
    public static string SelectedDifficulty = "Easy";

    public string PlayerID { get; private set; }
    private string sessionID;
    private float startTime;
    private float elapsedTime;
    private bool sessionStarted;
    private bool sessionSaved;

    private int guardDetections;
    private int cameraDetections;
    private int evidenceCollected;
    private int hidingCount;
    private int terminalInteractions;
    private int restartCount;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateMetrics()
    {
        if (Instance != null) return;

        GameObject obj = new GameObject("GameplayMetrics");
        obj.AddComponent<GameplayMetrics>();
        DontDestroyOnLoad(obj);
    }


    private void SetupPlayerID()
    {
        // Reuse the ID if this player has played before.
        PlayerID = PlayerPrefs.GetString("AnonymousPlayerID", "");

        if (string.IsNullOrEmpty(PlayerID))
        {
            // Generate a random anonymous ID.
            PlayerID = "P-" + Guid.NewGuid()
                .ToString("N")
                .Substring(0, 8)
                .ToUpperInvariant();

            // Save it for future sessions.
            PlayerPrefs.SetString("AnonymousPlayerID", PlayerID);
            PlayerPrefs.Save();
        }

        Debug.Log("Anonymous Player ID: " + PlayerID);
    }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupPlayerID();
    }

    private void Update()
    {
        if (sessionStarted && !sessionSaved)
            elapsedTime += Time.deltaTime;
    }

    public void StartSession()
    {
        sessionID = Guid.NewGuid().ToString("N");
        startTime = Time.time;
        elapsedTime = 0f;

        guardDetections = 0;
        cameraDetections = 0;
        evidenceCollected = 0;
        hidingCount = 0;
        terminalInteractions = 0;
        restartCount = 0;

        sessionStarted = true;
        sessionSaved = false;

        Debug.Log("Telemetry session started: " + sessionID);
    }

    public void RecordGuardDetection()
    {
        if (!sessionStarted || sessionSaved) return;
        guardDetections++;
    }

    public void RecordCameraDetection()
    {
        if (!sessionStarted || sessionSaved) return;
        cameraDetections++;
    }

    public void RecordEvidence(int amount)
    {
        if (!sessionStarted || sessionSaved) return;
        evidenceCollected += amount;
    }

    public void RecordHiding()
    {
        if (!sessionStarted || sessionSaved) return;
        hidingCount++;
    }

    public void RecordTerminalInteraction()
    {
        if (!sessionStarted || sessionSaved) return;
        terminalInteractions++;
    }

    public void RecordRestart()
    {
        if (!sessionStarted || sessionSaved) return;
        restartCount++;
    }

    public void EndSession(string result)
    {
        if (!sessionStarted || sessionSaved) return;

        sessionSaved = true;
        sessionStarted = false;

        // Sves local CSV
        SaveToCSV(result);

        // Send the record to the shared Google Sheet.
        UploadTelemetry(result);
    }

    private void SaveToCSV(string result)
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            "gameplay_metrics.csv"
        );

        string header =
            "PlayerID,SessionID,Difficulty,TimeSeconds," +
            "GuardDetections,CameraDetections," +
            "EvidenceCollected,HidingCount," +
            "TerminalInteractions,RestartCount,Result";

        if (!File.Exists(path))
            File.WriteAllText(path, header + Environment.NewLine);

        string row = string.Join(",", new string[]
        {
            PlayerID,
            sessionID,
            SelectedDifficulty,
            elapsedTime.ToString("F2", CultureInfo.InvariantCulture),
            guardDetections.ToString(),
            cameraDetections.ToString(),
            evidenceCollected.ToString(),
            hidingCount.ToString(),
            terminalInteractions.ToString(),
            restartCount.ToString(),
            result
        });

        try
        {
            File.AppendAllText(path, row + Environment.NewLine);
            Debug.Log("Telemetry saved to: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogError("Telemetry save failed: " + ex.Message);
        }
    }


    [System.Serializable]
    public class TelemetryRecord
    {
        public string PlayerID;
        public string SessionID;
        public string Difficulty;
        public float TimeSeconds;
        public int GuardDetections;
        public int CameraDetections;
        public int EvidenceCollected;
        public int HidingCount;
        public int TerminalInteractions;
        public int RestartCount;
        public string Result;
    }

    private void UploadTelemetry(string result)
    {
        TelemetryRecord record = new TelemetryRecord
        {
            PlayerID = PlayerID,
            SessionID = sessionID,
            Difficulty = SelectedDifficulty,
            TimeSeconds = elapsedTime,
            GuardDetections = guardDetections,
            CameraDetections = cameraDetections,
            EvidenceCollected = evidenceCollected,
            HidingCount = hidingCount,
            TerminalInteractions = terminalInteractions,
            RestartCount = restartCount,
            Result = result
        };

        string json = JsonUtility.ToJson(record);

        StartCoroutine(SendData(json));
    }


    private IEnumerator SendData(string json)
    {
        using (UnityWebRequest request =
            new UnityWebRequest(GoogleSheetsURL, "POST"))
        {
            byte[] body =
                System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type", "text/plain"
            );

            yield return request.SendWebRequest();

            Debug.Log("HTTP Status: " + request.responseCode);
            Debug.Log("Response: " + request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Google Sheets upload failed: " +
                    request.error
                );
            }
            else
            {
                string response =
                    request.downloadHandler.text.Trim();

                if (response == "success" ||
                    response == "duplicate")
                {
                    Debug.Log(
                        "Google Sheets telemetry: " + response
                    );
                }
                else
                {
                    Debug.LogWarning(
                        "Unexpected Google Sheets response: " +
                        response
                    );
                }
            }
        }
    }
}
