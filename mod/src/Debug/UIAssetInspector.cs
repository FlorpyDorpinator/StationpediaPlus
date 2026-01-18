using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Scripts.UI;

namespace StationpediaAscended.Diagnostics
{
    /// <summary>
    /// Debug tool to inspect UI assets under the mouse cursor.
    /// Toggle with console command: assetdisplay
    /// </summary>
    public class UIAssetInspector : MonoBehaviour
    {
        public static UIAssetInspector Instance { get; private set; }
        public static bool IsEnabled { get; private set; } = false;
        
        private Canvas _tooltipCanvas;
        private GameObject _tooltipPanel;
        private List<TextMeshProUGUI> _columnTexts = new List<TextMeshProUGUI>();
        private RectTransform _tooltipRect;
        private LayoutElement _panelLayoutElement;
        
        private const int ITEMS_PER_COLUMN = 1;
        private const int MAX_COLUMNS = 6;
        private const float COLUMN_WIDTH = 300f;
        private const float COLUMN_SPACING = 10f;
        private const float PADDING = 10f;
        private const float MAX_HEIGHT_PERCENT = 0.90f; // Max 90% of screen height
        
        private PointerEventData _pointerData;
        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private StringBuilder _sb = new StringBuilder();
        private List<StringBuilder> _columnBuilders = new List<StringBuilder>();
        private string _lastPlainTextContent = ""; // For clipboard copy
        private List<LayoutElement> _columnLayoutElements = new List<LayoutElement>();
        
        // Colors for the tooltip - dark blue with orange accent like game UI
        private static readonly Color BG_COLOR = new Color(0.05f, 0.08f, 0.12f, 0.95f); // Dark blue
        private static readonly Color BORDER_COLOR = new Color(0.98431f, 0.6902f, 0.23137f, 1f); // Orange #FBB03B
        private static readonly Color TEXT_COLOR = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        public static void Initialize()
        {
            // Find and destroy any existing UIAssetInspector by name (handles hot reload across assemblies)
            var existingGO = GameObject.Find("UIAssetInspector");
            if (existingGO != null)
            {
                Destroy(existingGO);
                UnityEngine.Debug.Log("[UIAssetInspector] Destroyed old instance for hot reload");
            }
            Instance = null;
            
            var go = new GameObject("UIAssetInspector");
            Instance = go.AddComponent<UIAssetInspector>();
            DontDestroyOnLoad(go);
            
            Instance.CreateTooltip();
            Instance.SetEnabled(false);
            
            UnityEngine.Debug.Log("[UIAssetInspector] Initialized. Use 'assetdisplay' command to toggle.");
        }
        
        public static void Toggle()
        {
            if (Instance == null) Initialize();
            Instance.SetEnabled(!IsEnabled);
        }
        
        public static void Cleanup()
        {
            if (Instance != null)
            {
                if (Instance._tooltipCanvas != null)
                    Destroy(Instance._tooltipCanvas.gameObject);
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }
        
        private void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (_tooltipCanvas != null)
                _tooltipCanvas.enabled = enabled;
            
            string msg = enabled ? "ENABLED - Right-click to copy to clipboard" : "DISABLED";
            UnityEngine.Debug.Log($"[UIAssetInspector] {msg}");
        }
        
