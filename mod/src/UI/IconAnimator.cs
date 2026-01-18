using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StationpediaAscended.UI
{
    /// <summary>
    /// Animates icon transition between expanded/collapsed states with a scale "pop" effect
    /// </summary>
    public class IconAnimator : MonoBehaviour
    {
        public Image TargetImage;
        public Sprite ExpandedSprite;
        public Sprite CollapsedSprite;
        public float AnimationDuration = 0.15f;
        
        private bool _isExpanded = false;
        private Coroutine _currentAnimation;
        
        /// <summary>
        /// Sets the expanded/collapsed state, optionally with animation
        /// </summary>
        public void SetState(bool expanded, bool animate = true)
        {
            if (_isExpanded == expanded) return;
            _isExpanded = expanded;
            
            if (_currentAnimation != null)
                StopCoroutine(_currentAnimation);
            
            if (animate && gameObject.activeInHierarchy)
                _currentAnimation = StartCoroutine(AnimateTransition());
            else
                ApplyState();
        }
        
        /// <summary>
        /// Initialize with current state without animation
        /// </summary>
        public void Initialize(bool expanded)
        {
            _isExpanded = expanded;
            ApplyState();
        }
        
        private void ApplyState()
        {
            if (TargetImage == null) return;
            
            // Use custom sprites if available, otherwise keep existing sprite
            if (_isExpanded && ExpandedSprite != null)
                TargetImage.sprite = ExpandedSprite;
            else if (!_isExpanded && CollapsedSprite != null)
                TargetImage.sprite = CollapsedSprite;
        }
        
        private IEnumerator AnimateTransition()
        {
            if (TargetImage == null) yield break;
            
            // Quick scale animation for "pop" effect
            Vector3 originalScale = TargetImage.transform.localScale;
            float halfDuration = AnimationDuration / 2f;
            float elapsed = 0f;
            
            // Scale down
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled for UI
                float t = elapsed / halfDuration;
                float eased = 1f - (1f - t) * (1f - t); // Ease out
                TargetImage.transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.7f, eased);
                yield return null;
            }
            
            // Swap sprite at midpoint
            ApplyState();
            
            // Scale back up with overshoot
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfDuration;
                // Elastic ease out for bounce effect
                float eased = t < 0.5f 
                    ? 2f * t * t 
                    : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                TargetImage.transform.localScale = Vector3.Lerp(originalScale * 0.7f, originalScale, eased);
                yield return null;
            }
            
            TargetImage.transform.localScale = originalScale;
            _currentAnimation = null;
        }
        
        private void OnDisable()
        {
            // Ensure scale is reset if disabled mid-animation
            if (TargetImage != null)
            {
                TargetImage.transform.localScale = Vector3.one;
            }
            _currentAnimation = null;
        }
    }
}
