using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using Plantopia.Core;
using System.Collections.Generic;
using System.IO;

namespace Plantopia.Geocoding
{
    /// <summary>
    /// Manages geocoding operations using Backend API or Nominatim fallback
    /// </summary>
    public class GeocodeManager : MonoBehaviour
    {
        [Header("API Settings")]
        public string backendUrl = "http://localhost:5000";
        public bool useLocalAPI = true; // If false, uses direct Nominatim
        
        [Header("Fallback Settings")]
        private const string NOMINATIM_URL = "https://nominatim.openstreetmap.org/search";
        public string userAgent = "Plantopia/1.0";
        
        private void Awake()
        {
            LoadAPIConfig();
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
                                Debug.Log($"Backend URL loaded: {backendUrl}");
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
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load API config: {ex.Message}. Using defaults.");
            }
        }
        
        /// <summary>
        /// Simple JSON parser for Nominatim response
        /// Unity's JsonUtility requires public fields
        /// </summary>
        [System.Serializable]
        public class NominatimResult
        {
            public string place_id;
            public string lat;
            public string lon;
            public string display_name;
            public string name;
        }
        
        /// <summary>
        /// Geocode a location name to get coordinates
        /// </summary>
        public async Task<LocationData> GeocodeLocation(string locationName)
        {
            if (useLocalAPI)
            {
                return await GeocodeWithBackendAPI(locationName);
            }
            else
            {
                return await GeocodeWithNominatim(locationName);
            }
        }
        
