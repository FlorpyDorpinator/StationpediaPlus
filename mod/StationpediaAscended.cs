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
using StationpediaAscended.Data;
using StationpediaAscended.Tooltips;
using StationpediaAscended.Patches;

namespace StationpediaAscended
{
    /// <summary>
    /// Stationpedia Ascended - Enhanced Stationpedia with tooltips and operational details
    /// Per apro: BepInPlugin attribute + BaseUnityPlugin base class is required for ScriptEngine hot-reload
    /// </summary>
    [BepInPlugin("StationpediaAscended", "Stationpedia Ascended", "1.0.0")]
    public class StationpediaAscendedMod : BaseUnityPlugin
    {
        #region Constants & Static References
        
        public const string HarmonyId = "com.stationpediaascended.mod";
        
        // BepInEx logger - will show in BepInEx console
        internal static ManualLogSource Log;
        
        // Static reference for tooltip component access
        public static StationpediaAscendedMod Instance { get; private set; }
        
        // Description databases
        public static Dictionary<string, DeviceDescriptions> DeviceDatabase { get; private set; }
        public static GenericDescriptionsData GenericDescriptions { get; private set; }
        
        #endregion

        #region Instance Fields
        
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
        
        // Custom icon sprite (internal so nested HarmonyPatches class can access it)
        internal static Sprite _customIconSprite;
        
        // Track created GameObjects and components for cleanup
        internal static List<GameObject> _createdGameObjects = new List<GameObject>();
        internal static List<Component> _addedComponents = new List<Component>();
        
        // Track if we've already initialized (prevent double-init from both Awake and OnLoaded)
        private bool _initialized = false;
        
        // Reference to ScriptEngine loader's MonoBehaviour for coroutines when loaded via ScriptEngine
        private static MonoBehaviour _scriptEngineHost;
        
        #endregion

        #region ScriptEngine Hot-Reload Support
        
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
                    ConsoleWindow.Print("[Stationpedia Ascended] Stationpedia regenerated!");
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
            ConsoleWindow.Print("[Stationpedia Ascended] Initialized via ScriptEngine!");
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
            ConsoleWindow.Print("[Stationpedia Ascended] Cleaned up via ScriptEngine");
        }
        
        // Static Harmony instance for ScriptEngine path
        private static Harmony _harmonyStatic;

