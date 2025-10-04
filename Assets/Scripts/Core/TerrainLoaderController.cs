using UnityEngine;
using Plantopia.Geocoding;
using Plantopia.DEM;
using Plantopia.UI;

namespace Plantopia.Core
{
    /// <summary>
    /// Main controller for terrain loading pipeline
    /// </summary>
    public class TerrainLoaderController : MonoBehaviour
    {
        [Header("References")]
        public GeocodeManager geocodeManager;
        public DEMDownloader demDownloader;
        public DEMProcessor demProcessor;
        public TerrainGenerator terrainGenerator;
        public UIController uiController;
        public ProgressController progressController;
        
        [Header("Settings")]
        public TerrainMetadata currentMetadata;
        
        private void Awake()
        {
            // Initialize components
            if (geocodeManager == null) geocodeManager = GetComponent<GeocodeManager>();
            if (demDownloader == null) demDownloader = GetComponent<DEMDownloader>();
            if (demProcessor == null) demProcessor = GetComponent<DEMProcessor>();
            if (terrainGenerator == null) terrainGenerator = GetComponent<TerrainGenerator>();
        }
        
        /// <summary>
        /// Start the terrain loading process from a location name
        /// </summary>
        public async void LoadTerrainFromLocation(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
            {
                Debug.LogError("Location name is empty");
                uiController?.UpdateStatus("Please enter a location name", Color.red);
                return;
            }
            
            try
            {
                // Step 1: Geocode location
                progressController?.UpdateProgress(0.1f, "Geocoding location...");
                uiController?.UpdateStatus($"Finding coordinates for {locationName}...", Color.yellow);
                
                LocationData location = await geocodeManager.GeocodeLocation(locationName);
                
                if (location == null)
                {
                    uiController?.UpdateStatus("Location not found. Please try a different name.", Color.red);
                    progressController?.ResetProgress();
                    return;
                }
                
                Debug.Log($"Location found: {location}");
                
                // Step 2: Download DEM
                progressController?.UpdateProgress(0.3f, "Downloading elevation data...");
                uiController?.UpdateStatus($"Downloading DEM for {location.locationName}...", Color.yellow);
                
                string demFilePath = await demDownloader.DownloadDEM(location);
                
                if (string.IsNullOrEmpty(demFilePath))
                {
                    uiController?.UpdateStatus("Failed to download elevation data", Color.red);
                    progressController?.ResetProgress();
                    return;
                }
                
                // Step 3: Process DEM to heightmap
                progressController?.UpdateProgress(0.6f, "Processing elevation data...");
                uiController?.UpdateStatus("Converting DEM to heightmap...", Color.yellow);
                
                string heightmapPath = await demProcessor.ProcessDEM(demFilePath);
                
                if (string.IsNullOrEmpty(heightmapPath))
                {
                    uiController?.UpdateStatus("Failed to process elevation data", Color.red);
                    progressController?.ResetProgress();
                    return;
                }
                
                // Step 4: Generate terrain
                progressController?.UpdateProgress(0.8f, "Generating terrain...");
                uiController?.UpdateStatus("Creating Unity terrain...", Color.yellow);
                
                currentMetadata = new TerrainMetadata
                {
                    location = location,
                    demFilePath = demFilePath,
                    heightmapPath = heightmapPath
                };
                
                Terrain terrain = terrainGenerator.GenerateTerrainFromHeightmap(heightmapPath, currentMetadata);
                
                if (terrain == null)
                {
                    uiController?.UpdateStatus("Failed to generate terrain", Color.red);
                    progressController?.ResetProgress();
                    return;
                }
                
                // Success!
                progressController?.CompleteProgress("Terrain loaded successfully!");
                uiController?.UpdateStatus($"Terrain loaded: {location.locationName}", Color.green);
                
                Debug.Log($"Terrain generation complete for {location.locationName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Terrain loading failed: {ex.Message}");
                Debug.LogException(ex);
                uiController?.UpdateStatus($"Error: {ex.Message}", Color.red);
                progressController?.ResetProgress();
            }
        }
    }
}
