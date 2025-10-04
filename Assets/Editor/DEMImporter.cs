using UnityEngine;
using UnityEditor;
using System.IO;

namespace Plantopia.Editor
{
    /// <summary>
    /// Custom import tools for DEM files
    /// </summary>
    public class DEMImporter : EditorWindow
    {
        private string demFilePath = "";
        private int heightmapResolution = 513;
        private Vector3 terrainSize = new Vector3(2000, 600, 2000);
        
        [MenuItem("Tools/Plantopia/DEM Importer")]
        public static void ShowWindow()
        {
            GetWindow<DEMImporter>("DEM Importer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("DEM Import Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "Import DEM files and convert them to Unity terrain.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("DEM File", EditorStyles.miniLabel);
                
                EditorGUILayout.BeginHorizontal();
                {
                    demFilePath = EditorGUILayout.TextField(demFilePath);
                    
                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        string path = EditorUtility.OpenFilePanel(
                            "Select DEM File", 
                            Application.streamingAssetsPath, 
                            "tif,tiff");
                        
                        if (!string.IsNullOrEmpty(path))
                        {
                            demFilePath = path;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Terrain Settings", EditorStyles.miniLabel);
                
                heightmapResolution = EditorGUILayout.IntSlider(
                    "Heightmap Resolution", 
                    heightmapResolution, 
                    33, 
                    4097);
                
                terrainSize = EditorGUILayout.Vector3Field("Terrain Size", terrainSize);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Import DEM", GUILayout.Height(30)))
            {
                ImportDEM();
            }
        }
        
        private void ImportDEM()
        {
            if (string.IsNullOrEmpty(demFilePath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a DEM file", "OK");
                return;
            }
            
            if (!File.Exists(demFilePath))
            {
                EditorUtility.DisplayDialog("Error", "DEM file not found", "OK");
                return;
            }
            
            Debug.Log($"Importing DEM: {demFilePath}");
            EditorUtility.DisplayDialog("Import Started", 
                "DEM import process started. Check console for progress.", 
                "OK");
            
            // TODO: Implement actual import logic
        }
    }
}
