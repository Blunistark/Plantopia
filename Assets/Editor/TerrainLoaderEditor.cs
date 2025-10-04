using UnityEngine;
using UnityEditor;
using Plantopia.Core;

namespace Plantopia.Editor
{
    /// <summary>
    /// Custom inspector for TerrainLoaderController
    /// </summary>
    [CustomEditor(typeof(TerrainLoaderController))]
    public class TerrainLoaderEditor : UnityEditor.Editor
    {
        private string testLocationName = "";
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            TerrainLoaderController controller = (TerrainLoaderController)target;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Test Terrain Loading", EditorStyles.miniLabel);
                testLocationName = EditorGUILayout.TextField("Location Name:", testLocationName);
                
                if (GUILayout.Button("Load Test Terrain"))
                {
                    if (!string.IsNullOrEmpty(testLocationName))
                    {
                        controller.LoadTerrainFromLocation(testLocationName);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Please enter a location name", "OK");
                    }
                }
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniLabel);
                
                if (GUILayout.Button("Open StreamingAssets Folder"))
                {
                    EditorUtility.RevealInFinder(Application.streamingAssetsPath);
                }
                
                if (GUILayout.Button("Clear Cache"))
                {
                    if (EditorUtility.DisplayDialog("Clear Cache", 
                        "This will delete all cached DEM and heightmap files. Continue?", 
                        "Yes", "No"))
                    {
                        ClearCache();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void ClearCache()
        {
            string demCache = System.IO.Path.Combine(Application.streamingAssetsPath, "DEM", "cache");
            string heightmapCache = System.IO.Path.Combine(Application.streamingAssetsPath, "Heightmaps", "cache");
            
            if (System.IO.Directory.Exists(demCache))
            {
                System.IO.Directory.Delete(demCache, true);
                System.IO.Directory.CreateDirectory(demCache);
            }
            
            if (System.IO.Directory.Exists(heightmapCache))
            {
                System.IO.Directory.Delete(heightmapCache, true);
                System.IO.Directory.CreateDirectory(heightmapCache);
            }
            
            Debug.Log("Cache cleared successfully");
        }
    }
}
