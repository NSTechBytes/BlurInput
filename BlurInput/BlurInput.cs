using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Rainmeter;

namespace PluginBlurInput
{
    public class Measure
    {
        #region Constants
        private const string DEFAULT_CURSOR = "|";
        private const string TAB_SPACES = "    ";
        private const double UPDATE_INTERVAL = 25;
        private const int MAX_HISTORY_SIZE = 100;
        private const int RESET_TIMER_DELAY = 50;
        #endregion

        #region Win32 API
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint virtualKey, uint scanCode, byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder receivingBuffer, int bufferSize, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);
        #endregion

        #region Fields
        private Rainmeter.API _api;
        private System.Timers.Timer _updateTimer;
        private System.Timers.Timer _resetTimer;

        // Plugin configuration
        private PluginConfig _config;
        private InputValidator _validator;
        private TextProcessor _textProcessor;
        private CursorManager _cursorManager;

        // State management
        private bool _isActive = false;
        private bool _isInitialized = false;
        private bool _hasResetOnce = false;
        private bool _resetOnce = false;

        // Text and cursor state
        private string _textBuffer = "";
        private int _cursorPosition = 0;

        // History and undo/redo
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private readonly List<string> _historyStack = new List<string>();
        private int _historyIndex = -1;

        // UI state
        private bool _contextFocusForm = false;
        private bool _contextFormOpen = false;

        // Reference to the current context form
        private ContextForm _currentContextForm = null;
        #endregion

        #region Properties
        public string MyName { get; private set; }
        public bool ContextFocusForm
        {
            get => _contextFocusForm;
            set => _contextFocusForm = value;
        }
        public bool ContextFormOpen
        {
            get => _contextFormOpen;
            set => _contextFormOpen = value;
        }
        #endregion

        #region Public Methods
        public string GetUserInput() => _textBuffer;

        internal void Reload(Rainmeter.API api, ref double maxValue)
        {
            _api = api;
            MyName = api.GetMeasureName();

            LoadConfiguration();
            InitializeComponents();
            InitializeTimers();

            if (!_isInitialized)
            {
                InitializeTextBuffer();
                _isInitialized = true;
            }

            ValidateAndAdjustTextBuffer();
        }

        internal void Update()
        {
            if (!_isActive || string.IsNullOrEmpty(_config.MeterName))
                return;

            HandleMouseInput();
            HandleKeyboardInput();
        }

        internal void Start()
        {
            if (_config.Disabled || _isActive)
            {
                if (_isActive)
                    _api.Log(API.LogType.Debug, "Plugin is already running. Start operation skipped.");
                return;
            }

            ActivatePlugin();
        }

        internal void Stop()
        {
            if (!_isActive) return;

            DeactivatePlugin();
        }

        internal void ClearText()
        {
            _textBuffer = "";
            _cursorPosition = 0;
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateDisplay();
        }

        internal void ShowContextForm()
        {
            if (!_isActive || _contextFormOpen) return;

            _contextFocusForm = false;
            _contextFormOpen = true;

            _currentContextForm = new ContextForm(this, _config.BackgroundColor, _config.ButtonColor, _config.TextColor);
            _currentContextForm.ShowDialog();
        }

        // Add method to close context form programmatically
        public void CloseContextForm()
        {
            if (_contextFormOpen && _currentContextForm != null)
            {
                _currentContextForm.CloseForm();
                _currentContextForm = null;
                _contextFormOpen = false;
                _contextFocusForm = true;
            }
        }
        #endregion

