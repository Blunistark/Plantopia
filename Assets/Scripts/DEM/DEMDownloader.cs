using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Threading.Tasks;
using Plantopia.Core;

namespace Plantopia.DEM
{
    /// <summary>
    /// Downloads DEM data from Backend API or OpenTopography directly
    /// </summary>
    public class DEMDownloader : MonoBehaviour
    {
        private const string OPENTOPO_URL = "https://portal.opentopography.org/API/globaldem";
        
        [Header("API Settings")]
        public string backendUrl = "http://localhost:5000";
        public bool useLocalAPI = true; // If false, downloads directly from OpenTopography
        
        [Header("Settings")]
        public string apiKey = ""; // Will be loaded from config
        public string demType = "SRTMGL1"; // SRTM 30m resolution
        
        [Header("Paths")]
        public string demCachePath;
        public string demTempPath;
        
        // Store file ID from backend for processing
        public string lastFileId { get; private set; }
        
        private void Awake()
        {
            // Load API key from config
            LoadAPIConfig();
            
            // Set up paths
            demCachePath = Path.Combine(Application.streamingAssetsPath, "Heightmaps", "cache");
            demTempPath = Path.Combine(Application.streamingAssetsPath, "Heightmaps", "temp");
            
            // Create directories if they don't exist
            Directory.CreateDirectory(demCachePath);
            Directory.CreateDirectory(demTempPath);
        }
        
        /// <summary>
        /// Load API configuration from JSON file
        /// </summary>
        private void LoadAPIConfig()
        {
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "Config", "api_config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    
                    // Extract API key
                    int keyIndex = json.IndexOf("\"apiKey\"");
                    if (keyIndex > 0)
                    {
                        int colonIndex = json.IndexOf(":", keyIndex);
                        int quoteStart = json.IndexOf("\"", colonIndex + 1);
                        int quoteEnd = json.IndexOf("\"", quoteStart + 1);
                        
                        if (quoteStart > 0 && quoteEnd > quoteStart)
                        {
                            apiKey = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                            Debug.Log($"OpenTopography API key loaded: {apiKey.Substring(0, 8)}...");
                        }
                    }
                    
                    // Extract backend URL
                    int urlIndex = json.IndexOf("\"backendUrl\"");
                    if (urlIndex > 0)
                    {
                        int colonIndex = json.IndexOf(":", urlIndex);
                        int quoteStart = json.IndexOf("\"", colonIndex + 1);
                        int quoteEnd = json.IndexOf("\"", quoteStart + 1);
                        
                        if (quoteStart > 0 && quoteEnd > quoteStart)
                        {
                            string configUrl = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                            if (!string.IsNullOrEmpty(configUrl))
                            {
                                backendUrl = configUrl;
                            }
                        }
                    }
                    
                    // Extract useLocalAPI setting
                    int useLocalIndex = json.IndexOf("\"useLocalAPI\"");
                    if (useLocalIndex > 0)
                    {
                        int colonIndex = json.IndexOf(":", useLocalIndex);
                        string value = json.Substring(colonIndex + 1, 10).Trim();
                        useLocalAPI = value.StartsWith("true");
                    }
                    
                    if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
                    {
                        Debug.LogWarning("OpenTopography API key not configured! Get one from https://opentopography.org/");
                    }
                }
                else
                {
                    Debug.LogError($"API config file not found: {configPath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load API config: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Download DEM data for a specific location
        /// </summary>
        public async Task<string> DownloadDEM(LocationData location)
        {
            if (useLocalAPI)
            {
                return await DownloadDEMViaBackend(location);
            }
            else
            {
                return await DownloadDEMDirectly(location);
            }
        }
        
        /// <summary>
        /// Download DEM via backend API (returns file ID, not path)
        /// </summary>
        private async Task<string> DownloadDEMViaBackend(LocationData location)
        {
            string url = $"{backendUrl}/api/download-dem";
            
            // Create JSON request body
            string jsonBody = $"{{\"latitude\":{location.latitude},\"longitude\":{location.longitude},\"radius_km\":{location.radius},\"dem_type\":\"{demType}\",\"api_key\":\"{apiKey}\"}}";
            
            Debug.Log($"Downloading DEM via backend API: {url}");
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"Backend DEM download response: {jsonResponse}");
                    
                    // Extract file_id from response
                    int fileIdIndex = jsonResponse.IndexOf("\"file_id\"");
                    if (fileIdIndex > 0)
                    {
                        int colonIndex = jsonResponse.IndexOf(":", fileIdIndex);
                        int quoteStart = jsonResponse.IndexOf("\"", colonIndex + 1);
                        int quoteEnd = jsonResponse.IndexOf("\"", quoteStart + 1);
                        
                        if (quoteStart > 0 && quoteEnd > quoteStart)
                        {
                            lastFileId = jsonResponse.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                            Debug.Log($"✓ DEM downloaded via backend. File ID: {lastFileId}");
                            return lastFileId; // Return file ID, not path
                        }
                    }
                    
                    Debug.LogError("Failed to extract file_id from backend response");
                    return null;
                }
                else
                {
                    Debug.LogError($"Backend DEM download failed: {request.error}");
                    // Fallback to direct download
                    Debug.Log("Falling back to direct download...");
                    return await DownloadDEMDirectly(location);
                }
            }
        }
        
        /// <summary>
        /// Download DEM directly from OpenTopography (fallback)
        /// </summary>
        private async Task<string> DownloadDEMDirectly(LocationData location)
        {
            // Calculate bounding box based on location and radius
            float south = location.latitude - (location.radius / 111f);
            float north = location.latitude + (location.radius / 111f);
            float west = location.longitude - (location.radius / (111f * Mathf.Cos(location.latitude * Mathf.Deg2Rad)));
            float east = location.longitude + (location.radius / (111f * Mathf.Cos(location.latitude * Mathf.Deg2Rad)));
            
            string url = $"{OPENTOPO_URL}?demtype={demType}&south={south}&north={north}&west={west}&east={east}&outputFormat=GTiff&API_Key={apiKey}";
            
            string filename = $"dem_{location.locationName}_{System.DateTime.Now.Ticks}.tif";
            string filepath = Path.Combine(demTempPath, filename);
            
            Debug.Log($"Downloading DEM directly from: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(filepath, request.downloadHandler.data);
                    Debug.Log($"DEM downloaded successfully: {filepath}");
                    return filepath;
                }
                else
                {
                    Debug.LogError($"DEM download failed: {request.error}");
                    return null;
                }
            }
        }
    }
}
