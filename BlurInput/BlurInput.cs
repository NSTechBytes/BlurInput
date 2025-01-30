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
        public API api;
        public string myName;
        public int Disabled;
        private string MeterName = "";
        private string TextBuffer = "";
        private string OnEnterAction;
        private string OnESCAction;
        private string Cursor = "|";
        private int CursorPosition = 0;
        private Rainmeter.API Api;
        private string defaultValue = "";
        private int FormatMultiline = 0;
        private int ShowErrorForm = 1;
        private string DismissAction;
        private int ForceValidInput = 1;
        private Stack<string> UndoStack = new Stack<string>();
        private Stack<string> RedoStack = new Stack<string>();
        private bool CapsLockActive = false;
        private const string TabSpaces = "    ";
        private bool IsActive = false;
        private bool IsPassword = false;
        private bool IsMultiline = false;
        private int CharacterLimit = 0;
        private int Width = 0;
        private string InputType = "String";
        private string AllowedCharacters = "";
        private bool isTextCleared = false;
        private bool isInitialized = false;
        private System.Timers.Timer updateTimer;
        private const double UpdateInterval = 25;
        private int UnFocusDismiss = 0;
        private System.Timers.Timer resetTimer;
        private bool hasResetOnce = false;
        private string substituteRule = "";
        private string InActiveValue = "";
        private int useRegex = 0;
        private int MeterX,
            MeterY,
            MeterWidth,
            MeterHeight;
        private int SkinX,
            SkinY;
        public bool ContextFocusForm = false;
        public bool ContextFormOpen = false;
        private string UnValidAction = "";
        public Color BackgroundColor = Color.FromArgb(30, 30, 30);
        public Color ButtonColor = Color.FromArgb(70, 70, 70);
        public Color TextColor = Color.White;
        private int EnableInActiveValue = 0;
        private List<string> HistoryStack = new List<string>();
        private int HistoryIndex = -1;
        private bool  ResetOnce = false;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern int ToUnicode(
            uint virtualKey,
            uint scanCode,
            byte[] keyboardState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder receivingBuffer,
            int bufferSize,
            uint flags
        );

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        public string GetUserInput()
        {
            return TextBuffer;
        }

        //=================================================================================================================================//
        //                                                      Reload                                                                     //
        //=================================================================================================================================//

        internal void Reload(Rainmeter.API api, ref double maxValue)
        {
            Api = api;
            TextBuffer = ApplySubstitution(TextBuffer, substituteRule, useRegex);
            updateTimer = new System.Timers.Timer(UpdateInterval);
            updateTimer.Elapsed += (sender, e) => UpdateTest();
            updateTimer.AutoReset = true;
            MeterName = api.ReadString("MeterName", "");
            InActiveValue = api.ReadString("InActiveValue", "");
            FormatMultiline = api.ReadInt("FormatMultiline", 0);
            Disabled = api.ReadInt("Disabled", 0);
            Cursor = api.ReadString("Cursor", "|");
            IsPassword = api.ReadInt("Password", 0) == 1;
            IsMultiline = api.ReadInt("Multiline", 0) == 1;
            CharacterLimit = api.ReadInt("InputLimit", 0);
            OnEnterAction = api.ReadString("OnEnterAction", "").Trim();
            OnESCAction = api.ReadString("OnESCAction", "").Trim();
            defaultValue = api.ReadString("DefaultValue", "").Trim();
            Width = api.ReadInt("ViewLimit", 0);
            myName = api.GetMeasureName();
            UnFocusDismiss = api.ReadInt("UnFocusDismiss", 0);
            DismissAction = api.ReadString("OnDismissAction", "");
            MeterX = api.ReadInt("MeterX", 0);
            EnableInActiveValue = api.ReadInt("SetInActiveValue", 0);
            MeterY = api.ReadInt("MeterY", 0);
            MeterWidth = api.ReadInt("MeterW", 0);
            MeterHeight = api.ReadInt("MeterH", 0);
            SkinX = int.Parse(api.ReplaceVariables("#CURRENTCONFIGX#"));
            SkinY = int.Parse(api.ReplaceVariables("#CURRENTCONFIGY#"));
            useRegex = api.ReadInt("RegExpSubstitute", 0);
            substituteRule = api.ReadString("Substitute", "");
            ShowErrorForm = api.ReadInt("ShowErrorDialog", 0);
            ForceValidInput = api.ReadInt("ForceValidInput", 0);
            UnValidAction = api.ReadString("OnInvalidAction", "");
            string backgroundColorString = api.ReadString("FormBackgroundColor", "30,30,30");
            string buttonColorString = api.ReadString("FormButtonColor", "70,70,70");
            string textColorString = api.ReadString("FormTextColor", "255,255,255");
            BackgroundColor = ParseColor(backgroundColorString, BackgroundColor);
            ButtonColor = ParseColor(buttonColorString, ButtonColor);
            TextColor = ParseColor(textColorString, TextColor);
            InputType = api.ReadString("InputType", "String").Trim().ToLowerInvariant();
         
            if (
                InputType != "string"
                && InputType != "integer"
                && InputType != "float"
                && InputType != "letters"
                && InputType != "alphanumeric"
                && InputType != "hexadecimal"
                && InputType != "email"
                && InputType != "custom"
            )
            {
                api.Log(
                    API.LogType.Warning,
                    $"Invalid InputType '{InputType}', defaulting to 'String'."
                );
                InputType = "String";
            }

            if (InputType == "Custom")
            {
                AllowedCharacters = api.ReadString("AllowedCharacters", "");
              
                if (string.IsNullOrEmpty(AllowedCharacters))
                {
                    api.Log(
                        API.LogType.Warning,
                        "InputType 'Custom' requires 'AllowedCharacters'. Defaulting to 'String'."
                    );
                    InputType = "String";
                }
            }

            if (!isInitialized)
            {
                TextBuffer =
                    defaultValue.Length > CharacterLimit && CharacterLimit > 0
                        ? defaultValue.Substring(0, CharacterLimit)
                        : defaultValue;

                CursorPosition = TextBuffer.Length;
                isInitialized = true;
            }

            if (CharacterLimit > 0 && TextBuffer.Length > CharacterLimit)
            {
                TextBuffer = TextBuffer.Substring(0, CharacterLimit);
                CursorPosition = Math.Min(CursorPosition, CharacterLimit);
            }

            CursorPosition = Math.Max(0, Math.Min(CursorPosition, TextBuffer.Length));
        }

        internal void UpdateTest()
        {
            if (!IsActive)
                return;
            GetPos();
        }

        internal void GetPos()
        {
            Api.Execute($"!SetOption  \"{myName}\" MeterX \"[{MeterName}:X]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterY \"[{MeterName}:Y]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterW \"[{MeterName}:W]\" ");
            Api.Execute($"!SetOption  \"{myName}\" MeterH \"[{MeterName}:H]\" ");
            Api.Execute($"!UpdateMeasure  \"{myName}\"");
        }

        private Point GetMousePosition()
        {
            return System.Windows.Forms.Cursor.Position;
        }

        private bool IsMouseInsideMeter(Point mousePosition)
        {
            int meterGlobalX = MeterX + SkinX;
            int meterGlobalY = MeterY + SkinY;

            return mousePosition.X >= meterGlobalX
                && mousePosition.X <= meterGlobalX + MeterWidth
                && mousePosition.Y >= meterGlobalY
                && mousePosition.Y <= meterGlobalY + MeterHeight;
        }

        //=================================================================================================================================//
        //                                                     Update                                                                      //
        //=================================================================================================================================//
        internal void Update()
        {
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            if (
                Control.MouseButtons == MouseButtons.Left
                || Control.MouseButtons == MouseButtons.Right
            )
            {
                Point mousePosition = GetMousePosition();

                if (IsMouseInsideMeter(mousePosition))
                {
                    if (Control.MouseButtons == MouseButtons.Right)
                    {
                        ShowContextForm();
                    }
                    //  Api.Execute($"!Log  \"Meter In Focus\"");
                }
                else
                {
                    if (UnFocusDismiss == 1 && IsActive && ContextFocusForm)
                    {
                        UnFocusDismissHandler();
                        //Api.Execute($"!Log  \"Meter In UnFocus\"");
                    }
                }
            }
            if (Control.MouseButtons == MouseButtons.Left)
            {
                UpdateCursorWithMouse();
            }
            bool ctrlPressed = (GetAsyncKeyState(17) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(16) & 0x8000) != 0;
            CapsLockActive = (GetKeyState(20) & 0x0001) != 0;

            for (int i = 8; i <= 255; i++)
            {
                if ((GetAsyncKeyState(i) & 0x0001) != 0)
                {
                    if (i == 13 && ctrlPressed)
                    {
                        HandleCtrlEnter();
                        return;
                    }

                    if (ctrlPressed)
                    {
                        HandleCtrlShortcuts(i);
                    }
                    else
                    {
                        HandleSpecialKeys(i, shiftPressed, ctrlPressed);
                    }

                    UpdateText();
                }
            }
        }

        //=================================================================================================================================//
        //                                                      KeyBoardControl                                                            //
        //=================================================================================================================================//
        private void HandleCtrlShortcuts(int keyCode)
        {
            switch (keyCode)
            {
                case 67:
                    CopyToClipboard();
                    break;
                case 86:
                    PasteFromClipboard();
                    break;
                case 88:
                    CutToClipboard();
                    break;
                case 90:
                    Undo();
                    break;
                case 89:
                    Redo();
                    break;
            }
        }

        private void HandleSpecialKeys(int keyCode, bool shiftPressed, bool ctrlPressed)
        {
            SaveStateForUndo();

            switch (keyCode)
            {
                case 8:
                    if (CursorPosition > 0)
                    {
                        TextBuffer = TextBuffer.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                    }
                    return;

                case 27:
                    ESCHandler();
                    return;

                case 13:
                    if (IsMultiline && !ctrlPressed)
                    {
                        InsertText("\n");
                    }
                    else
                    {
                        ValidateTextBuffer(InputType);
                    }
                    return;

                case 46:
                    if (CursorPosition < TextBuffer.Length)
                    {
                        TextBuffer = TextBuffer.Remove(CursorPosition, 1);
                    }
                    return;

                case 37:
                    if (CursorPosition > 0)
                        CursorPosition--;
                    return;

                case 39:
                    if (CursorPosition < TextBuffer.Length)
                        CursorPosition++;
                    return;

                case 36:
                    CursorPosition = 0;
                    return;

                case 35:
                    CursorPosition = TextBuffer.Length;
                    return;

                case 9:
                    TextBuffer = TextBuffer.Insert(CursorPosition, TabSpaces);
                    CursorPosition += TabSpaces.Length;
                    return;

                case 20:
                    CapsLockActive = !CapsLockActive;
                    return;

                case 38:
                    if (IsMultiline)
                    {
                        MoveCursorUp();
                    }
                    else
                    {
                        NavigateHistory(-1);
                    }
                    return;

                case 40:
                    if (IsMultiline)
                    {
                        MoveCursorDown();
                    }
                    else
                    {
                        NavigateHistory(1);
                    }
                    return;
            }

            char keyChar = MapKeyToCharacterDynamic(keyCode);

            if (keyChar != '\0')
            {
                if (IsValidInput(keyChar))
                {
                    InsertText(keyChar.ToString());
                }
            }
        }

        private char MapKeyToCharacterDynamic(int keyCode)
        {
            StringBuilder result = new StringBuilder(2);

            byte[] keyboardState = new byte[256];
            if (!GetKeyboardState(keyboardState))
            {
                return '\0';
            }

            int charsWritten = ToUnicode(
                (uint)keyCode,
                0,
                keyboardState,
                result,
                result.Capacity,
                0
            );

            if (charsWritten == 1)
            {
                return result[0];
            }

            return '\0';
        }

        private void MoveCursorUp()
        {
            try
            {
                if (CursorPosition == 0)
                {
                 //   LogError("MoveCursorUp() called, but CursorPosition is already 0.");
                    return;
                }

                // Find the start of the current line
                int currentLineStart = TextBuffer.LastIndexOf('\n', CursorPosition - 1);
              //  LogError($"CursorPosition: {CursorPosition}, CurrentLineStart: {currentLineStart}");

                if (currentLineStart == -1)
                {
                    CursorPosition = 0;
                   // LogError("No previous line found. Cursor moved to 0.");
                    return;
                }

                // Find the start of the previous line
                int previousLineStart = TextBuffer.LastIndexOf('\n', currentLineStart - 1);
               // LogError($"PreviousLineStart: {previousLineStart}");

                if (previousLineStart == -1)
                {
                    CursorPosition = 0;
                    //LogError("Previous line does not exist. Cursor moved to 0.");
                    return;
                }

                // Find offset within the current line
                int lineOffset = CursorPosition - (currentLineStart + 1);
              //  LogError($"LineOffset: {lineOffset}");

                // Move cursor safely
                CursorPosition = Math.Min(previousLineStart + 1 + lineOffset, currentLineStart);
               // LogError($"Cursor moved up to {CursorPosition}");
            }
            catch (Exception ex)
            {
              //  LogError($"Exception in MoveCursorUp: {ex.Message}\n{ex.StackTrace}");
            }
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
                // If logging fails, avoid crashing the program.
            }
        }



        private void MoveCursorDown()
        {
            try
            {
                if (CursorPosition >= TextBuffer.Length)
                {
                   // LogError("MoveCursorDown() called, but CursorPosition is already at the end.");
                    return; // Already at the last position
                }

                // Find the start of the current line
                int currentLineStart = TextBuffer.LastIndexOf('\n', CursorPosition - 1);
                if (currentLineStart == -1)
                {
                    currentLineStart = -1; // Special case: first line
                }

                // Find the end of the current line
                int currentLineEnd = TextBuffer.IndexOf('\n', CursorPosition);
                if (currentLineEnd == -1)
                {
                  //  LogError("No next line exists. Cursor does not move.");
                    return; // No next line exists
                }

                // If the cursor is at position 0 and the first line is empty, move directly to the second line
                if (CursorPosition == 0 && currentLineEnd == 0)
                {
                    CursorPosition = 1;
                   // LogError($"Cursor was at position 0 on an empty line, moved to {CursorPosition}");
                    return;
                }

                // Find the start of the next line
                int nextLineStart = currentLineEnd + 1;
                if (nextLineStart >= TextBuffer.Length)
                {
                   // LogError("Next line start is at or beyond the text length. Cursor does not move.");
                    return;
                }

                // Find the end of the next line
                int nextLineEnd = TextBuffer.IndexOf('\n', nextLineStart);
                if (nextLineEnd == -1)
                {
                    nextLineEnd = TextBuffer.Length;
                }

                // Compute cursor offset in the current line
                int lineOffset = CursorPosition - (currentLineStart + 1);

                // Move cursor to the next line with the same column offset (or as close as possible)
                CursorPosition = Math.Min(nextLineStart + lineOffset, nextLineEnd);
               // LogError($"Cursor moved down to {CursorPosition}");
           }
            catch (Exception ex)
            {
                //LogError($"Exception in MoveCursorDown: {ex.Message}\n{ex.StackTrace}");
            }
        }


        private void NavigateHistory(int direction)
        {
            if (HistoryStack == null || HistoryStack.Count == 0)
                return;

            if (direction < 0)
            {
                if (HistoryIndex > 0)
                {
                    HistoryIndex--;
                    TextBuffer = HistoryStack[HistoryIndex];
                    CursorPosition = TextBuffer.Length;
                    UpdateText();
                }
            }
            else if (direction > 0)
            {
                if (HistoryIndex < HistoryStack.Count - 1)
                {
                    HistoryIndex++;
                    TextBuffer = HistoryStack[HistoryIndex];
                    CursorPosition = TextBuffer.Length;
                    UpdateText();
                }
                else if (HistoryIndex == HistoryStack.Count - 1)
                {
                    HistoryIndex++;
                    TextBuffer = string.Empty;
                    CursorPosition = 0;
                    UpdateText();
                }
            }
        }

        /*private void AddToHistory(string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                HistoryStack.Add(command);
                if (HistoryStack.Count > 100) // Limit history size
                {
                    HistoryStack.RemoveAt(0);
                }
                HistoryIndex = HistoryStack.Count; // Reset index
            }
        }
        */
        //=================================================================================================================================//
        //                                                      KeyBoard Functions                                                         //
        //=================================================================================================================================//

        public void ESCHandler()
        {
            if (!IsActive)
                return;
            IsActive = false;
            hasResetOnce = false;
            ResetOnce = false;
            Api.Execute(OnESCAction);
            TextBuffer = defaultValue;
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();
            UpdateMeter();
        }

        public void UpdateMeter()
        {
            if (!string.IsNullOrEmpty(MeterName))
            {
                if (EnableInActiveValue == 1)
                {
                    Api.Execute($"!SetOption  \"{MeterName}\" Text \"\"\"{InActiveValue}\"\"\" ");
                }
                else
                {
                    Api.Execute($"!SetOption  \"{MeterName}\" Text \"\"\"{TextBuffer}\"\"\" ");
                }

                Api.Execute($"!UpdateMeter  \"{MeterName}\" ");
                Api.Execute($"!Redraw");
            }
        }

        private void UpdateMeasure()
        {
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            UpdateMeter();
            if (EnableInActiveValue == 1)
            {
                Api.Execute($"!SetOption  \"{myName}\" DefaultValue \"\" ");
            }
            else
            {
                Api.Execute($"!SetOption  \"{myName}\" DefaultValue \"\"\"{TextBuffer}\"\"\" ");
            }
                
            Api.Execute($"!UpdateMeasure  \"{myName}\" ");
        }

        public void DismissHandler()
        {
            if (!IsActive)
                return;

            IsActive = false;
            hasResetOnce = false;
            ResetOnce = false;
            Api.Execute(UnValidAction);
            TextBuffer = defaultValue;
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();
            UpdateMeter();
        }

        public void UnFocusDismissHandler()
        {
            if (!IsActive)
                return;
            IsActive = false;
            hasResetOnce = false;
            ResetOnce = false;
            Api.Execute(DismissAction);
            TextBuffer = defaultValue;
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();
            UpdateMeter();
        }

        private void HandleCtrlEnter()
        {
            ValidateTextBuffer(InputType);
        }

        internal void CopyToClipboard()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                Clipboard.SetText(TextBuffer);
            }
        }

        internal void PasteFromClipboard()
        {
            if (Clipboard.ContainsText())
            {
                SaveStateForUndo();
                string clipboardText = Clipboard.GetText();
                InsertText(clipboardText.Trim());
                UpdateText();
            }
            else
            {
                Api.Log(API.LogType.Warning, "Clipboard does not contain text.");
            }
        }

        internal void CutToClipboard()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                Clipboard.SetText(TextBuffer);
                SaveStateForUndo();
                TextBuffer = "";
                CursorPosition = 0;
                UpdateText();
            }
        }

        internal void Undo()
        {
            if (UndoStack.Count > 0)
            {
                RedoStack.Push(TextBuffer);
                TextBuffer = UndoStack.Pop();
                CursorPosition = TextBuffer.Length;
                UpdateText();
            }
        }

        internal void Redo()
        {
            if (RedoStack.Count > 0)
            {
                UndoStack.Push(TextBuffer);
                TextBuffer = RedoStack.Pop();
                CursorPosition = TextBuffer.Length;
                UpdateText();
            }
        }

        internal void SaveStateForUndo()
        {
            UndoStack.Push(TextBuffer);
            RedoStack.Clear();
            UpdateText();
        }

        //=================================================================================================================================//
        //                                                     CommonFunctions                                                             //
        //=================================================================================================================================//
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
                Api.Log(
                    Rainmeter.API.LogType.Warning,
                    $"Invalid color format: '{colorString}'. Using default color."
                );
            }
            return defaultColor;
        }

        private void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (CharacterLimit > 0 && TextBuffer.Length + text.Length > CharacterLimit)
            {
                text = text.Substring(0, CharacterLimit - TextBuffer.Length);
            }
            TextBuffer = TextBuffer.Insert(CursorPosition, text);
            CursorPosition = Math.Min(TextBuffer.Length, CursorPosition + text.Length);
        }

        private void UpdateText()
        {
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            if (!ResetOnce)
                return;

            CursorPosition = Math.Max(0, Math.Min(CursorPosition, TextBuffer.Length));

            string displayText;

            if (IsPassword)
            {
                displayText = string.Concat(TextBuffer.Select(c => c == '\n' ? '\n' : '*'))
                    .Insert(CursorPosition, Cursor);
            }
            else
            {
                displayText = TextBuffer.Insert(CursorPosition, Cursor);
            }

            string substituteRule = Api.ReadString("Substitute", "");
            if (!string.IsNullOrEmpty(substituteRule))
            {
                int originalLength = displayText.Length;
                displayText = ApplySubstitution(displayText, substituteRule, useRegex);

                int newLength = displayText.Length;
                if (newLength != originalLength)
                {
                    int change = newLength - originalLength;
                    CursorPosition = Math.Max(0, Math.Min(CursorPosition + change, newLength));
                }
            }

            if (Width > 0 && displayText.Length > Width)
            {
                int startIndex = Math.Max(0, CursorPosition - Width / 2);
                startIndex = Math.Min(startIndex, displayText.Length - Width);
                displayText = displayText.Substring(startIndex, Width);
            }

            Api.Execute($"!SetOption  {MeterName} Text \"\"\"{displayText}\"\"\"");
            Api.Execute($"!UpdateMeter \"{MeterName}\"");
            Api.Execute("!Redraw");
        }

        private void UpdateCursorWithMouse()
        {
            if (!IsActive || string.IsNullOrEmpty(MeterName))
                return;

            Point mousePosition = GetMousePosition();

            if (IsMouseInsideMeter(mousePosition))
            {
                int relativeX = mousePosition.X - (MeterX + SkinX);
                int relativeY = mousePosition.Y - (MeterY + SkinY);

                string[] lines = TextBuffer.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

                int lineHeight = lines.Length > 0 ? MeterHeight / lines.Length : MeterHeight;

                int clickedLine = relativeY / lineHeight;

                clickedLine = Math.Max(0, Math.Min(clickedLine, lines.Length - 1));

                string line = lines[clickedLine];

                int maxLineLength = lines.Max(l => l.Length);
                int charWidth = maxLineLength > 0 ? MeterWidth / maxLineLength : MeterWidth;

                int charIndexInLine = charWidth > 0 ? relativeX / charWidth : 0;

                charIndexInLine = Math.Max(0, Math.Min(charIndexInLine, line.Length));

                int newCursorPosition = 0;
                for (int i = 0; i < clickedLine; i++)
                {
                    newCursorPosition += lines[i].Length + 1;
                }
                newCursorPosition += charIndexInLine;

                CursorPosition = Math.Max(0, Math.Min(newCursorPosition, TextBuffer.Length));

                UpdateText();
            }
        }

        private string ApplySubstitution(string text, string substituteRule, int useRegex = 1)
        {
            if (string.IsNullOrEmpty(substituteRule))
                return text;

            string[] rules = substituteRule.Split(
                new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries
            );
            foreach (string rule in rules)
            {
                if (TryParseRule(rule, out string pattern, out string replacement))
                {
                    if (string.IsNullOrEmpty(pattern))
                    {
                        Api.Log(
                            API.LogType.Warning,
                            $"Skipping substitution rule with empty pattern: {rule}"
                        );
                        continue;
                    }

                    try
                    {
                        if (useRegex == 1)
                        {
                            Regex.Match("", pattern);
                            text = Regex.Replace(text, pattern, replacement);
                        }
                        else
                        {
                            text = text.Replace(pattern, replacement);
                        }
                    }
                    catch (Exception ex)
                    {
                        Api.Log(API.LogType.Error, $"Failed to apply rule: {rule}. {ex.Message}");
                    }
                }
                /* else
                 {
                     Api.Log(API.LogType.Warning, $"Invalid rule format: {rule}");
                 }*/
            }

            text = text.Replace("#CRLF#", "\n");

            return text;
        }

        private bool TryParseRule(string rule, out string pattern, out string replacement)
        {
            string[] parts = rule.Split(
                new[] { "\":\"", "\':'", "'\":\"", "'\':'" },
                StringSplitOptions.None
            );
            if (parts.Length == 2)
            {
                pattern = parts[0].Trim('\"', '\'');
                replacement = parts[1].Trim('\"', '\'');
                return true;
            }
            pattern = replacement = null;
            return false;
        }

        //=================================================================================================================================//
        //                                                     Bangs Functions                                                             //
        //=================================================================================================================================//

        internal void ClearText()
        {
            TextBuffer = "";
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            UpdateText();
        }

        internal void Start()
        {
            if (Disabled == 1)
                return;

            if (IsActive)
            {
                Api.Log(API.LogType.Debug, "Plugin is already running. Start operation skipped.");
                return;
            }
            if (UnFocusDismiss == 1)
            {
                ContextFocusForm = true;
            }
            IsActive = true;
            hasResetOnce = false;
            CursorPosition = TextBuffer.Length;
            updateTimer.Start();

            if (!hasResetOnce)
            {
                hasResetOnce = true;
                resetTimer = new System.Timers.Timer(50);
                resetTimer.Elapsed += (sender, e) =>
                {
                    ResetToDefaultValue();
                    resetTimer.Stop();
                };
                resetTimer.AutoReset = false;
                resetTimer.Start();
            }
        }

        // This is used to reset textbuffer to default value as
        // the plugin why not listening when the plugin is Stop.
        private void ResetToDefaultValue()
        {
            TextBuffer = defaultValue;
            CursorPosition = TextBuffer.Length;
            UndoStack.Clear();
            RedoStack.Clear();
            ResetOnce = true;        
            UpdateText();
        }

        internal void ShowContextForm()
        {
            if (!IsActive)
                return;

            if (ContextFormOpen)
                return;

            ContextFocusForm = false;
            ContextFormOpen = true;
            ContextForm contextForm = new ContextForm(
                this,
                BackgroundColor,
                ButtonColor,
                TextColor
            );
            contextForm.ShowDialog();
        }

        internal void Stop()
        {
            if (!IsActive)
                return;

            IsActive = false;
            hasResetOnce = false;
            ResetOnce = false;
            CursorPosition = 0;
            UndoStack.Clear();
            RedoStack.Clear();
            updateTimer.Stop();
            updateTimer.Stop();
        }

        //=================================================================================================================================//
        //                                                      ValidInputs                                                                //
        //=================================================================================================================================//
        private bool IsValidInput(char keyChar)
        {
            if (ForceValidInput == 1)
            {
                switch (InputType)
                {
                    case "integer":
                        return char.IsDigit(keyChar) || (keyChar == '-' && CursorPosition == 0);
                    case "letters":
                        return char.IsLetter(keyChar);
                    case "default":
                        return true;
                    default:
                        return true;
                }
            }
            else
            {
                return true;
            }
        }

        private bool IsTextBufferString()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return true;
        }

        private bool IsTextBufferInteger()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return int.TryParse(TextBuffer, out _);
        }

        private bool IsTextBufferFloat()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return float.TryParse(TextBuffer, out _);
        }

        private bool IsTextBufferHexadecimal()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z");
        }

        private bool IsTextBufferEmail()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsTextBufferAlphanumeric()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"^[a-zA-Z0-9]+$");
        }

        private bool IsTextBufferLetters()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            return Regex.IsMatch(TextBuffer, @"^[a-zA-Z]+$");
        }

        private bool IsTextBufferCustom()
        {
            if (string.IsNullOrWhiteSpace(TextBuffer))
                return true;

            if (string.IsNullOrEmpty(AllowedCharacters))
            {
                Api.Log(
                    API.LogType.Warning,
                    "AllowedCharacters is empty. Custom validation cannot proceed."
                );
                return false;
            }

            string pattern = $"^[{Regex.Escape(AllowedCharacters)}]+$";
            return Regex.IsMatch(TextBuffer, pattern);
        }

        private void ValidateTextBuffer(string inputType)
        {
            bool isValid = false;

            switch (inputType)
            {
                case "string":
                    isValid = IsTextBufferString();
                    break;
                case "integer":
                    isValid = IsTextBufferInteger();
                    break;
                case "float":
                    isValid = IsTextBufferFloat();
                    break;
                case "hexadecimal":
                    isValid = IsTextBufferHexadecimal();
                    break;
                case "email":
                    isValid = IsTextBufferEmail();
                    break;
                case "alphanumeric":
                    isValid = IsTextBufferAlphanumeric();
                    break;
                case "letters":
                    isValid = IsTextBufferLetters();
                    break;
                case "custom":
                    isValid = IsTextBufferCustom();
                    break;
                default:
                    Api.Log(
                        API.LogType.Warning,
                        $"Unknown InputType: {inputType}. Validation skipped."
                    );
                    return;
            }

            if (!isValid)
            {
                DismissHandler();
                ShowError($"Input is not a valid.Only allow {inputType}.");
            }
            else
            {
                Api.Log(API.LogType.Debug, $"TextBuffer is a valid {inputType}.");
                ConvertTextBufferToSingleLine();
                UpdateMeasure();
                Api.Execute(OnEnterAction);
                Stop();
            }
        }

        private void ShowError(string errorMessage)
        {
            if (ShowErrorForm == 1)
            {
                ErrorForm errorForm = new ErrorForm(
                    errorMessage,
                    BackgroundColor,
                    ButtonColor,
                    TextColor
                );
                errorForm.ShowDialog();
            }
            else
            {
                Api.Log(API.LogType.Error, errorMessage);
            }
        }

        internal void ConvertTextBufferToSingleLine()
        {
            if (!string.IsNullOrEmpty(TextBuffer))
            {
                if (FormatMultiline == 1)
                {
                    TextBuffer = TextBuffer.Replace("\r\n", " ").Replace("\n", " ");
                }
            }
        }
    }
}