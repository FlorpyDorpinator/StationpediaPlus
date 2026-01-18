using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StationpediaAscended.Tooltips
{
    /// <summary>
    /// Base class for all SPDA tooltip components.
    /// Handles common hover delay, pointer events, and tooltip display logic.
    /// </summary>
    public abstract class SPDABaseTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        protected string _deviceKey;
        protected string _cachedTooltipText;
        protected Coroutine _showCoroutine;
        protected bool _isHovering;
        
        protected const float HOVER_DELAY = 0.3f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            HideTooltip();
        }

        private IEnumerator ShowTooltipAfterDelay()
        {
            // Use WaitForSecondsRealtime so tooltips work when game is paused (Time.timeScale = 0)
            yield return new WaitForSecondsRealtime(HOVER_DELAY);
            
            if (_isHovering)
            {
                string tooltipText = GetTooltipText();
                if (!string.IsNullOrEmpty(tooltipText))
                {
                    StationpediaAscendedMod.CurrentTooltipText = tooltipText;
                    StationpediaAscendedMod.ShowTooltip = true;
                }
            }
        }

        /// <summary>
        /// Gets the tooltip text, using cached value if available.
        /// Override in derived classes to implement specific lookup logic.
        /// </summary>
        protected abstract string GetTooltipText();

        /// <summary>
        /// Clears the cached tooltip text, forcing a refresh on next hover.
        /// </summary>
        public void ClearCache()
        {
            _cachedTooltipText = null;
        }

        protected void OnDisable()
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            HideTooltip();
        }

        protected void HideTooltip()
        {
            StationpediaAscendedMod.ShowTooltip = false;
            StationpediaAscendedMod.CurrentTooltipText = "";
        }

        /// <summary>
        /// Cleans a name by removing HTML/rich text tags.
        /// </summary>
        protected static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return Regex.Replace(name, "<[^>]+>", "").Trim();
        }
    }
}