        /// <summary>
        /// Geocode using backend API
        /// </summary>
        private async Task<LocationData> GeocodeWithBackendAPI(string locationName)
        {
            string url = $"{backendUrl}/api/geocode";
            
            // Create JSON request body
            string jsonBody = $"{{\"location\":\"{locationName}\"}}";
            
            Debug.Log($"Geocoding via backend API: {url}");
            
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
                    Debug.Log($"Backend geocoding response: {jsonResponse}");
                    
                    try
                    {
                        return ParseBackendResponse(jsonResponse, locationName);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Failed to parse backend response: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"Backend geocoding failed: {request.error}");
                    // Fallback to direct Nominatim
                    Debug.Log("Falling back to direct Nominatim API...");
                    return await GeocodeWithNominatim(locationName);
                }
            }
        }
        
        /// <summary>
        /// Geocode using direct Nominatim API (fallback)
        /// </summary>
        private async Task<LocationData> GeocodeWithNominatim(string locationName)
        {
            string url = $"{NOMINATIM_URL}?q={UnityWebRequest.EscapeURL(locationName)}&format=json&limit=1";
            
            Debug.Log($"Geocoding via Nominatim: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", userAgent);
                
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    return ParseNominatimResponse(jsonResponse, locationName);
                }
                else
                {
                    Debug.LogError($"Nominatim geocoding failed: {request.error}");
                    return null;
                }
            }
        }
        
        /// <summary>
        /// Parse backend API response
        /// </summary>
        private LocationData ParseBackendResponse(string jsonResponse, string locationName)
        {
            string lat = ExtractJsonValue(jsonResponse, "latitude");
            string lon = ExtractJsonValue(jsonResponse, "longitude");
            string displayName = ExtractJsonValue(jsonResponse, "display_name");
            
            if (!string.IsNullOrEmpty(lat) && !string.IsNullOrEmpty(lon))
            {
                float latValue = float.Parse(lat, System.Globalization.CultureInfo.InvariantCulture);
                float lonValue = float.Parse(lon, System.Globalization.CultureInfo.InvariantCulture);
                
                string finalName = !string.IsNullOrEmpty(displayName) ? displayName : locationName;
                LocationData location = new LocationData(finalName, latValue, lonValue);
                
                Debug.Log($"✓ Successfully geocoded via backend: {location}");
                return location;
            }
            
            return null;
        }
        
        /// <summary>
        /// Parse Nominatim JSON response using simple string parsing
        /// Unity's JsonUtility has issues with the Nominatim response format
        /// </summary>
        private LocationData ParseNominatimResponse(string jsonResponse, string locationName)
        {
            // Remove array brackets if present
            if (jsonResponse.StartsWith("["))
            {
                jsonResponse = jsonResponse.Substring(1);
                int endIndex = jsonResponse.IndexOf(']');
                if (endIndex > 0)
                {
                    jsonResponse = jsonResponse.Substring(0, endIndex);
                }
            }
            
            // Check if empty
            if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse == "")
            {
                Debug.LogWarning("Empty JSON response received");
                return null;
            }
            
            Debug.Log($"Parsing JSON: {jsonResponse.Substring(0, Mathf.Min(200, jsonResponse.Length))}...");
            
            try
            {
                // Manual parsing - more reliable than JsonUtility for this case
                string lat = ExtractJsonValue(jsonResponse, "lat");
                string lon = ExtractJsonValue(jsonResponse, "lon");
                string displayName = ExtractJsonValue(jsonResponse, "display_name");
                
                if (!string.IsNullOrEmpty(lat) && !string.IsNullOrEmpty(lon))
                {
                    Debug.Log($"Parsed - lat: {lat}, lon: {lon}, name: {displayName}");
                    
                    float latValue = float.Parse(lat, System.Globalization.CultureInfo.InvariantCulture);
                    float lonValue = float.Parse(lon, System.Globalization.CultureInfo.InvariantCulture);
                    
                    string finalName = !string.IsNullOrEmpty(displayName) ? displayName : locationName;
                    
                    LocationData location = new LocationData(finalName, latValue, lonValue);
                    Debug.Log($"✓ Successfully created LocationData: {location}");
                    
                    return location;
                }
                else
                {
                    Debug.LogWarning($"Could not extract lat/lon from JSON. Lat: '{lat}', Lon: '{lon}'");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"JSON parsing error: {ex.Message}\n{ex.StackTrace}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Extract a value from JSON string by key
        /// </summary>
        private string ExtractJsonValue(string json, string key)
        {
            string searchKey = $"\"{key}\":";
            int keyIndex = json.IndexOf(searchKey);
            
            if (keyIndex < 0)
            {
                return null;
            }
            
            int valueStart = keyIndex + searchKey.Length;
            
            // Skip whitespace
            while (valueStart < json.Length && (json[valueStart] == ' ' || json[valueStart] == '\t'))
            {
                valueStart++;
            }
            
            if (valueStart >= json.Length)
            {
                return null;
            }
            
            // Check if it's a string value (starts with ")
            if (json[valueStart] == '"')
            {
                valueStart++; // Skip opening quote
                int valueEnd = json.IndexOf('"', valueStart);
                if (valueEnd > valueStart)
                {
                    return json.Substring(valueStart, valueEnd - valueStart);
                }
            }
            else
            {
                // It's a number or other value
                int valueEnd = valueStart;
                while (valueEnd < json.Length && 
                       json[valueEnd] != ',' && 
                       json[valueEnd] != '}' && 
                       json[valueEnd] != ']')
                {
                    valueEnd++;
                }
                
                if (valueEnd > valueStart)
                {
                    return json.Substring(valueStart, valueEnd - valueStart).Trim();
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Reverse geocode coordinates to get location name
        /// </summary>
        public async Task<string> ReverseGeocode(float latitude, float longitude)
        {
            string url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("User-Agent", userAgent);
                
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    
                    try
                    {
                        NominatimResult result = JsonUtility.FromJson<NominatimResult>(jsonResponse);
                        return result.display_name ?? "Unknown Location";
                    }
                    catch
                    {
                        return "Unknown Location";
                    }
                }
                else
                {
                    Debug.LogError($"Reverse geocoding failed: {request.error}");
                    return "Unknown Location";
                }
            }
        }
    }
}
