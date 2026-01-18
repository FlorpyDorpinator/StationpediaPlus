using System;
using UnityEngine;

namespace StationpediaAscended.UI
{
    /// <summary>
    /// Manages the toggle between Ascended (custom styling) and Vanilla (native styling) modes.
    /// </summary>
    public static class VanillaModeManager
    {
        /// <summary>
        /// Event fired when vanilla mode is toggled
        /// </summary>
        public static event Action<bool> OnVanillaModeChanged;
        
        /// <summary>
        /// True if vanilla mode is enabled (native styling), false for Ascended mode (custom styling)
        /// Defaults to true - Ascended styling is an easter egg
        /// </summary>
        public static bool IsVanillaMode { get; private set; } = true;
        
        /// <summary>
        /// Toggle between vanilla and ascended modes
        /// </summary>
        public static void Toggle()
        {
            IsVanillaMode = !IsVanillaMode;
            OnVanillaModeChanged?.Invoke(IsVanillaMode);
            Assets.Scripts.ConsoleWindow.Print($"[Stationpedia Ascended] Mode: {(IsVanillaMode ? "Vanilla" : "Ascended")}");
        }
        
        /// <summary>
        /// Set vanilla mode directly
        /// </summary>
        public static void SetVanillaMode(bool vanilla)
        {
            if (IsVanillaMode != vanilla)
            {
                IsVanillaMode = vanilla;
                OnVanillaModeChanged?.Invoke(IsVanillaMode);
            }
        }
        
        /// <summary>
        /// Get the title color based on current mode and depth
        /// </summary>
        public static string GetTitleColor(int depth)
        {
            if (IsVanillaMode)
            {
                return "#FFFFFF"; // White in vanilla mode
            }
            
            // Ascended mode - orange gradient
            return depth == 0 ? "#FF7A18" : (depth == 1 ? "#E09030" : "#C08040");
        }
        
        /// <summary>
        /// Get the background color based on current mode
        /// </summary>
        public static Color GetBackgroundColor(string customColor = null)
        {
            if (IsVanillaMode)
            {
                // Native dark background - match vanilla Stationpedia
                return new Color(0.1f, 0.1f, 0.1f, 0.9f);
            }
            
            // Ascended mode - blue tint or custom color
            if (!string.IsNullOrEmpty(customColor) && ColorUtility.TryParseHtmlString(customColor, out Color custom))
            {
                return custom;
            }
            
            // Default Stationeers blue
            return new Color(0.06f, 0.12f, 0.22f, 0.92f);
        }
    }
}
