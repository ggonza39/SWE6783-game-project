using UnityEngine;

public class UXHUDController : MonoBehaviour
{
    private Transform player;
    private PlayerHiding playerHiding;

    private GameObject terminal;
    private GameObject extractionZone;
    private EvidenceCollectible[] evidenceItems;

    private Texture2D briefingTexture;

    private int totalEvidence = 1;
    private bool briefingVisible = true;
    private bool helpVisible = false;
    private bool terminalAccessed = false;

    private Texture2D blackTex;
    private Texture2D darkTex;
    private Texture2D cyanTex;
    private Texture2D greenTex;
    private Texture2D yellowTex;
    private Texture2D redTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateHUD()
    {
        if (FindFirstObjectByType<UXHUDController>() != null)
            return;

        GameObject hud = new GameObject("Victor_UX_HUD");
        hud.AddComponent<UXHUDController>();
    }

    private void Start()
    {
        briefingTexture =
            Resources.Load<Texture2D>("UX/BriefingWireframe");

        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHiding =
                playerObject.GetComponent<PlayerHiding>();
        }

        terminal = GameObject.Find("Terminal");
        extractionZone = GameObject.Find("ExtractionZone");

        evidenceItems =
            FindObjectsByType<EvidenceCollectible>(
                FindObjectsSortMode.None
            );

        if (evidenceItems != null &&
            evidenceItems.Length > 0)
        {
            totalEvidence = evidenceItems.Length;
        }

