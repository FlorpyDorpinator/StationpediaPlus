using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.Objects;
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
using StationpediaAscended.UI;
using StationpediaAscended.UI.StationPlanner;
using StationpediaAscended.Diagnostics;

namespace StationpediaAscended
{
    /// <summary>
    /// Stationpedia Ascended - Enhanced Stationpedia with tooltips and operational details
    /// Per apro: BepInPlugin attribute + BaseUnityPlugin base class is required for ScriptEngine hot-reload
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class StationpediaAscendedMod : BaseUnityPlugin
    {
        #region Constants & Static References
        
        // Plugin metadata
        public const string PluginGuid = "com.florpydorp.stationpediaascended";
        public const string PluginName = "Stationpedia Ascended";
        public const string PluginVersion = "0.3.0";
        
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
        
        // Custom icons for collapsible sections
        internal static Sprite _iconExpanded;   // Icon shown when category is expanded
        internal static Sprite _iconCollapsed;  // Icon shown when category is collapsed
        
        // Track created GameObjects and components for cleanup
        internal static List<GameObject> _createdGameObjects = new List<GameObject>();
        internal static List<Component> _addedComponents = new List<Component>();
        
        // Track if we've already initialized (prevent double-init from both Awake and OnLoaded)
        private bool _initialized = false;
        
        // Reference to ScriptEngine loader's MonoBehaviour for coroutines when loaded via ScriptEngine
        private static MonoBehaviour _scriptEngineHost;
        
        // Track if we've already hidden unwanted items in Stationpedia
        private static bool _hiddenItemsPopulated = false;
        
        // Station Planner button reference
        private static UnityEngine.UI.Button _stationPlannerButton = null;
        private static Sprite _stationPlannerIconSprite = null;
        
        #endregion

        #region ScriptEngine Hot-Reload Support
        
        /// <summary>
        /// Called by ScriptEngineLoader when mod is loaded via F6 hot-reload (no SLP instance)
        /// </summary>
        public static void InitializeFromScriptEngine(MonoBehaviour host, ManualLogSource log)
        {
            Log = log;
            _scriptEngineHost = host;
            
            // Clean up any existing tooltip components from previous loads
            CleanupExistingTooltips();
            
            // Load descriptions and apply patches
            LoadDescriptionsStatic();
            ApplyHarmonyPatchesStatic();
            
            // Reset page tracking so monitor will re-add tooltips after reload
            _lastPageKeyStatic = "";
            _stationpediaFoundStatic = false;
            
            // Regenerate Stationpedia if available
            if (Stationpedia.Instance != null)
            {
                try
                {
                    Stationpedia.Regenerate();
                }
                catch (Exception ex)
                {
                    Log.LogError($"Failed to regenerate Stationpedia: {ex}");
                }
            }
            
            // Start monitoring coroutine
            host.StartCoroutine(MonitorStationpediaCoroutineStatic());
            
            ConsoleWindow.Print($"[Stationpedia Ascended] v{PluginVersion} initialized via ScriptEngine!");
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
            
            // Clear our tracking lists (they may have stale references)
            _addedComponents.Clear();
            _createdGameObjects.Clear();
        }
        
        /// <summary>
        /// Called by ScriptEngineLoader OnDestroy to cleanup
        /// </summary>
        public static void CleanupFromScriptEngine()
        {
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
            
            ConsoleWindow.Print("[Stationpedia Ascended] Cleaned up");
        }
        
        // Static Harmony instance for ScriptEngine path
        private static Harmony _harmonyStatic;

        // Called by ScriptEngineLoader to trigger a reload
        public void TriggerReload()
        {
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
            try
            {
                // Initialize BepInEx logger
                Log = BepInEx.Logging.Logger.CreateLogSource("Stationpedia Ascended");
                
                // Log version on startup
                ConsoleWindow.Print($"[Stationpedia Ascended] v{PluginVersion} loading...");
                
                // Clean up any existing tooltip components from previous loads (for ScriptEngine hot-reload)
                CleanupExistingTooltips();
                
                // Reset initialization flag for hot-reload
                _initialized = false;
                
                Instance = this;
                Initialize();
            }
            catch (Exception ex)
            {
                Log?.LogError($"ERROR in Awake: {ex}");
                ConsoleWindow.Print($"[Stationpedia Ascended] ERROR: {ex.Message}");
            }
        }

        // Initialize everything - called from Awake
        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            
            // Load custom icon
            LoadCustomIcon();
            
            // Load custom expand/collapse icons
            LoadCustomIcons();
            
            // Load descriptions from JSON file
            LoadDescriptions();
            
            // Apply Harmony patches
            ApplyHarmonyPatches();
            
            // Start the monitoring coroutine
            _monitorCoroutine = StartCoroutine(MonitorStationpediaCoroutine());
            
            // Initialize UI Asset Inspector (debug tool)
            UIAssetInspector.Initialize();
            
            _initialized = true;
            ConsoleWindow.Print($"[Stationpedia Ascended] v{PluginVersion} initialized successfully!");
        }

