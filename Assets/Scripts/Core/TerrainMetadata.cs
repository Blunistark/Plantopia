using UnityEngine;

namespace Plantopia.Core
{
    /// <summary>
    /// Metadata for terrain generation
    /// </summary>
    [System.Serializable]
    public class TerrainMetadata
    {
        public LocationData location;
        public int heightmapResolution;
        public Vector3 terrainSize;
        public float heightScale;
        public string demFilePath;
        public string heightmapPath;
        public System.DateTime createdDate;
        
        public TerrainMetadata()
        {
            heightmapResolution = 4097;  // Maximum resolution (2^12 + 1) for highest detail
            terrainSize = new Vector3(2000, 600, 2000);
            heightScale = 1.0f;
            createdDate = System.DateTime.Now;
        }
    }
}
