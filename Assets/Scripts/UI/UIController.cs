using UnityEngine;
using UnityEngine.UI;

namespace Plantopia.UI
{
    /// <summary>
    /// Manages UI controls and user interactions
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("UI References")]
        public InputField locationInputField;
        public Button loadTerrainButton;
        public Text statusText;
        
        [Header("Panels")]
        public GameObject locationInputPanel;
        public GameObject progressBarPanel;
        public GameObject statusPanel;
        
        private void Start()
        {
            // Setup button listeners
            if (loadTerrainButton != null)
            {
                loadTerrainButton.onClick.AddListener(OnLoadTerrainClicked);
            }
        }
        
        private void OnLoadTerrainClicked()
        {
            if (locationInputField != null && !string.IsNullOrEmpty(locationInputField.text))
            {
                string location = locationInputField.text;
                Debug.Log($"Load terrain requested for: {location}");
                
                // Notify controller
                // TerrainLoaderController will handle the actual loading
            }
            else
            {
                UpdateStatus("Please enter a valid location name", Color.red);
            }
        }
        
        /// <summary>
        /// Update status text
        /// </summary>
        public void UpdateStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
            Debug.Log($"Status: {message}");
        }
        
        /// <summary>
        /// Show/hide panels
        /// </summary>
        public void SetPanelActive(string panelName, bool active)
        {
            switch (panelName)
            {
                case "LocationInput":
                    if (locationInputPanel != null) locationInputPanel.SetActive(active);
                    break;
                case "ProgressBar":
                    if (progressBarPanel != null) progressBarPanel.SetActive(active);
                    break;
                case "Status":
                    if (statusPanel != null) statusPanel.SetActive(active);
                    break;
            }
        }
    }
}