        // Coroutine-based monitoring (more reliable than Update() for ModBehaviour)
        private IEnumerator MonitorStationpediaCoroutine()
        {
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
                        
                        // Hide unwanted items from Stationpedia searches (burnt cables, wreckage, etc.)
                        PopulateHiddenItems();
                        
                        // Change the window title to Stationpedia Ascended and make it clickable
                        try
                        {
                            var titleGO = Stationpedia.Instance.StationpediaTitleText;
                            if (titleGO != null)
                            {
                                _headerTitleObject = titleGO;
                                var titleText = titleGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                                if (titleText != null)
                                {
                                    _headerTitleText = titleText;
                                    UpdateHeaderAppearance(); // Set initial appearance based on mode
                                }
                                
                                // Find and store the icon in the header (for Ascended mode styling)
                                var headerParent = titleGO.transform.parent;
                                if (headerParent != null)
                                {
                                    for (int i = 0; i < headerParent.childCount; i++)
                                    {
                                        var child = headerParent.GetChild(i);
                                        var img = child.GetComponent<UnityEngine.UI.Image>();
                                        
                                        if (img != null && child.gameObject != titleGO)
                                        {
                                            if (img.sprite != null && !img.sprite.name.ToLower().Contains("background"))
                                            {
                                                // Store reference to the icon
                                                _headerIconImage = img;
                                                
                                                // Store original sprite so we can restore it in vanilla mode
                                                if (_originalHeaderIconSprite == null)
                                                {
                                                    _originalHeaderIconSprite = img.sprite;
                                                }
                                                
                                                // Only replace sprite if in Ascended mode
                                                if (!UI.VanillaModeManager.IsVanillaMode && _customIconSprite != null)
                                                {
                                                    img.sprite = _customIconSprite;
                                                    img.preserveAspect = true;
                                                }
                                                
                                                // Add LayoutElement if missing to control size
                                                var layoutElement = child.GetComponent<UnityEngine.UI.LayoutElement>();
                                                if (layoutElement == null)
                                                {
                                                    layoutElement = child.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                                                }
                                                
                                                // Force size to 32x32 (15% larger than original 28x28)
                                                layoutElement.preferredWidth = 32;
                                                layoutElement.preferredHeight = 32;
                                                layoutElement.minWidth = 32;
                                                layoutElement.minHeight = 32;
                                                layoutElement.flexibleWidth = 0;
                                                layoutElement.flexibleHeight = 0;
                                                
                                                var rt = child.GetComponent<RectTransform>();
                                                if (rt != null)
                                                {
                                                    rt.sizeDelta = new Vector2(32, 32);
                                                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32);
                                                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32);
                                                }
                                                
                                                break; // Found the icon
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex) 
                        { 
                            Log?.LogWarning($"Error setting title/icon: {ex.Message}");
                        }
                        
                        // Setup header as clickable toggle for Ascended mode (easter egg)
                        try
                        {
                            SetupHeaderToggle();
                        }
                        catch (Exception ex)
                        {
                            Log?.LogWarning($"Error setting up header toggle: {ex.Message}");
                        }
                        
                        // Add Station Planner button to Stationpedia header
                        try
                        {
                            SetupStationPlannerButton();
                        }
                        catch (Exception ex)
                        {
                            Log?.LogWarning($"Error setting up Station Planner button: {ex.Message}");
                        }
                        
                        // Initialize Station Planner
                        try
                        {
                            StationPlannerWindow.Initialize();
                        }
                        catch (Exception ex)
                        {
                            Log?.LogWarning($"Error initializing Station Planner: {ex.Message}");
                        }
                        
                        // Initialize search system early for instant first search
                        try
                        {
                            Patches.SearchPatches.InitializeSearchSystem(Stationpedia.Instance);
                        }
                        catch (Exception ex)
                        {
                            Log?.LogWarning($"Error initializing search system: {ex.Message}");
                        }
                        
                        // Initialize home page layout (Survival Manual & Game Mechanics buttons)
                        try
                        {
                            UI.HomePageLayoutManager.Initialize();
                        }
                        catch (Exception ex)
                        {
                            Log?.LogWarning($"Error initializing home page layout: {ex.Message}");
                        }
                        
                        // Register Survival Manual page
                        try
                        {
                            Data.SurvivalManualLoader.RegisterSurvivalManualPage();
                        }
                        catch (Exception ex)
                        {
                            Log?.LogWarning($"Error registering Survival Manual: {ex.Message}");
                        }
                        
                        // Register Daylight Sensor Guide page
                        try
                        {
                            Data.DaylightSensorGuideLoader.RegisterDaylightSensorGuidePage();
                        }
                        catch (Exception ex)
                        {
                            Log?.LogWarning($"Error registering Daylight Sensor Guide: {ex.Message}");
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
                        _lastPageKey = currentPageKey;
                        
                        // Start coroutine to add tooltips after UI settles
                        StartCoroutine(AddTooltipsAfterDelay(currentPageKey));
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"Error in monitor: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unity lifecycle - called after Awake, when component is ready
        /// </summary>
        void Start()
        {
            // Unity lifecycle - component ready
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
        /// Load the custom phoenix icon from file or embedded resource
        /// </summary>
        private void LoadCustomIcon()
        {
            try
            {
                byte[] imageData = null;
                
#if !DEBUG
                // In Release builds, try embedded resource first
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("StationpediaAscended.phoenix-icon.png"))
                {
                    if (stream != null)
                    {
                        imageData = new byte[stream.Length];
                        stream.Read(imageData, 0, imageData.Length);
                    }
                }
#endif
                
                // If no embedded resource (Debug build or resource not found), try file paths
                if (imageData == null)
                {
                    var possiblePaths = new List<string>();
                    
#if DEBUG
                    // Dev folder - only included in Debug builds for hot-reload development
                    possiblePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\images\phoenix-icon.png");
#endif
                    // Images subfolder next to the executing assembly
                    possiblePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "images", "phoenix-icon.png"));
                    // BepInEx scripts/images folder
                    possiblePaths.Add(Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "images", "phoenix-icon.png"));
                    
                    foreach (var path in possiblePaths)
                    {
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            imageData = File.ReadAllBytes(path);
                            break;
                        }
                    }
                }
                
                if (imageData == null)
                {
                    return;
                }
                
                // Load the image
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                
                if (ImageConversion.LoadImage(texture, imageData))
                {
                    // Create a sprite from the texture
                    _customIconSprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }
                else
                {
                    Log?.LogWarning("Failed to load phoenix-icon.png - invalid image format");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error loading custom icon: {ex.Message}");
            }
        }

        /// <summary>
        /// Load custom icons for expand/collapse buttons from images folder
        /// </summary>
        private void LoadCustomIcons()
        {
            try
            {
                // Paths to check for custom icons
                var basePaths = new List<string>();
                
#if DEBUG
                basePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\images");
#endif
                basePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "images"));
                basePaths.Add(Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "images"));
                
                foreach (var basePath in basePaths)
                {
                    if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath)) continue;
                    
