using UnityEngine;
using Plantopia.Geocoding;
using Plantopia.Core;
using System.Threading.Tasks;

namespace Plantopia.Testing
{
    /// <summary>
    /// Simple test script to verify geocoding works
    /// Attach to any GameObject and it will run on start
    /// </summary>
    public class GeocodeTest : MonoBehaviour
    {
        [Header("Test Settings")]
        public string testLocationName = "Grand Canyon";
        public bool runOnStart = false;
        
        private GeocodeManager geocodeManager;
        
        private void Start()
        {
            geocodeManager = gameObject.AddComponent<GeocodeManager>();
            
            if (runOnStart)
            {
                TestGeocode();
            }
        }
        
        [ContextMenu("Run Geocode Test")]
        public async void TestGeocode()
        {
            Debug.Log($"Testing geocoding for: {testLocationName}");
            
            try
            {
                LocationData result = await geocodeManager.GeocodeLocation(testLocationName);
                
                if (result != null)
                {
                    Debug.Log($"✅ SUCCESS! Location: {result.locationName}");
                    Debug.Log($"   Latitude: {result.latitude}");
                    Debug.Log($"   Longitude: {result.longitude}");
                    Debug.Log($"   Radius: {result.radius}km");
                }
                else
                {
                    Debug.LogError("❌ FAILED - No location found");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ EXCEPTION: {ex.Message}");
                Debug.LogException(ex);
            }
        }
    }
}
