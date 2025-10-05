using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public enum FarmingAction
{
    Planting,
    Watering,
    Fertilizing,
    Harvesting,
    Monitoring
}

public enum CropType
{
    Corn,
    Soybeans,
    Wheat,
    Cotton,
    Rice
}

[System.Serializable]
public class CropData
{
    public CropType cropType;
    public Vector3 position;
    public float growthStage; // 0-1
    public float health; // 0-1
    public float waterLevel; // 0-1
    public float nutrientLevel; // 0-1
    public bool isReadyForHarvest;
    public float lastActionTime;
}

[System.Serializable]
public class FarmingDecision
{
    public FarmingAction action;
    public Vector3 location;
    public CropType cropType;
    public float decisionTime;
    public float ndviAtTime;
    public float soilMoistureAtTime;
    public string reasoning;
    public float sustainabilityImpact;
}

public class FarmController : MonoBehaviour
{
    [Header("Farm Configuration")]
    public Vector2 farmBounds = new Vector2(100f, 100f);
    public int maxCrops = 50;
    public float cropSpacing = 2f;

    [Header("Crop Management")]
    public List<CropData> activeCrops;
    public GameObject[] cropPrefabs;
    public Transform cropContainer;

    [Header("NASA Data Integration")]
    public NASADataManager nasaDataManager;

    [Header("Sustainability Metrics")]
    public float waterEfficiencyScore = 100f;
    public float soilHealthScore = 100f;
    public float yieldOptimizationScore = 100f;
    public float totalDecisionsMade = 0f;
    public float successfulDecisions = 0f;

    // Events
    public System.Action<FarmingDecision> OnFarmingDecisionMade;
    public System.Action<CropData> OnCropStateChanged;
    public System.Action<float> OnSustainabilityScoreUpdated;

    // Private variables
    private List<FarmingDecision> decisionHistory;
    private float lastUpdateTime;
    private bool isInitialized = false;

    void Start()
    {
        InitializeFarm();
    }

    void InitializeFarm()
    {
        activeCrops = new List<CropData>();
        decisionHistory = new List<FarmingDecision>();

        // Create crop container if not assigned
        if (cropContainer == null)
        {
            GameObject container = new GameObject("Crop Container");
            cropContainer = container.transform;
            cropContainer.SetParent(transform);
        }

        // Subscribe to NASA data events
        if (nasaDataManager != null)
        {
            nasaDataManager.OnCropHealthUpdated += OnNASACropHealthUpdate;
            nasaDataManager.OnIrrigationStatusUpdated += OnNASAIrrigationUpdate;
        }

        isInitialized = true;

        Debug.Log("🌾 Farm Controller initialized");

        // Start the farm update loop
        InvokeRepeating(nameof(UpdateFarm), 1f, 5f); // Update every 5 seconds
    }

    public void InitializeFarmAtLocation(float latitude, float longitude, float areaSize)
    {
        Debug.Log($"🌾 Initializing farm at {latitude}, {longitude} (Area: {areaSize}km²)");

        // Configure farm based on location
        ConfigureFarmForLocation(latitude, longitude);

        // Set NASA data location
        if (nasaDataManager != null)
        {
            nasaDataManager.SetFarmLocation(latitude, longitude, areaSize);
        }

        // Start with some initial crops
        StartCoroutine(PlantInitialCrops());
    }

    void ConfigureFarmForLocation(float latitude, float longitude)
    {
        // Configure farm parameters based on geographic location

        // Determine climate zone
        if (Mathf.Abs(latitude) < 30f)
        {
            // Tropical - more water-intensive crops
            maxCrops = 30;
            cropSpacing = 3f;
        }
        else if (Mathf.Abs(latitude) < 50f)
        {
            // Temperate - optimal for most crops
            maxCrops = 50;
            cropSpacing = 2f;
        }
        else
        {
            // High latitude - hardier crops, shorter season
            maxCrops = 25;
            cropSpacing = 2.5f;
        }

        Debug.Log($"🌍 Farm configured for latitude {latitude}: {maxCrops} max crops, {cropSpacing}m spacing");
    }

