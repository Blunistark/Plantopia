using UnityEngine;
using System.IO;
using Plantopia.Core;

namespace Plantopia.DEM
{
    /// <summary>
    /// Generates Unity terrain from heightmap data
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Settings")]
        public int heightmapResolution = 513;
        public Vector3 terrainSize = new Vector3(2000, 600, 2000);
        public Material terrainMaterial;
        
        [Header("Generated Terrain")]
        public Terrain currentTerrain;
        
        /// <summary>
        /// Generate terrain from heightmap file
        /// </summary>
        public Terrain GenerateTerrainFromHeightmap(string heightmapPath, TerrainMetadata metadata)
        {
            if (!File.Exists(heightmapPath))
            {
                Debug.LogError($"Heightmap file not found: {heightmapPath}");
                return null;
            }
            
            // Load heightmap texture
            byte[] fileData = File.ReadAllBytes(heightmapPath);
            Texture2D heightmapTexture = new Texture2D(2, 2);
            heightmapTexture.LoadImage(fileData);
            
            // Create terrain data
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = metadata?.terrainSize ?? terrainSize;
            
            // Convert texture to heightmap
            float[,] heights = ConvertTextureToHeightmap(heightmapTexture, heightmapResolution);
            terrainData.SetHeights(0, 0, heights);
            
            // Create terrain game object
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = $"Terrain_{metadata?.location.locationName ?? "Generated"}";
            
            currentTerrain = terrainObject.GetComponent<Terrain>();
            
            // Apply material if available
            if (terrainMaterial != null)
            {
                currentTerrain.materialTemplate = terrainMaterial;
            }
            
            Debug.Log($"Terrain generated successfully: {terrainObject.name}");
            return currentTerrain;
        }
        
        /// <summary>
        /// Convert texture to heightmap array
        /// </summary>
        private float[,] ConvertTextureToHeightmap(Texture2D texture, int resolution)
        {
            float[,] heights = new float[resolution, resolution];
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // Sample texture and convert to height value
                    float u = (float)x / (resolution - 1);
                    float v = (float)y / (resolution - 1);
                    
                    Color pixel = texture.GetPixelBilinear(u, v);
                    heights[y, x] = pixel.grayscale;
                }
            }
            
            return heights;
        }
    }
}
