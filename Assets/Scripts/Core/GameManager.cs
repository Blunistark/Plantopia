using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GameState
{
    MainMenu,
    LocationSelection,
    FarmSetup,
    Farming,
    DataAnalysis,
    Settings
}

[System.Serializable]
public class PlayerData
{
    public string playerName = "NASA Farmer";
    public float farmLatitude;
    public float farmLongitude;
    public float farmAreaSize;
    public int playerLevel = 1;
    public float sustainabilityScore = 100f;
    public int totalDecisionsMade = 0;
    public int successfulHarvests = 0;
    public float waterSaved = 0f;
    public float co2Reduced = 0f;
}

public class GameManager : MonoBehaviour
{
    [Header("Game Configuration")]
    public static GameManager Instance;
    public bool isDebugMode = true;
    public string serverURL = "http://localhost:3000";

    [Header("Game State")]
    public GameState currentState = GameState.MainMenu;
    public PlayerData playerData;

    [Header("Components")]
    public LocationSelectorUI locationSelector;
    public NASADataManager nasaDataManager;
    public FarmController farmController;

    // Events
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<PlayerData> OnPlayerDataUpdated;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeGame()
    {
        Debug.Log("🚀 NASA Farm Navigator - Game Starting...");

        // Initialize player data
        playerData = new PlayerData();
        LoadPlayerData();

        // Initialize server connection
        StartCoroutine(InitializeServerConnection());

        // Start with location selection
        ChangeGameState(GameState.LocationSelection);
    }

    IEnumerator InitializeServerConnection()
    {
        Debug.Log("🌐 Connecting to NASA data server...");

        // Test server connection
        yield return StartCoroutine(TestServerConnection());
    }

