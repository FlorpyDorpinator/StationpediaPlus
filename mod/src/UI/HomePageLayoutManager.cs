using System;
using System.Reflection;
using Assets.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StationpediaAscended.UI
{
    /// <summary>
    /// Manages modifications to the Stationpedia home page layout.
    /// Creates Survival Manual and Game Mechanics buttons styled like the Guides/Universe buttons.
    /// </summary>
    public static class HomePageLayoutManager
    {
        // Track created elements for cleanup
        private static GameObject _customButtonContainer = null;
        private static GameObject _survivalManualButton = null;
        private static GameObject _gameMechanicsButton = null;
        
        /// <summary>
        /// Initialize home page modifications - call when Stationpedia is found
        /// </summary>
        public static void Initialize()
        {
            try
            {
                var stationpedia = Stationpedia.Instance;
                if (stationpedia == null) return;
                
                // Find the _homeGuideButtonContainer through reflection (it's private)
                var guideContainerField = typeof(Stationpedia).GetField("_homeGuideButtonContainer", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (guideContainerField == null)
                {
                    StationpediaAscendedMod.Log?.LogWarning("Could not find _homeGuideButtonContainer field");
                    return;
                }
                
                var guideButtonContainer = guideContainerField.GetValue(stationpedia) as GameObject;
                if (guideButtonContainer == null)
                {
                    StationpediaAscendedMod.Log?.LogWarning("_homeGuideButtonContainer is null");
                    return;
                }
                
                // Get the parent of the guide button container
                var containerParent = guideButtonContainer.transform.parent;
                if (containerParent == null)
                {
                    StationpediaAscendedMod.Log?.LogWarning("Guide button container has no parent");
                    return;
                }
                
                // Check if our custom container already exists - find and destroy duplicates
                CleanupExistingButtons(containerParent, stationpedia.HomePageContent);
                
                // Find the buttons in the guide container
                // There should be two buttons: Guides (orange) and Universe/Lore (blue)
                Button guideButton = null;      // Orange button - for Survival Manual
                Button universeButton = null;   // Blue button - for Game Mechanics
                TextMeshProUGUI guideText = null;
                TextMeshProUGUI universeText = null;
                
                foreach (Transform child in guideButtonContainer.transform)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        var btnText = child.GetComponentInChildren<TextMeshProUGUI>();
                        // First button found becomes guideButton (Guides - orange)
                        // Second button found becomes universeButton (Universe/Lore - blue)
                        if (guideButton == null)
                        {
                            guideButton = btn;
                            guideText = btnText;
                        }
                        else if (universeButton == null)
                        {
                            universeButton = btn;
                            universeText = btnText;
                        }
                    }
                }
                
                if (guideButton == null)
                {
                    StationpediaAscendedMod.Log?.LogWarning("Could not find Guide button in guide container");
                    return;
                }
                
                // If no second button found, use the first one for both
                if (universeButton == null)
                {
                    universeButton = guideButton;
                    universeText = guideText;
                }
                
                // Create our custom button container - clone the structure of guideButtonContainer
                _customButtonContainer = new GameObject("CustomButtonContainer_SPA");
                _customButtonContainer.transform.SetParent(containerParent, false);
                
                // Position it BEFORE the guide container
                int guideIndex = guideButtonContainer.transform.GetSiblingIndex();
                _customButtonContainer.transform.SetSiblingIndex(guideIndex);
                
                // Copy the RectTransform from guide container
                var templateContainerRect = guideButtonContainer.GetComponent<RectTransform>();
                var customRect = _customButtonContainer.AddComponent<RectTransform>();
                customRect.anchorMin = templateContainerRect.anchorMin;
                customRect.anchorMax = templateContainerRect.anchorMax;
                customRect.pivot = templateContainerRect.pivot;
                customRect.sizeDelta = templateContainerRect.sizeDelta;
                customRect.anchoredPosition = templateContainerRect.anchoredPosition;
                
                // Get button height for container sizing
                var guideButtonRect = guideButton.GetComponent<RectTransform>();
                float buttonHeight = guideButtonRect != null ? guideButtonRect.sizeDelta.y : 108f;
                
                StationpediaAscendedMod.Log?.LogInfo($"Container template size: {templateContainerRect.sizeDelta}, Button height: {buttonHeight}");
                
                // Copy the layout group from guide container
                var templateLayout = guideButtonContainer.GetComponent<HorizontalLayoutGroup>();
                if (templateLayout != null)
                {
                    var customLayout = _customButtonContainer.AddComponent<HorizontalLayoutGroup>();
                    customLayout.spacing = templateLayout.spacing;
                    customLayout.padding = templateLayout.padding;
                    customLayout.childAlignment = templateLayout.childAlignment;
                    customLayout.childControlWidth = templateLayout.childControlWidth;
                    customLayout.childControlHeight = templateLayout.childControlHeight;
                    customLayout.childForceExpandWidth = templateLayout.childForceExpandWidth;
                    customLayout.childForceExpandHeight = templateLayout.childForceExpandHeight;
                    
                    StationpediaAscendedMod.Log?.LogInfo($"Container HLG: childControlHeight={templateLayout.childControlHeight}, childForceExpandHeight={templateLayout.childForceExpandHeight}");
                }
                else
                {
                    // Default to horizontal layout matching guide buttons
                    var defaultLayout = _customButtonContainer.AddComponent<HorizontalLayoutGroup>();
                    defaultLayout.spacing = 10;
                    defaultLayout.childAlignment = TextAnchor.MiddleCenter;
                    defaultLayout.childControlWidth = true;
                    defaultLayout.childControlHeight = false;  // Don't control height - let buttons determine their own
                    defaultLayout.childForceExpandWidth = true;
                    defaultLayout.childForceExpandHeight = false;
                }
                
                // Add layout element to match guide container - use actual button height!
                var templateLayoutElem = guideButtonContainer.GetComponent<LayoutElement>();
                if (templateLayoutElem != null)
                {
                    var customLayoutElem = _customButtonContainer.AddComponent<LayoutElement>();
                    customLayoutElem.preferredHeight = templateLayoutElem.preferredHeight;
                    customLayoutElem.flexibleWidth = templateLayoutElem.flexibleWidth;
                    StationpediaAscendedMod.Log?.LogInfo($"Container LE from template: preferredHeight={templateLayoutElem.preferredHeight}");
                }
                else
                {
                    // No LayoutElement - use button height for container
                    var customLayoutElem = _customButtonContainer.AddComponent<LayoutElement>();
                    customLayoutElem.preferredHeight = buttonHeight;  // Use actual button height, not hardcoded 50!
                    customLayoutElem.flexibleWidth = 1;
                    StationpediaAscendedMod.Log?.LogInfo($"Container LE created: preferredHeight={buttonHeight}");
                }
                
                // Create Survival Manual button (clone from Guide button - orange style)
                _survivalManualButton = CloneButton(guideButton.gameObject, "SurvivalManualButton_SPA", 
                    "Stationeers Survival Manual", OnSurvivalManualClick, guideText);
                _survivalManualButton.transform.SetParent(_customButtonContainer.transform, false);
                
                // Create Game Mechanics button (clone from Universe button - blue style)
                _gameMechanicsButton = CloneButton(universeButton.gameObject, "GameMechanicsButton_SPA", 
                    "Game Mechanics", OnGameMechanicsClick, universeText);
                _gameMechanicsButton.transform.SetParent(_customButtonContainer.transform, false);
                
                // Force layout rebuild - critical for hot-reload to work properly
                LayoutRebuilder.ForceRebuildLayoutImmediate(customRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerParent.GetComponent<RectTransform>());
                
                // Log debug info
                var guideRect = guideButton.GetComponent<RectTransform>();
                var survivalRect = _survivalManualButton.GetComponent<RectTransform>();
                StationpediaAscendedMod.Log?.LogInfo($"Guide button size: {guideRect?.sizeDelta}, Survival button size: {survivalRect?.sizeDelta}");
                
                var guideLE = guideButton.GetComponent<LayoutElement>();
                var survivalLE = _survivalManualButton.GetComponent<LayoutElement>();
                if (guideLE != null)
                    StationpediaAscendedMod.Log?.LogInfo($"Guide LE: pref={guideLE.preferredWidth}x{guideLE.preferredHeight}, flex={guideLE.flexibleWidth}x{guideLE.flexibleHeight}");
                if (survivalLE != null)
                    StationpediaAscendedMod.Log?.LogInfo($"Survival LE: pref={survivalLE.preferredWidth}x{survivalLE.preferredHeight}, flex={survivalLE.flexibleWidth}x{survivalLE.flexibleHeight}");
                
                StationpediaAscendedMod.Log?.LogInfo("Home page custom buttons created successfully");
            }
            catch (Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"Error initializing home page layout: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Cleanup any existing custom buttons to prevent duplicates
        /// </summary>
        private static void CleanupExistingButtons(Transform guideParent, Transform homePageContent)
        {
            // Find and destroy any existing custom containers in guide button area
            var toDestroy = new System.Collections.Generic.List<GameObject>();
            
            if (guideParent != null)
            {
                foreach (Transform child in guideParent)
                {
                    if (child.name.Contains("CustomButtonContainer"))
                    {
                        toDestroy.Add(child.gameObject);
                    }
                }
            }
            
            // Also clean up old-style buttons that were added to HomePageContent
            if (homePageContent != null)
            {
                foreach (Transform child in homePageContent)
                {
                    string name = child.name;
                    if (name == "SurvivalManualButton" || name == "GameMechanicsButton" ||
                        name.Contains("SurvivalManual") || name.Contains("GameMechanics"))
                    {
                        // Only remove if it's one of our custom buttons (not a vanilla category button)
                        var btn = child.GetComponent<Button>();
                        if (btn != null)
                        {
                            // Check if this has our custom text
                            var text = child.GetComponentInChildren<TextMeshProUGUI>();
                            if (text != null && (text.text.Contains("Survival Manual") || text.text.Contains("Game Mechanics")))
                            {
                                toDestroy.Add(child.gameObject);
                            }
                        }
                    }
                }
            }
            
            // Destroy all found duplicates
            foreach (var go in toDestroy)
            {
                StationpediaAscendedMod.Log?.LogInfo($"Removing duplicate button: {go.name}");
                UnityEngine.Object.DestroyImmediate(go);
            }
            
            // Clear our references if they were destroyed
            _customButtonContainer = null;
            _survivalManualButton = null;
            _gameMechanicsButton = null;
        }
        
        /// <summary>
        /// Clone a button from template with custom text and click handler
        /// </summary>
        private static GameObject CloneButton(GameObject template, string name, string buttonText, 
            UnityEngine.Events.UnityAction onClick, TextMeshProUGUI textTemplate)
        {
            // Clone the entire button
            var clone = UnityEngine.Object.Instantiate(template);
            clone.name = name;
            
            // Update the text
            var text = clone.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = buttonText;
                // Enable word wrapping and center alignment for long text
                text.enableWordWrapping = true;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.overflowMode = TMPro.TextOverflowModes.Truncate;
                
                // Keep the same font, size, and style as the template
                if (textTemplate != null)
                {
                    text.font = textTemplate.font;
                    text.fontSize = textTemplate.fontSize;
                    text.fontStyle = textTemplate.fontStyle;
                }
            }
            
            // CRITICAL: Replace the Button's onClick with a NEW UnityEvent to clear persistent listeners
            // RemoveAllListeners() only removes runtime listeners, not serialized/persistent ones!
            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                // Create a completely new onClick event to replace the cloned one
                // This removes any persistent listeners that were serialized from the prefab
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(onClick);
                
                // Clear navigation to prevent button linking issues
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
            }
            
            // Ensure RectTransform matches template exactly
            var templateRect = template.GetComponent<RectTransform>();
            var cloneRect = clone.GetComponent<RectTransform>();
            if (templateRect != null && cloneRect != null)
            {
                // Copy all RectTransform properties
                cloneRect.anchorMin = templateRect.anchorMin;
                cloneRect.anchorMax = templateRect.anchorMax;
                cloneRect.pivot = templateRect.pivot;
                cloneRect.sizeDelta = templateRect.sizeDelta;
                cloneRect.anchoredPosition = templateRect.anchoredPosition;
            }
            
            // Handle LayoutElement - if template has one, copy it; otherwise use RectTransform size
            var templateLE = template.GetComponent<LayoutElement>();
            var cloneLE = clone.GetComponent<LayoutElement>();
            if (templateLE != null)
            {
                if (cloneLE == null)
                {
                    cloneLE = clone.AddComponent<LayoutElement>();
                }
                cloneLE.minWidth = templateLE.minWidth;
                cloneLE.minHeight = templateLE.minHeight;
                cloneLE.preferredWidth = templateLE.preferredWidth;
                cloneLE.preferredHeight = templateLE.preferredHeight;
                cloneLE.flexibleWidth = templateLE.flexibleWidth;
                cloneLE.flexibleHeight = templateLE.flexibleHeight;
                cloneLE.layoutPriority = templateLE.layoutPriority;
            }
            else if (templateRect != null)
            {
                // No LayoutElement on template - use RectTransform size for preferred dimensions
                if (cloneLE == null)
                {
                    cloneLE = clone.AddComponent<LayoutElement>();
                }
                cloneLE.flexibleWidth = 1;
                // Use the actual template height, not a hardcoded value!
                cloneLE.preferredHeight = templateRect.sizeDelta.y;
                StationpediaAscendedMod.Log?.LogInfo($"Added LayoutElement with preferredHeight={templateRect.sizeDelta.y} from RectTransform");
            }
            
            return clone;
        }
        
        /// <summary>
        /// Click handler for Survival Manual button
        /// </summary>
        private static void OnSurvivalManualClick()
        {
            StationpediaAscendedMod.Log?.LogInfo($"Survival Manual clicked. CurrentPageKey: {Stationpedia.CurrentPageKey}");
            Stationpedia.Instance?.SetPage("SurvivalManual", true);
        }
        
        /// <summary>
        /// Click handler for Game Mechanics button
        /// </summary>
        private static void OnGameMechanicsClick()
        {
            StationpediaAscendedMod.Log?.LogInfo($"Game Mechanics clicked. CurrentPageKey: {Stationpedia.CurrentPageKey}");
            Stationpedia.Instance?.SetPage("GameMechanics", true);
        }
        
        /// <summary>
        /// Modify the Guide/Lore contents layout
        /// NOTE: Disabled - vanilla layout works fine and modifications cause issues
        /// </summary>
        public static void ModifyGuideLoreLayout(RectTransform loreGuideContents)
        {
            // DISABLED: This was causing null reference exceptions because it modifies
            // the layout before all items are fully populated by vanilla code.
            // The vanilla horizontal layout works fine.
        }
        
        /// <summary>
        /// Cleanup for hot-reload
        /// </summary>
        public static void Cleanup()
        {
            if (_survivalManualButton != null)
            {
                UnityEngine.Object.Destroy(_survivalManualButton);
                _survivalManualButton = null;
            }
            
            if (_gameMechanicsButton != null)
            {
                UnityEngine.Object.Destroy(_gameMechanicsButton);
                _gameMechanicsButton = null;
            }
            
            if (_customButtonContainer != null)
            {
                UnityEngine.Object.Destroy(_customButtonContainer);
                _customButtonContainer = null;
            }
        }
    }
}
