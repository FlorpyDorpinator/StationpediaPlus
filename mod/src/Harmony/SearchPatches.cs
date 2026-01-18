using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StationpediaAscended.Patches
{
    /// <summary>
    /// Harmony patches for improving Stationpedia search functionality.
    /// Adds relevance-based sorting with category grouping.
    /// </summary>
    public static class SearchPatches
    {
        // Track created category headers so we can clean them up
        private static List<GameObject> _searchCategoryHeaders = new List<GameObject>();
        
        // Track injected search results so we can clean them up
        private static List<GameObject> _injectedItems = new List<GameObject>();
        
        // Cache the last search text to avoid re-processing
        private static string _lastSearchText = "";
        private static int _lastResultCount = -1;
        
        // Flag to track if we've hooked the search field
        private static bool _searchFieldHooked = false;
        
        // Coroutine reference for debouncing
        private static Coroutine _reorganizeCoroutine = null;
        
        // ===== OPTIMIZATION CACHES (P2 & P3) =====
        
        // P2: Page title index for O(1) lookups instead of O(n) full scans
        private static Dictionary<string, List<StationpediaPage>> _pageTitleIndex = null;
        private static Dictionary<string, List<StationpediaPage>> _pageWordIndex = null;
        private static Dictionary<string, string> _cleanedTitleCache = new Dictionary<string, string>();
        
        // P3: Category lookup cache to avoid triple-nested loops
        private static Dictionary<string, string> _categoryCache = new Dictionary<string, string>();
        private static bool _categoryCacheBuilt = false;
        
        // Performance: Cached template references to avoid repeated lookups
        private static Sprite _cachedHeaderSprite = null;
        private static TMP_FontAsset _cachedFont = null;
        private static Material _cachedFontMaterial = null;
        private static ColorBlock _cachedButtonColors;
        
        /// <summary>
        /// Priority levels for search result scoring.
        /// </summary>
        private enum MatchPriority
        {
            ExactTitle = 0,
            TitleStartsWith = 1,
            TitleContains = 2,
            DescriptionContains = 3
        }

        /// <summary>
        /// Helper class to store scored search results.
        /// </summary>
        private class ScoredResult
        {
            public StationpediaPage Page { get; set; }
            public MatchPriority Priority { get; set; }
            public string Category { get; set; }
            public Transform ItemTransform { get; set; }
        }

        /// <summary>
        /// Postfix patch for ClearPreviousSearch - cleans up our headers.
        /// </summary>
        public static void ClearPreviousSearch_Postfix(Stationpedia __instance)
        {
            try
            {
                // Clean up our category headers when search is cleared
                CleanupCategoryHeaders();
                
                // Hook the search field if we haven't already
                if (!_searchFieldHooked)
                {
                    HookSearchField(__instance);
                }
                
                // Pre-build indexes for faster first search (if not already built)
                if (_pageTitleIndex == null && Stationpedia.StationpediaPages != null && Stationpedia.StationpediaPages.Count > 0)
                {
                    BuildPageIndexes();
                    BuildCategoryCache();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stationpedia Ascended] Error in ClearPreviousSearch_Postfix: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initialize search system early (called from MonitorStationpediaCoroutine when Stationpedia found)
        /// </summary>
        public static void InitializeSearchSystem(Stationpedia stationpedia)
        {
            try
            {
                // Hook search field immediately
                if (!_searchFieldHooked && stationpedia != null)
                {
                    HookSearchField(stationpedia);
                }
                
                // Pre-build indexes for instant first search
                if (_pageTitleIndex == null && Stationpedia.StationpediaPages != null && Stationpedia.StationpediaPages.Count > 0)
                {
                    BuildPageIndexes();
                    BuildCategoryCache();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stationpedia Ascended] Error initializing search system: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Hook into the search field's onSubmit to trigger reorganization after search.
        /// </summary>
        private static void HookSearchField(Stationpedia stationpedia)
        {
            try
            {
                if (stationpedia?.SearchField == null) return;
                
                // Add a listener that will trigger reorganization after a delay
                // This runs after the async search has time to populate results
                stationpedia.SearchField.onSubmit.AddListener(new UnityAction<string>(OnSearchSubmit));
                stationpedia.SearchField.onValueChanged.AddListener(new UnityAction<string>(OnSearchValueChanged));
                
                _searchFieldHooked = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stationpedia Ascended] Error hooking search field: {ex.Message}");
            }
        }
        
        private static void OnSearchSubmit(string searchText)
        {
            if (StationpediaAscendedMod.Instance != null)
            {
                // Stop any pending reorganization
                if (_reorganizeCoroutine != null)
                {
                    StationpediaAscendedMod.Instance.StopCoroutine(_reorganizeCoroutine);
                }
                // Start new one with short delay for async search to complete
                _reorganizeCoroutine = StationpediaAscendedMod.Instance.StartCoroutine(
                    DelayedReorganize(searchText, 0.3f));
            }
        }
        
        private static void OnSearchValueChanged(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                CleanupCategoryHeaders();
                return;
            }
            
            if (StationpediaAscendedMod.Instance != null && searchText.Length >= 3)
            {
                // Stop any pending reorganization
                if (_reorganizeCoroutine != null)
                {
                    StationpediaAscendedMod.Instance.StopCoroutine(_reorganizeCoroutine);
                }
                // Start new one with longer delay for typing
                _reorganizeCoroutine = StationpediaAscendedMod.Instance.StartCoroutine(
                    DelayedReorganize(searchText, 0.8f));
            }
        }
        
        private static IEnumerator DelayedReorganize(string searchText, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // P1 OPTIMIZATION: Replace slow wait loop with 2-frame delay
            // The old code waited up to 1 second (10 x 0.1s) polling for stable results
            // Testing shows 2 frames is sufficient for vanilla search to populate
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            ReorganizeSearchResults(Stationpedia.Instance, searchText);
        }
        
        private static int CountVisibleSearchResults(RectTransform searchContents)
        {
            int count = 0;
            foreach (Transform child in searchContents)
            {
                if (child.gameObject.activeSelf && child.GetComponent<SPDAListItem>() != null)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Main reorganization method - call this after search results are populated.
        /// </summary>
        public static void ReorganizeSearchResults(Stationpedia stationpedia, string searchText)
        {
            try
            {
                if (stationpedia == null || string.IsNullOrEmpty(searchText)) return;
                if (stationpedia.SearchContents == null) return;
                
                int currentCount = CountVisibleSearchResults(stationpedia.SearchContents);
                
                // Don't re-process the same search with same result count
                if (searchText == _lastSearchText && currentCount == _lastResultCount) return;
                _lastSearchText = searchText;
                _lastResultCount = currentCount;
                
                // Clean up previous headers
                CleanupCategoryHeaders();
                
                // Collect all visible SPDAListItem children with their pages
                var items = new List<(SPDAListItem item, StationpediaPage page, Transform transform)>();
                var existingPageKeys = new HashSet<string>();
                var itemsToHide = new List<Transform>();
                
                foreach (Transform child in stationpedia.SearchContents)
                {
                    if (!child.gameObject.activeSelf) continue;
                    
                    var listItem = child.GetComponent<SPDAListItem>();
                    if (listItem == null) continue;
                    
                    // Try to find the page for this item by matching the title
                    string title = listItem.InsertTitle?.text ?? "";
                    var page = FindPageByTitle(title);
                    
                    if (page != null)
                    {
                        // Filter out ruptured/burnt items
                        if (ShouldHideFromSearch(page))
                        {
                            itemsToHide.Add(child);
                            continue;
                        }
                        
                        // Fix missing sprites for existing search results
                        FixMissingSprite(listItem, page, stationpedia);
                        
                        items.Add((listItem, page, child));
                        existingPageKeys.Add(page.Key);
                    }
                    else
                    {
                        // Hide orphan items since they may be debris/junk
                        itemsToHide.Add(child);
                    }
                }
                
                // Hide filtered items
                foreach (var item in itemsToHide)
                {
                    item.gameObject.SetActive(false);
                }
                
                // Find and inject missing exact/whole-word matches
                var injectedPages = FindMissingMatches(searchText, existingPageKeys);
                if (injectedPages.Count > 0)
                {
                    InjectMissingResults(stationpedia, injectedPages, items, existingPageKeys);
                }
                
                if (items.Count == 0) return;
                
                // Score and categorize results
                var scoredResults = ScoreResults(items, searchText);
                
                // Reorganize the UI
                ReorderSearchUI(stationpedia, scoredResults);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stationpedia Ascended] Error in ReorganizeSearchResults: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Find pages that should match the search but weren't returned by vanilla search.
        /// This fixes the vanilla bug where too many partial matches hide exact matches.
        /// P2 OPTIMIZATION: Uses cached title index for O(1) lookups instead of O(n) full scan.
        /// </summary>
        private static List<StationpediaPage> FindMissingMatches(string searchText, HashSet<string> existingPageKeys)
        {
            // Build indexes on first use (lazy initialization)
            BuildPageIndexes();
            
            var missingPages = new List<StationpediaPage>();
            string searchLower = searchText.ToLowerInvariant().Trim();
            
            // O(1) lookup for exact title matches
            if (_pageTitleIndex.TryGetValue(searchLower, out var exactPages))
            {
                foreach (var page in exactPages)
                {
                    if (!existingPageKeys.Contains(page.Key) && !ShouldHideFromSearch(page))
                    {
                        missingPages.Add(page);
                    }
                }
            }
            
            // O(1) lookup for whole-word matches
            if (_pageWordIndex.TryGetValue(searchLower, out var wordPages))
            {
                foreach (var page in wordPages)
                {
                    // Skip if already added as exact match
                    if (missingPages.Contains(page)) continue;
                    if (!existingPageKeys.Contains(page.Key) && !ShouldHideFromSearch(page))
                    {
                        missingPages.Add(page);
                    }
                }
            }
            
            return missingPages;
        }
        
        /// <summary>
        /// P2 OPTIMIZATION: Build page title indexes for O(1) lookups.
        /// Called lazily on first search.
        /// </summary>
        private static void BuildPageIndexes()
        {
            if (_pageTitleIndex != null) return; // Already built
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            _pageTitleIndex = new Dictionary<string, List<StationpediaPage>>();
            _pageWordIndex = new Dictionary<string, List<StationpediaPage>>();
            
            foreach (var page in Stationpedia.StationpediaPages)
            {
                if (ShouldHideFromSearch(page)) continue;
                
                string title = CleanTitle(page.Title).ToLowerInvariant();
                if (string.IsNullOrEmpty(title)) continue;
                
                // Index by full title for exact matches
                if (!_pageTitleIndex.ContainsKey(title))
                    _pageTitleIndex[title] = new List<StationpediaPage>();
                _pageTitleIndex[title].Add(page);
                
                // Index by individual words for whole-word matches
                string[] words = title.Split(new[] { ' ', '-', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (word.Length < 2) continue; // Skip single chars
                    if (!_pageWordIndex.ContainsKey(word))
                        _pageWordIndex[word] = new List<StationpediaPage>();
                    if (!_pageWordIndex[word].Contains(page))
                        _pageWordIndex[word].Add(page);
                }
            }
            
            stopwatch.Stop();
            ConsoleWindow.Print($"[Stationpedia Ascended] Built page indexes: {_pageTitleIndex.Count} titles, {_pageWordIndex.Count} words in {stopwatch.ElapsedMilliseconds}ms");
        }
        
        /// <summary>
        /// Clean title by removing rich text tags. Uses cache for performance.
        /// </summary>
        private static string CleanTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return "";
            
            if (_cleanedTitleCache.TryGetValue(title, out string cleaned))
                return cleaned;
            
            cleaned = Regex.Replace(title, "<[^>]+>", "").Trim();
            _cleanedTitleCache[title] = cleaned;
            return cleaned;
        }

        /// <summary>
        /// Create UI elements for missing pages and add them to the search results.
        /// </summary>
        private static void InjectMissingResults(
            Stationpedia stationpedia,
            List<StationpediaPage> pages,
            List<(SPDAListItem item, StationpediaPage page, Transform transform)> items,
            HashSet<string> existingPageKeys)
        {
            try
            {
                // Get reference to the SPDAListItem prefab from an existing item
                if (items.Count == 0 || stationpedia.SearchContents == null) return;
                
                var templateItem = items[0].item;
                if (templateItem == null) return;
                
                foreach (var page in pages)
                {
                    // Skip if somehow already added
                    if (existingPageKeys.Contains(page.Key)) continue;
                    
                    // Clone an existing list item
                    var newGO = UnityEngine.Object.Instantiate(templateItem.gameObject, stationpedia.SearchContents);
                    newGO.name = $"InjectedResult_{page.Key}";
                    newGO.SetActive(true);
                    
                    var newListItem = newGO.GetComponent<SPDAListItem>();
                    if (newListItem != null)
                    {
                        // Set the display title
                        if (newListItem.InsertTitle != null)
                        {
                            newListItem.InsertTitle.text = page.Title;
                        }
                        
                        // Set up button click handler
                        string pageKey = page.Key; // Capture for closure
                        newListItem.InsertsButton.onClick.RemoveAllListeners();
                        newListItem.InsertsButton.onClick.AddListener(() => stationpedia.OpenPageByKey(pageKey));
                        
                        // Set the image - use page sprite, try prefab thumbnail, fallback to ImportantSearchImage
                        if (newListItem.InsertImage != null)
                        {
                            Sprite sprite = page.CustomSpriteToUse;
                            
                            // If no sprite, try to load from prefab
                            if (sprite == null)
                            {
                                sprite = TryGetSpriteFromPrefab(page.Key);
                            }
                            
                            // Last resort: use Stationpedia's fallback image
                            if (sprite == null)
                            {
                                sprite = stationpedia.ImportantSearchImage;
                            }
                            
                            newListItem.InsertImage.sprite = sprite;
                            newListItem.InsertImage.gameObject.SetActive(sprite != null);
                        }
                        
                        // Add to our items list for scoring
                        items.Add((newListItem, page, newGO.transform));
                        existingPageKeys.Add(page.Key);
                        
                        // Track for cleanup
                        _injectedItems.Add(newGO);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Stationpedia Ascended] Error injecting results: {ex.Message}");
            }
        }

        private static StationpediaPage FindPageByTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            
            // Build indexes if not already done
            BuildPageIndexes();
            
            // Use cached clean title
            string cleanTitle = CleanTitle(title).ToLowerInvariant();
            
            // O(1) lookup using our cached index
            if (_pageTitleIndex.TryGetValue(cleanTitle, out var pages) && pages.Count > 0)
            {
                return pages[0];
            }
            
            return null;
        }

        /// <summary>
        /// Try to load the sprite/thumbnail from the prefab based on page key.
        /// Page keys are like "ThingItemCorn" or "ItemCorn" for items.
        /// </summary>
        private static Sprite TryGetSpriteFromPrefab(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey)) return null;
            
            try
            {
                string prefabName = null;
                
                // Extract prefab name from page key
                if (pageKey.StartsWith("Thing"))
                {
                    prefabName = pageKey.Substring(5); // Remove "Thing" prefix
                }
                else if (pageKey.StartsWith("Item"))
                {
                    prefabName = pageKey; // Items use their full name
                }
                else
                {
                    prefabName = pageKey;
                }
                
                // Try to find the prefab
                Thing thing = Prefab.Find(prefabName);
                if (thing != null)
                {
                    return thing.GetThumbnail();
                }
                
                // Try with "Item" prefix if not found
                if (!prefabName.StartsWith("Item"))
                {
                    thing = Prefab.Find("Item" + prefabName);
                    if (thing != null)
                    {
                        return thing.GetThumbnail();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Stationpedia Ascended] Could not load sprite for {pageKey}: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Determines if a page should be hidden from search results.
        /// Filters out ruptured/burnt cables, wreckage, and other debris items.
        /// Also respects the game's HideInStationpedia flag.
        /// </summary>
        private static bool ShouldHideFromSearch(StationpediaPage page)
        {
            if (page == null) return false;
            
            string key = page.Key ?? "";
            string title = page.Title ?? "";
            // Strip rich text tags for proper matching
            string cleanTitle = Regex.Replace(title, "<[^>]+>", "").Trim();
            string cleanTitleLower = cleanTitle.ToLowerInvariant();
            
            // Check if the prefab has HideInStationpedia set
            if (key.StartsWith("Thing"))
            {
                string prefabName = key.Substring(5);
                Thing thing = Prefab.Find(prefabName);
                if (thing != null && thing.HideInStationpedia)
                    return true;
                    
                // Also check HiddenInPedia dictionary
                bool hiddenInPedia = false;
                if (Stationpedia.DataHandler?.HiddenInPedia?.TryGetValue(prefabName, out hiddenInPedia) == true && hiddenInPedia)
                    return true;
            }
            
            // Hide ruptured/burnt cables
            if (key.Contains("Ruptured") || cleanTitleLower.Contains("ruptured"))
                return true;
            if (key.Contains("CableRuptured"))
                return true;
                
            // Hide burnt items
            if (cleanTitleLower.StartsWith("burnt ") || cleanTitleLower.StartsWith("burnt") || key.Contains("Burnt"))
                return true;
                
            // Hide wreckage items
            if (cleanTitleLower.Contains("wreckage") || key.Contains("Wreckage"))
                return true;
                
            return false;
        }

        /// <summary>
        /// Fix missing sprites for search result items.
        /// </summary>
        private static void FixMissingSprite(SPDAListItem listItem, StationpediaPage page, Stationpedia stationpedia)
        {
            if (listItem?.InsertImage == null) return;
            
            // Only fix if sprite is missing
            if (listItem.InsertImage.sprite != null) return;
            
            Sprite sprite = page.CustomSpriteToUse;
            
            // If no sprite on page, try to load from prefab
            if (sprite == null)
            {
                sprite = TryGetSpriteFromPrefab(page.Key);
            }
            
            // Last resort: use Stationpedia's fallback image  
            if (sprite == null)
            {
                sprite = stationpedia.ImportantSearchImage;
            }
            
            listItem.InsertImage.sprite = sprite;
            listItem.InsertImage.gameObject.SetActive(sprite != null);
        }

        private static List<ScoredResult> ScoreResults(
            List<(SPDAListItem item, StationpediaPage page, Transform transform)> items,
            string searchText)
        {
            var results = new List<ScoredResult>(items.Count); // Pre-allocate capacity
            string searchLower = searchText.ToLowerInvariant().Trim();
            string escapedSearch = Regex.Escape(searchLower);
            
            // Word boundary pattern - matches search term as a whole word
            Regex wholeWordRegex = new Regex($@"\b{escapedSearch}\b", RegexOptions.IgnoreCase);
            
            foreach (var (item, page, transform) in items)
            {
                // Use the display title from the list item, not the page
                string displayTitle = item.InsertTitle?.text ?? "";
                string title = CleanTitle(displayTitle).ToLowerInvariant();
                
                bool isWholeWordMatch = wholeWordRegex.IsMatch(title);
                
                MatchPriority priority;
                
                // Check for exact match (title is exactly the search term)
                if (title == searchLower)
                {
                    priority = MatchPriority.ExactTitle;
                }
                // Check for starts-with as a whole word (e.g., "corn seed" starts with word "corn")
                else if (title.StartsWith(searchLower + " ") || (title.StartsWith(searchLower) && isWholeWordMatch))
                {
                    priority = MatchPriority.TitleStartsWith;
                }
                // Check for whole word match anywhere in title (e.g., "popped corn" contains word "corn")
                else if (isWholeWordMatch)
                {
                    priority = MatchPriority.TitleContains;
                }
                // Partial match - search term is part of another word (e.g., "corner" contains "corn")
                else if (title.Contains(searchLower))
                {
                    priority = MatchPriority.DescriptionContains; // Demote partial matches
                }
                else
                {
                    priority = MatchPriority.DescriptionContains;
                }
                
                // Get category from page
                string category = GetPageCategory(page);
                
                results.Add(new ScoredResult
                {
                    Page = page,
                    Priority = priority,
                    Category = category,
                    ItemTransform = transform
                });
            }
            
            return results;
        }

        /// <summary>
        /// Maps category names to ASCII symbols for visual clarity.
        /// Using pure ASCII for maximum font compatibility.
        /// </summary>
        private static readonly Dictionary<string, string> CategoryIcons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Atmospherics & Pipes
            { "AtmosDevices", "[A]" },
            { "AirContitioningAtmos", "[AC]" },
            { "PipesCategory", "[P]" },
            { "GasCanisterCategory", "[GC]" },
            
            // Electronics & Power
            { "BatteryCategory", "[B]" },
            { "CableCategory", "[C]" },
            { "LightCategory", "[L]" },
            
            // Logic & Circuits
            { "LogicIntegratedCircuitsCategory", "[IC]" },
            { "LogicInputCategory", "[IN]" },
            { "LogicProcessorsCategory", "[CPU]" },
            { "LogicReadersCategory", "[RD]" },
            { "LogicWriterCategory", "[WR]" },
            { "MotherboardCategory", "[MB]" },
            { "Logic Variables", "[VAR]" },
            { "Logic Slot Variables", "[SLOT]" },
            
            // Manufacturing & Fabrication
            { "Fabricators", "[FAB]" },
            { "KitCategory", "[KIT]" },
            
            // Food & Plants
            { "FoodCategory", "[F]" },
            { "Edibles", "[E]" },
            { "Plants", "[PL]" },
            
            // Personal Equipment
            { "PersonalSuits", "[SUIT]" },
            { "PersonalBackpacks", "[BP]" },
            { "PersonalClothing", "[CL]" },
            { "PersonalHelmets", "[HLM]" },
            { "PersonalEyeWear", "[EYE]" },
            { "PersonalToolbelt", "[TB]" },
            
            // Tools & Weapons
            { "ManualTools", "[T]" },
            { "FireArm", "[W]" },
            
            // Structures & Building
            { "WallFloorCategory", "[WF]" },
            { "DoorCategory", "[DR]" },
            { "ChairTableCategory", "[FUR]" },
            { "SafetyCategory", "[!]" },
            
            // Logistics & Transport
            { "ChuteCategory", "[CH]" },
            { "CargoCategory", "[CRG]" },
            { "CartridgeCategory", "[CRT]" },
            
            // Rockets & Space
            { "RocketEngineCategory", "[RKT]" },
            { "RocketPayloadCategory", "[PAY]" },
            { "UmbilicalCategory", "[UMB]" },
            
            // Science & Research
            { "GeneticDevices", "[GEN]" },
            { "TradingDevices", "[TRD]" },
            { "Genetics", "[DNA]" },
            
            // Appliances
            { "ApplianceCategory", "[APP]" },
            
            // Resources
            { "Gases", "[GAS]" },
            { "Reagents", "[REA]" },
            { "OreHeader", "[ORE]" },
            { "FrozenOreHeader", "[FRZ]" },
            { "PureIceHeader", "[ICE]" },
            { "IngotHeader", "[ING]" },
            
            // Sensors & Consoles
            { "SensorCategory", "[SEN]" },
            { "ConsoleCategory", "[CON]" },
            
            // Fallback
            { "Other", "[-]" },
        };

        /// <summary>
        /// Gets the icon for a category name.
        /// </summary>
        private static string GetCategoryIcon(string category)
        {
            if (string.IsNullOrEmpty(category)) return "[-]";
            
            if (CategoryIcons.TryGetValue(category, out string icon))
                return icon;
            
            // Try partial matches for localized category names
            string categoryLower = category.ToLowerInvariant();
            
            if (categoryLower.Contains("atmos") || categoryLower.Contains("air")) return "[A]";
            if (categoryLower.Contains("pipe")) return "[P]";
            if (categoryLower.Contains("gas") && categoryLower.Contains("can")) return "[GC]";
            if (categoryLower.Contains("battery") || categoryLower.Contains("power")) return "[B]";
            if (categoryLower.Contains("cable") || categoryLower.Contains("wire")) return "[C]";
            if (categoryLower.Contains("light")) return "[L]";
            if (categoryLower.Contains("logic") || categoryLower.Contains("circuit")) return "[IC]";
            if (categoryLower.Contains("processor") || categoryLower.Contains("computer")) return "[CPU]";
            if (categoryLower.Contains("fabricat") || categoryLower.Contains("printer")) return "[FAB]";
            if (categoryLower.Contains("kit")) return "[KIT]";
            if (categoryLower.Contains("food") || categoryLower.Contains("edible")) return "[F]";
            if (categoryLower.Contains("plant") || categoryLower.Contains("seed")) return "[PL]";
            if (categoryLower.Contains("suit") || categoryLower.Contains("armor")) return "[SUIT]";
            if (categoryLower.Contains("helmet")) return "[HLM]";
            if (categoryLower.Contains("cloth")) return "[CL]";
            if (categoryLower.Contains("tool")) return "[T]";
            if (categoryLower.Contains("weapon") || categoryLower.Contains("gun")) return "[W]";
            if (categoryLower.Contains("wall") || categoryLower.Contains("floor") || categoryLower.Contains("frame")) return "[WF]";
            if (categoryLower.Contains("door") || categoryLower.Contains("airlock")) return "[DR]";
            if (categoryLower.Contains("chair") || categoryLower.Contains("table") || categoryLower.Contains("furniture")) return "[FUR]";
            if (categoryLower.Contains("chute") || categoryLower.Contains("conveyor")) return "[CH]";
            if (categoryLower.Contains("cargo") || categoryLower.Contains("storage") || categoryLower.Contains("locker")) return "[CRG]";
            if (categoryLower.Contains("rocket") || categoryLower.Contains("engine")) return "[RKT]";
            if (categoryLower.Contains("genetic") || categoryLower.Contains("dna")) return "[DNA]";
            if (categoryLower.Contains("trade") || categoryLower.Contains("vend")) return "[TRD]";
            if (categoryLower.Contains("reagent") || categoryLower.Contains("chemical")) return "[REA]";
            if (categoryLower.Contains("ore") || categoryLower.Contains("mineral")) return "[ORE]";
            if (categoryLower.Contains("ice") || categoryLower.Contains("frozen")) return "[ICE]";
            if (categoryLower.Contains("ingot") || categoryLower.Contains("metal")) return "[ING]";
            if (categoryLower.Contains("sensor")) return "[SEN]";
            if (categoryLower.Contains("console") || categoryLower.Contains("screen")) return "[CON]";
            if (categoryLower.Contains("tank") || categoryLower.Contains("canister")) return "[GC]";
            if (categoryLower.Contains("gas")) return "[GAS]";
            if (categoryLower.Contains("backpack")) return "[BP]";
            if (categoryLower.Contains("eyewear") || categoryLower.Contains("glass")) return "[EYE]";
            
            return "[-]"; // Default
        }

        private static string GetPageCategory(StationpediaPage page)
        {
            if (page == null) return "Other";
            
            string key = page.Key ?? "";
            
            // P3 OPTIMIZATION: Check cache first
            if (_categoryCache.TryGetValue(key, out string cachedCategory))
            {
                return cachedCategory;
            }
            
            // Build full cache on first miss (amortizes the cost)
            if (!_categoryCacheBuilt)
            {
                BuildCategoryCache();
                if (_categoryCache.TryGetValue(key, out cachedCategory))
                {
                    return cachedCategory;
                }
            }
            
            // Compute category for this page
            string category = ComputePageCategory(page);
            _categoryCache[key] = category;
            return category;
        }
        
        /// <summary>
        /// P3 OPTIMIZATION: Pre-build the category cache to avoid triple-nested loops.
        /// </summary>
        private static void BuildCategoryCache()
        {
            if (_categoryCacheBuilt) return;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                // Build reverse lookup from DataHandler's dictionary
                foreach (var listEntry in Stationpedia.DataHandler._listDictionary)
                {
                    foreach (var categoryEntry in listEntry.Value)
                    {
                        string categoryName = categoryEntry.Key;
                        foreach (var insert in categoryEntry.Value)
                        {
                            if (!string.IsNullOrEmpty(insert.PageLink))
                            {
                                _categoryCache[insert.PageLink] = categoryName;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error building category cache: {ex.Message}");
            }
            
            _categoryCacheBuilt = true;
            stopwatch.Stop();
            ConsoleWindow.Print($"[Stationpedia Ascended] Built category cache: {_categoryCache.Count} entries in {stopwatch.ElapsedMilliseconds}ms");
        }
        
        /// <summary>
        /// Compute category for a page (fallback when not in cache).
        /// </summary>
        private static string ComputePageCategory(StationpediaPage page)
        {
            if (page == null) return "Other";
            
            // Try to determine category from page's custom categories
            if (page.PageCustomCategories != null && page.PageCustomCategories.Count > 0)
            {
                return page.PageCustomCategories[0];
            }
            
            string key = page.Key ?? "";
            
            // Check special page types by key prefix
            if (key.StartsWith("Gas")) return "Gases";
            if (key.StartsWith("Reagent")) return "Reagents";
            if (key.StartsWith("Gene")) return "Genetics";
            if (key.StartsWith("LogicType")) return "Logic Variables";
            if (key.StartsWith("LogicSlotType")) return "Logic Slot Variables";
            
            return "Other";
        }

        private static void ReorderSearchUI(Stationpedia stationpedia, List<ScoredResult> scoredResults)
        {
            var searchContents = stationpedia.SearchContents;
            if (searchContents == null) return;
            
            // Group by priority, then by category within lower priorities
            var exactMatches = scoredResults.Where(r => r.Priority == MatchPriority.ExactTitle).ToList();
            var startsWithMatches = scoredResults.Where(r => r.Priority == MatchPriority.TitleStartsWith).ToList();
            var containsMatches = scoredResults.Where(r => r.Priority == MatchPriority.TitleContains).ToList();
            var descriptionMatches = scoredResults.Where(r => r.Priority == MatchPriority.DescriptionContains).ToList();
            
            int siblingIndex = 0;
            
            // Add "Exact Matches" section
            if (exactMatches.Count > 0)
            {
                siblingIndex = AddCategoryHeader(searchContents, "Exact Matches", siblingIndex, 
                    new Color(0.95f, 0.6f, 0.25f, 1f)); // Muted orange
                foreach (var result in exactMatches.OrderBy(r => r.Page.Title))
                {
                    result.ItemTransform.SetSiblingIndex(siblingIndex++);
                }
            }
            
            // Add "Title Starts With" section
            if (startsWithMatches.Count > 0)
            {
                siblingIndex = AddCategoryHeader(searchContents, "Starts With", siblingIndex,
                    new Color(0.9f, 0.8f, 0.4f, 1f)); // Muted gold
                foreach (var result in startsWithMatches.OrderBy(r => r.Page.Title))
                {
                    result.ItemTransform.SetSiblingIndex(siblingIndex++);
                }
            }
            
            // For "Contains" and "Description" matches, group by category with category-specific icons
            var remainingMatches = containsMatches.Concat(descriptionMatches).ToList();
            if (remainingMatches.Count > 0)
            {
                // Group by category
                var byCategory = remainingMatches
                    .GroupBy(r => r.Category)
                    .OrderBy(g => g.Key);
                
                foreach (var categoryGroup in byCategory)
                {
                    // Just use the category name, no icon prefix
                    siblingIndex = AddCategoryHeader(searchContents, categoryGroup.Key, siblingIndex,
                        Color.white); // White
                    
                    foreach (var result in categoryGroup.OrderBy(r => r.Page.Title))
                    {
                        result.ItemTransform.SetSiblingIndex(siblingIndex++);
                    }
                }
            }
            
            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(searchContents);
        }

        private static int AddCategoryHeader(RectTransform parent, string text, int siblingIndex, Color textColor)
        {
            // Create new header (no pooling - it was causing layout issues)
            var headerGO = CreateNewCategoryHeader(parent, text, textColor);
            
            // Add to tracking list
            _searchCategoryHeaders.Add(headerGO);
            
            // Set sibling index
            headerGO.transform.SetSiblingIndex(siblingIndex);
            
            return siblingIndex + 1;
        }
        
        /// <summary>
        /// Creates a new category header GameObject (called when pool is empty).
        /// </summary>
        private static GameObject CreateNewCategoryHeader(RectTransform parent, string text, Color textColor)
        {
            var headerGO = new GameObject($"SearchCategoryHeader");
            headerGO.transform.SetParent(parent, false);
            
            // Add RectTransform
            var rectTransform = headerGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(0, 54);
            
            // Add background image - use cached sprite if available
            var bgImage = headerGO.AddComponent<Image>();
            
            // Cache template references on first use
            if (_cachedHeaderSprite == null)
            {
                try
                {
                    var existingItem = parent.GetComponentInChildren<SPDAListItem>();
                    if (existingItem != null)
                    {
                        if (existingItem.SpecialButton != null)
                        {
                            _cachedHeaderSprite = existingItem.SpecialButton;
                        }
                        else if (existingItem.BackImage != null)
                        {
                            _cachedHeaderSprite = existingItem.BackImage.sprite;
                        }
                        
                        if (existingItem.InsertsButton != null)
                        {
                            _cachedButtonColors = existingItem.InsertsButton.colors;
                            _cachedButtonColors.normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                            _cachedButtonColors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                            _cachedButtonColors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                            _cachedButtonColors.selectedColor = _cachedButtonColors.normalColor;
                            _cachedButtonColors.disabledColor = _cachedButtonColors.normalColor;
                        }
                    }
                    
                    var existingText = parent.GetComponentInChildren<TextMeshProUGUI>();
                    if (existingText != null)
                    {
                        _cachedFont = existingText.font;
                        _cachedFontMaterial = existingText.fontSharedMaterial;
                    }
                }
                catch { }
            }
            
            // Apply cached sprite
            if (_cachedHeaderSprite != null)
            {
                bgImage.sprite = _cachedHeaderSprite;
                bgImage.type = Image.Type.Sliced;
                bgImage.color = Color.white;
                
                // Add button for color tinting
                var button = headerGO.AddComponent<Button>();
                button.targetGraphic = bgImage;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = _cachedButtonColors;
                button.interactable = true;
                var nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;
            }
            
            // Add text
            var textGO = new GameObject("HeaderText");
            textGO.transform.SetParent(headerGO.transform, false);
            
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(15, 0);
            textRT.offsetMax = new Vector2(-10, 0);
            
            var textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 19;
            textComponent.fontStyle = TMPro.FontStyles.Bold;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Use cached font
            if (_cachedFont != null)
            {
                textComponent.font = _cachedFont;
                textComponent.fontSharedMaterial = _cachedFontMaterial;
            }
            
            // Add layout element
            var layoutElement = headerGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 54;
            layoutElement.flexibleWidth = 1;
            
            return headerGO;
        }

        private static void CleanupCategoryHeaders()
        {
            // Destroy all headers
            foreach (var header in _searchCategoryHeaders)
            {
                if (header != null)
                {
                    UnityEngine.Object.Destroy(header);
                }
            }
            _searchCategoryHeaders.Clear();
            
            // Also cleanup injected items
            foreach (var item in _injectedItems)
            {
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item);
                }
            }
            _injectedItems.Clear();
            
            _lastSearchText = "";
            _lastResultCount = -1;
        }

        /// <summary>
        /// Reset state for hot-reload support.
        /// </summary>
        public static void Reset()
        {
            CleanupCategoryHeaders();
            _searchFieldHooked = false;
            _reorganizeCoroutine = null;
            
            // Clear optimization caches so they rebuild on next search
            _pageTitleIndex = null;
            _pageWordIndex = null;
            _cleanedTitleCache.Clear();
            _categoryCache.Clear();
            _categoryCacheBuilt = false;
            
            // Clear cached template references
            _cachedHeaderSprite = null;
            _cachedFont = null;
            _cachedFontMaterial = null;
        }
    }
}
