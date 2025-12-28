using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.UI;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;

namespace StationpediaPlus
{
    /// <summary>
    /// StationpediaPlus - Enhanced Stationpedia with tooltips and special behaviors
    /// Per apro: BepInPlugin attribute + BaseUnityPlugin base class is required for ScriptEngine hot-reload
    /// </summary>
    [BepInPlugin("StationpediaPlus", "StationpediaPlus", "1.0.0")]
    public class StationpediaPlusMod : BaseUnityPlugin
    {
        public const string HarmonyId = "com.stationpediaplus.mod";
        
        // BepInEx logger - will show in BepInEx console
        internal static ManualLogSource Log;
        
        // Static reference for tooltip component access
        public static StationpediaPlusMod Instance { get; private set; }
        
        // Description databases
        public static Dictionary<string, DeviceDescriptions> DeviceDatabase { get; private set; }
        public static GenericDescriptionsData GenericDescriptions { get; private set; }
        
        // Page tracking
        private string _lastPageKey = "";
        private bool _stationpediaFound = false;
        private const float CHECK_INTERVAL = 0.1f;
        
        // Monitor coroutine
        private Coroutine _monitorCoroutine;
        
        // Tooltip display state (shared with SPDALogicTooltip components)
        public static string CurrentTooltipText { get; set; } = "";
        public static bool ShowTooltip { get; set; } = false;
        
        // GUI styling
        private GUIStyle _tooltipStyle;
        private bool _stylesInitialized = false;
        private Texture2D _tooltipBackground;
        
        // Harmony instance for patching
        private Harmony _harmony;
        
        // Track created GameObjects and components for cleanup
        internal static List<GameObject> _createdGameObjects = new List<GameObject>();
        internal static List<Component> _addedComponents = new List<Component>();
        
        // Track if we've already initialized (prevent double-init from both Awake and OnLoaded)
        private bool _initialized = false;
        
        // Reference to ScriptEngine loader's MonoBehaviour for coroutines when loaded via ScriptEngine
        private static MonoBehaviour _scriptEngineHost;

        /// <summary>
        /// Called by ScriptEngineLoader when mod is loaded via F6 hot-reload (no SLP instance)
        /// </summary>
        public static void InitializeFromScriptEngine(MonoBehaviour host, ManualLogSource log)
        {
            Log = log;
            _scriptEngineHost = host;
            
            Log.LogInfo("InitializeFromScriptEngine called");
            
            // FIRST: Clean up any existing tooltip components from previous loads
            // This is important because when ScriptEngine reloads, the old static lists are gone
            // but the tooltip components may still be attached to GameObjects
            Log.LogInfo("Cleaning up old tooltip components...");
            CleanupExistingTooltips();
            
            // Load descriptions
            Log.LogInfo("Loading descriptions...");
            LoadDescriptionsStatic();
            
            // Apply Harmony patches
            Log.LogInfo("Applying Harmony patches...");
            ApplyHarmonyPatchesStatic();
            
            // Reset page tracking so monitor will re-add tooltips after reload
            _lastPageKeyStatic = "";
            _stationpediaFoundStatic = false;
            
            // Regenerate Stationpedia
            Log.LogInfo($"Stationpedia.Instance = {Stationpedia.Instance}");
            if (Stationpedia.Instance != null)
            {
                Log.LogInfo(">>> REGENERATING STATIONPEDIA NOW <<<");
                try
                {
                    Stationpedia.Regenerate();
                    Log.LogInfo(">>> STATIONPEDIA REGENERATED SUCCESSFULLY <<<");
                    ConsoleWindow.Print("[StationpediaPlus] Stationpedia regenerated!");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to regenerate Stationpedia: {ex}");
                }
            }
            else
            {
                Log.LogWarning("Stationpedia.Instance is NULL");
            }
            
            // Start monitoring coroutine on the host MonoBehaviour
            Log.LogInfo("Starting monitoring coroutine...");
            host.StartCoroutine(MonitorStationpediaCoroutineStatic());
            
            Log.LogInfo("InitializeFromScriptEngine complete!");
            ConsoleWindow.Print("[StationpediaPlus] Initialized via ScriptEngine!");
        }
        