        private void CreateTooltip()
        {
            // Create canvas for tooltip (highest sort order to always be on top)
            var canvasGO = new GameObject("AssetInspectorCanvas");
            canvasGO.transform.SetParent(transform);
            
            _tooltipCanvas = canvasGO.AddComponent<Canvas>();
            _tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _tooltipCanvas.sortingOrder = 32000; // Very high to be on top of everything
            
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Don't add GraphicRaycaster - we don't want this to block raycasts
            
            // Create tooltip panel
            _tooltipPanel = new GameObject("TooltipPanel");
            _tooltipPanel.transform.SetParent(canvasGO.transform, false);
            
            _tooltipRect = _tooltipPanel.AddComponent<RectTransform>();
            _tooltipRect.pivot = new Vector2(0, 1); // Top-left pivot
            _tooltipRect.anchorMin = new Vector2(0, 0);
            _tooltipRect.anchorMax = new Vector2(0, 0);
            
            var bg = _tooltipPanel.AddComponent<Image>();
            bg.color = BG_COLOR;
            bg.raycastTarget = false;
            
            var outline = _tooltipPanel.AddComponent<Outline>();
            outline.effectColor = BORDER_COLOR;
            outline.effectDistance = new Vector2(2, 2);
            
            // Content size fitter - only fit width, not height (we'll control height manually)
            var csf = _tooltipPanel.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            
            // Add LayoutElement to panel to enforce max height
            _panelLayoutElement = _tooltipPanel.AddComponent<LayoutElement>();
            _panelLayoutElement.preferredHeight = Screen.height * MAX_HEIGHT_PERCENT;
            
            // Horizontal layout for columns
            var hlg = _tooltipPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            
            // Create column containers (we'll show/hide as needed)
            _columnTexts.Clear();
            _columnBuilders.Clear();
            for (int i = 0; i < MAX_COLUMNS; i++)
            {
                var colGO = new GameObject($"Column{i}");
                colGO.transform.SetParent(_tooltipPanel.transform, false);
                
                var colText = colGO.AddComponent<TextMeshProUGUI>();
                colText.fontSize = 11;
                colText.color = TEXT_COLOR;
                colText.alignment = TextAlignmentOptions.TopLeft;
                colText.enableWordWrapping = true;
                colText.overflowMode = TextOverflowModes.Truncate;
                colText.raycastTarget = false;
                colText.richText = true;
                
                var colLE = colGO.AddComponent<LayoutElement>();
                colLE.minWidth = COLUMN_WIDTH;
                colLE.preferredWidth = COLUMN_WIDTH;
                colLE.minHeight = 50;
                colLE.preferredHeight = Screen.height * MAX_HEIGHT_PERCENT - 50; // Leave room for padding
                colLE.flexibleWidth = 0;
                colLE.flexibleHeight = 0;
                
                _columnTexts.Add(colText);
                _columnBuilders.Add(new StringBuilder());
                _columnLayoutElements.Add(colLE);
                
                colGO.SetActive(false); // Start hidden
            }
        }
        
        void Update()
        {
            if (!IsEnabled || _tooltipCanvas == null) return;
            
            // Check for right-click to copy to clipboard
            if (Input.GetMouseButtonDown(1))
            {
                CopyToClipboard();
            }
            
            // Raycast to find UI elements first (to know how many columns we need)
            UpdateTooltipContent();
            
            // Force layout update to get accurate size
            LayoutRebuilder.ForceRebuildLayoutImmediate(_tooltipRect);
            
            // Position tooltip near mouse with smart screen boundary handling
            PositionTooltip();
        }
        
        private void CopyToClipboard()
        {
            if (string.IsNullOrEmpty(_lastPlainTextContent)) return;
            
            GUIUtility.systemCopyBuffer = _lastPlainTextContent;
            UnityEngine.Debug.Log("[UIAssetInspector] Copied to clipboard!");
        }
        
        private void PositionTooltip()
        {
            Vector2 mousePos = Input.mousePosition;
            
            // Calculate max allowed height
            float maxHeight = Screen.height * MAX_HEIGHT_PERCENT;
            
            // Force the panel and columns to respect max height
            _panelLayoutElement.preferredHeight = maxHeight;
            foreach (var le in _columnLayoutElements)
            {
                le.preferredHeight = maxHeight - 20; // Account for padding
            }
            
            // Set the rect height directly
            _tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight);
            
            Vector2 size = new Vector2(_tooltipRect.sizeDelta.x, maxHeight);
            
            // Default position: to the right and below cursor
            float tooltipX = mousePos.x + 20;
            float tooltipY = mousePos.y - 10;
            
            // Check right edge - if tooltip goes off screen, flip to left side of cursor
            if (tooltipX + size.x > Screen.width - 10)
            {
                tooltipX = mousePos.x - size.x - 20;
            }
            
            // Check left edge - if still off screen (very wide tooltip), clamp to left edge
            if (tooltipX < 10)
            {
                tooltipX = 10;
            }
            
            // Check bottom edge - if tooltip goes off screen, move it up
            if (tooltipY - size.y < 10)
            {
                // Position at bottom of screen with padding
                tooltipY = size.y + 10;
            }
            
            // Check top edge - clamp to top
            if (tooltipY > Screen.height - 10)
            {
                tooltipY = Screen.height - 10;
            }
            
            _tooltipRect.position = new Vector2(tooltipX, tooltipY);
        }
        
        private void UpdateTooltipContent()
        {
            // Clear all column builders
            foreach (var sb in _columnBuilders)
                sb.Clear();
            
            // Get all EventSystems and raycast
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                _columnBuilders[0].AppendLine("<color=#FBB03B><b>== UI ASSET INSPECTOR ==</b></color>");
                _columnBuilders[0].AppendLine("<color=#FF8080>No EventSystem found</color>");
                ApplyColumnsToUI(1);
                return;
            }
            