    IEnumerator PlantInitialCrops()
    {
        yield return new WaitForSeconds(2f); // Wait for NASA data

        // Plant some initial crops based on location suitability
        for (int i = 0; i < Mathf.Min(10, maxCrops); i++)
        {
            Vector3 position = GetRandomFarmPosition();
            CropType cropType = ChooseOptimalCropType();

            PlantCrop(cropType, position, "Initial planting based on NASA data analysis");

            yield return new WaitForSeconds(0.2f); // Stagger planting
        }
    }

    Vector3 GetRandomFarmPosition()
    {
        float x = UnityEngine.Random.Range(-farmBounds.x / 2, farmBounds.x / 2);
        float z = UnityEngine.Random.Range(-farmBounds.y / 2, farmBounds.y / 2);

        // Raycast to get ground height
        float y = 0f;
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(x, 100f, z), Vector3.down, out hit, 200f))
        {
            y = hit.point.y;
        }

        return new Vector3(x, y, z);
    }

    CropType ChooseOptimalCropType()
    {
        // Choose crop based on NASA data and location
        if (nasaDataManager != null && nasaDataManager.IsDataAvailable())
        {
            float ndvi = nasaDataManager.GetCurrentNDVI();
            float soilMoisture = nasaDataManager.GetCurrentSoilMoisture();

            // Choose crop based on conditions
            if (soilMoisture > 0.6f)
            {
                return CropType.Rice; // Water-loving crop
            }
            else if (ndvi > 0.6f)
            {
                return CropType.Corn; // High-productivity crop
            }
            else if (soilMoisture < 0.3f)
            {
                return CropType.Wheat; // Drought-tolerant
            }
            else
            {
                return CropType.Soybeans; // Versatile crop
            }
        }

        // Default fallback
        return CropType.Corn;
    }

    public void PlantCrop(CropType cropType, Vector3 position, string reasoning = "")
    {
        if (activeCrops.Count >= maxCrops)
        {
            Debug.LogWarning("⚠️ Farm at maximum capacity!");
            return;
        }

        // Create crop data
        CropData newCrop = new CropData
        {
            cropType = cropType,
            position = position,
            growthStage = 0f,
            health = 1f,
            waterLevel = 0.5f,
            nutrientLevel = 0.8f,
            isReadyForHarvest = false,
            lastActionTime = Time.time
        };

        activeCrops.Add(newCrop);

        // Instantiate visual crop
        if (cropPrefabs.Length > 0)
        {
            int prefabIndex = (int)cropType % cropPrefabs.Length;
            GameObject cropObj = Instantiate(cropPrefabs[prefabIndex], position, Quaternion.identity, cropContainer);
            cropObj.name = $"{cropType}_Crop_{activeCrops.Count}";
        }

        // Record decision
        RecordFarmingDecision(FarmingAction.Planting, position, cropType, reasoning);

        Debug.Log($"🌱 Planted {cropType} at {position} - {reasoning}");
    }

    public void WaterCrops(Vector3 center, float radius)
    {
        int cropsWatered = 0;
        float currentSoilMoisture = nasaDataManager != null ? nasaDataManager.GetCurrentSoilMoisture() : 0.4f;

        foreach (CropData crop in activeCrops)
        {
            float distance = Vector3.Distance(crop.position, center);
            if (distance <= radius)
            {
                // Water the crop
                crop.waterLevel = Mathf.Min(1f, crop.waterLevel + 0.3f);
                crop.lastActionTime = Time.time;
                cropsWatered++;

                OnCropStateChanged?.Invoke(crop);
            }
        }

        // Calculate water efficiency
        float efficiency = currentSoilMoisture > 0.6f ? 0.7f : 1.0f; // Penalize over-watering
        waterEfficiencyScore = Mathf.Lerp(waterEfficiencyScore, efficiency * 100f, 0.1f);

        string reasoning = $"Watered {cropsWatered} crops. Soil moisture: {currentSoilMoisture:P0}";
        RecordFarmingDecision(FarmingAction.Watering, center, CropType.Corn, reasoning);

        Debug.Log($"💧 {reasoning}");
    }

    public void FertilizeCrops(Vector3 center, float radius)
    {
        int cropsFertilized = 0;
        float currentNDVI = nasaDataManager != null ? nasaDataManager.GetCurrentNDVI() : 0.5f;

        foreach (CropData crop in activeCrops)
        {
            float distance = Vector3.Distance(crop.position, center);
            if (distance <= radius)
            {
                // Fertilize the crop
                crop.nutrientLevel = Mathf.Min(1f, crop.nutrientLevel + 0.4f);
                crop.health = Mathf.Min(1f, crop.health + 0.2f);
                crop.lastActionTime = Time.time;
                cropsFertilized++;

                OnCropStateChanged?.Invoke(crop);
            }
        }

        // Calculate fertilizer efficiency based on NDVI
        float efficiency = currentNDVI < 0.4f ? 1.0f : 0.6f; // More efficient on stressed crops
        soilHealthScore = Mathf.Lerp(soilHealthScore, efficiency * 100f, 0.1f);

        string reasoning = $"Fertilized {cropsFertilized} crops. Current NDVI: {currentNDVI:F2}";
        RecordFarmingDecision(FarmingAction.Fertilizing, center, CropType.Corn, reasoning);

        Debug.Log($"🌿 {reasoning}");
    }

    public void HarvestCrops(Vector3 center, float radius)
    {
        List<CropData> cropsToRemove = new List<CropData>();
        int cropsHarvested = 0;

        foreach (CropData crop in activeCrops)
        {
            float distance = Vector3.Distance(crop.position, center);
            if (distance <= radius && crop.isReadyForHarvest)
            {
                cropsHarvested++;
                cropsToRemove.Add(crop);

                // Remove visual crop
                GameObject cropObj = GameObject.Find($"{crop.cropType}_Crop_{activeCrops.IndexOf(crop) + 1}");
                if (cropObj != null)
                {
                    Destroy(cropObj);
                }
            }
        }

        // Remove harvested crops
        foreach (CropData crop in cropsToRemove)
        {
            activeCrops.Remove(crop);
        }

        // Update yield optimization score
        float harvestEfficiency = cropsHarvested > 0 ? 1.0f : 0.8f;
        yieldOptimizationScore = Mathf.Lerp(yieldOptimizationScore, harvestEfficiency * 100f, 0.1f);

        string reasoning = $"Harvested {cropsHarvested} crops at optimal time";
        RecordFarmingDecision(FarmingAction.Harvesting, center, CropType.Corn, reasoning);

        Debug.Log($"🚜 {reasoning}");
    }

    void UpdateFarm()
    {
        if (!isInitialized) return;

        float deltaTime = Time.time - lastUpdateTime;
        lastUpdateTime = Time.time;

        // Update all crops
        foreach (CropData crop in activeCrops)
        {
            UpdateCrop(crop, deltaTime);
        }

        // Update sustainability scores
        UpdateSustainabilityMetrics();
    }

    void UpdateCrop(CropData crop, float deltaTime)
    {
        // Growth simulation
        float growthRate = 0.01f * crop.health * crop.waterLevel * crop.nutrientLevel;
        crop.growthStage = Mathf.Min(1f, crop.growthStage + growthRate * deltaTime);

        // Natural resource depletion
        crop.waterLevel = Mathf.Max(0f, crop.waterLevel - 0.005f * deltaTime);
        crop.nutrientLevel = Mathf.Max(0f, crop.nutrientLevel - 0.002f * deltaTime);

        // Health calculation
        crop.health = (crop.waterLevel + crop.nutrientLevel) / 2f;

        // Harvest readiness
        if (crop.growthStage > 0.9f && crop.health > 0.6f)
        {
            crop.isReadyForHarvest = true;
        }

        // Notify of state changes
        if (Time.time - crop.lastActionTime > 10f) // Only notify occasionally
        {
            OnCropStateChanged?.Invoke(crop);
        }
    }

    void UpdateSustainabilityMetrics()
    {
        float overallScore = (waterEfficiencyScore + soilHealthScore + yieldOptimizationScore) / 3f;
        OnSustainabilityScoreUpdated?.Invoke(overallScore);
    }

    void RecordFarmingDecision(FarmingAction action, Vector3 location, CropType cropType, string reasoning)
    {
        FarmingDecision decision = new FarmingDecision
        {
            action = action,
            location = location,
            cropType = cropType,
            decisionTime = Time.time,
            ndviAtTime = nasaDataManager != null ? nasaDataManager.GetCurrentNDVI() : 0.5f,
            soilMoistureAtTime = nasaDataManager != null ? nasaDataManager.GetCurrentSoilMoisture() : 0.4f,
            reasoning = reasoning,
            sustainabilityImpact = CalculateSustainabilityImpact(action)
        };

        decisionHistory.Add(decision);
        totalDecisionsMade++;

        OnFarmingDecisionMade?.Invoke(decision);

        // Send to server for analytics
        if (Application.isPlaying)
        {
            StartCoroutine(SendDecisionToServer(decision));
        }
    }

    float CalculateSustainabilityImpact(FarmingAction action)
    {
        switch (action)
        {
            case FarmingAction.Planting:
                return 10f; // Positive impact
            case FarmingAction.Watering:
                return nasaDataManager != null && nasaDataManager.GetCurrentSoilMoisture() > 0.6f ? -5f : 5f;
            case FarmingAction.Fertilizing:
                return nasaDataManager != null && nasaDataManager.GetCurrentNDVI() > 0.6f ? -3f : 7f;
            case FarmingAction.Harvesting:
                return 8f; // Generally positive
            case FarmingAction.Monitoring:
                return 2f; // Small positive for data-driven decisions
            default:
                return 0f;
        }
    }

    IEnumerator SendDecisionToServer(FarmingDecision decision)
    {
        if (string.IsNullOrEmpty(nasaDataManager.serverURL)) yield break;

        string jsonData = JsonUtility.ToJson(decision);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = UnityWebRequest.Put($"{nasaDataManager.serverURL}/api/farm/decision", bodyRaw))
        {
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("📊 Decision sent to server for analysis");
            }
        }
    }

    // Event handlers for NASA data
    void OnNASACropHealthUpdate(CropHealthStatus status, string explanation)
    {
        Debug.Log($"🛰️ NASA Crop Health Update: {status} - {explanation}");

        // Adjust farming strategy based on NASA data
        if (status == CropHealthStatus.Poor)
        {
            // Recommend fertilization
            Debug.Log("💡 Recommendation: Consider fertilizing crops based on low NDVI");
        }
        else if (status == CropHealthStatus.Excellent)
        {
            // Recommend harvest timing
            Debug.Log("💡 Recommendation: Crops are healthy, monitor for harvest readiness");
        }
    }

    void OnNASAIrrigationUpdate(IrrigationStatus status, string recommendation)
    {
        Debug.Log($"🛰️ NASA Irrigation Update: {status} - {recommendation}");

        // Adjust irrigation strategy
        if (status == IrrigationStatus.Critical)
        {
            Debug.Log("💡 Urgent: Begin irrigation immediately based on SMAP data");
        }
        else if (status == IrrigationStatus.Adequate)
        {
            Debug.Log("💡 Info: Soil moisture adequate, focus on other activities");
        }
    }

    // Public getters for UI and other systems
    public int GetActiveCropCount() => activeCrops.Count;
    public int GetMaxCropCapacity() => maxCrops;
    public float GetWaterEfficiencyScore() => waterEfficiencyScore;
    public float GetSoilHealthScore() => soilHealthScore;
    public float GetYieldOptimizationScore() => yieldOptimizationScore;
    public float GetOverallSustainabilityScore() => (waterEfficiencyScore + soilHealthScore + yieldOptimizationScore) / 3f;
    public List<FarmingDecision> GetDecisionHistory() => decisionHistory;

    void OnDestroy()
    {
        // Unsubscribe from events
        if (nasaDataManager != null)
        {
            nasaDataManager.OnCropHealthUpdated -= OnNASACropHealthUpdate;
            nasaDataManager.OnIrrigationStatusUpdated -= OnNASAIrrigationUpdate;
        }
    }
}