        CreateTextures();

        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (briefingVisible)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetMouseButtonDown(0))
            {
                briefingVisible = false;
                Time.timeScale = 1f;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            helpVisible = !helpVisible;
            Time.timeScale = helpVisible ? 0f : 1f;
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
        {
            Time.timeScale = 0f;
            return;
        }

        if (helpVisible)
            return;

        if (player != null &&
            terminal != null &&
            Vector2.Distance(
                player.position,
                terminal.transform.position
            ) <= 2f &&
            Input.GetKeyDown(KeyCode.E))
        {
            if (EvidenceManager.Instance != null &&
                EvidenceManager.Instance.EvidenceCollected > 0)
            {
                terminalAccessed = true;
            }
        }
    }

    private void OnGUI()
    {
        if (briefingVisible)
        {
            DrawBriefing();
            return;
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.GameEnded)
        {
            DrawMissionResult();
            return;
        }

        DrawHUD();

        if (helpVisible)
        {
            DrawHelp();
            return;
        }
    }

    private void DrawBriefing()
    {
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            blackTex
        );

        if (briefingTexture != null)
        {
            GUI.DrawTexture(
                new Rect(0, 0, Screen.width, Screen.height),
                briefingTexture,
                ScaleMode.ScaleToFit,
                true
            );
        }
        else
        {
            DrawPanel(
                new Rect(
                    Screen.width / 2f - 300,
                    Screen.height / 2f - 150,
                    600,
                    300
                ),
                darkTex,
                cyanTex
            );

            GUI.Label(
                new Rect(
                    Screen.width / 2f - 250,
                    Screen.height / 2f - 40,
                    500,
                    80
                ),
                "MISSION BRIEFING\n\nPRESS ENTER TO START",
                TitleStyle(24)
            );
        }
    }

    private void DrawHUD()
    {
        int evidence =
            EvidenceManager.Instance != null
            ? EvidenceManager.Instance.EvidenceCollected
            : 0;

        bool hidden =
            playerHiding != null &&
            playerHiding.IsHidden;

        Rect leftPanel =
            new Rect(18, 18, 300, 126);

        DrawPanel(leftPanel, darkTex, cyanTex);

        GUI.Label(
            new Rect(35, 28, 265, 30),
            "PIXEL REBELLION",
            TitleStyle(20)
        );

        GUI.Label(
            new Rect(35, 65, 250, 24),
            "EVIDENCE   " +
            evidence + " / " + totalEvidence,
            TextStyle(17, Color.white)
        );

        GUI.Label(
            new Rect(35, 95, 250, 24),
            hidden
                ? "STATUS     HIDDEN"
                : "STATUS     VISIBLE",
            TextStyle(
                17,
                hidden
                    ? new Color(0.35f, 1f, 0.48f)
                    : new Color(1f, 0.85f, 0.25f)
            )
        );

        Rect objectivePanel =
            new Rect(
                Screen.width - 368,
                18,
                350,
                126
            );

        DrawPanel(
            objectivePanel,
            darkTex,
            cyanTex
        );

        GUI.Label(
            new Rect(
                Screen.width - 345,
                28,
                305,
                28
            ),
            "CURRENT OBJECTIVE",
            TitleStyle(18)
        );

        string objective;

        if (evidence < totalEvidence)
        {
            objective = "COLLECT THE EVIDENCE";
        }
        else if (!terminalAccessed)
        {
            objective = "ACCESS SECURITY TERMINAL  [E]";
        }
        else
        {
            objective = "REACH EXTRACTION";
        }

        GUI.Label(
            new Rect(
                Screen.width - 345,
                70,
                305,
                44
            ),
            objective,
            TextStyle(
                17,
                new Color(0.45f, 1f, 0.55f)
            )
        );

        Rect controls =
            new Rect(
                Screen.width / 2f - 335,
                Screen.height - 54,
                670,
                38
            );

        DrawPanel(
            controls,
            darkTex,
            cyanTex
        );

        GUI.Label(
            new Rect(
                controls.x + 10,
                controls.y + 7,
                controls.width - 20,
                24
            ),
            "WASD / ARROWS  MOVE    SHIFT  SPRINT    E  INTERACT    ESC: PAUSE / HELP",
            CenterStyle(14, Color.white)
        );

        DrawContextPrompt(evidence);
    }

    private void DrawContextPrompt(int evidence)
    {
        if (player == null)
            return;

        string message = "";
        Texture2D border = yellowTex;

        float closestEvidenceDistance =
            float.MaxValue;

        if (evidenceItems != null)
        {
            foreach (EvidenceCollectible item
                     in evidenceItems)
            {
                if (item == null)
                    continue;

                float d = Vector2.Distance(
                    player.position,
                    item.transform.position
                );

                if (d < closestEvidenceDistance)
                    closestEvidenceDistance = d;
            }
        }

        if (closestEvidenceDistance <= 2f)
        {
            message =
                "EVIDENCE FOUND  -  MOVE CLOSER TO COLLECT";
            border = cyanTex;
        }

        if (terminal != null &&
            Vector2.Distance(
                player.position,
                terminal.transform.position
            ) <= 2f)
        {
            if (evidence < 1)
            {
                message =
                    "TERMINAL LOCKED  -  COLLECT EVIDENCE";
                border = redTex;
            }
            else if (terminalAccessed)
            {
                message =
                    "TERMINAL ACCESSED";
                border = greenTex;
            }
            else
            {
                message =
                    "PRESS E TO ACCESS SECURITY TERMINAL";
                border = greenTex;
            }
        }

        if (extractionZone != null &&
            Vector2.Distance(
                player.position,
                extractionZone.transform.position
            ) <= 2.2f)
        {
            if (evidence < totalEvidence)
            {
                message =
                    "EXTRACTION LOCKED  -  COLLECT EVIDENCE";
                border = redTex;
            }
            else
            {
                message =
                    "EXTRACTION ZONE";
                border = greenTex;
            }
        }

        if (string.IsNullOrEmpty(message))
            return;

        Rect prompt =
            new Rect(
                Screen.width / 2f - 280,
                Screen.height - 108,
                560,
                42
            );

        DrawPanel(prompt, darkTex, border);

        GUI.Label(
            new Rect(
                prompt.x + 10,
                prompt.y + 7,
                prompt.width - 20,
                28
            ),
            message,
            CenterStyle(16, Color.white)
        );
    }

    private void DrawHelp()
    {
        GUI.DrawTexture(
            new Rect(
                0,
                0,
                Screen.width,
                Screen.height
            ),
            blackTex
        );

        Rect panel =
            new Rect(
                Screen.width / 2f - 320,
                Screen.height / 2f - 205,
                640,
                410
            );

        DrawPanel(panel, darkTex, cyanTex);

        GUI.Label(
            new Rect(
                panel.x + 40,
                panel.y + 25,
                panel.width - 80,
                40
            ),
            "PAUSE / HELP",
            TitleStyle(27)
        );

        GUI.Label(
            new Rect(
                panel.x + 65,
                panel.y + 90,
                panel.width - 130,
                205
            ),
            "WASD / ARROW KEYS   Move\n\n" +
            "SHIFT   Sprint\n\n" +
            "E   Interact with terminal\n\n" +
            "Enter hiding areas to conceal yourself\n\n" +
            "Collect evidence before extraction",
            TextStyle(18, Color.white)
        );

        GUI.Label(
            new Rect(
                panel.x + 50,
                panel.y + 340,
                panel.width - 100,
                30
            ),
            "PRESS ESC TO RETURN",
            CenterStyle(
                17,
                new Color(0.35f, 0.8f, 1f)
            )
        );
    }

    private void DrawMissionResult()
    {
        if (GameManager.Instance == null ||
            !GameManager.Instance.GameEnded)
        {
            return;
        }

        bool won =
            GameManager.Instance.PlayerWon;

        Rect result =
            new Rect(
                Screen.width / 2f - 310,
                Screen.height / 2f - 85,
                620,
                170
            );

        DrawPanel(
            result,
            darkTex,
            won ? greenTex : redTex
        );

        GUI.Label(
            new Rect(
                result.x + 25,
                result.y + 45,
                result.width - 50,
                70
            ),
            won
                ? "MISSION COMPLETE"
                : "MISSION FAILED",
            TitleStyle(30)
        );
    }

    private void DrawPanel(
        Rect rect,
        Texture2D background,
        Texture2D border)
    {
        GUI.DrawTexture(rect, border);

        GUI.DrawTexture(
            new Rect(
                rect.x + 2,
                rect.y + 2,
                rect.width - 4,
                rect.height - 4
            ),
            background
        );
    }

    private GUIStyle TitleStyle(int size)
    {
        GUIStyle style =
            new GUIStyle(GUI.skin.label);

        style.fontSize = size;
        style.fontStyle = FontStyle.Bold;
        style.alignment =
            TextAnchor.MiddleCenter;

        style.normal.textColor =
            new Color(0.35f, 0.82f, 1f);

        return style;
    }

    private GUIStyle TextStyle(
        int size,
        Color color)
    {
        GUIStyle style =
            new GUIStyle(GUI.skin.label);

        style.fontSize = size;
        style.normal.textColor = color;
        style.wordWrap = true;

        return style;
    }

    private GUIStyle CenterStyle(
        int size,
        Color color)
    {
        GUIStyle style =
            TextStyle(size, color);

        style.alignment =
            TextAnchor.MiddleCenter;

        style.fontStyle =
            FontStyle.Bold;

        return style;
    }

    private void CreateTextures()
    {
        blackTex =
            MakeTexture(
                new Color(0f, 0f, 0f, 1f)
            );

        darkTex =
            MakeTexture(
                new Color(
                    0.025f,
                    0.045f,
                    0.075f,
                    0.94f
                )
            );

        cyanTex =
            MakeTexture(
                new Color(
                    0.10f,
                    0.62f,
                    0.90f,
                    1f
                )
            );

        greenTex =
            MakeTexture(
                new Color(
                    0.16f,
                    0.78f,
                    0.33f,
                    1f
                )
            );

        yellowTex =
            MakeTexture(
                new Color(
                    0.95f,
                    0.68f,
                    0.10f,
                    1f
                )
            );

        redTex =
            MakeTexture(
                new Color(
                    0.90f,
                    0.18f,
                    0.18f,
                    1f
                )
            );
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture =
            new Texture2D(1, 1);

        texture.SetPixel(0, 0, color);
        texture.Apply();

        return texture;
    }
}