                    // Try custom icon files first, then fall back to phoenix/book icons
                    string expandedPath = Path.Combine(basePath, "icon_expanded.png");
                    string collapsedPath = Path.Combine(basePath, "icon_collapsed.png");
                    string phoenixPath = Path.Combine(basePath, "phoenix-icon.png");
                    string bookClosedPath = Path.Combine(basePath, "Book-Closed.png");
                    
                    // Expanded icon: try icon_expanded.png, then phoenix-icon.png
                    if (_iconExpanded == null)
                    {
                        if (File.Exists(expandedPath))
                            _iconExpanded = LoadSpriteFromFile(expandedPath);
                        else if (File.Exists(phoenixPath))
                            _iconExpanded = LoadSpriteFromFile(phoenixPath);
                    }
                    
                    // Collapsed icon: try icon_collapsed.png, then Book-Closed.png
                    if (_iconCollapsed == null)
                    {
                        if (File.Exists(collapsedPath))
                            _iconCollapsed = LoadSpriteFromFile(collapsedPath);
                        else if (File.Exists(bookClosedPath))
                            _iconCollapsed = LoadSpriteFromFile(bookClosedPath);
                    }
                    
                    if (_iconExpanded != null && _iconCollapsed != null)
                        break;
                }
                
                // Fallback: Use phoenix icon for both states if custom icons not found
                if (_iconExpanded == null && _customIconSprite != null)
                {
                    _iconExpanded = _customIconSprite;
                }
                if (_iconCollapsed == null && _customIconSprite != null)
                {
                    _iconCollapsed = _customIconSprite;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error loading custom icons: {ex.Message}");
            }
        }

        // Track header references for easter egg toggle
        private static GameObject _headerTitleObject = null;
        private static TMPro.TextMeshProUGUI _headerTitleText = null;
        private static UnityEngine.UI.Image _headerIconImage = null;
        private static UnityEngine.UI.Button _headerButton = null;
        private static Sprite _originalHeaderIconSprite = null;  // Store original sprite for vanilla mode
        
        /// <summary>
        /// Make the header title/icon clickable to toggle Ascended mode (easter egg)
        /// </summary>
        private void SetupHeaderToggle()
        {
            if (_headerTitleObject == null) return;
            
            // Find the header parent that contains title and icon
            var headerParent = _headerTitleObject.transform.parent;
            if (headerParent == null) return;
            
            // Check if button already exists
            var existingButton = headerParent.GetComponent<UnityEngine.UI.Button>();
            if (existingButton != null)
            {
                _headerButton = existingButton;
                _headerButton.onClick.RemoveAllListeners();
                _headerButton.onClick.AddListener(OnHeaderClicked);
                return;
            }
            
            // Make the header parent clickable by adding a Button component
            // First ensure it has a Graphic for raycast
            var graphic = headerParent.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic == null)
            {
                var img = headerParent.gameObject.AddComponent<UnityEngine.UI.Image>();
                img.color = new Color(0, 0, 0, 0); // Fully transparent
                img.raycastTarget = true;
                graphic = img;
            }
            
            _headerButton = headerParent.gameObject.AddComponent<UnityEngine.UI.Button>();
            _headerButton.targetGraphic = graphic;
            
            // Set up button colors (subtle highlight)
            var colors = _headerButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.8f, 1f);  // Subtle warm highlight
            colors.pressedColor = new Color(1f, 0.7f, 0.5f, 1f);
            colors.selectedColor = Color.white;
            _headerButton.colors = colors;
            
            // Add click handler
            _headerButton.onClick.AddListener(OnHeaderClicked);
        }
        
        /// <summary>
        /// Handle header click - toggle Ascended mode (easter egg)
        /// </summary>
        private void OnHeaderClicked()
        {
            UI.VanillaModeManager.Toggle();
            UpdateHeaderAppearance();
            
            // Force refresh the current page to apply new styling
            RefreshCurrentPage();
        }
        
        /// <summary>
        /// Update header appearance based on current mode
        /// </summary>
        private void UpdateHeaderAppearance()
        {
            if (_headerTitleText != null)
            {
                // Always show "Ascended" in orange for both modes
                _headerTitleText.text = "Stationpedia <color=#FF7A18>Ascended</color>";
            }
            
            if (_headerIconImage != null && _customIconSprite != null)
            {
                // Always use custom icon, just different tint colors
                _headerIconImage.sprite = _customIconSprite;
                _headerIconImage.preserveAspect = true;
                
                // Make icon 20% larger
                _headerIconImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                
                if (UI.VanillaModeManager.IsVanillaMode)
                {
                    // Vanilla mode - white/normal color
                    _headerIconImage.color = Color.white;
                }
                else
                {
                    // Ascended mode - orange tint
                    _headerIconImage.color = new Color(1f, 0.6f, 0.2f, 1f);
                }
            }
        }
        
        // Track if we've already created the button (persistent across hot reloads)
        private static bool _stationPlannerButtonCreated = false;
        
        /// <summary>
        /// Setup the Station Planner button next to the ToggleMouse button
        /// </summary>
        private void SetupStationPlannerButton()
        {
            var stationpedia = Stationpedia.Instance;
            if (stationpedia == null) return;
            
            // Find the ToggleMouse to position our button next to it
            var toggleMouse = stationpedia.ToggleMouse;
            if (toggleMouse == null)
            {
                Log?.LogWarning("ToggleMouse not found, cannot create Station Planner button");
                return;
            }
            
            // Get the parent of the toggle to add our button as a sibling
            var parent = toggleMouse.transform.parent;
            if (parent == null) return;
            
            // Check if button already exists by searching in hierarchy
            var existingButton = parent.Find("StationPlannerButton");
            if (existingButton != null)
            {
                _stationPlannerButton = existingButton.GetComponent<UnityEngine.UI.Button>();
                _stationPlannerButtonCreated = true;
                return;
            }
            
            // Also check our tracked state
            if (_stationPlannerButton != null && _stationPlannerButtonCreated) return;
            
            // Load the Station Planner icon (Book-ClosedONE.png)
            if (_stationPlannerIconSprite == null)
            {
                _stationPlannerIconSprite = LoadImageFromModFolder("Book-ClosedONE.png");
            }
            
            // Create button GameObject
            var buttonGO = new GameObject("StationPlannerButton");
            buttonGO.transform.SetParent(parent, false);
            
            // Position to LEFT of mouse toggle
            // The header layout appears to use higher index = further LEFT
            // So we set our index to mouseIndex + 1 to appear LEFT of the mouse icon
            int mouseIndex = toggleMouse.transform.GetSiblingIndex();
            buttonGO.transform.SetSiblingIndex(mouseIndex + 1);
            Log?.LogInfo($"Station Planner button: mouseIndex={mouseIndex}, set to {mouseIndex + 1}");
            
            // Add RectTransform with same size as mouse toggle
            var rt = buttonGO.AddComponent<RectTransform>();
            var mouseToggleRect = toggleMouse.GetComponent<RectTransform>();
            if (mouseToggleRect != null)
            {
                rt.sizeDelta = mouseToggleRect.sizeDelta;
            }
            else
            {
                rt.sizeDelta = new Vector2(32, 32);
            }
            
            // Add Image for button icon
            var image = buttonGO.AddComponent<UnityEngine.UI.Image>();
            if (_stationPlannerIconSprite != null)
            {
                image.sprite = _stationPlannerIconSprite;
                image.preserveAspect = true;
            }
            else
            {
                // Fallback if icon not found - use a simple book emoji/color
                image.color = new Color(0.9f, 0.8f, 0.6f, 1f);
            }
            
            // Add Button component
            _stationPlannerButton = buttonGO.AddComponent<UnityEngine.UI.Button>();
            _stationPlannerButton.targetGraphic = image;
            
            // Setup button colors
            var colors = _stationPlannerButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.7f, 1f);  // Warm highlight
            colors.pressedColor = new Color(0.9f, 0.7f, 0.5f, 1f);
            colors.selectedColor = Color.white;
            _stationPlannerButton.colors = colors;
            
            // Add click handler to toggle Station Planner
            _stationPlannerButton.onClick.AddListener(() => {
                StationPlannerWindow.Toggle();
            });
            
            _stationPlannerButtonCreated = true;
            Log?.LogInfo("Station Planner button added to Stationpedia header (to LEFT of mouse toggle)");
        }
        
        /// <summary>
        /// Force refresh the current Stationpedia page to apply mode changes
        /// </summary>
        private void RefreshCurrentPage()
        {
            try
            {
                // Force page to re-render by navigating away and back
                if (Stationpedia.Instance != null && !string.IsNullOrEmpty(Stationpedia.CurrentPageKey))
                {
                    string currentKey = Stationpedia.CurrentPageKey;
                    _lastPageKey = "";  // Reset tracking so our patches will re-run
                    
                    // Navigate to Home briefly then back to force re-render
                    Stationpedia.Instance.SetPage("Home", false);
                    StartCoroutine(DelayedSetPage(currentKey));
                }
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Error refreshing page: {ex.Message}");
            }
        }
        
        private IEnumerator DelayedSetPage(string pageKey)
        {
            yield return null;  // Wait one frame
            if (Stationpedia.Instance != null)
            {
                Stationpedia.Instance.SetPage(pageKey, false);
            }
        }

        /// <summary>
        /// Helper to load a sprite from a PNG file
        /// </summary>
        private static Sprite LoadSpriteFromFile(string path)
        {
            try
            {
                byte[] imageData = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                
                if (ImageConversion.LoadImage(texture, imageData))
                {
                    return Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Failed to load sprite from {path}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Load a sprite with 9-slice borders for proper scaling (used for rounded rectangles)
        /// </summary>
        /// <param name="relativePath">Path relative to images folder</param>
        /// <param name="borderSize">Size of the border in pixels (same on all sides)</param>
        public static Sprite LoadSlicedSprite(string relativePath, int borderSize = 10)
        {
            var basePaths = new List<string>();
            
#if DEBUG
            basePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\images");
#endif
            basePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "images"));
            basePaths.Add(Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "images"));
            
            foreach (var basePath in basePaths)
            {
                if (string.IsNullOrEmpty(basePath)) continue;
                string fullPath = Path.Combine(basePath, relativePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        byte[] imageData = File.ReadAllBytes(fullPath);
                        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        texture.filterMode = FilterMode.Bilinear;
                        
                        if (ImageConversion.LoadImage(texture, imageData))
                        {
                            // Create sprite with 9-slice border
                            // Border: left, bottom, right, top (in pixels)
                            Vector4 border = new Vector4(borderSize, borderSize, borderSize, borderSize);
                            
                            return Sprite.Create(
                                texture,
                                new Rect(0, 0, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f),
                                100f,
                                0,
                                SpriteMeshType.FullRect,
                                border
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Log?.LogWarning($"Failed to load sliced sprite from {fullPath}: {ex.Message}");
                    }
                }
            }
            
            Log?.LogWarning($"Sliced sprite not found: {relativePath}");
            return null;
        }
        
        // Cached rounded background sprite
        private static Sprite _roundedBgSprite = null;
        private static Sprite _windowBgSprite = null;
        
        /// <summary>
        /// Get the rounded background sprite (cached)
        /// </summary>
        public static Sprite GetRoundedBackgroundSprite()
        {
            if (_roundedBgSprite == null)
            {
                // Try to load rounded-bg.png with 9-slice borders
                // Assuming the rounded corners are about 10-15 pixels
                _roundedBgSprite = LoadSlicedSprite("rounded-bg.png", 12);
            }
            return _roundedBgSprite;
        }
        
        /// <summary>
        /// Get the fancy window background sprite (Inv-window-bg.png) - cached
        /// This is the game's native window background with nice borders
        /// </summary>
        public static Sprite GetWindowBackgroundSprite()
        {
            if (_windowBgSprite == null)
            {
                // Load Inv-window-bg.png with 9-slice borders
                // The border area is typically around 8-12 pixels
                _windowBgSprite = LoadSlicedSprite("Inv-window-bg.png", 10);
            }
            return _windowBgSprite;
        }

        /// <summary>
        /// Public method to load a sprite from the mod's images folder
        /// </summary>
        public static Sprite LoadImageFromModFolder(string relativePath)
        {
            var basePaths = new List<string>();
            
#if DEBUG
            basePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\images");
#endif
            basePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "images"));
            basePaths.Add(Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "images"));
            
            foreach (var basePath in basePaths)
            {
                if (string.IsNullOrEmpty(basePath)) continue;
                string fullPath = Path.Combine(basePath, relativePath);
                if (File.Exists(fullPath))
                {
                    return LoadSpriteFromFile(fullPath);
                }
            }
            
            Log?.LogWarning($"Image not found: {relativePath}");
            return null;
        }

        /// <summary>
        /// Get the full file path for a file in the mod's images folder.
        /// Returns null if file doesn't exist.
        /// </summary>
        public static string GetImageFilePath(string relativePath)
        {
            var basePaths = new List<string>();
            
#if DEBUG
            basePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\images");
#endif
            basePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "images"));
            basePaths.Add(Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "images"));
            
            foreach (var basePath in basePaths)
            {
                if (string.IsNullOrEmpty(basePath)) continue;
                string fullPath = Path.Combine(basePath, relativePath);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            
            Log?.LogWarning($"File not found: {relativePath}");
            return null;
        }

        /// <summary>
        /// Load device and generic descriptions from JSON file or embedded resource
        /// </summary>
        private void LoadDescriptions()
        {
            DeviceDatabase = new Dictionary<string, DeviceDescriptions>();
            GenericDescriptions = new GenericDescriptionsData();
            
            try
            {
                string json = null;
                
#if !DEBUG
                // In Release builds, try embedded resource first
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("StationpediaAscended.descriptions.json"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            json = reader.ReadToEnd();
                        }
                    }
                }
