using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StationpediaAscended.UI
{
    /// <summary>
    /// Handles clicks on Table of Contents links and scrolls to the target section.
    /// Also handles hover effects to brighten links on mouseover.
    /// </summary>
    public class TocLinkHandler : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public TextMeshProUGUI TextComponent;
        
        // Registry of section IDs to their category GameObjects
        private static Dictionary<string, RectTransform> _sectionRegistry = new Dictionary<string, RectTransform>();
        private static Dictionary<string, StationpediaCategory> _categoryRegistry = new Dictionary<string, StationpediaCategory>();
        
        // Parent chain registry - maps a tocId to its parent tocId for nested items
        private static Dictionary<string, string> _parentRegistry = new Dictionary<string, string>();
        
        // Track currently hovered link for highlight effect
        private int _lastHoveredLinkIndex = -1;
        
        // Colors for link states
        private static readonly Color NormalLinkColor = new Color(1f, 1f, 1f, 1f);       // #FFFFFF white
        private static readonly Color HoverLinkColor = new Color(0.6f, 0.6f, 0.6f, 1f);   // Darker grey on hover
        
        /// <summary>
        /// Register a section for TOC navigation
        /// </summary>
        public static void RegisterSection(string tocId, RectTransform rectTransform, StationpediaCategory category = null, string parentTocId = null)
        {
            if (string.IsNullOrEmpty(tocId)) return;
            _sectionRegistry[tocId] = rectTransform;
            if (category != null)
                _categoryRegistry[tocId] = category;
            if (!string.IsNullOrEmpty(parentTocId))
                _parentRegistry[tocId] = parentTocId;
        }
        
        /// <summary>
        /// Clear all registered sections (call when page changes)
        /// </summary>
        public static void ClearRegistry()
        {
            _sectionRegistry.Clear();
            _categoryRegistry.Clear();
            _parentRegistry.Clear();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (TextComponent == null) return;
            
            // Find which link was clicked using event data position
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(TextComponent, eventData.position, null);
            if (linkIndex == -1) return;
            
            TMP_LinkInfo linkInfo = TextComponent.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();
            
            // Check if it's a TOC link (prefixed with "toc_")
            if (linkId.StartsWith("toc_"))
            {
                string sectionId = linkId.Substring(4); // Remove "toc_" prefix
                ScrollToSection(sectionId);
            }
            else if (!string.IsNullOrEmpty(linkId) && linkId != "Clipboard")
            {
                // Handle page navigation links (e.g., {LINK:PageKey;Text} generates <link=PageKey>)
                // Navigate to the Stationpedia page with this key
                try
                {
                    Stationpedia.Instance?.SetPage(linkId, true);
                }
                catch (System.Exception ex)
                {
                    StationpediaAscended.StationpediaAscendedMod.Log?.LogWarning($"Failed to navigate to page '{linkId}': {ex.Message}");
                }
            }
        }
        
        public void OnPointerMove(PointerEventData eventData)
        {
            if (TextComponent == null) return;
            
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(TextComponent, eventData.position, null);
            
            if (linkIndex != _lastHoveredLinkIndex)
            {
                // Clear previous highlight
                if (_lastHoveredLinkIndex >= 0)
                {
                    ClearLinkHighlight();
                }
                
                // Apply new highlight
                if (linkIndex >= 0)
                {
                    ApplyLinkHighlight(linkIndex);
                }
                
                _lastHoveredLinkIndex = linkIndex;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_lastHoveredLinkIndex >= 0)
            {
                ClearLinkHighlight();
                _lastHoveredLinkIndex = -1;
            }
        }
        
        private void ApplyLinkHighlight(int linkIndex)
        {
            if (TextComponent == null || linkIndex < 0) return;
            if (linkIndex >= TextComponent.textInfo.linkCount) return;
            
            try
            {
                var linkInfo = TextComponent.textInfo.linkInfo[linkIndex];
                
                // Get the mesh info for this link and brighten the vertex colors
                for (int i = linkInfo.linkTextfirstCharacterIndex; 
                     i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength; 
                     i++)
                {
                    if (i >= TextComponent.textInfo.characterCount) break;
                    
                    var charInfo = TextComponent.textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;
                    
                    int materialIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;
                    
                    var colors = TextComponent.textInfo.meshInfo[materialIndex].colors32;
                    
                    // Set to brighter hover color
                    Color32 hoverColor32 = HoverLinkColor;
                    colors[vertexIndex + 0] = hoverColor32;
                    colors[vertexIndex + 1] = hoverColor32;
                    colors[vertexIndex + 2] = hoverColor32;
                    colors[vertexIndex + 3] = hoverColor32;
                }
                
                TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
            catch { }
        }
        
        private void ClearLinkHighlight()
        {
            // Force TMP to rebuild with original colors
            if (TextComponent != null)
            {
                TextComponent.ForceMeshUpdate();
            }
        }
        
        /// <summary>
        /// Public static method to scroll to a section from anywhere
        /// </summary>
        public static void ScrollToSectionStatic(string sectionId)
        {
            if (!_sectionRegistry.TryGetValue(sectionId, out var targetRect))
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] TOC target not found: {sectionId}");
                return;
            }
            
            // First, expand all parent categories in the chain
            ExpandParentChainStatic(sectionId);
            
            // Then expand the target category if it's collapsed
            if (_categoryRegistry.TryGetValue(sectionId, out var category))
            {
                if (category.Contents != null && !category.Contents.gameObject.activeSelf)
                {
                    category.ToggleContentVisibility();
                }
            }
            
            // Start scroll coroutine
            if (StationpediaAscendedMod.Instance != null)
            {
                StationpediaAscendedMod.Instance.StartCoroutine(ScrollToTargetCoroutineStatic(targetRect));
            }
        }
        
        private void ScrollToSection(string sectionId)
        {
            ScrollToSectionStatic(sectionId);
        }
        
        /// <summary>
        /// Static version of ExpandParentChain
        /// </summary>
        private static void ExpandParentChainStatic(string tocId)
        {
            // Build the chain from root to target
            var chain = new List<string>();
            string current = tocId;
            
            while (_parentRegistry.TryGetValue(current, out string parentId))
            {
                chain.Insert(0, parentId); // Insert at beginning to build root-first order
                current = parentId;
            }
            
            // Expand from root to target
            foreach (string id in chain)
            {
                if (_categoryRegistry.TryGetValue(id, out var category))
                {
                    if (category.Contents != null && !category.Contents.gameObject.activeSelf)
                    {
                        category.ToggleContentVisibility();
                    }
                }
            }
        }
        
        /// <summary>
        /// Recursively expand all parent categories from root to the target
        /// </summary>
        private void ExpandParentChain(string tocId)
        {
            ExpandParentChainStatic(tocId);
        }
        
        private static IEnumerator ScrollToTargetCoroutineStatic(RectTransform target)
        {
            // Wait for layout to update after potential expand
            yield return null;
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            
            var stationpedia = Stationpedia.Instance;
            if (stationpedia == null) yield break;
            
            var scrollRect = stationpedia.GetComponentInChildren<ScrollRect>();
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null) yield break;
            
            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            yield return null;
            
            // Calculate the target position using a more reliable method
            float contentHeight = scrollRect.content.rect.height;
            float viewportHeight = scrollRect.viewport.rect.height;
            
            if (contentHeight <= viewportHeight)
            {
                // Content fits in viewport, no scrolling needed
                yield break;
            }
            
            // Get the world corners of the target to find its TOP edge
            Vector3[] targetCorners = new Vector3[4];
            target.GetWorldCorners(targetCorners);
            // Corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
            Vector3 targetTopWorld = targetCorners[1]; // Top-left corner
            
            // Convert to content local space
            Vector3 targetTopLocal = scrollRect.content.InverseTransformPoint(targetTopWorld);
            
            // Content's pivot is usually at top, so y=0 is top and negative y is down
            // We want the TOP of the target to be at the TOP of the viewport
            float targetFromTop = -targetTopLocal.y;
            
            // Add a small margin above the title so it's not flush with the top edge
            float topMargin = 10f;
            targetFromTop -= topMargin;
            
            // Clamp to valid range
            targetFromTop = Mathf.Max(0, targetFromTop);
            
            // Calculate scrollable range
            float scrollableHeight = contentHeight - viewportHeight;
            
            // Calculate normalized position (1 = top, 0 = bottom in Unity's scroll)
            float targetNormalizedPos = 1f - (targetFromTop / scrollableHeight);
            targetNormalizedPos = Mathf.Clamp01(targetNormalizedPos);
            
            // Animate scroll
            float startPos = scrollRect.verticalNormalizedPosition;
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // Ease out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, targetNormalizedPos, eased);
                yield return null;
            }
            
            scrollRect.verticalNormalizedPosition = targetNormalizedPos;
        }
    }
}
