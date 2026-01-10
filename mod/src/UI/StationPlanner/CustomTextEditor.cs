using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace StationpediaAscended.UI.StationPlanner
{
    /// <summary>
    /// Custom text editor with manual cursor implementation.
    /// Since TMP_InputField's caret doesn't work when created at runtime,
    /// we implement our own cursor rendering and text input handling.
    /// </summary>
    public class CustomTextEditor : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IScrollHandler
    {
        #region UI Components
        
        private TextMeshProUGUI _textDisplay;
        private TextMeshProUGUI _placeholder;
        private Image _cursorImage;
        private RectTransform _cursorRect;
        private RectTransform _textRect;
        private RectTransform _viewportRect;
        private ScrollRect _scrollRect;
        
        #endregion
        
        #region State
        
        private StringBuilder _text = new StringBuilder();
        private int _cursorPosition = 0;
        private int _selectionStart = -1; // -1 = no selection
        private int _selectionEnd = -1;
        private bool _isFocused = false;
        private float _cursorBlinkTimer = 0f;
        private bool _cursorVisible = true;
        
        #endregion
        
        #region Settings
        
        public Color CursorColor = new Color(0.984f, 0.69f, 0.231f, 1f); // Orange
        public Color SelectionColor = new Color(0.984f, 0.69f, 0.231f, 0.25f);
        public Color TextColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        public Color PlaceholderColor = new Color(0.5f, 0.55f, 0.6f, 1f);
        public float CursorBlinkRate = 0.53f;
        public int CursorWidth = 2;
        public string PlaceholderText = "Click here to type...";
        public int FontSize = 14;
        
        #endregion
        
        #region Events
        
        public event Action<string> OnTextChanged;
        public event Action OnFocusGained;
        public event Action OnFocusLost;
        
        #endregion
        
        #region Properties
        
        public string Text
        {
            get => _text.ToString();
            set
            {
                _text.Clear();
                if (!string.IsNullOrEmpty(value))
                    _text.Append(value);
                _cursorPosition = Mathf.Clamp(_cursorPosition, 0, _text.Length);
                ClearSelection();
                UpdateDisplay();
            }
        }
        
        public int CursorPos
        {
            get => _cursorPosition;
            set
            {
                _cursorPosition = Mathf.Clamp(value, 0, _text.Length);
                UpdateCursorPosition();
            }
        }
        
        public bool IsFocused => _isFocused;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the custom editor with required components
        /// </summary>
        public void Initialize(RectTransform viewport, TMP_FontAsset font = null, Material fontMaterial = null)
        {
            _viewportRect = viewport;
            
            // Create scroll rect for scrolling long text
            _scrollRect = gameObject.GetComponent<ScrollRect>();
            if (_scrollRect == null)
                _scrollRect = gameObject.AddComponent<ScrollRect>();
            
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 25f;
            _scrollRect.viewport = viewport;
            
            // Content container for text
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewport, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.anchoredPosition = Vector2.zero;
            
            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _scrollRect.content = contentRect;
            
            // Text display
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(contentGO.transform, false);
            _textDisplay = textGO.AddComponent<TextMeshProUGUI>();
            _textDisplay.fontSize = FontSize;
            _textDisplay.color = TextColor;
            _textDisplay.alignment = TextAlignmentOptions.TopLeft;
            _textDisplay.enableWordWrapping = true;
            _textDisplay.overflowMode = TextOverflowModes.Overflow;
            _textDisplay.raycastTarget = false;
            _textDisplay.richText = false;
            
            if (font != null)
            {
                _textDisplay.font = font;
                if (fontMaterial != null)
                    _textDisplay.fontSharedMaterial = fontMaterial;
            }
            
            _textRect = textGO.GetComponent<RectTransform>();
            _textRect.anchorMin = Vector2.zero;
            _textRect.anchorMax = new Vector2(1, 1);
            _textRect.pivot = new Vector2(0, 1);
            _textRect.offsetMin = new Vector2(4, 0);
            _textRect.offsetMax = new Vector2(-4, 0);
            
            // Placeholder
            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(contentGO.transform, false);
            _placeholder = phGO.AddComponent<TextMeshProUGUI>();
            _placeholder.text = PlaceholderText;
            _placeholder.fontSize = FontSize;
            _placeholder.color = PlaceholderColor;
            _placeholder.fontStyle = FontStyles.Italic;
            _placeholder.alignment = TextAlignmentOptions.TopLeft;
            _placeholder.enableWordWrapping = true;
            _placeholder.raycastTarget = false;
            
            if (font != null)
            {
                _placeholder.font = font;
                if (fontMaterial != null)
                    _placeholder.fontSharedMaterial = fontMaterial;
            }
            
            var phRect = phGO.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = new Vector2(1, 1);
            phRect.pivot = new Vector2(0, 1);
            phRect.offsetMin = new Vector2(4, 0);
            phRect.offsetMax = new Vector2(-4, 0);
            
            // Cursor (blinking line)
            var cursorGO = new GameObject("Cursor");
            cursorGO.transform.SetParent(contentGO.transform, false);
            _cursorImage = cursorGO.AddComponent<Image>();
            _cursorImage.color = CursorColor;
            _cursorImage.raycastTarget = false;
            
            _cursorRect = cursorGO.GetComponent<RectTransform>();
            _cursorRect.anchorMin = new Vector2(0, 1);
            _cursorRect.anchorMax = new Vector2(0, 1);
            _cursorRect.pivot = new Vector2(0, 1);
            _cursorRect.sizeDelta = new Vector2(CursorWidth, FontSize + 4);
            
            UpdateDisplay();
        }
        
        #endregion
        
        #region Unity Events
        
        void Update()
        {
            if (!_isFocused) return;
            
            // Handle cursor blinking
            _cursorBlinkTimer += Time.deltaTime;
            if (_cursorBlinkTimer >= CursorBlinkRate)
            {
                _cursorBlinkTimer = 0f;
                _cursorVisible = !_cursorVisible;
                if (_cursorImage != null)
                    _cursorImage.enabled = _cursorVisible;
            }
            
            // Handle text input
            HandleTextInput();
            
            // Handle special keys
            HandleSpecialKeys();
        }
        
        void OnDisable()
        {
            if (_isFocused)
                Unfocus();
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleTextInput()
        {
            string input = Input.inputString;
            if (string.IsNullOrEmpty(input)) return;
            
            foreach (char c in input)
            {
                // Skip control characters except for allowed ones
                if (c == '\b') continue; // Backspace handled in HandleSpecialKeys
                if (c == '\n' || c == '\r')
                {
                    InsertCharacter('\n');
                    continue;
                }
                if (c < ' ' && c != '\t') continue; // Skip other control chars
                
                InsertCharacter(c);
            }
        }
        
        private void HandleSpecialKeys()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            
            // Backspace
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (HasSelection())
                    DeleteSelection();
                else if (_cursorPosition > 0)
                {
                    if (ctrl)
                    {
                        // Delete word
                        int wordStart = FindWordStart(_cursorPosition - 1);
                        _text.Remove(wordStart, _cursorPosition - wordStart);
                        _cursorPosition = wordStart;
                    }
                    else
                    {
                        _text.Remove(_cursorPosition - 1, 1);
                        _cursorPosition--;
                    }
                    OnTextModified();
                }
            }
            
            // Delete
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (HasSelection())
                    DeleteSelection();
                else if (_cursorPosition < _text.Length)
                {
                    if (ctrl)
                    {
                        // Delete word forward
                        int wordEnd = FindWordEnd(_cursorPosition);
                        _text.Remove(_cursorPosition, wordEnd - _cursorPosition);
                    }
                    else
                    {
                        _text.Remove(_cursorPosition, 1);
                    }
                    OnTextModified();
                }
            }
            
            // Arrow keys
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (shift)
                    ExtendSelection(-1, ctrl);
                else
                {
                    ClearSelection();
                    if (ctrl)
                        _cursorPosition = FindWordStart(_cursorPosition - 1);
                    else if (_cursorPosition > 0)
                        _cursorPosition--;
                }
                UpdateCursorPosition();
                ResetBlink();
            }
            
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (shift)
                    ExtendSelection(1, ctrl);
                else
                {
                    ClearSelection();
                    if (ctrl)
                        _cursorPosition = FindWordEnd(_cursorPosition);
                    else if (_cursorPosition < _text.Length)
                        _cursorPosition++;
                }
                UpdateCursorPosition();
                ResetBlink();
            }
            
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveToLine(-1, shift);
            }
            
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveToLine(1, shift);
            }
            
            // Home/End
            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (shift)
                    StartOrExtendSelection();
                else
                    ClearSelection();
                    
                if (ctrl)
                    _cursorPosition = 0;
                else
                    _cursorPosition = FindLineStart(_cursorPosition);
                    
                if (shift)
                    _selectionEnd = _cursorPosition;
                    
                UpdateCursorPosition();
                ResetBlink();
            }
            
            if (Input.GetKeyDown(KeyCode.End))
            {
                if (shift)
                    StartOrExtendSelection();
                else
                    ClearSelection();
                    
                if (ctrl)
                    _cursorPosition = _text.Length;
                else
                    _cursorPosition = FindLineEnd(_cursorPosition);
                    
                if (shift)
                    _selectionEnd = _cursorPosition;
                    
                UpdateCursorPosition();
                ResetBlink();
            }
            
            // Select All (Ctrl+A)
            if (ctrl && Input.GetKeyDown(KeyCode.A))
            {
                SelectAll();
            }
            
            // Copy (Ctrl+C)
            if (ctrl && Input.GetKeyDown(KeyCode.C))
            {
                CopyToClipboard();
            }
            
            // Cut (Ctrl+X)
            if (ctrl && Input.GetKeyDown(KeyCode.X))
            {
                CutToClipboard();
            }
            
            // Paste (Ctrl+V)
            if (ctrl && Input.GetKeyDown(KeyCode.V))
            {
                PasteFromClipboard();
            }
            
            // Undo placeholder (Ctrl+Z) - just beep for now
            if (ctrl && Input.GetKeyDown(KeyCode.Z))
            {
                // TODO: Implement undo stack
            }
        }
        
        private void InsertCharacter(char c)
        {
            if (HasSelection())
                DeleteSelection();
                
            _text.Insert(_cursorPosition, c);
            _cursorPosition++;
            OnTextModified();
        }
        
        public void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            if (HasSelection())
                DeleteSelection();
                
            _text.Insert(_cursorPosition, text);
            _cursorPosition += text.Length;
            OnTextModified();
        }
        
        #endregion
        
        #region Selection
        
        private bool HasSelection()
        {
            return _selectionStart >= 0 && _selectionEnd >= 0 && _selectionStart != _selectionEnd;
        }
        
        private void ClearSelection()
        {
            _selectionStart = -1;
            _selectionEnd = -1;
            UpdateDisplay();
        }
        
        private void StartOrExtendSelection()
        {
            if (_selectionStart < 0)
                _selectionStart = _cursorPosition;
        }
        
        private void ExtendSelection(int direction, bool byWord)
        {
            StartOrExtendSelection();
            
            if (byWord)
            {
                if (direction < 0)
                    _cursorPosition = FindWordStart(_cursorPosition - 1);
                else
                    _cursorPosition = FindWordEnd(_cursorPosition);
            }
            else
            {
                _cursorPosition = Mathf.Clamp(_cursorPosition + direction, 0, _text.Length);
            }
            
            _selectionEnd = _cursorPosition;
            UpdateDisplay();
        }
        
        private void DeleteSelection()
        {
            if (!HasSelection()) return;
            
            int start = Mathf.Min(_selectionStart, _selectionEnd);
            int end = Mathf.Max(_selectionStart, _selectionEnd);
            
            _text.Remove(start, end - start);
            _cursorPosition = start;
            ClearSelection();
            OnTextModified();
        }
        
        public void SelectAll()
        {
            _selectionStart = 0;
            _selectionEnd = _text.Length;
            _cursorPosition = _text.Length;
            UpdateDisplay();
        }
        
        private string GetSelectedText()
        {
            if (!HasSelection()) return "";
            
            int start = Mathf.Min(_selectionStart, _selectionEnd);
            int end = Mathf.Max(_selectionStart, _selectionEnd);
            
            return _text.ToString(start, end - start);
        }
        
        #endregion
        
        #region Clipboard
        
        private void CopyToClipboard()
        {
            string selected = GetSelectedText();
            if (!string.IsNullOrEmpty(selected))
                GUIUtility.systemCopyBuffer = selected;
        }
        
        private void CutToClipboard()
        {
            CopyToClipboard();
            if (HasSelection())
                DeleteSelection();
        }
        
        private void PasteFromClipboard()
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrEmpty(clipboard))
            {
                // Sanitize - remove carriage returns, keep newlines
                clipboard = clipboard.Replace("\r\n", "\n").Replace("\r", "\n");
                InsertText(clipboard);
            }
        }
        
        #endregion
        
        #region Navigation Helpers
        
        private int FindWordStart(int pos)
        {
            if (pos <= 0) return 0;
            pos = Mathf.Min(pos, _text.Length - 1);
            
            // Skip whitespace
            while (pos > 0 && char.IsWhiteSpace(_text[pos]))
                pos--;
            
            // Find word boundary
            while (pos > 0 && !char.IsWhiteSpace(_text[pos - 1]))
                pos--;
                
            return pos;
        }
        
        private int FindWordEnd(int pos)
        {
            if (pos >= _text.Length) return _text.Length;
            
            // Skip current word
            while (pos < _text.Length && !char.IsWhiteSpace(_text[pos]))
                pos++;
            
            // Skip whitespace
            while (pos < _text.Length && char.IsWhiteSpace(_text[pos]))
                pos++;
                
            return pos;
        }
        
        private int FindLineStart(int pos)
        {
            while (pos > 0 && _text[pos - 1] != '\n')
                pos--;
            return pos;
        }
        
        private int FindLineEnd(int pos)
        {
            while (pos < _text.Length && _text[pos] != '\n')
                pos++;
            return pos;
        }
        
        private void MoveToLine(int direction, bool extendSelection)
        {
            if (_textDisplay == null) return;
            
            // Force text update to get accurate info
            _textDisplay.ForceMeshUpdate();
            
            var textInfo = _textDisplay.textInfo;
            if (textInfo.characterCount == 0)
            {
                if (!extendSelection) ClearSelection();
                return;
            }
            
            // Find current line
            int currentLine = 0;
            int charIndex = Mathf.Min(_cursorPosition, textInfo.characterCount - 1);
            if (charIndex >= 0 && charIndex < textInfo.characterCount)
            {
                currentLine = textInfo.characterInfo[charIndex].lineNumber;
            }
            
            int targetLine = Mathf.Clamp(currentLine + direction, 0, textInfo.lineCount - 1);
            
            if (targetLine == currentLine)
            {
                // At first or last line, go to start or end
                if (direction < 0)
                    _cursorPosition = 0;
                else
                    _cursorPosition = _text.Length;
            }
            else
            {
                // Get horizontal position on current line
                float xPos = 0;
                if (charIndex >= 0 && charIndex < textInfo.characterCount)
                {
                    xPos = textInfo.characterInfo[charIndex].origin;
                }
                
                // Find closest character on target line
                var lineInfo = textInfo.lineInfo[targetLine];
                int bestChar = lineInfo.firstCharacterIndex;
                float bestDist = float.MaxValue;
                
                for (int i = lineInfo.firstCharacterIndex; i <= lineInfo.lastCharacterIndex && i < textInfo.characterCount; i++)
                {
                    float dist = Mathf.Abs(textInfo.characterInfo[i].origin - xPos);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestChar = i;
                    }
                }
                
                _cursorPosition = Mathf.Min(bestChar, _text.Length);
            }
            
            if (extendSelection)
            {
                StartOrExtendSelection();
                _selectionEnd = _cursorPosition;
            }
            else
            {
                ClearSelection();
            }
            
            UpdateCursorPosition();
            ResetBlink();
        }
        
        #endregion
        
        #region Display Updates
        
        private void OnTextModified()
        {
            UpdateDisplay();
            OnTextChanged?.Invoke(_text.ToString());
        }
        
        private void UpdateDisplay()
        {
            if (_textDisplay == null) return;
            
            string text = _text.ToString();
            
            // Show placeholder if empty
            if (_placeholder != null)
                _placeholder.gameObject.SetActive(string.IsNullOrEmpty(text) && !_isFocused);
            
            // Update text display
            // For selection, we'd need to add highlight - simplified for now
            _textDisplay.text = text;
            
            // Force mesh update for cursor positioning
            _textDisplay.ForceMeshUpdate();
            
            UpdateCursorPosition();
        }
        
        private void UpdateCursorPosition()
        {
            if (_cursorRect == null || _textDisplay == null) return;
            
            _textDisplay.ForceMeshUpdate();
            
            var textInfo = _textDisplay.textInfo;
            Vector2 cursorPos = new Vector2(4, 0); // Default position with padding
            
            if (_cursorPosition > 0 && textInfo.characterCount > 0)
            {
                int charIndex = Mathf.Min(_cursorPosition - 1, textInfo.characterCount - 1);
                
                if (charIndex >= 0)
                {
                    var charInfo = textInfo.characterInfo[charIndex];
                    
                    // Position after the character
                    cursorPos.x = charInfo.origin + charInfo.xAdvance + 4; // Add padding
                    
                    // Get line info for Y position
                    int lineIndex = charInfo.lineNumber;
                    if (lineIndex < textInfo.lineCount)
                    {
                        var lineInfo = textInfo.lineInfo[lineIndex];
                        cursorPos.y = -lineInfo.baseline + lineInfo.ascender;
                    }
                }
            }
            else if (textInfo.lineCount > 0)
            {
                // At start - use first line info
                var lineInfo = textInfo.lineInfo[0];
                cursorPos.y = -lineInfo.baseline + lineInfo.ascender;
            }
            
            _cursorRect.anchoredPosition = new Vector2(cursorPos.x, -cursorPos.y);
            
            // Scroll to keep cursor visible
            EnsureCursorVisible();
        }
        
        private void EnsureCursorVisible()
        {
            if (_scrollRect == null || _cursorRect == null || _viewportRect == null) return;
            
            // Get cursor position relative to viewport
            float cursorY = -_cursorRect.anchoredPosition.y;
            float viewportHeight = _viewportRect.rect.height;
            float contentHeight = _scrollRect.content.rect.height;
            
            if (contentHeight <= viewportHeight) return; // No scrolling needed
            
            float scrollRange = contentHeight - viewportHeight;
            float currentScroll = (1f - _scrollRect.verticalNormalizedPosition) * scrollRange;
            
            // Calculate visible range
            float visibleTop = currentScroll;
            float visibleBottom = currentScroll + viewportHeight;
            
            // Scroll if cursor outside visible range
            float cursorHeight = _cursorRect.sizeDelta.y;
            
            if (cursorY < visibleTop + 10)
            {
                // Scroll up
                float newScroll = Mathf.Max(0, cursorY - 10);
                _scrollRect.verticalNormalizedPosition = 1f - (newScroll / scrollRange);
            }
            else if (cursorY + cursorHeight > visibleBottom - 10)
            {
                // Scroll down
                float newScroll = Mathf.Min(scrollRange, cursorY + cursorHeight - viewportHeight + 10);
                _scrollRect.verticalNormalizedPosition = 1f - (newScroll / scrollRange);
            }
        }
        
        private void ResetBlink()
        {
            _cursorBlinkTimer = 0f;
            _cursorVisible = true;
            if (_cursorImage != null)
                _cursorImage.enabled = true;
        }
        
        #endregion
        
        #region Focus Management
        
        public void Focus()
        {
            if (_isFocused) return;
            
            _isFocused = true;
            ResetBlink();
            
            if (_placeholder != null)
                _placeholder.gameObject.SetActive(false);
            
            if (_cursorImage != null)
                _cursorImage.enabled = true;
                
            OnFocusGained?.Invoke();
        }
        
        public void Unfocus()
        {
            if (!_isFocused) return;
            
            _isFocused = false;
            ClearSelection();
            
            if (_placeholder != null)
                _placeholder.gameObject.SetActive(_text.Length == 0);
            
            if (_cursorImage != null)
                _cursorImage.enabled = false;
                
            OnFocusLost?.Invoke();
        }
        
        #endregion
        
        #region Pointer Events
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Focus();
            PositionCursorFromClick(eventData);
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            Focus();
            PositionCursorFromClick(eventData);
        }
        
        public void OnScroll(PointerEventData eventData)
        {
            // ScrollRect handles this
        }
        
        private void PositionCursorFromClick(PointerEventData eventData)
        {
            if (_textDisplay == null) return;
            
            _textDisplay.ForceMeshUpdate();
            var textInfo = _textDisplay.textInfo;
            
            if (textInfo.characterCount == 0)
            {
                _cursorPosition = 0;
                ClearSelection();
                UpdateCursorPosition();
                return;
            }
            
            // Convert click position to local coordinates
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _textRect, eventData.position, eventData.pressEventCamera, out localPoint);
            
            // Find closest character
            int closestChar = 0;
            float closestDist = float.MaxValue;
            
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                
                // Check distance to character center
                float charCenterX = charInfo.origin + charInfo.xAdvance * 0.5f;
                int lineIndex = charInfo.lineNumber;
                float charCenterY = 0;
                
                if (lineIndex < textInfo.lineCount)
                {
                    var lineInfo = textInfo.lineInfo[lineIndex];
                    charCenterY = -(lineInfo.baseline - lineInfo.ascender * 0.5f);
                }
                
                float dist = Vector2.Distance(localPoint, new Vector2(charCenterX, charCenterY));
                
                if (dist < closestDist)
                {
                    closestDist = dist;
                    // Determine if click is before or after character center
                    if (localPoint.x < charCenterX)
                        closestChar = i;
                    else
                        closestChar = i + 1;
                }
            }
            
            _cursorPosition = Mathf.Clamp(closestChar, 0, _text.Length);
            ClearSelection();
            UpdateCursorPosition();
            ResetBlink();
        }
        
        #endregion
    }
}