#endif
                
                // If no embedded resource (Debug build or resource not found), try file paths
                if (json == null)
                {
                    var possiblePaths = new List<string>();
                    
#if DEBUG
                    // Dev folder - only included in Debug builds for hot-reload development
                    possiblePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\descriptions.json");
#endif
                    // Next to the executing assembly (works for both SLP mods folder and BepInEx scripts)
                    possiblePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "descriptions.json"));
                    // BepInEx scripts folder via Application.dataPath
                    possiblePaths.Add(Path.Combine(Application.dataPath, "..", "BepInEx", "scripts", "descriptions.json"));
                    
                    foreach (var path in possiblePaths)
                    {
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            json = File.ReadAllText(path);
                            break;
                        }
                    }
                }
                
                if (json != null)
                {
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
                    }
                    
                    // Load custom guides from JSON
                    if (data?.guides != null && data.guides.Count > 0)
                    {
                        Data.JsonGuideLoader.LoadGuides(data);
                    }
                    
                    // Load game mechanics from JSON
                    if (data?.mechanics != null && data.mechanics.Count > 0)
                    {
                        Data.JsonMechanicsLoader.LoadMechanics(data);
                    }
                }
                else
                {
                    Log?.LogWarning($"descriptions.json not found");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error loading descriptions: {ex.Message}");
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
                }
                else
                {
                    Log?.LogError("Could not find PopulateLogicSlotInserts method");
                }
                
                // Patch ChangeDisplay to add Operational Details section
                var changeDisplay = universalPageType.GetMethod("ChangeDisplay", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (changeDisplay != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("ChangeDisplay_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(changeDisplay, postfix: new HarmonyMethod(postfix));
                }
                else
                {
                    Log?.LogError("Could not find ChangeDisplay method");
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
                
                // Patch ClearPreviousSearch to clean up our category headers
                var clearPreviousSearch = stationpediaType.GetMethod("ClearPreviousSearch", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (clearPreviousSearch != null)
                {
                    var postfix = typeof(SearchPatches).GetMethod("ClearPreviousSearch_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(clearPreviousSearch, postfix: new HarmonyMethod(postfix));
                }
                
                // Patch SetPage to handle Game Mechanics navigation
                var setPage = stationpediaType.GetMethod("SetPage", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (setPage != null)
                {
                    var prefix = typeof(HarmonyPatches).GetMethod("Stationpedia_SetPage_Prefix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(setPage, prefix: new HarmonyMethod(prefix));
                }
                
                // Patch SetPageGuides to modify button layout
                var setPageGuides = stationpediaType.GetMethod("SetPageGuides", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (setPageGuides != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("Stationpedia_SetPageGuides_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(setPageGuides, postfix: new HarmonyMethod(postfix));
                }
                
                // Patch SetPageLore to clear orphaned items and modify button layout
                var setPageLore = stationpediaType.GetMethod("SetPageLore", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (setPageLore != null)
                {
                    var prefix = typeof(HarmonyPatches).GetMethod("Stationpedia_SetPageLore_Prefix", 
                        BindingFlags.Public | BindingFlags.Static);
                    var postfix = typeof(HarmonyPatches).GetMethod("Stationpedia_SetPageLore_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmony.Patch(setPageLore, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
                }
                
                // Register console command to center Stationpedia
                RegisterConsoleCommands();
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error applying Harmony patches: {ex.Message}");
            }
        }
        
        #endregion

        #region Hidden Items Population
        
        /// <summary>
        /// Add burnt cables, wreckage, and other debris items to HiddenInPedia 
        /// so vanilla search doesn't return them at all.
        /// Also removes their pages from StationpediaPages since they were already registered.
        /// </summary>
        private static void PopulateHiddenItems()
        {
            if (_hiddenItemsPopulated) return;
            _hiddenItemsPopulated = true;
            
            try
            {
                var hiddenPedia = Stationpedia.DataHandler?.HiddenInPedia;
                if (hiddenPedia == null)
                {
                    Log?.LogWarning("HiddenInPedia dictionary not available");
                    return;
                }
                
                int hiddenCount = 0;
                int prefabCount = 0;
                var pagesToRemove = new List<string>();
                
                // Iterate through all prefabs and hide unwanted items
                foreach (Thing thing in Prefab.AllPrefabs)
                {
                    if (thing == null) continue;
                    prefabCount++;
                    
                    string prefabName = thing.PrefabName ?? "";
                    string displayName = thing.DisplayName ?? "";
                    string displayNameLower = displayName.ToLowerInvariant();
                    string prefabNameLower = prefabName.ToLowerInvariant();
                    
                    bool shouldHide = false;
                    
                    // Hide burnt items
                    if (displayNameLower.StartsWith("burnt") || prefabNameLower.Contains("burnt"))
                        shouldHide = true;
                    
                    // Hide ruptured items
                    if (displayNameLower.Contains("ruptured") || prefabNameLower.Contains("ruptured"))
                        shouldHide = true;
                    
                    // Hide CableRuptured class items
                    if (thing is Assets.Scripts.Objects.Electrical.CableRuptured)
                        shouldHide = true;
                    
                    // Hide wreckage items
                    if (displayNameLower.Contains("wreckage") || prefabNameLower.Contains("wreckage"))
                        shouldHide = true;
                    
                    if (shouldHide)
                    {
                        hiddenPedia[prefabName] = true;
                        thing.HideInStationpedia = true;
                        hiddenCount++;
                        
                        // Track the page key to remove from StationpediaPages
                        pagesToRemove.Add("Thing" + prefabName);
                    }
                }
                
                // Remove pages that were already registered
                int pagesRemoved = 0;
                foreach (string pageKey in pagesToRemove)
                {
                    // Find and remove the page from StationpediaPages
                    var pageToRemove = Stationpedia.StationpediaPages.Find(p => p.Key == pageKey);
                    if (pageToRemove != null)
                    {
                        Stationpedia.StationpediaPages.Remove(pageToRemove);
                        pagesRemoved++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error populating hidden items: {ex.Message}");
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
                var commandsMap = Util.Commands.CommandLine.CommandsMap;
                
                // Register "stationpediacenter" command to center the Stationpedia window
                if (!commandsMap.ContainsKey("stationpediacenter"))
                {
                    Util.Commands.CommandLine.AddCommand("stationpediacenter", 
                        new Util.Commands.BasicCommand(CenterStationpediaCommand, 
                            "Centers the Stationpedia window on screen", null, false));
                }
                
                // Register "spda_dumpkeys" command to export all page keys
                if (!commandsMap.ContainsKey("spda_dumpkeys"))
                {
                    Util.Commands.CommandLine.AddCommand("spda_dumpkeys", 
                        new Util.Commands.BasicCommand(DumpPageKeysCommand, 
                            "Exports all Stationpedia page keys to a file for use in descriptions.json", null, false));
                }
                
                // Register "spda_currentkey" command to show current page key
                if (!commandsMap.ContainsKey("spda_currentkey"))
                {
                    Util.Commands.CommandLine.AddCommand("spda_currentkey", 
                        new Util.Commands.BasicCommand(CurrentPageKeyCommand, 
                            "Shows the deviceKey of the currently open Stationpedia page", null, false));
                }
                
                // Register "assetdisplay" command to toggle the UI Asset Inspector (debug tool)
                if (!commandsMap.ContainsKey("assetdisplay"))
                {
                    Util.Commands.CommandLine.AddCommand("assetdisplay", 
                        new Util.Commands.BasicCommand(AssetDisplayCommand, 
                            "Toggles the UI Asset Inspector to show asset names under mouse cursor", null, false));
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error registering console commands: {ex.Message}");
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
        
        private static string CurrentPageKeyCommand(string[] args)
        {
            try
            {
                string currentKey = Stationpedia.CurrentPageKey;
                if (string.IsNullOrEmpty(currentKey))
                {
                    return "No page currently open";
                }
                return $"Current page deviceKey: {currentKey}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        
        private static string AssetDisplayCommand(string[] args)
        {
            try
            {
                UIAssetInspector.Toggle();
                bool isEnabled = UIAssetInspector.IsEnabled;
                return $"UI Asset Inspector: {(isEnabled ? "ENABLED - hover over UI elements to see asset names" : "DISABLED")}";
            }
            catch (Exception ex)
            {
                return $"Error toggling UI Asset Inspector: {ex.Message}";
            }
        }
        
        private static string DumpPageKeysCommand(string[] args)
        {
            try
            {
                var pages = Stationpedia.StationpediaPages;
                if (pages == null || pages.Count == 0)
                {
                    return "No Stationpedia pages found. Open Stationpedia first.";
                }
                
                // Build output
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# Stationpedia Page Keys");
                sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"# Total pages: {pages.Count}");
                sb.AppendLine("#");
                sb.AppendLine("# Use these keys as 'deviceKey' in descriptions.json");
                sb.AppendLine("# Format: deviceKey | Title");
                sb.AppendLine("#");
                sb.AppendLine();
                
                // Sort alphabetically by key
                var sortedPages = new List<StationpediaPage>(pages);
                sortedPages.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
                
                foreach (var page in sortedPages)
                {
                    sb.AppendLine($"{page.Key} | {page.Title}");
                }
                
                // Save to file
                string outputPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "My Games", "Stationeers", "stationpedia_keys.txt");
                
                System.IO.File.WriteAllText(outputPath, sb.ToString());
                
                return $"Exported {pages.Count} page keys to:\n{outputPath}";
            }
            catch (Exception ex)
            {
                return $"Error dumping keys: {ex.Message}";
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
                string json = null;
                
#if !DEBUG
                // In Release builds, try embedded resource first
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("StationpediaAscended.descriptions.json"))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            json = reader.ReadToEnd();
                        }
                    }
                }
#endif
                
                // If no embedded resource (Debug build or resource not found), try file paths
                if (json == null)
                {
                    var possiblePaths = new List<string>();
                    
#if DEBUG
                    // Dev folder - only included in Debug builds for hot-reload development
                    possiblePaths.Add(@"C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\descriptions.json");
#endif
                    // Scripts folder (where we deploy to)
                    possiblePaths.Add(Path.Combine(Application.dataPath, "..", "BepInEx", "scripts", "descriptions.json"));
                    // Next to the executing assembly
                    possiblePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "descriptions.json"));
                    // Fallback - mod folder in My Games
                    possiblePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                        "My Games", "Stationeers", "mods", "StationpediaAscended", "descriptions.json"));
                    
                    foreach (var path in possiblePaths)
                    {
                        if (string.IsNullOrEmpty(path)) continue;
                        string fullPath = Path.GetFullPath(path);
                        if (File.Exists(fullPath))
                        {
                            json = File.ReadAllText(fullPath);
                            break;
                        }
                    }
                }
                
                if (json != null)
                {
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
                    }
                    
                    // Load custom guides from JSON
                    if (data?.guides != null && data.guides.Count > 0)
                    {
                        Data.JsonGuideLoader.LoadGuides(data);
                    }
                    
                    // Load game mechanics from JSON
                    if (data?.mechanics != null && data.mechanics.Count > 0)
                    {
                        Data.JsonMechanicsLoader.LoadMechanics(data);
                    }
                }
                else
                {
                    Log?.LogWarning($"descriptions.json not found");
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
                }
                
                var changeDisplay = universalPageType.GetMethod("ChangeDisplay", 
                    BindingFlags.Public | BindingFlags.Instance);
                if (changeDisplay != null)
                {
                    var postfix = typeof(HarmonyPatches).GetMethod("ChangeDisplay_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmonyStatic.Patch(changeDisplay, postfix: new HarmonyMethod(postfix));
                }
                
                // Patch ClearPreviousSearch for search result reorganization
                var stationpediaType = typeof(Stationpedia);
                var clearPreviousSearch = stationpediaType.GetMethod("ClearPreviousSearch", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (clearPreviousSearch != null)
                {
                    var postfix = typeof(SearchPatches).GetMethod("ClearPreviousSearch_Postfix", 
                        BindingFlags.Public | BindingFlags.Static);
                    _harmonyStatic.Patch(clearPreviousSearch, postfix: new HarmonyMethod(postfix));
                }
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
                        
                        // Hide unwanted items from Stationpedia searches
                        PopulateHiddenItems();
                    }

                    if (!Stationpedia.Instance.gameObject.activeInHierarchy)
                    {
                        _lastPageKeyStatic = "";
                        continue;
                    }

                    string currentPageKey = Stationpedia.CurrentPageKey;
                    
                    if (!string.IsNullOrEmpty(currentPageKey) && currentPageKey != _lastPageKeyStatic)
                    {
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

                try { totalTooltipsAdded += AddTooltipsToPropertiesStatic(universalPage); }
                catch { }
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

        private static int AddTooltipsToPropertiesStatic(UniversalPage universalPage)
        {
            if (universalPage == null)
            {
                return 0;
            }

            int added = 0;

            // Helper to add tooltip to parent GameObject (covers both label and value)
            void AddPropertyTooltip(TMPro.TextMeshProUGUI textElement, string propertyName)
            {
                if (textElement != null && textElement.gameObject != null && !string.IsNullOrEmpty(textElement.text))
                {
                    // Add tooltip to the PARENT GameObject so it covers both label and value
                    var parentObject = textElement.transform.parent?.gameObject;
                    if (parentObject != null)
                    {
                        // CRITICAL: Parent needs a Graphic component to receive pointer events
                        // Add transparent Image if no Graphic exists
                        var graphic = parentObject.GetComponent<UnityEngine.UI.Graphic>();
                        if (graphic == null)
                        {
                            var image = parentObject.AddComponent<UnityEngine.UI.Image>();
                            image.color = new Color(0, 0, 0, 0); // Fully transparent
                            image.raycastTarget = true; // Enable raycasting
                        }

                        var existingTooltip = parentObject.GetComponent<SPDAPropertyTooltip>();
                        if (existingTooltip == null)
                        {
                            var tooltip = parentObject.AddComponent<SPDAPropertyTooltip>();
                            tooltip.Initialize(propertyName);
                            _addedComponents.Add(tooltip);
                            added++;
                        }
                    }
                }
            }

            // Gas/Material properties
            AddPropertyTooltip(universalPage.FlashPointText, "Flashpoint");
            AddPropertyTooltip(universalPage.AutoIgniteText, "Autoignition");
            AddPropertyTooltip(universalPage.HeatTransferConvectionText, "Thermal Convection");
            AddPropertyTooltip(universalPage.HeatTransferRadiationText, "Thermal Radiation");
            AddPropertyTooltip(universalPage.SolarHeatingFactorText, "Solar Heating");
            AddPropertyTooltip(universalPage.SpecificHeat, "Specific Heat");
            AddPropertyTooltip(universalPage.FreezeTemperature, "Freeze Temperature");
            AddPropertyTooltip(universalPage.BoilingTemperature, "Boiling Temperature");
            AddPropertyTooltip(universalPage.MaxLiquidTemperature, "Max Liquid Temperature");
            AddPropertyTooltip(universalPage.MinLiquidPressure, "Min Liquid Pressure");
            AddPropertyTooltip(universalPage.LatentHeat, "Latent Heat");
            AddPropertyTooltip(universalPage.MolesPerLitre, "Moles Per Litre");

            // Device/Structure properties
            AddPropertyTooltip(universalPage.MaxPressure, "Max Pressure");
            AddPropertyTooltip(universalPage.Volume, "Volume");
            AddPropertyTooltip(universalPage.DeviceBasePower, "Base Power");
            AddPropertyTooltip(universalPage.DevicePowerStorage, "Power Storage");
            AddPropertyTooltip(universalPage.DevicePowerGeneration, "Power Generation");

            // Plant/Food properties
            AddPropertyTooltip(universalPage.GrowthTime, "Growth Time");
            AddPropertyTooltip(universalPage.Nutrition, "Nutrition");
            AddPropertyTooltip(universalPage.NutritionQuality, "Nutrition Quality");
            AddPropertyTooltip(universalPage.MoodBonus, "Mood Bonus");

            // Rocket properties
            AddPropertyTooltip(universalPage.PlaceableInRocket, "Placeable In Rocket");
            AddPropertyTooltip(universalPage.RocketMass, "Rocket Mass");

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
                catch { }

                try { totalTooltipsAdded += AddTooltipsToVersions(universalPage.StructureVersionContents, pageKey); }
                catch { }

                try { totalTooltipsAdded += AddTooltipsToMemory(universalPage.LogicInstructions, pageKey); }
                catch { }

                try { totalTooltipsAdded += AddTooltipsToProperties(universalPage); }
                catch { }
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error adding tooltips: {ex.Message}");
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

        private int AddTooltipsToProperties(UniversalPage universalPage)
        {
            if (universalPage == null)
            {
                return 0;
            }

            int added = 0;

            // Helper to add tooltip to parent GameObject (covers both label and value)
            void AddPropertyTooltip(TMPro.TextMeshProUGUI textElement, string propertyName)
            {
                if (textElement != null && textElement.gameObject != null && !string.IsNullOrEmpty(textElement.text))
                {
                    // Add tooltip to the PARENT GameObject so it covers both label and value
                    var parentObject = textElement.transform.parent?.gameObject;
                    if (parentObject != null)
                    {
                        // CRITICAL: Parent needs a Graphic component to receive pointer events
                        // Add transparent Image if no Graphic exists
                        var graphic = parentObject.GetComponent<UnityEngine.UI.Graphic>();
                        if (graphic == null)
                        {
                            var image = parentObject.AddComponent<UnityEngine.UI.Image>();
                            image.color = new Color(0, 0, 0, 0); // Fully transparent
                            image.raycastTarget = true; // Enable raycasting
                        }

                        var existingTooltip = parentObject.GetComponent<SPDAPropertyTooltip>();
                        if (existingTooltip == null)
                        {
                            var tooltip = parentObject.AddComponent<SPDAPropertyTooltip>();
                            tooltip.Initialize(propertyName);
                            _addedComponents.Add(tooltip);
                            added++;
                        }
                    }
                }
            }

            // Gas/Material properties
            AddPropertyTooltip(universalPage.FlashPointText, "Flashpoint");
            AddPropertyTooltip(universalPage.AutoIgniteText, "Autoignition");
            AddPropertyTooltip(universalPage.HeatTransferConvectionText, "Thermal Convection");
            AddPropertyTooltip(universalPage.HeatTransferRadiationText, "Thermal Radiation");
            AddPropertyTooltip(universalPage.SolarHeatingFactorText, "Solar Heating");
            AddPropertyTooltip(universalPage.SpecificHeat, "Specific Heat");
            AddPropertyTooltip(universalPage.FreezeTemperature, "Freeze Temperature");
            AddPropertyTooltip(universalPage.BoilingTemperature, "Boiling Temperature");
            AddPropertyTooltip(universalPage.MaxLiquidTemperature, "Max Liquid Temperature");
            AddPropertyTooltip(universalPage.MinLiquidPressure, "Min Liquid Pressure");
            AddPropertyTooltip(universalPage.LatentHeat, "Latent Heat");
            AddPropertyTooltip(universalPage.MolesPerLitre, "Moles Per Litre");

            // Device/Structure properties
            AddPropertyTooltip(universalPage.MaxPressure, "Max Pressure");
            AddPropertyTooltip(universalPage.Volume, "Volume");
            AddPropertyTooltip(universalPage.DeviceBasePower, "Base Power");
            AddPropertyTooltip(universalPage.DevicePowerStorage, "Power Storage");
            AddPropertyTooltip(universalPage.DevicePowerGeneration, "Power Generation");

            // Plant/Food properties
            AddPropertyTooltip(universalPage.GrowthTime, "Growth Time");
            AddPropertyTooltip(universalPage.Nutrition, "Nutrition");
            AddPropertyTooltip(universalPage.NutritionQuality, "Nutrition Quality");
            AddPropertyTooltip(universalPage.MoodBonus, "Mood Bonus");

            // Rocket properties
            AddPropertyTooltip(universalPage.PlaceableInRocket, "Placeable In Rocket");
            AddPropertyTooltip(universalPage.RocketMass, "Rocket Mass");

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

        // Static helper for property descriptions (Flashpoint, Autoignition, Thermal Convection, etc.)
        public static PropertyDescription GetPropertyDescription(string propertyName)
        {
            string cleanName = CleanLogicTypeName(propertyName);

            // Check genericDescriptions.properties for structured property descriptions
            if (GenericDescriptions?.properties != null && GenericDescriptions.properties.TryGetValue(cleanName, out var desc))
            {
                return desc;
            }
            
            // Also check AdditionalData for flat string tooltips (Flashpoint, Autoignition, etc.)
            if (GenericDescriptions?.AdditionalData != null && GenericDescriptions.AdditionalData.TryGetValue(cleanName, out var token))
            {
                var stringValue = token?.ToString();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    return new PropertyDescription { description = stringValue };
                }
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
        
        #endregion

        #region Update Loop
        
        /// <summary>
        /// Unity Update - handles F2 hotkey for Station Planner
        /// </summary>
        void Update()
        {
            // F2 hotkey to toggle Station Planner
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                StationPlannerWindow.Toggle();
            }
            
            // Let Station Planner update its state
            StationPlannerWindow.UpdateWindow();
        }
        
        #endregion

        #region Cleanup
        
        void OnDestroy()
        {
            // Cleanup Station Planner
            try
            {
                StationPlannerWindow.Cleanup();
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Error cleaning up Station Planner: {ex.Message}");
            }
            
            // Cleanup UI Asset Inspector
            try
            {
                UIAssetInspector.Cleanup();
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Error cleaning up UI Asset Inspector: {ex.Message}");
            }
            
            // Stop all coroutines on this MonoBehaviour
            StopAllCoroutines();
            _monitorCoroutine = null;
            
            // Cleanup Harmony patches
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
            
            // Cleanup tooltip components we added
            if (_addedComponents != null)
            {
                foreach (var component in _addedComponents)
                {
                    if (component != null)
                    {
                        try { Destroy(component); } catch { }
                    }
                }
                _addedComponents.Clear();
            }
            _addedComponents = new List<Component>();
            
            // Cleanup GameObjects we created
            if (_createdGameObjects != null)
            {
                foreach (var go in _createdGameObjects)
                {
                    if (go != null)
                    {
                        try { Destroy(go); } catch { }
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
            
            ConsoleWindow.Print("[Stationpedia Ascended] Cleaned up");
        }
        
        #endregion
    }
}
