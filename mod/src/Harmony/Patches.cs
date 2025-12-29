using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;
using StationpediaAscended.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StationpediaAscended.Patches
{
    /// <summary>
    /// Harmony patch methods for Stationpedia UI modifications.
    /// </summary>
    public static class HarmonyPatches
    {
        // Store drag offset for our custom OnDrag
        private static Vector3 _dragOffset;
        
        // Track if we've already fixed the scrollbar visibility mode
        private static bool _scrollbarVisibilityFixed = false;

        #region PopulateLogicSlotInserts Patch

        /// <summary>
        /// Postfix patch for PopulateLogicSlotInserts - condenses slot number lists.
        /// </summary>
        public static void PopulateLogicSlotInserts_Postfix(UniversalPage __instance)
        {
            try
            {
                var slotContents = __instance.LogicSlotContents;
                if (slotContents == null || slotContents.Contents == null) return;

                foreach (Transform child in slotContents.Contents)
                {
                    var spdalogic = child.GetComponent<SPDALogic>();
                    if (spdalogic == null || spdalogic.InfoReadWrite == null) continue;

                    string originalText = spdalogic.InfoReadWrite.text;
                    if (string.IsNullOrEmpty(originalText)) continue;
                    
                    if (!originalText.Contains(",")) continue;
                    if (!IsSlotNumberList(originalText)) continue;

                    string condensed = CondenseSlotNumbers(originalText);
                    if (condensed != originalText)
                    {
                        spdalogic.InfoReadWrite.text = condensed;
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore errors in patch
            }
        }

        #endregion

        #region Drag Patches

        /// <summary>
        /// Prefix to replace OnDrag entirely - the original calls ClampToScreen which crashes in main menu.
        /// </summary>
        public static bool Stationpedia_OnDrag_Prefix(Stationpedia __instance, PointerEventData eventData)
        {
            try
            {
                // Simple drag implementation that doesn't call ClampToScreen
                Vector3 mousePos = new Vector3(eventData.position.x, eventData.position.y, 0);
                __instance.RectTransform.position = mousePos - _dragOffset;
            }
            catch { }
            return false; // Skip original
        }
        
        /// <summary>
        /// Prefix to capture the drag offset at the start of dragging.
        /// </summary>
        public static bool Stationpedia_OnBeginDrag_Prefix(Stationpedia __instance, PointerEventData eventData)
        {
            try
            {
                Vector3 mousePos = new Vector3(eventData.position.x, eventData.position.y, 0);
                _dragOffset = mousePos - __instance.RectTransform.position;
            }
            catch { }
            return false; // Skip original
        }

        #endregion

        #region ChangeDisplay Patch

        /// <summary>
        /// Postfix patch for ChangeDisplay - handles page descriptions and operational details.
        /// </summary>
        public static void ChangeDisplay_Postfix(UniversalPage __instance, StationpediaPage page)
        {
            // Always try to fix scrollbar visibility (base game bug)
            StartDelayedScrollbarFix();
            
            try
            {
                // Unity-safe null checks - destroyed objects are "fake null"
                if ((object)__instance == null || !__instance) return;
                if ((object)page == null) return;
                
                // Check if the Content transform is valid (not destroyed)
                Transform contentTransform;
                try
                {
                    contentTransform = __instance.Content;
                    if ((object)contentTransform == null || !contentTransform) return;
                    var testGO = contentTransform.gameObject;
                    if ((object)testGO == null || !testGO) return;
                }
                catch
                {
                    return;
                }
                
                string pageKey = page.Key;
                if (string.IsNullOrEmpty(pageKey)) return;

                // Check if we have any custom data for this page
                if (!StationpediaAscendedMod.DeviceDatabase.TryGetValue(pageKey, out var deviceDesc)) return;
                
                // Handle page description modifications
                HandlePageDescriptionModifications(__instance, deviceDesc);
                
                // Handle operational details section
                if (deviceDesc.operationalDetails == null || deviceDesc.operationalDetails.Count == 0) return;

                CreateOperationalDetailsCategory(__instance, contentTransform, deviceDesc);
            }
            catch (Exception)
            {
                // Silently ignore errors in patch
            }
        }

        private static void HandlePageDescriptionModifications(UniversalPage page, DeviceDescriptions deviceDesc)
        {
            var pageDescription = page.PageDescription;
            if ((object)pageDescription == null || !pageDescription) return;

            if (!string.IsNullOrEmpty(deviceDesc.pageDescription))
            {
                // Complete replacement
                pageDescription.text = deviceDesc.pageDescription;
            }
            else
            {
                // Prepend/Append
                string currentText = pageDescription.text ?? "";
                
                if (!string.IsNullOrEmpty(deviceDesc.pageDescriptionPrepend))
                {
                    currentText = deviceDesc.pageDescriptionPrepend + "\n\n" + currentText;
                }
                
                if (!string.IsNullOrEmpty(deviceDesc.pageDescriptionAppend))
                {
                    currentText = currentText + "\n\n" + deviceDesc.pageDescriptionAppend;
                }
                
                if (!string.IsNullOrEmpty(deviceDesc.pageDescriptionPrepend) || 
                    !string.IsNullOrEmpty(deviceDesc.pageDescriptionAppend))
                {
                    pageDescription.text = currentText;
                }
            }
        }

        private static void CreateOperationalDetailsCategory(UniversalPage page, Transform contentTransform, DeviceDescriptions deviceDesc)
        {
            // Check for existing by GameObject name - destroy and recreate
            var existingCategory = contentTransform.Find("OperationalDetailsCategory");
            if (existingCategory != null)
            {
                UnityEngine.Object.DestroyImmediate(existingCategory.gameObject);
            }

            var stationpedia = Stationpedia.Instance;
            if ((object)stationpedia == null || !stationpedia) return;
            
            var categoryPrefab = stationpedia.CategoryPrefab;
            if ((object)categoryPrefab == null || !categoryPrefab) return;
            
            // Get font/material from PageDescription (a working TMP element)
            var sourceText = page.PageDescription;
            if ((object)sourceText == null || !sourceText) return;

            // Create the collapsible category container
            StationpediaCategory category = UnityEngine.Object.Instantiate<StationpediaCategory>(
                categoryPrefab, contentTransform);
            if ((object)category == null || !category) return;
            
            // Name it so we can find it later
            category.gameObject.name = "OperationalDetailsCategory";
            
            if ((object)category.Title == null || !category.Title) return;
            if ((object)category.Contents == null || !category.Contents) return;
            
            // Set the title - apply custom color if configured
            string titleColor = !string.IsNullOrEmpty(deviceDesc.operationalDetailsTitleColor) 
                ? deviceDesc.operationalDetailsTitleColor 
                : "#FF7A18"; // Default orange
            category.Title.text = $"<color={titleColor}>Operational Details</color>";
            
            // Try to add our custom phoenix icon next to the category title
            AddCategoryIcon(category);

            // Build combined text for all operational details
            var sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (var detail in deviceDesc.operationalDetails)
            {
                if (!first) sb.AppendLine().AppendLine();
                sb.AppendLine($"<color=#FF7A18>{detail.title}</color>");
                sb.Append(detail.description);
                first = false;
            }

            // Create a text GameObject inside the category contents
            CreateOperationalDetailsText(category, sourceText, sb.ToString());
            
            // Configure category layout
            ConfigureCategoryLayout(category, page);
            
            // Move and set initial state
            category.transform.SetSiblingIndex(20);
            
            // Default to COLLAPSED state
            category.Contents.gameObject.SetActive(false);
            if (category.CollapseImage != null && category.NotVisibleImage != null)
            {
                category.CollapseImage.sprite = category.NotVisibleImage;
            }
            
            // Force rebuild of the main content area to fix scrollbar visibility
            if (Stationpedia.Instance?.ContentRectTransform != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(Stationpedia.Instance.ContentRectTransform);
            }
            
            // Add to game's category list so it handles cleanup
            page.CreatedCategories.Add(category);
        }

        private static void AddCategoryIcon(StationpediaCategory category)
        {
            try
            {
                if (StationpediaAscendedMod._customIconSprite != null)
                {
                    // Find the title's parent (usually the header bar)
                    var titleParent = category.Title.transform.parent;
                    if (titleParent != null)
                    {
                        // Create a new Image for the icon
                        var iconGO = new GameObject("OperationalDetailsIcon");
                        iconGO.transform.SetParent(titleParent, false);
                        iconGO.transform.SetAsFirstSibling(); // Put it before the title
                        
                        var iconImage = iconGO.AddComponent<UnityEngine.UI.Image>();
                        iconImage.sprite = StationpediaAscendedMod._customIconSprite;
                        iconImage.preserveAspect = true;
                        
                        // Set up the RectTransform for proper sizing
                        var iconRT = iconGO.GetComponent<RectTransform>();
                        iconRT.sizeDelta = new Vector2(20, 20);
                        
                        // Add a LayoutElement to work with any layout groups
                        var iconLayout = iconGO.AddComponent<UnityEngine.UI.LayoutElement>();
                        iconLayout.preferredWidth = 20;
                        iconLayout.preferredHeight = 20;
                        iconLayout.minWidth = 20;
                        iconLayout.minHeight = 20;
                        
                        ConsoleWindow.Print("[Stationpedia Ascended] Added phoenix icon to Operational Details category");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Could not add icon to category: {ex.Message}");
            }
        }

        private static void CreateOperationalDetailsText(StationpediaCategory category, TMPro.TextMeshProUGUI sourceText, string textContent)
        {
            var textGO = new GameObject("OperationalDetailsText");
            textGO.transform.SetParent(category.Contents, false);
            
            // Add TextMeshProUGUI and copy settings from PageDescription
            var textComponent = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            textComponent.font = sourceText.font;
            textComponent.fontSharedMaterial = sourceText.fontSharedMaterial;
            textComponent.fontSize = sourceText.fontSize;
            textComponent.color = sourceText.color;
            textComponent.enableWordWrapping = true;
            textComponent.overflowMode = TMPro.TextOverflowModes.Overflow;
            textComponent.richText = true;
            textComponent.lineSpacing = sourceText.lineSpacing;
            textComponent.margin = new Vector4(10, 10, 10, 10);
            textComponent.text = textContent;
            
            // Set up RectTransform - stretch horizontally, auto-size vertically
            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(-20, 0); // Width with margins, height will be auto
            
            // Let ContentSizeFitter handle the height automatically
            var fitter = textGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureCategoryLayout(StationpediaCategory category, UniversalPage page)
        {
            // The Contents has a GridLayoutGroup by default which forces fixed cell sizes
            // We need to DESTROY it (not just disable) so we can add a VerticalLayoutGroup
            var existingGridLayout = category.Contents.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (existingGridLayout != null)
            {
                UnityEngine.Object.DestroyImmediate(existingGridLayout);
            }
            
            // Copy the background Image settings from LogicSlotContents (which has proper styling)
            var bgImage = category.Contents.gameObject.GetComponent<UnityEngine.UI.Image>();
            var sourceImage = page.LogicSlotContents?.Contents?.GetComponent<UnityEngine.UI.Image>();
            if (sourceImage != null)
            {
                if (bgImage == null)
                {
                    bgImage = category.Contents.gameObject.AddComponent<UnityEngine.UI.Image>();
                }
                bgImage.sprite = sourceImage.sprite;
                bgImage.color = sourceImage.color;
                bgImage.type = sourceImage.type;
                bgImage.material = sourceImage.material;
            }
            
            // Now add our VerticalLayoutGroup
            var contentsLayout = category.Contents.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (contentsLayout == null)
            {
                contentsLayout = category.Contents.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                contentsLayout.childForceExpandWidth = true;
                contentsLayout.childForceExpandHeight = false;
                contentsLayout.childControlWidth = true;
                contentsLayout.childControlHeight = true;
                contentsLayout.spacing = 5;
                contentsLayout.padding = new RectOffset(10, 10, 10, 10);
            }
            
            // Ensure category.Contents has a ContentSizeFitter to expand based on children
            var contentsFitter = category.Contents.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (contentsFitter == null)
            {
                contentsFitter = category.Contents.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                contentsFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                contentsFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            }
            
            // Make sure contents are visible
            category.Contents.gameObject.SetActive(true);
            
            // Force layout rebuild to calculate correct sizes
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(category.Contents);
        }

        #endregion

        #region Scrollbar Fix

        private static void StartDelayedScrollbarFix()
        {
            try
            {
                // Use the mod instance to start coroutine
                if (StationpediaAscendedMod.Instance != null)
                {
                    StationpediaAscendedMod.Instance.StartCoroutine(DelayedScrollbarFix());
                }
            }
            catch { }
        }

        private static IEnumerator DelayedScrollbarFix()
        {
            // The game's FixTheScrollValue async method runs after layout rebuild
            // We need to run our fix AFTER that completes
            yield return new WaitForEndOfFrame();
            yield return null;
            yield return null;
            yield return null;
            
            var stationpedia = Stationpedia.Instance;
            if (stationpedia == null) yield break;
            
            var scrollRect = stationpedia.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            if (scrollRect == null) yield break;
            
            // Change visibility mode from AutoHide to Permanent (only need to do this once)
            if (!_scrollbarVisibilityFixed)
            {
                scrollRect.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
                _scrollbarVisibilityFixed = true;
                ConsoleWindow.Print("[SLP] Set scrollbar visibility to Permanent");
            }
            
            var scrollbar = scrollRect.verticalScrollbar;
            if (scrollbar == null || scrollbar.handleRect == null) yield break;
            
            // Apply fix multiple times over several frames to combat re-corruption
            for (int i = 0; i < 5; i++)
            {
                FixHandleLocalPosition(scrollbar.handleRect, i == 0);
                yield return null;
            }
        }
        
        private static void FixHandleLocalPosition(RectTransform handleRect, bool logFirst)
        {
            if (handleRect == null) return;
            
            try
            {
                Vector3 pos = handleRect.localPosition;
                bool needsFix = float.IsNaN(pos.y) || float.IsNaN(pos.x) || 
                               Mathf.Abs(pos.x) > 0.01f || Mathf.Abs(pos.y) > 0.01f || Mathf.Abs(pos.z) > 0.01f;
                
                if (needsFix)
                {
                    if (logFirst)
                    {
                        ConsoleWindow.Print($"[SLP] Fixing handle localPosition: {pos}");
                    }
                    
                    // Set position directly via transform
                    handleRect.localPosition = Vector3.zero;
                    
                    // Also ensure anchoredPosition is zero (for anchor-based positioning)
                    handleRect.anchoredPosition = Vector2.zero;
                }
            }
            catch { }
        }

        /// <summary>
        /// Resets the scrollbar visibility fixed flag (for hot-reload).
        /// </summary>
        public static void ResetScrollbarState()
        {
            _scrollbarVisibilityFixed = false;
        }

        #endregion

        #region Helper Methods

        private static bool IsSlotNumberList(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != ',' && c != ' ')
                    return false;
            }
            return true;
        }

        private static string CondenseSlotNumbers(string input)
        {
            var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var numbers = new List<int>();

            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int num))
                {
                    numbers.Add(num);
                }
            }

            if (numbers.Count <= 5)
            {
                return input;
            }

            numbers.Sort();

            var ranges = new List<string>();
            int rangeStart = numbers[0];
            int rangeEnd = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == rangeEnd + 1)
                {
                    rangeEnd = numbers[i];
                }
                else
                {
                    ranges.Add(FormatRange(rangeStart, rangeEnd));
                    rangeStart = numbers[i];
                    rangeEnd = numbers[i];
                }
            }

            ranges.Add(FormatRange(rangeStart, rangeEnd));
            return string.Join(", ", ranges);
        }

        private static string FormatRange(int start, int end)
        {
            if (start == end)
                return start.ToString();
            else if (end == start + 1)
                return $"{start}, {end}";
            else
                return $"{start}-{end}";
        }

        #endregion
    }
}