            if (_pointerData == null)
                _pointerData = new PointerEventData(eventSystem);
            
            _pointerData.position = Input.mousePosition;
            _raycastResults.Clear();
            
            // Raycast all
            eventSystem.RaycastAll(_pointerData, _raycastResults);
            
            // Filter out our own inspector
            var filteredResults = new List<RaycastResult>();
            foreach (var result in _raycastResults)
            {
                if (result.gameObject != null && !result.gameObject.transform.IsChildOf(transform))
                    filteredResults.Add(result);
            }
            
            // Check for TranslucentImageSources globally
            var tiSources = FindObjectsOfType<LeTai.Asset.TranslucentImage.TranslucentImageSource>();
            
            // Header in first column
            _columnBuilders[0].AppendLine("<color=#FBB03B><b>== UI ASSET INSPECTOR ==</b></color>");
            _columnBuilders[0].AppendLine("<color=#808080>Right-click to copy</color>");
            _columnBuilders[0].AppendLine("<color=#606060>------------------------</color>");
            
            // Show TranslucentImageSource status
            if (tiSources.Length > 0)
            {
                _columnBuilders[0].AppendLine($"<color=#FF80FF>TranslucentImageSources: {tiSources.Length}</color>");
                foreach (var tis in tiSources)
                    _columnBuilders[0].AppendLine($"  -> {tis.gameObject.name}");
            }
            else
            {
                _columnBuilders[0].AppendLine("<color=#FF8080>No TranslucentImageSource!</color>");
            }
            _columnBuilders[0].AppendLine("<color=#606060>------------------------</color>");
            
            if (filteredResults.Count == 0)
            {
                _columnBuilders[0].AppendLine("<color=#FFFF80>No UI elements under cursor</color>");
                ApplyColumnsToUI(1);
                return;
            }
            
            _columnBuilders[0].AppendLine($"<color=#80FFFF>Found {filteredResults.Count} element(s):</color>\n");
            
            // Calculate how many columns we need
            int totalItems = Mathf.Min(filteredResults.Count, ITEMS_PER_COLUMN * MAX_COLUMNS);
            int columnsNeeded = Mathf.CeilToInt((float)totalItems / ITEMS_PER_COLUMN);
            columnsNeeded = Mathf.Max(1, Mathf.Min(columnsNeeded, MAX_COLUMNS));
            
            // Distribute items across columns
            int itemIndex = 0;
            for (int col = 0; col < columnsNeeded && itemIndex < totalItems; col++)
            {
                // First column already has header, so add fewer items
                int itemsInThisColumn = (col == 0) ? ITEMS_PER_COLUMN - 1 : ITEMS_PER_COLUMN;
                
                for (int i = 0; i < itemsInThisColumn && itemIndex < totalItems; i++)
                {
                    var go = filteredResults[itemIndex].gameObject;
                    AppendGameObjectInfo(_columnBuilders[col], go, itemIndex + 1);
                    itemIndex++;
                }
            }
            
            // Show remaining count if truncated
            if (filteredResults.Count > totalItems)
            {
                int lastCol = columnsNeeded - 1;
                _columnBuilders[lastCol].AppendLine($"<color=#808080>... and {filteredResults.Count - totalItems} more</color>");
            }
            
            ApplyColumnsToUI(columnsNeeded);
            