        // Called by ScriptEngineLoader to trigger a reload
        public void TriggerReload()
        {
            Log?.LogInfo("TriggerReload called - reinitializing mod...");
            ConsoleWindow.Print("[Stationpedia Ascended] TriggerReload called - reinitializing...");
            
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
        
        #endregion

        #region Unity Lifecycle Methods
        
        /// <summary>
        /// Called by Unity/BepInEx when plugin loads (works for both SLP and ScriptEngine)
        /// </summary>
        void Awake()
        {
            this.Logger.LogInfo(
                $"Awake Stationpedia Ascended"
            );
            
            try
            {
                // Initialize BepInEx logger
                Log = BepInEx.Logging.Logger.CreateLogSource("Stationpedia Ascended");
                Log.LogInfo("========================================");
                Log.LogInfo("=== StationpediaAscended Awake() CALLED ===");
                Log.LogInfo("========================================");
                
                ConsoleWindow.Print("[Stationpedia Ascended] === Awake() ===");
                
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
                ConsoleWindow.Print($"[Stationpedia Ascended] ERROR in Awake: {ex.Message}");
            }
        }

        // Initialize everything - called from Awake
        private void Initialize()
        {
            if (_initialized)
            {
                Debug.Log("[Stationpedia Ascended] Initialize called but already initialized, skipping");
                ConsoleWindow.Print("[Stationpedia Ascended] Initialize called but already initialized, skipping");
                return;
            }
            
            Debug.Log("[Stationpedia Ascended] Initialize STARTING...");
            ConsoleWindow.Print("[Stationpedia Ascended] Initialize called");
            
            // Load custom icon
            Debug.Log("[Stationpedia Ascended] Loading custom icon...");
            LoadCustomIcon();
            
            // Load descriptions from JSON file
            Debug.Log("[Stationpedia Ascended] Loading descriptions...");
            LoadDescriptions();
            
            // Apply Harmony patches
            Debug.Log("[Stationpedia Ascended] Applying Harmony patches...");
            ApplyHarmonyPatches();
            
            // On hot-reload, skip Regenerate() - it fails because Stationpedia's internal state
            // has references to destroyed Unity objects. Pages will update when viewed.
            // We detect hot-reload by checking if Stationpedia.Instance exists (it wouldn't during cold start).
            Log?.LogInfo($"Stationpedia.Instance = {Stationpedia.Instance}");
            Debug.Log($"[Stationpedia Ascended] Stationpedia.Instance = {Stationpedia.Instance}");
            if (Stationpedia.Instance != null)
            {
                // Don't call Regenerate() - it causes NullReferenceException during hot-reload
                // because the Stationpedia's CreatedCategories list has destroyed object references.
                // Instead, just close and re-open the Stationpedia to refresh, or navigate to a new page.
                Log?.LogInfo(">>> Skipping Regenerate() to avoid hot-reload crash <<<");
                Debug.Log("[Stationpedia Ascended] Skipping Regenerate() - pages will refresh when navigated");
                ConsoleWindow.Print("[Stationpedia Ascended] Patches applied! Navigate to a page to see changes.");
            }
            else
            {
                Log?.LogWarning("Stationpedia.Instance is NULL - cannot regenerate yet");
                Debug.Log("[Stationpedia Ascended] Stationpedia.Instance is null - patches will apply later");
                ConsoleWindow.Print("[Stationpedia Ascended] Stationpedia not yet initialized - patches will apply on first open");
            }
            
            // Start the monitoring coroutine
            Debug.Log("[Stationpedia Ascended] Starting monitoring coroutine...");
            _monitorCoroutine = StartCoroutine(MonitorStationpediaCoroutine());
            ConsoleWindow.Print("[Stationpedia Ascended] Started monitoring coroutine");
            
            _initialized = true;
            Debug.Log("[Stationpedia Ascended] Initialize COMPLETE!");
            ConsoleWindow.Print("[Stationpedia Ascended] Initialized successfully!");
        }

        // Coroutine-based monitoring (more reliable than Update() for ModBehaviour)
        private IEnumerator MonitorStationpediaCoroutine()
        {
            ConsoleWindow.Print("[Stationpedia Ascended] MonitorStationpediaCoroutine started!");
            
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
                        ConsoleWindow.Print("[Stationpedia Ascended] Stationpedia.Instance found! Now monitoring for page changes.");
                        
                        // Change the window title to Stationpedia Ascended
                        try
                        {
                            var titleGO = Stationpedia.Instance.StationpediaTitleText;
                            if (titleGO != null)
                            {
                                var titleText = titleGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                                if (titleText != null)
                                {
                                    titleText.text = "Stationpedia <color=#FF7A18>Ascended</color>";
                                    ConsoleWindow.Print("[Stationpedia Ascended] Changed window title");
                                }
                                
                                // The icon is likely a sibling of the title text, not a child
                                // Search in the parent (header bar) for an Image component
                                var headerParent = titleGO.transform.parent;
                                if (headerParent != null)
                                {
                                    ConsoleWindow.Print($"[Stationpedia Ascended] Searching header parent: {headerParent.name}");
                                    
                                    // Check for layout components on the parent
                                    var hlg = headerParent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                                    if (hlg != null)
                                    {
                                        ConsoleWindow.Print($"[Stationpedia Ascended] Parent has HorizontalLayoutGroup - spacing: {hlg.spacing}, childForceExpand: {hlg.childForceExpandWidth}x{hlg.childForceExpandHeight}");
                                    }
                                    
                                    // Log all children to help debug
                                    for (int i = 0; i < headerParent.childCount; i++)
                                    {
                                        var child = headerParent.GetChild(i);
                                        var img = child.GetComponent<UnityEngine.UI.Image>();
                                        ConsoleWindow.Print($"[Stationpedia Ascended]   Child[{i}]: {child.name}, hasImage={img != null}");
                                        
                                        // Look for an image that might be the book icon (not the title text itself)
                                        if (img != null && child.gameObject != titleGO && _customIconSprite != null)
                                        {
                                            // Check if this looks like an icon (small, near the title)
                                            var rt = child.GetComponent<RectTransform>();
                                            if (rt != null)
                                            {
                                                ConsoleWindow.Print($"[Stationpedia Ascended]   -> Size: {rt.sizeDelta}, sprite: {img.sprite?.name ?? "null"}");
                                            }
                                            
                                            // Replace the first Image we find that's not the background
                                            if (img.sprite != null && !img.sprite.name.ToLower().Contains("background"))
                                            {
                                                ConsoleWindow.Print($"[Stationpedia Ascended] BEFORE: {rt?.sizeDelta}");
                                                
                                                // Replace the sprite
                                                img.sprite = _customIconSprite;
                                                img.preserveAspect = true; // Keep aspect ratio
                                                
                                                // Check for layout element on this specific child - CREATE ONE if missing
                                                var layoutElement = child.GetComponent<UnityEngine.UI.LayoutElement>();
                                                if (layoutElement == null)
                                                {
                                                    layoutElement = child.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                                                    ConsoleWindow.Print($"[Stationpedia Ascended] Added LayoutElement to control size in HorizontalLayoutGroup");
                                                }
                                                
                                                // Force the LayoutElement to constrain the size
                                                layoutElement.preferredWidth = 28;
                                                layoutElement.preferredHeight = 28;
                                                layoutElement.minWidth = 28;
                                                layoutElement.minHeight = 28;
                                                layoutElement.flexibleWidth = 0;
                                                layoutElement.flexibleHeight = 0;
                                                ConsoleWindow.Print($"[Stationpedia Ascended] Set LayoutElement sizes to 28x28");
                                                
                                                // Force back to the original small square size that the book icon used
                                                if (rt != null)
                                                {
                                                    rt.sizeDelta = new Vector2(28, 28);
                                                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 28);
                                                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 28);
                                                }
                                                
                                                ConsoleWindow.Print($"[Stationpedia Ascended] AFTER: {rt?.sizeDelta} - Replaced icon in {child.name}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex) 
                        { 
                            ConsoleWindow.Print($"[Stationpedia Ascended] Error setting title/icon: {ex.Message}");
                        }
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
                        ConsoleWindow.Print($"[Stationpedia Ascended] Page changed to: {currentPageKey}");
                        _lastPageKey = currentPageKey;
                        
                        // Start coroutine to add tooltips after UI settles
                        StartCoroutine(AddTooltipsAfterDelay(currentPageKey));
                    }
                }
                catch (Exception ex)
                {
                    // Don't spam errors - just log once
                    ConsoleWindow.Print($"[Stationpedia Ascended] Error in monitor: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unity lifecycle - called after Awake, when component is ready
        /// </summary>
        void Start()
        {
            ConsoleWindow.Print("[Stationpedia Ascended] Start() called");
        }
        
        #endregion

        #region GUI Rendering
        
        /// <summary>
        /// Initialize GUI styles (must be called from OnGUI)
        /// </summary>
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
            catch (Exception)
            {
                // Silently handle GUI errors
            }
        }
        
        #endregion

        #region Resource Loading
        
        /// <summary>
        /// Load the custom phoenix icon from file
        /// </summary>
        private void LoadCustomIcon()
        {
            try
            {
                // Possible paths for the custom icon
                string[] possiblePaths = new[]
                {
                    // Dev folder - hardcoded for hot-reload development
                    @"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\icon.png",
                    // Next to the executing assembly
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png"),
                    // BepInEx scripts folder
                    Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "icon.png"),
                };
                
                string iconPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        iconPath = path;
                        ConsoleWindow.Print($"[Stationpedia Ascended] Found icon at: {path}");
                        break;
                    }
                }
                
                if (iconPath == null)
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] Custom icon not found (icon.png) - using default");
                    return;
                }
                
                // Load the image from file
                byte[] imageData = File.ReadAllBytes(iconPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                
                if (ImageConversion.LoadImage(texture, imageData))
                {
                    // Create a sprite from the texture
                    // Use standard 100 pixelsPerUnit - the Image component will scale it to fit the rect
                    _customIconSprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    ConsoleWindow.Print($"[Stationpedia Ascended] Loaded custom icon: {texture.width}x{texture.height}");
                }
                else
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] Failed to load icon.png - invalid image format");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error loading custom icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Load device and generic descriptions from JSON file
        /// </summary>
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
                    @"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\descriptions.json",
                    // Next to the executing assembly (BepInEx scripts folder)
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "descriptions.json"),
                    // BepInEx scripts folder via Application.dataPath
                    Path.Combine(Application.dataPath, "..", "BepInEx", "scripts", "descriptions.json"),
                };
                
                string jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Checking: {path}");
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        jsonPath = path;
                        ConsoleWindow.Print($"[Stationpedia Ascended] Found descriptions.json at: {path}");
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
                        ConsoleWindow.Print($"[Stationpedia Ascended] Loaded generic descriptions: {logicCount} logic, {slotCount} slots, {memoryCount} memory");
                    }
                    
