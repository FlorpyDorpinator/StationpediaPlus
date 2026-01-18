using System.Collections;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StationpediaAscended.UI
{
    /// <summary>
    /// Makes the entire category header clickable to toggle content visibility
    /// and adds hover effects (text dims on hover while icon still animates)
    /// </summary>
    public class CategoryHeaderHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public StationpediaCategory Category;
        public TextMeshProUGUI TitleText;
        public IconAnimator IconAnimator;
        
        // Store original title for restoration
        private string _originalTitleMarkup;
        private bool _isHovering = false;
        
        // Track original title color codes for proper dimming
        private static readonly Color DimMultiplier = new Color(0.7f, 0.7f, 0.7f, 1f);
        
        public void Initialize(StationpediaCategory category, IconAnimator iconAnimator)
        {
            Category = category;
            TitleText = category.Title;
            IconAnimator = iconAnimator;
            
            if (TitleText != null)
            {
                _originalTitleMarkup = TitleText.text;
            }
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Category != null)
            {
                Category.ToggleContentVisibility();
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            
            // Dim the title text
            if (TitleText != null)
            {
                // Store original and apply dimmed version
                _originalTitleMarkup = TitleText.text;
                ApplyDimmedTitle();
            }
            
            // Trigger icon hover animation (scale up slightly)
            if (IconAnimator != null && IconAnimator.TargetImage != null)
            {
                StartCoroutine(AnimateIconHover(true));
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            
            // Restore original title
            if (TitleText != null && !string.IsNullOrEmpty(_originalTitleMarkup))
            {
                TitleText.text = _originalTitleMarkup;
            }
            
            // Return icon to normal
            if (IconAnimator != null && IconAnimator.TargetImage != null)
            {
                StartCoroutine(AnimateIconHover(false));
            }
        }
        
        private void ApplyDimmedTitle()
        {
            if (TitleText == null) return;
            
            // Parse the color from the markup and dim it
            // Original format: <color=#FF7A18>Title</color>
            string text = _originalTitleMarkup;
            
            // Find color code and modify it
            int colorStart = text.IndexOf("<color=#");
            if (colorStart >= 0)
            {
                int hashPos = colorStart + 7;
                int colorEnd = text.IndexOf(">", hashPos);
                if (colorEnd > hashPos)
                {
                    string colorCode = text.Substring(hashPos + 1, colorEnd - hashPos - 1);
                    if (ColorUtility.TryParseHtmlString("#" + colorCode, out Color originalColor))
                    {
                        // Dim the color
                        Color dimmedColor = new Color(
                            originalColor.r * DimMultiplier.r,
                            originalColor.g * DimMultiplier.g,
                            originalColor.b * DimMultiplier.b,
                            originalColor.a
                        );
                        string dimmedHex = ColorUtility.ToHtmlStringRGB(dimmedColor);
                        
                        // Replace in text
                        TitleText.text = text.Substring(0, hashPos + 1) + dimmedHex + text.Substring(colorEnd);
                    }
                }
            }
        }
        
        private IEnumerator AnimateIconHover(bool entering)
        {
            if (IconAnimator == null || IconAnimator.TargetImage == null) yield break;
            
            var targetImage = IconAnimator.TargetImage;
            Vector3 startScale = targetImage.transform.localScale;
            Vector3 targetScale = entering ? Vector3.one * 1.15f : Vector3.one;
            
            float duration = 0.1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // Ease out
                float eased = 1f - (1f - t) * (1f - t);
                targetImage.transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
                yield return null;
            }
            
            targetImage.transform.localScale = targetScale;
        }
        
        private void OnDisable()
        {
            // Reset state when disabled
            if (_isHovering && TitleText != null && !string.IsNullOrEmpty(_originalTitleMarkup))
            {
                TitleText.text = _originalTitleMarkup;
            }
            _isHovering = false;
        }
    }
}