            // Build plain text version for clipboard
            BuildPlainTextContent(columnsNeeded);
        }
        
        private void BuildPlainTextContent(int columnsNeeded)
        {
            _sb.Clear();
            for (int i = 0; i < columnsNeeded; i++)
            {
                // Strip rich text tags for plain clipboard content
                string text = _columnBuilders[i].ToString();
                text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
                _sb.Append(text);
            }
            _lastPlainTextContent = _sb.ToString();
        }
        
        private void ApplyColumnsToUI(int columnsNeeded)
        {
            // Calculate available width and adjust columns to fit
            float availableWidth = Screen.width - PADDING * 2;
            float availableHeight = Screen.height * MAX_HEIGHT_PERCENT;
            float totalNeededWidth = columnsNeeded * COLUMN_WIDTH + (columnsNeeded - 1) * COLUMN_SPACING + PADDING * 2;
            
            // If too wide, reduce columns or shrink width
            int actualColumns = columnsNeeded;
            float columnWidth = COLUMN_WIDTH;
            
            if (totalNeededWidth > availableWidth)
            {
                // First try reducing columns
                while (actualColumns > 1)
                {
                    float testWidth = actualColumns * COLUMN_WIDTH + (actualColumns - 1) * COLUMN_SPACING + PADDING * 2;
                    if (testWidth <= availableWidth) break;
                    actualColumns--;
                }
                
                // If still too wide with one column, shrink the column width
                if (actualColumns == 1 && COLUMN_WIDTH + PADDING * 2 > availableWidth)
                {
                    columnWidth = availableWidth - PADDING * 2 - 20;
                }
            }
            
            for (int i = 0; i < _columnTexts.Count; i++)
            {
                bool shouldShow = i < actualColumns;
                _columnTexts[i].gameObject.SetActive(shouldShow);
                
                if (shouldShow)
                {
                    _columnTexts[i].text = _columnBuilders[i].ToString();
                    _columnLayoutElements[i].minWidth = columnWidth;
                    _columnLayoutElements[i].preferredWidth = columnWidth;
                    _columnLayoutElements[i].preferredHeight = availableHeight - 50;
                }
            }
        }
        
        private void AppendGameObjectInfo(StringBuilder sb, GameObject go, int index)
        {
            sb.AppendLine($"<color=#FFD700>[{index}] {go.name}</color>");
            
            // Get hierarchy path (up to 5 levels for more context)
            string path = GetHierarchyPath(go.transform, 5);
            sb.AppendLine($"  <color=#A0A0A0>Path: {path}</color>");
            
            // RectTransform details
            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                sb.AppendLine($"  <color=#C080C0>RectTransform:</color>");
                sb.AppendLine($"    <color=#808080>sizeDelta: {rect.sizeDelta}</color>");
                sb.AppendLine($"    <color=#808080>anchorMin: {rect.anchorMin} anchorMax: {rect.anchorMax}</color>");
                sb.AppendLine($"    <color=#808080>pivot: {rect.pivot}</color>");
                sb.AppendLine($"    <color=#808080>offsetMin: {rect.offsetMin} offsetMax: {rect.offsetMax}</color>");
            }
            
            // Check for Image component and sprite
            var image = go.GetComponent<Image>();
            if (image != null)
            {
                sb.Append("  <color=#80FF80>Image:</color> ");
                if (image.sprite != null)
                {
                    sb.AppendLine($"<color=#FFFFFF>{image.sprite.name}</color>");
                    if (image.sprite.texture != null)
                    {
                        var tex = image.sprite.texture;
                        sb.AppendLine($"    <color=#808080>Texture: {tex.name} ({tex.width}x{tex.height})</color>");
                        sb.AppendLine($"    <color=#808080>TexFormat: {tex.format}, isReadable: {tex.isReadable}</color>");
                        
                        // Try to sample pixels if readable
                        if (tex.isReadable)
                        {
                            try
                            {
                                var centerPixel = tex.GetPixel(tex.width / 2, tex.height / 2);
                                var cornerPixel = tex.GetPixel(2, 2);
                                sb.AppendLine($"    <color=#AAFFAA>CenterPixel: {ColorToHex(centerPixel)}</color>");
                                sb.AppendLine($"    <color=#AAFFAA>CornerPixel: {ColorToHex(cornerPixel)}</color>");
                            }
                            catch { }
                        }
                    }
                    
                    // Show sprite border for 9-slice info
                    var border = image.sprite.border;
                    if (border != Vector4.zero)
                        sb.AppendLine($"    <color=#808080>Border(9-slice): L={border.x} B={border.y} R={border.z} T={border.w}</color>");
                    
                    // Show sprite rect
                    sb.AppendLine($"    <color=#808080>SpriteRect: {image.sprite.rect}</color>");
                }
                else
                {
                    sb.AppendLine("<color=#808080>(no sprite)</color>");
                }
                sb.AppendLine($"    <color=#808080>Color: {ColorToHex(image.color)}</color>");
                sb.AppendLine($"    <color=#808080>Type: {image.type}, RaycastTarget: {image.raycastTarget}</color>");
                sb.AppendLine($"    <color=#808080>Material: {(image.material != null ? image.material.name : "null")}</color>");
                sb.AppendLine($"    <color=#808080>maskable: {image.maskable}, preserveAspect: {image.preserveAspect}</color>");
                
                // Check if it's a TranslucentImage
                var translucentImage = go.GetComponent<LeTai.Asset.TranslucentImage.TranslucentImage>();
                if (translucentImage != null)
                {
                    sb.AppendLine($"    <color=#FF80FF>== TranslucentImage ==</color>");
                    sb.AppendLine($"    <color=#FF80FF>vibrancy: {translucentImage.vibrancy:F3}</color>");
                    sb.AppendLine($"    <color=#FF80FF>brightness: {translucentImage.brightness:F3}</color>");
                    sb.AppendLine($"    <color=#FF80FF>flatten: {translucentImage.flatten:F3}</color>");
                    sb.AppendLine($"    <color=#FF80FF>spriteBlending: {translucentImage.spriteBlending:F3}</color>");
                    var tiMat = translucentImage.material;
                    sb.AppendLine($"    <color=#FF80FF>TI.material: {(tiMat != null ? tiMat.name : "null")}</color>");
                    if (tiMat != null && tiMat.shader != null)
                        sb.AppendLine($"    <color=#FF80FF>TI.shader: {tiMat.shader.name}</color>");
                    sb.AppendLine($"    <color=#FF80FF>TI.materialForRendering: {(translucentImage.materialForRendering != null ? translucentImage.materialForRendering.name : "null")}</color>");
                }
            }
            
            // Check ALL children for additional layers
            int childCount = go.transform.childCount;
            if (childCount > 0)
            {
                sb.AppendLine($"  <color=#FFAA00>Children ({childCount}):</color>");
                for (int i = 0; i < Mathf.Min(childCount, 8); i++)
                {
                    var child = go.transform.GetChild(i);
                    var childImg = child.GetComponent<Image>();
                    var childRect = child.GetComponent<RectTransform>();
                    
                    sb.Append($"    <color=#D0D000>{child.name}</color>");
                    if (childImg != null)
                    {
                        string spriteName = childImg.sprite != null ? childImg.sprite.name : "(no sprite)";
                        sb.Append($" -> <color=#80FF80>{spriteName}</color>");
                        sb.Append($" color:{ColorToHex(childImg.color)}");
                        sb.Append($" type:{childImg.type}");
                    }
                    if (childRect != null)
                    {
                        sb.Append($" size:{childRect.sizeDelta}");
                    }
                    sb.AppendLine();
                }
                if (childCount > 8)
                    sb.AppendLine($"    <color=#808080>... and {childCount - 8} more children</color>");
            }
            
            // Check parent for sprites (to understand context)
            if (go.transform.parent != null)
            {
                var parentGO = go.transform.parent.gameObject;
                var parentImage = parentGO.GetComponent<Image>();
                sb.AppendLine($"  <color=#60A060>Parent: {parentGO.name}</color>");
                if (parentImage != null)
                {
                    string pSpriteName = parentImage.sprite != null ? parentImage.sprite.name : "(no sprite)";
                    sb.AppendLine($"    <color=#60A060>Parent Image: {pSpriteName} color:{ColorToHex(parentImage.color)}</color>");
                }
                
                // Grandparent too for more context
                if (go.transform.parent.parent != null)
                {
                    var grandparent = go.transform.parent.parent.gameObject;
                    sb.AppendLine($"    <color=#508050>Grandparent: {grandparent.name}</color>");
                    var gpImage = grandparent.GetComponent<Image>();
                    if (gpImage != null && gpImage.sprite != null)
                    {
                        sb.AppendLine($"    <color=#508050>GP Image: {gpImage.sprite.name} color:{ColorToHex(gpImage.color)}</color>");
                    }
                }
            }
            
            // Check for siblings with sprites
            if (go.transform.parent != null)
            {
                int sibIdx = go.transform.GetSiblingIndex();
                int siblingCount = go.transform.parent.childCount;
                sb.AppendLine($"  <color=#6060A0>Sibling index: {sibIdx}/{siblingCount}</color>");
                
                // Show ALL siblings for context
                for (int i = 0; i < Mathf.Min(siblingCount, 6); i++)
                {
                    if (i == sibIdx) continue; // Skip self
                    var sibling = go.transform.parent.GetChild(i);
                    var sibImage = sibling.GetComponent<Image>();
                    if (sibImage != null)
                    {
                        string sSpr = sibImage.sprite != null ? sibImage.sprite.name : "(no sprite)";
                        sb.AppendLine($"    <color=#6060A0>Sib[{i}] {sibling.name}: {sSpr} {ColorToHex(sibImage.color)}</color>");
                    }
                }
            }
            
            // Check for RawImage
            var rawImage = go.GetComponent<RawImage>();
            if (rawImage != null)
            {
                sb.Append("  <color=#80FF80>RawImage:</color> ");
                if (rawImage.texture != null)
                    sb.AppendLine($"<color=#FFFFFF>{rawImage.texture.name}</color>");
                else
                    sb.AppendLine("<color=#808080>(no texture)</color>");
            }
            
            // Check for TextMeshProUGUI
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                string textPreview = tmp.text ?? "";
                if (textPreview.Length > 30)
                    textPreview = textPreview.Substring(0, 30) + "...";
                textPreview = textPreview.Replace("\n", "\\n").Replace("<", "[").Replace(">", "]");
                
                sb.AppendLine($"  <color=#80FFFF>TMP:</color> \"{textPreview}\"");
                if (tmp.font != null)
                    sb.AppendLine($"    <color=#808080>Font: {tmp.font.name}</color>");
            }
            
            // Check for Button
            var button = go.GetComponent<Button>();
            if (button != null)
            {
                sb.AppendLine($"  <color=#FFFF80>Button</color>");
            }
            
            // Check for UiComponentRenderer
            var uiRenderer = go.GetComponent<UiComponentRenderer>();
            if (uiRenderer != null)
            {
                sb.AppendLine($"  <color=#FF8080>UiComponentRenderer</color>");
            }
            
            // Check for Canvas
            var canvas = go.GetComponent<Canvas>();
            if (canvas != null)
            {
                sb.AppendLine($"  <color=#8080FF>Canvas: sortOrder={canvas.sortingOrder}, renderMode={canvas.renderMode}</color>");
            }
            
            // Check for CanvasGroup
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                sb.AppendLine($"  <color=#8080FF>CanvasGroup: alpha={canvasGroup.alpha}, blocksRaycasts={canvasGroup.blocksRaycasts}</color>");
            }
            
            // Check for Mask
            var mask = go.GetComponent<Mask>();
            if (mask != null)
            {
                sb.AppendLine($"  <color=#FF80FF>Mask present, enabled={mask.enabled}</color>");
            }
            
            // Check for RectMask2D  
            var rectMask = go.GetComponent<RectMask2D>();
            if (rectMask != null)
            {
                sb.AppendLine($"  <color=#FF80FF>RectMask2D present</color>");
            }
            
            // Check for Shadow/Outline effects
            var shadow = go.GetComponent<Shadow>();
            if (shadow != null)
            {
                sb.AppendLine($"  <color=#FFA0A0>Shadow: color={ColorToHex(shadow.effectColor)}, dist={shadow.effectDistance}</color>");
            }
            
            var outline = go.GetComponent<Outline>();
            if (outline != null)
            {
                sb.AppendLine($"  <color=#FFA0A0>Outline: color={ColorToHex(outline.effectColor)}, dist={outline.effectDistance}</color>");
            }
            
            // List other notable components
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                
                // Skip common/already listed components
                if (typeName == "RectTransform" || typeName == "CanvasRenderer" || 
                    typeName == "Image" || typeName == "RawImage" || typeName == "Button" ||
                    typeName == "TextMeshProUGUI" || typeName == "Canvas" || 
                    typeName == "UiComponentRenderer" || typeName == "TranslucentImage" ||
                    typeName == "CanvasGroup" || typeName == "Mask" || typeName == "RectMask2D" ||
                    typeName == "Shadow" || typeName == "Outline")
                    continue;
                
                // Show interesting components
                if (typeName.Contains("Scroll") || typeName.Contains("Layout") || 
                    typeName.Contains("Mask") || typeName.Contains("Input") ||
                    typeName.Contains("Toggle") || typeName.Contains("Slider") ||
                    typeName.Contains("Dropdown") || typeName.Contains("Editor") ||
                    typeName.Contains("Panel") || typeName.Contains("Window") ||
                    typeName.Contains("Fitter") || typeName.Contains("Anim"))
                {
                    sb.AppendLine($"  <color=#C0C0C0>{typeName}</color>");
                }
            }
            
            sb.AppendLine();
        }
        
        private string GetHierarchyPath(Transform t, int maxDepth)
        {
            if (t == null || maxDepth <= 0) return "";
            
            var parts = new List<string>();
            var current = t;
            int depth = 0;
            
            while (current != null && depth < maxDepth)
            {
                parts.Insert(0, current.name);
                current = current.parent;
                depth++;
            }
            
            if (current != null)
                parts.Insert(0, "...");
            
            return string.Join("/", parts);
        }
        
        private string ColorToHex(Color c)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
        }
    }
}