                    ConsoleWindow.Print($"[Stationpedia Ascended] Loaded {DeviceDatabase.Count} device descriptions from JSON");
                }
                else
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] WARNING: descriptions.json not found at {jsonPath}");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error loading descriptions: {ex.Message}");
            }
        }
        
        #endregion

        #region Harmony Patching
        
        /// <summary>
        /// Apply all Harmony patches to the game
        /// </summary>
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
                    ConsoleWindow.Print("[Stationpedia Ascended] Patched PopulateLogicSlotInserts");
                }
                else
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] ERROR: Could not find PopulateLogicSlotInserts method");
                }
                
                // Patch ChangeDisplay to add Operational Details section
                var changeDisplay = universalPageType.GetMethod("ChangeDisplay", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (changeDisplay != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("ChangeDisplay_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(changeDisplay, postfix: new HarmonyMethod(postfix));
                    ConsoleWindow.Print("[Stationpedia Ascended] Patched ChangeDisplay");
                }
                else
                {
                    ConsoleWindow.Print("[Stationpedia Ascended] ERROR: Could not find ChangeDisplay method");
                }
                
                // Patch OnDrag on Stationpedia to work properly in main menu (skip ClampToScreen which crashes)
                var stationpediaType = typeof(Stationpedia);
                var onDrag = stationpediaType.GetMethod("OnDrag", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (onDrag != null)
                {
                    var prefix = typeof(HarmonyPatches).GetMethod("Stationpedia_OnDrag_Prefix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(onDrag, prefix: new HarmonyMethod(prefix));
                    ConsoleWindow.Print("[Stationpedia Ascended] Patched Stationpedia.OnDrag - window now draggable in main menu");
                }
                
                // Also patch OnBeginDrag to capture offset
                var onBeginDrag = stationpediaType.GetMethod("OnBeginDrag", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (onBeginDrag != null)
                {
                    var prefix = typeof(HarmonyPatches).GetMethod("Stationpedia_OnBeginDrag_Prefix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(onBeginDrag, prefix: new HarmonyMethod(prefix));
                }
                
                // NOTE: We tried to fix the scrollbar visibility bug but it broke scrolling.
                // The base game has verticalScrollbarVisibility set to AutoHide and the content
                // size isn't being calculated correctly. This is a base game bug we can't easily fix.
                
                // Register console command to center Stationpedia
                RegisterConsoleCommands();
                
                ConsoleWindow.Print("[Stationpedia Ascended] Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error applying Harmony patches: {ex.Message}");
            }
        }
        
        #endregion

        #region Console Commands
        
        /// <summary>
        /// Register custom console commands
        /// </summary>
        private void RegisterConsoleCommands()
        {
            try
            {
                // Check if command already exists (for F6 reload)
                if (Util.Commands.CommandLine.CommandsMap.ContainsKey("stationpediacenter"))
                {
                    return; // Already registered
                }
                
                // Register "stationpediacenter" command to center the Stationpedia window
                Util.Commands.CommandLine.AddCommand("stationpediacenter", 
                    new Util.Commands.BasicCommand(CenterStationpediaCommand, 
                        "Centers the Stationpedia window on screen", null, false));
                ConsoleWindow.Print("[Stationpedia Ascended] Registered console command: stationpediacenter");
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error registering console commands: {ex.Message}");
            }
        }
        
        private static string CenterStationpediaCommand(string[] args)
        {
            try
            {
                var stationpedia = Stationpedia.Instance;
                if (stationpedia == null)
                {
                    return "Stationpedia instance not found";
                }
                
                var rectTransform = stationpedia.RectTransform;
                if (rectTransform == null)
                {
                    return "Stationpedia RectTransform not found";
                }
                
                // Center on screen
                rectTransform.localPosition = Vector3.zero;
                
                return "Stationpedia centered on screen";
            }
            catch (Exception ex)
            {
                return $"Error centering Stationpedia: {ex.Message}";
            }
        }
        
        #endregion

        #region Static ScriptEngine Methods
        
        /// <summary>
        /// Static version of LoadDescriptions for ScriptEngine path
        /// </summary>
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
                    @"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\descriptions.json",
                    // Scripts folder (where we deploy to)
                    Path.Combine(Application.dataPath, "..", "BepInEx", "scripts", "descriptions.json"),
                    // Next to the executing assembly
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "descriptions.json"),
                    // Fallback - mod folder in My Games
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                        "My Games", "Stationeers", "mods", "StationpediaAscended", "descriptions.json")
                };
                
                string jsonPath = null;
                foreach (var path in possiblePaths)
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    string fullPath = Path.GetFullPath(path);
                    ConsoleWindow.Print($"[Stationpedia Ascended] Checking: {fullPath}");
                    if (File.Exists(fullPath))
                    {
                        jsonPath = fullPath;
                        ConsoleWindow.Print($"[Stationpedia Ascended] Found descriptions.json at: {fullPath}");
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
                    ConsoleWindow.Print($"[Stationpedia Ascended] Loaded {DeviceDatabase.Count} device descriptions");
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
                ConsoleWindow.Print("[Stationpedia Ascended] Harmony patches applied");
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
            ConsoleWindow.Print("[Stationpedia Ascended] Monitor coroutine started!");
            
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
                        ConsoleWindow.Print($"[Stationpedia Ascended] Page: {currentPageKey}");
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
        
        #endregion

        #region Tooltip Adding
        
        /// <summary>
        /// Add tooltips after UI has settled
        /// </summary>
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
                catch (Exception ex) { ConsoleWindow.Print($"[Stationpedia Ascended] Error adding slot tooltips: {ex.Message}"); }
                
                try { totalTooltipsAdded += AddTooltipsToVersions(universalPage.StructureVersionContents, pageKey); }
                catch (Exception ex) { ConsoleWindow.Print($"[Stationpedia Ascended] Error adding version tooltips: {ex.Message}"); }
                
                try { totalTooltipsAdded += AddTooltipsToMemory(universalPage.LogicInstructions, pageKey); }
                catch (Exception ex) { ConsoleWindow.Print($"[Stationpedia Ascended] Error adding memory tooltips: {ex.Message}"); }

                if (totalTooltipsAdded > 0)
                {
                    ConsoleWindow.Print($"[Stationpedia Ascended] Added {totalTooltipsAdded} tooltips for page {pageKey}");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Error adding tooltips: {ex.Message}");
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
        
        #endregion

        #region Description Lookup Helpers
        
        /// <summary>
        /// Get logic type description for a device
        /// </summary>
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
            ConsoleWindow.Print("[Stationpedia Ascended] OnDestroy called - cleaning up...");
            
            // Stop all coroutines on this MonoBehaviour (including monitor coroutine)
            StopAllCoroutines();
            _monitorCoroutine = null;
            
            // Cleanup Harmony patches
            if (_harmony != null)
            {
                ConsoleWindow.Print("[Stationpedia Ascended] Unpatching Harmony patches...");
                _harmony.UnpatchSelf();
                _harmony = null;
            }
            
            // Cleanup tooltip components we added to game objects
            if (_addedComponents != null)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Cleaning up {_addedComponents.Count} added components...");
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
                            ConsoleWindow.Print($"[Stationpedia Ascended] Error destroying component: {ex.Message}");
                        }
                    }
                }
                _addedComponents.Clear();
            }
            _addedComponents = new List<Component>();
            
            // Cleanup GameObjects we created (Operational Details categories, etc.)
            if (_createdGameObjects != null)
            {
                ConsoleWindow.Print($"[Stationpedia Ascended] Cleaning up {_createdGameObjects.Count} created GameObjects...");
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
                            ConsoleWindow.Print($"[Stationpedia Ascended] Error destroying GameObject: {ex.Message}");
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
            
            ConsoleWindow.Print("[Stationpedia Ascended] Mod destroyed and cleaned up successfully");
        }
    }
}