        #region Initialization
        private void LoadConfiguration()
        {
            _config = new PluginConfig
            {
                MeterName = _api.ReadString("MeterName", ""),
                InActiveValue = _api.ReadString("InActiveValue", ""),
                FormatMultiline = _api.ReadInt("FormatMultiline", 0) == 1,
                Disabled = _api.ReadInt("Disabled", 0) == 1,
                Cursor = _api.ReadString("Cursor", DEFAULT_CURSOR),
                IsPassword = _api.ReadInt("Password", 0) == 1,
                IsMultiline = _api.ReadInt("Multiline", 0) == 1,
                CharacterLimit = _api.ReadInt("InputLimit", 0),
                OnEnterAction = _api.ReadString("OnEnterAction", "").Trim(),
                OnESCAction = _api.ReadString("OnESCAction", "").Trim(),
                DefaultValue = _api.ReadString("DefaultValue", "").Trim(),
                Width = _api.ReadInt("ViewLimit", 0),
                UnFocusDismiss = _api.ReadInt("UnFocusDismiss", 0) == 1,
                DismissAction = _api.ReadString("OnDismissAction", ""),
                EnableInActiveValue = _api.ReadInt("SetInActiveValue", 0) == 1,
                UseRegex = _api.ReadInt("RegExpSubstitute", 0) == 1,
                SubstituteRule = _api.ReadString("Substitute", ""),
                ShowErrorForm = _api.ReadInt("ShowErrorDialog", 0) == 1,
                ForceValidInput = _api.ReadInt("ForceValidInput", 0) == 1,
                UnValidAction = _api.ReadString("OnInvalidAction", ""),
                InputType = _api.ReadString("InputType", "String").Trim().ToLowerInvariant(),
                AllowedCharacters = _api.ReadString("AllowedCharacters", ""),

                // Meter positioning
                MeterX = _api.ReadInt("MeterX", 0),
                MeterY = _api.ReadInt("MeterY", 0),
                MeterWidth = _api.ReadInt("MeterW", 0),
                MeterHeight = _api.ReadInt("MeterH", 0),

                // Skin positioning
                SkinX = int.Parse(_api.ReplaceVariables("#CURRENTCONFIGX#")),
                SkinY = int.Parse(_api.ReplaceVariables("#CURRENTCONFIGY#")),

                // Colors
                BackgroundColor = ParseColor(_api.ReadString("FormBackgroundColor", "30,30,30"), Color.FromArgb(30, 30, 30)),
                ButtonColor = ParseColor(_api.ReadString("FormButtonColor", "70,70,70"), Color.FromArgb(70, 70, 70)),
                TextColor = ParseColor(_api.ReadString("FormTextColor", "255,255,255"), Color.White)
            };

            ValidateInputType();
        }

        private void InitializeComponents()
        {
            _validator = new InputValidator(_config, _api);
            _textProcessor = new TextProcessor(_config, _api);
            _cursorManager = new CursorManager(_config);
        }

        private void InitializeTimers()
        {
            _updateTimer = new System.Timers.Timer(UPDATE_INTERVAL)
            {
                AutoReset = true
            };
            _updateTimer.Elapsed += (sender, e) => UpdatePosition();
        }

        private void InitializeTextBuffer()
        {
            _textBuffer = _config.CharacterLimit > 0 && _config.DefaultValue.Length > _config.CharacterLimit
                ? _config.DefaultValue.Substring(0, _config.CharacterLimit)
                : _config.DefaultValue;

            _cursorPosition = _textBuffer.Length;
        }

        private void ValidateInputType()
        {
            var validTypes = new[] { "string", "integer", "float", "letters", "alphanumeric", "hexadecimal", "email", "custom" };

            if (!validTypes.Contains(_config.InputType))
            {
                _api.Log(API.LogType.Warning, $"Invalid InputType '{_config.InputType}', defaulting to 'string'.");
                _config.InputType = "string";
            }

            if (_config.InputType == "custom" && string.IsNullOrEmpty(_config.AllowedCharacters))
            {
                _api.Log(API.LogType.Warning, "InputType 'custom' requires 'AllowedCharacters'. Defaulting to 'string'.");
                _config.InputType = "string";
            }
        }

