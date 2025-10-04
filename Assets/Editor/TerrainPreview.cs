using UnityEngine;
using UnityEditor;

namespace Plantopia.Editor
{
    /// <summary>
    /// Preview terrain in the editor
    /// </summary>
    public class TerrainPreview : EditorWindow
    {
        private Terrain selectedTerrain;
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/Plantopia/Terrain Preview")]
        public static void ShowWindow()
        {
            GetWindow<TerrainPreview>("Terrain Preview");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Terrain Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            selectedTerrain = (Terrain)EditorGUILayout.ObjectField(
                "Terrain", 
                selectedTerrain, 
                typeof(Terrain), 
                true);
            
            if (selectedTerrain == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a terrain to preview its properties.",
                    MessageType.Info);
                return;
            }
            
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                DisplayTerrainInfo();
            }
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Focus on Terrain"))
            {
                FocusOnTerrain();
            }
        }
        
        private void DisplayTerrainInfo()
        {
            TerrainData terrainData = selectedTerrain.terrainData;
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Terrain Information", EditorStyles.boldLabel);
                
                EditorGUILayout.LabelField("Name:", selectedTerrain.name);
                EditorGUILayout.LabelField("Size:", terrainData.size.ToString());
                EditorGUILayout.LabelField("Heightmap Resolution:", 
                    terrainData.heightmapResolution.ToString());
                EditorGUILayout.LabelField("Detail Resolution:", 
                    terrainData.detailResolution.ToString());
                EditorGUILayout.LabelField("Base Map Resolution:", 
                    terrainData.baseMapResolution.ToString());
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Elevation Statistics", EditorStyles.boldLabel);
                
                float minHeight = 0f;
                float maxHeight = 0f;
                CalculateHeightRange(terrainData, out minHeight, out maxHeight);
                
                EditorGUILayout.LabelField("Min Height:", $"{minHeight:F2}m");
                EditorGUILayout.LabelField("Max Height:", $"{maxHeight:F2}m");
                EditorGUILayout.LabelField("Height Range:", $"{(maxHeight - minHeight):F2}m");
            }
            EditorGUILayout.EndVertical();
        }
        
        private void CalculateHeightRange(TerrainData terrainData, out float min, out float max)
        {
            float[,] heights = terrainData.GetHeights(0, 0, 
                terrainData.heightmapResolution, 
                terrainData.heightmapResolution);
            
            min = float.MaxValue;
            max = float.MinValue;
            
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    float height = heights[y, x] * terrainData.size.y;
                    if (height < min) min = height;
                    if (height > max) max = height;
                }
            }
        }
        
        private void FocusOnTerrain()
        {
            if (selectedTerrain != null)
            {
                Selection.activeGameObject = selectedTerrain.gameObject;
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }
    }
}
