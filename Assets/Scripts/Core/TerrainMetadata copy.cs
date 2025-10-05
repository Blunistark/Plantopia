using UnityEngine;

[System.Serializable]
public class TerrainMetadata
{
    public float minElevation;
    public float maxElevation;
    public float elevationRange;
    public int heightmapSize;
    public float realWorldWidth;
    public float realWorldHeight;
    public string demType;
    public string filePath;
    
    public TerrainMetadata()
    {
    }
    
    public void CalculateRange()
    {
        elevationRange = maxElevation - minElevation;
    }
    
    public float GetTerrainHeight()
    {
        // Return appropriate terrain height for Unity
        return Mathf.Max(elevationRange, 100f);
    }
}
