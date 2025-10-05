using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Plantopia.Core;
using Debug = UnityEngine.Debug;

namespace Plantopia.DEM
{
    /// <summary>
    /// Processes DEM files using Python scripts or Backend API
    /// </summary>
    public class DEMProcessor : MonoBehaviour
    {
        [Header("Processing Settings")]
        public int defaultResolution = 4097;  // Maximum resolution (2^12 + 1) for highest detail
        public string pythonExecutable = "python";
        
        [Header("API Settings")]
        public bool useAPI = true;
        public string backendUrl = "http://localhost:5000";
        
        private string pythonScriptPath;
        private string heightmapOutputPath;
        private APIConfig apiConfig;

        void Awake()
        {
            // Initialize paths
            pythonScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "dem_processor_alt.py");
            heightmapOutputPath = Path.Combine(Application.streamingAssetsPath, "Heightmaps", "temp");
            
            // Ensure output directory exists
            if (!Directory.Exists(heightmapOutputPath))
            {
                Directory.CreateDirectory(heightmapOutputPath);
            }

            // Load API config
            LoadAPIConfig();
            
            Debug.Log($"DEMProcessor initialized. Using API: {useAPI}");
            Debug.Log($"Backend URL: {backendUrl}");
        }

        private void LoadAPIConfig()
        {
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "Config", "api_config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    apiConfig = JsonUtility.FromJson<APIConfig>(json);
                    
                    if (apiConfig != null)
                    {
                        useAPI = apiConfig.useLocalAPI;
                        backendUrl = apiConfig.backendUrl;
                        Debug.Log($"Loaded API config - useAPI: {useAPI}, backend: {backendUrl}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load API config: {e.Message}. Using defaults.");
            }
        }

        /// <summary>
        /// Process DEM file to heightmap
        /// fileIdOrPath: When using API, this is the file_id. When using local Python, this is the file path.
        /// </summary>
        public async Task<string> ProcessDEM(string fileIdOrPath)
        {
            if (string.IsNullOrEmpty(fileIdOrPath))
            {
                Debug.LogError("DEM file ID or path is empty");
                return null;
            }

            if (useAPI)
            {
                // fileIdOrPath is actually a file_id from the backend
                return await ProcessDEMWithAPI(fileIdOrPath);
            }
            else
            {
                // fileIdOrPath is a local file path
                if (!File.Exists(fileIdOrPath))
                {
                    Debug.LogError($"DEM file not found: {fileIdOrPath}");
                    return null;
                }
                return await ProcessDEMWithPython(fileIdOrPath);
            }
        }

