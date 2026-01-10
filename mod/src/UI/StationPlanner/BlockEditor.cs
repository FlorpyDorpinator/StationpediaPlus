using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace StationpediaAscended.UI.StationPlanner
{
    /// <summary>
    /// Line formatting style for the block-based editor.
    /// Each line has exactly one style - no inline formatting.
    /// </summary>
    public enum LineStyle
    {
        Normal,
        H1,
        H2,
        H3,
        Bullet,
        Checkbox,        // Unchecked checkbox: ☐ text
        CheckboxChecked, // Checked checkbox: ☑ text (with strikethrough)
        Strikethrough    // Struck-through grey text
    }
    
    /// <summary>
    /// Represents a single line with its text and formatting style.
    /// </summary>
    public class EditorLine
    {
        public string Text { get; set; } = "";
        public LineStyle Style { get; set; } = LineStyle.Normal;
    }
    
    /// <summary>
    /// Serializable format for saving line data.
    /// </summary>
    [Serializable]
    public class EditorLineData
    {
        public string text;
        public int style; // Stored as int for JSON compatibility
    }
    
    /// <summary>
    /// Serializable document format.
    /// </summary>
    [Serializable]
    public class BlockEditorDocument
    {
        public List<EditorLineData> lines = new List<EditorLineData>();
    }
    
    /// <summary>
    /// Block-based editor that operates on an instantiated TMP_InputField prefab.
    /// 
    /// Key features:
    /// - Loads TMP_InputField from AssetBundle prefab (working caret!)
    /// - Line-level formatting only (Normal, H1, H2, H3, Bullet)
    /// - Any manual text edit on a formatted line downgrades it to Normal
    /// - Stores plain text + line metadata, renders to TMP rich text for display
    /// </summary>
    public class BlockEditor : MonoBehaviour
    {
        #region Constants
        
        private const string BUNDLE_NAME = "stationpediaascended_ui";
        private const string PREFAB_NAME = "SA_TMPInputField_Base";
        
        // Rich text formatting for each style
        private const string H1_OPEN = "<size=150%><b>";
        private const string H1_CLOSE = "</b></size>";
        private const string H2_OPEN = "<size=130%><b>";
        private const string H2_CLOSE = "</b></size>";
        private const string H3_OPEN = "<size=115%><b>";
        private const string H3_CLOSE = "</b></size>";
        private const string BULLET_PREFIX = "  <color=#FBB03B>•</color> "; // Indented orange bullet
        private const string CHECKBOX_UNCHECKED = "  <color=#FBB03B>☐</color> "; // Unchecked checkbox
        private const string CHECKBOX_CHECKED_OPEN = "  <color=#888888>☑</color> <s><color=#888888>"; // Checked with strikethrough
        private const string CHECKBOX_CHECKED_CLOSE = "</color></s>";
        private const string STRIKETHROUGH_OPEN = "<s><color=#888888>"; // Grey strikethrough
        private const string STRIKETHROUGH_CLOSE = "</color></s>";
        
        #endregion
        
        #region Fields
        
        // Event fired when content changes (for auto-save)
        public event Action OnContentChanged;
        
        private TMP_InputField _inputField;
        private RectTransform _inputFieldRect;
        private GameObject _prefabInstance;
        
        // Line metadata - parallel to the lines in the input field
        private List<LineStyle> _lineStyles = new List<LineStyle>();
        
        // Track previous text for change detection
        private string _previousPlainText = "";
        private int _previousLineCount = 0;
        
        // Flag to prevent recursive updates
        private bool _isUpdatingDisplay = false;
        
        // AssetBundle reference (keep loaded to avoid unloading prefab)
        private static AssetBundle _loadedBundle;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// The TMP_InputField from the prefab instance.
        /// </summary>
        public TMP_InputField InputField => _inputField;
        
        /// <summary>
        /// Whether the editor has been successfully initialized.
        /// </summary>
        public bool IsInitialized => _inputField != null;
        
        /// <summary>
        /// Gets the current line index where the caret is positioned.
        /// </summary>
        public int CurrentLineIndex
        {
            get
            {
                if (_inputField == null || _inputField.textComponent == null)
                    return 0;
                    
                string text = GetPlainText();
                int caretPos = Mathf.Min(_inputField.caretPosition, text.Length);
                
                // Count newlines before caret position
                int lineIndex = 0;
                for (int i = 0; i < caretPos; i++)
                {
                    if (text[i] == '\n')
                        lineIndex++;
                }
                
                return lineIndex;
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the block editor by loading the prefab from AssetBundle.
        /// </summary>
        /// <param name="parent">Parent transform to attach the input field to.</param>
        /// <returns>True if initialization succeeded.</returns>
        public bool Initialize(Transform parent)
        {
            try
            {
                // Try to load prefab from AssetBundle
                _prefabInstance = LoadPrefabFromBundle(parent);
                
                if (_prefabInstance == null)
                {
                    StationpediaAscendedMod.Log?.LogError("[BlockEditor] Failed to load prefab from AssetBundle");
                    return false;
                }
                
                // Get the TMP_InputField component
                _inputField = _prefabInstance.GetComponent<TMP_InputField>();
                if (_inputField == null)
                {
                    StationpediaAscendedMod.Log?.LogError("[BlockEditor] Prefab does not have TMP_InputField component");
                    Destroy(_prefabInstance);
                    return false;
                }
                
                _inputFieldRect = _prefabInstance.GetComponent<RectTransform>();
                
                // Configure for our use case
                ConfigureInputField();
                
                // Subscribe to text changes
                _inputField.onValueChanged.AddListener(OnTextChanged);
                
                // Add checkbox click handler
                var clickHandler = _prefabInstance.AddComponent<CheckboxClickHandler>();
                clickHandler.BlockEditor = this;
                
                // Initialize with empty document
                _lineStyles.Add(LineStyle.Normal);
                _previousPlainText = "";
                _previousLineCount = 1;
                
                StationpediaAscendedMod.Log?.LogInfo("[BlockEditor] Successfully initialized from prefab");
                return true;
            }
            catch (Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"[BlockEditor] Initialization failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Load the prefab from the AssetBundle.
        /// </summary>
        private GameObject LoadPrefabFromBundle(Transform parent)
        {
            // Try to find the bundle file
            string bundlePath = GetBundlePath();
            
            if (string.IsNullOrEmpty(bundlePath))
            {
                StationpediaAscendedMod.Log?.LogError("[BlockEditor] Could not find AssetBundle file");
                return null;
            }
            
            StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Loading AssetBundle from: {bundlePath}");
            
            // Load the bundle if not already loaded
            if (_loadedBundle == null)
            {
                // Check if Unity already has this bundle loaded (can happen after hot reload)
                // Try to get all loaded bundles and find ours
                var allBundles = AssetBundle.GetAllLoadedAssetBundles();
                foreach (var bundle in allBundles)
                {
                    if (bundle.name == BUNDLE_NAME || bundle.name.Contains("stationpediaascended"))
                    {
                        StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Found already-loaded bundle: {bundle.name}");
                        _loadedBundle = bundle;
                        break;
                    }
                }
                
                // If still not found, load it fresh
                if (_loadedBundle == null)
                {
                    _loadedBundle = AssetBundle.LoadFromFile(bundlePath);
                    if (_loadedBundle == null)
                    {
                        StationpediaAscendedMod.Log?.LogError("[BlockEditor] Failed to load AssetBundle from file");
                        return null;
                    }
                }
                
                // Log what's in the bundle
                StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Bundle loaded. Contents:");
                foreach (var assetName in _loadedBundle.GetAllAssetNames())
                {
                    StationpediaAscendedMod.Log?.LogInfo($"  - {assetName}");
                }
            }
            
            // Load the prefab from the bundle
            // Try with full path first, then just the name
            GameObject prefab = _loadedBundle.LoadAsset<GameObject>(PREFAB_NAME);
            if (prefab == null)
            {
                // Try loading with .prefab extension
                prefab = _loadedBundle.LoadAsset<GameObject>(PREFAB_NAME + ".prefab");
            }
            if (prefab == null)
            {
                // Try loading with full asset path
                prefab = _loadedBundle.LoadAsset<GameObject>("assets/prefabs/ui/" + PREFAB_NAME.ToLower() + ".prefab");
            }
            if (prefab == null)
            {
                StationpediaAscendedMod.Log?.LogError($"[BlockEditor] Prefab '{PREFAB_NAME}' not found in bundle. Available assets logged above.");
                return null;
            }
            
            StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Successfully loaded prefab: {prefab.name}");
            
            // Instantiate the prefab
            GameObject instance = Instantiate(prefab, parent);
            instance.name = "BlockEditor_InputField";
            
            return instance;
        }
        
        /// <summary>
        /// Find the AssetBundle file path.
        /// Searches in multiple locations.
        /// </summary>
        private string GetBundlePath()
        {
            // Possible locations for the bundle
            string[] searchPaths = new string[]
            {
                // Next to the mod DLL
                Path.Combine(Path.GetDirectoryName(typeof(BlockEditor).Assembly.Location), BUNDLE_NAME),
                // In BepInEx scripts folder
                Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", BUNDLE_NAME),
                // In plugins folder
                Path.Combine(BepInEx.Paths.PluginPath, "StationpediaAscended", BUNDLE_NAME),
                // In a dedicated assets folder
                Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "assets", BUNDLE_NAME),
            };
            
            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Found bundle at: {path}");
                    return path;
                }
            }
            
            // Log all paths we searched
            StationpediaAscendedMod.Log?.LogWarning("[BlockEditor] Bundle not found in any of these locations:");
            foreach (string path in searchPaths)
            {
                StationpediaAscendedMod.Log?.LogWarning($"  - {path}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Configure the input field for our block editor use case.
        /// </summary>
        private void ConfigureInputField()
        {
            // Multi-line mode
            _inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            
            // Enable rich text rendering (we generate the tags)
            _inputField.richText = true;
            
            // But don't allow users to edit rich text directly
            // (they edit plain text, we convert to rich text for display)
            // Note: TMP_InputField doesn't have a direct way to do this,
            // so we handle it in OnTextChanged by stripping tags
            
            // Standard text settings
            _inputField.characterLimit = 0; // Unlimited
            _inputField.interactable = true;
            _inputField.readOnly = false;
        }
        
        #endregion
        
        #region Text Change Handling
        
        /// <summary>
        /// Called when the input field text changes.
        /// Handles the downgrade-on-edit logic.
        /// </summary>
        private void OnTextChanged(string newText)
        {
            if (_isUpdatingDisplay) return;
            
            try
            {
                // Strip any rich text tags the user might have typed/pasted
                string plainText = StripRichTextTags(newText);
                
                // Split into lines
                string[] newLines = plainText.Split('\n');
                string[] oldLines = _previousPlainText.Split('\n');
                
                // Ensure we have enough line styles
                while (_lineStyles.Count < newLines.Length)
                {
                    _lineStyles.Add(LineStyle.Normal);
                }
                
                // Trim excess line styles
                while (_lineStyles.Count > newLines.Length)
                {
                    _lineStyles.RemoveAt(_lineStyles.Count - 1);
                }
                
                // Detect which lines were edited and downgrade them
                for (int i = 0; i < newLines.Length; i++)
                {
                    // Check if this line existed before and what it contained
                    bool wasEmpty = (i >= oldLines.Length || string.IsNullOrEmpty(oldLines[i]));
                    bool isNowEmpty = string.IsNullOrEmpty(newLines[i]);
                    bool contentChanged = (i >= oldLines.Length || newLines[i] != oldLines[i]);
                    
                    // Only downgrade if:
                    // 1. Content actually changed AND
                    // 2. The line wasn't empty before (preserve formatting on empty->text transitions)
                    if (contentChanged && !wasEmpty && !isNowEmpty)
                    {
                        // Only downgrade if the line has a non-normal style
                        if (_lineStyles[i] != LineStyle.Normal)
                        {
                            StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Line {i} edited, downgrading from {_lineStyles[i]} to Normal");
                            _lineStyles[i] = LineStyle.Normal;
                        }
                    }
                }
                
                // Update tracking
                _previousPlainText = plainText;
                _previousLineCount = newLines.Length;
                
                // Refresh display with proper formatting
                RefreshDisplay();
                
                // Trigger content changed event for auto-save
                OnContentChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"[BlockEditor] OnTextChanged error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Strip all TMP rich text tags from a string.
        /// </summary>
        private string StripRichTextTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Simple regex-free approach: remove anything between < and >
            var result = new StringBuilder();
            bool inTag = false;
            
            foreach (char c in text)
            {
                if (c == '<')
                {
                    inTag = true;
                }
                else if (c == '>')
                {
                    inTag = false;
                }
                else if (!inTag)
                {
                    result.Append(c);
                }
            }
            
            return result.ToString();
        }
        
        #endregion
        
        #region Display Rendering
        
        /// <summary>
        /// Refresh the display by converting plain text + styles to rich text.
        /// </summary>
        public void RefreshDisplay()
        {
            if (_inputField == null) return;
            
            _isUpdatingDisplay = true;
            
            try
            {
                // Save caret position
                int caretPos = _inputField.caretPosition;
                
                // Generate rich text from plain text + styles
                string richText = GenerateRichText();
                
                // Update the input field
                _inputField.text = richText;
                
                // Restore caret position (may need adjustment for added tags)
                _inputField.caretPosition = Mathf.Min(caretPos, richText.Length);
            }
            finally
            {
                _isUpdatingDisplay = false;
            }
        }
        
        /// <summary>
        /// Generate TMP rich text from plain text and line styles.
        /// </summary>
        private string GenerateRichText()
        {
            string[] lines = _previousPlainText.Split('\n');
            var result = new StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) result.Append('\n');
                
                // Ensure we have a style for this line
                LineStyle style = i < _lineStyles.Count ? _lineStyles[i] : LineStyle.Normal;
                
                // Apply formatting based on style
                string line = lines[i];
                
                switch (style)
                {
                    case LineStyle.H1:
                        result.Append(H1_OPEN);
                        result.Append(line);
                        result.Append(H1_CLOSE);
                        break;
                        
                    case LineStyle.H2:
                        result.Append(H2_OPEN);
                        result.Append(line);
                        result.Append(H2_CLOSE);
                        break;
                        
                    case LineStyle.H3:
                        result.Append(H3_OPEN);
                        result.Append(line);
                        result.Append(H3_CLOSE);
                        break;
                        
                    case LineStyle.Bullet:
                        result.Append(BULLET_PREFIX);
                        result.Append(line);
                        break;
                        
                    case LineStyle.Checkbox:
                        result.Append(CHECKBOX_UNCHECKED);
                        result.Append(line);
                        break;
                        
                    case LineStyle.CheckboxChecked:
                        result.Append(CHECKBOX_CHECKED_OPEN);
                        result.Append(line);
                        result.Append(CHECKBOX_CHECKED_CLOSE);
                        break;
                        
                    case LineStyle.Strikethrough:
                        result.Append(STRIKETHROUGH_OPEN);
                        result.Append(line);
                        result.Append(STRIKETHROUGH_CLOSE);
                        break;
                        
                    case LineStyle.Normal:
                    default:
                        result.Append(line);
                        break;
                }
            }
            
            return result.ToString();
        }
        
        #endregion
        
        #region Style Application
        
        /// <summary>
        /// Apply a style to the current line (where caret is).
        /// </summary>
        public void ApplyStyleToCurrentLine(LineStyle style)
        {
            int lineIndex = CurrentLineIndex;
            ApplyStyleToLine(lineIndex, style);
        }
        
        /// <summary>
        /// Insert text at the current caret position.
        /// </summary>
        public void InsertText(string text)
        {
            if (_inputField == null || string.IsNullOrEmpty(text)) return;
            
            int caretPos = _inputField.caretPosition;
            string currentText = _inputField.text;
            
            // Insert text at caret position
            string newText = currentText.Insert(caretPos, text);
            _inputField.text = newText;
            
            // Move caret after inserted text
            _inputField.caretPosition = caretPos + text.Length;
            
            // Focus the input field
            _inputField.ActivateInputField();
        }
        
        /// <summary>
        /// Apply a style to a specific line.
        /// </summary>
        public void ApplyStyleToLine(int lineIndex, LineStyle style)
        {
            // Ensure we have enough styles
            while (_lineStyles.Count <= lineIndex)
            {
                _lineStyles.Add(LineStyle.Normal);
            }
            
            _lineStyles[lineIndex] = style;
            StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Applied {style} to line {lineIndex}");
            
            RefreshDisplay();
            
            // Don't reposition cursor - let it stay where the user has it
            // The prefix will appear before the text but cursor remains in place
        }
        
        /// <summary>
        /// Position the caret after the line's prefix (bullet/checkbox).
        /// </summary>
        private void PositionCaretAfterPrefix(int lineIndex)
        {
            if (_inputField == null) return;
            
            // Calculate the character position at the start of this line's content
            string[] lines = _previousPlainText.Split('\n');
            int charPos = 0;
            
            // Sum up lengths of all previous lines plus newlines
            for (int i = 0; i < lineIndex && i < lines.Length; i++)
            {
                charPos += lines[i].Length + 1; // +1 for newline
            }
            
            // Position at start of line content (after any visual prefix)
            _inputField.caretPosition = charPos;
            _inputField.selectionAnchorPosition = charPos;
            _inputField.selectionFocusPosition = charPos;
        }
        
        /// <summary>
        /// Toggle a style on the current line.
        /// If already that style, revert to Normal. Otherwise, apply the style.
        /// </summary>
        public void ToggleStyleOnCurrentLine(LineStyle style)
        {
            int lineIndex = CurrentLineIndex;
            
            if (lineIndex < _lineStyles.Count && _lineStyles[lineIndex] == style)
            {
                ApplyStyleToLine(lineIndex, LineStyle.Normal);
            }
            else
            {
                ApplyStyleToLine(lineIndex, style);
            }
        }
        
        /// <summary>
        /// Toggle checkbox state on a specific line.
        /// If unchecked -> checked, if checked -> unchecked.
        /// </summary>
        public void ToggleCheckboxOnLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _lineStyles.Count) return;
            
            var currentStyle = _lineStyles[lineIndex];
            
            if (currentStyle == LineStyle.Checkbox)
            {
                _lineStyles[lineIndex] = LineStyle.CheckboxChecked;
                StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Checked checkbox on line {lineIndex}");
            }
            else if (currentStyle == LineStyle.CheckboxChecked)
            {
                _lineStyles[lineIndex] = LineStyle.Checkbox;
                StationpediaAscendedMod.Log?.LogInfo($"[BlockEditor] Unchecked checkbox on line {lineIndex}");
            }
            
            RefreshDisplay();
        }
        
        /// <summary>
        /// Check if a click position is on a checkbox character.
        /// Returns the line index if clicked on checkbox, -1 otherwise.
        /// </summary>
        public int GetCheckboxLineAtPosition(Vector2 localPoint)
        {
            if (_inputField == null || _inputField.textComponent == null) return -1;
            
            var textComp = _inputField.textComponent;
            var textInfo = textComp.textInfo;
            
            if (textInfo == null || textInfo.characterCount == 0) return -1;
            
            // Find which character was clicked
            int charIndex = TMP_TextUtilities.FindIntersectingCharacter(textComp, localPoint, null, true);
            if (charIndex < 0) return -1;
            
            // Convert character index to line index in our plain text
            // The rich text has extra characters for tags, so we need to map back
            int lineIndex = GetLineIndexFromDisplayCharIndex(charIndex);
            
            if (lineIndex < 0 || lineIndex >= _lineStyles.Count) return -1;
            
            var style = _lineStyles[lineIndex];
            if (style != LineStyle.Checkbox && style != LineStyle.CheckboxChecked) return -1;
            
            // Check if the click was on the checkbox character (first few chars of line)
            // The checkbox prefix is "  ☐ " or "  ☑ " which is about 4 characters visually
            int lineStartChar = GetDisplayCharIndexForLineStart(lineIndex);
            if (charIndex >= lineStartChar && charIndex < lineStartChar + 6)  // Within checkbox area
            {
                return lineIndex;
            }
            
            return -1;
        }
        
        /// <summary>
        /// Convert a display character index to our plain text line index.
        /// </summary>
        private int GetLineIndexFromDisplayCharIndex(int displayCharIndex)
        {
            if (_inputField == null || _inputField.textComponent == null) return -1;
            
            var textComp = _inputField.textComponent;
            var textInfo = textComp.textInfo;
            
            if (textInfo == null) return -1;
            
            // Find which line this character is on
            for (int i = 0; i < textInfo.lineCount; i++)
            {
                var lineInfo = textInfo.lineInfo[i];
                if (displayCharIndex >= lineInfo.firstCharacterIndex && 
                    displayCharIndex <= lineInfo.lastCharacterIndex)
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// Get the display character index where a line starts.
        /// </summary>
        private int GetDisplayCharIndexForLineStart(int lineIndex)
        {
            if (_inputField == null || _inputField.textComponent == null) return 0;
            
            var textInfo = _inputField.textComponent.textInfo;
            if (textInfo == null || lineIndex >= textInfo.lineCount) return 0;
            
            return textInfo.lineInfo[lineIndex].firstCharacterIndex;
        }
        
        /// <summary>
        /// Get the style of the current line.
        /// </summary>
        public LineStyle GetCurrentLineStyle()
        {
            int lineIndex = CurrentLineIndex;
            if (lineIndex < _lineStyles.Count)
            {
                return _lineStyles[lineIndex];
            }
            return LineStyle.Normal;
        }
        
        #endregion
        
        #region Document I/O
        
        /// <summary>
        /// Get the plain text content (no rich text tags).
        /// </summary>
        public string GetPlainText()
        {
            return _previousPlainText;
        }
        
        /// <summary>
        /// Set the content from plain text and line styles.
        /// </summary>
        public void SetContent(string plainText, List<LineStyle> styles)
        {
            _previousPlainText = plainText ?? "";
            _lineStyles = styles ?? new List<LineStyle>();
            
            // Ensure at least one line style
            if (_lineStyles.Count == 0)
            {
                _lineStyles.Add(LineStyle.Normal);
            }
            
            // Ensure styles match line count
            string[] lines = _previousPlainText.Split('\n');
            while (_lineStyles.Count < lines.Length)
            {
                _lineStyles.Add(LineStyle.Normal);
            }
            while (_lineStyles.Count > lines.Length)
            {
                _lineStyles.RemoveAt(_lineStyles.Count - 1);
            }
            
            _previousLineCount = lines.Length;
            
            RefreshDisplay();
        }
        
        /// <summary>
        /// Get the line styles.
        /// </summary>
        public List<LineStyle> GetLineStyles()
        {
            return new List<LineStyle>(_lineStyles);
        }
        
        /// <summary>
        /// Serialize the document to JSON.
        /// </summary>
        public string SerializeDocument()
        {
            var doc = new BlockEditorDocument();
            string[] lines = _previousPlainText.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                doc.lines.Add(new EditorLineData
                {
                    text = lines[i],
                    style = i < _lineStyles.Count ? (int)_lineStyles[i] : 0
                });
            }
            
            return JsonConvert.SerializeObject(doc, Formatting.Indented);
        }
        
        /// <summary>
        /// Deserialize and load a document from JSON.
        /// </summary>
        public void DeserializeDocument(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                SetContent("", new List<LineStyle>());
                return;
            }
            
            try
            {
                var doc = JsonConvert.DeserializeObject<BlockEditorDocument>(json);
                
                var text = new StringBuilder();
                var styles = new List<LineStyle>();
                
                for (int i = 0; i < doc.lines.Count; i++)
                {
                    if (i > 0) text.Append('\n');
                    text.Append(doc.lines[i].text);
                    styles.Add((LineStyle)doc.lines[i].style);
                }
                
                SetContent(text.ToString(), styles);
            }
            catch (Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"[BlockEditor] Failed to deserialize document: {ex.Message}");
                SetContent("", new List<LineStyle>());
            }
        }
        
        /// <summary>
        /// Load content from legacy plain text format (no styles).
        /// Converts to block editor format with all lines as Normal.
        /// </summary>
        public void LoadFromPlainText(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                SetContent("", new List<LineStyle>());
                return;
            }
            
            string[] lines = plainText.Split('\n');
            var styles = new List<LineStyle>();
            for (int i = 0; i < lines.Length; i++)
            {
                styles.Add(LineStyle.Normal);
            }
            
            SetContent(plainText, styles);
        }
        
        #endregion
        
        #region Cleanup
        
        void OnDestroy()
        {
            if (_inputField != null)
            {
                _inputField.onValueChanged.RemoveListener(OnTextChanged);
            }
            
            if (_prefabInstance != null)
            {
                Destroy(_prefabInstance);
            }
            
            // Note: We don't unload the AssetBundle here because other instances might be using it
        }
        
        /// <summary>
        /// Unload the AssetBundle. Call this when completely done with the mod.
        /// </summary>
        public static void UnloadBundle()
        {
            if (_loadedBundle != null)
            {
                _loadedBundle.Unload(true);
                _loadedBundle = null;
            }
        }
        
        /// <summary>
        /// Create a TMP_InputField for dialogs using the AssetBundle prefab.
        /// This provides consistent cursor/caret support across the mod.
        /// </summary>
        /// <param name="parent">Parent transform to attach the input field to.</param>
        /// <returns>The instantiated TMP_InputField, or null if loading failed.</returns>
        public static TMP_InputField CreateDialogInputField(Transform parent)
        {
            try
            {
                // Find the bundle file
                string bundlePath = GetBundlePathStatic();
                
                if (string.IsNullOrEmpty(bundlePath))
                {
                    StationpediaAscendedMod.Log?.LogWarning("[BlockEditor] Could not find AssetBundle for dialog input field");
                    return null;
                }
                
                // Load the bundle if not already loaded
                if (_loadedBundle == null)
                {
                    var allBundles = AssetBundle.GetAllLoadedAssetBundles();
                    foreach (var bundle in allBundles)
                    {
                        if (bundle.name == BUNDLE_NAME || bundle.name.Contains("stationpediaascended"))
                        {
                            _loadedBundle = bundle;
                            break;
                        }
                    }
                    
                    if (_loadedBundle == null)
                    {
                        _loadedBundle = AssetBundle.LoadFromFile(bundlePath);
                    }
                }
                
                if (_loadedBundle == null)
                {
                    StationpediaAscendedMod.Log?.LogWarning("[BlockEditor] Failed to load AssetBundle for dialog");
                    return null;
                }
                
                // Load the prefab
                GameObject prefab = _loadedBundle.LoadAsset<GameObject>(PREFAB_NAME);
                if (prefab == null)
                    prefab = _loadedBundle.LoadAsset<GameObject>(PREFAB_NAME + ".prefab");
                if (prefab == null)
                    prefab = _loadedBundle.LoadAsset<GameObject>("assets/prefabs/ui/" + PREFAB_NAME.ToLower() + ".prefab");
                
                if (prefab == null)
                {
                    StationpediaAscendedMod.Log?.LogWarning("[BlockEditor] Prefab not found for dialog input field");
                    return null;
                }
                
                // Instantiate
                GameObject instance = Instantiate(prefab, parent);
                instance.name = "Dialog_InputField";
                
                var inputField = instance.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    // Configure for single-line dialog use
                    inputField.lineType = TMP_InputField.LineType.SingleLine;
                    inputField.characterLimit = 0;
                    inputField.interactable = true;
                    
                    StationpediaAscendedMod.Log?.LogInfo("[BlockEditor] Created dialog input field from prefab");
                }
                
                return inputField;
            }
            catch (Exception ex)
            {
                StationpediaAscendedMod.Log?.LogError($"[BlockEditor] Failed to create dialog input field: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Static version of GetBundlePath for use by CreateDialogInputField.
        /// </summary>
        private static string GetBundlePathStatic()
        {
            string[] searchPaths = new string[]
            {
                Path.Combine(Path.GetDirectoryName(typeof(BlockEditor).Assembly.Location), BUNDLE_NAME),
                Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", BUNDLE_NAME),
                Path.Combine(BepInEx.Paths.PluginPath, "StationpediaAscended", BUNDLE_NAME),
                Path.Combine(BepInEx.Paths.BepInExRootPath, "scripts", "assets", BUNDLE_NAME),
            };
            
            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            return null;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Handles click events on checkboxes in the editor to toggle their state.
    /// </summary>
    public class CheckboxClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public BlockEditor BlockEditor;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (BlockEditor == null || !BlockEditor.IsInitialized) return;
            
            // Only handle left clicks
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            // Get local point in the text component
            var inputField = BlockEditor.InputField;
            if (inputField == null || inputField.textComponent == null) return;
            
            RectTransform textRect = inputField.textComponent.GetComponent<RectTransform>();
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                textRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                return;
            }
            
            // Check if click is on a checkbox
            int checkboxLine = BlockEditor.GetCheckboxLineAtPosition(localPoint);
            if (checkboxLine >= 0)
            {
                BlockEditor.ToggleCheckboxOnLine(checkboxLine);
                
                // Prevent the click from changing caret position
                eventData.Use();
            }
        }
    }
}
