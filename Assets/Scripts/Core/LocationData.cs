using UnityEngine;

namespace Plantopia.Core
{
    /// <summary>
    /// Data structure for storing location information
    /// </summary>
    [System.Serializable]
    public class LocationData
    {
        public string locationName;
        public float latitude;
        public float longitude;
        public float radius; // in kilometers
        
        public LocationData(string name, float lat, float lon, float rad = 10f)
        {
            locationName = name;
            latitude = lat;
            longitude = lon;
            radius = rad;
        }
        
        public override string ToString()
        {
            return $"{locationName} ({latitude}, {longitude}) - Radius: {radius}km";
        }
    }
}
