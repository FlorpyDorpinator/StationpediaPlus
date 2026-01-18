using System.Collections.Generic;

namespace StationpediaAscended.Data
{
    /// <summary>
    /// Static registry for Game Mechanics pages - similar to how Guides/Lore pages are tracked.
    /// This is our own entry type separate from SPDAEntryType.
    /// </summary>
    public static class GameMechanicsRegistry
    {
        /// <summary>
        /// List of registered Game Mechanics page keys
        /// </summary>
        public static List<string> GameMechanicsPages { get; } = new List<string>();
        
        /// <summary>
        /// Register a page as a Game Mechanics page
        /// </summary>
        public static void RegisterPage(string pageKey)
        {
            if (!string.IsNullOrEmpty(pageKey) && !GameMechanicsPages.Contains(pageKey))
            {
                GameMechanicsPages.Add(pageKey);
            }
        }
        
        /// <summary>
        /// Clear all registered pages (for hot-reload)
        /// </summary>
        public static void Clear()
        {
            GameMechanicsPages.Clear();
        }
        
        /// <summary>
        /// Check if a page key is registered as Game Mechanics
        /// </summary>
        public static bool IsGameMechanicsPage(string pageKey)
        {
            return GameMechanicsPages.Contains(pageKey);
        }
    }
}