    IEnumerator TestServerConnection()
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get($"{serverURL}/api/health"))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Server connection established");
                ShowNotification("Connected to NASA data services", "success");
            }
            else
            {
                Debug.LogError($"❌ Failed to connect to server: {request.error}");
                ShowNotification("Connection failed. Using offline mode.", "warning");
            }
        }
    }

    public void ChangeGameState(GameState newState)
    {
        GameState previousState = currentState;
        currentState = newState;

        Debug.Log($"🎮 Game state changed: {previousState} → {newState}");

        switch (newState)
        {
            case GameState.MainMenu:
                LoadScene("MainMenu");
                break;

            case GameState.LocationSelection:
                LoadScene("LocationSelection");
                break;

            case GameState.FarmSetup:
                LoadScene("FarmSetup");
                break;

            case GameState.Farming:
                LoadScene("FarmSimulation");
                break;

            case GameState.DataAnalysis:
                ShowDataAnalysisPanel();
                break;

            case GameState.Settings:
                ShowSettingsPanel();
                break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"⚠️ Scene '{sceneName}' not found in build settings!");
        }
    }

    public void StartFarmAtLocation(float lat, float lng, float areaSize)
    {
        Debug.Log($"🌾 Starting farm at coordinates: {lat}, {lng} (Area: {areaSize}km²)");

        // Store farm location
        playerData.farmLatitude = lat;
        playerData.farmLongitude = lng;
        playerData.farmAreaSize = areaSize;

        // Initialize NASA data for this location
        if (nasaDataManager != null)
        {
            nasaDataManager.SetFarmLocation(lat, lng, areaSize);
        }
        else
        {
            Debug.LogWarning("⚠️ NASADataManager not found! Creating one...");
            CreateNASADataManager();
        }

        // Initialize farm controller
        if (farmController != null)
        {
            farmController.InitializeFarmAtLocation(lat, lng, areaSize);
        }
        else
        {
            Debug.LogWarning("⚠️ FarmController not found! Will be created in farming scene.");
        }

        // Save player data
        SavePlayerData();

        // Transition to farming simulation
        ChangeGameState(GameState.Farming);
    }

    void CreateNASADataManager()
    {
        GameObject nasaManagerObj = new GameObject("NASA Data Manager");
        nasaManagerObj.transform.SetParent(transform);
        nasaDataManager = nasaManagerObj.AddComponent<NASADataManager>();
        nasaDataManager.serverURL = serverURL;

        Debug.Log("📡 Created NASA Data Manager");
    }

    public void UpdatePlayerProgress(int decisions, int harvests, float waterSaved, float co2Reduced)
    {
        playerData.totalDecisionsMade += decisions;
        playerData.successfulHarvests += harvests;
        playerData.waterSaved += waterSaved;
        playerData.co2Reduced += co2Reduced;

        // Calculate level based on progress
        int newLevel = 1 + (playerData.totalDecisionsMade / 10) + (playerData.successfulHarvests / 5);
        if (newLevel > playerData.playerLevel)
        {
            playerData.playerLevel = newLevel;
            ShowNotification($"Level up! You are now level {newLevel}", "success");
        }

        OnPlayerDataUpdated?.Invoke(playerData);
        SavePlayerData();
    }

    public void UpdateSustainabilityScore(float newScore)
    {
        float oldScore = playerData.sustainabilityScore;
        playerData.sustainabilityScore = newScore;

        if (newScore > oldScore)
        {
            ShowNotification($"Sustainability improved: {newScore:F1}%", "success");
        }
        else if (newScore < oldScore - 5f)
        {
            ShowNotification($"Sustainability declined: {newScore:F1}%", "warning");
        }

        OnPlayerDataUpdated?.Invoke(playerData);
    }

    void ShowDataAnalysisPanel()
    {
        Debug.Log("📊 Showing data analysis panel");
        // This would show NASA data visualization
    }

    void ShowSettingsPanel()
    {
        Debug.Log("⚙️ Showing settings panel");
        // This would show game settings
    }

    void ShowNotification(string message, string type)
    {
        Debug.Log($"📢 {type.ToUpper()}: {message}");

        // Find UIManager and show notification
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            NotificationType notificationType = NotificationType.Info;
            switch (type.ToLower())
            {
                case "success":
                    notificationType = NotificationType.Success;
                    break;
                case "warning":
                    notificationType = NotificationType.Warning;
                    break;
                case "error":
                    notificationType = NotificationType.Error;
                    break;
            }

            uiManager.ShowNotification(message, notificationType);
        }
    }

    void SavePlayerData()
    {
        try
        {
            string jsonData = JsonUtility.ToJson(playerData);
            PlayerPrefs.SetString("PlayerData", jsonData);
            PlayerPrefs.Save();

            Debug.Log("💾 Player data saved locally");

            // Also save to server
            if (Application.isPlaying)
            {
                StartCoroutine(SavePlayerDataToServer());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to save player data: {e.Message}");
        }
    }

    void LoadPlayerData()
    {
        try
        {
            if (PlayerPrefs.HasKey("PlayerData"))
            {
                string jsonData = PlayerPrefs.GetString("PlayerData");
                playerData = JsonUtility.FromJson<PlayerData>(jsonData);
                Debug.Log("📁 Player data loaded from local storage");
            }
            else
            {
                Debug.Log("📁 No saved player data found, using defaults");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to load player data: {e.Message}");
            playerData = new PlayerData(); // Use defaults
        }
    }

    IEnumerator SavePlayerDataToServer()
    {
        string jsonData = JsonUtility.ToJson(playerData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Put($"{serverURL}/api/player/save", bodyRaw))
        {
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("💾 Player data saved to server");
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to save to server: {request.error}");
            }
        }
    }

    // Application lifecycle events
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SavePlayerData();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SavePlayerData();
        }
    }

    void OnDestroy()
    {
        SavePlayerData();
    }

    // Public getters
    public bool IsServerConnected()
    {
        return nasaDataManager != null && nasaDataManager.IsDataAvailable();
    }

    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    public string GetServerURL()
    {
        return serverURL;
    }

    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugSetLocation(float lat, float lng)
    {
        if (isDebugMode)
        {
            StartFarmAtLocation(lat, lng, 5f);
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugAddProgress()
    {
        if (isDebugMode)
        {
            UpdatePlayerProgress(5, 1, 100f, 50f);
        }
    }
}