using UnityEngine;
using UnityEditor;
using Plantopia.Core;
using Plantopia.Geocoding;
using Plantopia.DEM;
using Plantopia.UI;

namespace Plantopia.Editor
{
    /// <summary>
    /// Quick setup utility to create a complete terrain loader setup
    /// </summary>
    public class QuickSetup : EditorWindow
    {
        [MenuItem("Tools/Plantopia/Quick Setup Scene")]
        public static void ShowWindow()
        {
            GetWindow<QuickSetup>("Quick Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Plantopia Quick Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "This will create a complete TerrainLoader setup in your scene with all required components.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Terrain Loader Setup", GUILayout.Height(40)))
            {
                CreateTerrainLoaderSetup();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create System Tester", GUILayout.Height(30)))
            {
                CreateSystemTester();
            }
        }
        
        private void CreateTerrainLoaderSetup()
        {
            // Check if already exists
            TerrainLoaderController existing = FindObjectOfType<TerrainLoaderController>();
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Already Exists", 
                    "A TerrainLoader already exists in the scene. Create another one?", 
                    "Yes", "No"))
                {
                    return;
                }
            }
            
            // Create main GameObject
            GameObject terrainLoader = new GameObject("TerrainLoader");
            
            // Add all components
            TerrainLoaderController controller = terrainLoader.AddComponent<TerrainLoaderController>();
            GeocodeManager geocoder = terrainLoader.AddComponent<GeocodeManager>();
            DEMDownloader downloader = terrainLoader.AddComponent<DEMDownloader>();
            DEMProcessor processor = terrainLoader.AddComponent<DEMProcessor>();
            TerrainGenerator generator = terrainLoader.AddComponent<TerrainGenerator>();
            UIController uiController = terrainLoader.AddComponent<UIController>();
            ProgressController progressController = terrainLoader.AddComponent<ProgressController>();
            
            // Wire up references using SerializedObject
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("geocodeManager").objectReferenceValue = geocoder;
            so.FindProperty("demDownloader").objectReferenceValue = downloader;
            so.FindProperty("demProcessor").objectReferenceValue = processor;
            so.FindProperty("terrainGenerator").objectReferenceValue = generator;
            so.FindProperty("uiController").objectReferenceValue = uiController;
            so.FindProperty("progressController").objectReferenceValue = progressController;
            so.ApplyModifiedProperties();
            
            // Select the new object
            Selection.activeGameObject = terrainLoader;
            
            Debug.Log("✓ TerrainLoader setup created successfully!");
            EditorUtility.DisplayDialog("Success", 
                "TerrainLoader setup created!\n\n" +
                "You can now use the TerrainLoaderEditor in the Inspector to test terrain loading.", 
                "OK");
        }
        
        private void CreateSystemTester()
        {
            // Check if already exists
            SystemTester existing = FindObjectOfType<SystemTester>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("Already Exists", 
                    "SystemTester already exists in the scene. Selected it for you.", 
                    "OK");
                return;
            }
            
            // Create SystemTester GameObject
            GameObject tester = new GameObject("SystemTester");
            SystemTester systemTester = tester.AddComponent<SystemTester>();
            
            // Set default test location
            SerializedObject so = new SerializedObject(systemTester);
            so.FindProperty("testLocation").stringValue = "Grand Canyon";
            so.ApplyModifiedProperties();
            
            // Select the new object
            Selection.activeGameObject = tester;
            
            Debug.Log("✓ SystemTester created successfully!");
            EditorUtility.DisplayDialog("Success", 
                "SystemTester created!\n\n" +
                "Right-click the component in Inspector to run tests:\n" +
                "- Run All Tests\n" +
                "- Test Full Pipeline\n" +
                "- Test Geocoding", 
                "OK");
        }
    }
}
