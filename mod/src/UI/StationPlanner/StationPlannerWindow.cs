using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Newtonsoft.Json;
using Assets.Scripts.UI;
using Assets.Scripts;
using Assets.Scripts.Networking;
using Assets.Scripts.Inventory;
using Assets.Scripts.Serialization;
using BepInEx;
using LeTai.Asset.TranslucentImage;

namespace StationpediaAscended.UI.StationPlanner
{
    /// <summary>
    /// Station Notepad - A note-taking text editor window for Stationeers
    /// Rebuilt from first principles with simple, functional UI
    /// </summary>
    public class StationPlannerWindow : MonoBehaviour
    {
        #region Singleton & State
        
        public static StationPlannerWindow Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._windowCanvas != null && Instance._windowCanvas.enabled;
        
        private Canvas _windowCanvas;
        private GameObject _windowPanel;
        private RectTransform _windowRect;
        
        // Cached TranslucentImage material from game's UI
        private static Material _translucentMaterial;
        
        #endregion
        
        #region UI References
        
        private BlockEditor _blockEditor;  // Block-based editor with prefab TMP_InputField
        private ScrollRect _fileTreeScrollRect;
        private RectTransform _fileTreeContent;
        private TextMeshProUGUI _currentFileLabel;
        
        // Formatting toolbar buttons (to highlight active style)
        private Button _btnH1;
        private Button _btnH2;
        private Button _btnH3;
        private Button _btnBullet;
        private Button _btnStrike;
        
        // Title bar buttons
        private Button _pauseButton;
        private TextMeshProUGUI _pauseButtonText;
        private Image _pauseButtonBg;
        private bool _isPaused = false;
        
        // Cached font from game UI for consistent styling
        private static TMP_FontAsset _cachedFont;
        private static Material _cachedFontMaterial;
        
        #endregion
        
        #region Data
        
        private PlannerFileSystem _fileSystem;
        private PlannerFile _currentFile;
        private PlannerFolder _currentFolder;
        private HashSet<PlannerFolder> _selectedFolders = new HashSet<PlannerFolder>();  // Multi-select support
        private string _savePath;
        private bool _hasUnsavedChanges = false;
        private string _lastKnownSaveName = null;  // Track save changes to refresh folder display
        
        #endregion
        
        #region Constants
        
        private const float MIN_WIDTH = 600f;   // Single row toolbar
        private const float MIN_HEIGHT = 300f;   // Reduced for compact layout
        private const float DEFAULT_WIDTH = 700f;  // Back to wider default
        private const float DEFAULT_HEIGHT = 480f;
        private const float TITLE_BAR_HEIGHT = 50f;  // More padding above title (increased from 44)
        private const float TOOLBAR_HEIGHT = 40f;    // Taller toolbar for breathing room
        private const float FILE_TREE_WIDTH = 180f;
        
        // Stationeers IC10 editor colors (researched from UI inspector)
        // IC10 buttons use button-bg sprite with color #FFFFFF10 (white at 6.3% opacity)
        private static readonly Color BG_DARK = new Color(0.035f, 0.055f, 0.082f, 0.98f);     // Very dark navy background
        private static readonly Color BG_PANEL = new Color(0.055f, 0.082f, 0.118f, 0.95f);     // Slightly lighter panel
        private static readonly Color BG_INPUT = new Color(0.025f, 0.04f, 0.065f, 1f);         // Input field background (darkest)
        private static readonly Color ACCENT = new Color(0.984f, 0.69f, 0.231f, 1f);           // Orange #FBB03B (game's highlight color)
        private static readonly Color ACCENT_ORANGE = new Color(0.996f, 0.353f, 0.086f, 1f);   // Brighter orange #FF5A16 for outlines
        private static readonly Color TEXT_NORMAL = new Color(0.9f, 0.9f, 0.9f, 1f);           // White text
        private static readonly Color TEXT_DIM = new Color(0.5f, 0.55f, 0.6f, 1f);             // Dimmed text
        private static readonly Color BORDER = new Color(0.984f, 0.69f, 0.231f, 0.8f);         // Orange border
        // IC10-accurate button colors - black at 50% opacity like the game (#00000080)
        private static readonly Color BTN_NORMAL = new Color(0f, 0f, 0f, 0.5f);                // #00000080 - black at 50% opacity
        private static readonly Color BTN_HOVER = new Color(0f, 0f, 0f, 0.65f);                // Slightly darker on hover
        private static readonly Color BTN_PRESS = new Color(0f, 0f, 0f, 0.8f);                 // Even darker on press
        private static readonly Color SELECTED = new Color(0.984f, 0.69f, 0.231f, 0.3f);         // Orange selection highlight
        private static readonly Color FOLDER_PERSISTENT = new Color(0.4f, 0.7f, 1f, 1f);       // Light blue for folders that persist across saves
        
        // Toolbar - simple layout
        private GameObject _toolbarContainer;
        private List<Button> _toolbarButtons = new List<Button>();
        
        #endregion
        
        #region Initialization
        
        public static void Initialize()
        {
            if (Instance != null) return;
            
            var go = new GameObject("StationPlannerWindow");
            Instance = go.AddComponent<StationPlannerWindow>();
            DontDestroyOnLoad(go);
            
            Instance.CreateWindow();
            Instance.LoadFileSystem();
            Instance.Hide();
        }
        
        public static void Cleanup()
        {
            if (Instance != null)
            {
                Instance.SaveCurrentFile();
                Instance.SaveFileSystem();
                
                if (Instance._windowPanel != null)
                    Destroy(Instance._windowPanel);
                if (Instance._windowCanvas != null)
                    Destroy(Instance._windowCanvas.gameObject);
                    
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }
        
        void OnDestroy()
        {
            SaveCurrentFile();
            SaveFileSystem();
        }
        
        #endregion
        
        #region Window Creation
        
        private void CreateWindow()
        {
            // Create canvas
            var canvasGO = new GameObject("StationPlannerCanvas");
            canvasGO.transform.SetParent(transform);
            
            _windowCanvas = canvasGO.AddComponent<Canvas>();
            _windowCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _windowCanvas.sortingOrder = 900;
            
            // Required for TranslucentImage to work - needs TexCoord1 channel for spriteBlending
            _windowCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Main window panel - uses IC10 editor sprite layering
            _windowPanel = CreatePanel(canvasGO.transform, "Window", DEFAULT_WIDTH, DEFAULT_HEIGHT);
            _windowRect = _windowPanel.GetComponent<RectTransform>();
            _windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _windowRect.pivot = new Vector2(0.5f, 0.5f);
            _windowRect.anchoredPosition = Vector2.zero;
            
            // Layer 1: Window shadow (bottom layer) - white sprite with 50% opacity
            var shadowGO = new GameObject("WindowShadow");
            shadowGO.transform.SetParent(_windowPanel.transform, false);
            var shadowRect = shadowGO.AddComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.offsetMin = new Vector2(-8, -8);
            shadowRect.offsetMax = new Vector2(8, 8);
            var shadowImg = shadowGO.AddComponent<Image>();
            shadowImg.sprite = LoadSlicedSprite("window-shadow.png", 16);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = new Color(1f, 1f, 1f, 0.5f); // #FFFFFF80
            shadowImg.raycastTarget = false;
            
            // Layer 2: Dialog background - use regular Image (TranslucentImage requires special material)
            var bgGO = new GameObject("DialogBg");
            bgGO.transform.SetParent(_windowPanel.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = LoadSlicedSprite("dialog-bg.png", 18); // 18px border matches game
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0f, 0.165f, 0.278f, 1f); // #002A47FF - dark blue tint
            bgImg.raycastTarget = true;
            
            // Layer 3: Dialog outline (orange border)
            var outlineGO = new GameObject("DialogOutline");
            outlineGO.transform.SetParent(_windowPanel.transform, false);
            var outlineRect = outlineGO.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            var outlineImg = outlineGO.AddComponent<Image>();
            outlineImg.sprite = LoadSlicedSprite("dialog-outline.png", 18);
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = new Color(0.996f, 0.353f, 0.086f, 1f); // #FF5A16FF - orange
            outlineImg.raycastTarget = false;
            
            // Create UI sections
            CreateTitleBar(_windowPanel.transform);
            CreateToolbar(_windowPanel.transform);
            CreateMainContent(_windowPanel.transform);
            CreateResizeHandle(_windowPanel.transform);
        }
        
        /// <summary>
        /// Load a 9-sliced sprite from the images folder
        /// </summary>
        private Sprite LoadSlicedSprite(string filename, int borderSize)
        {
            // Use the mod's image loading utility
            return StationpediaAscendedMod.LoadSlicedSprite(filename, borderSize);
        }
        
        /// <summary>
        /// Load a regular sprite from the images folder
        /// </summary>
        private Sprite LoadSprite(string filename)
        {
            return StationpediaAscendedMod.LoadImageFromModFolder(filename);
        }
        
        /// <summary>
        /// Get the cached game font. Looks for existing TextMeshProUGUI in game UI to extract font.
        /// </summary>
        private TMP_FontAsset GetCachedFont()
        {
            if (_cachedFont != null) return _cachedFont;
            
            try
            {
                // Find an existing TextMeshProUGUI in the game's UI to copy font from
                var existingText = FindObjectOfType<TextMeshProUGUI>();
                if (existingText != null)
                {
                    _cachedFont = existingText.font;
                    _cachedFontMaterial = existingText.fontSharedMaterial;
                    StationpediaAscendedMod.Log?.LogInfo($"[StationPlanner] Cached font: {_cachedFont?.name}");
                }
            }
            catch (System.Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"[StationPlanner] Failed to cache font: {ex.Message}");
            }
            
            return _cachedFont;
        }
        
        /// <summary>
        /// Apply the cached game font to a TextMeshProUGUI component
        /// </summary>
        private void ApplyGameFont(TextMeshProUGUI textComp)
        {
            var font = GetCachedFont();
            if (font != null)
            {
                textComp.font = font;
                if (_cachedFontMaterial != null)
                    textComp.fontSharedMaterial = _cachedFontMaterial;
            }
        }
        
        /// <summary>
        /// Get the TranslucentImage material. Creates it from shader if needed.
        /// Returns null if shader not found.
        /// </summary>
        private Material GetTranslucentMaterial()
        {
            // Return cached material if already created
            if (_translucentMaterial != null) return _translucentMaterial;
            
            // Find the UI/TranslucentImage shader (built into the game)
            var shader = Shader.Find("UI/TranslucentImage");
            if (shader == null)
            {
                StationpediaAscendedMod.Log?.LogError("[StationPlanner] UI/TranslucentImage shader not found!");
                return null;
            }
            
            // Create material from shader
            _translucentMaterial = new Material(shader);
            _translucentMaterial.name = "StationPlanner-Translucent";
            
            StationpediaAscendedMod.Log?.LogInfo("[StationPlanner] Created TranslucentImage material from shader");
            return _translucentMaterial;
        }
        
        /// <summary>
        /// Create a panel background with TranslucentImage if shader is available.
        /// </summary>
        private Image CreatePanelBackground(GameObject go, Sprite sprite, Color color)
        {
            // Try to get/create the material from shader
            var mat = GetTranslucentMaterial();
            
            if (mat != null)
            {
                // Use TranslucentImage with the material we created
                var transImg = go.AddComponent<TranslucentImage>();
                
                if (sprite != null)
                {
                    transImg.sprite = sprite;
                    transImg.type = Image.Type.Sliced;
                }
                
                transImg.color = color;
                
                // IC10's exact TranslucentImage properties
                transImg.vibrancy = 2f;
                transImg.brightness = -0.257f;
                transImg.flatten = 0.2f;
                transImg.spriteBlending = 1f;
                transImg.material = mat;
                
                StationpediaAscendedMod.Log?.LogInfo($"[StationPlanner] Created TranslucentImage on {go.name} - material={mat.name}, shader={mat.shader?.name}");
                
                return transImg;
            }
            
            // Fallback to regular Image if shader not found
            var img = go.AddComponent<Image>();
            
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            
            img.color = color;
            return img;
        }
        
