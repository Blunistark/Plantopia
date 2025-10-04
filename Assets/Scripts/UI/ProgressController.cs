using UnityEngine;
using UnityEngine.UI;

namespace Plantopia.UI
{
    /// <summary>
    /// Manages progress bar display and updates
    /// </summary>
    public class ProgressController : MonoBehaviour
    {
        [Header("Progress Bar")]
        public Slider progressSlider;
        public Text progressText;
        public Text statusMessageText;
        
        [Header("Settings")]
        public bool showPercentage = true;
        
        private void Start()
        {
            // Initialize progress bar
            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = 0f;
            }
        }
        
        /// <summary>
        /// Update progress bar
        /// </summary>
        public void UpdateProgress(float progress, string message = "")
        {
            // Clamp progress between 0 and 1
            progress = Mathf.Clamp01(progress);
            
            // Update slider
            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }
            
            // Update percentage text
            if (progressText != null && showPercentage)
            {
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
            
            // Update status message
            if (statusMessageText != null && !string.IsNullOrEmpty(message))
            {
                statusMessageText.text = message;
            }
            
            Debug.Log($"Progress: {progress * 100:F1}% - {message}");
        }
        
        /// <summary>
        /// Reset progress bar
        /// </summary>
        public void ResetProgress()
        {
            UpdateProgress(0f, "Ready");
        }
        
        /// <summary>
        /// Complete progress bar
        /// </summary>
        public void CompleteProgress(string message = "Completed!")
        {
            UpdateProgress(1f, message);
        }
    }
}
