using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;

namespace StationpediaAscended.Data
{
    /// <summary>
    /// Loads and manages custom guides from descriptions.json.
    /// These guides appear under the "Guides" button in Stationpedia.
    /// </summary>
    public static class JsonGuideLoader
    {
        private static Dictionary<string, GuideDescription> _loadedGuides = new Dictionary<string, GuideDescription>();
        private static bool _guidesRegistered = false;

        /// <summary>
        /// Load guides from the parsed DescriptionsRoot data
        /// </summary>
        public static void LoadGuides(DescriptionsRoot data)
        {
            _loadedGuides.Clear();

            if (data?.guides == null || data.guides.Count == 0)
            {
                ConsoleWindow.Print("[Stationpedia Ascended] No custom guides found in descriptions.json");
                return;
            }

            foreach (var guide in data.guides)
            {
                if (string.IsNullOrEmpty(guide.guideKey))
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] Skipping guide with no guideKey");
                    continue;
                }

                _loadedGuides[guide.guideKey] = guide;
                ConsoleWindow.Print($"[Stationpedia Ascended] Loaded guide: {guide.displayName ?? guide.guideKey}");
            }

            ConsoleWindow.Print($"[Stationpedia Ascended] Loaded {_loadedGuides.Count} custom guides from JSON");
        }

        /// <summary>
        /// Register all loaded guides as Stationpedia pages
        /// </summary>
        public static void RegisterGuidePages()
        {
            if (_guidesRegistered) return;

            foreach (var kvp in _loadedGuides)
            {
                try
                {
                    var guide = kvp.Value;
                    
                    // Create a StationpediaPage for this guide
                    var page = new StationpediaPage
                    {
                        Key = guide.guideKey,
                        Title = guide.displayName ?? guide.guideKey
                    };

                    // Set introductory text from pageDescription
                    page.Text = guide.pageDescription ?? "";

                    // Register the page
                    Stationpedia.Register(page, false);

                    ConsoleWindow.Print($"[Stationpedia Ascended] Registered guide page: {guide.guideKey}");
                }
                catch (Exception ex)
                {
                    StationpediaAscendedMod.Log?.LogError($"Error registering guide {kvp.Key}: {ex.Message}");
                }
            }

            _guidesRegistered = true;
        }

        /// <summary>
        /// Get all loaded guides sorted by sortOrder
        /// </summary>
        public static List<GuideDescription> GetAllGuides()
        {
            return _loadedGuides.Values
                .OrderBy(g => g.sortOrder)
                .ThenBy(g => g.displayName ?? g.guideKey)
                .ToList();
        }

        /// <summary>
        /// Get a specific guide by key
        /// </summary>
        public static GuideDescription GetGuide(string guideKey)
        {
            if (_loadedGuides.TryGetValue(guideKey, out var guide))
            {
                return guide;
            }
            return null;
        }

        /// <summary>
        /// Check if a guide exists
        /// </summary>
        public static bool HasGuide(string guideKey)
        {
            return _loadedGuides.ContainsKey(guideKey);
        }

        /// <summary>
        /// Convert a GuideDescription to DeviceDescriptions for rendering
        /// (Reuses existing operational details rendering logic)
        /// </summary>
        public static DeviceDescriptions ToDeviceDescriptions(GuideDescription guide)
        {
            if (guide == null) return null;

            return new DeviceDescriptions
            {
                deviceKey = guide.guideKey,
                displayName = guide.displayName,
                pageDescription = guide.pageDescription,
                pageDescriptionPrepend = guide.pageDescriptionPrepend,
                pageDescriptionAppend = guide.pageDescriptionAppend,
                pageImage = guide.pageImage,
                operationalDetails = guide.operationalDetails,
                operationalDetailsTitleColor = guide.operationalDetailsTitleColor,
                generateToc = guide.generateToc,
                tocTitle = guide.tocTitle,
                tocFlat = guide.tocFlat,
                operationalDetailsBackgroundColor = guide.operationalDetailsBackgroundColor
            };
        }

        /// <summary>
        /// Clear all loaded guides (for hot-reload)
        /// </summary>
        public static void Clear()
        {
            _loadedGuides.Clear();
            _guidesRegistered = false;
        }

        /// <summary>
        /// Parse button color from guide settings
        /// </summary>
        public static Color GetButtonColor(GuideDescription guide)
        {
            if (guide == null || string.IsNullOrEmpty(guide.buttonColor))
            {
                // Default to blue (like Universe buttons)
                return new Color(0f, 0.54f, 0.90f, 1f); // #008AE6
            }

            var colorStr = guide.buttonColor.ToLower();
            
            if (colorStr == "blue")
            {
                return new Color(0f, 0.54f, 0.90f, 1f); // #008AE6
            }
            else if (colorStr == "orange")
            {
                return new Color(1f, 0.42f, 0.09f, 1f); // #FF6A18
            }
            else if (colorStr == "green")
            {
                return new Color(0.27f, 0.68f, 0.51f, 1f); // #44AD83
            }
            else if (colorStr.StartsWith("#"))
            {
                // Try to parse hex color
                if (ColorUtility.TryParseHtmlString(colorStr, out var parsedColor))
                {
                    return parsedColor;
                }
            }

            // Default to blue
            return new Color(0f, 0.54f, 0.90f, 1f);
        }
    }
}