        private GameObject CreatePanel(Transform parent, string name, float width, float height)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            return panel;
        }
        
        private void CreateTitleBar(Transform parent)
        {
            var titleBar = CreatePanel(parent, "TitleBar", 0, TITLE_BAR_HEIGHT);
            var rect = titleBar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, TITLE_BAR_HEIGHT);
            
            // Transparent background - we inherit from the main dialog-bg
            // No background - inherit from main dialog-bg
            
            // Make draggable
            var drag = titleBar.AddComponent<WindowDragHandler>();
            drag.WindowRect = _windowRect;
            
            // Title text - matches IC10's "EDIT SCRIPT" style exactly
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(titleBar.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "STATION NOTEPAD";
            titleText.fontSize = 20; // Larger font like IC10
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = TEXT_NORMAL;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.characterSpacing = 4f; // Character spacing like IC10
            
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(18, 4); // Left padding, slight bottom padding
            titleRect.offsetMax = new Vector2(-110, -10); // More top padding (10px from top), room for buttons
            
            // Pause button (leftmost of the title bar buttons) - with background like toolbar buttons
            var pauseBtn = CreateTitleBarButton(titleBar.transform, "||", 28, 28);
            var pauseBtnRect = pauseBtn.GetComponent<RectTransform>();
            pauseBtnRect.anchorMin = new Vector2(1, 0.5f);
            pauseBtnRect.anchorMax = new Vector2(1, 0.5f);
            pauseBtnRect.pivot = new Vector2(1, 0.5f);
            pauseBtnRect.anchoredPosition = new Vector2(-84, 0);  // Left of help button
            _pauseButton = pauseBtn.GetComponent<Button>();
            _pauseButton.onClick.AddListener(TogglePause);
            _pauseButtonText = pauseBtn.GetComponentInChildren<TextMeshProUGUI>();
            _pauseButtonBg = pauseBtn.GetComponent<Image>();  // Store background for paused state indication
            AddSimpleTooltip(pauseBtn, "Pause/Resume game");
            
            // Help button - with background like toolbar buttons
            var helpBtn = CreateTitleBarButton(titleBar.transform, "?", 28, 28);
            var helpBtnRect = helpBtn.GetComponent<RectTransform>();
            helpBtnRect.anchorMin = new Vector2(1, 0.5f);
            helpBtnRect.anchorMax = new Vector2(1, 0.5f);
            helpBtnRect.pivot = new Vector2(1, 0.5f);
            helpBtnRect.anchoredPosition = new Vector2(-50, 0);  // Left of close button
            helpBtn.GetComponent<Button>().onClick.AddListener(ShowHelpDialog);
            AddSimpleTooltip(helpBtn, "Show help guide");
            
            // Close button - with background like toolbar buttons
            var closeBtn = CreateTitleBarButton(titleBar.transform, "X", 28, 28);
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1, 0.5f);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.anchoredPosition = new Vector2(-16, 0);  // Right edge
            closeBtn.GetComponent<Button>().onClick.AddListener(Hide);
            AddSimpleTooltip(closeBtn, "Close Station Notepad");
        }
        
        private void CreateToolbar(Transform parent)
        {
            _toolbarContainer = CreatePanel(parent, "Toolbar", 0, TOOLBAR_HEIGHT);
            var rect = _toolbarContainer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -TITLE_BAR_HEIGHT);
            rect.sizeDelta = new Vector2(0, TOOLBAR_HEIGHT);
            
            // Use HorizontalLayoutGroup for single row
            var hlg = _toolbarContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            
            _toolbarButtons.Clear();
            
            // File operations
            var newFileBtn = CreateToolbarBtn(_toolbarContainer.transform, "Note", 55);
            newFileBtn.onClick.AddListener(OnNewFile);
            AddSimpleTooltip(newFileBtn.gameObject, "Create a new note file");
            
            var newFolderBtn = CreateToolbarBtn(_toolbarContainer.transform, "Folder", 70);
            newFolderBtn.onClick.AddListener(OnNewFolder);
            AddSimpleTooltip(newFolderBtn.gameObject, "Create a new folder");
            
            var renameBtn = CreateToolbarBtn(_toolbarContainer.transform, "Rename", 75);
            renameBtn.onClick.AddListener(OnRename);
            AddSimpleTooltip(renameBtn.gameObject, "Rename selected file or folder");
            
            var deleteBtn = CreateToolbarBtn(_toolbarContainer.transform, "Delete", 70);
            deleteBtn.onClick.AddListener(OnDelete);
            AddSimpleTooltip(deleteBtn.gameObject, "Delete selected file or folder");
            
            CreateToolbarSeparator(_toolbarContainer.transform);
            
            // Formatting buttons
            _btnH1 = CreateToolbarBtn(_toolbarContainer.transform, "H1", 36);
            _btnH1.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            _btnH1.onClick.AddListener(() => ApplyLineStyle(LineStyle.H1));
            AddSimpleTooltip(_btnH1.gameObject, "Heading 1 - Large title");
            
            _btnH2 = CreateToolbarBtn(_toolbarContainer.transform, "H2", 36);
            _btnH2.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            _btnH2.onClick.AddListener(() => ApplyLineStyle(LineStyle.H2));
            AddSimpleTooltip(_btnH2.gameObject, "Heading 2 - Medium title");
            
            _btnH3 = CreateToolbarBtn(_toolbarContainer.transform, "H3", 36);
            _btnH3.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            _btnH3.onClick.AddListener(() => ApplyLineStyle(LineStyle.H3));
            AddSimpleTooltip(_btnH3.gameObject, "Heading 3 - Small title");
            
            _btnBullet = CreateToolbarBtn(_toolbarContainer.transform, "•", 28);
            _btnBullet.onClick.AddListener(() => _blockEditor?.InsertText("• "));
            AddSimpleTooltip(_btnBullet.gameObject, "Bullet point");
            
            _btnStrike = CreateToolbarBtn(_toolbarContainer.transform, "/", 28);
            _btnStrike.onClick.AddListener(() => ApplyLineStyle(LineStyle.Strikethrough));
            AddSimpleTooltip(_btnStrike.gameObject, "Strikethrough - Grey crossed-out text");
            
            var normalBtn = CreateToolbarBtn(_toolbarContainer.transform, "¶", 28);
            normalBtn.onClick.AddListener(() => ApplyLineStyle(LineStyle.Normal));
            AddSimpleTooltip(normalBtn.gameObject, "Reset to normal text");
        }
        
        /// <summary>
        /// Creates the main content area containing file tree and editor
        /// </summary>
        private void CreateMainContent(Transform parent)
        {
            // Container for file tree and editor
            var contentArea = CreatePanel(parent, "ContentArea", 0, 0);
            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(16, 16);  // Left and bottom padding
            contentRect.offsetMax = new Vector2(-16, -(TITLE_BAR_HEIGHT + TOOLBAR_HEIGHT + 12));  // Right and below toolbar
            
            // File tree on the left
            CreateFileTreePanel(contentArea.transform);
            
            // Editor on the right - IC10 style
            CreateEditorPanel(contentArea.transform);
        }
        
        private void CreateFileTreePanel(Transform parent)
        {
            var panel = CreatePanel(parent, "FileTree", FILE_TREE_WIDTH, 0);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(FILE_TREE_WIDTH, 0);
            
            // Background - dark fill
            var bg = panel.AddComponent<Image>();
            var bgSprite = LoadSlicedSprite("button-bg.png", 7);
            if (bgSprite != null)
            {
                bg.sprite = bgSprite;
                bg.type = Image.Type.Sliced;
            }
            bg.color = new Color(0f, 0f, 0f, 0.75f); // Dark background
            bg.raycastTarget = true;
            
            // Add click handler to background for deselection
            var bgBtn = panel.AddComponent<Button>();
            bgBtn.targetGraphic = bg;
            bgBtn.colors = new ColorBlock { normalColor = Color.clear, highlightedColor = Color.clear, pressedColor = Color.clear, selectedColor = Color.clear, colorMultiplier = 1, fadeDuration = 0 };
            bgBtn.onClick.AddListener(DeselectAll);
            
            // Scroll view
            _fileTreeScrollRect = panel.AddComponent<ScrollRect>();
            _fileTreeScrollRect.horizontal = false;
            _fileTreeScrollRect.vertical = true;
            _fileTreeScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _fileTreeScrollRect.scrollSensitivity = 25f;
            
            // Viewport with mask for clipping
            var viewport = CreatePanel(panel.transform, "Viewport", 0, 0);
            var vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(4, 4);
            vpRect.offsetMax = new Vector2(-4, -4);
            viewport.AddComponent<RectMask2D>();
            _fileTreeScrollRect.viewport = vpRect;
            
            // Content
            var content = CreatePanel(viewport.transform, "Content", 0, 0);
            _fileTreeContent = content.GetComponent<RectTransform>();
            _fileTreeContent.anchorMin = new Vector2(0, 1);
            _fileTreeContent.anchorMax = new Vector2(1, 1);
            _fileTreeContent.pivot = new Vector2(0, 1);
            _fileTreeContent.anchoredPosition = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 1;
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _fileTreeScrollRect.content = _fileTreeContent;
            
            // Outline on top
            var outlineGO = new GameObject("Outline");
            outlineGO.transform.SetParent(panel.transform, false);
            var outlineRect = outlineGO.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            var outlineImg = outlineGO.AddComponent<Image>();
            var outlineSprite = LoadSlicedSprite("slot-outline.png", 8);
            if (outlineSprite != null)
            {
                outlineImg.sprite = outlineSprite;
                outlineImg.type = Image.Type.Sliced;
                outlineImg.fillCenter = false;
            }
            outlineImg.color = new Color(0f, 0f, 0f, 0.5f);
            outlineImg.raycastTarget = false;
        }
        
        /// <summary>
        /// Creates the editor panel using the BlockEditor (loads TMP_InputField from AssetBundle prefab)
        /// </summary>
        private void CreateEditorPanel(Transform parent)
        {
            // Editor container - positioned to the right of file tree
            var panel = CreatePanel(parent, "Editor", 0, 0);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(FILE_TREE_WIDTH + 10, 0);
            rect.offsetMax = Vector2.zero;
            
            // Dark background fill - same as file tree
            var bgImg = panel.AddComponent<Image>();
            var bgSprite = LoadSlicedSprite("button-bg.png", 7);
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0f, 0f, 0f, 0.75f); // Dark background
            bgImg.raycastTarget = true;
            
            // Current file label (top right corner)
            var labelGO = new GameObject("FileLabel");
            labelGO.transform.SetParent(panel.transform, false);
            _currentFileLabel = labelGO.AddComponent<TextMeshProUGUI>();
            _currentFileLabel.text = "No file selected";
            _currentFileLabel.fontSize = 11;
            _currentFileLabel.color = TEXT_DIM;
            _currentFileLabel.alignment = TextAlignmentOptions.TopRight;
            _currentFileLabel.raycastTarget = false;
            
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(1, 1);
            labelRect.anchoredPosition = new Vector2(-8, -4);
            labelRect.sizeDelta = new Vector2(0, 16);
            
            // Container for the BlockEditor (holds the prefab instance)
            var editorContainerGO = new GameObject("EditorContainer");
            editorContainerGO.transform.SetParent(panel.transform, false);
            var containerRect = editorContainerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(8, 8);
            containerRect.offsetMax = new Vector2(-8, -22); // Leave room for file label
            
            // Create and initialize BlockEditor
            _blockEditor = editorContainerGO.AddComponent<BlockEditor>();
            if (!_blockEditor.Initialize(editorContainerGO.transform))
            {
                StationpediaAscendedMod.Log?.LogError("[StationPlanner] Failed to initialize BlockEditor - AssetBundle may be missing");
                
                // Show error message in the panel
                var errorGO = new GameObject("ErrorText");
                errorGO.transform.SetParent(editorContainerGO.transform, false);
                var errorText = errorGO.AddComponent<TextMeshProUGUI>();
                errorText.text = "ERROR: Could not load editor prefab.\n\nPlease ensure 'stationpediaascended_ui' AssetBundle\nis in the BepInEx/scripts folder.";
                errorText.fontSize = 14;
                errorText.color = new Color(1f, 0.4f, 0.4f, 1f);
                errorText.alignment = TextAlignmentOptions.Center;
                var errorRect = errorGO.GetComponent<RectTransform>();
                errorRect.anchorMin = Vector2.zero;
                errorRect.anchorMax = Vector2.one;
                errorRect.offsetMin = Vector2.zero;
                errorRect.offsetMax = Vector2.zero;
            }
            else
            {
                // Hook up auto-save on text change
                _blockEditor.OnContentChanged += OnEditorContentChanged;
                
                // Position and size the instantiated prefab
                if (_blockEditor.InputField != null)
                {
                    var inputRect = _blockEditor.InputField.GetComponent<RectTransform>();
                    inputRect.anchorMin = Vector2.zero;
                    inputRect.anchorMax = Vector2.one;
                    inputRect.offsetMin = Vector2.zero;
                    inputRect.offsetMax = Vector2.zero;
                    
                    // Apply our color scheme to the prefab
                    _blockEditor.InputField.caretColor = ACCENT;
                    _blockEditor.InputField.customCaretColor = true;
                    _blockEditor.InputField.caretWidth = 2;
                    _blockEditor.InputField.selectionColor = SELECTED;
                    
                    // Track unsaved changes
                    _blockEditor.InputField.onValueChanged.AddListener((_) => _hasUnsavedChanges = true);
                    
                    StationpediaAscendedMod.Log?.LogInfo("[StationPlanner] BlockEditor initialized successfully");
                }
            }
            
            // Outline on top
            var outlineGO = new GameObject("Outline");
            outlineGO.transform.SetParent(panel.transform, false);
            var outlineRect = outlineGO.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            var outlineImg = outlineGO.AddComponent<Image>();
            var outlineSprite = LoadSlicedSprite("slot-outline.png", 8);
            if (outlineSprite != null)
            {
                outlineImg.sprite = outlineSprite;
                outlineImg.type = Image.Type.Sliced;
                outlineImg.fillCenter = false;
            }
            outlineImg.color = new Color(0f, 0f, 0f, 0.5f);
            outlineImg.raycastTarget = false;
        }
        
        /// <summary>
        /// Creates an IC10-style scrollbar with custom sprites
        /// </summary>
        private void CreateIC10Scrollbar(Transform parent, ScrollRect scrollRect)
        {
            // Scrollbar container
            var scrollbarGO = new GameObject("Scrollbar");
            scrollbarGO.transform.SetParent(parent, false);
            var scrollbarRect = scrollbarGO.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(12, 0);
            
            // Scrollbar background - IC10 uses scrollbar-bg sprite
            var scrollbarBg = scrollbarGO.AddComponent<Image>();
            var scrollbarBgSprite = LoadSlicedSprite("scrollbar-bg.png", 4);
            if (scrollbarBgSprite != null)
            {
                scrollbarBg.sprite = scrollbarBgSprite;
                scrollbarBg.type = Image.Type.Sliced;
            }
            scrollbarBg.color = Color.white; // #FFFFFFFF as shown in inspector
            
            // Sliding area
            var slidingAreaGO = new GameObject("Sliding Area");
            slidingAreaGO.transform.SetParent(scrollbarGO.transform, false);
            var slidingRect = slidingAreaGO.AddComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(2, 2);
            slidingRect.offsetMax = new Vector2(-2, -2);
            
            // Handle
            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(slidingAreaGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            
            // Handle image - IC10 uses scrollbar-handle sprite
            var handleImg = handleGO.AddComponent<Image>();
            var handleSprite = LoadSlicedSprite("scrollbar-handle.png", 4);
            if (handleSprite != null)
            {
                handleImg.sprite = handleSprite;
                handleImg.type = Image.Type.Sliced;
            }
            handleImg.color = Color.white; // #FFFFFFFF
            
            // Scrollbar component
            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;
            
            // Set button colors for handle
            var colors = scrollbar.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            scrollbar.colors = colors;
            
            // Connect to scroll rect
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2;
        }
        
        private void CreateResizeHandle(Transform parent)
        {
            // Larger invisible hit area for easier grabbing (10% smaller than before: 22 instead of 24)
            var handle = CreatePanel(parent, "ResizeHandle", 22, 22);
            var rect = handle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-1, 1);  // Moved slightly more into corner
            
            // Invisible background for hit testing
            var img = handle.AddComponent<Image>();
            img.color = Color.clear;
            
            // Create 3 diagonal grip lines (bottom-right corner style) - 10% smaller
            for (int i = 0; i < 3; i++)
            {
                var line = new GameObject($"GripLine{i}");
                line.transform.SetParent(handle.transform, false);
                var lineRect = line.AddComponent<RectTransform>();
                
                // Position lines diagonally from bottom-right corner, inset from border (10% smaller spacing)
                float offset = 12.5f + i * 4.5f;  // Reduced from 14 + i*5
                lineRect.anchorMin = new Vector2(1, 0);
                lineRect.anchorMax = new Vector2(1, 0);
                lineRect.pivot = new Vector2(1, 0);
                lineRect.sizeDelta = new Vector2(offset + 1.8f, 1.8f);  // Slightly thinner lines
                lineRect.anchoredPosition = new Vector2(-5, offset + 1.8f);  // Adjusted offset
                lineRect.localRotation = Quaternion.Euler(0, 0, 45);  // 45-degree diagonal
                
                var lineImg = line.AddComponent<Image>();
                lineImg.color = new Color(TEXT_DIM.r, TEXT_DIM.g, TEXT_DIM.b, 0.5f);  // Subtle gray
                lineImg.raycastTarget = false;
            }
            
            var resizer = handle.AddComponent<WindowResizeHandler>();
            resizer.WindowRect = _windowRect;
            resizer.MinSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
        }
        
        #endregion
        
        #region UI Helpers
        
        private Button CreateToolbarBtn(Transform parent, string label, float width)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            
            // Set explicit size via RectTransform - this is key!
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(width, 26);
            
            // LayoutElement tells the layout group our preferred size
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 26;
            le.minWidth = width;      // Don't allow shrinking
            le.minHeight = 26;
            le.flexibleWidth = 0;     // Don't expand
            le.flexibleHeight = 0;
            
            // Use IC10-style button-bg sprite
            var img = go.AddComponent<Image>();
            var buttonSprite = LoadSlicedSprite("button-bg.png", 7);  // 7px border for proper corners
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white; // White base, button colors control the actual color
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            // IC10 button colors:
            // Normal: #00000080 (black 50%)
            // Hover: #FFFFFF80 (white 50%) 
            // Press: #FF651620 (red ~12%)
            colors.normalColor = BTN_NORMAL;                                    // #00000080
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.5f);              // #FFFFFF80
            colors.pressedColor = new Color(1f, 0.396f, 0.125f, 0.125f);        // #FF651620
            colors.selectedColor = BTN_NORMAL;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label.ToUpper();  // All button text uppercase like IC10
            text.fontSize = 11;  // Slightly smaller to fit
            text.fontStyle = FontStyles.Bold;  // Bold like title
            text.characterSpacing = 2f;  // Less spacing to fit in buttons
            text.color = TEXT_NORMAL;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            ApplyGameFont(text);  // Use game's font for consistency
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(2, 0);
            textRect.offsetMax = new Vector2(-2, 0);
            
            _toolbarButtons.Add(btn);
            return btn;
        }
        
        private GameObject CreateTextButton(Transform parent, string label, float w, float h)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);
            
            var img = go.AddComponent<Image>();
            img.color = BTN_NORMAL;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = BTN_NORMAL;
            colors.highlightedColor = BTN_HOVER;
            colors.pressedColor = BTN_PRESS;
            btn.colors = colors;
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 13;
            text.color = TEXT_NORMAL;
            text.alignment = TextAlignmentOptions.Center;
            ApplyGameFont(text);  // Use game's font for consistency
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return go;
        }
        
        /// <summary>
        /// Creates a title bar button with a background (like toolbar buttons).
        /// </summary>
        private GameObject CreateTitleBarButton(Transform parent, string label, float w, float h)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);
            
            // Use IC10-style button-bg sprite
            var img = go.AddComponent<Image>();
            var buttonSprite = LoadSlicedSprite("button-bg.png", 7);
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = BTN_NORMAL;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.5f);
            colors.pressedColor = new Color(1f, 0.396f, 0.125f, 0.125f);
            colors.selectedColor = BTN_NORMAL;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 2f;
            text.color = TEXT_NORMAL;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            ApplyGameFont(text);
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(2, 0);
            textRect.offsetMax = new Vector2(-2, 0);
            
            return go;
        }
        
        /// <summary>
        /// Creates a thin separator line for the toolbar.
        /// </summary>
        private void CreateToolbarSeparator(Transform parent)
        {
            var sep = new GameObject("Sep");
            sep.transform.SetParent(parent, false);
            var sepRect = sep.AddComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(2, 20);
            var sepLE = sep.AddComponent<LayoutElement>();
            sepLE.preferredWidth = 2;
            sepLE.preferredHeight = 20;
            var sepImg = sep.AddComponent<Image>();
            sepImg.color = new Color(1f, 1f, 1f, 0.3f);
            sepImg.raycastTarget = false;
        }
        
        private void AddSimpleTooltip(GameObject target, string text)
        {
            var trigger = target.AddComponent<TooltipTrigger>();
            trigger.TooltipText = text;
        }
        
        #endregion
        
        #region Show/Hide
        
        public void Show()
        {
            if (_windowCanvas != null)
            {
                _windowCanvas.enabled = true;
                CursorManager.SetCursor(false);
                
                // Activate the block editor's input field
                if (_blockEditor != null && _blockEditor.InputField != null)
                {
                    _blockEditor.InputField.ActivateInputField();
                }
            }
        }
        
        public void Hide()
        {
            if (_windowCanvas != null)
            {
                SaveCurrentFile();
                _windowCanvas.enabled = false;
            }
        }
        
        public static void Toggle()
        {
            if (Instance == null) Initialize();
            
            if (IsOpen) Instance.Hide();
            else Instance.Show();
        }
        
        public static void UpdateWindow()
        {
            if (Instance == null) return;
            
            // Check for save/scene change even when window is closed
            Instance.CheckForSaveChange();
            
            if (!IsOpen) return;
            
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                Instance.SaveCurrentFile();
                Instance.SaveFileSystem();
            }
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                Instance.Hide();
            }
        }
        
        /// <summary>
        /// Check if the save/scene has changed and refresh the folder display if needed.
        /// Called every frame to detect save changes proactively.
        /// </summary>
        private void CheckForSaveChange()
        {
            var currentSave = GetCurrentSaveName();
            
            // Normalize null to empty string for comparison
            var currentNormalized = currentSave ?? "";
            var lastNormalized = _lastKnownSaveName ?? "";
            
            if (currentNormalized != lastNormalized)
            {
                Debug.Log($"[Station Notepad] Save changed from '{lastNormalized}' to '{currentNormalized}' - refreshing folder display");
                _lastKnownSaveName = currentSave;
                
                // Clear selection when save changes
                _selectedFolders.Clear();
                _currentFolder = null;
                _currentFile = null;
                
                // Refresh the display to show folders for the new save
                if (_fileTreeContent != null)
                {
                    RefreshFileTree();
                }
                
                // Update label
                if (_currentFileLabel != null)
                {
                    _currentFileLabel.text = "No file";
                }
            }
        }
        
        #endregion
        
        #region File System
        
        /// <summary>
        /// Gets the current save/station name. Returns null if in main menu.
        /// </summary>
        private string GetCurrentSaveName()
        {
            try
            {
                return XmlSaveLoad.Instance?.CurrentStationName;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Check if we're currently in the main menu (no active save)
        /// </summary>
        private bool IsInMainMenu()
        {
            return string.IsNullOrEmpty(GetCurrentSaveName());
        }
        
        private void LoadFileSystem()
        {
            _savePath = GetSavePath();
            var path = Path.Combine(_savePath, "notepad_files.json");
            
            if (File.Exists(path))
            {
                try
                {
                    _fileSystem = JsonConvert.DeserializeObject<PlannerFileSystem>(File.ReadAllText(path));
                    Debug.Log($"[Station Notepad] Loaded file system from {path}");
                    
                    // Don't filter here - filtering happens in BuildFolder for display only
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Station Notepad] Failed to load file system: {ex.Message}");
                    _fileSystem = new PlannerFileSystem();
                }
            }
            else
            {
                _fileSystem = new PlannerFileSystem();
                // Welcome file - make it global so it shows everywhere
                _fileSystem.RootFolder.PersistAcrossSaves = true;
                _fileSystem.RootFolder.Files.Add(new PlannerFile
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Welcome",
                    Content = "Welcome to Station Notepad!\n\n" +
                             "Your personal note-taking space.\n\n" +
                             "Shortcuts:\n" +
                             "- F2 = Toggle window\n" +
                             "- Escape = Close\n\n" +
                             "Click 'Note' to create a note!"
                });
                Debug.Log("[Station Notepad] Created new file system with welcome note");
            }
            
            // Fix folder tree visibility on open
            RefreshFileTree();
            
            // Select first visible file
            var firstFile = FindFirstFile(_fileSystem.RootFolder);
            if (firstFile != null)
                SelectFile(firstFile);
        }
        
        /// <summary>
        /// Check if a folder should be visible for the given save name.
        /// Used by BuildFolder to filter display without modifying data.
        /// </summary>
        private bool ShouldShowFolder(PlannerFolder folder, string currentSave)
        {
            // Global folders always show
            if (folder.PersistAcrossSaves)
                return true;
            
            // In main menu (no save loaded), only show global folders
            if (string.IsNullOrEmpty(currentSave))
                return false;
            
            // Per-save folder: show if SaveName matches current save
            return folder.SaveName == currentSave;
        }
        
        private void SaveFileSystem()
        {
            if (_fileSystem == null)
            {
                Debug.Log("[Station Notepad] SaveFileSystem: _fileSystem is null");
                return;
            }
            try
            {
                Debug.Log($"[Station Notepad] Saving to: {_savePath}");
                if (!Directory.Exists(_savePath))
                {
                    Directory.CreateDirectory(_savePath);
                    Debug.Log($"[Station Notepad] Created directory: {_savePath}");
                }
                
                // Save the full file system (we keep all folders in memory, filtering only on display)
                var fullPath = Path.Combine(_savePath, "notepad_files.json");
                var json = JsonConvert.SerializeObject(_fileSystem, Formatting.Indented);
                File.WriteAllText(fullPath, json);
                
                Debug.Log($"[Station Notepad] Saved file system to {fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Station Notepad] Save failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private string GetSavePath()
        {
            // Try BepInEx scripts folder first (where the mod DLL lives)
            try
            {
                var bepinPath = Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "StationNotepadData");
                Debug.Log($"[Station Notepad] Save path: {bepinPath}");
                return bepinPath;
            }
            catch
            {
                // Fallback to Documents
                var docPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "My Games", "Stationeers", "StationNotepadData"
                );
                Debug.Log($"[Station Notepad] Fallback save path: {docPath}");
                return docPath;
            }
        }
        
        private void RefreshFileTree()
        {
            foreach (Transform child in _fileTreeContent)
                Destroy(child.gameObject);
                
            BuildFolder(_fileSystem.RootFolder, 0);
        }
        
        private void BuildFolder(PlannerFolder folder, int depth)
        {
            var currentSave = GetCurrentSaveName();
            
            foreach (var sub in folder.SubFolders)
            {
                // Filter: only show folders that should be visible for current save
                if (!ShouldShowFolder(sub, currentSave))
                    continue;
                    
                CreateFolderRow(sub, depth);
                if (sub.IsExpanded)
                    BuildFolder(sub, depth + 1);
            }
            
            foreach (var file in folder.Files)
                CreateFileRow(file, depth);
        }
        
        private void CreateFolderRow(PlannerFolder folder, int depth)
        {
            var row = new GameObject($"F_{folder.Name}");
            row.transform.SetParent(_fileTreeContent, false);
            
            var rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 24);
            
            // LayoutElement for proper sizing in scroll view
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 24;
            le.preferredHeight = 24;
            
            bool selected = _currentFolder == folder || _selectedFolders.Contains(folder);
            
            var bg = row.AddComponent<Image>();
            bg.color = selected ? SELECTED : Color.clear;
            
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2;
            hlg.padding = new RectOffset(depth * 12 + 2, 4, 2, 2);  // Less left padding
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            
            // Arrow
            var arrowGO = new GameObject("Arrow");
            arrowGO.transform.SetParent(row.transform, false);
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(14, 20);
            var arrow = arrowGO.AddComponent<TextMeshProUGUI>();
            arrow.text = folder.IsExpanded ? "v" : ">";
            arrow.fontSize = 12;
            arrow.color = selected ? ACCENT : TEXT_DIM;
            arrow.alignment = TextAlignmentOptions.MidlineLeft;
            
            // Name (no emoji - TMP doesn't support it)
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(row.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(110, 20);  // Reduced to make room for G button
            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = "[" + folder.Name + "]";

            nameText.fontSize = 12;
            nameText.fontStyle = FontStyles.Bold;
            // Light blue if persists across saves, orange if selected, otherwise normal
            nameText.color = folder.PersistAcrossSaves ? FOLDER_PERSISTENT : (selected ? ACCENT : TEXT_NORMAL);
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            
            // Global toggle button (G) - dark button like other buttons
            var globalGO = new GameObject("Global");
            globalGO.transform.SetParent(row.transform, false);
            var globalRect = globalGO.AddComponent<RectTransform>();
            globalRect.sizeDelta = new Vector2(20, 20);
            
            // Dark background like other buttons
            var globalBg = globalGO.AddComponent<Image>();
            globalBg.color = BTN_NORMAL;  // Dark button background
            
            var globalBtn = globalGO.AddComponent<Button>();
            globalBtn.targetGraphic = globalBg;
            var globalColors = globalBtn.colors;
            globalColors.normalColor = BTN_NORMAL;
            globalColors.highlightedColor = BTN_HOVER;  // Slightly darker on hover
            globalColors.pressedColor = BTN_PRESS;      // Even darker on press, no white flicker
            globalColors.selectedColor = BTN_NORMAL;    // No change when selected
            globalColors.disabledColor = BTN_NORMAL;
            globalBtn.colors = globalColors;
            
            var globalTextGO = new GameObject("Text");
            globalTextGO.transform.SetParent(globalGO.transform, false);
            var globalTextRect = globalTextGO.AddComponent<RectTransform>();
            globalTextRect.anchorMin = Vector2.zero;
            globalTextRect.anchorMax = Vector2.one;
            globalTextRect.offsetMin = Vector2.zero;
            globalTextRect.offsetMax = Vector2.zero;
            var globalText = globalTextGO.AddComponent<TextMeshProUGUI>();
            globalText.text = "G";  // Always show G
            globalText.fontSize = 11;
            globalText.fontStyle = FontStyles.Bold;
            globalText.characterSpacing = 2f;
            // Blue when global (persists), dim when per-save
            globalText.color = folder.PersistAcrossSaves ? new Color(0.4f, 0.7f, 1f, 1f) : TEXT_DIM;
            globalText.alignment = TextAlignmentOptions.Center;
            globalText.raycastTarget = false;
            ApplyGameFont(globalText);
            
            var folderRef = folder;
            globalBtn.onClick.AddListener(() => ToggleFolderPersistence(folderRef));
            AddSimpleTooltip(globalGO, folder.PersistAcrossSaves ? 
                "Global folder (persists across all saves). Click to make per-save." : 
                "Per-save folder. Click to make global (persists across all saves).");
            
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = selected ? SELECTED : Color.clear;
            colors.highlightedColor = selected ? SELECTED : new Color(0.2f, 0.3f, 0.4f, 0.5f);
            colors.pressedColor = SELECTED;
            btn.colors = colors;
            
            // NOTE: No onClick listener here - FolderShiftClickHandler handles ALL clicks
            // This allows proper shift-click detection for multi-select
            
            // Add drop handler for receiving dragged files/folders
            var dropHandler = row.AddComponent<FolderDropHandler>();
            dropHandler.Folder = folder;
            dropHandler.Window = this;
            dropHandler.Background = bg;
            
            // Add drag handler for dragging this folder
            var dragHandler = row.AddComponent<FolderDragHandler>();
            dragHandler.Folder = folder;
            dragHandler.Window = this;
            
            // Add shift-click handler for multi-select
            var shiftClickHandler = row.AddComponent<FolderShiftClickHandler>();
            shiftClickHandler.Folder = folder;
            shiftClickHandler.Window = this;
        }
        
        /// <summary>
        /// Deselect all folders and files
        /// </summary>
        private void DeselectAll()
        {
            _currentFolder = null;
            _currentFile = null;
            _selectedFolders.Clear();
            
            if (_currentFileLabel != null)
                _currentFileLabel.text = "No file";
            
            RefreshFileTree();
        }
        
        private void SelectFolder(PlannerFolder folder)
        {
            SelectFolder(folder, false);
        }
        
        private void SelectFolder(PlannerFolder folder, bool shiftHeld)
        {
            if (shiftHeld)
            {
                // Multi-select mode: toggle this folder in selection (don't change expansion)
                if (_selectedFolders.Contains(folder))
                {
                    _selectedFolders.Remove(folder);
                    if (_currentFolder == folder)
                        _currentFolder = _selectedFolders.Count > 0 ? new List<PlannerFolder>(_selectedFolders)[0] : null;
                }
                else
                {
                    _selectedFolders.Add(folder);
                    _currentFolder = folder;
                }
            }
            else
            {
                // Single-select mode: toggle expansion and select
                folder.IsExpanded = !folder.IsExpanded;
                _selectedFolders.Clear();
                _selectedFolders.Add(folder);
                _currentFolder = folder;
            }
            
            _currentFile = null; // Deselect file when folder is selected
            
            if (_currentFileLabel != null)
            {
                if (_selectedFolders.Count > 1)
                    _currentFileLabel.text = $"[{_selectedFolders.Count} folders]";
                else
                    _currentFileLabel.text = $"[{folder.Name}]";
            }
            
            RefreshFileTree();
        }
        
        private void CreateFileRow(PlannerFile file, int depth)
        {
            var row = new GameObject($"f_{file.Name}");
            row.transform.SetParent(_fileTreeContent, false);
            
            var rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 24);
            
            // LayoutElement for proper sizing in scroll view
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 24;
            le.preferredHeight = 24;
            
            bool selected = _currentFile == file;
            
            var bg = row.AddComponent<Image>();
            bg.color = selected ? SELECTED : Color.clear;
            
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2;
            hlg.padding = new RectOffset(depth * 12 + 14, 2, 2, 2);  // Shift left a bit
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            
            // Name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(row.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(100, 20);
            var nameText = nameGO.AddComponent<TextMeshProUGUI>();
            nameText.text = file.Name;
            nameText.fontSize = 12;
            nameText.color = selected ? ACCENT : TEXT_NORMAL;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            
            // Move up button (^)
            var upGO = new GameObject("MoveUp");
            upGO.transform.SetParent(row.transform, false);
            var upRect = upGO.AddComponent<RectTransform>();
            upRect.sizeDelta = new Vector2(16, 16);
            var upBg = upGO.AddComponent<Image>();
            upBg.color = BTN_NORMAL;
            var upBtn = upGO.AddComponent<Button>();
            upBtn.targetGraphic = upBg;
            var upColors = upBtn.colors;
            upColors.normalColor = BTN_NORMAL;
            upColors.highlightedColor = BTN_HOVER;
            upColors.pressedColor = BTN_PRESS;
            upBtn.colors = upColors;
            var upTextGO = new GameObject("Text");
            upTextGO.transform.SetParent(upGO.transform, false);
            var upTextRect = upTextGO.AddComponent<RectTransform>();
            upTextRect.anchorMin = Vector2.zero;
            upTextRect.anchorMax = Vector2.one;
            upTextRect.offsetMin = Vector2.zero;
            upTextRect.offsetMax = Vector2.zero;
            var upText = upTextGO.AddComponent<TextMeshProUGUI>();
            upText.text = "^";
            upText.fontSize = 10;
            upText.color = TEXT_NORMAL;
            upText.alignment = TextAlignmentOptions.Center;
            var f1 = file;
            upBtn.onClick.AddListener(() => MoveFileUp(f1));
            
            // Move down button (v)
            var downGO = new GameObject("MoveDown");
            downGO.transform.SetParent(row.transform, false);
            var downRect = downGO.AddComponent<RectTransform>();
            downRect.sizeDelta = new Vector2(16, 16);
            var downBg = downGO.AddComponent<Image>();
            downBg.color = BTN_NORMAL;
            var downBtn = downGO.AddComponent<Button>();
            downBtn.targetGraphic = downBg;
            var downColors = downBtn.colors;
            downColors.normalColor = BTN_NORMAL;
            downColors.highlightedColor = BTN_HOVER;
            downColors.pressedColor = BTN_PRESS;
            downBtn.colors = downColors;
            var downTextGO = new GameObject("Text");
            downTextGO.transform.SetParent(downGO.transform, false);
            var downTextRect = downTextGO.AddComponent<RectTransform>();
            downTextRect.anchorMin = Vector2.zero;
            downTextRect.anchorMax = Vector2.one;
            downTextRect.offsetMin = Vector2.zero;
            downTextRect.offsetMax = Vector2.zero;
            var downText = downTextGO.AddComponent<TextMeshProUGUI>();
            downText.text = "v";
            downText.fontSize = 10;
            downText.color = TEXT_NORMAL;
            downText.alignment = TextAlignmentOptions.Center;
            var f2 = file;
            downBtn.onClick.AddListener(() => MoveFileDown(f2));
            
            // Main row click selects file
            var mainBtn = row.AddComponent<Button>();
            mainBtn.targetGraphic = bg;
            var colors = mainBtn.colors;
            colors.normalColor = selected ? SELECTED : Color.clear;
            colors.highlightedColor = selected ? SELECTED : new Color(0.2f, 0.3f, 0.4f, 0.4f);
            colors.pressedColor = SELECTED;
            mainBtn.colors = colors;
            
            var f = file;
            mainBtn.onClick.AddListener(() => SelectFile(f));
            
            // Add drag handler for moving files between folders
            var dragHandler = row.AddComponent<FileDragHandler>();
            dragHandler.File = file;
            dragHandler.Window = this;
        }
        
        private void SelectFile(PlannerFile file)
        {
            SaveCurrentFile();
            _currentFile = file;
            _currentFolder = null; // Deselect folder when file is selected
            
            if (_blockEditor != null && _blockEditor.IsInitialized && file != null)
            {
                // Try to load as block editor format first, fall back to plain text
                if (!string.IsNullOrEmpty(file.Content) && file.Content.TrimStart().StartsWith("{"))
                {
                    // Looks like JSON - try to load as block editor format
                    _blockEditor.DeserializeDocument(file.Content);
                }
                else
                {
                    // Plain text - load with all Normal styles
                    _blockEditor.LoadFromPlainText(file.Content ?? "");
                }
                
                _blockEditor.InputField.ActivateInputField();
            }
            
            if (_currentFileLabel != null)
                _currentFileLabel.text = file?.Name ?? "No file";
                
            _hasUnsavedChanges = false;
            RefreshFileTree();
        }
        
        private void SaveCurrentFile()
        {
            if (_currentFile != null && _blockEditor != null && _blockEditor.IsInitialized && _hasUnsavedChanges)
            {
                // Save as block editor format (JSON with line styles)
                _currentFile.Content = _blockEditor.SerializeDocument();
                _currentFile.LastModified = DateTime.Now;
                _hasUnsavedChanges = false;
            }
        }
        
        /// <summary>
        /// Auto-save handler - called whenever editor content changes
        /// </summary>
        private void OnEditorContentChanged()
        {
            _hasUnsavedChanges = true;
            SaveCurrentFile();
            SaveFileSystem();
        }
        
        #endregion
        
        #region Actions
        
        private void OnNewFile()
        {
            Debug.Log("[Station Planner] OnNewFile called");
            if (_fileSystem == null)
            {
                Debug.LogError("[Station Planner] _fileSystem is null!");
                _fileSystem = new PlannerFileSystem();
            }
            
            // Determine target folder - use selected folder or root
            var targetFolder = _currentFolder ?? _fileSystem.RootFolder;
            
            var file = new PlannerFile
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Note " + (CountAllFiles(_fileSystem.RootFolder) + 1),
                Content = ""
            };
            Debug.Log($"[Station Planner] Created file: {file.Name} in folder: {targetFolder.Name}");
            targetFolder.Files.Add(file);
            
            // Make sure parent folder is expanded so we can see the new file
            targetFolder.IsExpanded = true;
            
            SaveFileSystem();
            RefreshFileTree();
            SelectFile(file);
        }
        
        private int CountAllFiles(PlannerFolder folder)
        {
            int count = folder.Files.Count;
            foreach (var sub in folder.SubFolders)
                count += CountAllFiles(sub);
            return count;
        }
        
        private void OnNewFolder()
        {
            Debug.Log("[Station Notepad] OnNewFolder called");
            if (_fileSystem == null)
            {
                Debug.LogError("[Station Notepad] _fileSystem is null!");
                _fileSystem = new PlannerFileSystem();
            }
            
            // Create folder inside selected folder, or in root if nothing selected
            var targetFolder = _currentFolder ?? _fileSystem.RootFolder;
            
            var currentSave = GetCurrentSaveName();
            var isMainMenu = string.IsNullOrEmpty(currentSave);
            
            var folder = new PlannerFolder
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Folder " + (CountAllFolders(_fileSystem.RootFolder) + 1),
                IsExpanded = true,
                // In main menu, folders are automatically global
                // Otherwise, they belong to the current save
                PersistAcrossSaves = isMainMenu,
                SaveName = isMainMenu ? "" : currentSave
            };
            
            Debug.Log($"[Station Notepad] Created folder: {folder.Name} in: {targetFolder.Name}, Global: {folder.PersistAcrossSaves}, SaveName: {folder.SaveName}");
            targetFolder.SubFolders.Add(folder);
            
            // Expand parent so we see the new folder
            targetFolder.IsExpanded = true;
            
            SaveFileSystem();
            RefreshFileTree();
            SelectFolder(folder);
        }
        
        private int CountAllFolders(PlannerFolder folder)
        {
            int count = folder.SubFolders.Count;
            foreach (var sub in folder.SubFolders)
                count += CountAllFolders(sub);
            return count;
        }
        
        private void OnRename()
        {
            // Rename folder if folder is selected
            if (_currentFolder != null)
            {
                ShowInputDialog("Rename Folder", _currentFolder.Name, (newName) =>
                {
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        _currentFolder.Name = newName.Trim();
                        SaveFileSystem();
                        RefreshFileTree();
                        if (_currentFileLabel != null)
                            _currentFileLabel.text = "[" + _currentFolder.Name + "]";
                    }
                });
                return;
            }
            
            // Rename file if file is selected
            if (_currentFile != null)
            {
                ShowInputDialog("Rename", _currentFile.Name, (newName) =>
                {
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        _currentFile.Name = newName.Trim();
                        SaveFileSystem();
                        RefreshFileTree();
                        if (_currentFileLabel != null)
                            _currentFileLabel.text = _currentFile.Name;
                    }
                });
            }
        }
        
        private void OnDelete()
        {
            // Delete multiple folders if multiple are selected
            if (_selectedFolders.Count > 1)
            {
                foreach (var folder in new List<PlannerFolder>(_selectedFolders))
                {
                    var parent = FindParentFolder(_fileSystem.RootFolder, folder);
                    if (parent != null)
                    {
                        parent.SubFolders.Remove(folder);
                    }
                }
                _selectedFolders.Clear();
                _currentFolder = null;
                if (_currentFileLabel != null)
                    _currentFileLabel.text = "No selection";
                SaveFileSystem();
                RefreshFileTree();
                return;
            }
            
            // Delete folder if folder is selected
            if (_currentFolder != null)
            {
                // Find parent folder and remove this folder from it
                var parent = FindParentFolder(_fileSystem.RootFolder, _currentFolder);
                if (parent != null)
                {
                    parent.SubFolders.Remove(_currentFolder);
                    _selectedFolders.Remove(_currentFolder);
                    _currentFolder = null;
                    if (_currentFileLabel != null)
                        _currentFileLabel.text = "No selection";
                    SaveFileSystem();
                    RefreshFileTree();
                }
                return;
            }
            
            // Delete file if file is selected
            if (_currentFile != null)
            {
                // Find and remove file from its parent folder
                RemoveFileFromAllFolders(_fileSystem.RootFolder, _currentFile);
                
                // Select another file if available
                var firstFile = FindFirstFile(_fileSystem.RootFolder);
                if (firstFile != null)
                    SelectFile(firstFile);
                else
                {
                    _currentFile = null;
                    if (_blockEditor != null && _blockEditor.IsInitialized)
                        _blockEditor.LoadFromPlainText("");
                    if (_currentFileLabel != null)
                        _currentFileLabel.text = "No file";
                }
                
                SaveFileSystem();
                RefreshFileTree();
            }
        }
        
        private PlannerFolder FindParentFolder(PlannerFolder searchIn, PlannerFolder target)
        {
            if (searchIn.SubFolders.Contains(target))
                return searchIn;
            foreach (var sub in searchIn.SubFolders)
            {
                var result = FindParentFolder(sub, target);
                if (result != null) return result;
            }
            return null;
        }
        
        private PlannerFolder FindParentFolderOfFile(PlannerFolder searchIn, PlannerFile target)
        {
            if (searchIn.Files.Contains(target))
                return searchIn;
            foreach (var sub in searchIn.SubFolders)
            {
                var result = FindParentFolderOfFile(sub, target);
                if (result != null) return result;
            }
            return null;
        }
        
        private void RemoveFileFromAllFolders(PlannerFolder folder, PlannerFile file)
        {
            folder.Files.Remove(file);
            foreach (var sub in folder.SubFolders)
                RemoveFileFromAllFolders(sub, file);
        }
        
        private PlannerFile FindFirstFile(PlannerFolder folder)
        {
            if (folder.Files.Count > 0)
                return folder.Files[0];
            foreach (var sub in folder.SubFolders)
            {
                var file = FindFirstFile(sub);
                if (file != null) return file;
            }
            return null;
        }
        
        private void MoveFileUp(PlannerFile file)
        {
            var parent = FindParentFolderOfFile(_fileSystem.RootFolder, file);
            if (parent == null) return;
            
            int idx = parent.Files.IndexOf(file);
            if (idx > 0)
            {
                parent.Files.RemoveAt(idx);
                parent.Files.Insert(idx - 1, file);
                SaveFileSystem();
                RefreshFileTree();
            }
        }
        
        private void MoveFileDown(PlannerFile file)
        {
            var parent = FindParentFolderOfFile(_fileSystem.RootFolder, file);
            if (parent == null) return;
            
            int idx = parent.Files.IndexOf(file);
            if (idx < parent.Files.Count - 1)
            {
                parent.Files.RemoveAt(idx);
                parent.Files.Insert(idx + 1, file);
                SaveFileSystem();
                RefreshFileTree();
            }
        }
        
        /// <summary>
        /// Moves a file from its current folder to a target folder (for drag and drop)
        /// </summary>
        public void MoveFileToFolder(PlannerFile file, PlannerFolder targetFolder)
        {
            if (file == null || targetFolder == null) return;
            
            // Find current parent folder
            var currentParent = FindParentFolderOfFile(_fileSystem.RootFolder, file);
            if (currentParent == null || currentParent == targetFolder) return;
            
            // Remove from current folder
            currentParent.Files.Remove(file);
            
            // Add to target folder
            targetFolder.Files.Add(file);
            targetFolder.IsExpanded = true;  // Expand target to show the moved file
            
            Debug.Log($"[Station Notepad] Moved '{file.Name}' from '{currentParent.Name}' to '{targetFolder.Name}'");
            
            SaveFileSystem();
            RefreshFileTree();
        }
        
        /// <summary>
        /// Moves a folder from its current parent to a target folder (for drag and drop)
        /// </summary>
        public void MoveFolderToFolder(PlannerFolder folder, PlannerFolder targetFolder)
        {
            if (folder == null || targetFolder == null) return;
            
            // Don't allow moving a folder into itself or its descendants
            if (IsDescendantOf(targetFolder, folder)) return;
            
            // Find current parent folder
            var currentParent = FindParentFolder(_fileSystem.RootFolder, folder);
            if (currentParent == null || currentParent == targetFolder) return;
            
            // Remove from current parent
            currentParent.SubFolders.Remove(folder);
            
            // Add to target folder
            targetFolder.SubFolders.Add(folder);
            targetFolder.IsExpanded = true;  // Expand target to show the moved folder
            
            Debug.Log($"[Station Notepad] Moved folder '{folder.Name}' from '{currentParent.Name}' to '{targetFolder.Name}'");
            
            SaveFileSystem();
            RefreshFileTree();
        }
        
        /// <summary>
        /// Reorders a folder by placing it before or after another folder (sibling reorder)
        /// </summary>
        public void ReorderFolder(PlannerFolder folder, PlannerFolder referenceFolder, bool insertBefore)
        {
            if (folder == null || referenceFolder == null || folder == referenceFolder) return;
            
            // Find parent of the reference folder (this is where we'll insert)
            var targetParent = FindParentFolder(_fileSystem.RootFolder, referenceFolder);
            if (targetParent == null) return;
            
            // Find current parent of the dragged folder
            var currentParent = FindParentFolder(_fileSystem.RootFolder, folder);
            if (currentParent == null) return;
            
            // Don't allow moving a folder into its own descendants
            if (IsDescendantOf(targetParent, folder)) return;
            
            // Remove from current parent
            currentParent.SubFolders.Remove(folder);
            
            // Find index of reference folder in target parent
            int refIndex = targetParent.SubFolders.IndexOf(referenceFolder);
            if (refIndex < 0)
            {
                // Reference folder not found, just add at end
                targetParent.SubFolders.Add(folder);
            }
            else
            {
                // Insert before or after reference
                int insertIndex = insertBefore ? refIndex : refIndex + 1;
                targetParent.SubFolders.Insert(insertIndex, folder);
            }
            
            Debug.Log($"[Station Notepad] Reordered folder '{folder.Name}' {(insertBefore ? "before" : "after")} '{referenceFolder.Name}'");
            
            SaveFileSystem();
            RefreshFileTree();
        }
        
        /// <summary>
        /// Check if potentialDescendant is a descendant of potentialAncestor
        /// </summary>
        private bool IsDescendantOf(PlannerFolder potentialDescendant, PlannerFolder potentialAncestor)
        {
            if (potentialDescendant == potentialAncestor) return true;
            
            foreach (var sub in potentialAncestor.SubFolders)
            {
                if (IsDescendantOf(potentialDescendant, sub))
                    return true;
            }
            
            return false;
        }
        
        private void ShowInputDialog(string title, string defaultValue, Action<string> onConfirm)
        {
            var dialog = new GameObject("InputDialog");
            dialog.transform.SetParent(_windowPanel.transform, false);
            
            var rect = dialog.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 130);
            rect.anchoredPosition = Vector2.zero;
            
            // Outer blue background (like dialog-bg)
            var bgImg = dialog.AddComponent<Image>();
            var bgSprite = LoadSlicedSprite("dialog-bg.png", 18);
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0f, 0.165f, 0.278f, 0.98f); // IC10 blue
            
            // Orange border around dialog (like IC10's Outline which uses dialog-outline with #FF6516FF)
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(dialog.transform, false);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGO.AddComponent<Image>();
            var borderSprite = LoadSlicedSprite("dialog-outline.png", 18); // IC10 uses 18px border
            if (borderSprite != null)
            {
                borderImg.sprite = borderSprite;
                borderImg.type = Image.Type.Sliced;
                borderImg.fillCenter = false;
            }
            borderImg.color = ACCENT_ORANGE;  // #FF6516FF like IC10
            borderImg.raycastTarget = false;
            
            // Title - white, uppercase, bold like STATION PLANNER
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(dialog.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = title.ToUpper();
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = TEXT_NORMAL;  // White like STATION PLANNER
            titleText.characterSpacing = 2f;
            titleText.alignment = TextAlignmentOptions.Center;
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -12);
            titleRect.sizeDelta = new Vector2(0, 24);
            
            // Input container with dark background
            var inputContainerGO = new GameObject("InputContainer");
            inputContainerGO.transform.SetParent(dialog.transform, false);
            var inputContainerRect = inputContainerGO.AddComponent<RectTransform>();
            inputContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputContainerRect.sizeDelta = new Vector2(260, 32);
            inputContainerRect.anchoredPosition = new Vector2(0, 8);
            
            // Fill background (button-bg)
            var fillImg = inputContainerGO.AddComponent<Image>();
            var fillSprite = LoadSlicedSprite("button-bg.png", 7);
            if (fillSprite != null)
            {
                fillImg.sprite = fillSprite;
                fillImg.type = Image.Type.Sliced;
                fillImg.fillCenter = true;
            }
            fillImg.color = new Color(0f, 0f, 0f, 0.75f);
            
            // Outline (slot-outline)
            var outlineGO = new GameObject("Outline");
            outlineGO.transform.SetParent(inputContainerGO.transform, false);
            var outlineRect = outlineGO.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            var outlineImg = outlineGO.AddComponent<Image>();
            var outlineSprite = LoadSlicedSprite("slot-outline.png", 8);
            if (outlineSprite != null)
            {
                outlineImg.sprite = outlineSprite;
                outlineImg.type = Image.Type.Sliced;
                outlineImg.fillCenter = false;
            }
            outlineImg.color = new Color(0f, 0f, 0f, 0.5f);
            outlineImg.raycastTarget = false;
            
            // Try to use the prefab-based input field for proper cursor support
            TMP_InputField inputField = BlockEditor.CreateDialogInputField(inputContainerGO.transform);
            
            if (inputField != null)
            {
                // Successfully loaded from prefab - configure it to fill the container
                var inputRect = inputField.GetComponent<RectTransform>();
                inputRect.anchorMin = Vector2.zero;
                inputRect.anchorMax = Vector2.one;
                inputRect.offsetMin = Vector2.zero;
                inputRect.offsetMax = Vector2.zero;
                inputRect.sizeDelta = Vector2.zero;  // Use anchors for sizing
                
                // Also configure the text viewport to fill the input field properly
                if (inputField.textViewport != null)
                {
                    var viewportRect = inputField.textViewport;
                    viewportRect.anchorMin = Vector2.zero;
                    viewportRect.anchorMax = Vector2.one;
                    viewportRect.offsetMin = new Vector2(8, 4);
                    viewportRect.offsetMax = new Vector2(-8, -4);
                }
                
                // Configure the text component
                if (inputField.textComponent != null)
                {
                    var textRect = inputField.textComponent.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    inputField.textComponent.fontSize = 14;
                    inputField.textComponent.color = TEXT_NORMAL;
                    inputField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
                }
                
                inputField.text = defaultValue;
                inputField.caretColor = ACCENT;
                inputField.customCaretColor = true;
                inputField.caretWidth = 2;
                inputField.caretBlinkRate = 0.85f;
                inputField.selectionColor = SELECTED;
            }
            else
            {
                // Fallback to manual creation if prefab loading fails
                var textAreaGO = new GameObject("TextArea");
                textAreaGO.transform.SetParent(inputContainerGO.transform, false);
                var textAreaRect = textAreaGO.AddComponent<RectTransform>();
                textAreaRect.anchorMin = Vector2.zero;
                textAreaRect.anchorMax = Vector2.one;
                textAreaRect.offsetMin = new Vector2(8, 4);
                textAreaRect.offsetMax = new Vector2(-8, -4);
                textAreaGO.AddComponent<RectMask2D>();
                
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(textAreaGO.transform, false);
                var textComp = textGO.AddComponent<TextMeshProUGUI>();
                textComp.fontSize = 14;
                textComp.color = TEXT_NORMAL;
                textComp.alignment = TextAlignmentOptions.MidlineLeft;
                textComp.raycastTarget = false;
                var textCompRect = textGO.GetComponent<RectTransform>();
                textCompRect.anchorMin = Vector2.zero;
                textCompRect.anchorMax = Vector2.one;
                textCompRect.offsetMin = Vector2.zero;
                textCompRect.offsetMax = Vector2.zero;
                
                inputField = inputContainerGO.AddComponent<TMP_InputField>();
                inputField.textComponent = textComp;
                inputField.textViewport = textAreaRect;
                inputField.text = defaultValue;
                inputField.targetGraphic = fillImg;
                inputField.interactable = true;
                inputField.caretColor = ACCENT;
                inputField.customCaretColor = true;
                inputField.caretWidth = 2;
                inputField.caretBlinkRate = 0.85f;
                inputField.selectionColor = SELECTED;
            }
            
            // Buttons
            var btnsGO = new GameObject("Buttons");
            btnsGO.transform.SetParent(dialog.transform, false);
            var btnsRect = btnsGO.AddComponent<RectTransform>();
            btnsRect.anchorMin = new Vector2(0.5f, 0);
            btnsRect.anchorMax = new Vector2(0.5f, 0);
            btnsRect.pivot = new Vector2(0.5f, 0);
            btnsRect.anchoredPosition = new Vector2(0, 10);
            btnsRect.sizeDelta = new Vector2(180, 26);
            
            var hlg = btnsGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            
            var dialogInputField = inputField;  // Capture for lambdas
            var okBtn = CreateToolbarBtn(btnsGO.transform, "OK", 70);
            okBtn.onClick.AddListener(() =>
            {
                onConfirm?.Invoke(dialogInputField.text);
                Destroy(dialog);
            });
            
            var cancelBtn = CreateToolbarBtn(btnsGO.transform, "Cancel", 70);
            cancelBtn.onClick.AddListener(() => Destroy(dialog));
            
            // Focus input field after a frame (needed for EventSystem to register it)
            StartCoroutine(ActivateInputFieldDelayed(inputField));
        }
        
        private System.Collections.IEnumerator ActivateInputFieldDelayed(TMP_InputField inputField)
        {
            yield return null;  // Wait one frame
            if (inputField != null)
            {
                // For dialogs, we need Select() to make the EventSystem aware of it
                // This is different from the main editor - dialogs are newly created GameObjects
                inputField.Select();
                inputField.ActivateInputField();
            }
        }
        
        /// <summary>
        /// Apply a line style to the current line in the block editor.
        /// </summary>
        private void ApplyLineStyle(LineStyle style)
        {
            if (_blockEditor == null || !_blockEditor.IsInitialized) return;
            
            _blockEditor.ToggleStyleOnCurrentLine(style);
            _hasUnsavedChanges = true;
            
            // Re-focus the input field after button click
            _blockEditor.InputField.ActivateInputField();
        }
        
        /// <summary>
        /// Toggle game pause state (like Stationpedia does)
        /// </summary>
        private void TogglePause()
        {
            // Only allow pause in single player (no clients, not a client ourselves)
            if (!NetworkManager.IsClient && NetworkBase.Clients.Count == 0 && !InventoryManager.Instance.InGameMenuOpen)
            {
                _isPaused = !_isPaused;
                WorldManager.SetGamePause(_isPaused);
                
                // Update button appearance to show current state
                // Keep text as || always, but change background color when paused
                if (_pauseButtonText != null)
                {
                    _pauseButtonText.color = _isPaused ? ACCENT : TEXT_NORMAL;  // Orange text when paused
                }
                if (_pauseButtonBg != null)
                {
                    // Grey background when paused to show it's active
                    _pauseButtonBg.color = _isPaused ? new Color(0.4f, 0.4f, 0.4f, 0.7f) : BTN_NORMAL;
                }
                
                StationpediaAscendedMod.Log?.LogInfo($"[StationPlanner] Game pause toggled: {_isPaused}");
            }
        }
        
        /// <summary>
        /// Toggle folder persistence setting (across saves vs per-save)
        /// </summary>
        private void ToggleFolderPersistence(PlannerFolder folder)
        {
            if (folder == null) return;
            
            folder.PersistAcrossSaves = !folder.PersistAcrossSaves;
            
            // When making per-save, assign to current save
            // When making global, clear the save name
            if (folder.PersistAcrossSaves)
            {
                folder.SaveName = "";
            }
            else
            {
                // Assign to current save (or keep in current location if already set)
                var currentSave = GetCurrentSaveName();
                if (!string.IsNullOrEmpty(currentSave))
                {
                    folder.SaveName = currentSave;
                }
                // If in main menu, can't make per-save - keep it global
                else
                {
                    folder.PersistAcrossSaves = true;
                    Debug.Log("[Station Notepad] Can't make folder per-save in main menu - keeping global");
                }
            }
            
            Debug.Log($"[Station Notepad] Folder '{folder.Name}' persistence: {(folder.PersistAcrossSaves ? "Global" : "Per-save (" + folder.SaveName + ")")}");
            
            SaveFileSystem();
            RefreshFileTree();
        }
        
        /// <summary>
        /// Update a specific folder row's appearance without rebuilding entire tree
        /// </summary>
        private void UpdateFolderRowAppearance(PlannerFolder folder)
        {
            // For now, just refresh the tree - optimizing this would require tracking folder row GameObjects
            RefreshFileTree();
        }
        
        /// <summary>
        /// Handler for Global toolbar button - toggles selected folder's persistence
        /// </summary>
        private void OnToggleFolderGlobal()
        {
            // Use the currently selected folder
            if (_currentFolder == null)
            {
                Debug.Log("[Station Planner] No folder selected - cannot toggle global/per-save");
                return;
            }
            
            ToggleFolderPersistence(_currentFolder);
        }
        
        /// <summary>
        /// Show the Station Planner help/guide dialog
        /// </summary>
        private void ShowHelpDialog()
        {
            var dialog = new GameObject("HelpDialog");
            dialog.transform.SetParent(_windowPanel.transform, false);
            
            var rect = dialog.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 340);
            rect.anchoredPosition = Vector2.zero;
            
            // Outer blue background (like dialog-bg)
            var bgImg = dialog.AddComponent<Image>();
            var bgSprite = LoadSlicedSprite("dialog-bg.png", 18);
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0f, 0.165f, 0.278f, 0.98f); // IC10 blue
            
            // Orange border around dialog (like IC10's Outline)
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(dialog.transform, false);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var borderImg = borderGO.AddComponent<Image>();
            var borderSprite = LoadSlicedSprite("dialog-outline.png", 18);
            if (borderSprite != null)
            {
                borderImg.sprite = borderSprite;
                borderImg.type = Image.Type.Sliced;
                borderImg.fillCenter = false;
            }
            borderImg.color = ACCENT_ORANGE;
            borderImg.raycastTarget = false;
            
            // Title - white, uppercase, bold
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(dialog.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "STATION PLANNER GUIDE";
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = TEXT_NORMAL;
            titleText.characterSpacing = 2f;
            titleText.alignment = TextAlignmentOptions.Center;
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -12);
            titleRect.sizeDelta = new Vector2(0, 24);
            
            // Content scroll view with help text
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(dialog.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(360, 240);
            contentRect.anchoredPosition = new Vector2(0, 8);
            
            // Background for content area
            var contentBg = contentGO.AddComponent<Image>();
            var contentBgSprite = LoadSlicedSprite("button-bg.png", 7);
            if (contentBgSprite != null)
            {
                contentBg.sprite = contentBgSprite;
                contentBg.type = Image.Type.Sliced;
            }
            contentBg.color = new Color(0f, 0f, 0f, 0.75f);
            
            // Scroll view
            var scrollRect = contentGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;
            
            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(contentGO.transform, false);
            var vpRect = viewportGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(8, 8);
            vpRect.offsetMax = new Vector2(-8, -8);
            viewportGO.AddComponent<RectMask2D>();
            scrollRect.viewport = vpRect;
            
            // Text content
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(viewportGO.transform, false);
            var textComp = textGO.AddComponent<TextMeshProUGUI>();
            textComp.text = "QUICK START\n\n" +
                           "Click 'New' to create a note\n" +
                           "Click 'Folder' to organize\n" +
                           "Use toolbar to format text\n\n" +
                           "FORMATTING\n\n" +
                           "H1, H2, H3 - Headings\n" +
                           "• - Bullet point\n" +
                           "/ - Strikethrough (grey)\n" +
                           "¶ - Normal paragraph\n\n" +
                           "SHORTCUTS\n\n" +
                           "F2 - Toggle window\n" +
                           "Escape - Close\n" +
                           "|| - Pause/Resume game\n\n" +
                           "FEATURES\n\n" +
                           "Auto-saves as you type\n" +
                           "Drag notes between folders\n" +
                           "Move notes up/down\n\n" +
                           "FOLDER PERSISTENCE\n\n" +
                           "Click G button on folder to\n" +
                           "toggle folder persistence.\n" +
                           "Blue G = Global (across saves)\n" +
                           "Empty = Per-save folder";
            textComp.fontSize = 12;
            textComp.color = TEXT_NORMAL;
            textComp.alignment = TextAlignmentOptions.TopLeft;
            textComp.wordWrappingRatios = 0.8f;
            
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 1);
            textRect.anchoredPosition = new Vector2(0, 0);
            textRect.sizeDelta = new Vector2(0, 800);  // Large height for content sizing
            
            // Layout element to size the content
            var le = textGO.AddComponent<LayoutElement>();
            le.minHeight = 100;
            
            scrollRect.content = textRect;
            
            // Outline on content
            var outlineGO = new GameObject("Outline");
            outlineGO.transform.SetParent(contentGO.transform, false);
            var outlineRect = outlineGO.AddComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;
            var outlineImg = outlineGO.AddComponent<Image>();
            var outlineSprite = LoadSlicedSprite("slot-outline.png", 8);
            if (outlineSprite != null)
            {
                outlineImg.sprite = outlineSprite;
                outlineImg.type = Image.Type.Sliced;
                outlineImg.fillCenter = false;
            }
            outlineImg.color = new Color(0f, 0f, 0f, 0.5f);
            outlineImg.raycastTarget = false;
            
            // Close button at bottom
            var closeBtn = CreateToolbarBtn(dialog.transform, "Close", 70);
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.5f, 0);
            closeBtnRect.anchorMax = new Vector2(0.5f, 0);
            closeBtnRect.pivot = new Vector2(0.5f, 0);
            closeBtnRect.anchoredPosition = new Vector2(0, 10);
            closeBtn.onClick.AddListener(() => Destroy(dialog));
        }
        
        #endregion
    }
    
    #region Helper Components
    
    /// <summary>
    /// Window drag handler
    /// </summary>
    public class WindowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform WindowRect;
        private Vector2 _offset;
        
        public void OnBeginDrag(PointerEventData e)
        {
            if (WindowRect == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                WindowRect.parent as RectTransform, e.position, e.pressEventCamera, out _offset);
            _offset = WindowRect.anchoredPosition - _offset;
        }
        
        public void OnDrag(PointerEventData e)
        {
            if (WindowRect == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                WindowRect.parent as RectTransform, e.position, e.pressEventCamera, out Vector2 pos))
            {
                WindowRect.anchoredPosition = pos + _offset;
            }
        }
    }
    
    /// <summary>
    /// Window resize handler
    /// </summary>
    public class WindowResizeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public RectTransform WindowRect;
        public Vector2 MinSize = new Vector2(400, 300);
        
        private Vector2 _startSize;
        private Vector2 _startMouse;
        
        public void OnBeginDrag(PointerEventData e)
        {
            if (WindowRect == null) return;
            _startSize = WindowRect.sizeDelta;
            _startMouse = e.position;
        }
        
        public void OnDrag(PointerEventData e)
        {
            if (WindowRect == null) return;
            Vector2 delta = e.position - _startMouse;
            Vector2 newSize = new Vector2(
                Mathf.Max(MinSize.x, _startSize.x + delta.x),
                Mathf.Max(MinSize.y, _startSize.y - delta.y)
            );
            WindowRect.sizeDelta = newSize;
        }
    }
    
    /// <summary>
    /// Click handler to focus input field (like IC10's EditorLineOfCode)
    /// IC10 uses: OnPointerDown sets CurrentLine, which calls just ActivateInputField()
    /// </summary>
    public class EditorClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public TMP_InputField InputField;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // IC10 sets CurrentLine on click, which triggers ActivateInputField
            ActivateField();
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // IC10 sets CurrentLine on pointer down
            ActivateField();
        }
        
        private void ActivateField()
        {
            if (InputField == null) return;
            
            // IC10 pattern: Just call ActivateInputField() - NO Select() call!
            // The IC10's EditorLineOfCode.CurrentLine setter does:
            //   _currentLine.InputField.ActivateInputField();
            // That's IT - no Select(), no EventSystem manipulation
            InputField.ActivateInputField();
            
            Debug.Log($"[Station Planner] ActivateInputField called - isFocused: {InputField.isFocused}");
        }
    }
    
    /// <summary>
    /// Debug component to monitor TMP_InputField caret state
    /// </summary>
    public class InputFieldDebugger : MonoBehaviour
    {
        public TMP_InputField InputField;
        private float _logTimer = 0f;
        private bool _lastFocusState = false;
        
        void Update()
        {
            if (InputField == null) return;
            
            // Log when focus changes
            if (InputField.isFocused != _lastFocusState)
            {
                _lastFocusState = InputField.isFocused;
                LogCaretState($"Focus changed to {_lastFocusState}");
            }
            
            // Periodic logging when focused (every 2 seconds)
            if (InputField.isFocused)
            {
                _logTimer += Time.deltaTime;
                if (_logTimer >= 2f)
                {
                    _logTimer = 0f;
                    LogCaretState("Periodic check");
                }
            }
        }
        
        private void LogCaretState(string trigger)
        {
            if (InputField == null) return;
            
            var textArea = InputField.textViewport;
            var textComp = InputField.textComponent;
            
            Debug.Log($"[Station Planner DEBUG] {trigger}");
            Debug.Log($"  isFocused: {InputField.isFocused}");
            Debug.Log($"  interactable: {InputField.interactable}");
            Debug.Log($"  caretPosition: {InputField.caretPosition}");
            Debug.Log($"  caretColor: {InputField.caretColor}");
            Debug.Log($"  caretWidth: {InputField.caretWidth}");
            Debug.Log($"  customCaretColor: {InputField.customCaretColor}");
            Debug.Log($"  selectionColor: {InputField.selectionColor}");
            Debug.Log($"  textViewport: {(textArea != null ? textArea.name : "NULL")}");
            Debug.Log($"  textComponent: {(textComp != null ? textComp.name : "NULL")}");
            
            // Check for Caret child object in textViewport
            if (textArea != null)
            {
                Debug.Log($"  textViewport children: {textArea.childCount}");
                for (int i = 0; i < textArea.childCount; i++)
                {
                    var child = textArea.GetChild(i);
                    var img = child.GetComponent<UnityEngine.UI.Image>();
                    var canvasRenderer = child.GetComponent<CanvasRenderer>();
                    Debug.Log($"    [{i}] {child.name} - Image: {img != null}, CanvasRenderer: {canvasRenderer != null}, Active: {child.gameObject.activeSelf}");
                    if (img != null)
                    {
                        Debug.Log($"         Image color: {img.color}, enabled: {img.enabled}, raycastTarget: {img.raycastTarget}");
                    }
                }
            }
            
            // Check parent hierarchy for any disabled components
            var current = InputField.transform;
            while (current != null)
            {
                var canvas = current.GetComponent<Canvas>();
                var canvasGroup = current.GetComponent<CanvasGroup>();
                if (canvas != null)
                    Debug.Log($"  Found Canvas on {current.name}: enabled={canvas.enabled}");
                if (canvasGroup != null)
                    Debug.Log($"  Found CanvasGroup on {current.name}: alpha={canvasGroup.alpha}, interactable={canvasGroup.interactable}");
                current = current.parent;
            }
        }
    }
    
    /// <summary>
    /// Simple tooltip trigger - works even when game is paused
    /// </summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string TooltipText;
        private static GameObject _tooltip;
        private static TextMeshProUGUI _tooltipLabel;
        private static TooltipTrigger _activeTooltip;
        
        public void OnPointerEnter(PointerEventData e)
        {
            if (string.IsNullOrEmpty(TooltipText)) return;
            
            _activeTooltip = this;
            
            if (_tooltip == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null) return;
                
                _tooltip = new GameObject("Tooltip");
                _tooltip.transform.SetParent(canvas.transform, false);
                
                var rect = _tooltip.AddComponent<RectTransform>();
                rect.pivot = new Vector2(0, 1);
                
                var bg = _tooltip.AddComponent<Image>();
                bg.color = new Color(0.1f, 0.14f, 0.18f, 0.95f);
                
                var outline = _tooltip.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.478f, 0.094f, 0.8f);
                outline.effectDistance = new Vector2(1, 1);
                
                var csf = _tooltip.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                var hlg = _tooltip.AddComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(6, 6, 3, 3);
                
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(_tooltip.transform, false);
                _tooltipLabel = textGO.AddComponent<TextMeshProUGUI>();
                _tooltipLabel.fontSize = 11;
                _tooltipLabel.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            }
            
            _tooltipLabel.text = TooltipText;
            _tooltip.SetActive(true);
            UpdateTooltipPosition();
        }
        
        void Update()
        {
            // Keep tooltip following the mouse (works with unscaled time, even when paused)
            if (_activeTooltip == this && _tooltip != null && _tooltip.activeSelf)
            {
                UpdateTooltipPosition();
            }
        }
        
        private void UpdateTooltipPosition()
        {
            if (_tooltip == null) return;
            var rect = _tooltip.GetComponent<RectTransform>();
            rect.position = Input.mousePosition + new Vector3(12, -18, 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
        
        public void OnPointerExit(PointerEventData e)
        {
            if (_activeTooltip == this)
                _activeTooltip = null;
            if (_tooltip != null)
                _tooltip.SetActive(false);
        }
    }
    
    /// <summary>
    /// Hover handler to highlight editor area on mouse over
    /// </summary>
    public class EditorHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Background;
        public Color NormalColor;
        public Color HoverColor;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Background != null)
                Background.color = HoverColor;
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (Background != null)
                Background.color = NormalColor;
        }
    }
    
    /// <summary>
    /// Handles dragging files to move them between folders
    /// </summary>
    public class FileDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PlannerFile File;
        public StationPlannerWindow Window;
        
        private GameObject _dragGhost;
        private Canvas _canvas;
        private static FileDragHandler _currentDrag;
        
        public static bool IsDragging => _currentDrag != null;
        public static PlannerFile DraggedFile => _currentDrag?.File;
        
        void OnDisable()
        {
            // Ensure cleanup if object is disabled during drag
            CleanupDrag();
        }
        
        void OnDestroy()
        {
            // Ensure cleanup if object is destroyed during drag
            CleanupDrag();
        }
        
        private void CleanupDrag()
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
            }
            if (_currentDrag == this)
                _currentDrag = null;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (File == null || Window == null) return;
            
            _currentDrag = this;
            _canvas = GetComponentInParent<Canvas>();
            
            // Create a ghost label that follows the cursor
            _dragGhost = new GameObject("DragGhost");
            _dragGhost.transform.SetParent(_canvas.transform, false);
            
            var rect = _dragGhost.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(120, 20);
            
            var bg = _dragGhost.AddComponent<Image>();
            bg.color = new Color(0.984f, 0.69f, 0.231f, 0.3f); // Orange tint
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(_dragGhost.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 0);
            textRect.offsetMax = new Vector2(-4, 0);
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = File.Name;
            text.fontSize = 11;
            text.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            
            UpdateGhostPosition(eventData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            UpdateGhostPosition(eventData);
        }
        
        private void UpdateGhostPosition(PointerEventData eventData)
        {
            if (_dragGhost == null || _canvas == null) return;
            
            var rect = _dragGhost.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(), 
                eventData.position, 
                _canvas.worldCamera, 
                out Vector2 localPoint);
            rect.anchoredPosition = localPoint + new Vector2(10, 0);
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            CleanupDrag();
        }
    }
    
    /// <summary>
    /// Handles receiving dropped files on folders and reordering folders
    /// </summary>
    public class FolderDropHandler : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public PlannerFolder Folder;
        public StationPlannerWindow Window;
        public Image Background;
        
        private Color _normalColor;
        private static readonly Color DROP_HIGHLIGHT = new Color(0.984f, 0.69f, 0.231f, 0.5f);
        private static readonly Color INSERT_HIGHLIGHT = new Color(0.984f, 0.69f, 0.231f, 1f);
        
        // Insert indicator line
        private GameObject _insertIndicator;
        private RectTransform _myRect;
        
        void Start()
        {
            if (Background != null)
                _normalColor = Background.color;
            _myRect = GetComponent<RectTransform>();
        }
        
        void OnDestroy()
        {
            DestroyInsertIndicator();
        }
        
        private void DestroyInsertIndicator()
        {
            if (_insertIndicator != null)
            {
                Destroy(_insertIndicator);
                _insertIndicator = null;
            }
        }
        
        private void CreateInsertIndicator(bool above)
        {
            DestroyInsertIndicator();
            
            _insertIndicator = new GameObject("InsertIndicator");
            _insertIndicator.transform.SetParent(transform, false);
            
            var rect = _insertIndicator.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, above ? 1 : 0);
            rect.anchorMax = new Vector2(1, above ? 1 : 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0, 3);
            rect.anchoredPosition = Vector2.zero;
            
            var img = _insertIndicator.AddComponent<Image>();
            img.color = INSERT_HIGHLIGHT;
            img.raycastTarget = false;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Show drop highlight for files
            if (FileDragHandler.IsDragging && Background != null)
            {
                Background.color = DROP_HIGHLIGHT;
            }
            // For folder dragging, we'll update the indicator in Update
        }
        
        void Update()
        {
            // Update insert indicator position during folder drag
            if (FolderDragHandler.IsDragging && FolderDragHandler.DraggedFolder != Folder && _myRect != null)
            {
                // Check if pointer is over this element
                if (RectTransformUtility.RectangleContainsScreenPoint(_myRect, Input.mousePosition, null))
                {
                    // Determine if we're in top half or bottom half
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _myRect, Input.mousePosition, null, out Vector2 localPoint);
                    
                    bool isTopHalf = localPoint.y > 0;
                    CreateInsertIndicator(isTopHalf);
                    Background.color = _normalColor; // Don't show background highlight for reorder
                }
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (Background != null)
            {
                Background.color = _normalColor;
            }
            DestroyInsertIndicator();
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            DestroyInsertIndicator();
            
            if (Background != null)
                Background.color = _normalColor;
                
            var draggedFile = FileDragHandler.DraggedFile;
            var draggedFolder = FolderDragHandler.DraggedFolder;
            
            if (draggedFile != null && Folder != null && Window != null)
            {
                // Move file to this folder
                Window.MoveFileToFolder(draggedFile, Folder);
            }
            else if (draggedFolder != null && Folder != null && Window != null && draggedFolder != Folder)
            {
                // Determine if we should insert before/after or move into
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _myRect, eventData.position, null, out Vector2 localPoint);
                
                bool isTopHalf = localPoint.y > 0;
                
                // Reorder: move folder to same parent as this folder, before or after
                Window.ReorderFolder(draggedFolder, Folder, isTopHalf);
            }
        }
    }
    
    /// <summary>
    /// Handles shift-click for multi-select on folders
    /// </summary>
    public class FolderShiftClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public PlannerFolder Folder;
        public StationPlannerWindow Window;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Folder == null || Window == null) return;
            
            // Check if shift is held
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            
            // Call the window's select method with shift state
            // Use reflection to call the private method or make it internal
            var method = typeof(StationPlannerWindow).GetMethod("SelectFolder", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new System.Type[] { typeof(PlannerFolder), typeof(bool) }, null);
            method?.Invoke(Window, new object[] { Folder, shiftHeld });
        }
    }
    
    /// <summary>
    /// Handles dragging folders to move them between parent folders
    /// </summary>
    public class FolderDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PlannerFolder Folder;
        public StationPlannerWindow Window;
        
        private GameObject _dragGhost;
        private Canvas _canvas;
        private static FolderDragHandler _currentDrag;
        
        public static bool IsDragging => _currentDrag != null;
        public static PlannerFolder DraggedFolder => _currentDrag?.Folder;
        
        void OnDisable()
        {
            CleanupDrag();
        }
        
        void OnDestroy()
        {
            CleanupDrag();
        }
        
        private void CleanupDrag()
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
            }
            if (_currentDrag == this)
                _currentDrag = null;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Folder == null || Window == null) return;
            
            _currentDrag = this;
            _canvas = GetComponentInParent<Canvas>();
            
            // Create a ghost label that follows the cursor
            _dragGhost = new GameObject("FolderDragGhost");
            _dragGhost.transform.SetParent(_canvas.transform, false);
            
            var rect = _dragGhost.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = new Vector2(120, 20);
            
            var bg = _dragGhost.AddComponent<Image>();
            bg.color = new Color(0.4f, 0.7f, 1f, 0.3f); // Blue tint for folders
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(_dragGhost.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 0);
            textRect.offsetMax = new Vector2(-4, 0);
            
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "[" + Folder.Name + "]";
            text.fontSize = 11;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            
            UpdateGhostPosition(eventData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            UpdateGhostPosition(eventData);
        }
        
        private void UpdateGhostPosition(PointerEventData eventData)
        {
            if (_dragGhost == null || _canvas == null) return;
            
            var rect = _dragGhost.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(), 
                eventData.position, 
                _canvas.worldCamera, 
                out Vector2 localPoint);
            rect.anchoredPosition = localPoint + new Vector2(10, 0);
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            CleanupDrag();
        }
    }
    
    #endregion
}