        /// <summary>
        /// Process DEM using Backend API
        /// </summary>
        private async Task<string> ProcessDEMWithAPI(string fileId)
        {
            try
            {
                Debug.Log($"Processing DEM with Backend API. File ID: {fileId}");

                // Request heightmap processing - backend returns PNG directly
                string heightmapPath = await RequestHeightmapProcessing(fileId);
                if (string.IsNullOrEmpty(heightmapPath))
                {
                    Debug.LogError("Failed to process DEM to heightmap");
                    return null;
                }

                Debug.Log($"✓ Heightmap saved to: {heightmapPath}");
                return heightmapPath;
            }
            catch (Exception e)
            {
                Debug.LogError($"API processing failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Request heightmap processing from backend - returns PNG file directly
        /// </summary>
        private async Task<string> RequestHeightmapProcessing(string fileId)
        {
            var requestData = new ProcessRequest
            {
                file_id = fileId,
                resolution = defaultResolution,
                output_format = "png"
            };

            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

            Debug.Log($"Requesting heightmap processing: {backendUrl}/api/process-dem");
            Debug.Log($"Request data: {jsonData}");

            using (UnityWebRequest request = new UnityWebRequest($"{backendUrl}/api/process-dem", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                var operation = request.SendWebRequest();
                
                // Wait for completion
                var tcs = new TaskCompletionSource<bool>();
                operation.completed += _ => tcs.SetResult(true);
                await tcs.Task;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Backend returns PNG file directly, not JSON
                    byte[] pngData = request.downloadHandler.data;
                    Debug.Log($"✓ Received PNG data: {pngData.Length} bytes");
                    
                    // Ensure output path is initialized
                    if (string.IsNullOrEmpty(heightmapOutputPath))
                    {
                        heightmapOutputPath = Path.Combine(Application.streamingAssetsPath, "Heightmaps", "temp");
                        if (!Directory.Exists(heightmapOutputPath))
                        {
                            Directory.CreateDirectory(heightmapOutputPath);
                        }
                    }
                    
                    // Save PNG to file
                    string outputPath = Path.Combine(heightmapOutputPath, $"heightmap_{fileId}.png");
                    File.WriteAllBytes(outputPath, pngData);
                    
                    Debug.Log($"✓ Heightmap saved to: {outputPath}");
                    return outputPath;
                }
                else
                {
                    Debug.LogError($"Processing failed: {request.error}");
                    Debug.LogError($"Response code: {request.responseCode}");
                    if (request.downloadHandler != null)
                    {
                        Debug.LogError($"Response: {request.downloadHandler.text}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Download heightmap from backend
        /// </summary>
        private async Task<string> DownloadHeightmap(string heightmapId)
        {
            string url = $"{backendUrl}/api/heightmap/{heightmapId}";
            string outputPath = Path.Combine(heightmapOutputPath, $"heightmap_{heightmapId}.png");

            Debug.Log($"Downloading heightmap from: {url}");

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                var operation = request.SendWebRequest();
                
                // Wait for completion
                var tcs = new TaskCompletionSource<bool>();
                operation.completed += _ => tcs.SetResult(true);
                await tcs.Task;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Save the texture to file
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    byte[] pngData = texture.EncodeToPNG();
                    File.WriteAllBytes(outputPath, pngData);
                    
                    Debug.Log($"✓ Heightmap downloaded and saved: {outputPath}");
                    Debug.Log($"Heightmap size: {texture.width}x{texture.height}");
                    return outputPath;
                }
                else
                {
                    Debug.LogError($"Download failed: {request.error}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                }
            }

            return null;
        }

        /// <summary>
        /// Process DEM using local Python script
        /// </summary>
        private async Task<string> ProcessDEMWithPython(string demFilePath)
        {
            if (!File.Exists(pythonScriptPath))
            {
                Debug.LogError($"Python script not found: {pythonScriptPath}");
                return null;
            }

            string outputFileName = $"heightmap_{DateTime.Now.Ticks}.png";
            string outputPath = Path.Combine(heightmapOutputPath, outputFileName);

            string arguments = $"\"{pythonScriptPath}\" \"{demFilePath}\" \"{outputFileName}\" {defaultResolution}";
            
            Debug.Log($"Running Python command: {pythonExecutable} {arguments}");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pythonExecutable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = heightmapOutputPath
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Debug.LogError("Failed to start Python process");
                        return null;
                    }

                    // Read output asynchronously
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Wait for process to exit
                    await Task.Run(() => process.WaitForExit());

                    string output = await outputTask;
                    string error = await errorTask;

                    if (!string.IsNullOrEmpty(output))
                    {
                        Debug.Log($"Python output: {output}");
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"Python error: {error}");
                    }

                    if (process.ExitCode == 0 && File.Exists(outputPath))
                    {
                        Debug.Log($"✓ Heightmap generated: {outputPath}");
                        return outputPath;
                    }
                    else
                    {
                        Debug.LogError($"Python process failed with exit code: {process.ExitCode}");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception running Python script: {e.Message}");
                return null;
            }
        }

        #region Response Classes

        [Serializable]
        private class ProcessRequest
        {
            public string file_id;
            public int resolution;
            public string output_format;
        }

        [Serializable]
        private class ProcessResponse
        {
            public string heightmap_path;
            public string heightmap_id;
            public HeightmapMetadata metadata;
        }

        [Serializable]
        private class HeightmapMetadata
        {
            public int resolution;
            public float min_elevation;
            public float max_elevation;
            public float elevation_range;
        }

        #endregion
    }

    [Serializable]
    public class APIConfig
    {
        public string opentopography_api_key;
        public string dem_type;
        public int default_radius_km;
        public int heightmap_resolution;
        public bool useLocalAPI;
        public string backendUrl;
    }
}