        /// <summary>
        /// Find and destroy any tooltip components that may exist from previous loads
        /// </summary>
        private static void CleanupExistingTooltips()
        {
            int destroyed = 0;
            
            // Find all tooltip components in the scene by type name
            // We search by type because after reload, the old types are from a different assembly
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        string typeName = comp.GetType().Name;
                        if (typeName == "SPDALogicTooltip" || 
                            typeName == "SPDASlotTooltip" ||
                            typeName == "SPDAVersionTooltip" ||
                            typeName == "SPDAMemoryTooltip" ||
                            typeName == "SELogicTooltip")
                        {
                            try
                            {
                                UnityEngine.Object.Destroy(comp);
                                destroyed++;
                            }
                            catch { }
                        }
                    }
                }
            }
            
            Log?.LogInfo($"Destroyed {destroyed} old tooltip components");
            
            // Clear our tracking lists (they may have stale references)
            _addedComponents.Clear();
            _createdGameObjects.Clear();
        }
        
        /// <summary>
        /// Called by ScriptEngineLoader OnDestroy to cleanup
        /// </summary>
        public static void CleanupFromScriptEngine()
        {
            Log?.LogInfo("CleanupFromScriptEngine called");
            
            // Unpatch Harmony
            if (_harmonyStatic != null)
            {
                _harmonyStatic.UnpatchSelf();
                _harmonyStatic = null;
            }
            
            // Cleanup components and GameObjects
            foreach (var component in _addedComponents)
            {
                if (component != null)
                {
                    try { UnityEngine.Object.Destroy(component); } catch { }
                }
            }
            _addedComponents.Clear();
            
            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    try { UnityEngine.Object.Destroy(go); } catch { }
                }
            }
            _createdGameObjects.Clear();
            
            // Reset state
            ShowTooltip = false;
            CurrentTooltipText = "";
            DeviceDatabase = null;
            GenericDescriptions = null;
            _scriptEngineHost = null;
            
            Log?.LogInfo("CleanupFromScriptEngine complete");
            ConsoleWindow.Print("[StationpediaPlus] Cleaned up via ScriptEngine");
        }
        
        // Static Harmony instance for ScriptEngine path
        private static Harmony _harmonyStatic;

        // Called by ScriptEngineLoader to trigger a reload
        public void TriggerReload()
        {
            Log?.LogInfo("TriggerReload called - reinitializing mod...");
            ConsoleWindow.Print("[StationpediaPlus] TriggerReload called - reinitializing...");
            
            // Cleanup existing state
            CleanupForReload();
            
            // Reinitialize
            _initialized = false;
            Initialize();
        }

        private void CleanupForReload()
        {
            // Stop coroutines
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
                _monitorCoroutine = null;
            }
            
            // Unpatch and re-patch Harmony
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
            
            // Cleanup added components
            foreach (var component in _addedComponents)
            {
                if (component != null)
                {
                    try { Destroy(component); } catch { }
                }
            }
            _addedComponents.Clear();
            
            // Cleanup created GameObjects
            foreach (var go in _createdGameObjects)
            {
                if (go != null)
                {
                    try { Destroy(go); } catch { }
                }
            }
            _createdGameObjects.Clear();
            
            // Reset state
            _lastPageKey = "";
            _stationpediaFound = false;
            ShowTooltip = false;
            CurrentTooltipText = "";
        }

        // Called by Unity/BepInEx when plugin loads (works for both SLP and ScriptEngine)
        void Awake()
        {
            this.Logger.LogInfo(
                $"Awake StationpediaPlus"
            );
            
            try
            {
                // Initialize BepInEx logger
                Log = BepInEx.Logging.Logger.CreateLogSource("StationpediaPlus");
                Log.LogInfo("========================================");
                Log.LogInfo("=== StationpediaPlusMod Awake() CALLED ===");
                Log.LogInfo("========================================");
                
                ConsoleWindow.Print("[StationpediaPlus] === Awake() ===");
                
                // Clean up any existing tooltip components from previous loads (for ScriptEngine hot-reload)
                CleanupExistingTooltips();
                
                // Reset initialization flag for hot-reload
                _initialized = false;
                
                Instance = this;
                Log.LogInfo("Calling Initialize from Awake...");
                Initialize();
            }
            catch (Exception ex)
            {
                Log?.LogError($"ERROR in Awake: {ex}");
                ConsoleWindow.Print($"[StationpediaPlus] ERROR in Awake: {ex.Message}");
            }
        }

        // Initialize everything - called from Awake
        private void Initialize()
        {
            if (_initialized)
            {
                Debug.Log("[StationpediaPlus] Initialize called but already initialized, skipping");
                ConsoleWindow.Print("[StationpediaPlus] Initialize called but already initialized, skipping");
                return;
            }
            
            Debug.Log("[StationpediaPlus] Initialize STARTING...");
            ConsoleWindow.Print("[StationpediaPlus] Initialize called");
            
            // Load descriptions from JSON file
            Debug.Log("[StationpediaPlus] Loading descriptions...");
            LoadDescriptions();
            
            // Apply Harmony patches
            Debug.Log("[StationpediaPlus] Applying Harmony patches...");
            ApplyHarmonyPatches();
            
            // On hot-reload, skip Regenerate() - it fails because Stationpedia's internal state
            // has references to destroyed Unity objects. Pages will update when viewed.
            // We detect hot-reload by checking if Stationpedia.Instance exists (it wouldn't during cold start).
            Log?.LogInfo($"Stationpedia.Instance = {Stationpedia.Instance}");
            Debug.Log($"[StationpediaPlus] Stationpedia.Instance = {Stationpedia.Instance}");
            if (Stationpedia.Instance != null)
            {
                // Don't call Regenerate() - it causes NullReferenceException during hot-reload
                // because the Stationpedia's CreatedCategories list has destroyed object references.
                // Instead, just close and re-open the Stationpedia to refresh, or navigate to a new page.
                Log?.LogInfo(">>> Skipping Regenerate() to avoid hot-reload crash <<<");
                Debug.Log("[StationpediaPlus] Skipping Regenerate() - pages will refresh when navigated");
                ConsoleWindow.Print("[StationpediaPlus] Patches applied! Navigate to a page to see changes.");
            }
            else
            {
                Log?.LogWarning("Stationpedia.Instance is NULL - cannot regenerate yet");
                Debug.Log("[StationpediaPlus] Stationpedia.Instance is null - patches will apply later");
                ConsoleWindow.Print("[StationpediaPlus] Stationpedia not yet initialized - patches will apply on first open");
            }
            
            // Start the monitoring coroutine
            Debug.Log("[StationpediaPlus] Starting monitoring coroutine...");
            _monitorCoroutine = StartCoroutine(MonitorStationpediaCoroutine());
            ConsoleWindow.Print("[StationpediaPlus] Started monitoring coroutine");
            
            _initialized = true;
            Debug.Log("[StationpediaPlus] Initialize COMPLETE!");
            ConsoleWindow.Print("[StationpediaPlus] Initialized successfully!");
        }

        // Coroutine-based monitoring (more reliable than Update() for ModBehaviour)
        private IEnumerator MonitorStationpediaCoroutine()
        {
            ConsoleWindow.Print("[StationpediaPlus] MonitorStationpediaCoroutine started!");
            
            while (true)
            {
                yield return new WaitForSeconds(CHECK_INTERVAL);
                
                try
                {
                    // Wait until Stationpedia.Instance exists
                    if (Stationpedia.Instance == null)
                    {
                        continue;
                    }

                    if (!_stationpediaFound)
                    {
                        _stationpediaFound = true;
                        ConsoleWindow.Print("[StationpediaPlus] Stationpedia.Instance found! Now monitoring for page changes.");
                    }

                    // Check if the Stationpedia is visible/active
                    if (!Stationpedia.Instance.gameObject.activeInHierarchy)
                    {
                        _lastPageKey = "";
                        continue;
                    }

                    // Get current page key
                    string currentPageKey = Stationpedia.CurrentPageKey;
                    
                    // If page changed, add tooltips
                    if (!string.IsNullOrEmpty(currentPageKey) && currentPageKey != _lastPageKey)
                    {
                        ConsoleWindow.Print($"[StationpediaPlus] Page changed to: {currentPageKey}");
                        _lastPageKey = currentPageKey;
                        
                        // Start coroutine to add tooltips after UI settles
                        StartCoroutine(AddTooltipsAfterDelay(currentPageKey));
                    }
                }
                catch (Exception ex)
                {
                    // Don't spam errors - just log once
                    ConsoleWindow.Print($"[StationpediaPlus] Error in monitor: {ex.Message}");
                }
            }
        }

        // Unity lifecycle - called after Awake, when component is ready
        void Start()
        {
            ConsoleWindow.Print("[StationpediaPlus] Start() called");
        }
        
        // Initialize GUI styles (must be called from OnGUI)
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            // Create tooltip background texture - dark blue like Stationeers UI
            _tooltipBackground = new Texture2D(1, 1);
            _tooltipBackground.SetPixel(0, 0, new Color(0.05f, 0.1f, 0.15f, 0.95f));
            _tooltipBackground.Apply();
            
            // Create tooltip style
            _tooltipStyle = new GUIStyle(GUI.skin.box);
            _tooltipStyle.normal.background = _tooltipBackground;
            _tooltipStyle.normal.textColor = Color.white;
            _tooltipStyle.padding = new RectOffset(10, 10, 8, 8);
            _tooltipStyle.wordWrap = true;
            _tooltipStyle.richText = true;
            _tooltipStyle.fontSize = 14;
            _tooltipStyle.alignment = TextAnchor.UpperLeft;
            
            _stylesInitialized = true;
        }
        
        // Render tooltip using IMGUI
        void OnGUI()
        {
            try
            {
                if (!ShowTooltip || string.IsNullOrEmpty(CurrentTooltipText))
                    return;
                
                if (!_stylesInitialized)
                    InitializeStyles();
                
                // Calculate tooltip size and position
                Vector2 mousePos = Event.current.mousePosition;
                GUIContent content = new GUIContent(CurrentTooltipText);
                
                // Calculate size with word wrap
                float maxWidth = 350f;
                Vector2 size = _tooltipStyle.CalcSize(content);
                if (size.x > maxWidth)
                {
                    size.x = maxWidth;
                    size.y = _tooltipStyle.CalcHeight(content, maxWidth);
                }
                
                // Add padding
                size.x += 10;
                size.y += 10;
                
                // Position tooltip near mouse, offset to bottom-right
                float xPos = mousePos.x + 15;
                float yPos = mousePos.y + 15;
                
                // Keep tooltip on screen
                if (xPos + size.x > Screen.width)
                    xPos = mousePos.x - size.x - 10;
                if (yPos + size.y > Screen.height)
                    yPos = mousePos.y - size.y - 10;
                
                // Clamp to screen bounds
                xPos = Mathf.Max(5, xPos);
                yPos = Mathf.Max(5, yPos);
                
                // Draw tooltip box
                Rect tooltipRect = new Rect(xPos, yPos, size.x, size.y);
                
                // Draw border
                GUI.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange border
                GUI.Box(new Rect(tooltipRect.x - 2, tooltipRect.y - 2, tooltipRect.width + 4, tooltipRect.height + 4), "");
                GUI.color = Color.white;
                
                // Draw tooltip content
                GUI.Box(tooltipRect, CurrentTooltipText, _tooltipStyle);
            }
            catch (Exception ex)
            {
                // Silently handle GUI errors
            }
        }

        private void LoadDescriptions()
        {
            DeviceDatabase = new Dictionary<string, DeviceDescriptions>();
            GenericDescriptions = new GenericDescriptionsData();
            
            try
            {
                // HARDCODED PATH FOR DEVELOPMENT - always use the source file first
                string[] possiblePaths = new[]
                {
                    // Dev folder - hardcoded for hot-reload development
                    @"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json",
                    // Next to the executing assembly
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "descriptions.json"),
                };
                
                string jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    Log?.LogInfo($"Checking for descriptions at: {path}");
                    if (File.Exists(path))
                    {
                        jsonPath = path;
                        Log?.LogInfo($"Found descriptions.json at: {path}");
                        break;
                    }
                }
                
                if (jsonPath != null && File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var data = JsonConvert.DeserializeObject<DescriptionsRoot>(json);
                    
                    if (data?.devices != null)
                    {
                        foreach (var device in data.devices)
                        {
                            DeviceDatabase[device.deviceKey] = device;
                        }
                    }
                    
                    if (data?.genericDescriptions != null)
                    {
                        GenericDescriptions = data.genericDescriptions;
                        int logicCount = GenericDescriptions.logic?.Count ?? 0;
                        int slotCount = GenericDescriptions.slots?.Count ?? 0;
                        int memoryCount = GenericDescriptions.memory?.Count ?? 0;
                        ConsoleWindow.Print($"[StationpediaPlus] Loaded generic descriptions: {logicCount} logic, {slotCount} slots, {memoryCount} memory");
                    }
                    
                    ConsoleWindow.Print($"[StationpediaPlus] Loaded {DeviceDatabase.Count} device descriptions from JSON");
                }
                else
                {
                    ConsoleWindow.Print($"[StationpediaPlus] WARNING: descriptions.json not found at {jsonPath}");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[StationpediaPlus] Error loading descriptions: {ex.Message}");
            }
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                _harmony = new Harmony(HarmonyId);
                
                // Manual patching - more reliable than attribute-based patching for game assemblies
                var universalPageType = typeof(Assets.Scripts.UI.UniversalPage);
                
                // Patch PopulateLogicSlotInserts to condense slot numbers
                var populateLogicSlotInserts = universalPageType.GetMethod("PopulateLogicSlotInserts", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (populateLogicSlotInserts != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("PopulateLogicSlotInserts_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(populateLogicSlotInserts, postfix: new HarmonyMethod(postfix));
                    ConsoleWindow.Print("[StationpediaPlus] Patched PopulateLogicSlotInserts");
                }
                else
                {
                    ConsoleWindow.Print("[StationpediaPlus] ERROR: Could not find PopulateLogicSlotInserts method");
                }
                
                // Patch ChangeDisplay to add Special Behaviors section
                var changeDisplay = universalPageType.GetMethod("ChangeDisplay", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (changeDisplay != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("ChangeDisplay_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(changeDisplay, postfix: new HarmonyMethod(postfix));
                    ConsoleWindow.Print("[StationpediaPlus] Patched ChangeDisplay");
                }
                else
                {
                    ConsoleWindow.Print("[StationpediaPlus] ERROR: Could not find ChangeDisplay method");
                }
                
                ConsoleWindow.Print("[StationpediaPlus] Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[StationpediaPlus] Error applying Harmony patches: {ex.Message}");
            }
        }

        // Static version of LoadDescriptions for ScriptEngine path
        private static void LoadDescriptionsStatic()
        {
            DeviceDatabase = new Dictionary<string, DeviceDescriptions>();
            GenericDescriptions = new GenericDescriptionsData();
            
            try
            {
                // HARDCODED PATH FOR DEVELOPMENT - always use the source file
                string[] possiblePaths = new[]
                {
                    // Dev folder - hardcoded for hot-reload development
                    @"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json",
                    // Scripts folder (where we deploy to)
                    Path.Combine(Application.dataPath, "..", "BepInEx", "scripts", "descriptions.json"),
                    // Next to the executing assembly (for SLP)
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "descriptions.json"),
                    // Fallback - mod folder in My Games
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                        "My Games", "Stationeers", "mods", "StationpediaPlus", "descriptions.json")
                };
                
                string jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    string fullPath = Path.GetFullPath(path);
                    Log?.LogInfo($"Checking for descriptions at: {fullPath}");
                    if (File.Exists(fullPath))
                    {
                        jsonPath = fullPath;
                        Log?.LogInfo($"Found descriptions.json at: {fullPath}");
                        break;
                    }
                }
                
                if (jsonPath != null && File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var data = JsonConvert.DeserializeObject<DescriptionsRoot>(json);
                    
                    if (data?.devices != null)
                    {
                        foreach (var device in data.devices)
                        {
                            DeviceDatabase[device.deviceKey] = device;
                        }
                    }
                    
                    if (data?.genericDescriptions != null)
                    {
                        GenericDescriptions = data.genericDescriptions;
                        int logicCount = GenericDescriptions.logic?.Count ?? 0;
                        int slotCount = GenericDescriptions.slots?.Count ?? 0;
                        int memoryCount = GenericDescriptions.memory?.Count ?? 0;
                        Log?.LogInfo($"Loaded generic descriptions: {logicCount} logic, {slotCount} slots, {memoryCount} memory");
                    }
                    
                    Log?.LogInfo($"Loaded {DeviceDatabase.Count} device descriptions from JSON");
                    ConsoleWindow.Print($"[StationpediaPlus] Loaded {DeviceDatabase.Count} device descriptions");
                }
                else
                {
                    Log?.LogWarning($"descriptions.json not found at {jsonPath}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error loading descriptions: {ex}");
            }
        }

        // Static version of ApplyHarmonyPatches for ScriptEngine path
        private static void ApplyHarmonyPatchesStatic()
        {
            try
            {
                _harmonyStatic = new Harmony(HarmonyId + ".scriptengine");
                
                var universalPageType = typeof(Assets.Scripts.UI.UniversalPage);
                
                var populateLogicSlotInserts = universalPageType.GetMethod("PopulateLogicSlotInserts", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (populateLogicSlotInserts != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("PopulateLogicSlotInserts_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmonyStatic.Patch(populateLogicSlotInserts, postfix: new HarmonyMethod(postfix));
                    Log?.LogInfo("Patched PopulateLogicSlotInserts");
                }
                
                var changeDisplay = universalPageType.GetMethod("ChangeDisplay", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (changeDisplay != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("ChangeDisplay_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmonyStatic.Patch(changeDisplay, postfix: new HarmonyMethod(postfix));
                    Log?.LogInfo("Patched ChangeDisplay");
                }
                
                Log?.LogInfo("Harmony patches applied successfully");
                ConsoleWindow.Print("[StationpediaPlus] Harmony patches applied");
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error applying Harmony patches: {ex}");
            }
        }

        // Static coroutine for ScriptEngine path (uses static state tracking)
        private static string _lastPageKeyStatic = "";
        private static bool _stationpediaFoundStatic = false;
        
        private static IEnumerator MonitorStationpediaCoroutineStatic()
        {
            Log?.LogInfo("MonitorStationpediaCoroutineStatic started!");
            ConsoleWindow.Print("[StationpediaPlus] Monitor coroutine started!");
            
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                
                try
                {
                    if (Stationpedia.Instance == null)
                        continue;

                    if (!_stationpediaFoundStatic)
                    {
                        _stationpediaFoundStatic = true;
                        Log?.LogInfo("Stationpedia.Instance found!");
                    }

                    if (!Stationpedia.Instance.gameObject.activeInHierarchy)
                    {
                        _lastPageKeyStatic = "";
                        continue;
                    }

                    string currentPageKey = Stationpedia.CurrentPageKey;
                    
                    if (!string.IsNullOrEmpty(currentPageKey) && currentPageKey != _lastPageKeyStatic)
                    {
                        Log?.LogInfo($"Page changed to: {currentPageKey}");
                        ConsoleWindow.Print($"[StationpediaPlus] Page: {currentPageKey}");
                        _lastPageKeyStatic = currentPageKey;
                        
                        // Add tooltips after a short delay
                        if (_scriptEngineHost != null)
                        {
                            _scriptEngineHost.StartCoroutine(AddTooltipsAfterDelayStatic(currentPageKey));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"Error in monitor: {ex.Message}");
                }
            }
        }
        
        private static IEnumerator AddTooltipsAfterDelayStatic(string pageKey)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            AddTooltipsToLogicItemsStatic(pageKey);
        }
        
        private static void AddTooltipsToLogicItemsStatic(string pageKey)
        {
            try
            {
                var universalPage = Stationpedia.Instance?.UniversalPageRef;
                if (universalPage == null) return;

                int totalTooltipsAdded = 0;

                totalTooltipsAdded += AddTooltipsToCategoryStatic(universalPage.LogicContents, pageKey, "Logic");
                totalTooltipsAdded += AddTooltipsToCategoryStatic(universalPage.LogicSlotContents, pageKey, "LogicSlot");
                totalTooltipsAdded += AddTooltipsToCategoryStatic(universalPage.ModeContents, pageKey, "Mode");
                totalTooltipsAdded += AddTooltipsToCategoryStatic(universalPage.ConnectionContents, pageKey, "Connection");

                if (totalTooltipsAdded > 0)
                {
                    Log?.LogInfo($"Added {totalTooltipsAdded} tooltips for page {pageKey}");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error adding tooltips: {ex.Message}");
            }
        }
        
        private static int AddTooltipsToCategoryStatic(StationpediaCategory category, string pageKey, string categoryName)
        {
            if (category == null || category.Contents == null) return 0;

            int added = 0;
            foreach (Transform child in category.Contents)
            {
                SPDALogic logic = child.GetComponent<SPDALogic>();
                if (logic != null && logic.InfoValue != null)
                {
                    var existingTooltip = logic.GetComponent<SPDALogicTooltip>();
                    if (existingTooltip == null)
                    {
                        string logicName = logic.InfoValue.text;
                        var tooltip = logic.gameObject.AddComponent<SPDALogicTooltip>();
                        tooltip.Initialize(pageKey, logicName, categoryName);
                        _addedComponents.Add(tooltip);
                        added++;
                    }
                }
            }
            return added;
        }

        private IEnumerator AddTooltipsAfterDelay(string pageKey)
        {
            // Wait for UI to settle
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            AddTooltipsToLogicItems(pageKey);
        }

        private void AddTooltipsToLogicItems(string pageKey)
        {
            try
            {
                var universalPage = Stationpedia.Instance.UniversalPageRef;
                if (universalPage == null)
                {
                    return;
                }

                int totalTooltipsAdded = 0;

                totalTooltipsAdded += AddTooltipsToCategory(universalPage.LogicContents, pageKey, "Logic");
                totalTooltipsAdded += AddTooltipsToCategory(universalPage.LogicSlotContents, pageKey, "LogicSlot");
                totalTooltipsAdded += AddTooltipsToCategory(universalPage.ModeContents, pageKey, "Mode");
                totalTooltipsAdded += AddTooltipsToCategory(universalPage.ConnectionContents, pageKey, "Connection");
                
                // New category types - wrapped in try-catch to prevent one failure from blocking others
                try { totalTooltipsAdded += AddTooltipsToSlots(universalPage.SlotContents, pageKey); }
                catch (Exception ex) { ConsoleWindow.Print($"[StationpediaPlus] Error adding slot tooltips: {ex.Message}"); }
                
                try { totalTooltipsAdded += AddTooltipsToVersions(universalPage.StructureVersionContents, pageKey); }
                catch (Exception ex) { ConsoleWindow.Print($"[StationpediaPlus] Error adding version tooltips: {ex.Message}"); }
                
                try { totalTooltipsAdded += AddTooltipsToMemory(universalPage.LogicInstructions, pageKey); }
                catch (Exception ex) { ConsoleWindow.Print($"[StationpediaPlus] Error adding memory tooltips: {ex.Message}"); }

                if (totalTooltipsAdded > 0)
                {
                    ConsoleWindow.Print($"[StationpediaPlus] Added {totalTooltipsAdded} tooltips for page {pageKey}");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[StationpediaPlus] Error adding tooltips: {ex.Message}");
            }
        }

        private int AddTooltipsToCategory(StationpediaCategory category, string pageKey, string categoryName)
        {
            if (category == null || category.Contents == null)
            {
                return 0;
            }

            int added = 0;
            foreach (Transform child in category.Contents)
            {
                SPDALogic logic = child.GetComponent<SPDALogic>();
                if (logic != null && logic.InfoValue != null)
                {
                    var existingTooltip = logic.GetComponent<SPDALogicTooltip>();
                    if (existingTooltip == null)
                    {
                        string logicName = logic.InfoValue.text;
                        var tooltip = logic.gameObject.AddComponent<SPDALogicTooltip>();
                        tooltip.Initialize(pageKey, logicName, categoryName);
                        _addedComponents.Add(tooltip);
                        added++;
                    }
                }
            }
            return added;
        }

        private int AddTooltipsToSlots(StationpediaCategory category, string pageKey)
        {
            if (category == null || category.Contents == null)
            {
                return 0;
            }

            int added = 0;
            foreach (Transform child in category.Contents)
            {
                SPDASlot slot = child.GetComponent<SPDASlot>();
                if (slot != null && slot.SlotTitle != null && !string.IsNullOrEmpty(slot.SlotTitle.text))
                {
                    var existingTooltip = slot.GetComponent<SPDASlotTooltip>();
                    if (existingTooltip == null)
                    {
                        string slotName = slot.SlotTitle.text;
                        var tooltip = slot.gameObject.AddComponent<SPDASlotTooltip>();
                        tooltip.Initialize(pageKey, slotName);
                        _addedComponents.Add(tooltip);
                        added++;
                    }
                }
            }
            return added;
        }

        private int AddTooltipsToVersions(StationpediaCategory category, string pageKey)
        {
            if (category == null || category.Contents == null)
            {
                return 0;
            }

            int added = 0;
            foreach (Transform child in category.Contents)
            {
                SPDAVersion version = child.GetComponent<SPDAVersion>();
                if (version != null && version.BuildTitle != null && !string.IsNullOrEmpty(version.BuildTitle.text))
                {
                    var existingTooltip = version.GetComponent<SPDAVersionTooltip>();
                    if (existingTooltip == null)
                    {
                        string versionName = version.BuildTitle.text;
                        var tooltip = version.gameObject.AddComponent<SPDAVersionTooltip>();
                        tooltip.Initialize(pageKey, versionName);
                        _addedComponents.Add(tooltip);
                        added++;
                    }
                }
            }
            return added;
        }

        private int AddTooltipsToMemory(StationpediaCategory category, string pageKey)
        {
            if (category == null || category.Contents == null)
            {
                return 0;
            }

            int added = 0;
            foreach (Transform child in category.Contents)
            {
                SPDAGeneric generic = child.GetComponent<SPDAGeneric>();
                if (generic != null && generic.Header != null && !string.IsNullOrEmpty(generic.Header.text))
                {
                    var existingTooltip = generic.GetComponent<SPDAMemoryTooltip>();
                    if (existingTooltip == null)
                    {
                        string instructionName = generic.Header.text;
                        var tooltip = generic.gameObject.AddComponent<SPDAMemoryTooltip>();
                        tooltip.Initialize(pageKey, instructionName);
                        _addedComponents.Add(tooltip);
                        added++;
                    }
                }
            }
            return added;
        }

        // Static helper for tooltip component to get descriptions
        public static LogicDescription GetLogicDescription(string deviceKey, string logicTypeName)
        {
            // Clean up the logic type name (remove Unity rich text tags)
            string cleanName = CleanLogicTypeName(logicTypeName);
            
            // Try device-specific description first
            if (!string.IsNullOrEmpty(deviceKey) && DeviceDatabase != null && 
                DeviceDatabase.TryGetValue(deviceKey, out var device))
            {
                if (device.logicDescriptions != null && 
                    device.logicDescriptions.TryGetValue(cleanName, out var desc))
                {
                    return desc;
                }
            }
            
            // Fall back to generic description
            if (GenericDescriptions?.logic != null && GenericDescriptions.logic.TryGetValue(cleanName, out var genericDesc))
            {
                return new LogicDescription
                {
                    dataType = "Varies",
                    range = "Device-specific",
                    description = genericDesc
                };
            }
            
            return null;
        }

        // Static helper for slot descriptions (physical slot types like "Battery", "Plant")
        public static SlotDescription GetSlotDescription(string deviceKey, string slotName)
        {
            string cleanName = CleanLogicTypeName(slotName);
            
            // Try device-specific description first
            if (!string.IsNullOrEmpty(deviceKey) && DeviceDatabase != null && 
                DeviceDatabase.TryGetValue(deviceKey, out var device))
            {
                if (device.slotDescriptions != null && 
                    device.slotDescriptions.TryGetValue(cleanName, out var desc))
                {
                    return desc;
                }
            }
            
            // Fall back to generic slot type description (e.g., "Battery", "Gas Canister")
            if (GenericDescriptions?.slotTypes != null && GenericDescriptions.slotTypes.TryGetValue(cleanName, out var slotTypeDesc))
            {
                return new SlotDescription
                {
                    slotType = cleanName,
                    description = slotTypeDesc
                };
            }
            
            // Also check slots (for slot logic types like "Quantity", "Occupied")
            if (GenericDescriptions?.slots != null && GenericDescriptions.slots.TryGetValue(cleanName, out var genericDesc))
            {
                return new SlotDescription
                {
                    slotType = cleanName,
                    description = genericDesc
                };
            }
            
            return null;
        }
        
        // Static helper for mode descriptions (mode VALUES like "Outward", "Inward", "Mode0")
        public static ModeDescription GetModeDescription(string deviceKey, string modeValue)
        {
            string cleanName = CleanLogicTypeName(modeValue);
            
            // Try device-specific mode description first
            if (!string.IsNullOrEmpty(deviceKey) && DeviceDatabase != null && 
                DeviceDatabase.TryGetValue(deviceKey, out var device))
            {
                if (device.modeDescriptions != null && 
                    device.modeDescriptions.TryGetValue(cleanName, out var desc))
                {
                    return desc;
                }
            }
            
            // Fall back to generic mode description
            if (GenericDescriptions?.modes != null && GenericDescriptions.modes.TryGetValue(cleanName, out var genericDesc))
            {
                return new ModeDescription
                {
                    modeValue = cleanName,
                    description = genericDesc
                };
            }
            
            return null;
        }
        
        // Static helper for connection descriptions (connection types like "Data", "Power", "LiquidPipe")
        public static LogicDescription GetConnectionDescription(string deviceKey, string connectionType)
        {
            string cleanName = CleanLogicTypeName(connectionType);
            
            // Fall back to generic connection description (connections are universal)
            if (GenericDescriptions?.connections != null && GenericDescriptions.connections.TryGetValue(cleanName, out var genericDesc))
            {
                return new LogicDescription
                {
                    dataType = "Connection",
                    range = "N/A",
                    description = genericDesc
                };
            }
            
            return null;
        }

        // Static helper for slot logic descriptions (LogicSlot types like "Occupied", "Quantity", "OccupantHash")
        public static string GetSlotLogicDescription(string slotLogicType)
        {
            string cleanName = CleanLogicTypeName(slotLogicType);
            
            // Check genericDescriptions.slots for slot logic types
            if (GenericDescriptions?.slots != null && GenericDescriptions.slots.TryGetValue(cleanName, out var desc))
            {
                return desc;
            }
            
            return null;
        }

        // Static helper for version descriptions
        public static VersionDescription GetVersionDescription(string deviceKey, string versionName)
        {
            string cleanName = CleanLogicTypeName(versionName);
            
            // Try device-specific description first
            if (!string.IsNullOrEmpty(deviceKey) && DeviceDatabase != null && 
                DeviceDatabase.TryGetValue(deviceKey, out var device))
            {
                if (device.versionDescriptions != null && 
                    device.versionDescriptions.TryGetValue(cleanName, out var desc))
                {
                    return desc;
                }
            }
            
            // Fall back to generic description
            if (GenericDescriptions?.versions != null && GenericDescriptions.versions.TryGetValue(cleanName, out var genericDesc))
            {
                return new VersionDescription
                {
                    description = genericDesc
                };
            }
            
            return null;
        }

        // Static helper for memory instruction descriptions
        public static MemoryDescription GetMemoryDescription(string deviceKey, string instructionName)
        {
            string cleanName = CleanLogicTypeName(instructionName);
            
            // Try device-specific description first
            if (!string.IsNullOrEmpty(deviceKey) && DeviceDatabase != null && 
                DeviceDatabase.TryGetValue(deviceKey, out var device))
            {
                if (device.memoryDescriptions != null && 
                    device.memoryDescriptions.TryGetValue(cleanName, out var desc))
                {
                    return desc;
                }
            }
            
            // Fall back to generic description (now with full MemoryDescription objects)
            if (GenericDescriptions?.memory != null && GenericDescriptions.memory.TryGetValue(cleanName, out var genericDesc))
            {
                return genericDesc;
            }
            
            return null;
        }

        private static string CleanLogicTypeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            string result = System.Text.RegularExpressions.Regex.Replace(name, "<[^>]+>", "");
            return result.Trim();
        }
        
        void OnDestroy()
        {
            ConsoleWindow.Print("[StationpediaPlus] OnDestroy called - cleaning up...");
            
            // Stop all coroutines on this MonoBehaviour (including monitor coroutine)
            StopAllCoroutines();
            _monitorCoroutine = null;
            
            // Cleanup Harmony patches
            if (_harmony != null)
            {
                ConsoleWindow.Print("[StationpediaPlus] Unpatching Harmony patches...");
                _harmony.UnpatchSelf();
                _harmony = null;
            }
            
            // Cleanup tooltip components we added to game objects
            if (_addedComponents != null)
            {
                ConsoleWindow.Print($"[StationpediaPlus] Cleaning up {_addedComponents.Count} added components...");
                foreach (var component in _addedComponents)
                {
                    if (component != null)
                    {
                        try
                        {
                            Destroy(component);
                        }
                        catch (Exception ex)
                        {
                            ConsoleWindow.Print($"[StationpediaPlus] Error destroying component: {ex.Message}");
                        }
                    }
                }
                _addedComponents.Clear();
            }
            _addedComponents = new List<Component>();
            
            // Cleanup GameObjects we created (Special Behaviors categories, etc.)
            if (_createdGameObjects != null)
            {
                ConsoleWindow.Print($"[StationpediaPlus] Cleaning up {_createdGameObjects.Count} created GameObjects...");
                foreach (var go in _createdGameObjects)
                {
                    if (go != null)
                    {
                        try
                        {
                            Destroy(go);
                        }
                        catch (Exception ex)
                        {
                            ConsoleWindow.Print($"[StationpediaPlus] Error destroying GameObject: {ex.Message}");
                        }
                    }
                }
                _createdGameObjects.Clear();
            }
            _createdGameObjects = new List<GameObject>();
            
            // Cleanup tooltip state
            ShowTooltip = false;
            CurrentTooltipText = "";
            
            // Cleanup GUI resources
            if (_tooltipBackground != null)
            {
                Destroy(_tooltipBackground);
                _tooltipBackground = null;
            }
            _tooltipStyle = null;
            _stylesInitialized = false;
            
            // Reset tracking state
            _lastPageKey = "";
            _stationpediaFound = false;
            _initialized = false;
            
            // Clear static references
            Instance = null;
            DeviceDatabase = null;
            GenericDescriptions = null;
            
            ConsoleWindow.Print("[StationpediaPlus] Mod destroyed and cleaned up successfully");
        }
    }

    #region Data Classes
    
    [Serializable]
    public class DescriptionsRoot
    {
        public string version;
        public List<DeviceDescriptions> devices;
        public GenericDescriptionsData genericDescriptions;
    }

    [Serializable]
    public class DeviceDescriptions
    {
        public string deviceKey;
        public string displayName;
        public Dictionary<string, LogicDescription> logicDescriptions;
        public Dictionary<string, ModeDescription> modeDescriptions;
        public Dictionary<string, SlotDescription> slotDescriptions;
        public Dictionary<string, VersionDescription> versionDescriptions;
        public Dictionary<string, MemoryDescription> memoryDescriptions;
        public List<SpecialBehavior> specialBehaviors;
    }

    [Serializable]
    public class LogicDescription
    {
        public string dataType;
        public string range;
        public string description;
    }

    [Serializable]
    public class ModeDescription
    {
        public string modeValue;
        public string description;
    }

    [Serializable]
    public class SlotDescription
    {
        public string slotType;
        public string description;
    }

    [Serializable]
    public class VersionDescription
    {
        public string description;
    }

    [Serializable]
    public class MemoryDescription
    {
        public string opCode;
        public string parameters;
        public string description;
        public string byteLayout;
    }

    [Serializable]
    public class SpecialBehavior
    {
        public string title;
        public string description;
    }

    [Serializable]
    public class GenericDescriptionsData
    {
        public Dictionary<string, string> logic;
        public Dictionary<string, string> slotTypes;
        public Dictionary<string, string> slots;
        public Dictionary<string, string> modes;
        public Dictionary<string, string> versions;
        public Dictionary<string, string> connections;
        public Dictionary<string, MemoryDescription> memory;
    }
    
    #endregion

    #region Tooltip Component

    public class SPDALogicTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _deviceKey;
        private string _logicTypeName;
        private string _categoryName;
        private string _cachedTooltipText;
        private Coroutine _showCoroutine;
        private bool _isHovering;
        
        private const float HOVER_DELAY = 0.3f;

        public void Initialize(string deviceKey, string logicTypeName, string categoryName)
        {
            _deviceKey = deviceKey;
            _logicTypeName = logicTypeName;
            _categoryName = categoryName;
            _cachedTooltipText = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }

        private IEnumerator ShowTooltipAfterDelay()
        {
            yield return new WaitForSeconds(HOVER_DELAY);
            
            if (_isHovering)
            {
                string tooltipText = GetTooltipText();
                if (!string.IsNullOrEmpty(tooltipText))
                {
                    StationpediaPlusMod.CurrentTooltipText = tooltipText;
                    StationpediaPlusMod.ShowTooltip = true;
                }
            }
        }

        private string GetTooltipText()
        {
            if (_cachedTooltipText != null)
                return _cachedTooltipText;

            string cleanName = CleanLogicTypeName(_logicTypeName);
            
            if (_categoryName == "Mode")
            {
                var modeDesc = StationpediaPlusMod.GetModeDescription(_deviceKey, cleanName);
                if (modeDesc != null)
                {
                    _cachedTooltipText = FormatModeTooltip(cleanName, modeDesc);
                    return _cachedTooltipText;
                }
            }
            else if (_categoryName == "Connection")
            {
                var connDesc = StationpediaPlusMod.GetConnectionDescription(_deviceKey, cleanName);
                if (connDesc != null)
                {
                    _cachedTooltipText = FormatTooltip(cleanName, connDesc);
                    return _cachedTooltipText;
                }
            }
            else if (_categoryName == "LogicSlot")
            {
                var slotLogicDesc = StationpediaPlusMod.GetSlotLogicDescription(cleanName);
                if (slotLogicDesc != null)
                {
                    _cachedTooltipText = FormatSlotLogicTooltip(cleanName, slotLogicDesc);
                    return _cachedTooltipText;
                }
                var desc = StationpediaPlusMod.GetLogicDescription(_deviceKey, cleanName);
                if (desc != null)
                {
                    _cachedTooltipText = FormatTooltip(cleanName, desc);
                    return _cachedTooltipText;
                }
            }
            else
            {
                var desc = StationpediaPlusMod.GetLogicDescription(_deviceKey, cleanName);
                if (desc != null)
                {
                    _cachedTooltipText = FormatTooltip(cleanName, desc);
                    return _cachedTooltipText;
                }
            }
            
            _cachedTooltipText = $"<color=#FFA500><b>{cleanName}</b></color>\n\n" +
                                 $"<color=#AAAAAA>No detailed description available yet.</color>\n" +
                                 $"<color=#666666>Device: {_deviceKey}</color>";
            return _cachedTooltipText;
        }

        private string FormatTooltip(string cleanName, LogicDescription desc)
        {
            return $"<color=#FFA500><b>{cleanName}</b></color>\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"<b>Type:</b> {desc.dataType}   <b>Range:</b> {desc.range}\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"{desc.description}";
        }
        
        private string FormatModeTooltip(string cleanName, ModeDescription desc)
        {
            return $"<color=#9932CC><b>Mode: {cleanName}</b></color>\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"{desc.description}";
        }

        private string FormatSlotLogicTooltip(string cleanName, string description)
        {
            return $"<color=#FFA500><b>Slot: {cleanName}</b></color>\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"<b>Type:</b> LogicSlot   <b>Slots:</b> All readable slots\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"{description}";
        }

        private string CleanLogicTypeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return System.Text.RegularExpressions.Regex.Replace(name, "<[^>]+>", "").Trim();
        }

        private void OnDisable()
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }
    }

    #endregion

    #region Slot Tooltip Component

    public class SPDASlotTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _deviceKey;
        private string _slotName;
        private string _cachedTooltipText;
        private Coroutine _showCoroutine;
        private bool _isHovering;
        
        private const float HOVER_DELAY = 0.3f;

        public void Initialize(string deviceKey, string slotName)
        {
            _deviceKey = deviceKey;
            _slotName = slotName;
            _cachedTooltipText = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }

        private IEnumerator ShowTooltipAfterDelay()
        {
            yield return new WaitForSeconds(HOVER_DELAY);
            
            if (_isHovering)
            {
                string tooltipText = GetTooltipText();
                if (!string.IsNullOrEmpty(tooltipText))
                {
                    StationpediaPlusMod.CurrentTooltipText = tooltipText;
                    StationpediaPlusMod.ShowTooltip = true;
                }
            }
        }

        private string GetTooltipText()
        {
            if (_cachedTooltipText != null)
                return _cachedTooltipText;

            string cleanName = CleanName(_slotName);
            var desc = StationpediaPlusMod.GetSlotDescription(_deviceKey, cleanName);
            
            if (desc != null)
            {
                _cachedTooltipText = FormatTooltip(cleanName, desc);
                return _cachedTooltipText;
            }
            
            _cachedTooltipText = $"<color=#00BFFF><b>{cleanName}</b></color>\n\n" +
                                 $"<color=#AAAAAA>No slot description available yet.</color>";
            return _cachedTooltipText;
        }

        private string FormatTooltip(string cleanName, SlotDescription desc)
        {
            return $"<color=#00BFFF><b>Slot: {cleanName}</b></color>\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"<b>Type:</b> {desc.slotType}\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"{desc.description}";
        }

        private string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return System.Text.RegularExpressions.Regex.Replace(name, "<[^>]+>", "").Trim();
        }

        private void OnDisable()
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }
    }

    #endregion

    #region Version Tooltip Component

    public class SPDAVersionTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _deviceKey;
        private string _versionName;
        private string _cachedTooltipText;
        private Coroutine _showCoroutine;
        private bool _isHovering;
        
        private const float HOVER_DELAY = 0.3f;

        public void Initialize(string deviceKey, string versionName)
        {
            _deviceKey = deviceKey;
            _versionName = versionName;
            _cachedTooltipText = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }

        private IEnumerator ShowTooltipAfterDelay()
        {
            yield return new WaitForSeconds(HOVER_DELAY);
            
            if (_isHovering)
            {
                string tooltipText = GetTooltipText();
                if (!string.IsNullOrEmpty(tooltipText))
                {
                    StationpediaPlusMod.CurrentTooltipText = tooltipText;
                    StationpediaPlusMod.ShowTooltip = true;
                }
            }
        }

        private string GetTooltipText()
        {
            if (_cachedTooltipText != null)
                return _cachedTooltipText;

            string cleanName = CleanName(_versionName);
            var desc = StationpediaPlusMod.GetVersionDescription(_deviceKey, cleanName);
            
            if (desc != null)
            {
                _cachedTooltipText = FormatTooltip(cleanName, desc);
                return _cachedTooltipText;
            }
            
            _cachedTooltipText = $"<color=#90EE90><b>{cleanName}</b></color>\n\n" +
                                 $"<color=#AAAAAA>No version description available yet.</color>";
            return _cachedTooltipText;
        }

        private string FormatTooltip(string cleanName, VersionDescription desc)
        {
            return $"<color=#90EE90><b>Version: {cleanName}</b></color>\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"{desc.description}";
        }

        private string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return System.Text.RegularExpressions.Regex.Replace(name, "<[^>]+>", "").Trim();
        }

        private void OnDisable()
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }
    }

    #endregion

    #region Memory Tooltip Component

    public class SPDAMemoryTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _deviceKey;
        private string _instructionName;
        private string _cachedTooltipText;
        private Coroutine _showCoroutine;
        private bool _isHovering;
        
        private const float HOVER_DELAY = 0.3f;

        public void Initialize(string deviceKey, string instructionName)
        {
            _deviceKey = deviceKey;
            _instructionName = instructionName;
            _cachedTooltipText = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }

        private IEnumerator ShowTooltipAfterDelay()
        {
            yield return new WaitForSeconds(HOVER_DELAY);
            
            if (_isHovering)
            {
                string tooltipText = GetTooltipText();
                if (!string.IsNullOrEmpty(tooltipText))
                {
                    StationpediaPlusMod.CurrentTooltipText = tooltipText;
                    StationpediaPlusMod.ShowTooltip = true;
                }
            }
        }

        private string GetTooltipText()
        {
            if (_cachedTooltipText != null)
                return _cachedTooltipText;

            string cleanName = CleanName(_instructionName);
            var desc = StationpediaPlusMod.GetMemoryDescription(_deviceKey, cleanName);
            
            if (desc != null)
            {
                _cachedTooltipText = FormatTooltip(cleanName, desc);
                return _cachedTooltipText;
            }
            
            _cachedTooltipText = $"<color=#FF69B4><b>{cleanName}</b></color>\n\n" +
                                 $"<color=#AAAAAA>No instruction description available yet.</color>";
            return _cachedTooltipText;
        }

        private string FormatTooltip(string cleanName, MemoryDescription desc)
        {
            string paramText = string.IsNullOrEmpty(desc.parameters) ? "none" : desc.parameters;
            string tooltip = $"<color=#FF69B4><b>Instruction: {cleanName}</b></color>\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"<b>OpCode:</b> {desc.opCode}\n" +
                   $"<b>Parameters:</b> {paramText}\n" +
                   $"<color=#888888>─────────────────────</color>\n" +
                   $"{desc.description}";
            
            if (!string.IsNullOrEmpty(desc.byteLayout))
            {
                tooltip += $"\n<color=#888888>─────────────────────</color>\n" +
                          $"<color=#AAAAAA><b>Byte Layout:</b></color>\n{desc.byteLayout}";
            }
            
            return tooltip;
        }

        private string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            string cleaned = System.Text.RegularExpressions.Regex.Replace(name, "<[^>]+>", "").Trim();
            
            int opCodeIndex = cleaned.IndexOf(" OP_CODE:");
            if (opCodeIndex > 0)
            {
                cleaned = cleaned.Substring(0, opCodeIndex).Trim();
            }
            
            return cleaned;
        }

        private void OnDisable()
        {
            _isHovering = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            StationpediaPlusMod.ShowTooltip = false;
            StationpediaPlusMod.CurrentTooltipText = "";
        }
    }

    #endregion

    #region Harmony Patches

    public static class HarmonyPatches
    {
        public static void PopulateLogicSlotInserts_Postfix(Assets.Scripts.UI.UniversalPage __instance)
        {
            try
            {
                var slotContents = __instance.LogicSlotContents;
                if (slotContents == null || slotContents.Contents == null) return;

                foreach (Transform child in slotContents.Contents)
                {
                    var spdalogic = child.GetComponent<Assets.Scripts.UI.SPDALogic>();
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
            }
        }
        
        private static bool IsSlotNumberList(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != ',' && c != ' ')
                    return false;
            }
            return true;
        }

        public static void ChangeDisplay_Postfix(Assets.Scripts.UI.UniversalPage __instance, StationpediaPage page)
        {
            try
            {
                // Unity-safe null checks - destroyed objects are "fake null"
                if ((object)__instance == null || !__instance) return;
                if ((object)page == null) return;
                
                // Check if the Content transform is valid (not destroyed)
                UnityEngine.Transform contentTransform;
                try
                {
                    contentTransform = __instance.Content;
                    if ((object)contentTransform == null || !contentTransform) return;
                    // Try to access gameObject to verify it's not destroyed
                    var testGO = contentTransform.gameObject;
                    if ((object)testGO == null || !testGO) return;
                }
                catch
                {
                    return; // Content is destroyed
                }
                
                string pageKey = page.Key;
                if (string.IsNullOrEmpty(pageKey)) return;

                if (!StationpediaPlusMod.DeviceDatabase.TryGetValue(pageKey, out var deviceDesc)) return;
                if (deviceDesc.specialBehaviors == null || deviceDesc.specialBehaviors.Count == 0) return;

                var stationpedia = Stationpedia.Instance;
                if ((object)stationpedia == null || !stationpedia) return;
                
                var categoryPrefab = stationpedia.CategoryPrefab;
                
                if ((object)categoryPrefab == null || !categoryPrefab)
                {
                    return;
                }

                // Create the collapsible category container
                StationpediaCategory category = UnityEngine.Object.Instantiate<StationpediaCategory>(
                    categoryPrefab, contentTransform);
                if ((object)category == null || !category) return;
                if ((object)category.Title == null || !category.Title) return;
                if ((object)category.Contents == null || !category.Contents) return;
                
                category.Title.text = "Special Behaviors";
                // Don't add to _createdGameObjects - we'll add this to CreatedCategories
                // and the game will handle cleanup when the page changes

                // Create a single container for all behaviors with dark background
                var containerGO = new UnityEngine.GameObject("SpecialBehaviorsContainer");
                containerGO.transform.SetParent(category.Contents, false);
                // Child of category, will be destroyed when category is destroyed
                
                // Add dark background image
                var containerImage = containerGO.AddComponent<UnityEngine.UI.Image>();
                containerImage.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.8f);
                
                // Add layout element for proper sizing
                var containerLayout = containerGO.AddComponent<UnityEngine.UI.LayoutElement>();
                containerLayout.flexibleWidth = 1;
                
                // Add content size fitter to expand based on text
                var containerFitter = containerGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                containerFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
                containerFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                
                // Add vertical layout for padding
                var containerVLayout = containerGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                containerVLayout.padding = new UnityEngine.RectOffset(15, 15, 10, 10);
                containerVLayout.childForceExpandWidth = true;
                containerVLayout.childForceExpandHeight = false;
                containerVLayout.childControlWidth = true;
                containerVLayout.childControlHeight = true;
                
                // Build the text content
                var sb = new System.Text.StringBuilder();
                bool first = true;
                foreach (var behavior in deviceDesc.specialBehaviors)
                {
                    if (!first) sb.AppendLine();
                    sb.AppendLine($"<color=#FFD700><b>{behavior.title}</b></color>");
                    sb.Append(behavior.description);
                    first = false;
                }

                // Create text object
                var textGO = new UnityEngine.GameObject("SpecialBehaviorsText");
                textGO.transform.SetParent(containerGO.transform, false);
                // Child of category, will be destroyed when category is destroyed
                
                var textComponent = textGO.AddComponent<TMPro.TextMeshProUGUI>();
                textComponent.text = sb.ToString();
                textComponent.fontSize = 16;
                textComponent.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 1f);
                textComponent.enableWordWrapping = true;
                textComponent.overflowMode = TMPro.TextOverflowModes.Overflow;
                textComponent.richText = true;
                
                // Make text expand to fit content
                var textLayout = textGO.AddComponent<UnityEngine.UI.LayoutElement>();
                textLayout.flexibleWidth = 1;

                category.Contents.gameObject.SetActive(true);
                
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(category.Contents);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.Content);
                
                __instance.CreatedCategories.Add(category);
            }
            catch (Exception)
            {
            }
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
    }

    #endregion
}


