using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.UI;

namespace StationpediaAscended.Data
{
    /// <summary>
    /// Loads and registers the Daylight Sensor Guide as a custom Stationpedia page.
    /// The guide is loaded from a markdown file and converted to Stationpedia format.
    /// </summary>
    public static class DaylightSensorGuideLoader
    {
        private static DeviceDescriptions _guideDescriptions = null;
        private static bool _isRegistered = false;

        /// <summary>
        /// Register the Daylight Sensor Guide as a Stationpedia page
        /// </summary>
        public static void RegisterDaylightSensorGuidePage()
        {
            if (_isRegistered) return;

            try
            {
                // Load the guide using GuideLoader
                var guideDesc = GuideLoader.LoadGuide(
                    "daylight-sensor-guide.md",
                    "DaylightSensorGuide",
                    "Daylight Sensor Guide"
                );

                if (guideDesc == null)
                {
                    StationpediaAscendedMod.Log?.LogWarning("[DaylightSensorGuideLoader] Failed to load daylight sensor guide");
                    return;
                }

                _guideDescriptions = guideDesc;

                // Create a StationpediaPage for the Daylight Sensor Guide
                var page = new StationpediaPage
                {
                    Key = "DaylightSensorGuide",
                    Title = "Daylight Sensor Guide"
                };

                // Set introductory text
                page.Text = "A comprehensive guide to the Daylight Sensor, covering solar tracking, logic integration, and automation setups.\n\n" +
                           "This guide includes modes, logic variables, panel tracking setups, and advanced automation examples.\n\n" +
                           "<i>Expand the sections below to learn more.</i>";

                // Register the page
                Stationpedia.Register(page, false);

                _isRegistered = true;
                ConsoleWindow.Print("[Stationpedia Ascended] Daylight Sensor Guide page registered");
            }
            catch (Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"Error registering Daylight Sensor Guide: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the DeviceDescriptions for the Daylight Sensor Guide (for operational details rendering)
        /// </summary>
        public static DeviceDescriptions GetDaylightSensorGuideDescriptions()
        {
            if (_guideDescriptions == null)
            {
                // Try to load if not already loaded
                var guide = GuideLoader.LoadGuide(
                    "daylight-sensor-guide.md",
                    "DaylightSensorGuide",
                    "Daylight Sensor Guide"
                );
                _guideDescriptions = guide;
            }
            return _guideDescriptions;
        }

        /// <summary>
        /// Clear cached data for hot-reload
        /// </summary>
        public static void Clear()
        {
            _guideDescriptions = null;
            _isRegistered = false;
        }
    }
}
