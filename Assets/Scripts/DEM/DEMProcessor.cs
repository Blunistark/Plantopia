using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Plantopia.DEM
{
    /// <summary>
    /// Processes DEM files using Backend API or local Python scripts
    /// </summary>
    public class DEMProcessor : MonoBehaviour
    {
        [Header("API Settings")]
        public string backendUrl = "http://localhost:5000";
        public bool useLocalAPI = true; // If false, uses local Python processing
        
        [Header("Settings")]
        public string pythonExecutable = "python";
        public int heightmapResolution = 513;
        
        private string pythonScriptPath;
        private string heightmapOutputPath;
        private bool pathsInitialized = false;
        
        private void Awake()
        {
            LoadAPIConfig();
            InitializePaths();
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
                UnityEngine.Debug.LogWarning($"Failed to load API config: {ex.Message}. Using defaults.");
            }
        }
        
        private void InitializePaths()
        {
            if (pathsInitialized) return;
            
            // Try alternative script first (uses rasterio instead of GDAL - easier Windows install)
            string altScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "dem_processor_alt.py");
            string mainScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "dem_processor.py");
            
            // Use alternative script if main doesn't exist, or prefer alternative for Windows
            pythonScriptPath = File.Exists(altScriptPath) ? altScriptPath : mainScriptPath;
            
            heightmapOutputPath = Path.Combine(Application.streamingAssetsPath, "Heightmaps", "temp");
            Directory.CreateDirectory(heightmapOutputPath);
            
            UnityEngine.Debug.Log($"Python script path: {pythonScriptPath}");
            UnityEngine.Debug.Log($"Heightmap output path: {heightmapOutputPath}");
            UnityEngine.Debug.Log($"Script exists: {File.Exists(pythonScriptPath)}");
            
            pathsInitialized = true;
        }
        
        /// <summary>
        /// Process DEM file to generate heightmap
        /// Can take either a file path (direct download) or file ID (backend API)
        /// </summary>
        public async Task<string> ProcessDEM(string demFilePathOrId)
        {
            if (useLocalAPI && !demFilePathOrId.Contains("\\") && !demFilePathOrId.Contains("/"))
            {
                // It's a file ID from backend API
                return await ProcessDEMViaBackend(demFilePathOrId);
            }
            else
            {
                // It's a file path for local processing
                return await ProcessDEMLocally(demFilePathOrId);
            }
        }
        
        /// <summary>
        /// Process DEM via backend API using file ID
        /// </summary>
        private async Task<string> ProcessDEMViaBackend(string fileId)
        {
            string url = $"{backendUrl}/api/process-dem";
            
            // Create JSON request body
            string jsonBody = $"{{\"file_id\":\"{fileId}\",\"resolution\":{heightmapResolution}}}";
            
            UnityEngine.Debug.Log($"Processing DEM via backend API: {url}");
            
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
                    // Save heightmap PNG from response
                    byte[] heightmapData = request.downloadHandler.data;
                    
                    // Ensure output directory exists
                    InitializePaths();
                    
                    string outputFilename = $"heightmap_{fileId}.png";
                    string outputPath = Path.Combine(heightmapOutputPath, outputFilename);
                    
                    File.WriteAllBytes(outputPath, heightmapData);
                    
                    UnityEngine.Debug.Log($"✓ Heightmap processed via backend: {outputPath} ({heightmapData.Length} bytes)");
                    
                    // Cleanup backend files
                    await CleanupBackendFiles(fileId);
                    
                    return outputPath;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Backend DEM processing failed: {request.error}");
                    return null;
                }
            }
        }
        
        /// <summary>
        /// Process DEM locally using Python script
        /// </summary>
        private async Task<string> ProcessDEMLocally(string demFilePath)
        {
            // Ensure paths are initialized (in case Awake hasn't run yet)
            InitializePaths();
            
            if (string.IsNullOrEmpty(pythonScriptPath) || !File.Exists(pythonScriptPath))
            {
                UnityEngine.Debug.LogError($"Python script not found: {pythonScriptPath}");
                return null;
            }
            
            string outputFilename = $"heightmap_{System.DateTime.Now.Ticks}.png";
            string outputPath = Path.Combine(heightmapOutputPath, outputFilename);
            
            // Build Python command
            string arguments = $"\"{pythonScriptPath}\" \"{demFilePath}\" \"{outputPath}\" {heightmapResolution}";
            
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            UnityEngine.Debug.Log($"Running Python command: {pythonExecutable} {arguments}");
            
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    
                    await Task.Run(() => process.WaitForExit());
                    
                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"DEM processing completed successfully: {output}");
                        return outputPath;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"DEM processing failed: {error}");
                        return null;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to run Python script: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Cleanup temporary files on backend
        /// </summary>
        private async Task CleanupBackendFiles(string fileId)
        {
            try
            {
                string url = $"{backendUrl}/api/cleanup";
                string jsonBody = $"{{\"file_id\":\"{fileId}\"}}";
                
                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    
                    await request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        UnityEngine.Debug.Log($"Backend files cleaned up for: {fileId}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to cleanup backend files: {ex.Message}");
            }
        }
    }
}
