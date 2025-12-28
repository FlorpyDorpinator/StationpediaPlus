using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Assets.Scripts;
using Assets.Scripts.UI;
using StationeersMods.Interface;
using UnityEngine;
using UnityEngine.EventSystems;
using Newtonsoft.Json;

namespace StationpediaPlus
{
    /// <summary>
    /// StationpediaPlus - Main mod class following IC10BatchUploader pattern
    /// ModBehaviour IS a MonoBehaviour, so we get Awake/Start/Update automatically
    /// when the framework attaches us to a GameObject
    /// </summary>
    public class StationpediaPlusMod : ModBehaviour
    {
        // Static reference for tooltip component access
        public static StationpediaPlusMod Instance { get; private set; }
        
        // Description databases
        public static Dictionary<string, DeviceDescriptions> DeviceDatabase { get; private set; }
        public static GenericDescriptionsData GenericDescriptions { get; private set; }
        
        // Page tracking
        private string _lastPageKey = "";
        private bool _stationpediaFound = false;
        private float _nextCheckTime = 0f;
        private const float CHECK_INTERVAL = 0.1f;
        
        // Tooltip display state (shared with SPDALogicTooltip components)
        public static string CurrentTooltipText { get; set; } = "";
        public static bool ShowTooltip { get; set; } = false;
        
        // GUI styling
        private GUIStyle _tooltipStyle;
        private bool _stylesInitialized = false;
        private Texture2D _tooltipBackground;

        // Called by StationeersLaunchPad when mod is loaded
        public override void OnLoaded(ContentHandler contentHandler)
        {
            base.OnLoaded(contentHandler);
            ConsoleWindow.Print("[StationpediaPlus] OnLoaded called via StationeersLaunchPad");
            
            // Load descriptions from JSON file
            LoadDescriptions();
            
            ConsoleWindow.Print("[StationpediaPlus] Mod loaded successfully - waiting for Awake/Start");
        }

        // Unity lifecycle - called when component is first created
        void Awake()
        {
            try
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ConsoleWindow.Print($"[StationpediaPlus] Awake() - GameObject: {gameObject.name}, Enabled: {enabled}");
            }
            catch (Exception ex)
            {
                ConsoleWindow.PrintError($"[StationpediaPlus] Error in Awake: {ex.Message}");
            }
        }

        // Unity lifecycle - called after Awake, when component is ready
        void Start()
        {
            ConsoleWindow.Print("[StationpediaPlus] Start() called - component is active and monitoring!");
        }

