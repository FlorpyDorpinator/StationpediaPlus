using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;
using StationpediaAscended.Data;
using StationpediaAscended.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;

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
        
        // Default Stationeers blue color for backgrounds (inner color) - more pronounced blue
        private static readonly Color StationeersBlue = new Color(0.06f, 0.12f, 0.22f, 0.92f);
        // Lighter border color for backgrounds (outer edge)
        private static readonly Color StationeersBlueBorder = new Color(0.15f, 0.30f, 0.45f, 0.7f);
        
        // Cache the game's native panel sprite (has built-in borders)
        private static Sprite _nativePanelSprite = null;
        private static Material _nativePanelMaterial = null;
        private static UnityEngine.UI.Image.Type _nativePanelType = UnityEngine.UI.Image.Type.Sliced;

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

                // IMPORTANT: Clean up any leftover guide content from previous page navigation
                // This prevents prepend/append text from persisting across page changes
                var existingGuideContent = contentTransform.Find("GuideSectionsContent");
                if (existingGuideContent != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingGuideContent.gameObject);
                }
                
                // Also clean up SurvivalManualContent if navigating away from that page
                var existingSurvivalManual = contentTransform.Find("SurvivalManualContent");
                if (existingSurvivalManual != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingSurvivalManual.gameObject);
                }
                
                // Also clean up OperationalDetailsCategory if navigating to a page without it
                var existingOpDetails = contentTransform.Find("OperationalDetailsCategory");
                if (existingOpDetails != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingOpDetails.gameObject);
                }

                // Fix long prefab names overlapping with Stack Size (only when needed)
                TruncateLongPrefabName(__instance);
                
                // Add tooltips to Prefab Name and Prefab Hash for IC10 users
                AddPrefabTooltips(__instance);

                // Check for Survival Manual page - use special rendering (no Operational Details wrapper)
                if (pageKey == "SurvivalManual")
                {
                    RenderSurvivalManualPage(__instance, contentTransform);
                    return; // Don't continue to normal device processing
                }
                
                // Check for JSON-defined guide pages
                if (Data.JsonGuideLoader.HasGuide(pageKey))
                {
                    RenderJsonGuidePage(__instance, contentTransform, pageKey);
                    return; // Don't continue to normal device processing
                }
                
                // Check for JSON-defined game mechanics pages
                if (Data.JsonMechanicsLoader.HasMechanic(pageKey))
                {
                    RenderJsonMechanicPage(__instance, contentTransform, pageKey);
                    return; // Don't continue to normal device processing
                }
                
                // Check for Daylight Sensor Guide page - use special rendering with operational details
                if (pageKey == "DaylightSensorGuide")
                {
                    RenderDaylightSensorGuidePage(__instance, contentTransform);
                    return; // Don't continue to normal device processing
                }
                
                // Check if we have any custom data for this page in DeviceDatabase
                DeviceDescriptions deviceDesc = null;
                StationpediaAscendedMod.DeviceDatabase.TryGetValue(pageKey, out deviceDesc);
                
                if (deviceDesc == null) return;

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

        /// <summary>
        /// Truncates long prefab names to prevent overlap with Stack Size.
        /// Uses TextMeshPro's built-in Ellipsis mode with LayoutElement constraints.
        /// </summary>
        private static void TruncateLongPrefabName(UniversalPage page)
        {
            try
            {
                if (page.PrefabNameText == null || string.IsNullOrEmpty(page.PrefabNameText.text))
                    return;
                
                // Configure TextMeshPro to handle overflow dynamically
                page.PrefabNameText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                page.PrefabNameText.enableWordWrapping = false; // Keep on single line
                
                // Add a LayoutElement to constrain the maximum width
                var layoutElement = page.PrefabNameText.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = page.PrefabNameText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                }
                
                // Set a maximum preferred width
                // This allows the text field to shrink if needed but not grow beyond this width
                layoutElement.preferredWidth = 300f;
                layoutElement.flexibleWidth = 0f; // Don't allow it to expand beyond preferred width
                
                // Also constrain the RectTransform's sizeDelta
                var rectTransform = page.PrefabNameText.rectTransform;
                if (rectTransform != null)
                {
                    // Get current size
                    Vector2 currentSize = rectTransform.sizeDelta;
                    
                    // Constrain width if it's too large (but keep original if smaller)
                    if (currentSize.x > 300f || currentSize.x == 0)
                    {
                        rectTransform.sizeDelta = new Vector2(300f, currentSize.y);
                    }
                }
            }
            catch
            {
                // Silently ignore errors
            }
        }

        /// <summary>
        /// Adds tooltip components to Prefab Name and Prefab Hash text elements.
        /// Tooltips show full value, click-to-copy instruction, and IC10 usage info.
        /// </summary>
        private static void AddPrefabTooltips(UniversalPage page)
        {
            try
            {
                string pageKey = Stationpedia.CurrentPageKey;
                
                // Add tooltip to Prefab Name
                if (page.PrefabNameText != null && !string.IsNullOrEmpty(page.PrefabNameText.text))
                {
                    var prefabNameParent = page.PrefabNameText.transform.parent;
                    if (prefabNameParent != null)
                    {
                        // Get or add tooltip component to the parent (the row containing label + value)
                        var tooltip = prefabNameParent.gameObject.GetComponent<Tooltips.SPDAPrefabInfoTooltip>();
                        if (tooltip == null)
                        {
                            tooltip = prefabNameParent.gameObject.AddComponent<Tooltips.SPDAPrefabInfoTooltip>();
                        }
                        
                        // Extract clean prefab name (remove link formatting)
                        string cleanName = ExtractPrefabValue(page.PrefabNameText.text);
                        tooltip.Initialize(pageKey ?? "", cleanName, false);
                    }
                }
                
                // Add tooltip to Prefab Hash
                if (page.PrefabHashText != null && !string.IsNullOrEmpty(page.PrefabHashText.text))
                {
                    var prefabHashParent = page.PrefabHashText.transform.parent;
                    if (prefabHashParent != null)
                    {
                        // Get or add tooltip component to the parent (the row containing label + value)
                        var tooltip = prefabHashParent.gameObject.GetComponent<Tooltips.SPDAPrefabInfoTooltip>();
                        if (tooltip == null)
                        {
                            tooltip = prefabHashParent.gameObject.AddComponent<Tooltips.SPDAPrefabInfoTooltip>();
                        }
                        
                        // Extract clean prefab hash (remove link formatting)
                        string cleanHash = ExtractPrefabValue(page.PrefabHashText.text);
                        tooltip.Initialize(pageKey ?? "", cleanHash, true);
                    }
                }
            }
            catch
            {
                // Silently ignore errors
            }
        }

        /// <summary>
        /// Extracts the actual value from TMP link-formatted text.
        /// Input like: <link=Clipboard><color=#008AE6>StructurePressureFedLiquidRocketEngine</color></link>
        /// Output: StructurePressureFedLiquidRocketEngine
        /// </summary>
        private static string ExtractPrefabValue(string formattedText)
        {
            if (string.IsNullOrEmpty(formattedText))
                return formattedText;
            
            // Remove all tags
            string result = System.Text.RegularExpressions.Regex.Replace(formattedText, "<[^>]+>", "");
            return result.Trim();
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

        /// <summary>
        /// Render the Survival Manual page - special handling with per-Part TOCs
        /// Unlike other guides, the Survival Manual has nested TOCs within each Part section
        /// </summary>
        private static void RenderSurvivalManualPage(UniversalPage page, Transform contentTransform)
        {
            // Clear TOC registry for new page
            TocLinkHandler.ClearRegistry();
            
            // Check for existing - destroy and recreate
            var existingManual = contentTransform.Find("SurvivalManualContent");
            if (existingManual != null)
            {
                UnityEngine.Object.DestroyImmediate(existingManual.gameObject);
            }

            var stationpedia = Stationpedia.Instance;
            if (stationpedia == null) return;
            
            var categoryPrefab = stationpedia.CategoryPrefab;
            if (categoryPrefab == null) return;
            
            var sourceText = page.PageDescription;
            if (sourceText == null) return;
            
            // Load guide data from JSON (not markdown)
            var guide = Data.JsonGuideLoader.GetGuide("SurvivalManual");
            if (guide == null)
            {
                ConsoleWindow.Print("[Stationpedia Ascended] SurvivalManual guide not found in descriptions.json");
                return;
            }
            
            var guideData = Data.JsonGuideLoader.ToDeviceDescriptions(guide);
            if (guideData?.operationalDetails == null || guideData.operationalDetails.Count == 0)
            {
                ConsoleWindow.Print("[Stationpedia Ascended] SurvivalManual has no sections");
                return;
            }
            
            // Cache native panel sprite if not already cached
            if (_nativePanelSprite == null)
            {
                var sourceImage = page.LogicSlotContents?.Contents?.GetComponent<UnityEngine.UI.Image>();
                if (sourceImage != null)
                {
                    _nativePanelSprite = sourceImage.sprite;
                    _nativePanelMaterial = sourceImage.material;
                    _nativePanelType = sourceImage.type;
                }
            }

            // Create a container for all manual content
            var containerGO = new GameObject("SurvivalManualContent");
            containerGO.transform.SetParent(contentTransform, false);
            containerGO.transform.SetSiblingIndex(20);
            
            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0, 1);
            containerRT.anchorMax = new Vector2(1, 1);
            containerRT.pivot = new Vector2(0.5f, 1);
            
            var containerLayout = containerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            containerLayout.spacing = 10;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            
            var containerFitter = containerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            containerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            containerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            // Render page image at the top if specified
            if (!string.IsNullOrEmpty(guideData.pageImage))
            {
                CreateGuideTopImage(containerRT, guideData.pageImage);
            }
            
            // Render page description
            string pageDescText = guideData.pageDescription ?? "";
            if (!string.IsNullOrEmpty(guideData.pageDescriptionPrepend))
            {
                pageDescText = guideData.pageDescriptionPrepend + "\n\n" + pageDescText;
            }
            if (!string.IsNullOrEmpty(guideData.pageDescriptionAppend))
            {
                pageDescText = pageDescText + "\n\n" + guideData.pageDescriptionAppend;
            }
            if (!string.IsNullOrEmpty(pageDescText.Trim()))
            {
                CreateTextElement(containerRT, sourceText, pageDescText.Trim());
            }

            // Render each Part (Part 1, Part 2, Part 3) with its OWN nested TOC
            foreach (var partDetail in guideData.operationalDetails)
            {
                CreateSurvivalManualPart(containerRT, partDetail, categoryPrefab, sourceText, page);
            }
            
            // Force layout rebuild
            if (Stationpedia.Instance?.ContentRectTransform != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(Stationpedia.Instance.ContentRectTransform);
            }
            
            ConsoleWindow.Print("[Stationpedia Ascended] Rendered Survival Manual from JSON with per-Part TOCs");
        }

        /// <summary>
        /// <summary>
        /// Render a JSON-defined guide page - sections appear directly without wrapper
        /// </summary>
        private static void RenderJsonGuidePage(UniversalPage page, Transform contentTransform, string guideKey)
        {
            try
            {
                var stationpedia = Stationpedia.Instance;
                if (stationpedia == null) return;

                var categoryPrefab = stationpedia.CategoryPrefab;
                if (categoryPrefab == null) return;

                // Get the guide data
                var guide = Data.JsonGuideLoader.GetGuide(guideKey);
                if (guide == null)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Guide not found: {guideKey}");
                    return;
                }

                // Convert to DeviceDescriptions for rendering
                var guideData = Data.JsonGuideLoader.ToDeviceDescriptions(guide);
                if (guideData?.operationalDetails == null || guideData.operationalDetails.Count == 0)
                {
                    // If no sections, just show the page description
                    ConsoleWindow.Print($"[Stationpedia Ascended] Guide {guideKey} has no sections");
                    return;
                }

                // Render sections directly without "Operational Details" wrapper
                RenderGuideSections(page, contentTransform, guideData, guide.generateToc, guide.tocTitle);
                
                ConsoleWindow.Print($"[Stationpedia Ascended] Rendered JSON guide: {guideKey}");
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error rendering JSON guide {guideKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Render a JSON-defined game mechanic page - sections appear directly without wrapper
        /// </summary>
        private static void RenderJsonMechanicPage(UniversalPage page, Transform contentTransform, string mechanicKey)
        {
            try
            {
                var stationpedia = Stationpedia.Instance;
                if (stationpedia == null) return;

                var categoryPrefab = stationpedia.CategoryPrefab;
                if (categoryPrefab == null) return;

                // Get the mechanic data
                var mechanic = Data.JsonMechanicsLoader.GetMechanic(mechanicKey);
                if (mechanic == null)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Mechanic not found: {mechanicKey}");
                    return;
                }

                // Convert to DeviceDescriptions for rendering
                var mechanicData = Data.JsonMechanicsLoader.ToDeviceDescriptions(mechanic);
                if (mechanicData?.operationalDetails == null || mechanicData.operationalDetails.Count == 0)
                {
                    // If no sections, just show the page description
                    ConsoleWindow.Print($"[Stationpedia Ascended] Mechanic {mechanicKey} has no sections");
                    return;
                }

                // Render sections directly without "Operational Details" wrapper
                RenderGuideSections(page, contentTransform, mechanicData, mechanic.generateToc, mechanic.tocTitle);
                
                ConsoleWindow.Print($"[Stationpedia Ascended] Rendered JSON mechanic: {mechanicKey}");
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error rendering JSON mechanic {mechanicKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Render the Daylight Sensor Guide page - sections appear directly without wrapper
        /// </summary>
        private static void RenderDaylightSensorGuidePage(UniversalPage page, Transform contentTransform)
        {
            try
            {
                var stationpedia = Stationpedia.Instance;
                if (stationpedia == null) return;

                var categoryPrefab = stationpedia.CategoryPrefab;
                if (categoryPrefab == null) return;

                var sourceText = page.PageDescription;
                if (sourceText == null) return;

                // Get the guide data
                var guideData = Data.DaylightSensorGuideLoader.GetDaylightSensorGuideDescriptions();
                if (guideData?.operationalDetails == null || guideData.operationalDetails.Count == 0)
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] No Daylight Sensor Guide data found");
                    return;
                }

                // Render sections directly without wrapper
                RenderGuideSections(page, contentTransform, guideData, guideData.generateToc, guideData.tocTitle);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error rendering Daylight Sensor Guide: {ex.Message}");
            }
        }

        /// <summary>
        /// Render guide/mechanic sections directly - unified rendering for all guide types
        /// Uses the same rendering infrastructure as Operational Details but without the outer wrapper
        /// </summary>
        private static void RenderGuideSections(UniversalPage page, Transform contentTransform, DeviceDescriptions guideData, bool generateToc = false, string tocTitle = null)
        {
            try
            {
                // Clear TOC registry for new page
                TocLinkHandler.ClearRegistry();
                
                var stationpedia = Stationpedia.Instance;
                if (stationpedia == null) return;

                var categoryPrefab = stationpedia.CategoryPrefab;
                if (categoryPrefab == null) return;

                var sourceText = page.PageDescription;
                if (sourceText == null) return;

                // Cache native panel sprite if not already cached (for vanilla mode backgrounds)
                // This is critical - we need this sprite for proper backgrounds in vanilla mode
                if (_nativePanelSprite == null)
                {
                    var sourceImage = page.LogicSlotContents?.Contents?.GetComponent<UnityEngine.UI.Image>();
                    if (sourceImage != null)
                    {
                        _nativePanelSprite = sourceImage.sprite;
                        _nativePanelMaterial = sourceImage.material;
                        _nativePanelType = sourceImage.type;
                    }
                }

                // Clear any existing content
                var existingContent = contentTransform.Find("GuideSectionsContent");
                if (existingContent != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingContent.gameObject);
                }

                // Create container (same as Operational Details uses)
                var containerGO = new GameObject("GuideSectionsContent");
                containerGO.transform.SetParent(contentTransform, false);
                containerGO.transform.SetSiblingIndex(20);

                var containerRT = containerGO.AddComponent<RectTransform>();
                containerRT.anchorMin = new Vector2(0, 1);
                containerRT.anchorMax = new Vector2(1, 1);
                containerRT.pivot = new Vector2(0.5f, 1);

                var containerLayout = containerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                containerLayout.spacing = 10;
                containerLayout.childForceExpandWidth = true;
                containerLayout.childForceExpandHeight = false;
                containerLayout.childControlWidth = true;
                containerLayout.childControlHeight = true;

                var containerFitter = containerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                containerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                containerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

                // Render page image at the top (like vanilla guides do)
                if (!string.IsNullOrEmpty(guideData.pageImage))
                {
                    CreateGuideTopImage(containerRT, guideData.pageImage);
                }

                // Render page description (with prepend/append support)
                string pageDescText = guideData.pageDescription ?? "";
                if (!string.IsNullOrEmpty(guideData.pageDescriptionPrepend))
                {
                    pageDescText = guideData.pageDescriptionPrepend + "\n\n" + pageDescText;
                }
                if (!string.IsNullOrEmpty(guideData.pageDescriptionAppend))
                {
                    pageDescText = pageDescText + "\n\n" + guideData.pageDescriptionAppend;
                }
                if (!string.IsNullOrEmpty(pageDescText.Trim()))
                {
                    CreateTextElement(containerRT, sourceText, pageDescText.Trim());
                }

                // Generate Table of Contents if enabled (using unified TOC)
                if (generateToc && guideData.operationalDetails != null && guideData.operationalDetails.Count > 0)
                {
                    var tocEntries = new List<(string tocId, string title, int depth)>();
                    foreach (var detail in guideData.operationalDetails)
                    {
                        CollectTocEntries(tocEntries, detail, 0, guideData.tocFlat);
                    }
                    string effectiveTocTitle = string.IsNullOrEmpty(guideData.tocTitle) ? "Contents" : guideData.tocTitle;
                    // For guides: TOC goes AFTER image and description, not at the top
                    CreateUnifiedTableOfContents(containerRT, sourceText, tocEntries, effectiveTocTitle, centerColumns: true, placeAtTop: false);
                }

                // Render each top-level section using the SAME infrastructure as Operational Details
                // This gives us proper nested backgrounds, collapsibles, etc.
                foreach (var sectionDetail in guideData.operationalDetails)
                {
                    // Render at depth 0 since these are top-level sections (no outer wrapper)
                    RenderGuideSectionElement(containerRT, sourceText, sectionDetail, categoryPrefab, page, 0, null);
                }

                // Force layout rebuild
                if (stationpedia.ContentRectTransform != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(stationpedia.ContentRectTransform);
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error rendering guide sections: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a large top image for guide pages (like vanilla guides)
        /// </summary>
        private static void CreateGuideTopImage(RectTransform parent, string imageFile)
        {
            try
            {
                var sprite = StationpediaAscendedMod.LoadImageFromModFolder(imageFile);
                if (sprite == null)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Guide image not found: {imageFile}");
                    return;
                }
                
                var imageGO = new GameObject("GuideTopImage");
                imageGO.transform.SetParent(parent, false);
                
                var image = imageGO.AddComponent<UnityEngine.UI.Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                
                var rectTransform = imageGO.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1);
                
                // Match vanilla Stationpedia image sizing (smaller, like device thumbnails)
                float maxWidth = 100f;  // Smaller to match vanilla style
                float aspectRatio = (float)sprite.texture.width / sprite.texture.height;
                float height = maxWidth / aspectRatio;
                
                // Use LayoutElement to control size
                var layoutElement = imageGO.AddComponent<UnityEngine.UI.LayoutElement>();
                layoutElement.preferredWidth = maxWidth;
                layoutElement.preferredHeight = height;
                layoutElement.flexibleWidth = 0;  // No stretching
                layoutElement.flexibleHeight = 0;
                
                rectTransform.sizeDelta = new Vector2(maxWidth, height);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating guide top image: {ex.Message}");
            }
        }

        /// <summary>
        /// Render a single guide section element - uses same logic as RenderOperationalDetailElement
        /// </summary>
        private static void RenderGuideSectionElement(RectTransform parent, TMPro.TextMeshProUGUI sourceText,
            OperationalDetail detail, StationpediaCategory categoryPrefab, UniversalPage page, int depth, string parentTocId)
        {
            // Recursive callback for children
            Action<RectTransform, OperationalDetail, int, string> recurse = (p, d, dep, pid) => 
                RenderGuideSectionElement(p, sourceText, d, categoryPrefab, page, dep, pid);
            
            // Only make collapsible if explicitly set to true
            bool shouldBeCollapsible = detail.collapsible;
            
            if (shouldBeCollapsible && !string.IsNullOrEmpty(detail.title))
            {
                // Use unified collapsible section
                CreateUnifiedCollapsibleSection(parent, sourceText, detail, categoryPrefab, page, depth, parentTocId, recurse);
            }
            else
            {
                // Use unified inline content
                CreateUnifiedInlineContent(parent, sourceText, detail, categoryPrefab, page, depth, parentTocId, recurse);
            }
        }

        /// <summary>
        /// DEPRECATED - Use CreateUnifiedCollapsibleSection instead
        /// Create a collapsible guide section - mirrors CreateNestedCollapsibleCategory
        /// </summary>
        private static void CreateGuideCollapsibleSection(RectTransform parent, TMPro.TextMeshProUGUI sourceText,
            OperationalDetail detail, StationpediaCategory categoryPrefab, UniversalPage page, int depth, string parentTocId)
        {
            try
            {
                // Create nested category
                var nestedCategory = UnityEngine.Object.Instantiate<StationpediaCategory>(categoryPrefab, parent);
                if (nestedCategory == null) return;
                
                string safeTocId = detail.tocId ?? detail.title?.Replace(" ", "_") ?? $"section_{depth}";
                nestedCategory.gameObject.name = $"GuideSection_{safeTocId}";
                
                // Set title with depth-based coloring (uses VanillaModeManager for proper colors)
                string titleColor = VanillaModeManager.GetTitleColor(depth);
                nestedCategory.Title.text = $"<color={titleColor}>{detail.title}</color>";
                
                // Apply custom icons (respects vanilla mode)
                ApplyCustomCategoryIcons(nestedCategory);
                
                // Configure layout with proper backgrounds (respects vanilla mode)
                ConfigureNestedCategoryLayout(nestedCategory, detail, depth);
                
                // Register for TOC navigation
                if (!string.IsNullOrEmpty(detail.tocId))
                {
                    TocLinkHandler.RegisterSection(detail.tocId, nestedCategory.GetComponent<RectTransform>(), nestedCategory, parentTocId);
                }
                
                // Add image if specified
                if (!string.IsNullOrEmpty(detail.imageFile))
                {
                    CreateInlineImage(nestedCategory.Contents, detail.imageFile);
                }
                
                // Add description text
                if (!string.IsNullOrEmpty(detail.description))
                {
                    CreateTextElement(nestedCategory.Contents, sourceText, detail.description);
                }
                
                // Add bullet items
                if (detail.items != null && detail.items.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var item in detail.items)
                    {
                        sb.AppendLine($"  • {item}");
                    }
                    CreateTextElement(nestedCategory.Contents, sourceText, sb.ToString().TrimEnd());
                }
                
                // Add numbered steps
                if (detail.steps != null && detail.steps.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    int stepNum = 1;
                    string stepColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
                    foreach (var step in detail.steps)
                    {
                        sb.AppendLine($"  <color={stepColor}>{stepNum}.</color> {step}");
                        stepNum++;
                    }
                    CreateTextElement(nestedCategory.Contents, sourceText, sb.ToString().TrimEnd());
                }
                
                // Add YouTube link if specified
                if (!string.IsNullOrEmpty(detail.youtubeUrl))
                {
                    CreateYouTubeLink(nestedCategory.Contents, sourceText, detail.youtubeUrl, detail.youtubeLabel);
                }
                
                // Add inline video if specified
                if (!string.IsNullOrEmpty(detail.videoFile))
                {
                    CreateInlineVideo(nestedCategory.Contents, detail.videoFile);
                }
                
                // Recursively render children
                if (detail.children != null)
                {
                    foreach (var child in detail.children)
                    {
                        RenderGuideSectionElement(nestedCategory.Contents, sourceText, child, categoryPrefab, page, depth + 1, detail.tocId);
                    }
                }
                
                // Default to collapsed
                nestedCategory.Contents.gameObject.SetActive(false);
                if (nestedCategory.CollapseImage != null && nestedCategory.NotVisibleImage != null)
                {
                    nestedCategory.CollapseImage.sprite = nestedCategory.NotVisibleImage;
                }
                
                // Update animator state
                var animator = nestedCategory.GetComponent<IconAnimator>();
                if (animator != null)
                {
                    animator.Initialize(false);
                }
                
                page.CreatedCategories.Add(nestedCategory);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating guide section: {ex.Message}");
            }
        }

        /// <summary>
        /// Create inline content for non-collapsible guide sections
        /// </summary>
        private static void CreateGuideInlineContent(RectTransform parent, TMPro.TextMeshProUGUI sourceText,
            OperationalDetail detail, StationpediaCategory categoryPrefab, UniversalPage page, int depth, string parentTocId)
        {
            // Create container for inline content
            var containerGO = new GameObject($"InlineContent_{detail.title ?? "text"}");
            containerGO.transform.SetParent(parent, false);
            
            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0, 1);
            containerRT.anchorMax = new Vector2(1, 1);
            containerRT.pivot = new Vector2(0.5f, 1);
            
            var containerLayout = containerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            containerLayout.spacing = 5;
            
            var containerFitter = containerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            containerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            containerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            // Add title if present (as styled header text)
            if (!string.IsNullOrEmpty(detail.title))
            {
                string titleColor = VanillaModeManager.GetTitleColor(depth);
                CreateTextElement(containerRT, sourceText, $"<b><color={titleColor}>{detail.title}</color></b>");
            }
            
            // Add description
            if (!string.IsNullOrEmpty(detail.description))
            {
                CreateTextElement(containerRT, sourceText, detail.description);
            }
            
            // Add bullet items
            if (detail.items != null && detail.items.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var item in detail.items)
                {
                    sb.AppendLine($"  • {item}");
                }
                CreateTextElement(containerRT, sourceText, sb.ToString().TrimEnd());
            }
            
            // Add numbered steps
            if (detail.steps != null && detail.steps.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                int stepNum = 1;
                string stepColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
                foreach (var step in detail.steps)
                {
                    sb.AppendLine($"  <color={stepColor}>{stepNum}.</color> {step}");
                    stepNum++;
                }
                CreateTextElement(containerRT, sourceText, sb.ToString().TrimEnd());
            }
            
            // Render children
            if (detail.children != null)
            {
                foreach (var child in detail.children)
                {
                    RenderGuideSectionElement(containerRT, sourceText, child, categoryPrefab, page, depth + 1, detail.tocId);
                }
            }
        }

        /// <summary>
        /// Create Table of Contents for guide (uses existing TOC infrastructure)
        /// </summary>
        private static void CreateGuideTableOfContents(RectTransform parent, TMPro.TextMeshProUGUI sourceText, DeviceDescriptions guideData)
        {
            try
            {
                // Guard against null operationalDetails
                if (guideData.operationalDetails == null || guideData.operationalDetails.Count == 0)
                {
                    return;
                }
                
                // Create outer container that will hold the TOC box
                var outerGO = new GameObject("GuideTOCOuter");
                var outerRT = outerGO.AddComponent<RectTransform>();
                outerGO.transform.SetParent(parent, false);
                
                outerRT.anchorMin = new Vector2(0, 1);
                outerRT.anchorMax = new Vector2(1, 1);
                outerRT.pivot = new Vector2(0.5f, 1);
                
                // Add LayoutElement so the outer container participates in parent's vertical layout
                var outerLayoutElement = outerGO.AddComponent<UnityEngine.UI.LayoutElement>();
                outerLayoutElement.flexibleWidth = 1f;
                
                // Auto-size height for outer
                var outerFitter = outerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                outerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                outerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Add a horizontal layout to position TOC within outer
                var outerLayout = outerGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                outerLayout.childForceExpandWidth = false;
                outerLayout.childForceExpandHeight = false;
                outerLayout.childControlWidth = true;
                outerLayout.childControlHeight = true;
                outerLayout.childAlignment = UnityEngine.TextAnchor.UpperLeft;
                outerLayout.padding = new RectOffset(0, 0, 5, 10); // Add bottom padding
                
                // Create TOC container with background
                var tocGO = new GameObject("GuideTOC");
                tocGO.transform.SetParent(outerGO.transform, false);
                
                // Add background panel - matches device TOC style
                var bgImage = tocGO.AddComponent<UnityEngine.UI.Image>();
                if (VanillaModeManager.IsVanillaMode)
                {
                    if (_nativePanelSprite != null)
                    {
                        bgImage.sprite = _nativePanelSprite;
                        bgImage.type = _nativePanelType;
                        bgImage.material = _nativePanelMaterial;
                    }
                    bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
                }
                else
                {
                    var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
                    if (windowSprite != null)
                    {
                        bgImage.sprite = windowSprite;
                        bgImage.type = UnityEngine.UI.Image.Type.Sliced;
                    }
                    bgImage.color = new Color(0.05f, 0.1f, 0.2f, 0.95f);
                }
                
                // Add padding layout for TOC content - constrain max width to stay within bounds
                var tocLayout = tocGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                tocLayout.padding = new RectOffset(16, 16, 12, 12);
                tocLayout.spacing = 8; // Increased spacing between title and columns
                tocLayout.childForceExpandWidth = false;
                tocLayout.childForceExpandHeight = false;
                tocLayout.childControlWidth = true;
                tocLayout.childControlHeight = true;
                
                // Add LayoutElement to constrain max width
                var tocLayoutElement = tocGO.AddComponent<UnityEngine.UI.LayoutElement>();
                tocLayoutElement.flexibleWidth = 0f;
                tocLayoutElement.minWidth = 150f;
                
                // Auto-size for TOC - fit content but respect max width
                var tocFitter = tocGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                tocFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                tocFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create title
                var titleGO = new GameObject("TOCTitle");
                titleGO.transform.SetParent(tocGO.transform, false);
                var titleText = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
                titleText.font = sourceText.font;
                titleText.fontSharedMaterial = sourceText.fontSharedMaterial;
                titleText.fontSize = sourceText.fontSize * 0.9f;
                titleText.color = VanillaModeManager.IsVanillaMode ? Color.white : new Color(1f, 0.6f, 0.2f, 1f);
                titleText.text = string.IsNullOrEmpty(guideData.tocTitle) ? "<b>Contents</b>" : $"<b>{guideData.tocTitle}</b>";
                titleText.enableWordWrapping = false;
                
                var titleFitter = titleGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                titleFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Collect all TOC entries - use tocFlat option to control nesting
                var tocEntries = new List<(string tocId, string title, int depth)>();
                foreach (var detail in guideData.operationalDetails)
                {
                    CollectTocEntries(tocEntries, detail, 0, guideData.tocFlat);
                }
                
                // Calculate columns - same logic as device TOC
                const int MAX_ROWS = 8;
                int totalEntries = tocEntries.Count;
                int numColumns = (totalEntries + MAX_ROWS - 1) / MAX_ROWS;
                numColumns = Math.Max(1, Math.Min(3, numColumns)); // 1-3 columns max
                
                // Create horizontal container for columns
                var columnsGO = new GameObject("TOCColumns");
                columnsGO.transform.SetParent(tocGO.transform, false);
                
                var columnsLayout = columnsGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                columnsLayout.spacing = 24; // Increased spacing between columns
                columnsLayout.childForceExpandWidth = false;
                columnsLayout.childForceExpandHeight = false;
                columnsLayout.childControlWidth = true;
                columnsLayout.childControlHeight = true;
                columnsLayout.childAlignment = UnityEngine.TextAnchor.UpperLeft;
                
                var columnsFitter = columnsGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                columnsFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                columnsFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create each column
                int entryIndex = 0;
                for (int col = 0; col < numColumns && entryIndex < totalEntries; col++)
                {
                    var columnGO = new GameObject($"TOCColumn{col}");
                    columnGO.transform.SetParent(columnsGO.transform, false);
                    
                    var linksText = columnGO.AddComponent<TMPro.TextMeshProUGUI>();
                    linksText.font = sourceText.font;
                    linksText.fontSharedMaterial = sourceText.fontSharedMaterial;
                    linksText.fontSize = sourceText.fontSize * 0.85f;
                    linksText.color = sourceText.color;
                    linksText.enableWordWrapping = false;
                    linksText.richText = true;
                    linksText.overflowMode = TMPro.TextOverflowModes.Overflow;
                    linksText.lineSpacing = 8; // Add line spacing to prevent overlap
                    
                    // Build this column's entries
                    var sb = new System.Text.StringBuilder();
                    int rowsInColumn = 0;
                    while (entryIndex < totalEntries && rowsInColumn < MAX_ROWS)
                    {
                        var entry = tocEntries[entryIndex];
                        string indent = entry.depth > 0 ? new string(' ', entry.depth * 2) : "";
                        string bullet = entry.depth > 0 ? "- " : "";
                        string bulletColor = VanillaModeManager.IsVanillaMode ? "#888888" : "#CC6600";
                        string textColor = "#FFFFFF";
                        
                        if (entry.depth > 0)
                        {
                            sb.AppendLine($"{indent}<color={bulletColor}>{bullet}</color><link=toc_{entry.tocId}><color={textColor}>{entry.title}</color></link>");
                        }
                        else
                        {
                            sb.AppendLine($"<link=toc_{entry.tocId}><color={textColor}>{entry.title}</color></link>");
                        }
                        
                        entryIndex++;
                        rowsInColumn++;
                    }
                    linksText.text = sb.ToString().TrimEnd();
                    
                    // Add LayoutElement to limit column width
                    var colLayoutElement = columnGO.AddComponent<UnityEngine.UI.LayoutElement>();
                    colLayoutElement.preferredWidth = 180f; // Limit column width
                    
                    var linksFitter = columnGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    linksFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    linksFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    
                    // Add click handler for TOC links
                    var linkHandler = columnGO.AddComponent<TocLinkHandler>();
                    linkHandler.TextComponent = linksText;
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating guide TOC: {ex.Message}");
            }
        }


        /// <summary>
        /// Create a Part section (Part 1/2/3) with its own TOC box and nested sections
        /// </summary>
        private static void CreateSurvivalManualPart(RectTransform parent, OperationalDetail partDetail, 
            StationpediaCategory categoryPrefab, TMPro.TextMeshProUGUI sourceText, UniversalPage page)
        {
            // Create the Part category (Part 1, Part 2, Part 3)
            var partCategory = UnityEngine.Object.Instantiate(categoryPrefab, parent);
            partCategory.gameObject.name = $"Part_{partDetail.tocId}";
            
            // Set Part title with color
            string titleColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FF7A18";
            partCategory.Title.text = $"<color={titleColor}>{partDetail.title}</color>";
            
            // Apply custom icons
            ApplyCustomCategoryIcons(partCategory);
            
            // Configure the Part's contents layout
            ConfigureSurvivalManualPartLayout(partCategory);
            
            // Register for TOC navigation
            if (!string.IsNullOrEmpty(partDetail.tocId))
            {
                TocLinkHandler.RegisterSection(partDetail.tocId, partCategory.GetComponent<RectTransform>(), partCategory, null);
            }

            // Create Table of Contents box FIRST within the Part (using unified TOC)
            if (partDetail.children != null && partDetail.children.Count > 0)
            {
                // Collect TOC entries from children (sections within this Part)
                var tocEntries = new List<(string tocId, string title, int depth)>();
                foreach (var child in partDetail.children)
                {
                    if (child.tocId?.EndsWith("_toc") == true) continue;
                    CollectTocEntries(tocEntries, child, 0, false);
                }
                CreateUnifiedTableOfContents(partCategory.Contents, sourceText, tocEntries, "Table Of Contents", centerColumns: true);
            }
            
            // Create nested sections within this Part
            if (partDetail.children != null)
            {
                foreach (var sectionDetail in partDetail.children)
                {
                    // Skip the first child if it's the TOC we already handled
                    if (sectionDetail.tocId?.EndsWith("_toc") == true) continue;
                    
                    CreateSurvivalManualSection(partCategory.Contents, sectionDetail, categoryPrefab, sourceText, page, partDetail.tocId);
                }
            }
            
            // Default to EXPANDED state for Parts (user requested they start open)
            partCategory.Contents.gameObject.SetActive(true);
            if (partCategory.CollapseImage != null && partCategory.VisibleImage != null)
            {
                partCategory.CollapseImage.sprite = partCategory.VisibleImage;
            }
            
            var animator = partCategory.GetComponent<IconAnimator>();
            if (animator != null)
            {
                animator.Initialize(true); // Start expanded
            }
            
            page.CreatedCategories.Add(partCategory);
        }

        /// <summary>
        /// Create the Table of Contents box for a Part
        /// </summary>
        private static void CreatePartTableOfContents(RectTransform parent, OperationalDetail partDetail, TMPro.TextMeshProUGUI sourceText)
        {
            // Create TOC container with background
            var tocContainerGO = new GameObject("TableOfContents");
            tocContainerGO.transform.SetParent(parent, false);
            tocContainerGO.transform.SetAsFirstSibling();
            
            // Add background
            var tocBg = tocContainerGO.AddComponent<UnityEngine.UI.Image>();
            var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
            if (windowSprite != null)
            {
                tocBg.sprite = windowSprite;
                tocBg.type = UnityEngine.UI.Image.Type.Sliced;
            }
            tocBg.color = new Color(0.3f, 0.3f, 0.3f, 0.15f);
            
            // Add layout
            var tocLayout = tocContainerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            tocLayout.padding = new RectOffset(16, 16, 12, 12);
            tocLayout.spacing = 8;
            tocLayout.childForceExpandWidth = true;
            tocLayout.childForceExpandHeight = false;
            tocLayout.childControlWidth = true;
            tocLayout.childControlHeight = true;
            
            var tocFitter = tocContainerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            tocFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            tocFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            // Create centered title
            var titleGO = new GameObject("TOCTitle");
            titleGO.transform.SetParent(tocContainerGO.transform, false);
            var titleText = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
            titleText.font = sourceText.font;
            titleText.fontSharedMaterial = sourceText.fontSharedMaterial;
            titleText.fontSize = sourceText.fontSize * 0.95f;
            titleText.color = VanillaModeManager.IsVanillaMode ? Color.white : new Color(1f, 0.6f, 0.2f, 1f);
            titleText.text = "<b>Table Of Contents</b>";
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.enableWordWrapping = false;
            
            var titleFitter = titleGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            titleFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            // Create columns container
            var columnsGO = new GameObject("TOCColumns");
            columnsGO.transform.SetParent(tocContainerGO.transform, false);
            
            var columnsLayout = columnsGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            columnsLayout.spacing = 30;
            columnsLayout.childForceExpandWidth = false;
            columnsLayout.childForceExpandHeight = false;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childAlignment = UnityEngine.TextAnchor.UpperCenter;
            
            var columnsFitter = columnsGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            columnsFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            columnsFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            // Add horizontal layout element to center it
            var columnsLE = columnsGO.AddComponent<UnityEngine.UI.LayoutElement>();
            columnsLE.flexibleWidth = 1;

            // Collect TOC entries from children (sections within this Part)
            var tocEntries = new List<(string tocId, string title, int depth)>();
            if (partDetail.children != null)
            {
                foreach (var child in partDetail.children)
                {
                    // Skip TOC entries that we create
                    if (child.tocId?.EndsWith("_toc") == true) continue;
                    CollectTocEntriesFromSection(tocEntries, child, 0);
                }
            }

            // Calculate columns (max 8 items per column, up to 3 columns)
            const int MAX_ROWS = 8;
            int totalEntries = tocEntries.Count;
            int numColumns = Math.Min(3, (totalEntries + MAX_ROWS - 1) / MAX_ROWS);
            numColumns = Math.Max(1, numColumns);
            
            int entriesPerColumn = (totalEntries + numColumns - 1) / numColumns;
            
            // Create columns
            int entryIndex = 0;
            for (int col = 0; col < numColumns && entryIndex < totalEntries; col++)
            {
                var columnGO = new GameObject($"TOCColumn{col}");
                columnGO.transform.SetParent(columnsGO.transform, false);
                
                var columnText = columnGO.AddComponent<TMPro.TextMeshProUGUI>();
                columnText.font = sourceText.font;
                columnText.fontSharedMaterial = sourceText.fontSharedMaterial;
                columnText.fontSize = sourceText.fontSize * 0.8f;  // Slightly smaller for better fit
                columnText.color = sourceText.color;
                columnText.enableWordWrapping = true;  // Enable word wrapping to prevent overflow
                columnText.overflowMode = TMPro.TextOverflowModes.Ellipsis;  // Truncate with ellipsis if needed
                columnText.richText = true;
                
                // Add LayoutElement for better column width control
                var columnLE = columnGO.AddComponent<UnityEngine.UI.LayoutElement>();
                columnLE.preferredWidth = 200;  // Fixed column width
                columnLE.flexibleWidth = 0;
                
                var sb = new System.Text.StringBuilder();
                int rowsInColumn = 0;
                
                while (entryIndex < totalEntries && rowsInColumn < entriesPerColumn)
                {
                    var entry = tocEntries[entryIndex];
                    string indent = entry.depth > 0 ? "  " : "";  // Smaller indent for better fit
                    string bullet = entry.depth > 0 ? "• " : "";
                    
                    // In vanilla mode: section titles are underlined and bolded
                    // In ascended mode: section titles get orange color
                    if (entry.depth == 0)
                    {
                        // Main section - underlined+bold in vanilla, orange in ascended
                        if (VanillaModeManager.IsVanillaMode)
                        {
                            sb.AppendLine($"<link=toc_{entry.tocId}><u><b><color=#FFFFFF>{entry.title}</color></b></u></link>");
                        }
                        else
                        {
                            sb.AppendLine($"<link=toc_{entry.tocId}><b><color=#FFA500>{entry.title}</color></b></link>");
                        }
                    }
                    else
                    {
                        // Subsection
                        string bulletColor = VanillaModeManager.IsVanillaMode ? "#888888" : "#CC6600";
                        sb.AppendLine($"{indent}<color={bulletColor}>{bullet}</color><link=toc_{entry.tocId}><color=#FFFFFF>{entry.title}</color></link>");
                    }
                    
                    entryIndex++;
                    rowsInColumn++;
                }
                
                columnText.text = sb.ToString().TrimEnd();
                
                var colFitter = columnGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                colFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;  // Use LayoutElement width
                colFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Add click handler
                var linkHandler = columnGO.AddComponent<TocLinkHandler>();
                linkHandler.TextComponent = columnText;
            }
        }

        /// <summary>
        /// Collect TOC entries from a section and its children
        /// </summary>
        private static void CollectTocEntriesFromSection(List<(string tocId, string title, int depth)> entries, 
            OperationalDetail detail, int depth)
        {
            if (!string.IsNullOrEmpty(detail.tocId) && !string.IsNullOrEmpty(detail.title))
            {
                entries.Add((detail.tocId, detail.title, depth));
            }
            
            if (detail.children != null)
            {
                foreach (var child in detail.children)
                {
                    CollectTocEntriesFromSection(entries, child, depth + 1);
                }
            }
        }

        /// <summary>
        /// Create a nested section within a Part - now uses unified section renderer
        /// </summary>
        private static void CreateSurvivalManualSection(RectTransform parent, OperationalDetail sectionDetail,
            StationpediaCategory categoryPrefab, TMPro.TextMeshProUGUI sourceText, UniversalPage page, string parentTocId)
        {
            // Recursive callback for children
            Action<RectTransform, OperationalDetail, int, string> recurse = (p, d, dep, pid) => 
                CreateSurvivalManualSection(p, d, categoryPrefab, sourceText, page, pid);
            
            if (!sectionDetail.collapsible || string.IsNullOrEmpty(sectionDetail.title))
            {
                // Non-collapsible content - use unified inline renderer
                CreateUnifiedInlineContent(parent, sourceText, sectionDetail, categoryPrefab, page, 1, parentTocId, recurse);
                return;
            }
            
            // Use unified collapsible section (depth 1 for Manual sections)
            CreateUnifiedCollapsibleSection(parent, sourceText, sectionDetail, categoryPrefab, page, 1, parentTocId, recurse);
        }

        /// <summary>
        /// DEPRECATED - kept for reference, but CreateSurvivalManualSection now uses unified renderer
        /// Create collapsible section - old implementation
        /// </summary>
        private static void CreateSurvivalManualSectionOld(RectTransform parent, OperationalDetail sectionDetail,
            StationpediaCategory categoryPrefab, TMPro.TextMeshProUGUI sourceText, UniversalPage page, string parentTocId)
        {
            if (!sectionDetail.collapsible || string.IsNullOrEmpty(sectionDetail.title))
            {
                // Non-collapsible content - render as inline text
                if (!string.IsNullOrEmpty(sectionDetail.description))
                {
                    CreateTextElement(parent, sourceText, sectionDetail.description);
                }
                return;
            }
            
            // Create collapsible section
            var sectionCategory = UnityEngine.Object.Instantiate(categoryPrefab, parent);
            sectionCategory.gameObject.name = $"Section_{sectionDetail.tocId}";
            
            // Set title
            string titleColor = VanillaModeManager.GetTitleColor(1); // Depth 1 for sections
            sectionCategory.Title.text = $"<color={titleColor}>{sectionDetail.title}</color>";
            
            ApplyCustomCategoryIcons(sectionCategory);
            ConfigureNestedCategoryLayout(sectionCategory, sectionDetail, 0);
            
            // Register for TOC
            if (!string.IsNullOrEmpty(sectionDetail.tocId))
            {
                TocLinkHandler.RegisterSection(sectionDetail.tocId, sectionCategory.GetComponent<RectTransform>(), sectionCategory, parentTocId);
            }
            
            // Add description
            if (!string.IsNullOrEmpty(sectionDetail.description))
            {
                CreateTextElement(sectionCategory.Contents, sourceText, sectionDetail.description);
            }
            
            // Add bullet items
            if (sectionDetail.items != null && sectionDetail.items.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var item in sectionDetail.items)
                {
                    sb.AppendLine($"  • {item}");
                }
                CreateTextElement(sectionCategory.Contents, sourceText, sb.ToString().TrimEnd());
            }
            
            // Add steps
            if (sectionDetail.steps != null && sectionDetail.steps.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                int stepNum = 1;
                string stepColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
                foreach (var step in sectionDetail.steps)
                {
                    sb.AppendLine($"  <color={stepColor}>{stepNum}.</color> {step}");
                    stepNum++;
                }
                CreateTextElement(sectionCategory.Contents, sourceText, sb.ToString().TrimEnd());
            }
            
            // Add children (subsections)
            if (sectionDetail.children != null)
            {
                foreach (var child in sectionDetail.children)
                {
                    CreateSurvivalManualSection(sectionCategory.Contents, child, categoryPrefab, sourceText, page, sectionDetail.tocId);
                }
            }
            
            // Default to collapsed
            sectionCategory.Contents.gameObject.SetActive(false);
            if (sectionCategory.CollapseImage != null && sectionCategory.NotVisibleImage != null)
            {
                sectionCategory.CollapseImage.sprite = sectionCategory.NotVisibleImage;
            }
            
            var animator = sectionCategory.GetComponent<IconAnimator>();
            if (animator != null)
            {
                animator.Initialize(false);
            }
            
            page.CreatedCategories.Add(sectionCategory);
        }

        /// <summary>
        /// Configure layout for Survival Manual Part sections
        /// </summary>
        private static void ConfigureSurvivalManualPartLayout(StationpediaCategory category)
        {
            // Remove GridLayoutGroup if present
            var existingGridLayout = category.Contents.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (existingGridLayout != null)
            {
                UnityEngine.Object.DestroyImmediate(existingGridLayout);
            }
            
            // Add background
            var bgImage = category.Contents.gameObject.GetComponent<UnityEngine.UI.Image>();
            if (bgImage == null)
            {
                bgImage = category.Contents.gameObject.AddComponent<UnityEngine.UI.Image>();
            }
            
            if (VanillaModeManager.IsVanillaMode)
            {
                // Use cached native sprite if available
                if (_nativePanelSprite != null)
                {
                    bgImage.sprite = _nativePanelSprite;
                    bgImage.type = _nativePanelType;
                    bgImage.material = _nativePanelMaterial;
                }
                bgImage.color = Color.white;
            }
            else
            {
                var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
                if (windowSprite != null)
                {
                    bgImage.sprite = windowSprite;
                    bgImage.type = UnityEngine.UI.Image.Type.Sliced;
                    bgImage.material = null;
                }
                bgImage.color = StationeersBlue;
            }
            
            // Add VerticalLayoutGroup
            var layout = category.Contents.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = category.Contents.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            }
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 10;
            layout.padding = new RectOffset(12, 12, 12, 12);
            
            // Add ContentSizeFitter
            var fitter = category.Contents.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = category.Contents.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            }
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        }

        private static void CreateOperationalDetailsCategory(UniversalPage page, Transform contentTransform, DeviceDescriptions deviceDesc)
        {
            // Clear TOC registry for new page
            TocLinkHandler.ClearRegistry();
            
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
            
            // Set the title - use VanillaModeManager for color (white in vanilla, orange in ascended)
            string titleColor;
            if (VanillaModeManager.IsVanillaMode)
            {
                titleColor = "#FFFFFF";  // White in vanilla mode
            }
            else if (!string.IsNullOrEmpty(deviceDesc.operationalDetailsTitleColor))
            {
                titleColor = deviceDesc.operationalDetailsTitleColor;
            }
            else
            {
                titleColor = "#FF7A18";  // Default orange in ascended mode
            }
            category.Title.text = $"<color={titleColor}>Operational Details</color>";
            
            // Apply custom collapse/expand icons with animator
            ApplyCustomCategoryIcons(category);
            
            // NOTE: Removed AddCategoryIcon() call - left-side icon was unwanted
            
            // Configure category layout FIRST before adding content
            ConfigureCategoryLayout(category, page, deviceDesc);

            // Create Table of Contents if enabled (using unified TOC)
            if (deviceDesc.generateToc && deviceDesc.operationalDetails != null && deviceDesc.operationalDetails.Count > 0)
            {
                var tocEntries = new List<(string tocId, string title, int depth)>();
                foreach (var detail in deviceDesc.operationalDetails)
                {
                    CollectTocEntries(tocEntries, detail, 0, deviceDesc.tocFlat);
                }
                string tocTitle = string.IsNullOrEmpty(deviceDesc.tocTitle) ? "Contents" : deviceDesc.tocTitle;
                CreateUnifiedTableOfContents(category.Contents, sourceText, tocEntries, tocTitle, centerColumns: true);
            }

            // Render operational details - either as nested collapsibles or text
            RenderAllOperationalDetails(category, sourceText, deviceDesc, categoryPrefab, page);
            
            // Move and set initial state
            category.transform.SetSiblingIndex(20);
            
            // Default to COLLAPSED state
            category.Contents.gameObject.SetActive(false);
            if (category.CollapseImage != null && category.NotVisibleImage != null)
            {
                category.CollapseImage.sprite = category.NotVisibleImage;
            }
            
            // Update icon animator state
            var animator = category.GetComponent<IconAnimator>();
            if (animator != null)
            {
                animator.Initialize(false);
            }
            
            // Force rebuild of the main content area to fix scrollbar visibility
            if (Stationpedia.Instance?.ContentRectTransform != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(Stationpedia.Instance.ContentRectTransform);
            }
            
            // Add to game's category list so it handles cleanup
            page.CreatedCategories.Add(category);
        }

        /// <summary>
        /// Apply custom icons and animator to a category's collapse button
        /// </summary>
        private static void ApplyCustomCategoryIcons(StationpediaCategory category)
        {
            try
            {
                if (category.CollapseImage == null) return;
                
                // In vanilla mode, keep native icons; in Ascended mode, use custom icons
                if (!VanillaModeManager.IsVanillaMode)
                {
                    // Apply custom sprites if available
                    if (StationpediaAscendedMod._iconExpanded != null)
                    {
                        category.VisibleImage = StationpediaAscendedMod._iconExpanded;
                    }
                    if (StationpediaAscendedMod._iconCollapsed != null)
                    {
                        category.NotVisibleImage = StationpediaAscendedMod._iconCollapsed;
                    }
                }
                // If vanilla mode, the category keeps its original native sprites
                
                // Add IconAnimator component for smooth transitions
                var animator = category.gameObject.GetComponent<IconAnimator>();
                if (animator == null)
                {
                    animator = category.gameObject.AddComponent<IconAnimator>();
                }
                animator.TargetImage = category.CollapseImage;
                animator.ExpandedSprite = category.VisibleImage;
                animator.CollapsedSprite = category.NotVisibleImage;
                
                // Make the entire header clickable for toggle
                // Find or create header area (parent of Title and CollapseImage)
                Transform headerTransform = category.Title?.transform.parent;
                if (headerTransform == null)
                {
                    headerTransform = category.CollapseImage?.transform.parent;
                }
                if (headerTransform != null)
                {
                    // Add CategoryHeaderHandler to handle clicks and hover on the header
                    var headerHandler = headerTransform.gameObject.GetComponent<CategoryHeaderHandler>();
                    if (headerHandler == null)
                    {
                        headerHandler = headerTransform.gameObject.AddComponent<CategoryHeaderHandler>();
                    }
                    headerHandler.Initialize(category, animator);
                    
                    // Ensure header has a graphic for raycasting (needs Image or similar)
                    var headerGraphic = headerTransform.GetComponent<UnityEngine.UI.Graphic>();
                    if (headerGraphic == null)
                    {
                        // Add invisible image for raycast
                        var headerImage = headerTransform.gameObject.AddComponent<UnityEngine.UI.Image>();
                        headerImage.color = new Color(0, 0, 0, 0); // Fully transparent
                        headerImage.raycastTarget = true;
                    }
                    else
                    {
                        headerGraphic.raycastTarget = true;
                    }
                    
                    // Disable raycast on Title so clicks pass through to header
                    if (category.Title != null)
                    {
                        category.Title.raycastTarget = false;
                    }
                    
                    // Disable raycast on CollapseImage so clicks pass through to header
                    if (category.CollapseImage != null)
                    {
                        category.CollapseImage.raycastTarget = false;
                    }
                }
                
                // Hook into the toggle to trigger animation
                // We can't easily hook the button click, so we'll check state in Update via coroutine
                if (StationpediaAscendedMod.Instance != null)
                {
                    StationpediaAscendedMod.Instance.StartCoroutine(MonitorCategoryState(category, animator));
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error applying custom icons: {ex.Message}");
            }
        }

        /// <summary>
        /// Coroutine to monitor category state and trigger icon animation
        /// </summary>
        private static IEnumerator MonitorCategoryState(StationpediaCategory category, IconAnimator animator)
        {
            if (category == null || animator == null) yield break;
            
            bool? lastState = null;
            while (category != null && category.Contents != null)
            {
                bool isExpanded = category.Contents.gameObject.activeSelf;
                if (lastState != isExpanded)
                {
                    lastState = isExpanded;
                    animator.SetState(isExpanded, lastState.HasValue); // Animate only after initial state
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        /// <summary>
        /// Create Table of Contents panel at the top of Operational Details
        /// </summary>
        private static void CreateTableOfContents(StationpediaCategory parentCategory, TMPro.TextMeshProUGUI sourceText, DeviceDescriptions deviceDesc)
        {
            try
            {
                // Guard against null operationalDetails
                if (deviceDesc.operationalDetails == null || deviceDesc.operationalDetails.Count == 0)
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] TOC: No operational details found");
                    return;
                }
                
                // Create outer container with horizontal layout (TOC on left, thumbnail on right)
                var outerGO = new GameObject("TableOfContentsOuter");
                
                // Add RectTransform BEFORE setting parent to ensure proper UI setup
                var outerRT = outerGO.AddComponent<RectTransform>();
                outerGO.transform.SetParent(parentCategory.Contents, false);
                outerGO.transform.SetAsFirstSibling(); // Put at top
                
                // Set up outer RectTransform
                outerRT.anchorMin = new Vector2(0, 1);
                outerRT.anchorMax = new Vector2(1, 1);
                outerRT.pivot = new Vector2(0.5f, 1);
                
                // Horizontal layout for outer container
                var outerLayout = outerGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                outerLayout.spacing = 10;
                outerLayout.childForceExpandWidth = false;
                outerLayout.childForceExpandHeight = true;
                outerLayout.childControlWidth = true;
                outerLayout.childControlHeight = true;
                outerLayout.childAlignment = UnityEngine.TextAnchor.UpperLeft;
                
                // Auto-size height for outer
                var outerFitter = outerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                outerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                outerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create TOC container (left side with background)
                var tocGO = new GameObject("TableOfContents");
                tocGO.transform.SetParent(outerGO.transform, false);
                
                // Add background panel to TOC - use fancy window background with transparent grey
                var bgImage = tocGO.AddComponent<UnityEngine.UI.Image>();
                var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
                if (windowSprite != null)
                {
                    bgImage.sprite = windowSprite;
                    bgImage.type = UnityEngine.UI.Image.Type.Sliced;
                }
                bgImage.color = new Color(0.4f, 0.4f, 0.4f, 0.08f); // Fully transparent
                
                // TOC should fit its content width, not expand - with max width to prevent overflow
                var tocLayoutElement = tocGO.AddComponent<UnityEngine.UI.LayoutElement>();
                tocLayoutElement.flexibleWidth = 0f; // Don't expand
                tocLayoutElement.minWidth = 120f;
                tocLayoutElement.preferredWidth = 620f; // Max width to stay within bounds
                
                // Add padding layout for TOC content
                var tocLayout = tocGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                tocLayout.padding = new RectOffset(16, 16, 12, 12);
                tocLayout.spacing = 6;
                tocLayout.childForceExpandWidth = true;
                tocLayout.childForceExpandHeight = false;
                tocLayout.childControlWidth = true;
                tocLayout.childControlHeight = true;
                
                // Auto-size for TOC - fit both width and height to content
                var tocFitter = tocGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                tocFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                tocFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create title
                var titleGO = new GameObject("TOCTitle");
                titleGO.transform.SetParent(tocGO.transform, false);
                var titleText = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
                titleText.font = sourceText.font;
                titleText.fontSharedMaterial = sourceText.fontSharedMaterial;
                titleText.fontSize = sourceText.fontSize * 0.9f;
                // Use VanillaModeManager for color (white in vanilla, orange in ascended)
                titleText.color = VanillaModeManager.IsVanillaMode ? Color.white : new Color(1f, 0.6f, 0.2f, 1f);
                // No box character - just the title text
                titleText.text = string.IsNullOrEmpty(deviceDesc.tocTitle) ? "<b>Contents</b>" : $"<b>{deviceDesc.tocTitle}</b>";
                titleText.enableWordWrapping = false;
                
                var titleFitter = titleGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                titleFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Collect all TOC entries first to determine column layout
                var tocEntries = new List<(string tocId, string title, int depth)>();
                foreach (var detail in deviceDesc.operationalDetails)
                {
                    CollectTocEntries(tocEntries, detail, 0, deviceDesc.tocFlat);
                }
                
                const int MAX_ROWS = 8;
                int totalEntries = tocEntries.Count;
                int numColumns = (totalEntries + MAX_ROWS - 1) / MAX_ROWS; // Ceiling division
                numColumns = Math.Max(1, numColumns); // At least 1 column
                
                // Create horizontal container for columns
                var columnsGO = new GameObject("TOCColumns");
                columnsGO.transform.SetParent(tocGO.transform, false);
                
                var columnsLayout = columnsGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                columnsLayout.spacing = 20;
                columnsLayout.childForceExpandWidth = false;
                columnsLayout.childForceExpandHeight = false;
                columnsLayout.childControlWidth = true;
                columnsLayout.childControlHeight = true;
                columnsLayout.childAlignment = UnityEngine.TextAnchor.UpperLeft;
                
                var columnsFitter = columnsGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                columnsFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                columnsFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create each column
                int entryIndex = 0;
                for (int col = 0; col < numColumns && entryIndex < totalEntries; col++)
                {
                    var columnGO = new GameObject($"TOCColumn{col}");
                    columnGO.transform.SetParent(columnsGO.transform, false);
                    
                    var linksText = columnGO.AddComponent<TMPro.TextMeshProUGUI>();
                    linksText.font = sourceText.font;
                    linksText.fontSharedMaterial = sourceText.fontSharedMaterial;
                    linksText.fontSize = sourceText.fontSize * 0.85f;
                    linksText.color = sourceText.color;
                    linksText.enableWordWrapping = true;  // Enable word wrapping to prevent overflow
                    linksText.richText = true;
                    linksText.overflowMode = TMPro.TextOverflowModes.Ellipsis;  // Truncate with ellipsis if needed
                    linksText.lineSpacing = 8;  // Add line spacing to prevent overlap
                    
                    // Add LayoutElement for column width control
                    var colLayoutElement = columnGO.AddComponent<UnityEngine.UI.LayoutElement>();
                    colLayoutElement.preferredWidth = 200;  // Fixed column width like Survival Manual
                    colLayoutElement.flexibleWidth = 0;
                    
                    // Build this column's entries
                    var sb = new System.Text.StringBuilder();
                    int rowsInColumn = 0;
                    while (entryIndex < totalEntries && rowsInColumn < MAX_ROWS)
                    {
                        var entry = tocEntries[entryIndex];
                        string indent = entry.depth > 0 ? new string(' ', entry.depth * 2) : "";
                        string bullet = entry.depth > 0 ? "- " : "";
                        string bulletColor = VanillaModeManager.IsVanillaMode ? "#888888" : "#CC6600";
                        string textColor = "#FFFFFF";
                        
                        if (entry.depth > 0)
                        {
                            sb.AppendLine($"{indent}<color={bulletColor}>{bullet}</color><link=toc_{entry.tocId}><color={textColor}>{entry.title}</color></link>");
                        }
                        else
                        {
                            sb.AppendLine($"<link=toc_{entry.tocId}><color={textColor}>{entry.title}</color></link>");
                        }
                        
                        entryIndex++;
                        rowsInColumn++;
                    }
                    linksText.text = sb.ToString().TrimEnd();
                    
                    var linksFitter = columnGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    linksFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    linksFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    
                    // Add click handler for TOC links
                    var linkHandler = columnGO.AddComponent<TocLinkHandler>();
                    linkHandler.TextComponent = linksText;
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating TOC: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively build TOC links from operational details
        /// </summary>
        private static void BuildTocLinks(System.Text.StringBuilder sb, OperationalDetail detail, ref int index, int depth = 0)
        {
            if (!string.IsNullOrEmpty(detail.tocId) && !string.IsNullOrEmpty(detail.title))
            {
                string indent = depth > 0 ? new string(' ', depth * 3) : "";
                // Use dash for sub-items, no bullet for top-level
                string bullet = depth > 0 ? "- " : "";
                // Use VanillaModeManager for colors - grey bullets, white text
                string bulletColor = VanillaModeManager.IsVanillaMode ? "#888888" : "#CC6600";
                string textColor = "#FFFFFF";  // Always white text
                if (depth > 0)
                {
                    sb.AppendLine($"{indent}<color={bulletColor}>{bullet}</color><link=toc_{detail.tocId}><color={textColor}>{detail.title}</color></link>");
                }
                else
                {
                    sb.AppendLine($"<link=toc_{detail.tocId}><color={textColor}>{detail.title}</color></link>");
                }
                index++;
            }
            
            if (detail.children != null)
            {
                foreach (var child in detail.children)
                {
                    BuildTocLinks(sb, child, ref index, depth + 1);
                }
            }
        }
        
        /// <summary>
        /// Collect TOC entries into a flat list for column layout
        /// </summary>
        private static void CollectTocEntries(List<(string tocId, string title, int depth)> entries, OperationalDetail detail, int depth = 0, bool flatMode = false)
        {
            if (!string.IsNullOrEmpty(detail.tocId) && !string.IsNullOrEmpty(detail.title))
            {
                // In flat mode, all entries are at depth 0
                int effectiveDepth = flatMode ? 0 : depth;
                entries.Add((detail.tocId, detail.title, effectiveDepth));
            }
            
            if (detail.children != null)
            {
                foreach (var child in detail.children)
                {
                    CollectTocEntries(entries, child, depth + 1, flatMode);
                }
            }
        }

        /// <summary>
        /// Unified Table of Contents rendering - based on Survival Manual styling (centered, 3 columns max, proper padding)
        /// </summary>
        private static void CreateUnifiedTableOfContents(
            RectTransform parent,
            TMPro.TextMeshProUGUI sourceText,
            List<(string tocId, string title, int depth)> tocEntries,
            string title = "Contents",
            bool centerColumns = true,
            bool placeAtTop = true)
        {
            try
            {
                if (tocEntries == null || tocEntries.Count == 0) return;

                // Create TOC container with background
                var tocContainerGO = new GameObject("TableOfContents");
                tocContainerGO.transform.SetParent(parent, false);
                if (placeAtTop) tocContainerGO.transform.SetAsFirstSibling();
                
                // Add background - semi-transparent panel
                var tocBg = tocContainerGO.AddComponent<UnityEngine.UI.Image>();
                var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
                if (windowSprite != null)
                {
                    tocBg.sprite = windowSprite;
                    tocBg.type = UnityEngine.UI.Image.Type.Sliced;
                }
                tocBg.color = new Color(0.3f, 0.3f, 0.3f, 0.15f);
                
                // Vertical layout for title + columns
                var tocLayout = tocContainerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                tocLayout.padding = new RectOffset(16, 16, 12, 12);
                tocLayout.spacing = 8;
                tocLayout.childForceExpandWidth = true;
                tocLayout.childForceExpandHeight = false;
                tocLayout.childControlWidth = true;
                tocLayout.childControlHeight = true;
                
                var tocFitter = tocContainerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                tocFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                tocFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create centered title
                var titleGO = new GameObject("TOCTitle");
                titleGO.transform.SetParent(tocContainerGO.transform, false);
                var titleText = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
                titleText.font = sourceText.font;
                titleText.fontSharedMaterial = sourceText.fontSharedMaterial;
                titleText.fontSize = sourceText.fontSize * 0.95f;
                titleText.color = VanillaModeManager.IsVanillaMode ? Color.white : new Color(1f, 0.6f, 0.2f, 1f);
                titleText.text = $"<b>{title}</b>";
                titleText.alignment = centerColumns ? TMPro.TextAlignmentOptions.Center : TMPro.TextAlignmentOptions.Left;
                titleText.enableWordWrapping = false;
                
                var titleFitter = titleGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                titleFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create columns container
                var columnsGO = new GameObject("TOCColumns");
                columnsGO.transform.SetParent(tocContainerGO.transform, false);
                
                var columnsLayout = columnsGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                columnsLayout.spacing = 30;  // Survival Manual spacing
                columnsLayout.childForceExpandWidth = false;
                columnsLayout.childForceExpandHeight = false;
                columnsLayout.childControlWidth = true;
                columnsLayout.childControlHeight = true;
                columnsLayout.childAlignment = centerColumns ? UnityEngine.TextAnchor.UpperCenter : UnityEngine.TextAnchor.UpperLeft;
                
                var columnsFitter = columnsGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                columnsFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                columnsFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Add layout element to center columns horizontally
                var columnsLE = columnsGO.AddComponent<UnityEngine.UI.LayoutElement>();
                columnsLE.flexibleWidth = 1;

                // Calculate columns (max 8 items per column, up to 3 columns)
                const int MAX_ROWS = 8;
                const int MAX_COLUMNS = 3;
                int totalEntries = tocEntries.Count;
                int numColumns = Math.Min(MAX_COLUMNS, (totalEntries + MAX_ROWS - 1) / MAX_ROWS);
                numColumns = Math.Max(1, numColumns);
                int entriesPerColumn = (totalEntries + numColumns - 1) / numColumns;
                
                // Create columns
                int entryIndex = 0;
                for (int col = 0; col < numColumns && entryIndex < totalEntries; col++)
                {
                    var columnGO = new GameObject($"TOCColumn{col}");
                    columnGO.transform.SetParent(columnsGO.transform, false);
                    
                    var columnText = columnGO.AddComponent<TMPro.TextMeshProUGUI>();
                    columnText.font = sourceText.font;
                    columnText.fontSharedMaterial = sourceText.fontSharedMaterial;
                    columnText.fontSize = sourceText.fontSize * 0.8f;
                    columnText.color = sourceText.color;
                    columnText.enableWordWrapping = true;
                    columnText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                    columnText.richText = true;
                    columnText.lineSpacing = 8;  // Prevent overlap
                    
                    // Fixed column width for consistency
                    var columnLE = columnGO.AddComponent<UnityEngine.UI.LayoutElement>();
                    columnLE.preferredWidth = 200;
                    columnLE.flexibleWidth = 0;
                    
                    var sb = new System.Text.StringBuilder();
                    int rowsInColumn = 0;
                    
                    while (entryIndex < totalEntries && rowsInColumn < entriesPerColumn)
                    {
                        var entry = tocEntries[entryIndex];
                        string indent = entry.depth > 0 ? "  " : "";
                        string bullet = entry.depth > 0 ? "• " : "";
                        
                        if (entry.depth == 0)
                        {
                            // Main section - underlined+bold in vanilla, orange+bold in ascended
                            if (VanillaModeManager.IsVanillaMode)
                            {
                                sb.AppendLine($"<link=toc_{entry.tocId}><u><b><color=#FFFFFF>{entry.title}</color></b></u></link>");
                            }
                            else
                            {
                                sb.AppendLine($"<link=toc_{entry.tocId}><b><color=#FFA500>{entry.title}</color></b></link>");
                            }
                        }
                        else
                        {
                            // Subsection with bullet
                            string bulletColor = VanillaModeManager.IsVanillaMode ? "#888888" : "#CC6600";
                            sb.AppendLine($"{indent}<color={bulletColor}>{bullet}</color><link=toc_{entry.tocId}><color=#FFFFFF>{entry.title}</color></link>");
                        }
                        
                        entryIndex++;
                        rowsInColumn++;
                    }
                    
                    columnText.text = sb.ToString().TrimEnd();
                    
                    var colFitter = columnGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    colFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                    colFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    
                    // Add click handler for TOC links
                    var linkHandler = columnGO.AddComponent<TocLinkHandler>();
                    linkHandler.TextComponent = columnText;
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating unified TOC: {ex.Message}");
            }
        }

        /// <summary>
        /// Unified collapsible section renderer - handles devices, guides, and survival manual sections
        /// </summary>
        private static void CreateUnifiedCollapsibleSection(
            RectTransform parent,
            TMPro.TextMeshProUGUI sourceText,
            OperationalDetail detail,
            StationpediaCategory categoryPrefab,
            UniversalPage page,
            int depth,
            string parentTocId,
            Action<RectTransform, OperationalDetail, int, string> recurseCallback)
        {
            try
            {
                // Create nested category
                var nestedCategory = UnityEngine.Object.Instantiate<StationpediaCategory>(categoryPrefab, parent);
                if (nestedCategory == null) return;
                
                string safeTocId = detail.tocId ?? detail.title?.Replace(" ", "_") ?? $"section_{depth}";
                nestedCategory.gameObject.name = $"Section_{safeTocId}";
                
                // Set title with depth-based coloring
                string titleColor = VanillaModeManager.GetTitleColor(depth);
                nestedCategory.Title.text = $"<color={titleColor}>{detail.title}</color>";
                
                // Apply custom icons (respects vanilla mode)
                ApplyCustomCategoryIcons(nestedCategory);
                
                // Configure layout with proper backgrounds
                ConfigureNestedCategoryLayout(nestedCategory, detail, depth);
                
                // Register for TOC navigation
                if (!string.IsNullOrEmpty(detail.tocId))
                {
                    TocLinkHandler.RegisterSection(detail.tocId, nestedCategory.GetComponent<RectTransform>(), nestedCategory, parentTocId);
                }
                
                // Add image if specified
                if (!string.IsNullOrEmpty(detail.imageFile))
                {
                    CreateInlineImage(nestedCategory.Contents, detail.imageFile);
                }
                
                // Add description text
                if (!string.IsNullOrEmpty(detail.description))
                {
                    CreateTextElement(nestedCategory.Contents, sourceText, detail.description);
                }
                
                // Add bullet items
                if (detail.items != null && detail.items.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var item in detail.items)
                    {
                        sb.AppendLine($"  • {item}");
                    }
                    CreateTextElement(nestedCategory.Contents, sourceText, sb.ToString().TrimEnd());
                }
                
                // Add numbered steps
                if (detail.steps != null && detail.steps.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    int stepNum = 1;
                    string stepColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
                    foreach (var step in detail.steps)
                    {
                        sb.AppendLine($"  <color={stepColor}>{stepNum}.</color> {step}");
                        stepNum++;
                    }
                    CreateTextElement(nestedCategory.Contents, sourceText, sb.ToString().TrimEnd());
                }
                
                // Add YouTube link if specified
                if (!string.IsNullOrEmpty(detail.youtubeUrl))
                {
                    CreateYouTubeLink(nestedCategory.Contents, sourceText, detail.youtubeUrl, detail.youtubeLabel);
                }
                
                // Add inline video if specified
                if (!string.IsNullOrEmpty(detail.videoFile))
                {
                    CreateInlineVideo(nestedCategory.Contents, detail.videoFile);
                }
                
                // Add table if specified
                if (detail.table != null && detail.table.Count > 0)
                {
                    CreateTableElement(nestedCategory.Contents, sourceText, detail.table);
                }
                
                // Recursively render children using the provided callback
                if (detail.children != null && recurseCallback != null)
                {
                    foreach (var child in detail.children)
                    {
                        recurseCallback(nestedCategory.Contents, child, depth + 1, detail.tocId);
                    }
                }
                
                // Default to collapsed
                nestedCategory.Contents.gameObject.SetActive(false);
                if (nestedCategory.CollapseImage != null && nestedCategory.NotVisibleImage != null)
                {
                    nestedCategory.CollapseImage.sprite = nestedCategory.NotVisibleImage;
                }
                
                // Update animator state
                var animator = nestedCategory.GetComponent<IconAnimator>();
                if (animator != null)
                {
                    animator.Initialize(false);
                }
                
                // Add to page's category list for cleanup
                page.CreatedCategories.Add(nestedCategory);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating unified section: {ex.Message}");
            }
        }

        /// <summary>
        /// Unified inline content renderer - for non-collapsible content
        /// </summary>
        private static void CreateUnifiedInlineContent(
            RectTransform parent,
            TMPro.TextMeshProUGUI sourceText,
            OperationalDetail detail,
            StationpediaCategory categoryPrefab,
            UniversalPage page,
            int depth,
            string parentTocId,
            Action<RectTransform, OperationalDetail, int, string> recurseCallback)
        {
            // Create container for inline content
            var containerGO = new GameObject($"InlineContent_{detail.title ?? "text"}");
            containerGO.transform.SetParent(parent, false);
            
            var containerRT = containerGO.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0, 1);
            containerRT.anchorMax = new Vector2(1, 1);
            containerRT.pivot = new Vector2(0.5f, 1);
            
            var containerLayout = containerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            containerLayout.spacing = 5;
            
            var containerFitter = containerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            containerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            containerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            // Register for TOC navigation if has tocId
            if (!string.IsNullOrEmpty(detail.tocId))
            {
                TocLinkHandler.RegisterSection(detail.tocId, containerRT, null, parentTocId);
            }
            
            // Add title if present (as styled header text)
            if (!string.IsNullOrEmpty(detail.title))
            {
                string titleColor = VanillaModeManager.GetTitleColor(depth);
                CreateTextElement(containerRT, sourceText, $"<b><color={titleColor}>{detail.title}</color></b>");
            }
            
            // Add image if specified
            if (!string.IsNullOrEmpty(detail.imageFile))
            {
                CreateInlineImage(containerRT, detail.imageFile);
            }
            
            // Add description
            if (!string.IsNullOrEmpty(detail.description))
            {
                CreateTextElement(containerRT, sourceText, detail.description);
            }
            
            // Add bullet items
            if (detail.items != null && detail.items.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var item in detail.items)
                {
                    sb.AppendLine($"  • {item}");
                }
                CreateTextElement(containerRT, sourceText, sb.ToString().TrimEnd());
            }
            
            // Add numbered steps
            if (detail.steps != null && detail.steps.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                int stepNum = 1;
                string stepColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
                foreach (var step in detail.steps)
                {
                    sb.AppendLine($"  <color={stepColor}>{stepNum}.</color> {step}");
                    stepNum++;
                }
                CreateTextElement(containerRT, sourceText, sb.ToString().TrimEnd());
            }
            
            // Add YouTube link if specified
            if (!string.IsNullOrEmpty(detail.youtubeUrl))
            {
                CreateYouTubeLink(containerRT, sourceText, detail.youtubeUrl, detail.youtubeLabel);
            }
            
            // Add inline video if specified
            if (!string.IsNullOrEmpty(detail.videoFile))
            {
                CreateInlineVideo(containerRT, detail.videoFile);
            }
            
            // Add table if specified
            if (detail.table != null && detail.table.Count > 0)
            {
                CreateTableElement(containerRT, sourceText, detail.table);
            }
            
            // Render children
            if (detail.children != null && recurseCallback != null)
            {
                foreach (var child in detail.children)
                {
                    recurseCallback(containerRT, child, depth + 1, detail.tocId);
                }
            }
        }

        /// <summary>
        /// Render all operational details - creates nested collapsible categories or inline text
        /// </summary>
        private static void RenderAllOperationalDetails(StationpediaCategory parentCategory, TMPro.TextMeshProUGUI sourceText, 
            DeviceDescriptions deviceDesc, StationpediaCategory categoryPrefab, UniversalPage page)
        {
            foreach (var detail in deviceDesc.operationalDetails)
            {
                RenderOperationalDetailElement(parentCategory, sourceText, detail, categoryPrefab, page, 0);
            }
        }

        /// <summary>
        /// Render a single operational detail element - now uses unified section renderer
        /// </summary>
        private static void RenderOperationalDetailElement(StationpediaCategory parentCategory, TMPro.TextMeshProUGUI sourceText,
            OperationalDetail detail, StationpediaCategory categoryPrefab, UniversalPage page, int depth, string parentTocId = null)
        {
            // Recursive callback for children
            Action<RectTransform, OperationalDetail, int, string> recurse = (p, d, dep, pid) => 
            {
                // For devices, we need to find or use the parent category
                // Since we're in a StationpediaCategory context, we use its Contents
                var parentCat = p.GetComponentInParent<StationpediaCategory>();
                if (parentCat != null)
                {
                    RenderOperationalDetailElement(parentCat, sourceText, d, categoryPrefab, page, dep, pid);
                }
                else
                {
                    // Fallback: render as guide section element
                    RenderGuideSectionElement(p, sourceText, d, categoryPrefab, page, dep, pid);
                }
            };
            
            if (detail.collapsible && !string.IsNullOrEmpty(detail.title))
            {
                // Use unified collapsible section
                CreateUnifiedCollapsibleSection(parentCategory.Contents, sourceText, detail, categoryPrefab, page, depth, parentTocId, recurse);
            }
            else
            {
                // Use unified inline content
                CreateUnifiedInlineContent(parentCategory.Contents, sourceText, detail, categoryPrefab, page, depth, parentTocId, recurse);
            }
        }

        /// <summary>
        /// DEPRECATED - Use CreateUnifiedCollapsibleSection instead
        /// Create a nested collapsible category within parent's Contents
        /// </summary>
        private static void CreateNestedCollapsibleCategory(StationpediaCategory parentCategory, TMPro.TextMeshProUGUI sourceText,
            OperationalDetail detail, StationpediaCategory categoryPrefab, UniversalPage page, int depth, string parentTocId = null)
        {
            try
            {
                // Create nested category
                var nestedCategory = UnityEngine.Object.Instantiate<StationpediaCategory>(categoryPrefab, parentCategory.Contents);
                if (nestedCategory == null) return;
                
                string safeTocId = detail.tocId ?? detail.title?.Replace(" ", "_") ?? $"section_{depth}";
                nestedCategory.gameObject.name = $"NestedCategory_{safeTocId}";
                
                // Set title with depth-based coloring (uses VanillaModeManager)
                string titleColor = VanillaModeManager.GetTitleColor(depth);
                nestedCategory.Title.text = $"<color={titleColor}>{detail.title}</color>";
                
                // Apply custom icons (respects vanilla mode)
                ApplyCustomCategoryIcons(nestedCategory);
                
                // Configure layout (respects vanilla mode) - pass depth to skip backgrounds for deep nesting
                ConfigureNestedCategoryLayout(nestedCategory, detail, depth);
                
                // Register for TOC navigation - include parent tocId for hierarchy expansion
                if (!string.IsNullOrEmpty(detail.tocId))
                {
                    TocLinkHandler.RegisterSection(detail.tocId, nestedCategory.GetComponent<RectTransform>(), nestedCategory, parentTocId);
                }
                
                // Add image if specified (before text content)
                if (!string.IsNullOrEmpty(detail.imageFile))
                {
                    CreateInlineImage(nestedCategory.Contents, detail.imageFile);
                }
                
                // Add description text
                if (!string.IsNullOrEmpty(detail.description))
                {
                    CreateTextElement(nestedCategory.Contents, sourceText, detail.description);
                }
                
                // Add bullet items
                if (detail.items != null && detail.items.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var item in detail.items)
                    {
                        sb.AppendLine($"  • {item}");
                    }
                    CreateTextElement(nestedCategory.Contents, sourceText, sb.ToString().TrimEnd());
                }
                
                // Add numbered steps
                if (detail.steps != null && detail.steps.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    int stepNum = 1;
                    // Step number color - white in vanilla, orange in ascended
                    string stepColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
                    foreach (var step in detail.steps)
                    {
                        sb.AppendLine($"  <color={stepColor}>{stepNum}.</color> {step}");
                        stepNum++;
                    }
                    CreateTextElement(nestedCategory.Contents, sourceText, sb.ToString().TrimEnd());
                }
                
                // Add YouTube link if specified
                if (!string.IsNullOrEmpty(detail.youtubeUrl))
                {
                    CreateYouTubeLink(nestedCategory.Contents, sourceText, detail.youtubeUrl, detail.youtubeLabel);
                }
                
                // Add inline video if specified
                if (!string.IsNullOrEmpty(detail.videoFile))
                {
                    CreateInlineVideo(nestedCategory.Contents, detail.videoFile);
                }
                
                // Recursively render children - pass current tocId as parent for TOC hierarchy
                if (detail.children != null)
                {
                    foreach (var child in detail.children)
                    {
                        RenderOperationalDetailElement(nestedCategory, sourceText, child, categoryPrefab, page, depth + 1, detail.tocId);
                    }
                }
                
                // Default to collapsed
                nestedCategory.Contents.gameObject.SetActive(false);
                if (nestedCategory.CollapseImage != null && nestedCategory.NotVisibleImage != null)
                {
                    nestedCategory.CollapseImage.sprite = nestedCategory.NotVisibleImage;
                }
                
                // Update animator state
                var animator = nestedCategory.GetComponent<IconAnimator>();
                if (animator != null)
                {
                    animator.Initialize(false);
                }
                
                // Add to page's created categories for cleanup
                page.CreatedCategories.Add(nestedCategory);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating nested category: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure layout for nested collapsible categories
        /// </summary>
        private static void ConfigureNestedCategoryLayout(StationpediaCategory category, OperationalDetail detail, int depth = 0)
        {
            // Remove GridLayoutGroup if present
            var existingGridLayout = category.Contents.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (existingGridLayout != null)
            {
                UnityEngine.Object.DestroyImmediate(existingGridLayout);
            }
            
            // Remove the old border layer approach - it doesn't work well
            var oldBorderLayer = category.Contents.gameObject.transform.Find("BorderLayer");
            if (oldBorderLayer != null)
            {
                UnityEngine.Object.DestroyImmediate(oldBorderLayer.gameObject);
            }
            
            // Only add background for first level (depth 0 only)
            // Depth 1+ (like Coal, Charcoal under Fuel Types) won't have backgrounds
            if (depth == 0)
            {
                // Get or add background Image
                var bgImage = category.Contents.gameObject.GetComponent<UnityEngine.UI.Image>();
                if (bgImage == null)
                {
                    bgImage = category.Contents.gameObject.AddComponent<UnityEngine.UI.Image>();
                }
                
                if (VanillaModeManager.IsVanillaMode)
                {
                    // Vanilla mode - use native sprite with no tint (grey border like Mode section)
                    if (_nativePanelSprite != null)
                    {
                        bgImage.sprite = _nativePanelSprite;
                        bgImage.type = _nativePanelType;
                        bgImage.material = _nativePanelMaterial;
                    }
                    bgImage.color = Color.white;  // No tint - let native sprite show through
                }
                else
                {
                    // Ascended mode - use fancy window background with color tint
                    Color bgColor = VanillaModeManager.GetBackgroundColor(detail.backgroundColor);
                    
                    var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
                    if (windowSprite != null)
                    {
                        bgImage.sprite = windowSprite;
                        bgImage.type = UnityEngine.UI.Image.Type.Sliced;
                        bgImage.material = null;
                    }
                    bgImage.color = bgColor;
                }
            }
            else
            {
                // Depth 1+ - remove any existing background image
                var existingBgImage = category.Contents.gameObject.GetComponent<UnityEngine.UI.Image>();
                if (existingBgImage != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingBgImage);
                }
            }
            
            // Add VerticalLayoutGroup with increased padding for more room around text
            var layout = category.Contents.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = category.Contents.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            }
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.spacing = 8;
            layout.padding = new RectOffset(16, 16, 14, 14);
            
            // Add ContentSizeFitter
            var fitter = category.Contents.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = category.Contents.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            }
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>
        /// Create inline content (non-collapsible text, items, steps)
        /// </summary>
        private static void CreateInlineContent(RectTransform parent, TMPro.TextMeshProUGUI sourceText, OperationalDetail detail, int depth)
        {
            string indent = depth > 0 ? $"<indent={depth * 5}%>" : "";
            string indentEnd = depth > 0 ? "</indent>" : "";
            // Use VanillaModeManager for title color
            string titleColor = VanillaModeManager.GetTitleColor(depth);
            // Step number color - white in vanilla, orange in ascended
            string stepColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
            
            var sb = new System.Text.StringBuilder();
            
            // Add title
            if (!string.IsNullOrEmpty(detail.title))
            {
                sb.Append(indent);
                sb.AppendLine($"<color={titleColor}><b>{detail.title}</b></color>");
                sb.Append(indentEnd);
            }
            
            // Add description
            if (!string.IsNullOrEmpty(detail.description))
            {
                sb.Append(indent);
                sb.AppendLine(detail.description);
                sb.Append(indentEnd);
            }
            
            // Add bullet items
            if (detail.items != null && detail.items.Count > 0)
            {
                foreach (var item in detail.items)
                {
                    sb.Append(indent);
                    sb.AppendLine($"  • {item}");
                    sb.Append(indentEnd);
                }
            }
            
            // Add numbered steps
            if (detail.steps != null && detail.steps.Count > 0)
            {
                int stepNum = 1;
                foreach (var step in detail.steps)
                {
                    sb.Append(indent);
                    sb.AppendLine($"  <color={stepColor}>{stepNum}.</color> {step}");
                    sb.Append(indentEnd);
                    stepNum++;
                }
            }
            
            if (sb.Length > 0)
            {
                CreateTextElement(parent, sourceText, sb.ToString().TrimEnd());
            }
            
            // Add image if specified
            if (!string.IsNullOrEmpty(detail.imageFile))
            {
                CreateInlineImage(parent, detail.imageFile);
            }
            
            // Add video if specified
            if (!string.IsNullOrEmpty(detail.videoFile))
            {
                CreateInlineVideo(parent, detail.videoFile);
            }
        }

        /// <summary>
        /// Create a text element inside a parent container
        /// Processes game format tags like {HEADER:Text}, {LINK:Page;Text}, {THING:Key}
        /// </summary>
        private static void CreateTextElement(RectTransform parent, TMPro.TextMeshProUGUI sourceText, string text)
        {
            // Process {LINK:}, {THING:}, {GAS:}, etc. tags through Localization.ParseHelpText
            // This handles device/page links, gas links, reagent links, etc.
            text = Localization.ParseHelpText(text);
            
            // Process {HEADER:Text} tags - convert to styled headers (after ParseHelpText)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\{HEADER:([^}]+)\}", match =>
            {
                string headerText = match.Groups[1].Value;
                // Use orange color and bold for headers, matching vanilla style
                return $"<b><color=#FF7A18>{headerText}</color></b>";
            });
            
            var textGO = new GameObject("DetailText");
            textGO.transform.SetParent(parent, false);
            
            var textComponent = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            textComponent.font = sourceText.font;
            textComponent.fontSharedMaterial = sourceText.fontSharedMaterial;
            textComponent.fontSize = sourceText.fontSize;
            textComponent.color = sourceText.color;
            textComponent.enableWordWrapping = true;
            textComponent.overflowMode = TMPro.TextOverflowModes.Overflow;
            textComponent.richText = true;
            textComponent.lineSpacing = sourceText.lineSpacing;
            textComponent.margin = new Vector4(5, 5, 5, 5);
            textComponent.text = text;
            
            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            
            var fitter = textGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            
            // Add link handler to enable clicking on {LINK:PageKey;Text} links
            var linkHandler = textGO.AddComponent<TocLinkHandler>();
            linkHandler.TextComponent = textComponent;
        }

        /// <summary>
        /// Create an inline image element
        /// </summary>
        private static void CreateInlineImage(RectTransform parent, string imageFile)
        {
            try
            {
                var sprite = StationpediaAscendedMod.LoadImageFromModFolder(imageFile);
                if (sprite == null)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Image not found: {imageFile}");
                    return;
                }
                
                var imageGO = new GameObject("InlineImage");
                imageGO.transform.SetParent(parent, false);
                
                var image = imageGO.AddComponent<UnityEngine.UI.Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                
                var rectTransform = imageGO.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1);
                
                // Calculate size based on image aspect ratio (smaller to match vanilla)
                float maxWidth = 100f;
                float aspectRatio = (float)sprite.texture.width / sprite.texture.height;
                float height = maxWidth / aspectRatio;
                
                // Use LayoutElement to control size
                var layoutElement = imageGO.AddComponent<UnityEngine.UI.LayoutElement>();
                layoutElement.preferredWidth = maxWidth;
                layoutElement.preferredHeight = height;
                layoutElement.flexibleWidth = 0;
                layoutElement.flexibleHeight = 0;
                
                rectTransform.sizeDelta = new Vector2(maxWidth, height);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating inline image: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a clickable YouTube link that opens in the system browser
        /// </summary>
        private static void CreateYouTubeLink(RectTransform parent, TMPro.TextMeshProUGUI sourceText, string youtubeUrl, string customLabel = null)
        {
            try
            {
                string label = string.IsNullOrEmpty(customLabel) ? "Watch on YouTube" : customLabel;
                
                var linkGO = new GameObject("YouTubeLink");
                linkGO.transform.SetParent(parent, false);
                
                // Create background for the button
                var bgImage = linkGO.AddComponent<UnityEngine.UI.Image>();
                bgImage.color = new Color(0.8f, 0.1f, 0.1f, 0.9f); // YouTube red
                
                // Use rounded sprite if available
                var roundedSprite = StationpediaAscendedMod.GetRoundedBackgroundSprite();
                if (roundedSprite != null)
                {
                    bgImage.sprite = roundedSprite;
                    bgImage.type = UnityEngine.UI.Image.Type.Sliced;
                }
                
                // Add horizontal layout for icon and text
                var layout = linkGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                layout.padding = new RectOffset(12, 12, 8, 8);
                layout.spacing = 8;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childAlignment = UnityEngine.TextAnchor.MiddleLeft;
                
                // Create text label
                var textGO = new GameObject("LinkText");
                textGO.transform.SetParent(linkGO.transform, false);
                var text = textGO.AddComponent<TMPro.TextMeshProUGUI>();
                text.font = sourceText.font;
                text.fontSharedMaterial = sourceText.fontSharedMaterial;
                text.fontSize = sourceText.fontSize;
                text.color = Color.white;
                text.text = label;
                text.enableWordWrapping = false;
                
                var textFitter = textGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                textFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                textFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Auto-size the button
                var buttonFitter = linkGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                buttonFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                buttonFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Add button behavior
                var button = linkGO.AddComponent<UnityEngine.UI.Button>();
                var colors = button.colors;
                colors.normalColor = new Color(0.8f, 0.1f, 0.1f, 0.9f);
                colors.highlightedColor = new Color(1f, 0.2f, 0.2f, 1f);
                colors.pressedColor = new Color(0.6f, 0.05f, 0.05f, 1f);
                button.colors = colors;
                button.targetGraphic = bgImage;
                
                // Add click handler
                button.onClick.AddListener(() => {
                    Application.OpenURL(youtubeUrl);
                });
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating YouTube link: {ex.Message}");
            }
        }

        /// <summary>
        /// Create an inline video player for local video files
        /// </summary>
        private static void CreateInlineVideo(RectTransform parent, string videoFile)
        {
            try
            {
                // Get the full path to the video file
                string videoPath = StationpediaAscendedMod.GetImageFilePath(videoFile);
                if (string.IsNullOrEmpty(videoPath))
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Video file not found: {videoFile}");
                    return;
                }
                
                // Video dimensions (16:9 aspect ratio) - larger size
                float videoWidth = 400f;
                float videoHeight = 225f;
                float borderPadding = 6f;
                float buttonHeight = 28f;
                float buttonSpacing = 6f;
                
                // Total container height: video + border + spacing + button
                float totalHeight = videoHeight + borderPadding * 2 + buttonSpacing + buttonHeight;
                float totalWidth = videoWidth + borderPadding * 2;
                
                // Create outer container (holds video frame + button below)
                var outerContainerGO = new GameObject("VideoOuterContainer");
                outerContainerGO.transform.SetParent(parent, false);
                
                var outerRT = outerContainerGO.GetComponent<RectTransform>();
                if (outerRT == null) outerRT = outerContainerGO.AddComponent<RectTransform>();
                
                // Add LayoutElement to control size in parent layout
                var outerLayout = outerContainerGO.AddComponent<UnityEngine.UI.LayoutElement>();
                outerLayout.preferredWidth = totalWidth;
                outerLayout.preferredHeight = totalHeight;
                outerLayout.flexibleWidth = 0;
                outerLayout.flexibleHeight = 0;
                
                // Add vertical layout to stack video frame and button
                var vertLayout = outerContainerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                vertLayout.spacing = buttonSpacing;
                vertLayout.childForceExpandWidth = false;
                vertLayout.childForceExpandHeight = false;
                vertLayout.childControlWidth = false;
                vertLayout.childControlHeight = false;
                vertLayout.childAlignment = UnityEngine.TextAnchor.UpperCenter;
                
                // Create video frame container (border + video)
                var frameContainerGO = new GameObject("VideoFrameContainer");
                frameContainerGO.transform.SetParent(outerContainerGO.transform, false);
                
                var frameRT = frameContainerGO.GetComponent<RectTransform>();
                if (frameRT == null) frameRT = frameContainerGO.AddComponent<RectTransform>();
                frameRT.sizeDelta = new Vector2(totalWidth, videoHeight + borderPadding * 2);
                
                // Add frame layout element
                var frameLayout = frameContainerGO.AddComponent<UnityEngine.UI.LayoutElement>();
                frameLayout.preferredWidth = totalWidth;
                frameLayout.preferredHeight = videoHeight + borderPadding * 2;
                
                // Add border/frame background using native sprite for proper borders
                var frameImage = frameContainerGO.AddComponent<UnityEngine.UI.Image>();
                if (_nativePanelSprite != null)
                {
                    frameImage.sprite = _nativePanelSprite;
                    frameImage.type = _nativePanelType;
                    frameImage.material = _nativePanelMaterial;
                }
                else
                {
                    var roundedSprite = StationpediaAscendedMod.GetRoundedBackgroundSprite();
                    if (roundedSprite != null)
                    {
                        frameImage.sprite = roundedSprite;
                        frameImage.type = UnityEngine.UI.Image.Type.Sliced;
                    }
                }
                frameImage.color = new Color(0.02f, 0.04f, 0.08f, 1f); // Very dark blue-black
                
                // Create video container inside frame
                var videoContainerGO = new GameObject("VideoContainer");
                videoContainerGO.transform.SetParent(frameContainerGO.transform, false);
                
                var containerRT = videoContainerGO.GetComponent<RectTransform>();
                if (containerRT == null) containerRT = videoContainerGO.AddComponent<RectTransform>();
                containerRT.anchorMin = new Vector2(0.5f, 0.5f);
                containerRT.anchorMax = new Vector2(0.5f, 0.5f);
                containerRT.pivot = new Vector2(0.5f, 0.5f);
                containerRT.sizeDelta = new Vector2(videoWidth, videoHeight);
                
                // Create RenderTexture for video output
                var renderTexture = new RenderTexture((int)videoWidth * 2, (int)videoHeight * 2, 0);
                renderTexture.Create();
                
                // Create RawImage to display the video
                var rawImage = videoContainerGO.AddComponent<UnityEngine.UI.RawImage>();
                rawImage.texture = renderTexture;
                rawImage.color = Color.white;
                
                // Add VideoPlayer component
                var videoPlayer = videoContainerGO.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = true;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = renderTexture;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct; // Enable audio playback
                videoPlayer.url = "file://" + videoPath.Replace("\\", "/");
                videoPlayer.skipOnDrop = true;
                
                // Set volume (0-1 range)
                videoPlayer.SetDirectAudioVolume(0, 0.5f); // 50% volume on track 0
                
                // Prepare video to show first frame
                videoPlayer.prepareCompleted += (source) => {
                    videoPlayer.frame = 0;
                };
                videoPlayer.Prepare();
                
                // Create play button BELOW the video frame
                var playButtonGO = new GameObject("PlayButton");
                playButtonGO.transform.SetParent(outerContainerGO.transform, false);
                
                var playButtonRT = playButtonGO.GetComponent<RectTransform>();
                if (playButtonRT == null) playButtonRT = playButtonGO.AddComponent<RectTransform>();
                playButtonRT.sizeDelta = new Vector2(80, buttonHeight);
                
                // Add layout element for button
                var buttonLayoutElem = playButtonGO.AddComponent<UnityEngine.UI.LayoutElement>();
                buttonLayoutElem.preferredWidth = 80;
                buttonLayoutElem.preferredHeight = buttonHeight;
                
                var playButtonBg = playButtonGO.AddComponent<UnityEngine.UI.Image>();
                if (_nativePanelSprite != null)
                {
                    playButtonBg.sprite = _nativePanelSprite;
                    playButtonBg.type = _nativePanelType;
                }
                else
                {
                    var roundedSprite = StationpediaAscendedMod.GetRoundedBackgroundSprite();
                    if (roundedSprite != null)
                    {
                        playButtonBg.sprite = roundedSprite;
                        playButtonBg.type = UnityEngine.UI.Image.Type.Sliced;
                    }
                }
                playButtonBg.color = new Color(0.1f, 0.2f, 0.35f, 1f); // Blue button
                
                // Add play symbol text
                var playSymbolGO = new GameObject("PlaySymbol");
                playSymbolGO.transform.SetParent(playButtonGO.transform, false);
                
                var playSymbolRT = playSymbolGO.GetComponent<RectTransform>();
                if (playSymbolRT == null) playSymbolRT = playSymbolGO.AddComponent<RectTransform>();
                playSymbolRT.anchorMin = Vector2.zero;
                playSymbolRT.anchorMax = Vector2.one;
                playSymbolRT.offsetMin = Vector2.zero;
                playSymbolRT.offsetMax = Vector2.zero;
                
                var playText = playSymbolGO.AddComponent<TMPro.TextMeshProUGUI>();
                playText.text = "PLAY";
                playText.fontSize = 14;
                playText.color = Color.white;
                playText.alignment = TMPro.TextAlignmentOptions.Center;
                playText.enableWordWrapping = false;
                
                // Add button component
                var playButton = playButtonGO.AddComponent<UnityEngine.UI.Button>();
                playButton.targetGraphic = playButtonBg;
                
                var buttonColors = playButton.colors;
                buttonColors.normalColor = new Color(0.1f, 0.2f, 0.35f, 1f);
                buttonColors.highlightedColor = new Color(0.15f, 0.3f, 0.5f, 1f);
                buttonColors.pressedColor = new Color(0.05f, 0.15f, 0.25f, 1f);
                playButton.colors = buttonColors;
                
                // Track play state
                bool isPlaying = false;
                
                playButton.onClick.AddListener(() => {
                    if (isPlaying)
                    {
                        videoPlayer.Pause();
                        playText.text = "PLAY";
                        playButtonBg.color = new Color(0.1f, 0.2f, 0.35f, 1f);
                        isPlaying = false;
                    }
                    else
                    {
                        videoPlayer.Play();
                        playText.text = "STOP";
                        playButtonBg.color = new Color(0.1f, 0.4f, 0.2f, 1f); // Green when playing
                        isPlaying = true;
                    }
                });
                
                ConsoleWindow.Print($"[Stationpedia Ascended] Video player created for: {videoFile}");
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating inline video: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a markdown-style table element with header row and data rows.
        /// First row is treated as headers (bold), all cells are center-aligned.
        /// Uses nested section background styling.
        /// </summary>
        private static void CreateTableElement(RectTransform parent, TMPro.TextMeshProUGUI sourceText, List<TableRow> tableData)
        {
            try
            {
                if (tableData == null || tableData.Count == 0) return;
                
                // Determine column count from first row
                int columnCount = tableData[0].cells?.Count ?? 0;
                if (columnCount == 0) return;
                
                // Create outer container with background (like nested sections)
                var tableContainerGO = new GameObject("TableContainer");
                tableContainerGO.transform.SetParent(parent, false);
                
                var containerRT = tableContainerGO.AddComponent<RectTransform>();
                containerRT.anchorMin = new Vector2(0, 1);
                containerRT.anchorMax = new Vector2(1, 1);
                containerRT.pivot = new Vector2(0.5f, 1);
                
                // Add background image (like nested sections)
                var bgImage = tableContainerGO.AddComponent<UnityEngine.UI.Image>();
                if (VanillaModeManager.IsVanillaMode)
                {
                    if (_nativePanelSprite != null)
                    {
                        bgImage.sprite = _nativePanelSprite;
                        bgImage.type = _nativePanelType;
                        bgImage.material = _nativePanelMaterial;
                    }
                    bgImage.color = Color.white;
                }
                else
                {
                    var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
                    if (windowSprite != null)
                    {
                        bgImage.sprite = windowSprite;
                        bgImage.type = UnityEngine.UI.Image.Type.Sliced;
                    }
                    bgImage.color = StationeersBlue;
                }
                
                // Add vertical layout for rows
                var containerLayout = tableContainerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                containerLayout.padding = new RectOffset(12, 12, 10, 10);
                containerLayout.spacing = 4;
                containerLayout.childForceExpandWidth = true;
                containerLayout.childForceExpandHeight = false;
                containerLayout.childControlWidth = true;
                containerLayout.childControlHeight = true;
                
                // Auto-size the container
                var containerFitter = tableContainerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                containerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                containerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Create each row
                for (int rowIndex = 0; rowIndex < tableData.Count; rowIndex++)
                {
                    var rowData = tableData[rowIndex];
                    if (rowData?.cells == null) continue;
                    
                    bool isHeader = (rowIndex == 0);
                    
                    // Create row container
                    var rowGO = new GameObject($"TableRow_{rowIndex}");
                    rowGO.transform.SetParent(tableContainerGO.transform, false);
                    
                    // Add horizontal layout for cells
                    var rowLayout = rowGO.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    rowLayout.spacing = 8;
                    rowLayout.childForceExpandWidth = true;
                    rowLayout.childForceExpandHeight = false;
                    rowLayout.childControlWidth = true;
                    rowLayout.childControlHeight = true;
                    rowLayout.childAlignment = UnityEngine.TextAnchor.MiddleCenter;
                    
                    // Add row size fitter
                    var rowFitter = rowGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                    rowFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                    rowFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    
                    // Create cells (ensure we have the right column count)
                    for (int colIndex = 0; colIndex < columnCount; colIndex++)
                    {
                        string cellText = colIndex < rowData.cells.Count ? rowData.cells[colIndex] : "";
                        
                        // Create cell container
                        var cellGO = new GameObject($"Cell_{rowIndex}_{colIndex}");
                        cellGO.transform.SetParent(rowGO.transform, false);
                        
                        // Add layout element for flexible width distribution
                        var cellLayout = cellGO.AddComponent<UnityEngine.UI.LayoutElement>();
                        cellLayout.flexibleWidth = 1f;
                        cellLayout.minWidth = 50f;
                        
                        // Create text element
                        var cellTextComponent = cellGO.AddComponent<TMPro.TextMeshProUGUI>();
                        cellTextComponent.font = sourceText.font;
                        cellTextComponent.fontSharedMaterial = sourceText.fontSharedMaterial;
                        cellTextComponent.fontSize = sourceText.fontSize * 0.9f;
                        cellTextComponent.color = sourceText.color;
                        cellTextComponent.enableWordWrapping = true;
                        cellTextComponent.alignment = TMPro.TextAlignmentOptions.Center;
                        cellTextComponent.richText = true;
                        cellTextComponent.margin = new Vector4(4, 4, 4, 4);
                        
                        // Header row is bold
                        if (isHeader)
                        {
                            string headerColor = VanillaModeManager.IsVanillaMode ? "#FFFFFF" : "#FFA500";
                            cellTextComponent.text = $"<b><color={headerColor}>{cellText}</color></b>";
                        }
                        else
                        {
                            cellTextComponent.text = cellText;
                        }
                        
                        // Auto-size cell text
                        var cellFitter = cellGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                        cellFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                        cellFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    }
                    
                    // Add separator line after header row
                    if (isHeader && tableData.Count > 1)
                    {
                        var separatorGO = new GameObject("HeaderSeparator");
                        separatorGO.transform.SetParent(tableContainerGO.transform, false);
                        
                        var separatorImage = separatorGO.AddComponent<UnityEngine.UI.Image>();
                        separatorImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        
                        var separatorLayout = separatorGO.AddComponent<UnityEngine.UI.LayoutElement>();
                        separatorLayout.preferredHeight = 1f;
                        separatorLayout.flexibleWidth = 1f;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating table: {ex.Message}");
            }
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
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Could not add icon to category: {ex.Message}");
            }
        }

        private static void ConfigureCategoryLayout(StationpediaCategory category, UniversalPage page, DeviceDescriptions deviceDesc = null)
        {
            // The Contents has a GridLayoutGroup by default which forces fixed cell sizes
            // We need to DESTROY it (not just disable) so we can add a VerticalLayoutGroup
            var existingGridLayout = category.Contents.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (existingGridLayout != null)
            {
                UnityEngine.Object.DestroyImmediate(existingGridLayout);
            }
            
            // Set up background Image
            var bgImage = category.Contents.gameObject.GetComponent<UnityEngine.UI.Image>();
            var sourceImage = page.LogicSlotContents?.Contents?.GetComponent<UnityEngine.UI.Image>();
            if (sourceImage != null)
            {
                // Cache the native sprite for use in nested categories
                if (_nativePanelSprite == null)
                {
                    _nativePanelSprite = sourceImage.sprite;
                    _nativePanelMaterial = sourceImage.material;
                    _nativePanelType = sourceImage.type;
                }
                
                if (bgImage == null)
                {
                    bgImage = category.Contents.gameObject.AddComponent<UnityEngine.UI.Image>();
                }
                
                if (VanillaModeManager.IsVanillaMode)
                {
                    // Vanilla mode - use native sprite with native styling (no color tint)
                    bgImage.sprite = sourceImage.sprite;
                    bgImage.type = sourceImage.type;
                    bgImage.material = sourceImage.material;
                    bgImage.color = Color.white;  // No tint - let native sprite show through
                }
                else
                {
                    // Ascended mode - use fancy sprite with custom color
                    var windowSprite = StationpediaAscendedMod.GetWindowBackgroundSprite();
                    if (windowSprite != null)
                    {
                        bgImage.sprite = windowSprite;
                        bgImage.type = UnityEngine.UI.Image.Type.Sliced;
                        bgImage.material = null;
                    }
                    else
                    {
                        bgImage.sprite = sourceImage.sprite;
                        bgImage.type = sourceImage.type;
                        bgImage.material = sourceImage.material;
                    }
                    
                    // Apply custom background color or use Stationeers blue
                    if (deviceDesc != null && !string.IsNullOrEmpty(deviceDesc.operationalDetailsBackgroundColor))
                    {
                        if (ColorUtility.TryParseHtmlString(deviceDesc.operationalDetailsBackgroundColor, out Color customColor))
                        {
                            bgImage.color = customColor;
                        }
                        else
                        {
                            bgImage.color = StationeersBlue;
                        }
                    }
                    else
                    {
                        bgImage.color = StationeersBlue;
                    }
                }
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
            category.Contents.gameObject.SetActive(true);;
            
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

        #region SetPage Patches

        /// <summary>
        /// Prefix patch for SetPage - handles custom pages like GameMechanics
        /// </summary>
        public static bool Stationpedia_SetPage_Prefix(Stationpedia __instance, string key, bool newPage)
        {
            // Log all page navigation for debugging
            StationpediaAscendedMod.Log?.LogInfo($"SetPage called: key={key}, newPage={newPage}, CurrentPageKey={Stationpedia.CurrentPageKey}");
            
            // Handle Game Mechanics page
            if (key == "GameMechanics")
            {
                try
                {
                    SetPageGameMechanics(__instance, newPage);
                    return false; // Skip original
                }
                catch (Exception ex)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Error in SetPageGameMechanics: {ex.Message}");
                }
            }
            
            return true; // Let original handle it
        }

        /// <summary>
        /// Handle the Game Mechanics listing page
        /// </summary>
        private static void SetPageGameMechanics(Stationpedia stationpedia, bool newPage)
        {
            // Update history properly - mirror vanilla behavior
            if (newPage && Stationpedia.CurrentPageKey != "GameMechanics")
            {
                // Access private fields via reflection
                var historyField = typeof(Stationpedia).GetField("_pageHistory", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var indexField = typeof(Stationpedia).GetField("_currentHistoryIndex", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (historyField != null && indexField != null)
                {
                    var history = historyField.GetValue(null) as System.Collections.Generic.List<string>;
                    int currentIndex = (int)indexField.GetValue(stationpedia);
                    
                    if (history != null)
                    {
                        // Remove any forward history
                        if (currentIndex < history.Count - 1)
                        {
                            int count = history.Count - 1 - currentIndex;
                            history.RemoveRange(currentIndex + 1, count);
                        }
                        // Add to history
                        history.Add("GameMechanics");
                        indexField.SetValue(stationpedia, history.Count - 1);
                    }
                }
                
                Stationpedia.CurrentPageKey = "GameMechanics";
            }
            
            // Hide other pages
            stationpedia.HomePage.SetActive(false);
            stationpedia.UniversalPageRef.gameObject.SetActive(false);
            stationpedia.LoreGuideHolder.gameObject.SetActive(true);
            
            // Populate with Game Mechanics pages
            PopulateGameMechanicsContents(stationpedia);
            
            // Set title
            stationpedia.LoreGuideTitle.SetText("Game Mechanics", true);
            
            // Update navigation
            stationpedia.HomePageButton.interactable = true;
            Stationpedia.UpdateNavigationInteractButtons();
        }

        /// <summary>
        /// Populate the LoreGuideContents with Game Mechanics pages
        /// </summary>
        private static void PopulateGameMechanicsContents(Stationpedia stationpedia)
        {
            // Clear existing items - also clear the private _SPDAGuideLoreInserts list to prevent
            // NullReferenceException when vanilla PopulateGuideLoreContents runs next time
            var guideLoreInsertsField = typeof(Stationpedia).GetField("_SPDAGuideLoreInserts", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var guideLoreInserts = guideLoreInsertsField?.GetValue(stationpedia) as System.Collections.IList;
            if (guideLoreInserts != null)
            {
                guideLoreInserts.Clear();
            }
            
            foreach (Transform child in stationpedia.LoreGuideContents)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
            
            // First register all mechanics pages so they exist in Stationpedia
            Data.JsonMechanicsLoader.RegisterMechanicsPages();
            
            // Get all loaded mechanics from JSON
            var mechanics = Data.JsonMechanicsLoader.GetAllMechanics();
            
            if (mechanics == null || mechanics.Count == 0)
            {
                ConsoleWindow.Print("[Stationpedia Ascended] No game mechanics pages to display");
                return;
            }
            
            // Create buttons for each mechanic
            foreach (var mechanic in mechanics)
            {
                string pageKey = mechanic.guideKey;
                
                StationpediaPage page;
                if (!Stationpedia.StationpediaPages.Exists(p => p.Key == pageKey))
                    continue;
                    
                page = Stationpedia.StationpediaPages.Find(p => p.Key == pageKey);
                if (page == null) continue;
                
                var listItem = UnityEngine.Object.Instantiate(stationpedia.ListSearchPrefab, stationpedia.LoreGuideContents);
                listItem.Apply(mechanic.displayName ?? page.Title);
                
                // Capture for lambda
                string capturedKey = pageKey;
                listItem.InsertsButton.onClick.AddListener(() => {
                    stationpedia.SetPage(capturedKey, true);
                });
                
                // Use custom icon if available
                if (page.CustomSpriteToUse != null)
                {
                    listItem.InsertImage.sprite = page.CustomSpriteToUse;
                }
                else
                {
                    listItem.InsertImage.sprite = stationpedia.ImportantSearchImage;
                }
                listItem.SetSpecial();
            }
            
            // Modify layout to two columns
            HomePageLayoutManager.ModifyGuideLoreLayout(stationpedia.LoreGuideContents);
        }

        /// <summary>
        /// Postfix patch for SetPageGuides - modifies button layout to two columns
        /// </summary>
        public static void Stationpedia_SetPageGuides_Postfix(Stationpedia __instance)
        {
            try
            {
                // Create buttons for all JSON-defined guides (handles duplication check internally)
                CreateJsonGuideButtons(__instance);
                
                // Add the "Vanilla Guides" text header above vanilla guides
                AddVanillaGuidesHeader(__instance);
                
                // Modify layout to two columns
                HomePageLayoutManager.ModifyGuideLoreLayout(__instance.LoreGuideContents);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error modifying guide layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Create buttons for all guides defined in descriptions.json
        /// </summary>
        private static void CreateJsonGuideButtons(Stationpedia stationpedia)
        {
            try
            {
                var guides = Data.JsonGuideLoader.GetAllGuides();
                if (guides == null || guides.Count == 0)
                {
                    return;
                }

                // Register guide pages first
                Data.JsonGuideLoader.RegisterGuidePages();

                int createdCount = 0;
                foreach (var guide in guides)
                {
                    // Skip SurvivalManual - it has its own dedicated button on the home page
                    if (guide.guideKey == "SurvivalManual")
                        continue;
                    
                    // Check if button already exists in the hierarchy (more robust than static HashSet)
                    string buttonName = $"CustomGuide_{guide.guideKey}";
                    bool exists = false;
                    foreach (Transform child in stationpedia.LoreGuideContents)
                    {
                        if (child.gameObject.name == buttonName)
                        {
                            exists = true;
                            break;
                        }
                    }
                    
                    if (exists)
                        continue;
                        
                    CreateGuideButton(stationpedia, guide);
                    createdCount++;
                }

                if (createdCount > 0)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Created {createdCount} JSON guide buttons");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating JSON guide buttons: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a single guide button from a GuideDescription
        /// </summary>
        private static void CreateGuideButton(Stationpedia stationpedia, Data.GuideDescription guide)
        {
            try
            {
                if (stationpedia?.ListSearchPrefab == null || stationpedia.LoreGuideContents == null)
                    return;

                var button = UnityEngine.Object.Instantiate<SPDAListItem>(
                    stationpedia.ListSearchPrefab,
                    stationpedia.LoreGuideContents);

                if (button == null) return;

                // Mark this button as a custom guide (not vanilla) so it won't be wrapped
                button.gameObject.name = $"CustomGuide_{guide.guideKey}";

                // Set button text
                button.Apply(guide.displayName ?? guide.guideKey);

                // Set up click handler
                var guideKey = guide.guideKey; // Capture for closure
                if (button.InsertsButton != null)
                {
                    button.InsertsButton.onClick.RemoveAllListeners();
                    button.InsertsButton.onClick.AddListener(() =>
                    {
                        stationpedia.SetPage(guideKey, true);
                    });
                }

                // Use SetSpecial() to match vanilla guide button styling (light blue)
                // This uses the SpecialButton sprite which is the same as vanilla guides
                button.SetSpecial();

                // Set the icon image to ImportantSearchImage (star icon) like vanilla guides
                if (button.InsertImage != null && stationpedia.ImportantSearchImage != null)
                {
                    button.InsertImage.sprite = stationpedia.ImportantSearchImage;
                    button.InsertImage.gameObject.SetActive(true);
                }

                // Position at top
                button.transform.SetAsFirstSibling();
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error creating guide button for {guide?.guideKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a "Vanilla Guides" text header above the vanilla guides
        /// Instead of wrapping in a dropdown, just add a visual separator
        /// </summary>
        private static void AddVanillaGuidesHeader(Stationpedia stationpedia)
        {
            try
            {
                if (stationpedia == null || stationpedia.LoreGuideContents == null)
                {
                    return;
                }

                var loreContainer = stationpedia.LoreGuideContents;
                
                // Check if header already exists
                foreach (Transform child in loreContainer)
                {
                    if (child.gameObject.name == "VanillaGuidesHeader")
                    {
                        return; // Already has header
                    }
                }
                
                // Count custom guide buttons to know where to place the header
                int customGuideCount = 0;
                for (int i = 0; i < loreContainer.childCount; i++)
                {
                    var child = loreContainer.GetChild(i);
                    if (child.gameObject.name.StartsWith("CustomGuide_"))
                    {
                        customGuideCount++;
                    }
                }
                
                // If there are vanilla guides (not just our custom ones), add a header
                int vanillaCount = loreContainer.childCount - customGuideCount;
                if (vanillaCount > 0)
                {
                    // Create header using a new GameObject with TextMeshProUGUI
                    var headerObj = new GameObject("VanillaGuidesHeader");
                    headerObj.transform.SetParent(loreContainer, false);
                    
                    // Add RectTransform for layout
                    var rectTransform = headerObj.AddComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(0, 40);
                    
                    // Add LayoutElement to control sizing
                    var layoutElement = headerObj.AddComponent<UnityEngine.UI.LayoutElement>();
                    layoutElement.preferredHeight = 40;
                    layoutElement.flexibleWidth = 1;
                    
                    // Add TextMeshPro text component - use same font as LoreGuideTitle for consistency
                    var textComponent = headerObj.AddComponent<TMPro.TextMeshProUGUI>();
                    textComponent.text = "Vanilla Guides";
                    
                    // Copy font settings from LoreGuideTitle to match exactly
                    if (stationpedia.LoreGuideTitle != null)
                    {
                        textComponent.font = stationpedia.LoreGuideTitle.font;
                        textComponent.fontSharedMaterial = stationpedia.LoreGuideTitle.fontSharedMaterial;
                        textComponent.fontSize = stationpedia.LoreGuideTitle.fontSize;
                        textComponent.fontStyle = stationpedia.LoreGuideTitle.fontStyle;
                    }
                    else
                    {
                        textComponent.fontSize = 32;
                        textComponent.fontStyle = TMPro.FontStyles.Normal;
                    }
                    
                    textComponent.alignment = TMPro.TextAlignmentOptions.Center;
                    textComponent.color = new Color(1f, 1f, 1f, 1f); // White to match "Guides" title
                    
                    // Position the header after custom guides
                    headerObj.transform.SetSiblingIndex(customGuideCount);
                    
                    ConsoleWindow.Print($"[Stationpedia Ascended] Added Vanilla Guides header (Custom: {customGuideCount}, Vanilla: {vanillaCount})");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error adding vanilla guides header: {ex.Message}");
            }
        }

        /// <summary>
        /// Prefix patch for SetPageLore - clear orphaned items before vanilla repopulates
        /// When switching from Game Mechanics to Universe, our items remain because vanilla
        /// only clears items it tracks in _SPDAGuideLoreInserts. This clears everything first.
        /// </summary>
        public static void Stationpedia_SetPageLore_Prefix(Stationpedia __instance)
        {
            try
            {
                if (__instance?.LoreGuideContents == null) return;
                
                // Clear ALL children from LoreGuideContents before vanilla repopulates
                // This ensures our Game Mechanics buttons don't persist when switching to Universe
                var toRemove = new List<GameObject>();
                foreach (Transform child in __instance.LoreGuideContents)
                {
                    toRemove.Add(child.gameObject);
                }
                
                foreach (var obj in toRemove)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error in SetPageLore prefix: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix patch for SetPageLore - modifies button layout to two columns
        /// </summary>
        public static void Stationpedia_SetPageLore_Postfix(Stationpedia __instance)
        {
            try
            {
                HomePageLayoutManager.ModifyGuideLoreLayout(__instance.LoreGuideContents);
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error modifying lore layout: {ex.Message}");
            }
        }

        #endregion
    }
}