        private void ValidateAndAdjustTextBuffer()
        {
            if (_config.CharacterLimit > 0 && _textBuffer.Length > _config.CharacterLimit)
            {
                _textBuffer = _textBuffer.Substring(0, _config.CharacterLimit);
                _cursorPosition = Math.Min(_cursorPosition, _config.CharacterLimit);
            }

            _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _textBuffer.Length));
        }
        #endregion

        #region Plugin State Management
        private void ActivatePlugin()
        {
            if (_config.UnFocusDismiss)
                _contextFocusForm = true;

            _isActive = true;
            _hasResetOnce = false;
            _cursorPosition = _textBuffer.Length;
            _updateTimer.Start();

            ScheduleReset();
        }

        private void DeactivatePlugin()
        {
            _isActive = false;
            _hasResetOnce = false;
            _resetOnce = false;
            _cursorPosition = 0;
            _undoStack.Clear();
            _redoStack.Clear();
            _updateTimer.Stop();

            // Close any open context form
            CloseContextForm();
        }

        private void ScheduleReset()
        {
            if (_hasResetOnce) return;

            _hasResetOnce = true;
            _resetTimer = new System.Timers.Timer(RESET_TIMER_DELAY)
            {
                AutoReset = false
            };
            _resetTimer.Elapsed += (sender, e) =>
            {
                ResetToDefaultValue();
                _resetTimer.Stop();
            };
            _resetTimer.Start();
        }

        private void ResetToDefaultValue()
        {
            _textBuffer = _config.DefaultValue;
            _cursorPosition = _textBuffer.Length;
            _undoStack.Clear();
            _redoStack.Clear();
            _resetOnce = true;
            UpdateDisplay();
        }
        #endregion

        #region Input Handling
        private void HandleMouseInput()
        {
            var mouseButtons = Control.MouseButtons;
            if (mouseButtons != MouseButtons.Left && mouseButtons != MouseButtons.Right)
                return;

            var mousePosition = GetMousePosition();
            var isInsideMeter = IsMouseInsideMeter(mousePosition);

            if (isInsideMeter)
            {
                if (mouseButtons == MouseButtons.Right)
                    ShowContextForm();
                else if (mouseButtons == MouseButtons.Left)
                    UpdateCursorWithMouse(mousePosition);
            }
            else if (_config.UnFocusDismiss && _isActive && _contextFocusForm)
            {
                HandleUnFocusDismiss();
            }
        }

        private void HandleKeyboardInput()
        {
            bool ctrlPressed = (GetAsyncKeyState(17) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(16) & 0x8000) != 0;
            bool capsLockActive = (GetKeyState(20) & 0x0001) != 0;

            for (int i = 8; i <= 255; i++)
            {
                if ((GetAsyncKeyState(i) & 0x0001) == 0) continue;

                if (i == 13 && ctrlPressed)
                {
                    HandleCtrlEnter();
                    return;
                }

                if (ctrlPressed)
                    HandleCtrlShortcuts(i);
                else
                    HandleSpecialKeys(i, shiftPressed, ctrlPressed);

                UpdateDisplay();
                break;
            }
        }
        #endregion

        #region Keyboard Shortcuts and Special Keys
        private void HandleCtrlShortcuts(int keyCode)
        {
            switch (keyCode)
            {
                case 67: CopyToClipboard(); break;      // Ctrl+C
                case 86: PasteFromClipboard(); break;   // Ctrl+V
                case 88: CutToClipboard(); break;       // Ctrl+X
                case 90: Undo(); break;                 // Ctrl+Z
                case 89: Redo(); break;                 // Ctrl+Y
            }
        }

        private void HandleSpecialKeys(int keyCode, bool shiftPressed, bool ctrlPressed)
        {
            SaveStateForUndo();

            switch (keyCode)
            {
                case 8: HandleBackspace(); break;
                case 27: HandleEscape(); break;
                case 13: HandleEnter(ctrlPressed); break;
                case 46: HandleDelete(); break;
                case 37: HandleLeftArrow(); break;
                case 39: HandleRightArrow(); break;
                case 36: HandleHome(); break;
                case 35: HandleEnd(); break;
                case 9: HandleTab(); break;
                case 20: /* CapsLock - handled elsewhere */ break;
                case 38: HandleUpArrow(); break;
                case 40: HandleDownArrow(); break;
                default: HandleCharacterInput(keyCode); break;
            }
        }

        private void HandleBackspace()
        {
            if (_cursorPosition > 0)
            {
                _textBuffer = _textBuffer.Remove(_cursorPosition - 1, 1);
                _cursorPosition--;
            }
        }

        private void HandleEscape()
        {
            HandleESC();
        }

        private void HandleEnter(bool ctrlPressed)
        {
            if (_config.IsMultiline && !ctrlPressed)
                InsertText("\n");
            else
                _validator.ValidateTextBuffer(_textBuffer, _config.InputType, this);
        }

        private void HandleDelete()
        {
            if (_cursorPosition < _textBuffer.Length)
                _textBuffer = _textBuffer.Remove(_cursorPosition, 1);
        }

        private void HandleLeftArrow()
        {
            if (_cursorPosition > 0)
            {
                _cursorPosition--;
                _cursorManager.ResetPreferredColumn();
            }
        }

        private void HandleRightArrow()
        {
            if (_cursorPosition < _textBuffer.Length)
            {
                _cursorPosition++;
                _cursorManager.ResetPreferredColumn();
            }
        }

        private void HandleHome()
        {
            _cursorPosition = 0;
            _cursorManager.ResetPreferredColumn();
        }

        private void HandleEnd()
        {
            _cursorPosition = _textBuffer.Length;
            _cursorManager.ResetPreferredColumn();
        }

        private void HandleTab()
        {
            InsertText(TAB_SPACES);
        }

        private void HandleUpArrow()
        {
            if (_config.IsMultiline)
                _cursorManager.MoveCursorUp(ref _cursorPosition, _textBuffer);
            else
                NavigateHistory(-1);
        }

        private void HandleDownArrow()
        {
            if (_config.IsMultiline)
                _cursorManager.MoveCursorDown(ref _cursorPosition, _textBuffer);
            else
                NavigateHistory(1);
        }

        private void HandleCharacterInput(int keyCode)
        {
            char keyChar = MapKeyToCharacterDynamic(keyCode);
            if (keyChar != '\0' && _validator.IsValidInput(keyChar, _config.InputType, _cursorPosition))
            {
                InsertText(keyChar.ToString());
            }
        }

        private void HandleCtrlEnter()
        {
            _validator.ValidateTextBuffer(_textBuffer, _config.InputType, this);
        }
        #endregion

        #region Event Handlers
        public void HandleESC()
        {
            if (!_isActive) return;

            // First check if context menu is open and close it
            if (_contextFormOpen)
            {
                CloseContextForm();
                return;
            }

            // If no context menu is open, proceed with normal ESC behavior
            DeactivatePlugin();
            _api.Execute(_config.OnESCAction);
            ResetToDefault();
            UpdateMeter();
        }

        public void HandleDismiss()
        {
            if (!_isActive) return;

            DeactivatePlugin();
            _api.Execute(_config.UnValidAction);
            ResetToDefault();
            UpdateMeter();
        }

        public void HandleUnFocusDismiss()
        {
            if (!_isActive) return;

            DeactivatePlugin();
            _api.Execute(_config.DismissAction);
            ResetToDefault();
            UpdateMeter();
        }

        public void HandleValidInput()
        {
            _textProcessor.ConvertTextBufferToSingleLine(ref _textBuffer, _config.FormatMultiline);
            UpdateMeasure();
            _api.Execute(_config.OnEnterAction);
            Stop();
        }

        public void ShowError(string errorMessage)
        {
            if (_config.ShowErrorForm)
            {
                var errorForm = new ErrorForm(errorMessage, _config.BackgroundColor, _config.ButtonColor, _config.TextColor);
                errorForm.ShowDialog();
            }
            else
            {
                _api.Log(API.LogType.Error, errorMessage);
            }
        }

        private void ResetToDefault()
        {
            _textBuffer = _config.DefaultValue;
            _cursorPosition = 0;
            _undoStack.Clear();
            _redoStack.Clear();
        }
        #endregion

        #region Clipboard Operations
        internal void CopyToClipboard()
        {
            if (!string.IsNullOrEmpty(_textBuffer))
                Clipboard.SetText(_textBuffer);
        }

        internal void PasteFromClipboard()
        {
            if (!Clipboard.ContainsText()) return;

            SaveStateForUndo();
            string clipboardText = Clipboard.GetText();
            InsertText(clipboardText.Trim());
            UpdateDisplay();
        }

        internal void CutToClipboard()
        {
            if (string.IsNullOrEmpty(_textBuffer)) return;

            Clipboard.SetText(_textBuffer);
            SaveStateForUndo();
            _textBuffer = "";
            _cursorPosition = 0;
            UpdateDisplay();
        }
        #endregion

        #region Undo/Redo Operations
        internal void Undo()
        {
            if (_undoStack.Count == 0) return;

            _redoStack.Push(_textBuffer);
            _textBuffer = _undoStack.Pop();
            _cursorPosition = _textBuffer.Length;
            UpdateDisplay();
        }

        internal void Redo()
        {
            if (_redoStack.Count == 0) return;

            _undoStack.Push(_textBuffer);
            _textBuffer = _redoStack.Pop();
            _cursorPosition = _textBuffer.Length;
            UpdateDisplay();
        }

        internal void SaveStateForUndo()
        {
            _undoStack.Push(_textBuffer);
            _redoStack.Clear();
        }
        #endregion

        #region History Navigation
        private void NavigateHistory(int direction)
        {
            if (_historyStack.Count == 0) return;

            if (direction < 0 && _historyIndex > 0)
            {
                _historyIndex--;
                _textBuffer = _historyStack[_historyIndex];
            }
            else if (direction > 0)
            {
                if (_historyIndex < _historyStack.Count - 1)
                {
                    _historyIndex++;
                    _textBuffer = _historyStack[_historyIndex];
                }
                else if (_historyIndex == _historyStack.Count - 1)
                {
                    _historyIndex++;
                    _textBuffer = "";
                }
            }

            _cursorPosition = _textBuffer.Length;
            UpdateDisplay();
        }
        #endregion

        #region Display Updates
        private void UpdateDisplay()
        {
            if (!_isActive || string.IsNullOrEmpty(_config.MeterName) || !_resetOnce)
                return;

            _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _textBuffer.Length));

            string displayText = _textProcessor.PrepareDisplayText(_textBuffer, _cursorPosition, _config);

            if (_config.Width > 0 && displayText.Length > _config.Width)
            {
                displayText = _textProcessor.TruncateForDisplay(displayText, _cursorPosition, _config.Width);
            }

            _api.Execute($"!SetOption {_config.MeterName} Text \"\"\"{displayText}\"\"\"");
            _api.Execute($"!UpdateMeter \"{_config.MeterName}\"");
            _api.Execute("!Redraw");
        }

        public void UpdateMeter()
        {
            if (string.IsNullOrEmpty(_config.MeterName)) return;

            string text = _config.EnableInActiveValue ? _config.InActiveValue : _textBuffer;
            _api.Execute($"!SetOption \"{_config.MeterName}\" Text \"\"\"{text}\"\"\"");
            _api.Execute($"!UpdateMeter \"{_config.MeterName}\"");
            _api.Execute("!Redraw");
        }

        private void UpdateMeasure()
        {
            if (!_isActive || string.IsNullOrEmpty(_config.MeterName)) return;

            UpdateMeter();

            string defaultValue = _config.EnableInActiveValue ? "" : _textBuffer;
            _api.Execute($"!SetOption \"{MyName}\" DefaultValue \"\"\"{defaultValue}\"\"\"");
            _api.Execute($"!UpdateMeasure \"{MyName}\"");
        }

        private void UpdatePosition()
        {
            if (!_isActive) return;

            _api.Execute($"!SetOption \"{MyName}\" MeterX \"[{_config.MeterName}:X]\"");
            _api.Execute($"!SetOption \"{MyName}\" MeterY \"[{_config.MeterName}:Y]\"");
            _api.Execute($"!SetOption \"{MyName}\" MeterW \"[{_config.MeterName}:W]\"");
            _api.Execute($"!SetOption \"{MyName}\" MeterH \"[{_config.MeterName}:H]\"");
            _api.Execute($"!UpdateMeasure \"{MyName}\"");
        }
        #endregion

        #region Utility Methods
        private void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_config.CharacterLimit > 0 && _textBuffer.Length + text.Length > _config.CharacterLimit)
            {
                text = text.Substring(0, _config.CharacterLimit - _textBuffer.Length);
            }

            _textBuffer = _textBuffer.Insert(_cursorPosition, text);
            _cursorPosition = Math.Min(_textBuffer.Length, _cursorPosition + text.Length);

            // Reset preferred column when text is inserted
            _cursorManager.ResetPreferredColumn();
        }

        private char MapKeyToCharacterDynamic(int keyCode)
        {
            var result = new StringBuilder(2);
            var keyboardState = new byte[256];

            if (!GetKeyboardState(keyboardState))
                return '\0';

            int charsWritten = ToUnicode((uint)keyCode, 0, keyboardState, result, result.Capacity, 0);
            return charsWritten == 1 ? result[0] : '\0';
        }

        private Point GetMousePosition() => Cursor.Position;

        private bool IsMouseInsideMeter(Point mousePosition)
        {
            int meterGlobalX = _config.MeterX + _config.SkinX;
            int meterGlobalY = _config.MeterY + _config.SkinY;

            return mousePosition.X >= meterGlobalX &&
                   mousePosition.X <= meterGlobalX + _config.MeterWidth &&
                   mousePosition.Y >= meterGlobalY &&
                   mousePosition.Y <= meterGlobalY + _config.MeterHeight;
        }

        private void UpdateCursorWithMouse(Point mousePosition)
        {
            if (!IsMouseInsideMeter(mousePosition)) return;

            int relativeX = mousePosition.X - (_config.MeterX + _config.SkinX);
            int relativeY = mousePosition.Y - (_config.MeterY + _config.SkinY);

            _cursorPosition = _cursorManager.CalculateCursorPosition(_textBuffer, relativeX, relativeY,
                _config.MeterWidth, _config.MeterHeight);

            UpdateDisplay();
        }

        private Color ParseColor(string colorString, Color defaultColor)
        {
            try
            {
                string[] parts = colorString.Split(',');
                if (parts.Length == 3)
                {
                    int r = int.Parse(parts[0].Trim());
                    int g = int.Parse(parts[1].Trim());
                    int b = int.Parse(parts[2].Trim());
                    return Color.FromArgb(r, g, b);
                }
            }
            catch
            {
                _api.Log(API.LogType.Warning, $"Invalid color format: '{colorString}'. Using default color.");
            }
            return defaultColor;
        }
        #endregion
    }

    #region Helper Classes
    internal class PluginConfig
    {
        public string MeterName { get; set; } = "";
        public string InActiveValue { get; set; } = "";
        public bool FormatMultiline { get; set; } = false;
        public bool Disabled { get; set; } = false;
        public string Cursor { get; set; } = "|";
        public bool IsPassword { get; set; } = false;
        public bool IsMultiline { get; set; } = false;
        public int CharacterLimit { get; set; } = 0;
        public string OnEnterAction { get; set; } = "";
        public string OnESCAction { get; set; } = "";
        public string DefaultValue { get; set; } = "";
        public int Width { get; set; } = 0;
        public bool UnFocusDismiss { get; set; } = false;
        public string DismissAction { get; set; } = "";
        public bool EnableInActiveValue { get; set; } = false;
        public bool UseRegex { get; set; } = false;
        public string SubstituteRule { get; set; } = "";
        public bool ShowErrorForm { get; set; } = false;
        public bool ForceValidInput { get; set; } = false;
        public string UnValidAction { get; set; } = "";
        public string InputType { get; set; } = "string";
        public string AllowedCharacters { get; set; } = "";

        // Positioning
        public int MeterX { get; set; } = 0;
        public int MeterY { get; set; } = 0;
        public int MeterWidth { get; set; } = 0;
        public int MeterHeight { get; set; } = 0;
        public int SkinX { get; set; } = 0;
        public int SkinY { get; set; } = 0;

        // Colors
        public Color BackgroundColor { get; set; } = Color.FromArgb(30, 30, 30);
        public Color ButtonColor { get; set; } = Color.FromArgb(70, 70, 70);
        public Color TextColor { get; set; } = Color.White;
    }

    internal class InputValidator
    {
        private readonly PluginConfig _config;
        private readonly Rainmeter.API _api;

        public InputValidator(PluginConfig config, Rainmeter.API api)
        {
            _config = config;
            _api = api;
        }

        public bool IsValidInput(char keyChar, string inputType, int cursorPosition)
        {
            if (!_config.ForceValidInput) return true;

            return inputType switch
            {
                "integer" => char.IsDigit(keyChar) || (keyChar == '-' && cursorPosition == 0),
                "letters" => char.IsLetter(keyChar),
                "float" => char.IsDigit(keyChar) || keyChar == '.' || (keyChar == '-' && cursorPosition == 0),
                "alphanumeric" => char.IsLetterOrDigit(keyChar),
                "hexadecimal" => char.IsDigit(keyChar) || (keyChar >= 'a' && keyChar <= 'f') || (keyChar >= 'A' && keyChar <= 'F'),
                "custom" => !string.IsNullOrEmpty(_config.AllowedCharacters) && _config.AllowedCharacters.Contains(keyChar),
                _ => true
            };
        }

        public void ValidateTextBuffer(string textBuffer, string inputType, Measure measure)
        {
            bool isValid = inputType switch
            {
                "string" => true,
                "integer" => string.IsNullOrWhiteSpace(textBuffer) || int.TryParse(textBuffer, out _),
                "float" => string.IsNullOrWhiteSpace(textBuffer) || float.TryParse(textBuffer, out _),
                "hexadecimal" => string.IsNullOrWhiteSpace(textBuffer) || Regex.IsMatch(textBuffer, @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"),
                "email" => string.IsNullOrWhiteSpace(textBuffer) || Regex.IsMatch(textBuffer, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"),
                "alphanumeric" => string.IsNullOrWhiteSpace(textBuffer) || Regex.IsMatch(textBuffer, @"^[a-zA-Z0-9]+$"),
                "letters" => string.IsNullOrWhiteSpace(textBuffer) || Regex.IsMatch(textBuffer, @"^[a-zA-Z]+$"),
                "custom" => ValidateCustom(textBuffer),
                _ => true
            };

            if (!isValid)
            {
                measure.HandleDismiss();
                measure.ShowError($"Input is not valid. Only {inputType} is allowed.");
            }
            else
            {
                _api.Log(API.LogType.Debug, $"TextBuffer is a valid {inputType}.");
                measure.HandleValidInput();
            }
        }

        private bool ValidateCustom(string textBuffer)
        {
            if (string.IsNullOrWhiteSpace(textBuffer)) return true;
            if (string.IsNullOrEmpty(_config.AllowedCharacters))
            {
                _api.Log(API.LogType.Warning, "AllowedCharacters is empty. Custom validation cannot proceed.");
                return false;
            }

            string pattern = $"^[{Regex.Escape(_config.AllowedCharacters)}]+$";
            return Regex.IsMatch(textBuffer, pattern);
        }
    }

    internal class TextProcessor
    {
        private readonly PluginConfig _config;
        private readonly Rainmeter.API _api;

        public TextProcessor(PluginConfig config, Rainmeter.API api)
        {
            _config = config;
            _api = api;
        }

        public string PrepareDisplayText(string textBuffer, int cursorPosition, PluginConfig config)
        {
            string displayText;

            if (config.IsPassword)
            {
                displayText = string.Concat(textBuffer.Select(c => c == '\n' ? '\n' : '*'))
                    .Insert(cursorPosition, config.Cursor);
            }
            else
            {
                displayText = textBuffer.Insert(cursorPosition, config.Cursor);
            }

            if (!string.IsNullOrEmpty(config.SubstituteRule))
            {
                displayText = ApplySubstitution(displayText, config.SubstituteRule, config.UseRegex);
            }

            return displayText;
        }

        public string TruncateForDisplay(string displayText, int cursorPosition, int width)
        {
            if (displayText.Length <= width) return displayText;

            int startIndex = Math.Max(0, cursorPosition - width / 2);
            startIndex = Math.Min(startIndex, displayText.Length - width);
            return displayText.Substring(startIndex, width);
        }

        public void ConvertTextBufferToSingleLine(ref string textBuffer, bool formatMultiline)
        {
            if (!string.IsNullOrEmpty(textBuffer) && formatMultiline)
            {
                textBuffer = textBuffer.Replace("\r\n", " ").Replace("\n", " ");
            }
        }

        private string ApplySubstitution(string text, string substituteRule, bool useRegex)
        {
            if (string.IsNullOrEmpty(substituteRule)) return text;

            string[] rules = substituteRule.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rule in rules)
            {
                if (!TryParseRule(rule, out string pattern, out string replacement))
                    continue;

                if (string.IsNullOrEmpty(pattern))
                {
                    _api.Log(API.LogType.Warning, $"Skipping substitution rule with empty pattern: {rule}");
                    continue;
                }

                try
                {
                    if (useRegex)
                    {
                        Regex.Match("", pattern); // Validate regex
                        text = Regex.Replace(text, pattern, replacement);
                    }
                    else
                    {
                        text = text.Replace(pattern, replacement);
                    }
                }
                catch (Exception ex)
                {
                    _api.Log(API.LogType.Error, $"Failed to apply rule: {rule}. {ex.Message}");
                }
            }

            return text.Replace("#CRLF#", "\n");
        }

        private bool TryParseRule(string rule, out string pattern, out string replacement)
        {
            var separators = new[] { "\":\"", "\':'", "'\":\"", "'\':'" };
            string[] parts = rule.Split(separators, StringSplitOptions.None);

            if (parts.Length == 2)
            {
                pattern = parts[0].Trim('\"', '\'');
                replacement = parts[1].Trim('\"', '\'');
                return true;
            }

            pattern = replacement = null;
            return false;
        }
    }

    internal class CursorManager
    {
        private readonly PluginConfig _config;
        private int _preferredColumn = -1; // Remember the preferred column for vertical movement

        public CursorManager(PluginConfig config)
        {
            _config = config;
        }

        public void MoveCursorUp(ref int cursorPosition, string textBuffer)
        {
            try
            {
                if (cursorPosition == 0) return;

                var lineInfo = GetLineInfo(textBuffer, cursorPosition);
                if (lineInfo.LineIndex == 0)
                {
                    cursorPosition = 0;
                    _preferredColumn = 0;
                    return;
                }

                // Use preferred column if we have one, otherwise use current column
                int targetColumn = _preferredColumn >= 0 ? _preferredColumn : lineInfo.ColumnIndex;

                // Store the preferred column for subsequent moves
                if (_preferredColumn < 0) _preferredColumn = lineInfo.ColumnIndex;

                var previousLineInfo = GetLineInfoByIndex(textBuffer, lineInfo.LineIndex - 1);
                int newColumn = Math.Min(targetColumn, previousLineInfo.Length);

                cursorPosition = previousLineInfo.StartPosition + newColumn;
            }
            catch (Exception ex)
            {
                LogError($"Exception in MoveCursorUp: {ex.Message}");
            }
        }

        public void MoveCursorDown(ref int cursorPosition, string textBuffer)
        {
            try
            {
                if (cursorPosition >= textBuffer.Length) return;

                var lineInfo = GetLineInfo(textBuffer, cursorPosition);
                var lines = GetAllLines(textBuffer);

                if (lineInfo.LineIndex >= lines.Length - 1) return; // Already on last line

                // Use preferred column if we have one, otherwise use current column
                int targetColumn = _preferredColumn >= 0 ? _preferredColumn : lineInfo.ColumnIndex;

                // Store the preferred column for subsequent moves
                if (_preferredColumn < 0) _preferredColumn = lineInfo.ColumnIndex;

                var nextLineInfo = GetLineInfoByIndex(textBuffer, lineInfo.LineIndex + 1);
                int newColumn = Math.Min(targetColumn, nextLineInfo.Length);

                cursorPosition = nextLineInfo.StartPosition + newColumn;
            }
            catch (Exception ex)
            {
                LogError($"Exception in MoveCursorDown: {ex.Message}");
            }
        }

        public void ResetPreferredColumn()
        {
            _preferredColumn = -1;
        }

        private struct LineInfo
        {
            public int LineIndex;
            public int StartPosition;
            public int Length;
            public int ColumnIndex;
        }

        private LineInfo GetLineInfo(string textBuffer, int cursorPosition)
        {
            var lines = GetAllLines(textBuffer);
            int currentPos = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                int lineEnd = currentPos + lines[i].Length;

                if (cursorPosition >= currentPos && cursorPosition <= lineEnd)
                {
                    return new LineInfo
                    {
                        LineIndex = i,
                        StartPosition = currentPos,
                        Length = lines[i].Length,
                        ColumnIndex = cursorPosition - currentPos
                    };
                }

                currentPos = lineEnd + 1; // +1 for the newline character
            }

            // Fallback for edge cases
            return new LineInfo
            {
                LineIndex = lines.Length - 1,
                StartPosition = Math.Max(0, textBuffer.Length - (lines.LastOrDefault()?.Length ?? 0)),
                Length = lines.LastOrDefault()?.Length ?? 0,
                ColumnIndex = 0
            };
        }

        private LineInfo GetLineInfoByIndex(string textBuffer, int lineIndex)
        {
            var lines = GetAllLines(textBuffer);
            if (lineIndex < 0 || lineIndex >= lines.Length)
            {
                return new LineInfo { LineIndex = -1, StartPosition = 0, Length = 0, ColumnIndex = 0 };
            }

            int startPos = 0;
            for (int i = 0; i < lineIndex; i++)
            {
                startPos += lines[i].Length + 1; // +1 for newline
            }

            return new LineInfo
            {
                LineIndex = lineIndex,
                StartPosition = startPos,
                Length = lines[lineIndex].Length,
                ColumnIndex = 0
            };
        }

        private string[] GetAllLines(string textBuffer)
        {
            if (string.IsNullOrEmpty(textBuffer)) return new string[] { "" };
            return textBuffer.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        public int CalculateCursorPosition(string textBuffer, int relativeX, int relativeY, int meterWidth, int meterHeight)
        {
            string[] lines = textBuffer.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            if (lines.Length == 0) return 0;

            int lineHeight = meterHeight / lines.Length;
            int clickedLine = Math.Max(0, Math.Min(relativeY / lineHeight, lines.Length - 1));

            string line = lines[clickedLine];
            int maxLineLength = lines.Max(l => l.Length);
            int charWidth = maxLineLength > 0 ? meterWidth / maxLineLength : meterWidth;
            int charIndexInLine = charWidth > 0 ? Math.Max(0, Math.Min(relativeX / charWidth, line.Length)) : 0;

            int newCursorPosition = 0;
            for (int i = 0; i < clickedLine; i++)
            {
                newCursorPosition += lines[i].Length + 1; // +1 for newline
            }
            newCursorPosition += charIndexInLine;

            return Math.Max(0, Math.Min(newCursorPosition, textBuffer.Length));
        }

        private void LogError(string message)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "Rainmeter_ErrorLog.txt");
                string logMessage = $"{DateTime.Now}: {message}\n";
                File.AppendAllText(tempPath, logMessage);
            }
            catch
            {
                // Silently fail to avoid crashing
            }
        }
    }
    #endregion
}