        // Unity lifecycle - called every frame
        void Update()
        {
            try
            {
                // Throttle checks to every 100ms
                if (Time.time < _nextCheckTime)
                {
                    return;
                }
                _nextCheckTime = Time.time + CHECK_INTERVAL;

                // Wait until Stationpedia.Instance exists
                if (Stationpedia.Instance == null)
                {
                    return;
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
                    return;
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
                ConsoleWindow.PrintError($"[StationpediaPlus] Error in Update: {ex.Message}");
            }
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
                // Find the mod directory
                string modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string jsonPath = Path.Combine(modPath, "descriptions.json");
                
                ConsoleWindow.Print($"[StationpediaPlus] Looking for descriptions at: {jsonPath}");
                
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var data = JsonConvert.DeserializeObject<DescriptionsRoot>(json);
                    
                    if (data?.devices != null)
                    {
                        foreach (var device in data.devices)
                        {
                            DeviceDatabase[device.deviceKey] = device;
                            ConsoleWindow.Print($"[StationpediaPlus] Loaded descriptions for {device.deviceKey}");
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
                ConsoleWindow.PrintError($"[StationpediaPlus] Error loading descriptions: {ex.Message}");
            }
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
                    ConsoleWindow.Print("[StationpediaPlus] UniversalPageRef is null - page may not have logic items");
                    return;
                }

                int totalTooltipsAdded = 0;

                totalTooltipsAdded += AddTooltipsToCategory(universalPage.LogicContents, pageKey, "Logic");
                totalTooltipsAdded += AddTooltipsToCategory(universalPage.LogicSlotContents, pageKey, "LogicSlot");
                totalTooltipsAdded += AddTooltipsToCategory(universalPage.ModeContents, pageKey, "Mode");
                totalTooltipsAdded += AddTooltipsToCategory(universalPage.ConnectionContents, pageKey, "Connection");
                
                // New category types - wrapped in try-catch to prevent one failure from blocking others
                try { totalTooltipsAdded += AddTooltipsToSlots(universalPage.SlotContents, pageKey); }
                catch (Exception ex) { ConsoleWindow.PrintError($"[StationpediaPlus] Error adding slot tooltips: {ex.Message}"); }
                
                try { totalTooltipsAdded += AddTooltipsToVersions(universalPage.StructureVersionContents, pageKey); }
                catch (Exception ex) { ConsoleWindow.PrintError($"[StationpediaPlus] Error adding version tooltips: {ex.Message}"); }
                
                try { totalTooltipsAdded += AddTooltipsToMemory(universalPage.LogicInstructions, pageKey); }
                catch (Exception ex) { ConsoleWindow.PrintError($"[StationpediaPlus] Error adding memory tooltips: {ex.Message}"); }

                if (totalTooltipsAdded > 0)
                {
                    ConsoleWindow.Print($"[StationpediaPlus] Added {totalTooltipsAdded} tooltips for page {pageKey}");
                }
                else
                {
                    ConsoleWindow.Print($"[StationpediaPlus] No logic items found on page {pageKey}");
                }
            }
            catch (Exception ex)
            {
                ConsoleWindow.PrintError($"[StationpediaPlus] Error adding tooltips: {ex.Message}");
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
                        added++;
                    }
                }
            }
            if (added > 0)
                ConsoleWindow.Print($"[StationpediaPlus] Added {added} slot tooltips");
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
                        added++;
                    }
                }
            }
            if (added > 0)
                ConsoleWindow.Print($"[StationpediaPlus] Added {added} version tooltips");
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
                        added++;
                    }
                }
            }
            if (added > 0)
                ConsoleWindow.Print($"[StationpediaPlus] Added {added} memory tooltips");
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
            // Cleanup texture
            if (_tooltipBackground != null)
            {
                Destroy(_tooltipBackground);
            }
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
        // Logic, LogicSlot, Connection all use the same SPDALogic prefab
        public Dictionary<string, LogicDescription> logicDescriptions;
        // Mode value descriptions (e.g., "Outward", "Inward" for Active Vent)
        public Dictionary<string, ModeDescription> modeDescriptions;
        // Slot descriptions for SPDASlot items
        public Dictionary<string, SlotDescription> slotDescriptions;
        // Version/Tier descriptions for SPDAVersion items
        public Dictionary<string, VersionDescription> versionDescriptions;
        // Memory instruction/opcode descriptions for SPDAGeneric items
        public Dictionary<string, MemoryDescription> memoryDescriptions;
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
        public string modeValue;     // e.g., "Outward", "Inward", "Mode0", "Mode1"
        public string description;   // What this mode does on this device
    }

    [Serializable]
    public class SlotDescription
    {
        public string slotType;      // e.g., "Battery", "GasCanister", "ProgrammableChip"
        public string description;   // What this slot is used for on this device
    }

    [Serializable]
    public class VersionDescription
    {
        public string description;   // What changes in this version/tier
    }

    [Serializable]
    public class MemoryDescription
    {
        public string opCode;        // The instruction name (e.g., "ExecuteRecipe", "WriteTraderData")
        public string parameters;    // Expected parameters (e.g., "address, value")
        public string description;   // What this instruction does
        public string byteLayout;    // Byte layout description (e.g., "0-7: OP_CODE\n8-39: GAS_TYPES_BITFLAG")
    }

    [Serializable]
    public class GenericDescriptionsData
    {
        // Generic logic type descriptions (for Logic, LogicSlot, Connection)
        public Dictionary<string, string> logic;
        // Generic slot type descriptions (for physical slot types like "Battery", "Plant")
        public Dictionary<string, string> slotTypes;
        // Generic slot logic descriptions (for reading slot values like "Quantity", "Occupied")
        public Dictionary<string, string> slots;
        // Generic mode descriptions (for common mode values)
        public Dictionary<string, string> modes;
        // Generic version descriptions
        public Dictionary<string, string> versions;
        // Generic connection descriptions
        public Dictionary<string, string> connections;
        // Generic memory instruction descriptions (with byte layout)
        public Dictionary<string, MemoryDescription> memory;
    }
    
    #endregion

    #region Tooltip Component

    /// <summary>
    /// Custom tooltip component for SPDALogic items
    /// Uses IPointerEnterHandler/IPointerExitHandler with our custom IMGUI tooltip display
    /// Shows context-specific description on hover with 300ms delay
    /// </summary>
    public class SPDALogicTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string _deviceKey;
        private string _logicTypeName;
        private string _categoryName;
        private string _cachedTooltipText;
        private Coroutine _showCoroutine;
        private bool _isHovering;
        
        private const float HOVER_DELAY = 0.3f; // 300ms like the game

        public void Initialize(string deviceKey, string logicTypeName, string categoryName)
        {
            _deviceKey = deviceKey;
            _logicTypeName = logicTypeName;
            _categoryName = categoryName;
            _cachedTooltipText = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            string cleanName = CleanLogicTypeName(_logicTypeName);
            ConsoleWindow.Print($"[StationpediaPlus] HOVER: {cleanName} on page {_deviceKey}");
            _isHovering = true;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
            }
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
            // Hide our custom tooltip
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
                    // Show our custom tooltip via the main mod's OnGUI
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
            
            // Handle Mode category differently - it uses mode VALUES like "Outward", "Inward"
            if (_categoryName == "Mode")
            {
                var modeDesc = StationpediaPlusMod.GetModeDescription(_deviceKey, cleanName);
                if (modeDesc != null)
                {
                    ConsoleWindow.Print($"[StationpediaPlus] Found mode description for {cleanName} on {_deviceKey}");
                    _cachedTooltipText = FormatModeTooltip(cleanName, modeDesc);
                    return _cachedTooltipText;
                }
            }
            else if (_categoryName == "Connection")
            {
                // Connection types like "Data", "Power", "LiquidPipe" use connection descriptions
                var connDesc = StationpediaPlusMod.GetConnectionDescription(_deviceKey, cleanName);
                if (connDesc != null)
                {
                    ConsoleWindow.Print($"[StationpediaPlus] Found connection description for {cleanName} on {_deviceKey}");
                    _cachedTooltipText = FormatTooltip(cleanName, connDesc);
                    return _cachedTooltipText;
                }
            }
            else if (_categoryName == "LogicSlot")
            {
                // LogicSlot types like "Occupied", "OccupantHash", "Quantity" use slot logic descriptions
                var slotLogicDesc = StationpediaPlusMod.GetSlotLogicDescription(cleanName);
                if (slotLogicDesc != null)
                {
                    ConsoleWindow.Print($"[StationpediaPlus] Found slot logic description for {cleanName} on {_deviceKey}");
                    _cachedTooltipText = FormatSlotLogicTooltip(cleanName, slotLogicDesc);
                    return _cachedTooltipText;
                }
                // Also try regular logic descriptions as fallback
                var desc = StationpediaPlusMod.GetLogicDescription(_deviceKey, cleanName);
                if (desc != null)
                {
                    ConsoleWindow.Print($"[StationpediaPlus] Found logic description for {cleanName} on {_deviceKey}");
                    _cachedTooltipText = FormatTooltip(cleanName, desc);
                    return _cachedTooltipText;
                }
            }
            else
            {
                // Logic types use logic descriptions
                var desc = StationpediaPlusMod.GetLogicDescription(_deviceKey, cleanName);
                if (desc != null)
                {
                    ConsoleWindow.Print($"[StationpediaPlus] Found description for {cleanName} on {_deviceKey}");
                    _cachedTooltipText = FormatTooltip(cleanName, desc);
                    return _cachedTooltipText;
                }
            }
            
            ConsoleWindow.Print($"[StationpediaPlus] No description for {cleanName} ({_categoryName}) on {_deviceKey}");
            
            // Return a default message if no description found
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
            string result = System.Text.RegularExpressions.Regex.Replace(name, "<[^>]+>", "");
            return result.Trim();
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

    /// <summary>
    /// Tooltip component for SPDASlot items (device slots like Battery, ProgrammableChip, etc.)
    /// </summary>
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

    /// <summary>
    /// Tooltip component for SPDAVersion items (structure tiers like Tier One, Tier Two)
    /// </summary>
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

    /// <summary>
    /// Tooltip component for SPDAGeneric items (memory instructions/opcodes)
    /// </summary>
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
            
            // Add byte layout if available
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
            
            // Remove Unity rich text tags
            string cleaned = System.Text.RegularExpressions.Regex.Replace(name, "<[^>]+>", "").Trim();
            
            // Remove OP_CODE suffix (e.g., "TraderInstruction.FilterGasNotContains OP_CODE: 18" -> "TraderInstruction.FilterGasNotContains")
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
}
