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
            
            // Convert texture to heightmap with smoothing
            float[,] heights = ConvertTextureToHeightmap(heightmapTexture, heightmapResolution);
            
            // Apply smoothing to reduce noise
            heights = SmoothHeightmap(heights, heightmapResolution);
            
            terrainData.SetHeights(0, 0, heights);
            
            // Setup terrain layers (textures)
            SetupTerrainLayers(terrainData);
            
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
        
        /// <summary>
        /// Apply smoothing filter to heightmap to reduce noise and spikes
        /// </summary>
        private float[,] SmoothHeightmap(float[,] heights, int resolution)
        {
            float[,] smoothed = new float[resolution, resolution];
            int kernelSize = 3; // 3x3 smoothing kernel
            int halfKernel = kernelSize / 2;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    
                    // Average with neighbors
                    for (int ky = -halfKernel; ky <= halfKernel; ky++)
                    {
                        for (int kx = -halfKernel; kx <= halfKernel; kx++)
                        {
                            int nx = Mathf.Clamp(x + kx, 0, resolution - 1);
                            int ny = Mathf.Clamp(y + ky, 0, resolution - 1);
                            
                            sum += heights[ny, nx];
                            count++;
                        }
                    }
                    
                    smoothed[y, x] = sum / count;
                }
            }
            
            return smoothed;
        }
        
        /// <summary>
        /// Setup terrain texture layers
        /// </summary>
        private void SetupTerrainLayers(TerrainData terrainData)
        {
            // Create simple procedural textures for now
            TerrainLayer[] layers = new TerrainLayer[3];
            
            // Layer 0: Base grass (low elevations)
            layers[0] = new TerrainLayer();
            layers[0].diffuseTexture = CreateColorTexture(new Color(0.4f, 0.5f, 0.3f)); // Green
            layers[0].tileSize = new Vector2(15, 15);
            
            // Layer 1: Rock (mid elevations)
            layers[1] = new TerrainLayer();
            layers[1].diffuseTexture = CreateColorTexture(new Color(0.5f, 0.5f, 0.5f)); // Gray
            layers[1].tileSize = new Vector2(15, 15);
            
            // Layer 2: Snow (high elevations)
            layers[2] = new TerrainLayer();
            layers[2].diffuseTexture = CreateColorTexture(new Color(0.9f, 0.9f, 0.95f)); // White
            layers[2].tileSize = new Vector2(15, 15);
            
            terrainData.terrainLayers = layers;
            
            // Paint layers based on height
            PaintTerrainLayers(terrainData);
        }
        
        /// <summary>
        /// Create a simple colored texture
        /// </summary>
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Paint terrain layers based on elevation
        /// </summary>
        private void PaintTerrainLayers(TerrainData terrainData)
        {
            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            
            float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, 3];
            
            for (int y = 0; y < alphamapHeight; y++)
            {
                for (int x = 0; x < alphamapWidth; x++)
                {
                    // Get normalized height at this position
                    float height = terrainData.GetHeight(x, y) / terrainData.size.y;
                    
                    // Calculate layer weights based on height
                    float grassWeight = Mathf.Clamp01(1f - height * 2f); // 0-0.5 height
                    float rockWeight = Mathf.Clamp01(1f - Mathf.Abs(height - 0.5f) * 2f); // 0.25-0.75 height
                    float snowWeight = Mathf.Clamp01((height - 0.6f) * 2.5f); // 0.6-1.0 height
                    
                    // Normalize weights
                    float totalWeight = grassWeight + rockWeight + snowWeight;
                    if (totalWeight > 0)
                    {
                        grassWeight /= totalWeight;
                        rockWeight /= totalWeight;
                        snowWeight /= totalWeight;
                    }
                    
                    splatmapData[x, y, 0] = grassWeight;
                    splatmapData[x, y, 1] = rockWeight;
                    splatmapData[x, y, 2] = snowWeight;
                }
            }
            
            terrainData.SetAlphamaps(0, 0, splatmapData);
        }
    }
}
