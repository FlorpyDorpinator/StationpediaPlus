using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;

namespace StationpediaAscended.Data
{
    /// <summary>
    /// Loads and manages Game Mechanics pages from descriptions.json.
    /// These pages appear under the "Game Mechanics" button in Stationpedia.
    /// </summary>
    public static class JsonMechanicsLoader
    {
        private static Dictionary<string, GuideDescription> _loadedMechanics = new Dictionary<string, GuideDescription>();
        private static bool _mechanicsRegistered = false;

        /// <summary>
        /// Load mechanics from the parsed DescriptionsRoot data
        /// </summary>
        public static void LoadMechanics(DescriptionsRoot data)
        {
            _loadedMechanics.Clear();

            if (data?.mechanics == null || data.mechanics.Count == 0)
            {
                ConsoleWindow.Print("[Stationpedia Ascended] No game mechanics found in descriptions.json");
                return;
            }

            foreach (var mechanic in data.mechanics)
            {
                if (string.IsNullOrEmpty(mechanic.guideKey))
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] Skipping mechanic with no guideKey");
                    continue;
                }

                _loadedMechanics[mechanic.guideKey] = mechanic;
                ConsoleWindow.Print($"[Stationpedia Ascended] Loaded mechanic: {mechanic.displayName ?? mechanic.guideKey}");
            }

            ConsoleWindow.Print($"[Stationpedia Ascended] Loaded {_loadedMechanics.Count} game mechanics from JSON");
        }

        /// <summary>
        /// Register all loaded mechanics as Stationpedia pages and with GameMechanicsRegistry
        /// </summary>
        public static void RegisterMechanicsPages()
        {
            if (_mechanicsRegistered) return;

            foreach (var kvp in _loadedMechanics)
            {
                try
                {
                    var mechanic = kvp.Value;
                    
                    // Create a StationpediaPage for this mechanic
                    var page = new StationpediaPage
                    {
                        Key = mechanic.guideKey,
                        Title = mechanic.displayName ?? mechanic.guideKey
                    };

                    // Set introductory text from pageDescription
                    page.Text = mechanic.pageDescription ?? "";

                    // Register the page with Stationpedia
                    Stationpedia.Register(page, false);

                    // Also register with GameMechanicsRegistry so it shows up in the listing
                    GameMechanicsRegistry.RegisterPage(mechanic.guideKey);

                    ConsoleWindow.Print($"[Stationpedia Ascended] Registered mechanic page: {mechanic.guideKey}");
                }
                catch (Exception ex)
                {
                    StationpediaAscendedMod.Log?.LogError($"Error registering mechanic {kvp.Key}: {ex.Message}");
                }
            }

            _mechanicsRegistered = true;
        }

        /// <summary>
        /// Get all loaded mechanics sorted by sortOrder
        /// </summary>
        public static List<GuideDescription> GetAllMechanics()
        {
            return _loadedMechanics.Values
                .OrderBy(m => m.sortOrder)
                .ThenBy(m => m.displayName ?? m.guideKey)
                .ToList();
        }

        /// <summary>
        /// Get a specific mechanic by key
        /// </summary>
        public static GuideDescription GetMechanic(string mechanicKey)
        {
            if (_loadedMechanics.TryGetValue(mechanicKey, out var mechanic))
            {
                return mechanic;
            }
            return null;
        }

        /// <summary>
        /// Check if a mechanic exists
        /// </summary>
        public static bool HasMechanic(string mechanicKey)
        {
            return _loadedMechanics.ContainsKey(mechanicKey);
        }

        /// <summary>
        /// Convert a mechanic (GuideDescription) to DeviceDescriptions for rendering
        /// </summary>
        public static DeviceDescriptions ToDeviceDescriptions(GuideDescription mechanic)
        {
            if (mechanic == null) return null;

            return new DeviceDescriptions
            {
                deviceKey = mechanic.guideKey,
                displayName = mechanic.displayName,
                pageDescription = mechanic.pageDescription,
                pageDescriptionPrepend = mechanic.pageDescriptionPrepend,
                pageDescriptionAppend = mechanic.pageDescriptionAppend,
                pageImage = mechanic.pageImage,
                operationalDetails = mechanic.operationalDetails,
                operationalDetailsTitleColor = mechanic.operationalDetailsTitleColor,
                generateToc = mechanic.generateToc,
                tocTitle = mechanic.tocTitle,
                tocFlat = mechanic.tocFlat,
                operationalDetailsBackgroundColor = mechanic.operationalDetailsBackgroundColor
            };
        }

        /// <summary>
        /// Clear all loaded mechanics (for hot-reload)
        /// </summary>
        public static void Clear()
        {
            _loadedMechanics.Clear();
            _mechanicsRegistered = false;
        }

        /// <summary>
        /// Parse button color from mechanic settings
        /// </summary>
        public static Color GetButtonColor(GuideDescription mechanic)
        {
            if (mechanic == null || string.IsNullOrEmpty(mechanic.buttonColor))
            {
                // Default to blue for game mechanics
                return new Color(0f, 0.54f, 0.90f, 1f); // #008AE6
            }

            var colorStr = mechanic.buttonColor.ToLower();
            
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
