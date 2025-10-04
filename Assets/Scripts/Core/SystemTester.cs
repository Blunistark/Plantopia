using UnityEngine;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Plantopia.Core
{
    /// <summary>
    /// System testing and diagnostic utility
    /// </summary>
    public class SystemTester : MonoBehaviour
    {
        [Header("Test Settings")]
        public bool testOnStart = false;
        public string testLocation = "New York";
        
        private void Start()
        {
            if (testOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== PLANTOPIA SYSTEM TEST ===");
            
            TestPaths();
            TestPythonInstallation();
            TestDependencies();
            TestAPIConfig();
            
            Debug.Log("=== TEST COMPLETE ===");
        }
        
        [ContextMenu("Test Paths")]
        public void TestPaths()
        {
            Debug.Log("--- Testing Paths ---");
            
            Debug.Log($"Application.dataPath: {Application.dataPath}");
            Debug.Log($"Application.streamingAssetsPath: {Application.streamingAssetsPath}");
            
            string demPath = Path.Combine(Application.streamingAssetsPath, "DEM");
            string scriptsPath = Path.Combine(Application.streamingAssetsPath, "Scripts");
            string configPath = Path.Combine(Application.streamingAssetsPath, "Config");
            
            Debug.Log($"DEM Path exists: {Directory.Exists(demPath)} - {demPath}");
            Debug.Log($"Scripts Path exists: {Directory.Exists(scriptsPath)} - {scriptsPath}");
            Debug.Log($"Config Path exists: {Directory.Exists(configPath)} - {configPath}");
            
            string pythonScript = Path.Combine(scriptsPath, "dem_processor_alt.py");
            Debug.Log($"Python script exists: {File.Exists(pythonScript)} - {pythonScript}");
        }
        
        [ContextMenu("Test Python Installation")]
        public void TestPythonInstallation()
        {
            Debug.Log("--- Testing Python Installation ---");
            
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        Debug.Log($"✓ Python found: {output.Trim()}");
                    }
                    else
                    {
                        Debug.LogError($"✗ Python version check failed: {error}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ Python not found: {ex.Message}");
            }
        }
        
        [ContextMenu("Test Python Dependencies")]
        public void TestDependencies()
        {
            Debug.Log("--- Testing Python Dependencies ---");
            
            string[] packages = { "numpy", "PIL", "scipy", "rasterio" };
            
            foreach (string package in packages)
            {
                TestPythonPackage(package);
            }
        }
        
        private void TestPythonPackage(string packageName)
        {
            try
            {
                string importName = packageName == "PIL" ? "PIL" : packageName;
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"-c \"import {importName}; print('{packageName} OK')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        Debug.Log($"✓ {packageName} installed");
                    }
                    else
                    {
                        Debug.LogWarning($"✗ {packageName} not found: {error}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ Failed to test {packageName}: {ex.Message}");
            }
        }
        
        [ContextMenu("Test API Config")]
        public void TestAPIConfig()
        {
            Debug.Log("--- Testing API Configuration ---");
            
            string configPath = Path.Combine(Application.streamingAssetsPath, "Config", "api_config.json");
            
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                Debug.Log($"✓ API config found: {configPath}");
                
                if (json.Contains("YOUR_API_KEY_HERE"))
                {
                    Debug.LogWarning("⚠ OpenTopography API key not configured!");
                    Debug.LogWarning("Get your API key from: https://opentopography.org/");
                }
                else
                {
                    Debug.Log("✓ API key appears to be configured");
                }
            }
            else
            {
                Debug.LogError($"✗ API config not found: {configPath}");
            }
        }
        
        [ContextMenu("Test Geocoding")]
        public void TestGeocoding()
        {
            var geocoder = FindObjectOfType<Plantopia.Geocoding.GeocodeManager>();
            if (geocoder != null)
            {
                Debug.Log($"Testing geocoding for: {testLocation}");
                TestGeocodingAsync(geocoder);
            }
            else
            {
                Debug.LogError("GeocodeManager not found in scene!");
            }
        }
        
        private async void TestGeocodingAsync(Plantopia.Geocoding.GeocodeManager geocoder)
        {
            var result = await geocoder.GeocodeLocation(testLocation);
            if (result != null)
            {
                Debug.Log($"✓ Geocoding successful: {result}");
            }
            else
            {
                Debug.LogError("✗ Geocoding failed");
            }
        }
        
        [ContextMenu("Test Full Pipeline")]
        public void TestFullPipeline()
        {
            var controller = FindObjectOfType<TerrainLoaderController>();
            if (controller != null)
            {
                Debug.Log($"=== STARTING FULL TERRAIN PIPELINE TEST ===");
                Debug.Log($"Location: {testLocation}");
                controller.LoadTerrainFromLocation(testLocation);
            }
            else
            {
                Debug.LogError("TerrainLoaderController not found in scene!");
                Debug.LogError("Make sure you have a GameObject with TerrainLoaderController component!");
            }
        }
    }
}
